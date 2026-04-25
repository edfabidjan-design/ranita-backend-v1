(() => {
    "use strict";

    const qs = (s, r = document) => r.querySelector(s);
    const API = window.API || location.origin;

    const grid = qs("#favGrid");
    const msg = qs("#favMsg");
    const btnRefresh = qs("#btnRefresh");

    function esc(s) {
        return String(s ?? "").replace(/[&<>"']/g, c => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
        }[c]));
    }

    function fmtFCFA(n) {
        return Number(n || 0).toLocaleString("fr-FR") + " FCFA";
    }

    function absUrl(url) {
        if (!url) return "/assets/img/placeholder.jpg";
        if (String(url).startsWith("http")) return url;
        return url.startsWith("/") ? url : ("/" + url);
    }

    function getClientToken() {
        return localStorage.getItem("ranita_client_token") || "";
    }

    async function fetchAuthJson(url, opt = {}) {
        const t = getClientToken();
        const headers = new Headers(opt.headers || {});
        if (t) headers.set("Authorization", "Bearer " + t);
        headers.set("Content-Type", "application/json");

        const res = await fetch(API + url, { ...opt, headers, cache: "no-store" });
        const text = await res.text();
        let data;
        try { data = text ? JSON.parse(text) : null; } catch { data = { message: text }; }

        if (!res.ok) throw new Error(data?.message || `HTTP ${res.status}`);
        if (data && data.ok === false) throw new Error(data.message || "Erreur");
        return data;
    }

    function effectivePrice(p) {
        const price = Number(p?.price || 0);
        const promo = Number(p?.pricePromo || 0);
        return (promo > 0 && promo < price) ? promo : price;
    }

    function cardHtml(it) {
        const p = it.product || {};
        const price = effectivePrice(p);
        const img = absUrl(p.imageUrl);

        return `
<div class="pCard" style="border:1px solid rgba(255,255,255,.08);border-radius:16px;overflow:hidden;background:rgba(255,255,255,.03)">
  <div style="display:flex;gap:12px;padding:12px;align-items:center">
    <img src="${img}" alt="" style="width:74px;height:74px;border-radius:14px;object-fit:cover;border:1px solid rgba(255,255,255,.10)"
         onerror="this.onerror=null;this.src='/assets/img/placeholder.jpg';"/>
    <div style="flex:1;min-width:0">
      <div style="font-weight:950;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${esc(p.name || "Produit")}</div>
      <div class="muted" style="margin-top:4px">${fmtFCFA(price)}</div>
      ${p.isActive === false ? `<div class="muted" style="color:#fca5a5;margin-top:4px">Produit désactivé</div>` : ``}
    </div>
    <div style="display:flex;flex-direction:column;gap:8px;align-items:flex-end">
      <a class="btn btnGhost" href="/product.html?id=${p.id}">Voir</a>
      <button class="btn btnDanger" data-remove="${p.id}" type="button">Retirer</button>
    </div>
  </div>
</div>`;
    }

    async function load() {
        if (!grid || !msg) return;

        const t = getClientToken();
        if (!t) {
            msg.innerHTML = `Vous devez être connecté. <a class="btn btnGhost" href="/client-login.html">Se connecter</a>`;
            grid.innerHTML = "";
            return;
        }

        msg.textContent = "Chargement…";
        grid.innerHTML = "";

        const data = await fetchAuthJson("/api/client/favorites", { method: "GET" });
        const items = data?.items || [];

        if (!items.length) {
            msg.textContent = "Aucun favori pour le moment.";
            return;
        }

        msg.textContent = "";
        grid.innerHTML = items.map(cardHtml).join("");
    }

    async function remove(productId) {
        await fetchAuthJson(`/api/client/favorites/${productId}`, { method: "DELETE" });
    }

    document.addEventListener("click", async (e) => {
        const btn = e.target.closest("button[data-remove]");
        if (!btn) return;

        const id = Number(btn.getAttribute("data-remove"));
        if (!id) return;

        btn.disabled = true;
        try {
            await remove(id);
            await load();
            if (window.refreshHeaderCart) window.refreshHeaderCart();
        } catch (err) {
            btn.disabled = false;
            alert(err?.message || err);
        }
    });

    document.addEventListener("DOMContentLoaded", () => {
        window.renderSiteHeader?.();
        btnRefresh?.addEventListener("click", load);
        load();
    });
})();
