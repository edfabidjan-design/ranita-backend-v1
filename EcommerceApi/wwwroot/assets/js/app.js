// wwwroot/assets/js/app.js
(() => {
    "use strict";

    // ================================
    // CONSTANTES & GLOBALS
    // ================================
    const TOKEN_KEY = "ranita_admin_token";
    const USER_KEY = "ranita_user";
    // ✅ API_BASE = même domaine que la page (localhost sur PC, ngrok sur mobile)
    const API_BASE = "https://ranita-backend-v1-bzrh.onrender.com";
    window.API_BASE = API_BASE;
    window.API = API_BASE;




    window.FCM_CFG = {
        apiKey: "AIzaSyAEZxh97yKJKY0KMHESGNa5kEUCl7adpvI",
        authDomain: "ranita-e7fee.firebaseapp.com",
        projectId: "ranita-e7fee",
        storageBucket: "ranita-e7fee.firebasestorage.app",
        messagingSenderId: "18866880283",
        appId: "1:18866880283:web:2462a474ef3810c7751dbe"
    };

    // ⚠️ TU DOIS REMPLACER CETTE VALEUR
    const FCM_VAPID_KEY = "BD3AXF-M6bal_TaCm9-CxRvR8TsP2N8kltn_ArGUi7TkhgP1CS4neUOth0PQHXEJTRUwOLFOZklvRAmXywrCULg";

    window.API_BASE = API_BASE;
    window.API = API_BASE;

    window.qs = window.qs || ((s, root = document) => root.querySelector(s));
    window.qsa = window.qsa || ((s, root = document) => Array.from(root.querySelectorAll(s)));

    window.qs = window.qs || ((s, root = document) => root.querySelector(s));
    window.qsa = window.qsa || ((s, root = document) => Array.from(root.querySelectorAll(s)));


    // ================================
    // ADMIN AUTH STATE / PERMISSIONS
    // ================================
    window.AdminAuth = window.AdminAuth || {
        me: null,
        permissions: new Set()
    };

    function normalizeRoleCode(v) {
        const s = String(v || "").trim().toUpperCase();

        if (s === "SUPERADMIN") return "SUPER_ADMIN";
        if (s === "SUPER_ADMIN") return "SUPER_ADMIN";
        if (s === "ADMIN") return "ADMIN";
        if (s === "MANAGER") return "MANAGER";

        return s;
    }

    function getStoredAdminMe() {
        try {
            return JSON.parse(localStorage.getItem("ranita_admin_me") || "null");
        } catch {
            return null;
        }
    }

    function getStoredRoleCode() {
        const fromLs = localStorage.getItem("adminRoleCode");
        if (fromLs) return normalizeRoleCode(fromLs);

        const fromToken = getRoleFromToken();
        if (fromToken) return normalizeRoleCode(fromToken);

        try {
            const u = JSON.parse(localStorage.getItem(USER_KEY) || "null");
            if (u?.roleCode) return normalizeRoleCode(u.roleCode);
            if (u?.role) return normalizeRoleCode(u.role);
        } catch { }

        return "";
    }

    async function loadAdminPermissions(force = false) {
        const token = getToken();
        if (!token) return null;

        if (!force && window.AdminAuth.me && window.AdminAuth.permissions?.size) {
            return window.AdminAuth.me;
        }

        const data = await fetchJson("/api/admin/me/permissions", {
            method: "GET"
        });

        window.AdminAuth.me = data || null;
        window.AdminAuth.permissions = new Set((data?.permissions || []).map(x => String(x)));

        localStorage.setItem("ranita_admin_me", JSON.stringify(data || null));

        if (data?.role?.code) localStorage.setItem("adminRoleCode", data.role.code);
        if (data?.role?.id != null) localStorage.setItem("adminRoleId", String(data.role.id));

        return data;
    }

    function hasPermission(code) {
        code = String(code || "").trim();
        if (!code) return true;

        const roleCode = getStoredRoleCode();
        if (roleCode === "SUPER_ADMIN") return true;

        if (window.AdminAuth.permissions instanceof Set && window.AdminAuth.permissions.size > 0) {
            return window.AdminAuth.permissions.has(code);
        }

        try {
            const cached = JSON.parse(localStorage.getItem("ranita_admin_me") || "null");
            const perms = Array.isArray(cached?.permissions) ? cached.permissions : [];
            return perms.includes(code);
        } catch {
            return false;
        }
    }

    function renderAccessDenied(permission) {
        const main = document.querySelector("main");
        if (!main) {
            alert("Accès refusé.");
            return;
        }

        main.innerHTML = `
        <div class="card" style="max-width:760px;margin:30px auto;">
            <div style="font-size:24px;font-weight:900;margin-bottom:8px;">⛔ Accès refusé</div>
            <div class="muted" style="line-height:1.6">
                Vous n'avez pas la permission nécessaire pour accéder à cette page.
                ${permission ? `<div style="margin-top:8px"><b>Permission requise :</b> ${escapeHtml(permission)}</div>` : ""}
            </div>
        </div>
    `;
    }

    function requirePermission(permission) {
        if (hasPermission(permission)) return true;
        renderAccessDenied(permission);
        return false;
    }

    window.loadAdminPermissions = loadAdminPermissions;
    window.hasPermission = hasPermission;
    window.requirePermission = requirePermission;


    // ================================
    // AUTH / TOKEN
    // ================================
    function setToken(t) { localStorage.setItem(TOKEN_KEY, t); }
    function getToken() { return localStorage.getItem(TOKEN_KEY) || ""; }

    function base64UrlToBase64(s) {
        s = String(s || "").replace(/-/g, "+").replace(/_/g, "/");
        while (s.length % 4) s += "=";
        return s;
    }

    function parseJwt(token) {
        try {
            token = String(token || "").trim();
            const parts = token.split(".");
            if (parts.length !== 3) return null;

            const payloadB64 = base64UrlToBase64(parts[1]);
            if (!/^[A-Za-z0-9+/=]+$/.test(payloadB64)) return null;

            return JSON.parse(atob(payloadB64));
        } catch {
            return null;
        }
    }

    function isTokenExpired(t) {
        const p = parseJwt(t);
        if (!p || !p.exp) return true;
        return Date.now() >= (Number(p.exp) * 1000);
    }

    function getRoleFromToken() {
        const p = parseJwt(getToken());
        if (!p) return "";

        let r =
            p.role ||
            p.roles ||
            p["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
            "";

        if (Array.isArray(r)) r = r[0];
        return String(r || "");
    }
  

    function logout() {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        localStorage.removeItem("adminRole");
        localStorage.removeItem("adminRoleCode");
        localStorage.removeItem("adminRoleId");
        localStorage.removeItem("ranita_admin_me");

        window.AdminAuth = {
            me: null,
            permissions: new Set()
        };

        location.replace("/admin-login.html");
    }
    window.setToken = setToken;
    window.getToken = getToken;
    window.isTokenExpired = isTokenExpired;
    window.logout = logout;

    // ================================
    // FETCH HELPERS
    // ================================
    function buildUrl(path) {
        if (!path) return API_BASE;
        // si déjà absolu => on ne touche pas
        if (/^https?:\/\//i.test(path)) return path;
        // sinon => /api/...
        return API_BASE + (path.startsWith("/") ? path : ("/" + path));
    }

    async function fetchPublicJson(url, opt = {}) {
        const res = await fetch(buildUrl(url), { ...opt, cache: "no-store" });
        const text = await res.text();

        let data = null;
        try { data = text ? JSON.parse(text) : null; }
        catch { data = { message: text }; }

if (!res.ok) {
    throw {
        message: (data && (data.message || data.Message)) || "Erreur serveur.",
        detail: data?.detail,
        debug: data?.debug
    };
}

        if (data && data.ok === false) {
            throw new Error(String(data.message || "Erreur API"));
        }

        return data;
    }


    async function fetchJson(url, opt = {}) {
        const t = getToken();
        const headers = new Headers(opt.headers || {});

        if (t) headers.set("Authorization", "Bearer " + t);

        // ✅ AJOUT CRITIQUE POUR NGROK
        headers.set("ngrok-skip-browser-warning", "true");

        const res = await fetch(buildUrl(url), {
            ...opt,
            headers,
            cache: "no-store"
        });

        const text = await res.text();

        let data = null;
        try {
            data = text ? JSON.parse(text) : null;
        } catch {
            data = { message: text };
        }

        if (res.status === 401) {
            throw {
                message: "Session expirée. Reconnecte-toi.",
                detail: data?.detail,
                debug: data?.debug
            };
        }

        if (!res.ok) {
            throw {
                message: (data && (data.message || data.Message)) || ("HTTP " + res.status),
                detail: data?.detail,
                debug: data?.debug
            };
        }

        if (data && data.ok === false) {
            throw {
                message: String(data.message || "Erreur API"),
                detail: data?.detail,
                debug: data?.debug
            };
        }

        return data;
    }



    window.fetchPublicJson = fetchPublicJson;
    window.fetchJson = fetchJson;


    // ================================
    // ADMIN PAGE TITLE
    // ================================
window.setAdminPageTitle = function (title, icon = "") {
    const h1 = document.getElementById("pageTitle");
    if (!h1) return;

    h1.innerHTML = `
        ${icon ? `<span class="pageTitleIcon">${icon}</span>` : ""}
        <span class="pageTitleText">${title || ""}</span>
    `;

    document.title = `Ranita Admin - ${title || ""}`;
};


    // ================================
    // PUSH (FCM) - ADMIN
    // ================================


    let _toastTimer = null;

    function showAdminToast(title, body) {
        let box = document.getElementById("adminToast");
        if (!box) {
            box = document.createElement("div");
            box.id = "adminToast";
            box.style.cssText =
                "position:fixed;top:14px;right:14px;z-index:99999;" +
                "background:#111827;color:#e5e7eb;border:1px solid #334155;" +
                "padding:12px 14px;border-radius:14px;box-shadow:0 10px 30px rgba(0,0,0,.35);" +
                "max-width:360px;font-family:Arial;display:none;";
            document.body.appendChild(box);
        }

        box.innerHTML = `
    <div style="display:flex;gap:10px;align-items:flex-start;justify-content:space-between">
      <div>
        <b style="display:block;margin-bottom:4px">${title}</b>
        <div>${body}</div>
      </div>
      <button id="toastClose" style="background:transparent;border:0;color:#9ca3af;cursor:pointer;font-size:16px">✖</button>

    
    </div>
  `;

        box.style.display = "block";
        document.getElementById("toastClose").onclick = () => (box.style.display = "none");

        if (_toastTimer) clearTimeout(_toastTimer);
        _toastTimer = setTimeout(() => { box.style.display = "none"; }, 8000);
    }
    let _lastAdminPushKey = localStorage.getItem("ranita_last_admin_push_key") || "";
    let _pushInitDone = false;

    function isFirebaseCfgOk() {
        const bad = v => !v || v === "XXX" || String(v).startsWith("TON_");
        return !bad(FCM_CFG.apiKey)
            && !bad(FCM_CFG.authDomain)
            && !bad(FCM_CFG.projectId)
            && !bad(FCM_CFG.messagingSenderId)
            && !bad(FCM_CFG.appId);
    }

    function isVapidOk(v) {
        v = String(v || "").trim();
        // base64url : lettres/chiffres/_- uniquement
        return v && /^[A-Za-z0-9_-]+$/.test(v);
    }

    function cleanVapidKey(k) {
        // enlève espaces, retours, et caractères invisibles (zéro-width)
        return String(k || "").replace(/[\s\u200B-\u200D\uFEFF]/g, "");
    }

    function urlBase64ToUint8Array(base64String) {
        // base64url -> base64
        const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
        const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
        const rawData = atob(base64);
        const outputArray = new Uint8Array(rawData.length);
        for (let i = 0; i < rawData.length; ++i) outputArray[i] = rawData.charCodeAt(i);
        return outputArray;
    }

    async function initAdminPushOnce() {

        if (typeof firebase === "undefined") {
            console.warn("Firebase non chargé (OK si push non utilisé sur cette page).");
            return;
        }

        try {
            if (_pushInitDone) return;
            _pushInitDone = true;

            const path = (location.pathname || "").toLowerCase();
            if (!path.includes("admin-") || path.includes("admin-login")) return;

            const isLocalhost = (location.hostname === "localhost" || location.hostname === "127.0.0.1");
            if (!isLocalhost && location.protocol !== "https:") {
                console.warn("🔕 Push désactivé (HTTPS requis).");
                return;
            }

            if (!isFirebaseCfgOk()) {
                console.warn("🔕 Firebase config invalide.");
                return;
            }

            const vapid = cleanVapidKey(FCM_VAPID_KEY);

            if (!firebase.apps || !firebase.apps.length) firebase.initializeApp(FCM_CFG);
            const messaging = firebase.messaging();

            // ✅ SW d'abord
            const reg = await navigator.serviceWorker.register("/firebase-messaging-sw.js");
            await reg.update();

            // ✅ Permission (UNE FOIS)
            let perm = Notification.permission;
            if (perm !== "granted") perm = await Notification.requestPermission();
            if (perm !== "granted") {
                console.warn("🔕 Permission notifications refusée.");
                return;
            }

            // ✅ Foreground (site ouvert)
            // ✅ Foreground (site ouvert)
            messaging.onMessage(async (payload) => {
                console.log("📩 FCM foreground:", payload);

                const title = payload?.data?.title || payload?.notification?.title || "Ranita";
                const body = payload?.data?.body || payload?.notification?.body || "Nouvelle commande";

                const pushKey =
                    payload?.data?.type + "|" +
                    (payload?.data?.productId || payload?.data?.orderId || payload?.data?.returnId || "") + "|" +
                    title + "|" + body;

                _lastAdminPushKey = pushKey;
                localStorage.setItem("ranita_last_admin_push_key", pushKey);

                // 1) Toast in-page
                showAdminToast(title, body);



                if (typeof refreshAdminNotifCount === "function") refreshAdminNotifCount();
            });

            // ✅ Token
            const token = await messaging.getToken({
                vapidKey: vapid,
                serviceWorkerRegistration: reg
            });

            console.log("✅ FCM TOKEN =", token);

            if (!token) {
                console.warn("🔕 Aucun token FCM reçu.");
                return;
            }

            await fetchJson("/api/admin/push/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ token, platform: "web", deviceName: navigator.userAgent })
            });

            console.log("✅ Push admin prêt (token enregistré).");
        } catch (e) {
            console.warn("🔕 initAdminPushOnce error:", e?.message || e);
        }
    }
    window.initAdminPushOnce = initAdminPushOnce;





  


    // ================================
    // ADMIN MENU (inchangé)
    // ================================
    

    function getCurrentUserRole() {
        const roleCode = getStoredRoleCode();
        if (roleCode === "SUPER_ADMIN") return "SuperAdmin";
        if (roleCode === "ADMIN") return "Admin";
        if (roleCode === "MANAGER") return "Manager";

        try {
            const u = JSON.parse(localStorage.getItem(USER_KEY) || "null");
            return String(u?.role || "");
        } catch {
            return "";
        }
    }

    function requireAdminAuth() {
        const p = (location.pathname || "").toLowerCase();
        if (p.includes("admin-login")) return true;

        const t = getToken();
        if (!t || isTokenExpired(t)) { logout(); return false; }
        return true;
    }

    window.getCurrentUserRole = getCurrentUserRole;
    window.requireAdminAuth = requireAdminAuth;


    const PAGE_PERMISSIONS = {
        "/admin-dashboard.html": "dashboard.view",
        "/admin-users.html": "users.view",
        "/admin-roles.html": "roles.view",
        "/admin-orders.html": "orders.view",
        "/admin-returns.html": "returns.view",
        "/admin-categories.html": "categories.view",
        "/admin-customers.html": "customers.view",
        "/admin-products.html": "products.view",
        "/admin-products-moderation.html": "products.moderate",
        "/admin-vendors.html": "vendors.view",
        "/admin-vendor-payouts.html": "payouts.view",
        "/admin-commissions.html": "commissions.view",
        "/admin-attributes.html": "attributes.view",

        "/admin-hero-slides.html": "heroslides.view",
        "/admin-home-content.html": "homecms.view",
        "/admin-flash-deals.html": "flashdeals.view"
    };

    function applyRoleMenu() {
        const rules = {
            "admin-dashboard.html": "dashboard.view",
            "admin-users.html": "users.view",
            "admin-roles.html": "roles.view",
            "admin-orders.html": "orders.view",
            "admin-returns.html": "returns.view",
            "admin-categories.html": "categories.view",
            "admin-customers.html": "customers.view",
            "admin-products.html": "products.view",
            "admin-products-moderation.html": "products.moderate",
            "admin-vendors.html": "vendors.view",
            "admin-vendor-payouts.html": "payouts.view",
            "admin-commissions.html": "commissions.view",
            "admin-attributes.html": "attributes.view",

            "admin-hero-slides.html": "heroslides.view",
            "admin-home-content.html": "homecms.view",
            "admin-flash-deals.html": "flashdeals.view"
        };

        qsa(".admin-nav a").forEach(link => {
            const href = (link.getAttribute("href") || "").toLowerCase();

            Object.entries(rules).forEach(([page, perm]) => {
                if (href.includes(page) && !hasPermission(perm)) {
                    link.remove();
                }
            });
        });
    }



    function enforceRoleAccess() {
        const p = (location.pathname || "").toLowerCase();
        const permission = PAGE_PERMISSIONS[p];

        if (!permission) return true;
        if (hasPermission(permission)) return true;

        renderAccessDenied(permission);
        return false;
    }


    function escapeHtml(s) {
        return String(s ?? "").replace(/[&<>"']/g, c => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
        }[c]));
    }

    function getCurrentAdminName() {
        // 1) ranita_user (priorité)
        try {
            const u = JSON.parse(localStorage.getItem(USER_KEY) || "null");
            if (u) {
                return String(
                    u.fullName ||
                    u.nomComplet ||
                    u.name ||
                    u.username ||
                    u.userName ||
                    u.email ||
                    ""
                ).trim();
            }
        } catch { }

        // 2) fallback JWT (claims fréquents)
        const p = parseJwt(getToken());
        if (p) {
            return String(
                p.name ||
                p.unique_name ||
                p.email ||
                p.sub ||
                ""
            ).trim();
        }

        return "";
    }


    function getCurrentAdminRoleLabel() {
        try {
            const me = JSON.parse(localStorage.getItem("ranita_admin_me") || "null");
            const role = me?.role;

            const code = String(
                role?.code ||
                localStorage.getItem("adminRoleCode") ||
                getStoredRoleCode() ||
                ""
            ).trim().toUpperCase();

            const name = String(role?.name || "").trim();

            if (name) return name;
            if (code === "SUPER_ADMIN") return "Super Admin";
            if (code === "ADMIN") return "Admin";
            if (code === "MANAGER") return "Manager";

            return code || "";
        } catch {
            const code = String(getStoredRoleCode() || "").trim().toUpperCase();

            if (code === "SUPER_ADMIN") return "Super Admin";
            if (code === "ADMIN") return "Admin";
            if (code === "MANAGER") return "Manager";

            return code || "";
        }
    }

    async function refreshAdminVendorProductCount() {
        const badge = document.getElementById("adminVendorProductCount");
        if (!badge) return;

        try {
            const r = await fetchJson("/api/admin/products/pending");

            const items = r?.items || [];
            const count = items.length;

            badge.textContent = count;
            badge.style.display = count > 0 ? "inline-flex" : "none";

            if (count > 0) {
                badge.classList.add("pulse");
            } else {
                badge.classList.remove("pulse");
            }

        } catch (e) {
            console.error("vendor product count error", e);
        }
    }

    function renderAdminHeader(active = "") {
        const el = qs("#appHeader");
        if (!el) return;

        const adminName = getCurrentAdminName();
        const adminRoleLabel = getCurrentAdminRoleLabel();

        console.log("✅ HEADER APP.JS VERSION CUSTOMERS OK");

        el.innerHTML = `
      <div class="admin-topbar">
        <div class="admin-topbar-inner">

          <div class="admin-ident">
            <div style="
              width:44px;height:44px;border-radius:999px;
              background:#fff;border:2px solid rgba(34,197,94,.6);
              overflow:hidden;display:flex;align-items:center;justify-content:center;
              flex:0 0 auto;
            ">
              <img
                src="/assets/img/logo-192.png"
                alt="Ranita"
                style="width:75%;height:75%;object-fit:contain;display:block;"
                onerror="this.onerror=null; this.src='/assets/img/placeholder.jpg';"
              />
            </div>

            <div class="admin-who">
              <b>Ranita Admin</b>
              <span>Back-office</span>

              <div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;font-size:12px;opacity:.95">
                <span>👤 ${adminName ? escapeHtml(adminName) : "Connecté"}</span>
                ${adminRoleLabel ? `
                  <span style="
                    padding:3px 8px;
                    border-radius:999px;
                    background:rgba(34,197,94,.14);
                    border:1px solid rgba(34,197,94,.35);
                    color:#86efac;
                    font-weight:700;
                  ">
                    ${escapeHtml(adminRoleLabel)}
                  </span>
                ` : ""}
              </div>
            </div>
          </div>

          <nav class="admin-nav">
            <a class="${active === "dashboard" ? "active" : ""}" href="/admin-dashboard.html">Dashboard</a>
            <a class="${active === "users" ? "active" : ""}" href="/admin-users.html">Utilisateurs</a>
            <a class="${active === "roles" ? "active" : ""}" href="/admin-roles.html">Rôles & permissions</a>

            <a class="${active === "orders" ? "active" : ""}" href="/admin-orders.html">
              Commandes <span id="adminNotifCount" class="notif-badge" style="display:none">0</span>
            </a>

            <a class="${active === "returns" ? "active" : ""}" href="/admin-returns.html">
              Retours <span id="adminReturnCount" class="notif-badge" style="display:none">0</span>
            </a>

            <a class="${active === "categories" ? "active" : ""}" href="/admin-categories.html">Catégories</a>
            <a class="${active === "products" ? "active" : ""}" href="/admin-products.html">Produits</a>
            <a class="${active === "customers" ? "active" : ""}" href="/admin-customers.html">Clients</a>
<a class="${active === "vendorProducts" ? "active" : ""}" href="/admin-products-moderation.html">
  Modération produits <span id="adminVendorProductCount" class="notif-badge notif-badge-vendor" style="display:none">0</span>
</a>
            <a class="${active === "vendors" ? "active" : ""}" href="/admin-vendors.html">Vendeurs</a>

            <a class="${active === "payouts" ? "active" : ""}" href="/admin-vendor-payouts.html">
              Paiements vendeurs <span id="adminPayoutCount" class="notif-badge" style="display:none">0</span>
            </a>

            <a class="${active === "commissions" ? "active" : ""}" href="/admin-commissions.html">Commissions</a>
            <a class="${active === "attributes" ? "active" : ""}" href="/admin-attributes.html">Attributs</a>
<a class="${active === "heroslides" ? "active" : ""}" href="/admin-hero-slides.html">Hero Slider</a>
<a class="${active === "homecms" ? "active" : ""}" href="/admin-home-content.html">Home CMS</a>
<a class="${active === "flashdeals" ? "active" : ""}" href="/admin-flash-deals.html">Flash Deals</a>
          </nav>

          <div class="admin-actions">
            <button id="btnLogout" class="btn btnGhost" type="button">Déconnexion</button>
          </div>

        </div>
      </div>
    `;

        qs("#btnLogout")?.addEventListener("click", logout);

        applyRoleMenu();

        refreshAdminNotifCount();
        refreshAdminPayoutCount();
        refreshAdminVendorProductCount();
        startAdminNotifPolling();
        enableAdminNavScroll();
    }
    window.renderAdminHeader = renderAdminHeader;
    // ================================
    // SHOP HELPERS
    // ================================
    function absUrl(url) {
        if (!url || typeof url !== "string") return "";
        if (url.startsWith("http://") || url.startsWith("https://")) return url;
        const u = url.startsWith("/") ? url : ("/" + url);
        return (window.API_BASE || location.origin) + u;
    }

    function fmtFCFA(n) {
        return Number(n || 0).toLocaleString("fr-FR") + " FCFA";
    }

    window.absUrl = absUrl;
    window.fmtFCFA = fmtFCFA;

    // ================================
    // BOOT UNIQUE (admin only)
    // ================================
    document.addEventListener("DOMContentLoaded", async () => {
        const p = (location.pathname || "").toLowerCase();

        if (p.includes("admin-") && !p.includes("admin-login")) {
            if (!requireAdminAuth()) return;

            try {
                await loadAdminPermissions(true);

                console.log("PERMS =", Array.from(window.AdminAuth.permissions || []));
                console.log("HAS returns.view =", hasPermission("returns.view"));
            } catch (e) {
                console.warn("Permissions admin indisponibles :", e?.message || e);
                logout();
                return;
            }

            if (!enforceRoleAccess()) {
                return;
            }

            if (p.includes("admin-dashboard")) renderAdminHeader("dashboard");
            else if (p.includes("admin-users")) renderAdminHeader("users");
            else if (p.includes("admin-roles")) renderAdminHeader("roles");
            else if (p.includes("admin-orders")) renderAdminHeader("orders");
            else if (p.includes("admin-returns")) renderAdminHeader("returns");
            else if (p.includes("admin-categories")) renderAdminHeader("categories");
            else if (p.includes("admin-products-moderation")) renderAdminHeader("vendorProducts");
            else if (p.includes("admin-products.html") || p.includes("admin-products")) renderAdminHeader("products");
            else if (p.includes("admin-customers")) renderAdminHeader("customers");
            else if (p.includes("admin-vendors")) renderAdminHeader("vendors");
            else if (p.includes("admin-vendor-payouts")) renderAdminHeader("payouts");
            else if (p.includes("admin-commissions")) renderAdminHeader("commissions");
            else if (p.includes("admin-attributes")) renderAdminHeader("attributes");
            else if (p.includes("admin-hero-slides")) renderAdminHeader("heroslides");
            else if (p.includes("admin-home-content")) renderAdminHeader("homecms");
            else if (p.includes("admin-flash-deals")) renderAdminHeader("flashdeals");
            else renderAdminHeader("");

            if (p.includes("admin-dashboard")) window.setAdminPageTitle?.("Dashboard", "📊");
            else if (p.includes("admin-users")) window.setAdminPageTitle?.("Utilisateurs", "👤");
            else if (p.includes("admin-roles")) window.setAdminPageTitle?.("Rôles & permissions", "🛡️");
            else if (p.includes("admin-orders")) window.setAdminPageTitle?.("Commandes", "📦");
            else if (p.includes("admin-returns")) window.setAdminPageTitle?.("Retours clients", "↩️");
            else if (p.includes("admin-categories")) window.setAdminPageTitle?.("Catégories", "📂");
            else if (p.includes("admin-products-moderation")) window.setAdminPageTitle?.("Modération produits", "🏪");
            else if (p.includes("admin-products")) window.setAdminPageTitle?.("Produits", "🛍️");
            else if (p.includes("admin-customers")) window.setAdminPageTitle?.("Clients", "👥");
            else if (p.includes("admin-vendors")) window.setAdminPageTitle?.("Vendeurs", "🧑‍💼");
            else if (p.includes("admin-vendor-payouts")) window.setAdminPageTitle?.("Paiements vendeurs", "💰");
            else if (p.includes("admin-commissions")) window.setAdminPageTitle?.("Commissions", "📈");
            else if (p.includes("admin-attributes")) window.setAdminPageTitle?.("Attributs", "⚙️");
            else if (p.includes("admin-hero-slides")) window.setAdminPageTitle?.("Hero Slider", "🎯");
            else if (p.includes("admin-home-content")) window.setAdminPageTitle?.("Home CMS", "🏠");
            else if (p.includes("admin-flash-deals")) window.setAdminPageTitle?.("Flash Deals", "⚡");

            // 🔥 AJOUTE ÇA ICI
            try {
                await initAdminPushOnce();
            } catch (e) {
                console.warn("Push admin non initialisé :", e?.message || e);
            }
        }

        if (p.endsWith("/admin-users.html") && window.initAdminUsersPage) window.initAdminUsersPage();
        if (p.endsWith("/admin-orders.html") && window.initAdminOrdersPage) window.initAdminOrdersPage();
        if (p.endsWith("/admin-order.html") && window.initAdminOrderPage) window.initAdminOrderPage();
        if (p.endsWith("/admin-categories.html") && window.initAdminCategoriesPage) window.initAdminCategoriesPage();
        if (p.endsWith("/admin-products.html") && window.initAdminProductsPage) window.initAdminProductsPage();
        if (p.endsWith("/admin-products-moderation.html") && window.initAdminProductsModerationPage) window.initAdminProductsModerationPage();
        if (p.endsWith("/admin-vendors.html") && window.initAdminVendorsPage) window.initAdminVendorsPage();
        if (p.endsWith("/admin-attributes.html") && window.initAdminAttributesPage) window.initAdminAttributesPage();
        if (p.endsWith("/admin-vendor-payouts.html") && window.initAdminVendorPayoutsPage) window.initAdminVendorPayoutsPage();
        if (p.endsWith("/admin-roles.html") && window.initAdminRolesPage) window.initAdminRolesPage();
    });

    // =====================
    // CLIENT / SHOP (home, products, product, cart...)
    // =====================
    const p2 = (location.pathname || "").toLowerCase();
    if (!p2.includes("admin-")) {
        window.renderSiteHeader?.();
        window.refreshClientNotifCount?.();
        window.startClientNotifPolling?.();
        window.startClientNotifRealtime?.();
    }


    function countWaitingOrders(list) {
        const norm = s => String(s || "").trim().toLowerCase().replace(/\s+/g, "");
        return (list || []).filter(o => {
            const st = norm(o.status ?? o.statut ?? o.Status ?? o.Statut ?? "");
            // EnAttente / En attente
            return st === "enattente" || st.includes("attente");
        }).length;
    }


    let _lastReturnCount = null;

    async function refreshAdminReturnCount() {
        const badge = qs("#adminReturnCount");
        if (!badge) return;

        try {
            const data = await fetchJson("/api/admin/returns/pending-count?ts=" + Date.now());
            const n = Number(data?.count || 0);

            badge.textContent = String(n);
            badge.style.display = n > 0 ? "inline-flex" : "none";

            if (_lastReturnCount !== null && n > _lastReturnCount) {
                showAdminToast("↩️ Nouveau retour", `Demande de remboursement (${n}).`);
            }
            _lastReturnCount = n;

        } catch {
            badge.style.display = "none";
        }
    }
    window.refreshAdminReturnCount = refreshAdminReturnCount;



    let _lastAdminNotifId = Number(localStorage.getItem("ranita_last_admin_notif") || "0");

    async function refreshAdminLatestNotif() {
        try {
            const data = await fetchJson("/api/admin/notifications/latest-unread?ts=" + Date.now());
            const item = data?.item;
            if (!item) return;

            const id = Number(item.id || 0);
            if (!id || id <= _lastAdminNotifId) return;

            const pollKey =
                String(item.type || "") + "|" +
                String(item.refId || "") + "|" +
                String(item.title || "") + "|" +
                String(item.message || "");

            // ✅ si on vient déjà de recevoir ce même push, on ne le réaffiche pas
            if (_lastAdminPushKey && pollKey === _lastAdminPushKey) {
                _lastAdminNotifId = id;
                localStorage.setItem("ranita_last_admin_notif", String(id));
                await fetchJson(`/api/admin/notifications/${id}/read`, { method: "POST" });
                return;
            }

            showAdminToast(item.title || "Notification", item.message || "");

            _lastAdminNotifId = id;
            localStorage.setItem("ranita_last_admin_notif", String(id));

            await fetchJson(`/api/admin/notifications/${id}/read`, { method: "POST" });

        } catch (e) {
            // silence
        }
    }
    window.refreshAdminLatestNotif = refreshAdminLatestNotif;


    async function refreshAdminPayoutCount() {
        const badge = qs("#adminPayoutCount");
        if (!badge) return;

        try {
            const data = await fetchJson("/api/admin/vendor-payouts?mode=payable&ts=" + Date.now(), { cache: "no-store" });
            const n = (data.items || []).length;

            badge.textContent = String(n);
            badge.style.display = n > 0 ? "inline-flex" : "none";
        } catch (e) {
            badge.style.display = "none";
        }
    }

    async function refreshAdminNotifCount() {
        const badge = qs("#adminNotifCount");
        if (!badge) return;

        try {
            const data = await fetchJson("/api/admin/orders?ts=" + Date.now(), { cache: "no-store" });

            // ✅ ton API: { ok:true, items:[...] }
            const orders = Array.isArray(data?.items) ? data.items
                : Array.isArray(data?.orders) ? data.orders
                    : Array.isArray(data) ? data
                        : [];

            const n = countWaitingOrders(orders);

            badge.textContent = String(n);
            badge.style.display = n > 0 ? "inline-flex" : "none";

            // ✅ DEBUG (enlève après)
            console.log("✅ orders:", orders.length, " | waiting:", n, " | sampleStatus:", orders[0]?.status);

        } catch (e) {
            console.warn("❌ refreshAdminNotifCount:", e?.message || e);
            badge.style.display = "none";
        }
    }

    function startAdminNotifPolling() {
        if (window.__adminNotifPollingStarted) return;
        window.__adminNotifPollingStarted = true;

        refreshAdminNotifCount();
        setInterval(refreshAdminNotifCount, 5000);

        refreshAdminPayoutCount();
        setInterval(refreshAdminPayoutCount, 5000);

        refreshAdminReturnCount();
        setInterval(refreshAdminReturnCount, 5000);

        refreshAdminLatestNotif();
        setInterval(refreshAdminLatestNotif, 5000);

        refreshAdminVendorProductCount();
        setInterval(refreshAdminVendorProductCount, 5000);
    }


    // ✅ essaie plusieurs URLs et garde la 1ère qui charge
    // ✅ essaie plusieurs URLs et garde la 1ère qui charge
    window.pickFirstImageUrl = function (urls, timeoutMs = 2500) {
        return new Promise((resolve) => {
            const list = (urls || []).filter(Boolean);
            if (!list.length) return resolve("");

            let done = false;
            let i = 0;

            const tryOne = () => {
                if (done) return;
                if (i >= list.length) return resolve("");

                const src = list[i++];
                const img = new Image();

                const t = setTimeout(() => {
                    img.onload = img.onerror = null;
                    tryOne();
                }, timeoutMs);

                img.onload = () => {
                    clearTimeout(t);
                    done = true;
                    resolve(src);
                };
                img.onerror = () => {
                    clearTimeout(t);
                    tryOne();
                };

                img.src = src + (src.includes("?") ? "&" : "?") + "v=" + Date.now();
            };

            tryOne();
        });
    };

    // ✅ FAVORIS (global)
    window.fav = window.fav || {};

    window.fav.fetchIds = async function () {
        const t = localStorage.getItem("ranita_client_token") || "";
        if (!t) return new Set();

        const res = await fetch((window.API || location.origin) + "/api/client/favorites/ids", {
            headers: { "Authorization": "Bearer " + t },
            cache: "no-store"
        });

        const data = await res.json();
        const ids = data?.items || [];
        return new Set(ids.map(Number));
    };

    window.fav.toggle = async function (productId, makeFav) {
        const t = localStorage.getItem("ranita_client_token") || "";
        if (!t) throw new Error("Connectez-vous pour ajouter aux favoris.");

        const method = makeFav ? "POST" : "DELETE";
        const res = await fetch((window.API || location.origin) + `/api/client/favorites/${productId}`, {
            method,
            headers: { "Authorization": "Bearer " + t }
        });

        const data = await res.json().catch(() => null);
        if (!res.ok) throw new Error(data?.message || "Erreur favoris.");
        return data;
    };

    // =========================
    // CLIENT NOTIFS (shop)
    // =========================
    window.setNotifCount = function (unread, totalItems) {
        const btn = document.getElementById("clientNotifBtn");
        const badge = document.getElementById("clientNotifCount");
        if (!btn || !badge) return;

        const u = Number(unread || 0);
        const total = Number(totalItems || 0);

        // ✅ bouton visible seulement si il y a AU MOINS 1 notif
        btn.style.display = total > 0 ? "inline-flex" : "none";

        badge.textContent = String(u);
        badge.style.display = u > 0 ? "inline-block" : "none";
    };

    window.refreshClientNotifCount = async function () {
        const btn = document.getElementById("clientNotifBtn");
        const badge = document.getElementById("clientNotifCount");
        if (!btn || !badge) return;

        const t = localStorage.getItem("ranita_client_token") || "";
        if (!t) {
            btn.style.display = "none";
            badge.style.display = "none";
            return;
        }

        try {
            const res = await fetch(location.origin + "/api/client/notifications?ts=" + Date.now(), {
                headers: { Authorization: "Bearer " + t },
                cache: "no-store"
            });

            if (!res.ok) {
                btn.style.display = "none";
                badge.style.display = "none";
                return;
            }

            const data = await res.json();
            const items = Array.isArray(data.items) ? data.items : [];
            const unread = (typeof data.unreadCount === "number")
                ? data.unreadCount
                : items.filter(x => !x.isRead).length;

            window.setNotifCount(unread, items.length);

        } catch {
            // en cas d'erreur réseau on cache
            btn.style.display = "none";
            badge.style.display = "none";
        }
    };

    window.startClientNotifPolling = function () {
        if (window.__clientNotifPollingStarted) return;
        window.__clientNotifPollingStarted = true;

        window.refreshClientNotifCount?.();
        setInterval(() => window.refreshClientNotifCount?.(), 20000);
    };



function initScrollTopButton() {
    let btn = document.getElementById("scrollTopBtn");

    if (!btn) {
        btn = document.createElement("button");
        btn.id = "scrollTopBtn";
        btn.type = "button";
        btn.innerHTML = "↑";

        btn.style.position = "fixed";
        btn.style.width = "52px";
        btn.style.height = "52px";
        btn.style.borderRadius = "50%";
        btn.style.border = "none";
        btn.style.background = "#111827";
        btn.style.color = "#ffffff";
        btn.style.fontSize = "26px";
        btn.style.fontWeight = "900";
        btn.style.display = "none";
        btn.style.alignItems = "center";
        btn.style.justifyContent = "center";
        btn.style.cursor = "pointer";
        btn.style.boxShadow = "0 10px 25px rgba(0,0,0,.35)";
        btn.style.zIndex = "999999";

        btn.addEventListener("click", () => {
            window.scrollTo({ top: 0, behavior: "smooth" });
            document.body.scrollTo({ top: 0, behavior: "smooth" });
            document.documentElement.scrollTo({ top: 0, behavior: "smooth" });
        });

        document.body.appendChild(btn);
    }

    function updateScrollTopPosition() {
        if (window.innerWidth <= 600) {
            btn.style.right = "12px";
            btn.style.bottom = "16px";
            btn.style.width = "52px";
            btn.style.height = "52px";
            btn.style.fontSize = "24px";
        } else {
            btn.style.right = "20px";
            btn.style.bottom = "20px";
            btn.style.width = "52px";
            btn.style.height = "52px";
            btn.style.fontSize = "26px";
        }
    }

    function getScrollTop() {
        return Math.max(
            window.pageYOffset || 0,
            document.documentElement.scrollTop || 0,
            document.body.scrollTop || 0
        );
    }

    function toggleScrollTopButton() {
        const y = getScrollTop();
        btn.style.display = y > 80 ? "flex" : "none";
    }

    updateScrollTopPosition();

    if (!window.__scrollTopBound) {
        window.__scrollTopBound = true;
        window.addEventListener("scroll", toggleScrollTopButton, { passive: true });
        document.addEventListener("scroll", toggleScrollTopButton, { passive: true, capture: true });
        window.addEventListener("resize", () => {
            updateScrollTopPosition();
            toggleScrollTopButton();
        });
    }

    setTimeout(() => {
        updateScrollTopPosition();
        toggleScrollTopButton();
    }, 50);

    setTimeout(() => {
        updateScrollTopPosition();
        toggleScrollTopButton();
    }, 300);

    setTimeout(() => {
        updateScrollTopPosition();
        toggleScrollTopButton();
    }, 800);
}

document.addEventListener("DOMContentLoaded", initScrollTopButton);


    function enableAdminNavScroll() {
        const nav = document.querySelector(".admin-nav");
        if (!nav) return;

        let isDown = false;
        let startX;
        let scrollLeft;

        nav.addEventListener("mousedown", (e) => {
            isDown = true;
            startX = e.pageX - nav.offsetLeft;
            scrollLeft = nav.scrollLeft;
            nav.style.cursor = "grabbing";
        });

        nav.addEventListener("mouseleave", () => {
            isDown = false;
            nav.style.cursor = "grab";
        });

        nav.addEventListener("mouseup", () => {
            isDown = false;
            nav.style.cursor = "grab";
        });

        nav.addEventListener("mousemove", (e) => {
            if (!isDown) return;
            e.preventDefault();
            const x = e.pageX - nav.offsetLeft;
            const walk = (x - startX) * 2;
            nav.scrollLeft = scrollLeft - walk;
        });

        nav.style.cursor = "grab";
    }



    // ================= FILTRE HOME =================

    setTimeout(() => {

        const btn = document.getElementById("filterBtnHeader");
        const panel = document.getElementById("filterPanel");

        if (!btn || !panel) return;

        btn.onclick = () => {
            panel.classList.add("open");
        };

        document.getElementById("closeFilter").onclick = () => {
            panel.classList.remove("open");
        };

    }, 500);


})();