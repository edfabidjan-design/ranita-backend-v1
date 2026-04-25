// wwwroot/assets/js/admin-product-detail.js
(() => {
    "use strict";

    const qs = (s, r = document) => r.querySelector(s);
    const qsa = (s, r = document) => Array.from(r.querySelectorAll(s));

    const msg = qs("#msg");
    const pTitle = qs("#pTitle");
    const pSub = qs("#pSub");
    const pBadge = qs("#pBadge");

    const mainImg = qs("#mainImg");
    const thumbs = qs("#thumbs");

    const infoRow = qs("#infoRow");
    const attrsBox = qs("#attrsBox");

    const varsHint = qs("#varsHint");
    const varsBody = qs("#varsBody");
    const th1 = qs("#th1");
    const th2 = qs("#th2");

    const btnBack = qs("#btnBack");
    const btnPublish = qs("#btnPublish");
    const btnReject = qs("#btnReject");
    const rejectReason = qs("#rejectReason");
    const rightInfo = qs("#rightInfo");

    function esc(s) {
        return String(s ?? "").replace(/[&<>"']/g, c => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;"
        }[c]));
    }

    function money(n) {
        return (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";
    }

    function fmtDate(dt) {
        if (!dt) return "—";
        const d = new Date(dt);
        if (isNaN(d.getTime())) return String(dt);
        return d.toLocaleString("fr-FR", { year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit" });
    }

    function getQuery() {
        const u = new URL(location.href);
        const id = Number(u.searchParams.get("id") || 0) || 0;
        const vendorId = Number(u.searchParams.get("vendorId") || 0) || 0;
        const tab = String(u.searchParams.get("tab") || "Pending");
        return { id, vendorId, tab };
    }

    function setMsg(t) { msg.textContent = t || ""; }

    function setBadge(status, isDeleted) {
        let st = String(status || "").trim();
        if (isDeleted) st = "Deleted";
        if (!st) st = "Pending";

        pBadge.className = "badge";
        const low = st.toLowerCase();

        if (low === "pending") pBadge.classList.add("b-pending");
        else if (low === "published") pBadge.classList.add("b-published");
        else if (low === "rejected") pBadge.classList.add("b-rejected");
        else pBadge.classList.add("b-deleted");

        pBadge.textContent = st;
    }

    function pickImages(it) {
        const arr = it?.Images || it?.images || [];
        return (arr || []).map(x => ({
            id: Number(x?.Id ?? x?.id ?? 0) || 0,
            url: String(x?.Url ?? x?.url ?? "").trim(),
            isMain: (x?.IsMain ?? x?.isMain) === true,
            sortOrder: Number(x?.SortOrder ?? x?.sortOrder ?? 0) || 0
        })).filter(x => x.url);
    }

    function sortImages(images) {
        return images.slice().sort((a, b) => {
            const am = a.isMain ? 1 : 0;
            const bm = b.isMain ? 1 : 0;
            if (am !== bm) return bm - am;
            return (a.sortOrder || 0) - (b.sortOrder || 0);
        });
    }

    function renderGallery(images) {
        const list = sortImages(images);

        if (!list.length) {
            mainImg.src = "/assets/placeholder.png";
            thumbs.innerHTML = "";
            return;
        }

        const main = list[0];
        mainImg.src = main.url;

        thumbs.innerHTML = list.map((im, idx) => `
      <img class="th ${idx === 0 ? "active" : ""}" data-url="${esc(im.url)}" src="${esc(im.url)}" alt="">
    `).join("");

        thumbs.onclick = (e) => {
            const el = e.target.closest("img.th");
            if (!el) return;
            const url = el.dataset.url || "";
            if (!url) return;
            mainImg.src = url;
            qsa("img.th", thumbs).forEach(x => x.classList.remove("active"));
            el.classList.add("active");
        };
    }

    function kv(k, v) {
        return `<div class="kv"><div class="k">${esc(k)}</div><div class="v">${esc(v)}</div></div>`;
    }

    function renderInfo(it) {
        const id = it?.id ?? it?.Id ?? "—";
        const name = it?.name ?? it?.Name ?? "—";
        const sku = it?.sku ?? it?.Sku ?? "—";
        const brand = it?.brand ?? it?.Brand ?? "—";
        const price = it?.price ?? it?.Price ?? 0;
        const promo = it?.pricePromo ?? it?.PricePromo ?? null;

        const stock = (it?.variants?.length || it?.Variants?.length)
            ? (it?.variants || it?.Variants || []).reduce((sum, v) => sum + (Number(v?.stock ?? v?.Stock ?? 0) || 0), 0)
            : (it?.stock ?? it?.Stock ?? 0);

        const catName = it?.category?.name ?? it?.Category?.Name ?? it?.categoryName ?? it?.CategoryName ?? "—";
        const vendorName = it?.vendorName ?? it?.VendorName ?? it?.Vendor?.Name ?? it?.vendor?.name ?? "—";
        const vendorId = it?.vendorId ?? it?.VendorId ?? "—";
        const createdAt = it?.createdAt ?? it?.CreatedAt ?? it?.submittedAt ?? it?.SubmittedAt;

        infoRow.innerHTML = [
            kv("ID", String(id)),
            kv("Nom", String(name)),
            kv("Prix", money(price)),
            kv("Promo", (promo && Number(promo) > 0) ? money(promo) : "—"),
            kv("Stock", String(stock)),
            kv("SKU", String(sku)),
            kv("Marque", String(brand)),
            kv("Catégorie", String(catName)),
            kv("Boutique", String(vendorName)),
            kv("VendorId", String(vendorId)),
            kv("Soumis le", fmtDate(createdAt))
        ].join("");
    }

    function renderAttributes(it) {
        const attrs = it?.attributes || it?.Attributes || [];
        if (!Array.isArray(attrs) || !attrs.length) {
            attrsBox.innerHTML = `<div class="muted">— Aucun attribut</div>`;
            return;
        }

        // format attendu côté public: [{code,name,values:[...]}, ...]
        attrsBox.innerHTML = `
      <div style="display:grid;gap:8px">
        ${attrs.map(a => {
            const name = a?.name ?? a?.Name ?? a?.code ?? a?.Code ?? "Attribut";
            const values = a?.values ?? a?.Values ?? [];
            const txt = Array.isArray(values) ? values.join(", ") : String(values ?? "");
            return `<div><b>${esc(name)} :</b> <span class="muted">${esc(txt || "—")}</span></div>`;
        }).join("")}
      </div>
    `;
    }

    function renderVariants(it) {
        const vars = it?.variants || it?.Variants || [];
        if (!Array.isArray(vars) || !vars.length) {
            varsHint.textContent = "Aucune variante (stock simple).";
            varsBody.innerHTML = `<tr><td colspan="3" class="muted">—</td></tr>`;
            return;
        }

        // Labels (si dispo)
        // On garde générique mais tu peux plus tard mapper via attributs variant
        th1.textContent = "Option 1";
        th2.textContent = "Option 2";

        varsHint.textContent = `Variantes: ${vars.length}`;

        varsBody.innerHTML = vars.map(v => {
            const k1 = v?.key1 ?? v?.Key1 ?? v?.size ?? v?.Size ?? v?.opt1 ?? "—";
            const k2 = v?.key2 ?? v?.Key2 ?? v?.color ?? v?.Color ?? v?.opt2 ?? "—";
            const st = Number(v?.stock ?? v?.Stock ?? 0) || 0;
            return `
        <tr>
          <td>${esc(String(k1 || "—"))}</td>
          <td>${esc(String(k2 || "—"))}</td>
          <td><b>${st}</b></td>
        </tr>
      `;
        }).join("");
    }

    function setButtonsEnabled(enabled) {
        btnPublish.disabled = !enabled;
        btnReject.disabled = !enabled;
    }

    async function apiGetAdminProduct(id) {
        // IMPORTANT: ton app.js doit gérer Authorization admin dans window.fetchJson
        return await window.fetchJson(`/api/admin/products/${id}?ts=${Date.now()}`, { cache: "no-store" });
    }

    async function apiAction(id, act, reason) {
        if (act === "reject") {
            return await window.fetchJson(`/api/admin/products/${id}/reject`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ reason: (reason || "").trim() })
            });
        }

        return await window.fetchJson(`/api/admin/products/${id}/${act}`, { method: "POST" });
    }

    function backUrl(vendorId, tab) {
        const v = Number(vendorId || 0) || 0;
        const t = String(tab || "Pending");
        if (v) return `/admin-vendor-products.html?vendorId=${encodeURIComponent(v)}&tab=${encodeURIComponent(t)}`;
        return `/admin-vendor-products.html`;
    }

    async function load() {
        const { id } = getQuery();
        if (!id) {
            setMsg("❌ id manquant dans l’URL.");
            return;
        }

        setMsg("Chargement…");
        setButtonsEnabled(false);

        try {
            const data = await apiGetAdminProduct(id);
            const it = data?.item || data?.product || data;

            const name = it?.name ?? it?.Name ?? `#${id}`;
            pTitle.textContent = `Produit — ${name}`;
            pSub.textContent = `ID ${id}`;

            const status = it?.publishedStatus ?? it?.PublishedStatus ?? it?.status ?? it?.Status ?? "Pending";
            const isDeleted = (it?.isDeleted ?? it?.IsDeleted) === true;

            setBadge(status, isDeleted);

            renderGallery(pickImages(it));
            renderInfo(it);
            renderAttributes(it);
            renderVariants(it);

            rightInfo.textContent =
                isDeleted ? "Produit supprimé (Deleted) — aucune action."
                    : `Statut actuel: ${String(status)} • Actif: ${(it?.isActive ?? it?.IsActive) ? "Oui" : "Non"}`;

            const rr = it?.rejectReason ?? it?.RejectReason ?? "";
            const rd = it?.rejectedAt ?? it?.RejectedAt ?? null;

            const box = qs("#rejectInfo");
            if (box) {
                if (rr) box.innerHTML = `<b>Motif :</b> ${esc(rr)}${rd ? ` <span class="muted">(${fmtDate(rd)})</span>` : ""}`;
                else box.textContent = "";
            }

            setButtonsEnabled(!isDeleted);
            setMsg("");
        } catch (e) {
            setMsg("❌ Erreur: " + (e?.message || String(e)));
            setButtonsEnabled(false);
        }
    }

    async function onPublish() {
        const { id } = getQuery();
        if (!id) return;

        if (!confirm("Publier ce produit ? Il deviendra visible côté client (si vendeur actif).")) return;

        try {
            setButtonsEnabled(false);
            setMsg("Publication…");
            await apiAction(id, "publish");
            await load();
            setMsg("✅ Publié.");
            setTimeout(() => setMsg(""), 900);
        } catch (e) {
            setMsg("❌ " + (e?.message || String(e)));
            setButtonsEnabled(true);
        }
    }

    async function onReject() {
        const { id } = getQuery();
        if (!id) return;

        const reason = (rejectReason.value || "").trim();
        const ask = reason ? `Rejeter ce produit ?\nMotif: ${reason}` : "Rejeter ce produit ?";

        if (!confirm(ask)) return;

        try {
            setButtonsEnabled(false);
            setMsg("Rejet…");
            await apiAction(id, "reject", reason);
            await load();
            setMsg("✅ Rejeté.");
            setTimeout(() => setMsg(""), 900);
        } catch (e) {
            setMsg("❌ " + (e?.message || String(e)));
            setButtonsEnabled(true);
        }
    }

    function init() {
        const { vendorId, tab } = getQuery();

        btnBack?.addEventListener("click", () => {
            location.href = backUrl(vendorId, tab);
        });

        btnPublish?.addEventListener("click", onPublish);
        btnReject?.addEventListener("click", onReject);

        load();
    }

    document.addEventListener("DOMContentLoaded", init);
})();