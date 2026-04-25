// /assets/js/vendor-dashboard.js
(() => {
    "use strict";

    const qs = (s, r = document) => r.querySelector(s);
    const qsa = (s, r = document) => Array.from(r.querySelectorAll(s));

    const money = (n) => (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";
    const norm = (s) => String(s || "").trim().toLowerCase();

    function escHtml(s) {
        return String(s ?? "").replace(/[&<>"']/g, c => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;"
        }[c]));
    }

    // ✅ essaie plusieurs noms possibles (selon ton API/DTO)
    function pickRejectReason(p) {
        return (
            p?.rejectReason ??
            p?.rejectionReason ??
            p?.rejectedReason ??
            p?.rejectMessage ??
            p?.rejectionMessage ??
            p?.adminNote ??
            p?.note ??
            p?.motifRejet ??
            p?.MotifRejet ??
            p?.RejectReason ??
            p?.RejectionReason ??
            ""
        );
    }

    let ALL = [];
    let current = "All";

    function badgeHtml(status) {
        const st = norm(status);
        if (st === "pending") return `<span class="badge pending">Pending</span>`;
        if (st === "published") return `<span class="badge published">Publié</span>`;
        if (st === "rejected") return `<span class="badge rejected">Rejet</span>`;
        return `<span class="badge">${String(status || "—")}</span>`;
    }

    function setActiveCard(filter) {
        current = filter;
        qsa(".kpi-card").forEach(c => c.classList.toggle("active", c.dataset.filter === filter));
    }

    function getFilteredItems(filter) {
        if (filter === "All") return ALL.slice();
        return ALL.filter(x => norm(x.publishedStatus || x.status) === norm(filter));
    }

    function renderList(filter) {
        const body = qs("#productsBody");
        const title = qs("#panelTitle");
        const sub = qs("#panelSubtitle");
        const rejectBox = qs("#rejectBox");

        const items = getFilteredItems(filter);

        if (title) title.textContent = `Produits : ${filter === "All" ? "Tous" : filter}`;
        if (sub) sub.textContent = items.length ? `${items.length} produit(s)` : "Aucun produit.";

        if (rejectBox) {
            rejectBox.innerHTML = (norm(filter) === "rejected" && items.length > 1)
                ? `<span class="muted">⚠️ ${items.length} produits rejetés : motif affiché sous chaque produit.</span>`
                : "";
        }

        if (!body) return;

        if (!items.length) {
            body.innerHTML = `<tr><td colspan="6" class="muted" style="padding:12px 8px">Aucun produit.</td></tr>`;
            return;
        }

        body.innerHTML = items.map(p => {
            const img = p.mainImageUrl
                ? `<img src="${p.mainImageUrl}" style="width:44px;height:44px;object-fit:cover;border-radius:10px;border:1px solid rgba(255,255,255,.12)">`
                : `<span class="muted">—</span>`;

            const st = p.publishedStatus || p.status;
            const stock = Number(p?.stock ?? p?.Stock ?? 0);

            const reason = pickRejectReason(p);
            const reasonHtml = (norm(st) === "rejected" && reason)
                ? `<div class="muted" style="margin-top:6px;font-size:12px">
       <b style="color:#fca5a5">Motif :</b> ${escHtml(reason)}
     </div>`
                : "";

            return `
        <tr data-id="${p.id}" style="border-bottom:1px solid rgba(255,255,255,.06)">
          <td style="padding:10px 8px">${img}</td>

          <td style="padding:10px 8px">
  <b>${escHtml(p.name || "")}</b>
  ${reasonHtml}
</td>

          <td style="padding:10px 8px">${badgeHtml(st)}</td>
          <td style="padding:10px 8px">${money(p.price)}</td>
          <td style="padding:10px 8px"><b>${stock}</b></td>
          <td style="padding:10px 8px;white-space:nowrap">
          <button class="btn" data-act="open" data-id="${p.id}">Ouvrir</button>
          </td>
        </tr>
      `;
        }).join("");
    }

    async function loadMe() {
        // statut boutique (si /api/vendor/me existe)
        const nameEl = qs("#vendorName");
        const stEl = qs("#vendorStatus");

        let cached = null;
        try { cached = JSON.parse(localStorage.getItem("ranita_vendor_user") || "null"); } catch { }

        if (nameEl && cached?.vendorName) nameEl.textContent = cached.vendorName;

        const me = await window.fetchVendorJson("/api/vendor/me").catch(() => null);
        if (me?.vendor) {
            if (nameEl) nameEl.textContent = me.vendor.name || "Boutique";
            if (stEl) {
                const s = Number(me.vendor.status);
                stEl.textContent = s === 1 ? "✅ Boutique active"
                    : (s === 0 ? "⏳ En attente de validation" : "⛔ Boutique bloquée");
            }
        } else {
            if (stEl) stEl.textContent = cached?.vendorStatus === 1 ? "✅ Boutique active" : "⏳ En attente de validation";
        }
    }

    function updateKpis(items) {
        const total = items.length;
        const pending = items.filter(x => norm(x.publishedStatus || x.status) === "pending").length;
        const published = items.filter(x => norm(x.publishedStatus || x.status) === "published").length;
        const rejected = items.filter(x => norm(x.publishedStatus || x.status) === "rejected").length;

        qs("#kpiTotal") && (qs("#kpiTotal").textContent = String(total));
        qs("#kpiPending") && (qs("#kpiPending").textContent = String(pending));
        qs("#kpiPublished") && (qs("#kpiPublished").textContent = String(published));
        qs("#kpiRejected") && (qs("#kpiRejected").textContent = String(rejected));

        const last = qs("#lastSync");
        if (last) last.textContent = "Dernière synchro : " + new Date().toLocaleString("fr-FR");
    }

    async function loadProducts() {

        const data = await window.fetchVendorJson(`/api/vendor/products?ts=${Date.now()}`, {
            method: "GET",
            cache: "no-store"
        }).catch(() => null);

        const items = data?.items || data?.products || [];
        ALL = Array.isArray(items) ? items : [];
        updateKpis(ALL);
        renderList(current);
    }
    async function loadSummary() {
        const data = await window.fetchVendorJson("/api/vendor/dashboard/summary", {
            method: "GET"
        }).catch(() => null);

        if (!data) return;

        const money = (n) =>
            (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";

        const set = (id, val) => {
            const el = document.getElementById(id);
            if (el) el.textContent = val;
        };

        set("kpiTodayRevenue", money(data.todayRevenue));
        set("kpiWeekRevenue", money(data.weekRevenue));
        set("kpiMonthRevenue", money(data.monthRevenue));
        set("kpiTotalRevenue", money(data.totalRevenue));
        set("kpiOrdersPending", String(data.pendingOrders ?? 0));
    }
    function wireUi() {


        // KPI click => filter
        qsa(".kpi-card").forEach(card => {
            card.addEventListener("click", () => {
                const filter = card.dataset.filter || "All";

                if (filter === "OrdersPending") {
                    location.href = "/vendor-orders.html?status=Pending";
                    return;
                }

                setActiveCard(filter);
                renderList(filter);
                qs("#productsPanel")?.scrollIntoView({ behavior: "smooth", block: "start" });


            });
        });

        // row action
        qs("#productsBody")?.addEventListener("click", (e) => {
            const btn = e.target.closest("button[data-act]");
            if (!btn) return;
            const id = btn.dataset.id;
            if (!id) return;

            if (btn.dataset.act === "open") {
                location.href = `/vendor-product-create-pro.html?id=${encodeURIComponent(id)}`;
            }
        });

   
    }

    async function init() {
        if (!window.requireVendorAuth?.()) return;

        wireUi();
        setActiveCard("All");
        await loadMe();
        await loadProducts();

        loadSummary();
    }


    document.addEventListener("DOMContentLoaded", init);

})();