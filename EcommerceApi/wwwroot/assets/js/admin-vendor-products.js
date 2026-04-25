// wwwroot/assets/js/admin-vendor-products.js
(() => {
    "use strict";

    const qs = (s, r = document) => r.querySelector(s);
    const qsa = (s, r = document) => Array.from(r.querySelectorAll(s));

    const body = qs("#itemsBody");
    const msg = qs("#msg");
    const count = qs("#countItems");
    const btnRefresh = qs("#btnRefresh");

    // ✅ Draft supprimé => on garde 4 statuts
    const ALLOWED_TABS = new Set(["Pending", "Published", "Rejected", "Deleted"]);
    let currentStatus = "Pending";

    function esc(s) {
        return String(s ?? "").replace(/[&<>"']/g, c => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;"
        }[c]));
    }

    function money(n) {
        return (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";
    }
    function fmtDate(dt) {
        if (!dt) return "";
        const d = new Date(dt);
        if (isNaN(d.getTime())) return String(dt);
        return d.toLocaleString("fr-FR", {
            year: "numeric", month: "2-digit", day: "2-digit",
            hour: "2-digit", minute: "2-digit"
        });
    }
    function setRowMessage(t) {
        body.innerHTML = `<tr><td colspan="11" class="muted">${esc(t)}</td></tr>`;
    }

    function normalizeStatus(s) {
        const st = String(s || "").trim();
        return ALLOWED_TABS.has(st) ? st : "Pending";
    }

    function setActiveTab(status) {
        currentStatus = normalizeStatus(status);
        qsa('button[data-tab]').forEach(b => {
            const isOn = b.dataset.tab === currentStatus;
            b.classList.toggle("active", isOn);
        });
    }

    function statusBadge(it) {
        if (it.isDeleted) return `<span class="badge" style="background:#7f1d1d;color:#fff;">Deleted</span>`;
        const st = String(it.status || "").toLowerCase();
        if (st === "pending") return `<span class="badge" style="background:#92400e;color:#fff;">Pending</span>`;
        if (st === "published") return `<span class="badge" style="background:#065f46;color:#fff;">Published</span>`;
        if (st === "rejected") return `<span class="badge" style="background:#7f1d1d;color:#fff;">Rejected</span>`;
        // ✅ Draft supprimé
        return `<span class="badge">${esc(it.status || "")}</span>`;
    }

    function actionButtons(it) {
        // Deleted : pas d’action ici
        if (it.isDeleted) return `<span class="muted">—</span>`;

        // ✅ Draft supprimé : seulement Publier / Rejeter
        return `
      <button class="btn" data-act="publish" data-id="${it.id}">Publier</button>
      <button class="btn danger" data-act="reject" data-id="${it.id}">Rejeter</button>
    `;
    }

    function render(items) {
        items = items || [];
        count.textContent = String(items.length);

        if (!items.length) {
            setRowMessage("Aucun élément.");
            return;
        }

        body.innerHTML = items.map(it => {
            const img = it.mainImageUrl
                ? `<img src="${esc(it.mainImageUrl)}" style="width:44px;height:44px;object-fit:cover;border-radius:10px;border:1px solid rgba(255,255,255,.12)" />`
                : `<span class="muted">—</span>`;

            return `
  <tr class="prod-row" data-id="${it.id}" data-vendorid="${it.vendorId ?? ""}">
          <td>${it.id}</td>
          <td>${img}</td>
          <td><b>${esc(it.name)}</b></td>
          <td>${statusBadge(it)}</td>
          <td>${esc(it.categoryName || "")}</td>
          <td>${money(it.price)}</td>
          <td>${it.stock ?? 0}</td>
<td><b>${esc(it.vendorName || "—")}</b></td>
<td>${esc(fmtDate(it.submittedAt) || "—")}</td>
<td>${it.vendorId ?? ""}</td>
<td style="white-space:nowrap;">${actionButtons(it)}</td>
        </tr>
      `;
        }).join("");
    }

    async function load() {
        currentStatus = normalizeStatus(currentStatus);

        msg.textContent = `Chargement ${currentStatus}…`;
        setRowMessage("Chargement…");

        try {
            const u = new URL(location.href);
            const vendorId = Number(u.searchParams.get("vendorId") || 0);

            if (!vendorId) throw new Error("vendorId manquant dans l'URL");

            const url = `/api/admin/products/by-vendor?vendorId=${vendorId}&status=${encodeURIComponent(currentStatus)}&ts=${Date.now()}`;
            const data = await window.fetchJson(url);

            render(data?.items || []);
            msg.textContent = "";
        } catch (e) {
            msg.textContent = "Erreur";
            setRowMessage("Erreur: " + (e?.message || e));
        }
    }

    async function doAction(id, act) {
        // ✅ sécurité : draft interdit
        if (act === "draft") throw new Error("Action Draft supprimée.");
        await window.fetchJson(`/api/admin/products/${id}/${act}`, { method: "POST" });
    }

    // Tabs
    qsa('button[data-tab]').forEach(btn => {
        btn.addEventListener("click", async () => {
            // ✅ ignore les tabs non autorisés (si HTML pas encore nettoyé)
            const st = normalizeStatus(btn.dataset.tab);
            setActiveTab(st);
            await load();
        });
    });

    // Actions (Publish/Reject)
    // Actions (Publish/Reject) + Click ligne (detail)
    body.addEventListener("click", async (e) => {
        // 1) Si on clique sur un bouton d'action => on fait l'action
        const btn = e.target.closest("button[data-act]");
        if (btn) {
            e.preventDefault();
            e.stopPropagation();

            const id = btn.dataset.id;
            const act = btn.dataset.act;
            if (!id || !act) return;

            try {
                btn.disabled = true;
                msg.textContent = "Traitement…";
                await doAction(id, act);
                await load();
                msg.textContent = "OK";
                setTimeout(() => (msg.textContent = ""), 800);
            } catch (err) {
                msg.textContent = "Erreur: " + (err?.message || err);
            } finally {
                btn.disabled = false;
            }
            return; // ✅ important
        }

        // 2) Sinon, si on clique sur une ligne => ouvrir la page détail
        const row = e.target.closest("tr.prod-row");
        if (!row) return;

        const id = Number(row.dataset.id || 0);
        if (!id) return;

        const u = new URL(location.href);
        const vendorId = Number(u.searchParams.get("vendorId") || 0) || 0;

        // on passe tab pour revenir sur le même onglet
        location.href =
            `/admin-product-detail.html?id=${encodeURIComponent(id)}&vendorId=${encodeURIComponent(vendorId)}&tab=${encodeURIComponent(currentStatus)}`;
    });

    btnRefresh?.addEventListener("click", load);

    // Init
    setActiveTab("Published");
    window.initAdminVendorProductsPage = load;
    load();
})();