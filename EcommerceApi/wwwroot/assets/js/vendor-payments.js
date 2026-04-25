// wwwroot/assets/js/vendor-payments.js
(() => {
    "use strict";

    const qs = (s, r = document) => r.querySelector(s);
    const esc = (s) =>
        String(s ?? "").replace(/[&<>"']/g, (c) => ({
            "&": "&amp;",
            "<": "&lt;",
            ">": "&gt;",
            '"': "&quot;",
            "'": "&#039;",
        }[c]));

    // UI
    const tbody = qs("#tbody");
    const msg = qs("#msg");
    const txtSearch = qs("#txtSearch");
    const count = qs("#count");
    const period = qs("#period");
    const totalWeek = qs("#totalWeek");
    const selWeek = qs("#selWeek");
    const btnReload = qs("#btnReload");

    // Modal
    const modal = qs("#modal");
    const btnClose = qs("#btnClose");
    const mSub = qs("#mSub");
    const mAmount = qs("#mAmount");
    const mRef = qs("#mRef");
    const mProd = qs("#mProd");
    const mQty = qs("#mQty");
    const mPrice = qs("#mPrice");
    const mFee = qs("#mFee");
    const mNet = qs("#mNet");
    const mDates = qs("#mDates");
    const imgEl = qs("#mImg");

    let ALL = [];

    const money = (n) => (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";
    const fmtDate = (d) => (d ? new Date(d).toLocaleString("fr-FR") : "—");
    const fmtShort = (d) => (d ? new Date(d).toLocaleDateString("fr-FR") : "—");

    function showError(e) {
        console.error(e);
        msg.textContent = "Erreur de chargement: " + (e?.message || e);
        msg.style.display = "block";
    }

    function badgeType(type, reference) {
        const t = String(type || "");
        const r = String(reference || "");

        // remboursement détecté par reference
        if (r.toUpperCase().startsWith("REFUND:")) {
            return `<span class="badge refund">Remboursement</span>`;
        }

        if (t === "DeliveredCredit" || t === "Delivered" || t === "Credit") {
            return `<span class="badge paid">Vente livrée</span>`;
        }

        if (t === "Payout") {
            return `<span class="badge pending">Paiement</span>`;
        }

        return `<span class="badge">${esc(t || "—")}</span>`;
    }

    function render(list) {
        tbody.innerHTML = "";
        count.textContent = String(list.length);

        if (!list.length) {
            msg.textContent = "Aucun paiement trouvé.";
            msg.style.display = "block";
            return;
        }

        msg.style.display = "none";

        for (const it of list) {
            const tr = document.createElement("tr");
            tr.innerHTML = `
      <td>${esc(fmtDate(it.createdAt))}</td>
      <td>${esc(it.reference || "—")}</td>
      <td class="money">${esc(money(it.amount))}</td>
      <td style="text-align:right; display:flex; gap:8px; justify-content:flex-end; align-items:center;">
        ${badgeType(it.type, it.reference)}
        <button class="btn ghost" data-id="${it.id}">Voir</button>
      </td>
    `;
            tbody.appendChild(tr);
        }

        tbody.querySelectorAll("button[data-id]").forEach((b) => {
            b.addEventListener("click", () => openDetail(Number(b.dataset.id)));
        });
    }

    function applyFilter() {
        const q = String(txtSearch.value || "").trim().toLowerCase();
        if (!q) return render(ALL);

        const f = ALL.filter((x) => {
            const ref = String(x.reference || "").toLowerCase();
            const amt = String(x.amount || "").toLowerCase();
            const dt = fmtDate(x.createdAt).toLowerCase();
            return ref.includes(q) || amt.includes(q) || dt.includes(q);
        });

        render(f);
    }

    async function load() {
        msg.textContent = "Chargement…";
        msg.style.display = "block";
        tbody.innerHTML = "";
        ALL = [];

        const weeksAgo = Number(selWeek.value || 0);

        // ✅ Unique fetch vendor centralisé
        const data = await VendorNav.fetchJson(`/api/vendor/payments?weeksAgo=${weeksAgo}`, {
            method: "GET",
        });

        period.textContent = `Du ${fmtShort(data.periodStart)} au ${fmtShort(data.periodEnd)}`;
        totalWeek.textContent = money(data.totalNet ?? 0);

        ALL = Array.isArray(data.items) ? data.items : [];
        render(ALL);
    }

    async function openDetail(txId) {
        try {
            const d = await VendorNav.fetchJson(`/api/vendor/payments/${txId}`, { method: "GET" });

            mSub.textContent = fmtDate(d.createdAt);
            mAmount.textContent = money(d.amount || 0);
            mRef.textContent = d.reference || "—";

            // reset image
            if (imgEl) {
                imgEl.removeAttribute("src");
                imgEl.style.display = "none";
            }

            const oi = d.orderItem;
            if (!oi) {
                mProd.textContent = "Aucun détail produit (référence non liée).";
                mQty.textContent = "—";
                mPrice.textContent = "—";
                mFee.textContent = "—";
                mNet.textContent = "—";
                mDates.textContent = "—";
                modal.style.display = "flex";
                return;
            }

            const buyerName = oi.buyerName ?? oi.BuyerName ?? "—";
            const buyerAddress = oi.buyerAddress ?? oi.BuyerAddress ?? "—";

            const imageUrl =
                oi.productImage ?? oi.ProductImage ?? oi.Productimage ?? "";

            if (imgEl && imageUrl) {
                imgEl.src = imageUrl;
                imgEl.style.display = "block";
            }

            mProd.innerHTML = `
        <div style="margin-bottom:6px">
         <span class="badge paid">Paiement crédité</span>
        </div>

        <div><strong>${esc(oi.productName)}</strong> (Order #${esc(oi.orderId)})</div>
        <div style="margin-top:8px;font-size:14px;opacity:.9">
          <div><strong style="color:#86efac">Acheteur :</strong> ${esc(buyerName)}</div>
          <div><strong>Adresse :</strong> ${esc(buyerAddress)}</div>
        </div>

      `;

            mQty.textContent = String(oi.quantity ?? "—");
            mPrice.textContent = money(oi.unitPriceSnapshot || 0);
            mFee.textContent = money(oi.platformFee || 0);
            mNet.textContent = money(oi.vendorAmount || 0);
            mDates.textContent = `Livré le: ${fmtDate(oi.deliveredAt)} • Crédité le: ${fmtDate(d.createdAt)}`;

            modal.style.display = "flex";
        } catch (e) {
            alert("Erreur détail: " + (e?.message || e));
        }
    }

    function closeModal() {
        modal.style.display = "none";
    }

    window.initVendorPaymentsPage = async function () {
        // ✅ le guard est déjà fait par VendorNav.initVendorPage() dans le HTML
        await load();
    };

    // events
    txtSearch?.addEventListener("input", applyFilter);
    btnReload?.addEventListener("click", () => load().catch(showError));
    selWeek?.addEventListener("change", () => load().catch(showError));
    btnClose?.addEventListener("click", closeModal);
    modal?.addEventListener("click", (e) => {
        if (e.target === modal) closeModal();
    });
})();