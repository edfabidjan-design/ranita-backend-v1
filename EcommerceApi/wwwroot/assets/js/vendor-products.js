// wwwroot/assets/js/vendor-products.js
(() => {
    "use strict";

    const qs = (s, r = document) => r.querySelector(s);
    let ALL = [];

    const esc = (s) => String(s ?? "").replace(/[&<>"']/g, c => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;"
    }[c]));

    const money = (n) => (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";
    const norm = (s) => String(s || "").toLowerCase().trim();

    function setMsg(txt = "") {
        const el = qs("#msg");
        if (el) el.textContent = txt;
    }

    function setCount(n) {
        const el = qs("#count");
        if (el) el.textContent = `${n} produit(s)`;
    }

    // ✅ essaye de trouver une image principale dans plusieurs formats possibles
    function pickThumb(p) {
        const placeholder = "/assets/placeholder.png";
        const u =
            p?.mainImageUrl ||
            p?.imageUrl ||
            p?.thumbnailUrl ||
            p?.thumbUrl ||
            p?.coverUrl ||
            p?.images?.[0]?.url ||
            p?.images?.[0]?.Url ||
            p?.images?.[0]?.path ||
            p?.images?.[0]?.Path ||
            p?.Images?.[0]?.Url ||
            null;

        if (!u) return placeholder;
        // si l'API renvoie un chemin relatif sans /
        if (typeof u === "string" && !u.startsWith("http") && !u.startsWith("/")) return "/" + u;
        return u;
    }

    function badgeHtml(stRaw) {
        const st = norm(stRaw);

        if (st === "published")
            return `<span class="vp-badge vp-ok">Publié</span>`;

        if (st === "rejected")
            return `<span class="vp-badge vp-bad">Rejeté</span>`;

        // ✅ plus de Draft
        return `<span class="vp-badge vp-warn">Pending</span>`;
    }

    function thumbUrl(p) {
        const placeholder = "/assets/placeholder.png";

        const u =
            p?.mainImageUrl || p?.MainImageUrl ||
            p?.imageUrl || p?.ImageUrl ||
            p?.thumbnailUrl || p?.thumbUrl || p?.coverUrl ||
            p?.images?.[0]?.url || p?.images?.[0]?.Url ||
            p?.Images?.[0]?.Url ||
            null;

        if (!u) return placeholder;

        if (typeof u === "string" && !u.startsWith("http") && !u.startsWith("/"))
            return "/" + u;

        return u;
    }

    async function doDelete(id) {
        if (!id) return;
        if (!confirm("Supprimer ce produit ?")) return;

        setMsg("Suppression...");
        const res = await window.fetchVendorJson(`/api/vendor/products/${id}`, { method: "DELETE" })
            .catch(e => ({ ok: false, message: e?.message || "Erreur" }));

        if (!res?.ok) {
            setMsg("Erreur suppression : " + (res?.message || "—"));
            return;
        }
        setMsg("Supprimé ✅");
        await loadList();
    }

    async function doDuplicate(p) {
        const id = p.id ?? p.productId;
        if (!id) return;

        if (!confirm("Dupliquer ce produit (images + variantes + attributs) ?")) return;

        setMsg("Duplication complète...");
        const res = await window.fetchVendorJson(`/api/vendor/products/${id}/duplicate`, {
            method: "POST"
        }).catch(e => ({ ok: false, message: e?.message || "Erreur" }));

        if (!res?.ok) {
            setMsg("Erreur duplication : " + (res?.message || "—"));
            return;
        }

        setMsg("Dupliqué ✅");
        await loadList();
    }

    function renderRows(items) {
        const body = qs("#body");
        if (!body) return;

        if (!items.length) {
            body.innerHTML = `<tr><td colspan="5" class="muted" style="padding:12px;">Aucun produit.</td></tr>`;
            return;
        }

        body.innerHTML = items.map(p => {
            const isDeleted = !!p.isDeleted;

            const id = esc(p.id ?? p.productId ?? "");
            const name = esc(p.name || p.title || "—");
            const sku = esc(p.sku || p.Sku || "—");
            const price = money(p.price);
            const stock = Number(p.stock ?? 0);
            const img = esc(thumbUrl(p));

            const badge = isDeleted
                ? `<span class="vp-badge vp-bad">Supprimé</span>`
                : badgeHtml(p.publishedStatus || p.status || "pending");

            return `
      <tr class="vp-rowPremium" data-id="${id}">
        <td style="padding:12px;border-bottom:1px solid rgba(148,163,184,.12)">
          <div style="display:flex;gap:12px;align-items:center">
            <img class="vp-thumb" src="${img}" alt="" loading="lazy" />
            <div style="min-width:0">
              <div class="vp-name">${name}</div>
              <div class="muted">SKU: <b>${sku}</b></div>
              <div style="margin-top:6px">${badge}</div>
            </div>
          </div>
        </td>

        <td style="padding:12px;border-bottom:1px solid rgba(148,163,184,.12)">${price}</td>
        <td style="padding:12px;border-bottom:1px solid rgba(148,163,184,.12)">${stock}</td>
        <td style="padding:12px;border-bottom:1px solid rgba(148,163,184,.12)">
          ${badge}
        </td>

        <td style="padding:12px;border-bottom:1px solid rgba(148,163,184,.12);white-space:nowrap">
          <div class="vp-actions">
            ${isDeleted
                    ? `<button class="btn" data-restore="${id}">Restaurer</button>`
                    : `
                        <button class="btn" data-edit="${id}">Modifier</button>
                        <button class="btn" data-dup="${id}">Dupliquer</button>
                        <button class="btn danger" data-del="${id}">Supprimer</button>
                      `
                }
          </div>
        </td>
      </tr>
    `;
        }).join("");

        body.querySelectorAll("[data-edit]").forEach(btn => {
            btn.addEventListener("click", () => {
                const id = btn.getAttribute("data-edit");
                location.href = `/vendor-product-create-pro.html?id=${encodeURIComponent(id)}`;
            });
        });

        body.querySelectorAll("[data-del]").forEach(btn => {
            btn.addEventListener("click", () => doDelete(btn.getAttribute("data-del")));
        });

        body.querySelectorAll("[data-restore]").forEach(btn => {
            btn.addEventListener("click", () => doRestore(btn.getAttribute("data-restore")));
        });

        body.querySelectorAll("[data-dup]").forEach(btn => {
            btn.addEventListener("click", () => {
                const id = btn.getAttribute("data-dup");
                const prod = ALL.find(x => String(x.id ?? x.productId) === String(id));
                if (prod) doDuplicate(prod);
            });
        });
    }


    async function doRestore(id) {
        if (!id) return;
        if (!confirm("Restaurer ce produit ?")) return;

        setMsg("Restauration...");
        const res = await window.fetchVendorJson(`/api/vendor/products/${id}/restore`, {
            method: "POST"
        }).catch(e => ({ ok: false, message: e?.message || "Erreur" }));

        if (!res?.ok) {
            setMsg("Erreur restauration : " + (res?.message || "—"));
            return;
        }

        setMsg("Produit restauré ✅");
        await loadList();
    }

    function applyFilters() {
        const q = norm(qs("#q")?.value);
        let st = norm(qs("#status")?.value);

        // ✅ on interdit Draft côté vendeur
        if (st === "draft") st = "";

        let items = [...ALL];

        if (q) {
            items = items.filter(p => {
                const name = norm(p.name || p.title);
                const sku = norm(p.sku || p.Sku);
                return name.includes(q) || sku.includes(q);
            });
        }

        if (st) {
            items = items.filter(p => norm(p.publishedStatus || p.status) === st);
        }

        setCount(items.length);
        renderRows(items);
    }

    async function loadList() {
        if (!window.requireVendorAuth()) return;

        setMsg("Chargement…");

        const cached = window.getVendorCachedUser?.() || null;
        const hint = qs("#vendorHint");
        if (hint) hint.textContent = cached?.vendorName || "Boutique";

        const data = await window.fetchVendorJson("/api/vendor/products?includeDeleted=true", { method: "GET" }).catch(e => {
            setMsg("Erreur chargement : " + (e?.message || "—"));
            return null;
        });

        const items = data?.items || data?.products || data || [];
        ALL = Array.isArray(items) ? items : [];
        setMsg("");
        applyFilters();
    }

    function bind() {



        qs("#q")?.addEventListener("input", applyFilters);
        qs("#status")?.addEventListener("change", applyFilters);
    }

    document.addEventListener("DOMContentLoaded", () => {
        bind();
        loadList();
    });

})();