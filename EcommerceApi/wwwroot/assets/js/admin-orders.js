// wwwroot/assets/js/admin-orders.js
(() => {
    "use strict";

    let _allOrders = [];
    let _filteredOrders = [];
    let _autoTimer = null;
    let _isRefreshing = false;

    const state = {
        page: 1,
        pageSize: 20,
        status: ""
    };

    const qs = window.qs || ((s, root = document) => root.querySelector(s));

    function esc(s) {
        return String(s ?? "").replace(/[&<>"']/g, c => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
        }[c]));
    }

    function sortOrders(list) {
        const getTime = (o) => {
            const v = o?.createdAt || o?.date || "";
            const t = Date.parse(v);
            return Number.isFinite(t) ? t : 0;
        };

        return (list || []).slice().sort((a, b) => {
            const ida = Number(a?.id) || 0;
            const idb = Number(b?.id) || 0;
            if (idb !== ida) return idb - ida;
            return getTime(b) - getTime(a);
        });
    }

    function setRowMessage(msg) {
        const body = qs("#ordersBody");
        if (!body) return;
        body.innerHTML = `<tr><td colspan="10" class="muted">${esc(msg)}</td></tr>`;
    }

    function formatDateAdmin(iso) {
        const d = new Date(iso);
        if (isNaN(d)) return esc(iso);
        const p = n => String(n).padStart(2, "0");
        return `${p(d.getDate())}/${p(d.getMonth() + 1)}/${d.getFullYear()}<br>${p(d.getHours())}:${p(d.getMinutes())}`;
    }

    function statusBadgeClass(status) {
        const s = String(status || "").toLowerCase();
        if (s.includes("attente")) return "badge badge-waiting";
        if (s.includes("préparation") || s.includes("preparation")) return "badge badge-preparing";
        if (s.includes("livraison")) return "badge badge-shipping";
        if (s.includes("livrée") || s.includes("livree")) return "badge badge-delivered";
        if (s.includes("annul")) return "badge badge-cancelled";
        return "badge";
    }

    function vendorBadgeHtml(vs) {
        const s = String(vs || "").toLowerCase();
        if (s === "pending") return `<span class="vbadge pending">Pending</span>`;
        if (s === "shipped") return `<span class="vbadge shipped">Shipped</span>`;
        if (s === "delivered") return `<span class="vbadge delivered">Delivered</span>`;
        if (s === "cancelled") return `<span class="vbadge cancelled">Cancelled</span>`;
        return `<span class="vbadge">${esc(vs || "—")}</span>`;
    }

    function renderPager(totalCount) {
        const info = qs("#ordersPagerInfo");
        const summary = qs("#ordersSummary");
        const btnPrev = qs("#btnPrevPage");
        const btnNext = qs("#btnNextPage");

        const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize));
        if (state.page > totalPages) state.page = totalPages;

        const start = totalCount === 0 ? 0 : ((state.page - 1) * state.pageSize) + 1;
        const end = Math.min(state.page * state.pageSize, totalCount);

        if (info) info.textContent = `Page ${state.page} / ${totalPages}`;
        if (summary) summary.textContent = `${start}–${end} sur ${totalCount} commande(s)`;

        if (btnPrev) btnPrev.disabled = state.page <= 1;
        if (btnNext) btnNext.disabled = state.page >= totalPages;
    }

    function render(list) {
        const body = qs("#ordersBody");
        if (!body) return;

        if (!list || !list.length) {
            setRowMessage("Aucune commande.");
            renderPager(0);
            return;
        }

        const start = (state.page - 1) * state.pageSize;
        const pageItems = list.slice(start, start + state.pageSize);

        body.innerHTML = pageItems.map(o => {
            const statusText = o.status ?? o.statut ?? "";
            const vendorText = o.vendorStatus || "—";
            const shop =
    o.vendorShopName ||
    o.vendorName ||
    o.shopName ||
    (o.vendorId ? "Ranita Shop" : "—");

            return `
<tr>
  <td>${esc(o.id)}</td>
  <td><div class="client-name">${esc(o.fullName || "")}</div></td>

  <td>
    <div class="shop-cell">
      <div class="shop-name">${esc(shop)}</div>
      <div class="shop-action">
        ${o.vendorId
                    ? `<a class="btn-shop" href="/shop.html?vendorId=${encodeURIComponent(o.vendorId)}"><span class="shop-icon">🏪</span><span>Voir boutique</span></a>`
                    : `<span class="muted">—</span>`}
      </div>
    </div>
  </td>

  <td>${esc(o.phone || "")}</td>
  <td>${esc(o.city || "")}</td>

  <td>
    <div class="order-total">
      ${typeof window.fmtFCFA === "function" ? window.fmtFCFA(o.total || 0) : esc(o.total || 0)}
    </div>
  </td>

  <td><span class="${statusBadgeClass(statusText)}">${esc(statusText)}</span></td>
  <td>${vendorBadgeHtml(vendorText)}</td>

  <td>
    <div class="order-date">
      ${formatDateAdmin(o.createdAt || "")}
    </div>
  </td>

  <td>
    <div class="order-action-cell">
      <a class="btn-view" href="/admin-order.html?id=${encodeURIComponent(o.id)}"><span class="view-icon">👁️</span><span>Voir</span></a>
    </div>
  </td>
</tr>`;
        }).join("");

        renderPager(list.length);
    }
    function applyFilters() {
        const input = qs("#txtSearch");
        const q = (input?.value || "").trim().toLowerCase();
        const status = state.status;

        _filteredOrders = _allOrders.filter(o => {
            const txt = `${o.id} ${o.fullName || o.clientName || ""} ${o.vendorShopName || ""} ${o.phone || ""} ${o.city || ""} ${o.status || o.statut || ""}`.toLowerCase();

            const matchesSearch = !q || txt.includes(q);
            const matchesStatus = !status || String(o.status || o.statut || "") === status;

            return matchesSearch && matchesStatus;
        });

        render(_filteredOrders);
    }

    async function fetchOrders() {
        const url = "/api/admin/orders?ts=" + Date.now();

        if (typeof window.fetchJson === "function") {
            const data = await window.fetchJson(url, { cache: "no-store" });
            return data.items || data.orders || (Array.isArray(data) ? data : []);
        }

        const token = localStorage.getItem("ranita_admin_token") || "";
        const res = await fetch(url, {
            headers: {
                "Accept": "application/json",
                ...(token ? { "Authorization": "Bearer " + token } : {})
            },
            cache: "no-store"
        });

        if (!res.ok) throw new Error("API " + res.status);
        const data = await res.json();
        return data.items || data.orders || (Array.isArray(data) ? data : []);
    }

    async function loadOrders(firstLoad = false) {
        try {
            if (firstLoad) setRowMessage("Chargement...");

            const listRaw = await fetchOrders();
            _allOrders = sortOrders(listRaw);
            applyFilters();

            if (typeof window.refreshAdminNotifCount === "function") {
                window.refreshAdminNotifCount();
            }
        } catch (e) {
            setRowMessage("Erreur: " + (e?.message || e));
        }
    }

    async function autoRefreshOrders() {
        if (_isRefreshing) return;
        _isRefreshing = true;

        try {
            const listRaw = await fetchOrders();
            _allOrders = sortOrders(listRaw);
            applyFilters();

            if (typeof window.refreshAdminNotifCount === "function") {
                window.refreshAdminNotifCount();
            }
        } finally {
            _isRefreshing = false;
        }
    }

    function initAdminOrdersPage() {
        const txtSearch = qs("#txtSearch");
        const selStatus = qs("#selStatus");
        const selPageSize = qs("#selPageSize");
        const btnReload = qs("#btnReload");
        const btnPrev = qs("#btnPrevPage");
        const btnNext = qs("#btnNextPage");

        txtSearch?.addEventListener("input", () => {
            state.page = 1;
            applyFilters();
        });

        selStatus?.addEventListener("change", () => {
            state.status = selStatus.value || "";
            state.page = 1;
            applyFilters();
        });

        selPageSize?.addEventListener("change", () => {
            state.pageSize = Number(selPageSize.value || 20);
            state.page = 1;
            render(_filteredOrders);
        });

        btnReload?.addEventListener("click", () => {
            loadOrders(true);
        });

        btnPrev?.addEventListener("click", () => {
            if (state.page > 1) {
                state.page--;
                render(_filteredOrders);
            }
        });

        btnNext?.addEventListener("click", () => {
            const totalPages = Math.max(1, Math.ceil(_filteredOrders.length / state.pageSize));
            if (state.page < totalPages) {
                state.page++;
                render(_filteredOrders);
            }
        });

        loadOrders(true);

        if (_autoTimer) clearInterval(_autoTimer);
        _autoTimer = setInterval(autoRefreshOrders, 7000);
    }

    window.initAdminOrdersPage = initAdminOrdersPage;
})();