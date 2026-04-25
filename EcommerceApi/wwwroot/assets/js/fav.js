// wwwroot/assets/js/fav.js
(() => {
    "use strict";

    const API = window.API || location.origin;
    const TOKEN_KEY = "ranita_client_token";

    function getToken() {
        return localStorage.getItem(TOKEN_KEY) || "";
    }

    async function request(path, { method = "GET", body } = {}) {
        const t = getToken();
        if (!t) throw new Error("Non connecté.");

        const headers = new Headers();
        headers.set("Authorization", "Bearer " + t);

        if (body != null) headers.set("Content-Type", "application/json");

        const res = await fetch(API + path, {
            method,
            headers,
            body: body != null ? JSON.stringify(body) : undefined,
            cache: "no-store",
        });

        const text = await res.text();
        let data = null;
        try { data = text ? JSON.parse(text) : null; } catch { data = { message: text }; }

        if (!res.ok) throw new Error(data?.message || `HTTP ${res.status}`);
        if (data && data.ok === false) throw new Error(data.message || "Erreur");
        return data;
    }

    // cache mémoire (évite trop d'appels)
    let _ids = null;

    async function fetchIds(force = false) {
        if (_ids && !force) return new Set(_ids);
        const data = await request("/api/client/favorites/ids");
        _ids = Array.isArray(data?.items) ? data.items.map(Number) : [];
        return new Set(_ids);
    }

    async function add(productId) {
        await request(`/api/client/favorites/${Number(productId)}`, { method: "POST" });
        _ids = null; // invalide cache
    }

    async function remove(productId) {
        await request(`/api/client/favorites/${Number(productId)}`, { method: "DELETE" });
        _ids = null; // invalide cache
    }

    // retourne true si favori ON, false si OFF
    async function toggle(productId) {
        const id = Number(productId);
        const set = await fetchIds();
        if (set.has(id)) {
            await remove(id);
            return false;
        } else {
            await add(id);
            return true;
        }
    }

    async function fetchAll() {
        const data = await request("/api/client/favorites");
        return Array.isArray(data?.items) ? data.items : [];
    }

    window.fav = { fetchIds, toggle, remove, add, fetchAll };
})();
