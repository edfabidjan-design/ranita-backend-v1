(() => {
    "use strict";

    const CLIENT_TOKEN_KEY = "ranita_client_token";
    const CLIENT_KEY = "ranita_client_user";

    // =========================
    // ✅ CLIENT AUTH (REGISTER / LOGIN) — DOIT ÊTRE EN HAUT
    // =========================
    async function _postJson(url, body) {
        const r = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            cache: "no-store",
            body: JSON.stringify(body || {})
        });

        const text = await r.text();
        let json = null;
        try { json = text ? JSON.parse(text) : null; } catch { json = null; }

        if (!r.ok) {
            const msg = json?.message || ("HTTP " + r.status);
            throw new Error(msg);
        }
        return json;
    }

    window.clientRegister = async function ({ fullName, phone, email, password }) {
        const data = await _postJson("/api/auth/client/register", { fullName, phone, email, password });
        if (data?.token) localStorage.setItem(CLIENT_TOKEN_KEY, data.token);
        if (data?.user) localStorage.setItem(CLIENT_KEY, JSON.stringify(data.user));
        return data;
    };

    window.clientLogin = async function ({ login, password }) {
        const data = await _postJson("/api/auth/client/login", { login, password });
        if (data?.token) localStorage.setItem(CLIENT_TOKEN_KEY, data.token);
        if (data?.user) localStorage.setItem(CLIENT_KEY, JSON.stringify(data.user));
        return data;
    };

    // ✅ (optionnel) preuve que le fichier a été exécuté jusqu’ici
    console.log("✅ client-auth.js OK — clientRegister:", typeof window.clientRegister);







    const ICON_USER = `
<svg class="acc-ico-svg" viewBox="0 0 24 24" aria-hidden="true" fill="currentColor">
  <path d="M12 12a4 4 0 1 0-4-4 4 4 0 0 0 4 4Zm0 2c-4.42 0-8 2-8 4.5V21h16v-2.5C20 16 16.42 14 12 14Z"/>
</svg>`;

    const ICON_HEART = `
<svg class="acc-ico-svg" viewBox="0 0 24 24" aria-hidden="true" fill="currentColor">
  <path d="M12 21s-7-4.35-10-9.5C.6 8.4 2.3 5 6 5c2 0 3.4 1.2 4 2 .6-.8 2-2 4-2 3.7 0 5.4 3.4 4 6.5C19 16.65 12 21 12 21Z"/>
</svg>`;

    const ICON_CART = `
<svg class="acc-ico-svg" viewBox="0 0 24 24" aria-hidden="true" fill="currentColor">
  <path d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2Zm10 0c-1.1 0-1.99.9-1.99 2S15.9 22 17 22s2-.9 2-2-.9-2-2-2ZM7.2 14h9.9c.75 0 1.4-.41 1.74-1.03L21 6H6.21L5.27 4H2v2h2l3.6 7.59-1.35 2.44C5.52 17.37 6.48 19 8 19h12v-2H8l1.2-3Z"/>
</svg>`;

    const ICON_LOGOUT = `
<svg class="acc-ico-svg" viewBox="0 0 24 24" aria-hidden="true" fill="currentColor">
  <path d="M10 17v-2h4v-6h-4V7l-5 5 5 5Zm9-14H12v2h7v14h-7v2h7c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2Z"/>
</svg>`;

    const qs = (s, root = document) => root.querySelector(s);

    function getClientToken() { return localStorage.getItem(CLIENT_TOKEN_KEY) || ""; }
    function getClientUser() {
        try { return JSON.parse(localStorage.getItem(CLIENT_KEY) || "null"); }
        catch { return null; }
    }

    window.logoutClient = function () {
        localStorage.removeItem(CLIENT_TOKEN_KEY);
        localStorage.removeItem(CLIENT_KEY);
        location.href = "/client-login.html";
    };

    function refreshHeaderCart() {
        try {
            const t = (typeof window.cartTotals === "function") ? window.cartTotals() : { count: 0 };
            const el = document.getElementById("cartCount");
            if (el) el.textContent = t.count || 0;
        } catch { }
    }
    window.refreshHeaderCart = refreshHeaderCart;

    // =========================
    // HEADER CLIENT
    // =========================
    window.renderSiteHeader = function () {
        const host = qs("#siteHeader");
        if (!host) return;

        host.innerHTML = `
  <div class="rhdr">
    <div class="wrap">
      <div class="row">

        <div class="left">
          <a href="/home.html" class="brandLink">
           <span class="site-logo">
    <img src="/assets/logo-192.png" alt="Ranita">
</span>
            <div>
              <div class="brandTitle">Ranita</div>
              <div class="brandSub">Boutique en ligne</div>
            </div>
          </a>
        </div>

        <div class="right" id="clientArea"></div>

        <div class="mobileActions">
          <div class="mega-wrap" id="megaWrap">
            <button class="mega-mobile-btn" id="btnMegaCatsMobile">☰</button>

            <div class="mega-mobile-overlay" id="megaMobileOverlay"></div>

            <div class="mega-mobile-panel" id="megaMobilePanel" aria-hidden="true">
              <div class="mega-mobile-head">
                <button type="button" id="megaMobileBack" class="mega-mobile-back" style="display:none">← Retour</button>
                <div class="mega-mobile-title" id="megaMobileTitle">Catégories</div>
                <button type="button" id="megaMobileClose" class="mega-mobile-close">✕</button>
              </div>

              <div class="mega-mobile-body" id="megaMobileBody"></div>
            </div>

            <button class="mega-btn" id="btnMegaCats" type="button" aria-label="Catégories">☰</button>
            <div class="mega-overlay" id="megaOverlay"></div>

            <div class="mega" id="megaMenu" aria-hidden="true">
              <div class="mega-left" id="megaLeft"></div>

              <div class="mega-right">
                <div class="mega-title" id="megaTitle">Catégories</div>

                <div class="mega-grid mega-grid-3">
                  <div class="mega-col" id="megaL2"></div>
                  <div class="mega-col" id="megaL3"></div>
                  <div class="mega-col" id="megaL4"></div>
                </div>
              </div>
            </div>
          </div>

          <a class="btn btnGhost" href="/products.html">🛍️ Catalogue</a>

          <button id="filterBtnHeader" class="btn btnGhost" type="button">
            🔎 Filtre
          </button>

          <a class="btn btnGhost" href="/cart.html">
            🛒 Panier <span class="pill" style="padding:4px 10px"><span id="cartCount">0</span></span>
          </a>
        </div>

        <a id="clientNotifBtn" class="btn btnGhost notifBtn" href="/client-notifications.html" style="display:none">
          <span class="notifIco">🔔</span>
          <span class="notifTxt">Notif</span>
          <span id="clientNotifCount" class="notifDot"></span>
        </a>

      </div>
    </div>
  </div>
`;

        renderClientArea();
      
        refreshHeaderCart();
        window.refreshClientNotifCount?.();
        window.startClientNotifPolling?.();
        window.startClientNotifRealtime?.();

        if (typeof window.initMegaMenu === "function") window.initMegaMenu();
    };

    function renderClientArea() {
        const box = qs("#clientArea");
        if (!box) return;

        const token = getClientToken();
        const u = getClientUser();
        const returnUrl = encodeURIComponent(location.pathname + location.search);

        const CLIENT_ACCOUNT = "/client-account.html";

        // ✅ Non connecté
        if (!token || !u) {
            box.innerHTML = `
<a class="acc-btn acc-main" href="/client-login.html?returnUrl=${returnUrl}" style="text-decoration:none">
  <span class="acc-ico">${ICON_USER}</span>
  <span class="acc-name">Se connecter</span>
  <span class="acc-caret">→</span>
</a>`;
            return;
        }

        const name = String(u.fullName || u.name || "Client").trim();

        const isMobile = window.innerWidth <= 900;

        // ✅ MOBILE : un seul bouton
        if (isMobile) {
            box.innerHTML = `
<a class="acc-btn acc-main" href="${CLIENT_ACCOUNT}" title="${name}" style="text-decoration:none">
  <span class="acc-ico">${ICON_USER}</span>
  <span class="acc-name">${name}</span>
  <span class="acc-caret">→</span>
</a>`;
            return;
        }

        // ✅ DESKTOP : nom + déconnexion
        box.innerHTML = `
<div class="acc-inline">
  <a class="acc-btn acc-main" href="${CLIENT_ACCOUNT}" title="${name}" style="text-decoration:none">
    <span class="acc-ico">${ICON_USER}</span>
    <span class="acc-name">${name}</span>
    <span class="acc-caret">→</span>
  </a>
</div>`;

   
    }

    // =========================
    // DROPDOWN BÉTON (event delegation)
    // =========================

  

    window.flashNotifBtn = function () {
        const btn = document.getElementById("clientNotifBtn");
        if (!btn) return;

        btn.classList.add("attn");
        clearTimeout(window.__notifFlashTimer);
        window.__notifFlashTimer = setTimeout(() => btn.classList.remove("attn"), 6000);
    };




    window.startClientNotifRealtime = function () {
        if (window.__clientNotifRealtimeStarted) return;
        window.__clientNotifRealtimeStarted = true;

        const t = localStorage.getItem("ranita_client_token") || "";
        if (!t || !window.signalR) return;

        window.__clientNotifConn = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/client-notifs", { accessTokenFactory: () => t })
            .withAutomaticReconnect()
            .build();

        window.__clientNotifConn.on("notif:new", async (payload) => {
            console.log("✅ NOTIF REÇUE realtime:", payload);

            // ✅ Ne force PAS l’affichage
            await window.refreshClientNotifCount?.();
            window.flashNotifBtn?.();

            if (location.pathname.endsWith("/client-notifications.html")) {
                await window.fetchNotif?.();
            }
        });

        window.__clientNotifConn.start()
            .then(() => console.log("✅ SignalR connected"))
            .catch(err => console.error("❌ SignalR start error:", err));
    };



    // ✅ Affichage bouton + badge (CLIENT)
    window.setNotifCount = function (unread, totalItems) {
        const btn = document.getElementById("clientNotifBtn");
        const badge = document.getElementById("clientNotifCount");
        if (!btn || !badge) return;

        const u = Number(unread || 0);

        // ✅ bouton visible UNIQUEMENT si unread > 0
        btn.style.display = u > 0 ? "inline-flex" : "none";

        // badge visible si unread > 0
        badge.textContent = String(u);
        badge.style.display = u > 0 ? "inline-block" : "none";
    };

    // ✅ Refresh depuis l’API
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
            const res = await fetch("/api/client/notifications?ts=" + Date.now(), {
                headers: { Authorization: "Bearer " + t },
                cache: "no-store"
            });

            if (res.status === 401) {
                localStorage.removeItem("ranita_client_token");
                localStorage.removeItem("ranita_client_user");
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
        } catch (e) {
            // réseau KO => on cache juste le badge
            badge.style.display = "none";
        }
    };

    window.startClientNotifPolling = function () {
        if (window.__clientNotifPollingStarted) return;
        window.__clientNotifPollingStarted = true;

        window.refreshClientNotifCount?.();
        setInterval(() => window.refreshClientNotifCount?.(), 20000);
    };
    function bindDropdownOnce() {
        console.log("bindDropdownOnce OK");

        if (window.__accDropBound) return;
        window.__accDropBound = true;

        document.addEventListener("pointerdown", (e) => {
            console.log("CLICK MENU", e.target);

            const btn = e.target.closest(".acc-btn");
            if (btn) {
                console.log("ACC BTN CLICK");
                e.preventDefault();
                e.stopPropagation();

                const wrap = btn.closest(".acc-wrap");
                if (!wrap) return;

                document.querySelectorAll(".acc-wrap.open").forEach(w => {
                    if (w !== wrap) w.classList.remove("open");
                });

                wrap.classList.toggle("open");
                console.log("OPEN =", wrap.classList.contains("open"));
                return;
            }

            if (e.target.closest(".acc-menu a.acc-item")) {
                e.target.closest(".acc-wrap")?.classList.remove("open");
                return;
            }

            document.querySelectorAll(".acc-wrap.open").forEach(w => w.classList.remove("open"));
        });

        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape") {
                document.querySelectorAll(".acc-wrap.open").forEach(w => w.classList.remove("open"));
            }
        });
    }


    window.initMegaMenu = function () {
        if (window.__megaDelegBound) return;
        window.__megaDelegBound = true;

        function getEls() {
            return {
                wrap: document.getElementById("megaWrap"),
                btn: document.getElementById("btnMegaCats"),
                overlay: document.getElementById("megaOverlay"),
                menu: document.getElementById("megaMenu"),

                left: document.getElementById("megaLeft"),
                title: document.getElementById("megaTitle"),

                l2: document.getElementById("megaL2"),
                l3: document.getElementById("megaL3"),
                l4: document.getElementById("megaL4"),

                mobileBtn: document.getElementById("btnMegaCatsMobile"),
                mobileOverlay: document.getElementById("megaMobileOverlay"),
                mobilePanel: document.getElementById("megaMobilePanel"),
                mobileBack: document.getElementById("megaMobileBack"),
                mobileClose: document.getElementById("megaMobileClose"),
                mobileTitle: document.getElementById("megaMobileTitle"),
                mobileBody: document.getElementById("megaMobileBody"),
            };
        }

        function open() {
            const { overlay, menu } = getEls();
            if (!overlay || !menu) return;
            overlay.classList.add("open");
            menu.classList.add("open");
        }

        function close() {
            const { overlay, menu } = getEls();
            if (!overlay || !menu) return;
            overlay.classList.remove("open");
            menu.classList.remove("open");
        }

        function openMobile() {
            const { mobileOverlay, mobilePanel } = getEls();
            if (!mobileOverlay || !mobilePanel) return;
            mobileOverlay.classList.add("open");
            mobilePanel.classList.add("open");
            document.body.classList.add("menu-open");
        }

        function closeMobile() {
            const { mobileOverlay, mobilePanel } = getEls();
            if (!mobileOverlay || !mobilePanel) return;
            mobileOverlay.classList.remove("open");
            mobilePanel.classList.remove("open");
            document.body.classList.remove("menu-open");
        }

        let mobileStack = [];

        function renderMobileList(items, title, level = 0) {
            const { mobileTitle, mobileBody, mobileBack } = getEls();
            if (!mobileTitle || !mobileBody || !mobileBack) return;

            mobileTitle.textContent = title || "Catégories";
            mobileBack.style.display = level > 0 ? "inline-flex" : "none";

            mobileBody.innerHTML = (items || []).map(item => `
        <button
            type="button"
            class="mega-mobile-item"
            data-id="${item.id}"
            data-slug="${item.slug || ""}"
            data-name="${escapeHtml(item.name || "")}">
            <span>${escapeHtml(item.name || "")}</span>
            <span class="mega-mobile-arrow">›</span>
        </button>
    `).join("");
        }


        async function loadMegaMobileRoots() {
            await loadMegaRootsOnce();

            const all = window.__megaAll || [];
            const idx = buildIndex(all);

            const roots = all.filter(c => {
                const pid = String(c.parentId ?? "").trim().toLowerCase();
                return pid === "" || pid === "0" || pid === "null";
            });

            mobileStack = [{
                title: "Catégories",
                items: roots,
                parentId: 0,
                path: ""
            }];


            renderMobileList(roots, "Catégories", 0);

            const { mobileBody, mobileBack } = getEls();
            if (!mobileBody) return;

            if (!mobileBody.dataset.bound) {
                mobileBody.dataset.bound = "1";

                mobileBody.addEventListener("click", (e) => {
                    const btn = e.target.closest(".mega-mobile-item");
                    if (!btn) return;

                    const id = btn.dataset.id || "";
                    const slug = btn.dataset.slug || "";
                    const name = btn.dataset.name || "";

                    const children = getChildrenSafe(id);
                    const current = mobileStack[mobileStack.length - 1];

                    const nextPath = current?.path
                        ? `${current.path}/${slug}`
                        : slug;

                    if (children.length > 0) {
                        mobileStack.push({
                            title: name,
                            items: children,
                            parentId: id,
                            path: nextPath
                        });

                        renderMobileList(children, name, mobileStack.length - 1);
                    } else {
                        if (nextPath) {
                            location.href = "/c/" + nextPath;
                        }
                    }
                });
            }

            if (mobileBack && !mobileBack.dataset.bound) {
                mobileBack.dataset.bound = "1";

                mobileBack.addEventListener("click", () => {
                    if (mobileStack.length <= 1) return;

                    mobileStack.pop();
                    const prev = mobileStack[mobileStack.length - 1];
                    renderMobileList(prev.items, prev.title, mobileStack.length - 1);
                });
            }
        }

        async function apiGetCategoriesSafe() {
            if (typeof window.apiGetCategories === "function") return await window.apiGetCategories();
            const res = await fetch("/api/categories", { cache: "no-store" }); // endpoint PUBLIC
            const json = await res.json();
            if (!res.ok) throw new Error("api/categories => " + res.status);
            return json;
        }

        function pick(obj, ...keys) {
            for (const k of keys) if (obj && obj[k] !== undefined && obj[k] !== null) return obj[k];
            return null;
        }

        function toBool(v) {
            if (v == null) return true;
            if (typeof v === "boolean") return v;
            if (typeof v === "number") return v === 1;
            return String(v).toLowerCase() === "true";
        }

        function escapeHtml(s) {
            return String(s || "").replace(/[&<>"']/g, (m) => ({
                "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;"
            }[m]));
        }

        function sortByName(a, b) {
            return String(a.name || "").localeCompare(String(b.name || ""), "fr");
        }

        function buildIndex(items) {
            const map = new Map();

            for (const c of items) {
                const pid = String(c.parentId ?? "").trim();

                if (!map.has(pid)) map.set(pid, []);
                map.get(pid).push(c);
            }

            for (const arr of map.values()) {
                arr.sort(sortByName);
            }

            return map;
        }

        function getChildrenSafe(parentId) {
            const pid = String(parentId ?? "").trim();

            return (window.__megaAll || [])
                .filter(c => String(c.parentId ?? "").trim() === pid)
                .sort(sortByName);
        }

        function splitIntoCols(blocks, colCount = 3) {
            const cols = Array.from({ length: colCount }, () => []);
            const weights = Array.from({ length: colCount }, () => 0);

            for (const b of blocks) {
                const i = weights.indexOf(Math.min(...weights));
                cols[i].push(b);
                weights[i] += b.weight;
            }
            return cols;
        }

        function blockHtml(rootSlug, cat2, lv3, idx) {
            const head = `<div class="mega-h">${escapeHtml(cat2.name)}</div>`;

            // si pas de lv3 => lien direct
            if (!lv3.length) {
                return `
<div class="mega-block">
  ${head}
 <a class="mega-link" href="/c/${encodeURIComponent(rootSlug)}/${encodeURIComponent(cat2.slug)}">
  ${escapeHtml(cat2.name)}
</a>

</div>`;
            }

            // lv3 + lv4
            const body = lv3.map(c3 => {
                const lv4 = idx.get(String(c3.id)) || [];

                // si c3 a des enfants (lv4) => on affiche un sous-titre + ses liens
                if (lv4.length) {
                    const lv4Links = lv4.map(c4 => `
<a class="mega-link mega-lv4"
   href="/c/${encodeURIComponent(rootSlug)}/${encodeURIComponent(cat2.slug)}/${encodeURIComponent(c3.slug)}/${encodeURIComponent(c4.slug)}">
  ${escapeHtml(c4.name)}
</a>`).join("");

                    return `
<div class="mega-subh">${escapeHtml(c3.name)}</div>
${lv4Links}`;
                }

                // sinon lien normal lv3
                return `
<a class="mega-link"
   href="/c/${encodeURIComponent(rootSlug)}/${encodeURIComponent(cat2.slug)}/${encodeURIComponent(c3.slug)}">
  ${escapeHtml(c3.name)}
</a>`;
            }).join("");

            return `<div class="mega-block">${head}${body}</div>`;
        }


        async function loadMegaRootsOnce() {
            if (window.__megaLoaded) return;
            window.__megaLoaded = true;

            const { left, l2, l3, l4, title } = getEls();
            if (!left || !l2 || !l3 || !l4 || !title) return;

            const cjson = await apiGetCategoriesSafe();
            const categories = Array.isArray(cjson) ? cjson : (cjson.items || cjson.data?.items || cjson.data || []);

            const normalized = categories.map(c => ({
                id: pick(c, "id", "Id"),
                name: pick(c, "name", "Name") || "",
                slug: pick(c, "slug", "Slug") || "",
                parentId: pick(c, "parentId", "ParentId", "parentKey", "ParentKey"),
                isActive: toBool(pick(c, "isActive", "IsActive")),
            }));

            window.__megaAll = normalized.filter(c => c.isActive && c.id != null);

            const roots = window.__megaAll.filter(c => {
                const pid = String(c.parentId ?? "").trim().toLowerCase();
                return pid === "" || pid === "0" || pid === "null";
            });

            if (!roots.length) {
                left.innerHTML = `<div style="padding:12px;color:#9ca3af">Aucune catégorie.</div>`;
                l2.innerHTML = ""; l3.innerHTML = ""; l4.innerHTML = "";
                return;
            }

            left.innerHTML = roots.map(r => `
<button class="mega-item" data-id="${r.id}" data-slug="${r.slug}" data-name="${r.name}">
  ${escapeHtml(r.name)}
</button>`).join("");

            const idx = buildIndex(window.__megaAll);

            async function renderRightJumia(root) {
                title.textContent = String(root.name || "Catégories");


                const lv2 = idx.get(String(root.id)) || [];
                const blocks = lv2.map(cat2 => {
                    const lv3 = idx.get(String(cat2.id)) || [];
                    return { cat2, lv3, weight: 1 + lv3.length };
                });

                const cols = splitIntoCols(blocks, 3);

                l2.innerHTML = cols[0].map(b => blockHtml(root.slug, b.cat2, b.lv3, idx)).join("");
                 
                l3.innerHTML = cols[1].map(b => blockHtml(root.slug, b.cat2, b.lv3, idx)).join("");
                l4.innerHTML = cols[2].map(b => blockHtml(root.slug, b.cat2, b.lv3, idx)).join("");
            }

            function highlightRoot(id) {
                left.querySelectorAll(".mega-item").forEach(b =>
                    b.classList.toggle("active", Number(b.dataset.id) === Number(id))
                );
            }

            // ✅ afficher le 1er root au chargement
            await renderRightJumia(roots[0]);
            highlightRoot(roots[0].id);

            // ✅ hover racines
            if (!left.dataset.bound) {
                left.dataset.bound = "1";

                left.addEventListener("mouseover", async (e) => {
                    const it = e.target.closest(".mega-item");
                    if (!it) return;

                    const root = {
                        id: Number(it.dataset.id),
                        slug: it.dataset.slug || "",
                        name: it.dataset.name || ""
                    };

                    await renderRightJumia(root);
                    highlightRoot(root.id);
                });

                left.addEventListener("click", (e) => {
                    const it = e.target.closest(".mega-item");
                    if (!it) return;
                    const slug = it.dataset.slug || "";
                    if (slug) location.href = "/c/" + slug;
                });

                // fermer au clic sur un lien
                [l2, l3, l4].forEach(col => {
                    col.addEventListener("click", (e) => {
                        const a = e.target.closest("a.mega-link");
                        if (!a) return;
                        close();
                    });
                });
            }
        } // ✅ FIN loadMegaRootsOnce

        // ✅ delegation open/close (TOUJOURS active)
        document.addEventListener("click", async (e) => {
            if (e.target.closest("#btnMegaCats")) {
                e.preventDefault();
                e.stopPropagation();
                open();
                try { await loadMegaRootsOnce(); } catch (err) { console.error(err); }
                return;
            }

            if (e.target.closest("#megaOverlay")) {
                close();
                return;
            }

            if (e.target.closest("#btnMegaCatsMobile")) {
                e.preventDefault();
                e.stopPropagation();
                openMobile();
                try { await loadMegaMobileRoots(); } catch (err) { console.error(err); }
                return;
            }

            if (e.target.closest("#megaMobileOverlay") || e.target.closest("#megaMobileClose")) {
                closeMobile();
                return;
            }
        }, true);



        // ✅ click dehors => ferme
        document.addEventListener("click", (e) => {
            const { menu, wrap, mobilePanel, mobileBtn } = getEls();

            if (menu && wrap && menu.classList.contains("open") && !wrap.contains(e.target)) {
                close();
            }

            if (
                mobilePanel &&
                mobilePanel.classList.contains("open") &&
                !mobilePanel.contains(e.target) &&
                !e.target.closest("#btnMegaCatsMobile")
            ) {
                closeMobile();
            }
        }, true);


        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape") {
                close();
                closeMobile();
            }
        });
    };




})();
