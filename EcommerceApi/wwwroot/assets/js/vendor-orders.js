// wwwroot/assets/js/vendor-orders.js
(() => {
    "use strict";

    const qs = (s, r = document) => r.querySelector(s);
    const qsa = (s, r = document) => Array.from(r.querySelectorAll(s));
    const norm = (s) => String(s || "").trim().toLowerCase();
    const money = (n) => (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";

    function escHtml(s) {
        return String(s ?? "").replace(/[&<>"']/g, (c) => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;"
        }[c]));
    }

    let ALL = [];
    let current = "All";
    let _statusOrderItemId = null;

    function badgeHtml(status) {
        const st = norm(status || "pending");
        if (st === "pending") return `<span class="badge pending">Pending</span>`;
        if (st === "confirmed") return `<span class="badge confirmed">Confirmée</span>`;
        if (st === "shipped") return `<span class="badge shipped">Expédiée</span>`;
        if (st === "delivered") return `<span class="badge delivered">Livrée</span>`;
        return `<span class="badge">${escHtml(status || "—")}</span>`;
    }

    function setActivePill(filter) {
        current = filter;
        qsa(".pill").forEach((p) => p.classList.toggle("active", p.dataset.filter === filter));
    }

    function passFilter(it, filter) {
        if (filter === "All") return true;
        return norm(it.vendorStatus) === norm(filter);
    }

    function passSearch(it, q) {
        if (!q) return true;
        q = norm(q);
        return (
            norm(it.productName).includes(q) ||
            norm(it.customerName).includes(q) ||
            norm(it.phone).includes(q) ||
            norm(it.city).includes(q) ||
            norm(it.orderId).includes(q)
        );
    }

    async function updateStatus(orderItemId, status) {
        const payload = { status };
        return await VendorNav.fetchJson(`/api/vendor/orders/${orderItemId}/status`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload),
        });
    }

    function render() {
        const body = qs("#ordersBody");
        const hint = qs("#hintBox");
        const q = qs("#txtSearch")?.value?.trim() || "";

        const rows = ALL
            .filter((it) => passFilter(it, current))
            .filter((it) => passSearch(it, q));

        if (hint) {
            hint.innerHTML = rows.length
                ? `<span class="muted">✅ ${rows.length} ligne(s) de commande.</span>`
                : `<span class="muted">Aucune commande.</span>`;
        }

        if (!body) return;

        if (!rows.length) {
            body.innerHTML = `<tr><td colspan="8" class="muted" style="padding:12px 8px">Aucune commande.</td></tr>`;
            return;
        }

        body.innerHTML = rows.map((it) => {
            const amount = it.vendorAmount ?? (Number(it.unitPrice || 0) * Number(it.quantity || 0));
            const dt = it.createdAt ? new Date(it.createdAt) : null;
            const img = it.productImage || "/assets/img/placeholder.jpg";

            // ✅ disable button from shipped (shipped + delivered)
            const status = norm(it.vendorStatus);
            const isLocked = status === "shipped" || status === "delivered";
            const disabledAttr = isLocked ? "disabled" : "";
            const disabledStyle = isLocked ? "opacity:.45;cursor:not-allowed;pointer-events:none" : "";

            return `
<tr data-id="${escHtml(it.id)}" style="border-bottom:1px solid rgba(255,255,255,.06)">
  <td style="padding:10px 8px">
    <b>#${escHtml(it.orderId)}</b>
    <div class="muted" style="font-size:12px">${dt ? dt.toLocaleString("fr-FR") : ""}</div>
  </td>

  <td style="padding:10px 8px">
    <div style="display:flex;gap:10px;align-items:center">
      <img
        src="${escHtml(img)}"
        alt=""
        style="width:56px;height:56px;flex:0 0 56px;border-radius:10px;object-fit:cover;border:1px solid rgba(255,255,255,.08);display:block"
        onerror="this.onerror=null;this.src='/assets/img/placeholder.jpg';"
      />
      <div>
        <b>${escHtml(it.productName || "")}</b>
        <div class="muted" style="font-size:12px">
          Couleur: ${escHtml(it.selectedColor || "—")} | Taille: ${escHtml(it.selectedSize || "—")}
        </div>
      </div>
    </div>
  </td>

  <td style="padding:10px 8px">
    ${escHtml(it.customerName || "-")}
    <div class="muted" style="font-size:12px">${escHtml(it.phone || "")}</div>
  </td>

  <td style="padding:10px 8px">${escHtml(it.city || "-")}</td>
  <td style="padding:10px 8px"><b>${escHtml(it.quantity)}</b></td>
  <td style="padding:10px 8px">${money(amount)}</td>
  <td style="padding:10px 8px">${badgeHtml(it.vendorStatus)}</td>

  <td style="padding:10px 8px;white-space:nowrap">
    <button class="btn" style="${disabledStyle}" ${disabledAttr}
      data-act="status" data-id="${escHtml(it.id)}">
      Changer statut
    </button>
  </td>
</tr>`;
        }).join("");
    }

    async function loadOrders() {
        const data = await VendorNav.fetchJson("/api/vendor/orders", { method: "GET" });
        ALL = Array.isArray(data) ? data : (data?.items || []);
        const last = qs("#lastSync");
        if (last) last.textContent = "Dernière synchro : " + new Date().toLocaleString("fr-FR");
        render();
    }

    function openStatusModal(orderItemId, currentStatus) {
        _statusOrderItemId = orderItemId;

        const modal = qs("#statusModal");
        const sel = qs("#selNewStatus");
        const err = qs("#statusErr");

        if (err) { err.style.display = "none"; err.textContent = ""; }

        if (sel) {
            const allowed = ["Pending", "Confirmed", "Shipped"];
            sel.value = allowed.includes(currentStatus) ? currentStatus : "Shipped";
        }

        if (modal) modal.style.display = "block";
    }

    function closeStatusModal() {
        const modal = qs("#statusModal");
        if (modal) modal.style.display = "none";
        _statusOrderItemId = null;
    }

    async function saveStatusFromModal() {
        const err = qs("#statusErr");
        const sel = qs("#selNewStatus");
        const status = (sel?.value || "").trim();

        if (!_statusOrderItemId) return;

        try {
            const ok = await updateStatus(_statusOrderItemId, status);

            const it = ALL.find((x) => String(x.id) === String(_statusOrderItemId));
            if (it) it.vendorStatus = ok?.vendorStatus || status;

            closeStatusModal();

            // ✅ recharge liste depuis serveur
            await loadOrders();

            // ✅ 🔴 recharge badge
            if (window.loadVendorOrdersBadge) {
                await loadVendorOrdersBadge();
            }

        } catch (e) {
            if (err) {
                err.style.display = "block";
                err.textContent = (e?.message || "Erreur lors de la mise à jour du statut.");
            }
        }
    }

    function wireUi() {
        qsa(".pill").forEach((p) => {
            p.addEventListener("click", () => {
                setActivePill(p.dataset.filter || "All");
                render();
            });
        });

        qs("#txtSearch")?.addEventListener("input", render);

        // modal buttons
        qs("#btnCloseStatusModal")?.addEventListener("click", closeStatusModal);
        qs("#btnCancelStatus")?.addEventListener("click", closeStatusModal);
        qs("#btnSaveStatus")?.addEventListener("click", saveStatusFromModal);

        // button "changer statut"
        document.addEventListener("click", (ev) => {
            const btn = ev.target.closest("button[data-act='status']");
            if (!btn) return;

            if (btn.disabled) return;

            const id = btn.getAttribute("data-id");
            const it = ALL.find((x) => String(x.id) === String(id));
            openStatusModal(id, it?.vendorStatus);
        });
    }

    window.initVendorOrdersPage = async function () {
        wireUi();
        setActivePill("All");
        await loadOrders();
    };
})();