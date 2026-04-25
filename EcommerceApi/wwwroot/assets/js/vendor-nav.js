// wwwroot/assets/js/vendor-nav.js
(() => {
    "use strict";

    // =========================
    // Config & helpers (global safe)
    // =========================
    const VENDOR_TOKEN_KEY = "ranita_vendor_token";
    const VENDOR_USER_KEY = "ranita_vendor_user"; // optionnel si tu stockes le user

    const API_BASE = location.origin; // même domaine (localhost/ngrok)
    window.VENDOR_API_BASE = API_BASE;

    const qs = (s, r = document) => r.querySelector(s);

    function getToken() {
        return localStorage.getItem(VENDOR_TOKEN_KEY) || "";
    }

    function decodeJwt(token) {
        try {
            const part = token.split(".")[1];
            const json = atob(part.replace(/-/g, "+").replace(/_/g, "/"));
            return JSON.parse(decodeURIComponent(escape(json)));
        } catch {
            return null;
        }
    }

    function isVendorTokenValid(token) {
        if (!token) return false;
        const payload = decodeJwt(token);
        if (!payload) return false;

        // exp (en secondes)
        if (payload.exp && Date.now() >= payload.exp * 1000) return false;

        // rôle (selon ton API)
        const role =
            payload.role ||
            payload?.["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
            "";

        return String(role).toLowerCase() === "vendor";
    }

    function logout() {
        localStorage.removeItem(VENDOR_TOKEN_KEY);
        localStorage.removeItem(VENDOR_USER_KEY);
        location.href = "/vendor-login.html";
    }

    function getVendorDisplayName() {
        const token = getToken();
        const payload = decodeJwt(token);
        if (!payload) return "Ma boutique";

        return (
            payload.storeName ||
            payload.shopName ||
            payload.name ||
            payload.unique_name ||
            "Ma boutique"
        );
    }

    // =========================
    // API wrapper (vendor)
    // =========================
    async function vendorFetchJson(url, opts = {}) {
        const token = getToken();
        const headers = Object.assign(
            { "Content-Type": "application/json" },
            opts.headers || {},
            token ? { Authorization: `Bearer ${token}` } : {}
        );

        const res = await fetch(url, { ...opts, headers, cache: "no-store" });
        const txt = await res.text();
        let data = null;
        try {
            data = txt ? JSON.parse(txt) : null;
        } catch {
            /* ignore */
        }

        if (!res.ok) {
            // si 401/403 -> logout
            if (res.status === 401 || res.status === 403) logout();
            const msg = data?.message || data?.error || txt || `HTTP ${res.status}`;
            throw new Error(msg);
        }
        return data;
    }

    // =========================
    // NAV rendering
    // =========================
function linkItem(href, label, icon = "•") {
    const badge = href === "/vendor-orders.html"
        ? `<span id="vendorOrdersBadge" class="vendor-orders-badge" style="display:none">0</span>`
        : "";

    return `
      <a class="vnav-link" href="${href}" data-href="${href}">
        <span class="vnav-ico">${icon}</span>
        <span>${label}</span>
        ${badge}
      </a>
    `.trim();
}

    function renderVendorNav(active) {
        return `
      <div class="vnav">
        <div class="vnav-inner">
          <div class="vnav-left">
            <a class="vnav-brand" href="/vendor-dashboard.html">
              <span class="vnav-logo">

              <img src="/assets/img/logo-192.png" alt="Ranita"
               onerror="this.onerror=null;this.src='/assets/img/placeholder.jpg';" />

              </span>
             <span id="vendorStoreName">Ma boutique</span>
            </a>

            <nav class="vnav-links">
              ${linkItem("/vendor-dashboard.html", "Dashboard", "🏠")}
              ${linkItem("/vendor-products.html", "Produits", "📦")}
              ${linkItem("/vendor-orders.html", "Commandes", "🧾")}
              ${linkItem("/vendor-payments.html", "Paiements", "💳")}
            </nav>
          </div>

<div class="vnav-right">
  <span class="vnav-badge">Vendor</span>

  <button class="vnav-btn vnav-btn-primary" id="btnVendorNewProduct" type="button">+ Produit</button>

  <button class="vnav-btn vnav-btn-danger" id="btnVendorLogout" type="button">Se déconnecter</button>
</div>
        </div>
      </div>
    `;
    }

    function setActiveLink(activePath) {
        const path = (activePath || location.pathname || "").toLowerCase();
        document.querySelectorAll(".vnav-link").forEach((a) => {
            const href = String(a.getAttribute("data-href") || "").toLowerCase();
            a.classList.toggle("active", href === path || href.endsWith(path));
        });
    }

    function ensureStyles() {
        if (qs("#vendorNavStyles")) return;

        const css = `
      /* ✅ BARRE PLEINE LARGEUR + COLLEE EN HAUT */
#vendorNav{
  width:100%;
  margin:0;
  padding:0;
  height:88px; /* réserve la place du header */
}

.vnav{
  position: fixed;
  top: 0;
  left: 0;
  z-index: 9999;
  width: 100%;
  margin: 0;
  padding: 0;

  background: rgba(17,24,39,.92);
  backdrop-filter: blur(12px);
  border-bottom: 1px solid rgba(148,163,184,.18);
  box-shadow: 0 12px 28px rgba(0,0,0,.35);
}

      /* ✅ contenu centré dans la barre */
      .vnav-inner{
        max-width:1180px;
        margin:0 auto;
        padding:14px 16px;
        display:flex;
        align-items:center;
        justify-content:space-between;
        gap:14px;
      }

      .vnav-left{display:flex;align-items:center;gap:14px;min-width:0}
      .vnav-brand{
        display:flex;align-items:center;gap:12px;
        color:#e5e7eb;text-decoration:none;font-weight:900;
        white-space:nowrap
      }

      /* ✅ LOGO ROND */
/* ✅ LOGO ROND (blindé contre base.css) */
.vnav-logo{
  width:48px !important;
  height:48px !important;
  border-radius:999px !important;
  background:#fff !important;
  border:2px solid rgba(34,197,94,.6) !important;
  overflow:hidden !important;
  display:flex !important;
  align-items:center !important;
  justify-content:center !important;
  flex:0 0 48px !important;
}

.vnav-logo img{
  width:72% !important;
  height:72% !important;
  object-fit:contain !important;
  object-position:center !important;
  display:block !important;
  border-radius:0 !important;
  max-width:none !important;
  max-height:none !important;
}
      .vnav-links{display:flex;align-items:center;gap:8px;flex-wrap:wrap}
.vnav-link{
  position: relative;
  display:flex;
  align-items:center;
  gap:8px;
  padding:9px 12px;
  border-radius:999px;
  color:#e5e7eb;
  text-decoration:none;
  border:1px solid rgba(148,163,184,.18);
  background: rgba(2,6,23,.35);
  font-size:13px;
}
      .vnav-link:hover{border-color:rgba(148,163,184,.35);background: rgba(2,6,23,.55)}
      .vnav-link.active{
        border-color: rgba(34,197,94,.55);
        box-shadow: 0 0 0 3px rgba(34,197,94,.12);
      }
      .vnav-ico{width:18px;text-align:center}

      .vnav-right{display:flex;align-items:center;gap:10px;flex:0 0 auto}
      .vnav-badge{
        font-size:12px;padding:6px 10px;border-radius:999px;
        border:1px solid rgba(148,163,184,.18);
        color:#94a3b8;background: rgba(2,6,23,.35);
      }
      .vnav-btn{
        padding:9px 12px;border-radius:12px;
        border:1px solid rgba(148,163,184,.22);
        background: rgba(2,6,23,.35);
        color:#e5e7eb;cursor:pointer;
      }

      .vnav-btn-primary{
  border-color: rgba(34,197,94,.55);
  box-shadow: 0 0 0 3px rgba(34,197,94,.10);
}
.vnav-btn-primary:hover{ border-color: rgba(34,197,94,.75); }

.vnav-btn-danger:hover{ border-color: rgba(239,68,68,.55); color:#fca5a5; }
      .vnav-btn:hover{border-color:rgba(239,68,68,.55);color:#fca5a5}

@media (max-width: 560px){
  .vnav-inner{
    flex-wrap: wrap !important;
    gap: 10px !important;
    padding: 12px 12px !important;
  }

  /* ✅ menu visible + scroll horizontal */
  .vnav-links{
    display: flex !important;
    width: 100% !important;
    order: 3 !important;
    flex-wrap: nowrap !important;
    overflow-x: auto !important;
    -webkit-overflow-scrolling: touch !important;
    gap: 8px !important;
    padding-bottom: 6px !important;
  }
  .vnav-links::-webkit-scrollbar{ height:0 !important; }

  .vnav-link{
    flex: 0 0 auto !important;
    white-space: nowrap !important;
    font-size: 12px !important;
    padding: 8px 10px !important;
  }

  /* ✅ boutons restent visibles */
  .vnav-right{
    width: 100% !important;
    order: 2 !important;
    justify-content: flex-end !important;
    gap: 8px !important;
    flex-wrap: wrap !important;
  }
}

.vendor-orders-badge{
  position: absolute;
  top: -6px;
  right: -6px;

  min-width:18px;
  height:18px;
  padding:0 6px;

  border-radius:999px;
  background:#ef4444;
  color:#fff;

  font-size:11px;
  font-weight:950;

  align-items:center;
  justify-content:center;

  display:none; /* ✅ UNE SEULE FOIS */
}
    `;

        const style = document.createElement("style");
        style.id = "vendorNavStyles";
        style.textContent = css;
        document.head.appendChild(style);
    }

    // =========================
    // Public API: initVendorPage
    // =========================

    async function loadVendorOrdersBadge() {
        try {
            const badge = document.getElementById("vendorOrdersBadge");
            if (!badge) return;

            const data = await vendorFetchJson("/api/vendor/orders/unread-count", {
                method: "GET"
            });

            const count = Number(data?.count || 0);

            console.log("BADGE COUNT =", count);

            if (count > 0) {
                badge.textContent = count > 99 ? "99+" : String(count);
                badge.style.display = "inline-flex";
            } else {
                badge.style.display = "none";
            }
        } catch (e) {
            console.warn("Badge commandes vendor:", e.message || e);
        }
    }
    function initVendorPage({ active = "" } = {}) {
        const token = getToken();
        if (!isVendorTokenValid(token)) {
            logout();
            return;
        }

        const host = qs("#vendorNav");
        if (!host) return;

        ensureStyles();
        host.innerHTML = renderVendorNav(active);
        setActiveLink(active);

        loadVendorOrdersBadge();
        setInterval(loadVendorOrdersBadge, 10000);

        // ✅ Affiche le nom boutique
        vendorFetchJson(`/api/vendor/me`)
            .then(data => {
                const nameEl = document.getElementById("vendorStoreName");
                if (!nameEl) return;

                // priorité: storeName (on l’a ajouté côté API)
                const storeName =
                    data?.storeName ||
                    data?.vendor?.name ||   // camelCase
                    data?.vendor?.Name ||   // au cas où
                    "Ma boutique";

                nameEl.textContent = storeName;
            })
            .catch(() => { });

        const btn = qs("#btnVendorLogout");
        btn && btn.addEventListener("click", logout);

        const btnNew = qs("#btnVendorNewProduct");
        btnNew && btnNew.addEventListener("click", () => {
            location.href = "/vendor-product-create-pro.html";
        });
    }
    

    // Expose minimal stable globals
    window.VendorNav = {
        initVendorPage,
        fetchJson: vendorFetchJson,
        getToken,
        logout,
        API_BASE,
    };
})();