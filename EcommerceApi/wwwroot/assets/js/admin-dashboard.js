(() => {
    "use strict";

    let salesChartInstance = null;
    let commissionsChartInstance = null;
    let autoRefreshTimer = null;

    function qs(sel) { return document.querySelector(sel); }
    function byId(id) { return document.getElementById(id); }

    function setText(id, value) {
        const el = byId(id);
        if (!el) return;

        if (typeof value === "string" && value.includes("FCFA")) {
            const parts = value.replace("FCFA", "").trim();

            el.innerHTML = `
            ${parts} <span class="currency">FCFA</span>
        `;
        } else {
            el.textContent = value ?? "-";
        }
    }

    function formatMoney(v) {
        const n = Number(v || 0);
        return new Intl.NumberFormat("fr-FR", {
            minimumFractionDigits: 0,
            maximumFractionDigits: 0
        }).format(n) + " FCFA";
    }

    function formatDateTime(value) {
        if (!value) return "-";
        const d = new Date(value);
        if (isNaN(d)) return value;
        return d.toLocaleString("fr-FR", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        });
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function getStatusBadgeClass(status) {
        const s = String(status || "").toLowerCase();

        if (s.includes("attente") || s.includes("requested")) return "bPending";
        if (s.includes("livraison") || s.includes("preparation")) return "bDelivery";
        if (s.includes("livree") || s.includes("livré")) return "bDone";
        if (s.includes("annule") || s.includes("retour") || s.includes("refus")) return "bDanger";

        return "bDelivery";
    }

    function renderDashboardKpis(kpis) {
        setText("kpiOrdersToday", kpis?.ordersToday ?? 0);
        setText("kpiOrdersMonth", kpis?.ordersMonth ?? 0);
        setText("kpiRevenueToday", formatMoney(kpis?.revenueToday));
        setText("kpiRevenueMonth", formatMoney(kpis?.revenueMonth));
        setText("kpiCommissionToday", formatMoney(kpis?.commissionToday));
        setText("kpiCommissionMonth", formatMoney(kpis?.commissionMonth));
        setText("kpiPendingOrders", kpis?.pendingOrders ?? 0);
        setText("kpiReturnsToProcess", kpis?.returnsToProcess ?? 0);
        setText("kpiActiveProducts", kpis?.activeProducts ?? 0);
        setText("kpiActiveVendors", kpis?.activeVendors ?? 0);
    }

    function renderRecentOrders(items) {
        const box = byId("recentOrdersList");
        if (!box) return;

        if (!items || !items.length) {
            box.innerHTML = `<div class="emptyState">Aucune commande récente.</div>`;
            return;
        }

        box.innerHTML = items.map(x => `
            <div class="item">
                <div class="itemTop">
                    <div class="itemTitle">${escapeHtml(x.orderNumber || ("CMD-" + x.id))}</div>
                    <span class="badge ${getStatusBadgeClass(x.status)}">${escapeHtml(x.status || "-")}</span>
                </div>
                <div class="itemMeta">Client : ${escapeHtml(x.customerName || "Client")}</div>
                <div class="itemMeta">Montant : ${formatMoney(x.total)}</div>
                <div class="itemMeta">Date : ${formatDateTime(x.createdAt)}</div>
            </div>
        `).join("");
    }

    function renderNotifications(items) {
        const box = byId("notificationsList");
        if (!box) return;

        if (!items || !items.length) {
            box.innerHTML = `<div class="emptyState">Aucune notification récente.</div>`;
            return;
        }

        box.innerHTML = items.map(x => `
            <div class="item">
                <div class="itemTop">
                    <div class="itemTitle">${escapeHtml(x.title || "Notification")}</div>
                    <div class="itemMeta">${formatDateTime(x.createdAt)}</div>
                </div>
                <div class="itemMeta" style="margin-bottom:8px">${escapeHtml(x.message || "")}</div>
                ${x.link ? `<a class="linkBtn" href="${escapeHtml(x.link)}">Voir</a>` : ""}
            </div>
        `).join("");
    }

    function renderAlerts(items) {
        const box = byId("alertsList");
        if (!box) return;

        if (!items || !items.length) {
            box.innerHTML = `<div class="emptyState">Aucune alerte opérationnelle.</div>`;
            return;
        }

        box.innerHTML = items.map(x => `
            <div class="alertItem">
                <div class="alertLeft">
                    <div class="alertSeverity sev-${escapeHtml((x.severity || "info").toLowerCase())}">
                        ${escapeHtml(x.severity || "info")}
                    </div>
                    <div>${escapeHtml(x.label || "")}</div>
                </div>
                ${x.link ? `<a class="linkBtn" href="${escapeHtml(x.link)}">Ouvrir</a>` : ""}
            </div>
        `).join("");
    }



    let topProductsChartInstance = null;

    async function loadTopProducts() {
        const data = await window.fetchJson("/api/admin/dashboard/top-products");
        const el = document.getElementById("topProductsChart");
        if (!el || !window.Chart) return;

        const labels = (data || []).map(x => x.name || "Produit");
        const values = (data || []).map(x => Number(x.quantity || 0));

        if (topProductsChartInstance) topProductsChartInstance.destroy();

        topProductsChartInstance = new Chart(el, {
            type: "bar",
            data: {
                labels,
                datasets: [{
                    label: "Produits vendus",
                    data: values,
                    backgroundColor: "#22c55e"
                }]
            },
            options: {
                indexAxis: "y",
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        labels: { color: "#e5e7eb" }
                    }
                },
                scales: {
                    x: {
                        ticks: { color: "#9ca3af" },
                        grid: { color: "rgba(255,255,255,.05)" }
                    },
                    y: {
                        ticks: { color: "#9ca3af" },
                        grid: { color: "rgba(255,255,255,.05)" }
                    }
                }
            }
        });
    }




    let salesMapInstance = null;
    let salesMapLayerGroup = null;

    function getCircleRadius(amount) {
        const v = Number(amount || 0);
        if (v <= 0) return 8;
        if (v < 100000) return 10;
        if (v < 300000) return 14;
        if (v < 700000) return 20;
        if (v < 1500000) return 26;
        if (v < 3000000) return 34;
        return 42;
    }

    async function loadSalesByCity() {
        const data = await window.fetchJson("/api/admin/dashboard/sales-by-city");
        const mapEl = document.getElementById("salesMap");
        if (!mapEl || typeof L === "undefined") return;

        if (!salesMapInstance) {
            salesMapInstance = L.map("salesMap", {
                center: [7.54, -5.55],
                zoom: 6,
                zoomControl: true
            });

            L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
                maxZoom: 18,
                attribution: "&copy; OpenStreetMap"
            }).addTo(salesMapInstance);

            salesMapLayerGroup = L.layerGroup().addTo(salesMapInstance);
        }

        salesMapLayerGroup.clearLayers();

        const items = Array.isArray(data) ? data : [];

        items.forEach(x => {
            const sales = Number(x.sales || 0);
            const ordersCount = Number(x.ordersCount || 0);
            const latitude = Number(x.latitude);
            const longitude = Number(x.longitude);

            if (!latitude || !longitude) return;

            const circle = L.circleMarker([latitude, longitude], {
                radius: getCircleRadius(sales),
                color: "#0ea5e9",
                weight: 3,
                fillColor: "#38bdf8",
                fillOpacity: 0.7
            });

            circle.bindPopup(`
            <div style="min-width:190px">
                <div style="font-weight:800;margin-bottom:6px">${escapeHtml(x.city || "Ville")}</div>
                <div>Ventes : <b>${formatMoney(sales)}</b></div>
                <div>Commandes : <b>${ordersCount}</b></div>
            </div>
        `);

            circle.bindTooltip(escapeHtml(x.city || "Ville"), {
                permanent: false,
                direction: "top",
                offset: [0, -8],
                className: "city-label"
            });

            salesMapLayerGroup.addLayer(circle);
        });

        setTimeout(() => {
            salesMapInstance.invalidateSize();
        }, 100);
    }


    let topVendorsChartInstance = null;

    async function loadTopVendors() {
        const data = await window.fetchJson("/api/admin/dashboard/top-vendors");
        const el = document.getElementById("topVendorsChart");
        if (!el || !window.Chart) return;

        const labels = (data || []).map(x => x.name || "Vendeur");
        const values = (data || []).map(x => Number(x.revenue || 0));

        if (topVendorsChartInstance) topVendorsChartInstance.destroy();

        topVendorsChartInstance = new Chart(el, {
            type: "bar",
            data: {
                labels,
                datasets: [{
                    label: "CA vendeurs",
                    data: values,
                    backgroundColor: "#f59e0b"
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        labels: { color: "#e5e7eb" }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return " " + formatMoney(ctx.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        ticks: { color: "#9ca3af" },
                        grid: { color: "rgba(255,255,255,.05)" }
                    },
                    y: {
                        ticks: {
                            color: "#9ca3af",
                            callback: function (value) {
                                return new Intl.NumberFormat("fr-FR").format(value);
                            }
                        },
                        grid: { color: "rgba(255,255,255,.05)" }
                    }
                }
            }
        });
    }

    function renderSalesChart(items) {
        const el = byId("salesChart");
        if (!el || !window.Chart) return;

        const labels = (items || []).map(x => x.label);
        const values = (items || []).map(x => Number(x.value || 0));

        if (salesChartInstance) salesChartInstance.destroy();

        const ctx = el.getContext("2d");
        const gradient = ctx.createLinearGradient(0, 0, 0, 320);
        gradient.addColorStop(0, "rgba(34, 197, 94, 0.38)");
        gradient.addColorStop(1, "rgba(34, 197, 94, 0.02)");

        salesChartInstance = new Chart(el, {
            type: "line",
            data: {
                labels,
                datasets: [{
                    label: "Ventes",
                    data: values,
                    borderColor: "#22c55e",
                    backgroundColor: gradient,
                    pointBackgroundColor: "#22c55e",
                    pointBorderColor: "#bbf7d0",
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    borderWidth: 3,
                    tension: 0.38,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: "index",
                    intersect: false
                },
                plugins: {
                    legend: {
                        labels: {
                            color: "#e5e7eb",
                            font: { weight: "700" }
                        }
                    },
                    tooltip: {
                        backgroundColor: "#0f172a",
                        titleColor: "#ffffff",
                        bodyColor: "#d1d5db",
                        borderColor: "rgba(34,197,94,.35)",
                        borderWidth: 1,
                        callbacks: {
                            label: function (ctx) {
                                return " " + formatMoney(ctx.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        ticks: {
                            color: "#94a3b8"
                        },
                        grid: {
                            color: "rgba(255,255,255,.04)"
                        }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            color: "#94a3b8",
                            callback: function (value) {
                                return new Intl.NumberFormat("fr-FR").format(value);
                            }
                        },
                        grid: {
                            color: "rgba(255,255,255,.05)"
                        }
                    }
                }
            }
        });
    }
    function renderCommissionsChart(items) {
        const el = byId("commissionsChart");
        if (!el || !window.Chart) return;

        const labels = (items || []).map(x => x.label);
        const values = (items || []).map(x => Number(x.value || 0));

        if (commissionsChartInstance) commissionsChartInstance.destroy();

        commissionsChartInstance = new Chart(el, {
            type: "bar",
            data: {
                labels,
                datasets: [{
                    label: "Commissions",
                    data: values,
                    backgroundColor: [
                        "rgba(56, 189, 248, 0.70)",
                        "rgba(59, 130, 246, 0.70)",
                        "rgba(99, 102, 241, 0.70)",
                        "rgba(168, 85, 247, 0.70)",
                        "rgba(236, 72, 153, 0.70)",
                        "rgba(249, 115, 22, 0.70)",
                        "rgba(34, 197, 94, 0.70)"
                    ],
                    borderColor: [
                        "#38bdf8",
                        "#3b82f6",
                        "#6366f1",
                        "#a855f7",
                        "#ec4899",
                        "#f97316",
                        "#22c55e"
                    ],
                    borderWidth: 1.5,
                    borderRadius: 12,
                    maxBarThickness: 46
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        labels: {
                            color: "#e5e7eb",
                            font: { weight: "700" }
                        }
                    },
                    tooltip: {
                        backgroundColor: "#0f172a",
                        titleColor: "#ffffff",
                        bodyColor: "#d1d5db",
                        borderColor: "rgba(56,189,248,.35)",
                        borderWidth: 1,
                        callbacks: {
                            label: function (ctx) {
                                return " " + formatMoney(ctx.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        ticks: {
                            color: "#94a3b8"
                        },
                        grid: {
                            color: "rgba(255,255,255,.04)"
                        }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            color: "#94a3b8",
                            callback: function (value) {
                                return new Intl.NumberFormat("fr-FR").format(value);
                            }
                        },
                        grid: {
                            color: "rgba(255,255,255,.05)"
                        }
                    }
                }
            }
        });
    }

    async function loadDashboard() {
        const errorBox = byId("dashboardError");
        if (errorBox) errorBox.textContent = "";

        const data = await window.fetchJson("/api/admin/dashboard/overview");

        renderDashboardKpis(data.kpis || {});
        renderSalesChart(data.salesByDay || []);
        renderCommissionsChart(data.commissionsByDay || []);
        renderRecentOrders(data.recentOrders || []);
        renderNotifications(data.notifications || []);
        renderAlerts(data.alerts || []);
        updateLastRefreshLabel();
    }
    function updateLastRefreshLabel() {
        const el = byId("lastDashboardUpdate");
        if (!el) return;

        const now = new Date();
        const text = now.toLocaleTimeString("fr-FR", {
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit"
        });

        el.textContent = "Dernière mise à jour : " + text;
    }

    function startAutoRefresh() {
        if (autoRefreshTimer) clearInterval(autoRefreshTimer);

        autoRefreshTimer = setInterval(async () => {
            try {
                await loadDashboard();
                await loadTopProducts();
                await loadTopVendors();
                await loadSalesByCity();
            } catch (err) {
                console.warn("dashboard auto refresh error", err);
            }
        }, 5000);
    }
    window.initAdminDashboardPage = async function () {
        try {
            if (typeof window.requireAdminAuth === "function" && !window.requireAdminAuth()) {
                return;
            }

            if (typeof window.renderAdminHeader === "function") {
                window.renderAdminHeader("Dashboard");
            }

            const btn = byId("btnRefreshDashboard");
            if (btn) {
                btn.addEventListener("click", async () => {
                    try {
                        await loadDashboard();
                    } catch (err) {
                        console.error(err);
                        const errorBox = byId("dashboardError");
                        if (errorBox) errorBox.textContent = err.message || "Erreur chargement dashboard.";
                    }
                });
            }

            await loadDashboard();
            await loadTopProducts();
            await loadTopVendors();
            await loadSalesByCity();
            startAutoRefresh();


        } catch (err) {
            console.error("dashboard init error", err);
            const errorBox = byId("dashboardError");
            if (errorBox) errorBox.textContent = err.message || "Erreur chargement dashboard.";
        }
    };

    document.addEventListener("DOMContentLoaded", () => {
        if (typeof window.initAdminDashboardPage === "function") {
            window.initAdminDashboardPage();
        }
    });
})();