// wwwroot/assets/js/admin-customers.js
(() => {
    "use strict";

    const qs = window.qs || ((s, root = document) => root.querySelector(s));
    const API = window.API || location.origin;

    const elBody = qs("#rows");
    const elSearch = qs("#q");
    const elFilter = qs("#flt");
    const elReload = qs("#btnReload");
    const elMsg = qs("#msg");

    const modal = qs("#modal");
    const mClose = qs("#mClose");
    const mBody = qs("#mBody");

    const modalOrder = qs("#modalOrder");
    const oClose = qs("#oClose");
    const oBody = qs("#oBody");


    let _items = [];
    let _timer = null;
    const CAN_VIEW_PAGE = () => window.hasPermission?.("customers.view");
    const CAN_VIEW_DETAILS = () => window.hasPermission?.("customers.details");
    const CAN_CHANGE_STATUS = () => window.hasPermission?.("customers.status");
    const CAN_DELETE = () => window.hasPermission?.("customers.delete");

    function esc(s) {
        return String(s ?? "").replace(/[&<>"']/g, c => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
        }[c]));
    }

    function fmtDate(v) {
        if (!v) return "";
        const d = new Date(v);
        if (Number.isNaN(d.getTime())) return "";
        return d.toLocaleString();
    }

    function badgeActive(isActive) {
        return isActive
            ? `<span class="badge badge-ok">Actif</span>`
            : `<span class="badge badge-cancelled">Désactivé</span>`;
    }

    function setMsg(text = "", ok = true) {
        if (!elMsg) return;
        elMsg.style.display = text ? "block" : "none";
        elMsg.style.borderColor = ok ? "rgba(34,197,94,.35)" : "rgba(239,68,68,.35)";
        elMsg.textContent = text;
    }

    async function fetchJson(url, opts = {}) {
        const token = localStorage.getItem("ranita_admin_token") || "";
        const headers = Object.assign({ "Content-Type": "application/json" }, opts.headers || {});
        if (token) headers.Authorization = `Bearer ${token}`;

        const res = await fetch(url, { ...opts, headers });

        let data = null;
        try { data = await res.json(); } catch { }

        if (!res.ok) {
            const msg = data?.message || data?.error || `HTTP ${res.status}`;
            throw new Error(msg);
        }
        return data;
    }
    function rowHtml(c) {

        console.log("ROW HTML PERMS", {
            details: CAN_VIEW_DETAILS(),
            status: CAN_CHANGE_STATUS(),
            del: CAN_DELETE()
        });

        const ordersCount = Number(c.ordersCount ?? c.OrdersCount ?? 0);
        const canDeleteBusiness = ordersCount === 0;

        return `
<tr data-id="${c.id}">
  <td style="padding:10px">${c.id}</td>
  <td style="padding:10px;font-weight:900">${esc(c.fullName)}</td>
  <td style="padding:10px" class="muted">${esc(c.phone || "")}</td>
  <td style="padding:10px" class="muted">${esc(c.email || "")}</td>
  <td style="padding:10px">${badgeActive(!!c.isActive)}</td>

  <td style="padding:10px;white-space:nowrap">
    <div style="display:flex;gap:8px;justify-content:flex-end;flex-wrap:wrap;align-items:center">
      <span class="pill">${ordersCount}</span>

      ${CAN_VIEW_DETAILS() ? `
        <button class="btn btnGhost" data-act="view">Voir</button>
      ` : ""}

      ${CAN_CHANGE_STATUS() ? `
        <button class="btn btnGhost" data-act="toggle">${c.isActive ? "Désactiver" : "Activer"}</button>
      ` : ""}

      ${CAN_DELETE() ? `
        <button class="btn ${canDeleteBusiness ? "btnDanger" : "btnGhost"}"
                data-act="delete"
                ${canDeleteBusiness ? "" : "disabled"}
                title="${canDeleteBusiness ? "" : "Client avec commandes : suppression interdite"}">
          Supprimer
        </button>
      ` : ""}
    </div>
  </td>
</tr>`;
    
    }

    function openOrderModal() {
        if (!modalOrder) return;
        modalOrder.style.display = "flex";
    }
    function closeOrderModal() {
        if (!modalOrder) return;
        modalOrder.style.display = "none";
    }

    function imgUrl(p) {
        const src =
            p?.imageUrl ||
            p?.ImageUrl ||          // ✅ ton DTO
            p?.image ||
            p?.photo ||
            p?.mainImageUrl ||
            p?.MainImageUrl ||
            p?.images?.[0]?.url ||
            p?.images?.[0]?.Url ||
            p?.images?.[0]?.imageUrl ||
            p?.images?.[0]?.ImageUrl ||
            "";

        if (!src) return "/assets/img/placeholder.jpg";
        if (String(src).startsWith("http")) return src;
        return (src.startsWith("/") ? src : ("/" + src));
    }


    function renderOrderDetail(d) {
        // ton API: { ok:true, item: dto }
        const root = d?.item || d?.order || d?.data || d;

        const order = root;

        const lines =
            d?.lines ||
            d?.items ||
            d?.item?.items ||
            d?.item?.Items ||      // ✅ cas réel (DTO C#)
            root?.items ||
            root?.Items ||         // ✅ cas réel (DTO C#)
            [];

        const header = `
<div style="display:flex;gap:10px;flex-wrap:wrap;align-items:center;justify-content:space-between">
  <div>
    <div style="font-weight:950;font-size:18px">Commande #${order?.id ?? order?.Id ?? ""}</div>
    <div class="muted" style="opacity:.8">📅 ${fmtDate(order?.createdAt || order?.CreatedAt || order?.createdAtUtc || order?.date)}</div>
    <div class="muted" style="opacity:.8">📌 Statut : ${esc(order?.status || order?.Status || "")}</div>
  </div>
  <div style="font-weight:950;font-size:18px">${Number(order?.total ?? order?.Total ?? 0).toLocaleString()} FCFA</div>
</div>`;

        if (!lines.length) {
            return header + `<div class="muted" style="opacity:.8">Aucune ligne.</div>`;
        }

        const grid = `
<div style="display:grid;gap:10px">
  ${lines.map(it => {
            const qty = Number(it?.qty ?? it?.Qty ?? it?.quantity ?? 0);
            const price = Number(it?.price ?? it?.Price ?? it?.unitPrice ?? 0);
            const name = it?.name || it?.Name || it?.productName || it?.title || "Produit";
            const lineTotal = (it?.lineTotal ?? it?.LineTotal) ?? (qty * price);

            const dim = it?.dimensions || it?.Dimensions || "";
            const w = it?.weightKg ?? it?.WeightKg;
            const opts = [
                (it?.size || it?.Size) ? `Taille: ${esc(it?.size || it?.Size)}` : "",
                (it?.color || it?.Color) ? `Couleur: ${esc(it?.color || it?.Color)}` : "",
                dim ? `Dim: ${esc(dim)}` : "",
                (w !== null && w !== undefined) ? `Poids: ${Number(w)} kg` : ""
            ].filter(Boolean).join(" • ");

            return `
    <div style="display:flex;gap:12px;align-items:center;border:1px solid rgba(255,255,255,.08);border-radius:14px;padding:10px;background:rgba(255,255,255,.03)">
      <img src="${imgUrl(it)}" style="width:56px;height:56px;border-radius:12px;object-fit:cover;border:1px solid rgba(255,255,255,.10)" />
      <div style="flex:1">
        <div style="font-weight:900">${esc(name)}</div>
        <div class="muted" style="opacity:.8">
          Qté: ${qty} • PU: ${price.toLocaleString()} FCFA
        </div>
        ${opts ? `<div class="muted" style="opacity:.75;font-size:12px;margin-top:2px">${opts}</div>` : ""}
      </div>
      <div style="font-weight:900">
        ${Number(lineTotal || 0).toLocaleString()} FCFA
      </div>
    </div>`;
        }).join("")}
</div>`;

        const oid = order?.id ?? order?.Id;
        const openPage = oid
            ? `<div style="display:flex;justify-content:flex-end">
         <a class="btn btnGhost" href="/admin-order.html?id=${oid}">Ouvrir la page détail</a>
       </div>`
            : "";

        return header + `<div style="margin-top:8px;font-weight:900">🛒 Articles</div>` + grid + openPage;
    }


    async function viewOrder(orderId) {
        if (!modalOrder || !oBody) {
            console.error("modalOrder/oBody introuvable. Vérifie #modalOrder et #oBody dans le HTML.");
            return;
        }
        openOrderModal();
        oBody.innerHTML = `<div class="muted">Chargement…</div>`;

        // ✅ endpoints possibles (tu gardes celui qui existe chez toi)
        const tries = [
            `${API}/api/admin/orders/${orderId}`,
            `${API}/api/admin/orders/${orderId}/detail`,
            `${API}/api/admin/order/${orderId}`,
        ];

        let data = null;
        let lastErr = null;

        for (const url of tries) {
            try {
                data = await fetchJson(url, { method: "GET" });
                break;
            } catch (e) {
                lastErr = e;
            }
        }

        if (!data) {
            oBody.innerHTML = `<div class="muted" style="color:#fca5a5">❌ ${esc(lastErr?.message || "Impossible de charger le détail.")}</div>`;
            return;
        }

        oBody.innerHTML = renderOrderDetail(data);
    }


    function renderTable(items) {
        if (!elBody) return;

        if (!items.length) {
            elBody.innerHTML = `<tr><td colspan="6" class="muted" style="padding:10px">Aucun client.</td></tr>`;
            return;
        }

        elBody.innerHTML = items.map(rowHtml).join("");
    }

    function applyFilters() {
        const q = (elSearch?.value || "").trim().toLowerCase();
        const flt = (elFilter?.value ?? "");

        let list = _items.slice();

        if (q) {
            list = list.filter(x => {
                const s = `${x.fullName || ""} ${x.phone || ""} ${x.email || ""}`.toLowerCase();
                return s.includes(q);
            });
        }

        if (flt === "1") list = list.filter(x => !!x.isActive);
        if (flt === "0") list = list.filter(x => !x.isActive);

        renderTable(list);
    }

    async function loadCustomers() {
        setMsg("");
        const u = new URL(`${API}/api/admin/customers`);
        const data = await fetchJson(u.toString(), { method: "GET" });
        _items = data?.items || [];
        applyFilters();
    }

    // ========= MODAL FICHE =========
    function openModal() {
        if (!modal) return;
        modal.style.display = "flex";
    }
    function closeModal() {
        if (!modal) return;
        modal.style.display = "none";
    }

    function renderOrders(list) {
        if (!list || !list.length) {
            return `<div class="muted" style="opacity:.8">Aucune commande liée (CustomerId peut être NULL sur anciennes commandes).</div>`;
        }

        return `
<div style="border:1px solid rgba(255,255,255,.08);border-radius:12px;overflow:hidden">
  <table style="width:100%;border-collapse:collapse">
    <thead>
      <tr style="background:rgba(255,255,255,.04)">
        <th style="text-align:left;padding:10px">ID</th>
        <th style="text-align:left;padding:10px">Total</th>
        <th style="text-align:left;padding:10px">Statut</th>
        <th style="text-align:left;padding:10px">Date</th>
      </tr>
    </thead>
    <tbody>
    ${list.map(o => `
  <tr data-oid="${o.id}" class="orderRow" style="cursor:pointer">
    <td style="padding:10px">${o.id}</td>
    <td style="padding:10px">${Number(o.total || 0).toLocaleString()} FCFA</td>
    <td style="padding:10px">${esc(o.status || "")}</td>
    <td style="padding:10px" class="muted">${fmtDate(o.createdAt)}</td>
  </tr>
`).join("")}

    </tbody>
  </table>
</div>`;
    }

    async function viewCustomer(id) {
        if (!modal || !mBody) {
            console.error("modal/mBody introuvable. Vérifie #modal et #mBody dans le HTML.");
            return;
        }
        openModal();
        mBody.innerHTML = `<div class="muted">Chargement…</div>`;

        const data = await fetchJson(`${API}/api/admin/customers/${id}`, { method: "GET" });
        const c = data?.customer;
        const orders = data?.orders || [];

        mBody.innerHTML = `
<div style="display:grid;gap:8px">
  <div style="font-weight:950;font-size:18px">${esc(c?.fullName || "")}</div>
  <div class="muted">📞 ${esc(c?.phone || "")}</div>
  <div class="muted">✉️ ${esc(c?.email || "")}</div>
  <div class="muted">🟢 Statut : ${c?.isActive ? "Actif" : "Désactivé"}</div>
  <div class="muted">🕒 Créé : ${fmtDate(c?.createdAtUtc)}</div>
  <div style="margin-top:8px;font-weight:900">🧾 Commandes</div>
  ${renderOrders(orders)}
</div>`;
    }

    async function toggleActive(id, makeActive) {
        // ✅ le DTO API attend isActive
        await fetchJson(`${API}/api/admin/customers/${id}/active`, {
            method: "PUT",
            body: JSON.stringify({ active: !!makeActive })

        });
    }

    async function deleteCustomer(id) {
        // ✅ par défaut supprime si pas de commandes, sinon l’API renverra un message
        await fetchJson(`${API}/api/admin/customers/${id}`, { method: "DELETE" });
    }

    // ========= EVENTS =========
    function wireEvents() {
        elReload?.addEventListener("click", loadCustomers);

        elFilter?.addEventListener("change", applyFilters);

        elSearch?.addEventListener("input", () => {
            clearTimeout(_timer);
            _timer = setTimeout(applyFilters, 120);
        });

        mClose?.addEventListener("click", closeModal);
        modal?.addEventListener("click", (e) => { if (e.target === modal) closeModal(); });

        oClose?.addEventListener("click", closeOrderModal);
        modalOrder?.addEventListener("click", (e) => { if (e.target === modalOrder) closeOrderModal(); });


        document.addEventListener("click", async (e) => {
            // ✅ 1) CLIC SUR UNE COMMANDE (ligne du tableau)
            const trOrder = e.target.closest("tr[data-oid]");
            if (trOrder) {
                const oid = Number(trOrder.getAttribute("data-oid"));
                if (oid) {
                    try { await viewOrder(oid); } catch (err) { console.error(err); }
                }
                return;
            }

            // ✅ 2) CLIC SUR LES BOUTONS CLIENT (Voir / Activer / Supprimer)
            const btn = e.target.closest("button[data-act]");
            if (!btn) return;

            const tr = btn.closest("tr[data-id]");
            if (!tr) return;

            const id = Number(tr.getAttribute("data-id"));
            const act = btn.getAttribute("data-act");

            try {
                setMsg("");

                if (act === "view") {
                    if (!CAN_VIEW_DETAILS()) {
                        setMsg("❌ Vous n'avez pas la permission de voir la fiche client.", false);
                        return;
                    }

                    await viewCustomer(id);
                    return;
                }

                if (act === "toggle") {
                    if (!CAN_CHANGE_STATUS()) {
                        setMsg("❌ Vous n'avez pas la permission de modifier le statut client.", false);
                        return;
                    }

                    const item = _items.find(x => Number(x.id) === id);
                    const makeActive = !(item?.isActive);
                    if (!confirm(makeActive ? "Réactiver ce client ?" : "Désactiver ce client ?")) return;

                    await toggleActive(id, makeActive);
                    await loadCustomers();
                    setMsg(makeActive ? "✅ Client réactivé." : "✅ Client désactivé.", true);
                    return;
                }

                if (act === "delete") {
                    if (!CAN_DELETE()) {
                        setMsg("❌ Vous n'avez pas la permission de supprimer un client.", false);
                        return;
                    }

                    if (!confirm("Supprimer définitivement ce client ?")) return;

                    await deleteCustomer(id);
                    await loadCustomers();
                    setMsg("✅ Client supprimé.", true);
                    return;
                }
            } catch (err) {
                console.error(err);
                setMsg("❌ " + (err?.message || err), false);
            }
        });





    }

    async function init() {
        wireEvents();
        await loadCustomers();
    }

    document.addEventListener("DOMContentLoaded", () => {
        if (!window.requireAdminAuth?.()) return;
        if (!window.requirePermission?.("customers.view")) return;

        if (
            qs("#customersBody") ||
            qs("#tbodyCustomers") ||
            qs("[data-page='admin-customers']") ||
            location.pathname.includes("customers")
        ) {
            init();
        }
    });

})();
