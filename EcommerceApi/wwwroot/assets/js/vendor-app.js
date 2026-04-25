(() => {
    "use strict";

    const VENDOR_TOKEN_KEY = "ranita_vendor_token";
    const VENDOR_USER_KEY = "ranita_vendor_user";

    const API_BASE = location.origin;
    window.VAPI = API_BASE;

    function setVendorToken(t) { localStorage.setItem(VENDOR_TOKEN_KEY, t); }
    function getVendorToken() { return localStorage.getItem(VENDOR_TOKEN_KEY) || ""; }

    function logoutVendor() {
        localStorage.removeItem(VENDOR_TOKEN_KEY);
        localStorage.removeItem(VENDOR_USER_KEY);
        location.replace("/vendor-login.html");
    }
    // dans vendor-app.js
    function getVendorCachedUser() {
        try { return JSON.parse(localStorage.getItem(VENDOR_USER_KEY) || "null"); }
        catch { return null; }
    }
    window.getVendorCachedUser = getVendorCachedUser;

    function buildUrl(path) {
        if (!path) return API_BASE;
        if (/^https?:\/\//i.test(path)) return path;
        return API_BASE + (path.startsWith("/") ? path : ("/" + path));
    }

    async function fetchVendorJson(url, opt = {}) {
        const t = getVendorToken();
        const headers = new Headers(opt.headers || {});
        if (t) headers.set("Authorization", "Bearer " + t);

        const res = await fetch(buildUrl(url), { ...opt, headers, cache: "no-store" });
        const text = await res.text();

        let data = null;
        try { data = text ? JSON.parse(text) : null; }
        catch { data = { message: text }; }

        if (res.status === 401) throw new Error("Session vendeur expirée. Reconnecte-toi.");
        if (!res.ok) throw new Error(data?.message || "Erreur serveur.");
        if (data && data.ok === false) throw new Error(data.message || "Erreur API.");

        return data;
    }

    // expose
    window.setVendorToken = setVendorToken;
    window.getVendorToken = getVendorToken;
    window.logoutVendor = logoutVendor;
    window.fetchVendorJson = fetchVendorJson;

    // guard minimal
    window.requireVendorAuth = function () {
        const t = getVendorToken();
        if (!t) { logoutVendor(); return false; }
        return true;
    };
})();
