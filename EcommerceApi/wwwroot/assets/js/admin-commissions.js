(() => {
    const API = location.origin;
    const token = localStorage.getItem("ranita_admin_token");

    if (!token) {
        location.href = "/admin-login.html";
        return;
    }

    const qs = (s) => document.querySelector(s);
    const formatMoney = (n) => Number(n || 0).toLocaleString("fr-FR") + " FCFA";

    let _chart = null;

    function toYMD(d) {
        const yyyy = d.getFullYear();
        const mm = String(d.getMonth() + 1).padStart(2, "0");
        const dd = String(d.getDate()).padStart(2, "0");
        return `${yyyy}-${mm}-${dd}`;
    }
    // ✅ Ajoute juste ici
    function asStartIso(ymd) {
        return `${ymd}T00:00:00Z`;
    }

    function asEndIso(ymd) {
        return `${ymd}T23:59:59Z`;
    }

    // ✅ SEMAINE EN COURS (lundi -> dimanche)
    function getCurrentWeek() {
        const now = new Date();
        const day = now.getDay(); // 0=dimanche
        const diffToMonday = (day === 0 ? -6 : 1) - day;

        const monday = new Date(now);
        monday.setDate(now.getDate() + diffToMonday);

        const sunday = new Date(monday);
        sunday.setDate(monday.getDate() + 6);

        return { start: toYMD(monday), end: toYMD(sunday) };
    }

    async function loadSummaryTop() {
        // garde ton summary global (solde + mois)
        const res = await fetch(`${API}/api/admin/commissions/summary?weeks=8`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        if (!res.ok) return;

        const data = await res.json();
        qs("#adminBalance").textContent = formatMoney(data.adminBalance);
        qs("#todayTotal").textContent = formatMoney(data.todayTotal);
    }

    async function loadData() {
        const start = qs("#startDate").value;
        const end = qs("#endDate").value;

        const url = `${API}/api/admin/commissions/weekly?start=${encodeURIComponent(asStartIso(start))}&end=${encodeURIComponent(asEndIso(end))}&ts=${Date.now()}`;
        const res = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });

        if (!res.ok) {
            const txt = await res.text();
            alert("Erreur chargement: " + txt);
            return;
        }

        const data = await res.json();

        qs("#totalCommission").textContent = formatMoney(data.totalCommission);
        qs("#totalOrders").textContent = data.totalOrders;
        qs("#countLines").textContent = data.count;

        const tbody = qs("#tableBody");
        tbody.innerHTML = "";

        (data.details || []).forEach((x) => {
            tbody.innerHTML += `
        <tr>
          <td>${x.id}</td>
          <td>${x.vendorShopName ?? x.shopName ?? "—"}</td>  <!-- ✅ AJOUT -->
          <td>${x.orderId ?? ""}</td>
          <td>${x.orderItemId ?? ""}</td>
          <td>${formatMoney(x.amount)}</td>
          <td>${new Date(x.createdAt).toLocaleString("fr-FR")}</td>
          <td>${x.note ?? ""}</td>
        </tr>
      `;
        });
    }

    // ✅ Graphique qui suit le filtre
    async function loadChartFromFilter() {
        const start = qs("#startDate").value;
        const end = qs("#endDate").value;

        const url = `${API}/api/admin/commissions/range-daily?start=${encodeURIComponent(asStartIso(start))}&end=${encodeURIComponent(asEndIso(end))}&ts=${Date.now()}`;
        const res = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
        if (!res.ok) return;

        const data = await res.json();

        const labels = (data.dailySeries || []).map(d => {
            const dt = new Date(d.day);
            return dt.toLocaleDateString("fr-FR", { day: "2-digit", month: "2-digit" });
        });

        const values = (data.dailySeries || []).map(d => Number(d.totalCommission || 0));

        const canvas = qs("#commChart");
        if (!canvas) return;

        const ctx = canvas.getContext("2d");
        if (_chart) _chart.destroy();

        _chart = new Chart(ctx, {
            type: "line",
            data: {
                labels,
                datasets: [{
                    label: "Commissions / jour",
                    data: values,
                    tension: 0.25,
                    fill: true,
                    pointRadius: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: true } },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: (v) => Number(v).toLocaleString("fr-FR") + " FCFA"
                        }
                    }
                }
            }
        });
    }

    async function reloadAll() {
        await loadSummaryTop();      // solde + mois (global)
        await loadData();            // tableau + cards (filtre)
        await loadChartFromFilter(); // graphique (filtre)
    }

    async function init() {
        const week = getCurrentWeek();
        qs("#startDate").value = week.start;
        qs("#endDate").value = week.end;

        await reloadAll();
    }

    qs("#btnLoad").addEventListener("click", reloadAll);

    qs("#startDate").addEventListener("change", reloadAll);
    qs("#endDate").addEventListener("change", reloadAll);

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();