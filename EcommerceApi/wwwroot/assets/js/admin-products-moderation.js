// wwwroot/assets/js/admin-products-moderation.js
(() => {
    "use strict";

    const qs = (s, r = document) => r.querySelector(s);
    const qsa = (s, r = document) => Array.from(r.querySelectorAll(s));

    const body = qs("#itemsBody");
    const msg = qs("#msg");
    const count = qs("#countItems");
    const btnRefresh = qs("#btnRefresh");

    const ALLOWED_TABS = new Set(["Pending", "Published", "Rejected", "Deleted"]);
    let currentStatus = "Pending";
    let currentStockFilter = "";
    // ✅ anti “réponses qui se croisent”
    let _loadSeq = 0;

    function esc(s) {
        return String(s ?? "").replace(/[&<>"']/g, (c) => ({
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
        return d.toLocaleString("fr-FR", {
            year: "numeric", month: "2-digit", day: "2-digit",
            hour: "2-digit", minute: "2-digit"
        });
    }

    function setRowMessage(t) {
        if (!body) return;
        body.innerHTML = `<tr><td colspan="11" class="muted">${esc(t)}</td></tr>`;
    }

    function normalizeStatus(s) {
        const st = String(s || "").trim();
        return ALLOWED_TABS.has(st) ? st : "Pending";
    }

    function setActiveTab(status) {
        currentStatus = normalizeStatus(status);
        qsa('button[data-tab]').forEach((b) => {
            b.classList.toggle("active", b.dataset.tab === currentStatus);
        });
    }

    function statusBadge(it) {
        if (it.isDeleted) return `<span class="badge" style="background:#7f1d1d;color:#fff;">Deleted</span>`;
        const st = String(it.status || "").toLowerCase();
        if (st === "pending") return `<span class="badge" style="background:#92400e;color:#fff;">Pending</span>`;
        if (st === "published") return `<span class="badge" style="background:#065f46;color:#fff;">Published</span>`;
        if (st === "rejected") return `<span class="badge" style="background:#7f1d1d;color:#fff;">Rejected</span>`;
        return `<span class="badge">${esc(it.status || "")}</span>`;
    }

    function actionButtons(it) {
        if (it.isDeleted || currentStatus === "Deleted") return `<span class="muted">—</span>`;
        return `
      <button class="btn" data-act="publish" data-id="${it.id}">Publier</button>
      <button class="btn danger" data-act="reject" data-id="${it.id}">Rejeter</button>
    `;
    }

    function render(items) {
        items = items || [];
        if (count) count.textContent = String(items.length);

        if (!items.length) {
            setRowMessage("Aucun élément.");
            return;
        }

        body.innerHTML = items.map((it) => {
            const img = it.mainImageUrl
                ? `<img src="${esc(it.mainImageUrl)}" style="width:44px;height:44px;object-fit:cover;border-radius:10px;border:1px solid rgba(255,255,255,.12)" />`
                : `<span class="muted">—</span>`;

            return `
        <tr class="prod-row"
            data-id="${it.id}"
            data-vendorid="${it.vendorId ?? ""}"
            data-tab="${esc(currentStatus)}">
          <td>${it.id}</td>
          <td>${img}</td>
          <td><b>${esc(it.name)}</b></td>
          <td>${statusBadge(it)}</td>
          <td>${esc(it.categoryName || "")}</td>
          <td>${money(it.price)}</td>
          <td>${it.stock ?? 0}</td>
          <td><b>${esc(it.vendorName || "—")}</b></td>
          <td>${esc(fmtDate(it.submittedAt))}</td>
          <td>${it.vendorId ?? ""}</td>
          <td style="white-space:nowrap;">${actionButtons(it)}</td>
        </tr>
      `;
        }).join("");
    }

    async function load() {
        currentStatus = normalizeStatus(currentStatus);

        const mySeq = ++_loadSeq; // ✅ numéro de ce load
        if (msg) msg.textContent = `Chargement ${currentStatus}…`;
        setRowMessage("Chargement…");

        try {
            let url = `/api/admin/products/moderation?status=${encodeURIComponent(currentStatus)}&ts=${Date.now()}`;

            if (currentStockFilter === "low") {
                url += `&stock=low`;
            }

            const data = await window.fetchJson(url, { cache: "no-store" });

            // ✅ si un autre load a démarré après => on ignore cette réponse
            if (mySeq !== _loadSeq) return;

            render(data?.items || []);
            if (msg) msg.textContent = "";
        } catch (e) {
            if (mySeq !== _loadSeq) return;
            if (msg) msg.textContent = "Erreur";
            setRowMessage("Erreur: " + (e?.message || e));
        }
    }

    async function doAction(id, act) {
        // ✅ force no-store pour éviter cache
        return await window.fetchJson(`/api/admin/products/${id}/${act}`, { method: "POST", cache: "no-store" });
    }

    window.initAdminProductsModerationPage = function () {
        const params = new URLSearchParams(window.location.search);

        const urlStatus = params.get("status");
        const urlStock = params.get("stock");

        if (urlStatus && ALLOWED_TABS.has(urlStatus)) {
            currentStatus = urlStatus;
        }

        if (urlStock === "low") {
            currentStockFilter = "low";
        }

        const filterInfo = qs("#activeFilterInfo");
        if (filterInfo && currentStockFilter === "low") {
            filterInfo.textContent = "Filtre actif : stock faible (≤ 5)";
        }


        qsa('button[data-tab]').forEach((btn) => {
            btn.addEventListener("click", async () => {
                setActiveTab(btn.dataset.tab);
                await load();
            });
        });

        btnRefresh?.addEventListener("click", load);

        // ✅ 1 SEUL listener body (actions + navigation)
        body?.addEventListener("click", async (e) => {
            const actBtn = e.target.closest("button[data-act]");
            if (actBtn) {
                e.preventDefault();
                e.stopPropagation();

                const id = actBtn.dataset.id;
                const act = actBtn.dataset.act;

                const row = actBtn.closest("tr.prod-row");
                const vendorId = row?.dataset?.vendorid || "";
                const tab = row?.dataset?.tab || currentStatus;

                // ✅ SI reject → on ouvre la page détail
                if (act === "reject") {
                    location.href =
                        `/admin-product-detail.html?id=${encodeURIComponent(id)}&vendorId=${encodeURIComponent(vendorId)}&tab=${encodeURIComponent(tab)}&mode=reject`;
                    return;
                }

                // Sinon publish normal
                try {
                    actBtn.disabled = true;
                    if (msg) msg.textContent = "Traitement…";
                    await doAction(id, act);
                    await load();
                    if (msg) msg.textContent = "";
                } catch (err) {
                    if (msg) msg.textContent = "Erreur: " + (err?.message || err);
                } finally {
                    actBtn.disabled = false;
                }

                return;
            }



            const row = e.target.closest("tr.prod-row");
            if (!row) return;

            const id = row.dataset.id;
            const vendorId = row.dataset.vendorid || "";
            const tab = row.dataset.tab || currentStatus;

            if (!id) return;

            location.href = `/admin-product-detail.html?id=${encodeURIComponent(id)}&vendorId=${encodeURIComponent(vendorId)}&tab=${encodeURIComponent(tab)}`;
        });

        setActiveTab(currentStatus);
        load();
    };
})();