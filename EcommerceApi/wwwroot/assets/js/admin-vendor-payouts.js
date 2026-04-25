(() => {
    "use strict";

    const qs = window.qs || ((s, r = document) => r.querySelector(s));
    const esc = (s) => String(s ?? "").replace(/[&<>"']/g, c => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[c]));

    const fmt = (n) => (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";

    function fmtDate(iso) {
        if (!iso) return "—";
        const d = new Date(iso);
        if (isNaN(d)) return esc(iso);
        const p = n => String(n).padStart(2, "0");
        return `${p(d.getDate())}/${p(d.getMonth() + 1)}/${d.getFullYear()} ${p(d.getHours())}:${p(d.getMinutes())}`;
    }
    function toTs(d) {
        const t = Date.parse(d || "");
        return Number.isFinite(t) ? t : 0;
    }

    function setActiveTab(mode) {
        const map = {
            all: "#tabAll",
            pending: "#tabPending",
            payable: "#tabPayable",
            paid: "#tabPaid"
        };

        // retirer active sur tous
        Object.values(map).forEach(sel => {
            const el = qs(sel);
            if (el) {
                el.classList.remove("active-tab", "pending", "payable", "paid");
            }
        });

        // ajouter active au bon bouton
        const active = qs(map[mode]);
        if (active) {
            active.classList.add("active-tab");
            if (mode === "pending") active.classList.add("pending");
            if (mode === "payable") active.classList.add("payable");
            if (mode === "paid") active.classList.add("paid");
        }
    }

    function money(n) {
        return Number(n || 0).toLocaleString("fr-FR") + " FCFA";
    }

    let BATCHES_ALL = [];


    let CURRENT_BATCH_DATA = null;

    async function openBatchDetails(batchId) {
        try {
            const res = await window.fetchJson(`/api/admin/vendor-payouts/batches/${batchId}`);
            if (!res?.ok) return;

            CURRENT_BATCH_DATA = res;

            const info = document.getElementById("batchModalInfo");
            info.innerHTML = `
      Batch #${res.batch.id} |
      ${fmtDate(res.batch.periodStart)} → ${fmtDate(res.batch.periodEnd)} |
      Statut: <b>${res.batch.status}</b>
    `;

            const body = document.getElementById("batchModalBody");
            body.innerHTML = res.payouts.map(p => `
      <tr>
        <td>${esc(p.vendorName)} (#${p.vendorId})</td>
        <td>${p.itemsCount}</td>
        <td><b>${money(p.amount)}</b></td>
        <td>${esc(p.status)}</td>
      </tr>
    `).join("");

            document.getElementById("batchModal").style.display = "block";

        } catch (e) {
            console.error(e);
            alert("Erreur chargement batch");
        }
    }

    function closeBatchModal() {
        document.getElementById("batchModal").style.display = "none";
    }

    function exportBatchCSV() {
        if (!CURRENT_BATCH_DATA) return;

        let csv = "VendorId,VendorName,ItemsCount,Amount,Status\n";

        CURRENT_BATCH_DATA.payouts.forEach(p => {
            csv += `${p.vendorId},"${p.vendorName}",${p.itemsCount},${p.amount},${p.status}\n`;
        });

        const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
        const url = URL.createObjectURL(blob);

        const a = document.createElement("a");
        a.href = url;
        a.download = `batch_${CURRENT_BATCH_DATA.batch.id}.csv`;
        a.click();

        URL.revokeObjectURL(url);
    }


    function groupByVendor(items, nowTs) {
        const now = Number(nowTs || Date.now());
        const map = new Map();

        for (const it of (items || [])) {
            const vendorId = Number(it.vendorId ?? it.VendorId ?? 0);
            if (!vendorId) continue;

            const vendorName = String(it.vendorName ?? it.VendorName ?? "Vendeur").trim();
            const amount = Number(it.amount ?? it.vendorAmount ?? it.VendorAmount ?? 0);

            const deliveredAt = it.deliveredAt ?? it.DeliveredAt;
            const payableAt = it.payableAt ?? it.VendorPayableAt;
            const isPaid = Boolean(it.isPaid ?? it.IsPaid ?? it.isVendorPaid ?? it.IsVendorPaid);

            // ✅ IMPORTANT : si déjà dans un batch, ce n’est plus “payable maintenant”
            const vendorPayoutId = it.vendorPayoutId ?? it.VendorPayoutId ?? null;
            const isBatched = vendorPayoutId !== null && vendorPayoutId !== 0;

            const isDelivered = !!toTs(deliveredAt);

            const isPayableNow =
                isDelivered && !isPaid && !isBatched &&
                toTs(payableAt) > 0 && toTs(payableAt) <= now;

            const isPending =
                isDelivered && !isPaid && !isBatched &&
                (!toTs(payableAt) || toTs(payableAt) > now);

            const isPaidRow = isPaid;

            if (!map.has(vendorId)) {
                map.set(vendorId, {
                    vendorId, vendorName,
                    totalPayable: 0, totalPending: 0, totalPaid: 0,
                    countPayable: 0, countPending: 0, countPaid: 0
                });
            }

            const g = map.get(vendorId);

            if (isPayableNow) { g.totalPayable += amount; g.countPayable++; }
            else if (isPending) { g.totalPending += amount; g.countPending++; }
            else if (isPaidRow) { g.totalPaid += amount; g.countPaid++; }
        }

        return Array.from(map.values()).sort((a, b) => b.totalPayable - a.totalPayable);
    }
    function renderVendorsSummary(groups) {
        const box = document.getElementById("vendorsSummary");
        const hint = document.getElementById("vendorsSummaryHint");
        if (!box) return;

        if (!groups || !groups.length) {
            box.innerHTML = `<div class="muted">Aucun vendeur.</div>`;
            if (hint) hint.textContent = "";
            return;
        }

        const totalPayable = groups.reduce((s, g) => s + g.totalPayable, 0);
        const vendorsPayable = groups.filter(g => g.totalPayable > 0).length;

        if (hint) {
            hint.textContent = `Payable maintenant: ${money(totalPayable)} • vendeurs concernés: ${vendorsPayable}`;
        }

        box.innerHTML = groups.map(g => {
            const badge =
                g.totalPayable > 0 ? `<span class="vbadge delivered">Payable</span>` :
                    g.totalPending > 0 ? `<span class="vbadge pending">En attente</span>` :
                        `<span class="vbadge">OK</span>`;

            return `
      <div style="border:1px solid #1f2937;border-radius:14px;padding:12px;background:#111827">
        <div style="display:flex;justify-content:space-between;align-items:center;gap:10px">
          <div style="font-weight:900">${g.vendorName} <span class="muted">(#${g.vendorId})</span></div>
          ${badge}
        </div>

        <div style="margin-top:10px;display:grid;grid-template-columns:1fr 1fr;gap:8px;font-size:13px">
          <div class="muted">Payable</div>
          <div style="text-align:right;font-weight:900">${money(g.totalPayable)}</div>

          <div class="muted">En attente</div>
          <div style="text-align:right">${money(g.totalPending)}</div>

          <div class="muted">Déjà payé</div>
          <div style="text-align:right">${money(g.totalPaid)}</div>
        </div>

        <div class="muted" style="margin-top:8px;font-size:12px">
          Payable: ${g.countPayable} • En attente: ${g.countPending} • Payés: ${g.countPaid}
        </div>
      </div>
    `;
        }).join("");
    }


    function getTopVendors(groups, limit = 6) {
        return (groups || [])
            .slice()
            .sort((a, b) => {
                if ((b.totalPayable - a.totalPayable) !== 0) return b.totalPayable - a.totalPayable;
                if ((b.totalPending - a.totalPending) !== 0) return b.totalPending - a.totalPending;
                return String(a.vendorName || "").localeCompare(String(b.vendorName || ""), "fr");
            })
            .slice(0, limit);
    }

    function renderVendorsSummaryTop6(groups) {
        const box = document.getElementById("vendorsSummary");
        const hint = document.getElementById("vendorsSummaryHint");
        if (!box) return;

        if (!groups || !groups.length) {
            box.innerHTML = `<div class="muted">Aucun vendeur.</div>`;
            if (hint) hint.textContent = "";
            return;
        }

        const totalPayable = groups.reduce((s, g) => s + g.totalPayable, 0);
        const vendorsPayable = groups.filter(g => g.totalPayable > 0).length;
        if (hint) hint.textContent = `Payable maintenant: ${money(totalPayable)} • vendeurs concernés: ${vendorsPayable}`;

        const top6 = getTopVendors(groups, 6);
        const restCount = Math.max(0, groups.length - top6.length);

        box.innerHTML = `
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:10px">
        <div class="muted">Top 6 vendeurs (par payable)</div>
        <button class="btn btn-sm" id="btnShowAllVendors">
          Voir tous (${groups.length})
        </button>
      </div>

      <div id="vendorsTopGrid" style="display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px"></div>

      <div id="vendorsAllPanel" style="display:none;margin-top:12px">
        <div class="muted" style="margin-bottom:8px">Tous les vendeurs (${groups.length})</div>
        <input id="vendorsAllSearch" class="search" placeholder="Rechercher vendeur..." style="max-width:340px;margin-bottom:10px" />
        <div id="vendorsAllGrid" style="display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px"></div>
      </div>
    `;

        // ✅ Top 6
        const topGrid = document.getElementById("vendorsTopGrid");
        topGrid.innerHTML = top6.map(g => vendorCard(g)).join("");

        // ✅ Panel “Tous”
        const allPanel = document.getElementById("vendorsAllPanel");
        const allGrid = document.getElementById("vendorsAllGrid");
        allGrid.innerHTML = groups.map(g => vendorCard(g)).join("");

        document.getElementById("btnShowAllVendors").onclick = () => {
            allPanel.style.display = (allPanel.style.display === "none") ? "block" : "none";
        };

        document.getElementById("vendorsAllSearch").addEventListener("input", (e) => {
            const q = String(e.target.value || "").trim().toLowerCase();
            const filtered = !q ? groups : groups.filter(v =>
                `${v.vendorId} ${v.vendorName}`.toLowerCase().includes(q)
            );
            allGrid.innerHTML = filtered.map(g => vendorCard(g)).join("");
        });
    }

    function vendorCard(g) {
        const badge =
            g.totalPayable > 0 ? `<span class="vbadge shipped">Payable</span>` :
                g.totalPending > 0 ? `<span class="vbadge pending">En attente</span>` :
                    `<span class="vbadge">OK</span>`;

        return `
      <div style="border:1px solid #1f2937;border-radius:14px;padding:12px;background:#111827">
        <div style="display:flex;justify-content:space-between;align-items:center;gap:10px">
          <div style="font-weight:900">${esc(g.vendorName)} <span class="muted">(#${esc(g.vendorId)})</span></div>
          ${badge}
        </div>

        <div style="margin-top:10px;display:grid;grid-template-columns:1fr 1fr;gap:8px;font-size:13px">
          <div class="muted">Payable</div>
          <div style="text-align:right;font-weight:900">${money(g.totalPayable)}</div>

          <div class="muted">En attente</div>
          <div style="text-align:right">${money(g.totalPending)}</div>

          <div class="muted">Déjà payé</div>
          <div style="text-align:right">${money(g.totalPaid)}</div>
        </div>

        <div class="muted" style="margin-top:8px;font-size:12px">
          Payable: ${g.countPayable} • En attente: ${g.countPending} • Payés: ${g.countPaid}
        </div>
      </div>
    `;
    }


    function badge(st) {
        const s = String(st || "").toLowerCase();
        if (s === "paid") return `<span class="vbadge delivered">Paid</span>`;
        if (s === "payable") return `<span class="vbadge shipped">Payable</span>`;
        if (s === "pending") return `<span class="vbadge pending">Pending</span>`;
        return `<span class="vbadge">${esc(st || "—")}</span>`;
    }

    let MODE = "all";
    let ALL = [];

    let LAST_TOTAL = 0;
    let NOW_TS = Date.now();


    async function apiGet(mode) {
        const url = `/api/admin/vendor-payouts?mode=${encodeURIComponent(mode)}&ts=${Date.now()}`;
        const data = await window.fetchJson(url, { cache: "no-store" });
        return data;
    }

    function setRow(msg) {
        const body = qs("#itemsBody");
        body.innerHTML = `<tr><td colspan="7" class="muted">${esc(msg)}</td></tr>`;
    }
    function render(list, totalPayableNow) {
        const body = qs("#itemsBody");
        const resume = qs("#resume");

        if (resume) {
            resume.innerHTML =
                `Total payable maintenant: <b>${fmt(totalPayableNow || 0)}</b> · éléments: <b>${list.length}</b>`;
        }

        if (!list.length) {
            setRow("Aucun élément.");
            return;
        }

        body.innerHTML = list.map(x => {
            const isBatched = x.vendorPayoutId != null && x.vendorPayoutId !== 0;

            let st = "Pending";

            if (x.isPaid) {
                st = "Paid";
            } else if (!isBatched && x.payableAt && new Date(x.payableAt).getTime() <= NOW_TS) {
                st = "Payable";
            } else {
                st = "Pending";
            }

            // ✅ si tu es sur l'onglet "payable", n'affiche Payable que si NON batché
            if (MODE === "payable") {
                st = (!x.isPaid && !isBatched && x.payableAt && new Date(x.payableAt).getTime() <= NOW_TS)
                    ? "Payable"
                    : "Pending";
            }

            return `
      <tr>
        <td>#${esc(x.id)}</td>
        <td><a class="btn-view" href="/admin-order.html?id=${encodeURIComponent(x.orderId)}">Commande ${esc(x.orderId)}</a></td>
        <td>${esc(x.vendorName)} <span class="muted">(#${esc(x.vendorId)})</span></td>
        <td><b>${fmt(x.amount)}</b></td>
        <td class="muted">${fmtDate(x.deliveredAt)}</td>
        <td class="muted">${fmtDate(x.payableAt)}</td>
        <td>${badge(st)}</td>
      </tr>
    `;
        }).join("");
    }

    function applySearch() {
        const q = (qs("#txtSearch")?.value || "").trim().toLowerCase();
        if (!q) return ALL;

        return ALL.filter(x => {
            const txt = `${x.id} ${x.orderId} ${x.vendorName} ${x.vendorId}`.toLowerCase();
            return txt.includes(q);
        });
    }



    async function load(mode) {
        MODE = mode;

        setActiveTab(mode);
        setRow("Chargement...");

        const data = await apiGet(mode);
        ALL = data.items || [];

        // ✅ On utilise l'heure serveur (important)
        NOW_TS = Number(data.nowTs || Date.now());

        // ✅ liste filtrée (recherche)
        const filtered = applySearch();

        // ✅ total (vient du serveur)
        LAST_TOTAL = data.totalPayableNow || 0;

        // ✅ table items
        render(filtered, LAST_TOTAL);

        // ✅ résumé par vendeur
        const groups = groupByVendor(ALL, NOW_TS);
        renderVendorsSummaryTop6(groups);
    }

    function wireTabs() {
        qs("#tabAll")?.addEventListener("click", () => load("all"));
        qs("#tabPending")?.addEventListener("click", () => load("pending"));
        qs("#tabPayable")?.addEventListener("click", () => load("payable"));
        qs("#tabPaid")?.addEventListener("click", () => load("paid"));

        qs("#txtSearch")?.addEventListener("input", () => {
            render(applySearch(), LAST_TOTAL);
        });
    }

    // =======================
    // BATCHES (manuel)
    // =======================
    async function apiGetBatches() {
        const url = `/api/admin/vendor-payouts/batches?ts=${Date.now()}`;
        return await window.fetchJson(url, { cache: "no-store" });
    }

    async function apiCreateBatch(periodStartIso, periodEndIso) {
        const url = `/api/admin/vendor-payouts/batches/create`;
        return await window.fetchJson(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ periodStart: periodStartIso, periodEnd: periodEndIso })
        });
    }

    async function apiMarkPaid(batchId, providerRef) {
        const url = `/api/admin/vendor-payouts/batches/${encodeURIComponent(batchId)}/mark-paid`;
        return await window.fetchJson(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ provider: "Manual", providerRef: providerRef || null })
        });
    }

    function getLastWeekRangeUtc() {
        // lundi 00:00 -> dimanche 23:59:59 de la semaine précédente (UTC)
        const now = new Date();
        const utcDay = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
        const day = utcDay.getUTCDay(); // 0=dim,1=lun...
        const diffToMonday = (day === 0 ? 6 : day - 1);

        const mondayThisWeek = new Date(utcDay);
        mondayThisWeek.setUTCDate(utcDay.getUTCDate() - diffToMonday);

        const mondayLastWeek = new Date(mondayThisWeek);
        mondayLastWeek.setUTCDate(mondayThisWeek.getUTCDate() - 7);

        const sundayLastWeekEnd = new Date(mondayThisWeek);
        sundayLastWeekEnd.setUTCSeconds(sundayLastWeekEnd.getUTCSeconds() - 1);

        return { startIso: mondayLastWeek.toISOString(), endIso: sundayLastWeekEnd.toISOString() };
    }

    function renderBatches(batches) {
        const box = qs("#batchesBox");
        const hint = qs("#batchesHint");
        if (!box) return;

        BATCHES_ALL = Array.isArray(batches) ? batches : [];

        if (!BATCHES_ALL.length) {
            box.innerHTML = `<div class="muted">Aucun batch.</div>`;
            if (hint) hint.textContent = "";
            return;
        }

        const totalUnpaid = BATCHES_ALL
            .filter(b => String(b.status || "").toLowerCase() !== "paid")
            .reduce((s, b) => s + Number(b.total || 0), 0);

        if (hint) hint.textContent = `Non payés: ${money(totalUnpaid)} • Batchs: ${BATCHES_ALL.length}`;

        applyBatchSearch(); // ✅ rend l'UI (filtrée)
    }

     




    function batchSearchText(b) {
        return [
            b.id,
            b.status,
            b.total,
            b.vendorsCount,
            b.shopsPreview,
            fmtDate(b.periodStart),
            fmtDate(b.periodEnd),
            fmtDate(b.createdAt),
            b.paidAt ? fmtDate(b.paidAt) : ""
        ].join(" ").toLowerCase();
    }

   


    function renderBatchCards(list) {
        const box = qs("#batchesBox");
        if (!box) return;

        box.innerHTML = (list || []).map(b => {
            const st = String(b.status || "—");
            const isPaid = st.toLowerCase() === "paid";

            return `
      <div class="batch-card" style="border:1px solid #1f2937;border-radius:14px;padding:12px;background:#111827">
        <div style="display:flex;justify-content:space-between;align-items:center;gap:10px">
          <div style="font-weight:900;cursor:pointer;color:#22c55e" onclick="openBatchDetails(${b.id})">
            Batch #${esc(b.id)}
          </div>
          ${badge(isPaid ? "Paid" : "Pending")}
        </div>

        <div class="muted" style="margin-top:6px;font-size:12px">
          Période: <b>${fmtDate(b.periodStart)}</b> → <b>${fmtDate(b.periodEnd)}</b>
        </div>

        <div style="margin-top:10px;display:grid;gap:6px">
         <div class="line"><span class="muted">Boutiques</span><span class="val">${esc(b.shopsPreview || "—")}</span></div>
          <div class="line"><span class="muted">Total</span><span class="val">${money(b.total)}</span></div>
          <div class="line"><span class="muted">Statut</span><span class="val">${esc(st)}</span></div>
        </div>

        <div class="muted" style="margin-top:8px;font-size:12px">
          Créé: ${fmtDate(b.createdAt)} ${b.paidAt ? ` • Payé: ${fmtDate(b.paidAt)}` : ""}
        </div>

        <div style="margin-top:10px;display:flex;gap:8px;justify-content:flex-end;flex-wrap:wrap">
          ${isPaid ? "" : `<button class="btn btnYellow" data-batch-pay="${esc(b.id)}">Marquer payé</button>`}
          <button class="btn" data-batch-pdf="${esc(b.id)}">PDF</button>
        </div>
      </div>
    `;
        }).join("");

        // ✅ rewire boutons (ICI seulement)
        document.querySelectorAll("[data-batch-pay]").forEach(btn => {
            btn.addEventListener("click", async () => {
                const id = btn.getAttribute("data-batch-pay");
                const ref = prompt("Référence virement (optionnel) :") || "";
                try {
                    btn.disabled = true;
                    await apiMarkPaid(id, ref);
                    await refreshAll();
                    alert("Batch marqué payé ✅");
                } catch (e) {
                    console.error(e);
                    alert("Erreur mark-paid ❌");
                } finally {
                    btn.disabled = false;
                }
            });
        });

        document.querySelectorAll("[data-batch-pdf]").forEach(btn => {
            btn.addEventListener("click", async () => {
                const id = btn.getAttribute("data-batch-pdf");
                try {
                    btn.disabled = true;
                    const token = localStorage.getItem("ranita_admin_token");
                    const res = await fetch(`/api/admin/vendor-payouts/batches/${id}/pdf`, {
                        method: "GET",
                        headers: { "Authorization": `Bearer ${token}` }
                    });

                    if (!res.ok) {
                        const txt = await res.text();
                        alert("Erreur PDF: " + res.status + "\n" + txt);
                        return;
                    }

                    const blob = await res.blob();
                    const url = URL.createObjectURL(blob);
                    const a = document.createElement("a");
                    a.href = url;
                    a.download = `batch_${id}.pdf`;
                    a.click();
                    URL.revokeObjectURL(url);
                } catch (e) {
                    console.error(e);
                    alert("Erreur téléchargement PDF");
                } finally {
                    btn.disabled = false;
                }
            });
        });
    }

    function digitsOnly(x) {
        return String(x ?? "").replace(/[^\d]/g, ""); // garde seulement 0-9
    }


    function applyBatchSearch() {
        const box = qs("#batchesBox");
        if (!box) return;

        const qRaw = (qs("#txtBatchSearch")?.value || "").trim().toLowerCase();
        let list = BATCHES_ALL;

        if (qRaw) {
            if (qRaw.startsWith("id:")) {
                const v = qRaw.replace("id:", "").trim();
                list = BATCHES_ALL.filter(b => String(b.id).includes(v));

            } else if (qRaw.startsWith("status:")) {
                const v = qRaw.replace("status:", "").trim();
                list = BATCHES_ALL.filter(b => String(b.status || "").toLowerCase().includes(v));

            } else if (qRaw.startsWith("total:")) {
                const v = digitsOnly(qRaw.replace("total:", "").trim());
                list = BATCHES_ALL.filter(b => digitsOnly(b.total).includes(v));

            } else if (qRaw.startsWith("date:")) {
                const v = qRaw.replace("date:", "").trim();
                list = BATCHES_ALL.filter(b =>
                    fmtDate(b.periodStart).includes(v) || fmtDate(b.periodEnd).includes(v)
                );

            } else if (qRaw.startsWith("vendor:")) {
                const v = qRaw.replace("vendor:", "").trim();
                list = BATCHES_ALL.filter(b => String(b.vendorsCount || "").includes(v));

            } else {
                // ✅ recherche globale : on supporte aussi les montants saisis avec espaces/FCFA
                const qDigits = digitsOnly(qRaw);
                list = BATCHES_ALL.filter(b => {
                    const text = batchSearchText(b);
                    if (text.includes(qRaw)) return true;
                    if (qDigits && digitsOnly(b.total).includes(qDigits)) return true;
                    return false;
                });
            }
        }

        if (!list.length) {
            box.innerHTML = `<div class="muted" style="padding:10px">Aucun batch trouvé.</div>`;
            return;
        }

        renderBatchCards(list);
    }

    async function refreshBatches() {
        const data = await apiGetBatches();
        console.log("[BATCHES API]", data);

        const list =
            data?.batches ??
            data?.items ??
            data?.data?.batches ??
            data?.data?.items ??
            []; // fallback

        renderBatches(list);
    }

    async function refreshAll() {
        await Promise.all([refreshBatches(), load(MODE)]);
    }

    function wireBatchButtons() {
        qs("#btnRefreshBatches")?.addEventListener("click", refreshBatches);

        qs("#btnCreateWeeklyBatch")?.addEventListener("click", async () => {
            const ok = confirm("Créer un batch avec TOUS les items payables maintenant ?");
            if (!ok) return;

            try {
                const res = await window.fetchJson(`/api/admin/vendor-payouts/batches/create-payable-now`, {
                    method: "POST"
                });

                if (res?.created === false) {
                    alert(res?.message || "Aucun item payable maintenant.");
                    return;
                }

                alert(`Batch créé ✅\nBatchId: ${res.batchId}\nVendeurs: ${res.vendors}\nItems: ${res.items}\nTotal: ${money(res.total)}`);
                await refreshAll();
            } catch (e) {
                console.error(e);
                alert("Erreur création batch ❌");
            }
        });
        qs("#txtBatchSearch")?.addEventListener("input", applyBatchSearch);
    }

    function initAdminVendorPayoutsPage() {
        wireTabs();
        wireBatchButtons();
        refreshBatches();
        load("all");
    }

    window.initAdminVendorPayoutsPage = initAdminVendorPayoutsPage;

    window.openBatchDetails = openBatchDetails;
    window.closeBatchModal = closeBatchModal;
    window.exportBatchCSV = exportBatchCSV;
})();