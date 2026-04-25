// wwwroot/assets/js/admin-products.js
(() => {
    "use strict";

    const PRODUCT_NAME_MAX = 60;

    function truncateText(text, max) {
        const s = String(text || "").trim();
        if (s.length <= max) return s;
        return s.slice(0, max).trim() + "…";
    }

    function updateNameCount() {
        const input = qs("#txtProdName");
        const box = qs("#nameCount");
        if (!input || !box) return;

        const len = String(input.value || "").length;
        box.textContent = `${len} / ${PRODUCT_NAME_MAX}`;
        box.style.color = len > PRODUCT_NAME_MAX ? "#fca5a5" : "";
    }

    console.log("✅ admin-products.js (NEW attributes-driven) chargé");

    // ========= Helpers =========
    const qs = (s, r = document) => r.querySelector(s);
    const qsa = (s, r = document) => Array.from(r.querySelectorAll(s));

    const esc = (s) => String(s ?? "").replace(/[&<>"']/g, c => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;"
    }[c]));

    const money = (n) => (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";

    // ✅ récupérer fetchJson depuis app.js (global)
    // ✅ fetchJson lazy (ne pas capturer trop tôt)
    function getFetchJson() {
        return window._fetchJson || window.fetchJson; // compat si tu as _fetchJson ailleurs
    }

    async function fetchAdminJson(url, opts = {}) {
        const token = localStorage.getItem("ranita_admin_token") || "";
        const headers = new Headers(opts.headers || {});
        if (token) headers.set("Authorization", `Bearer ${token}`);

        // si on envoie du JSON
        if (opts.body && !headers.has("Content-Type") && !(opts.body instanceof FormData)) {
            headers.set("Content-Type", "application/json");
        }

        // ✅ si url relative => même origin
        const res = await fetch(url, { ...opts, headers, cache: "no-store" });

        const ct = (res.headers.get("content-type") || "").toLowerCase();
        const raw = await res.text();

        let data = null;
        if (ct.includes("application/json")) {
            try { data = raw ? JSON.parse(raw) : null; } catch { data = null; }
        }

        // ✅ Erreurs auth propres
        if (res.status === 401) throw new Error("Session expirée. Reconnecte-toi.");
        if (res.status === 403) throw new Error("Accès refusé (droits insuffisants).");

        // ✅ Erreur HTTP => message métier uniquement
        if (!res.ok) {
            const msg =
                (data && (data.message || data.Message)) ||
                (raw && raw.trim()) ||
                "Erreur serveur.";
            throw new Error(String(msg));
        }

        // ✅ API ok:false => message métier
        if (data && data.ok === false) {
            throw new Error(String(data.message || "Erreur API"));
        }

        // ✅ si pas JSON
        if (!ct.includes("application/json")) return { ok: true, raw };

        return data;
    }



    function showMsg(type, title, desc = "") {
        const box = qs("#prodMsg");
        if (!box) return;
        box.className = "form-msg " + (type === "ok" ? "ok" : "err");
        box.innerHTML = `
      <div style="font-size:16px;font-weight:900;margin-bottom:${desc ? "4px" : "0"}">${esc(title)}</div>
      ${desc ? `<div style="font-weight:700;color:#cbd5e1">${esc(desc)}</div>` : ""}
    `;
        box.style.display = "block";
    }
    function clearMsg() {
        const box = qs("#prodMsg");
        if (!box) return;
        box.style.display = "none";
        box.textContent = "";
        box.className = "form-msg";
    }

    function openModal(type, title, desc = "") {
        const modal = qs("#appModal");
        if (!modal) return;
        const icon = qs("#modalIcon");
        const t = qs("#modalTitle");
        const d = qs("#modalDesc");

        if (icon) icon.textContent = (type === "ok") ? "✅" : "⚠️";
        if (t) t.textContent = title || "";
        if (d) {
            d.textContent = desc || "";
            d.style.display = desc ? "" : "none";
        }

        modal.classList.remove("ok", "err");
        modal.classList.add(type === "ok" ? "ok" : "err");
        modal.classList.add("show");
        modal.setAttribute("aria-hidden", "false");
        document.body.style.overflow = "hidden";
    }
    function closeModal() {
        const modal = qs("#appModal");
        if (!modal) return;
        modal.classList.remove("show");
        modal.setAttribute("aria-hidden", "true");
        document.body.style.overflow = "";
    }

    function getAdminToken() {
        return localStorage.getItem("ranita_admin_token") || "";
    }
    async function fetchAsFile(url, fallbackName) {
        const res = await fetch(url, { cache: "no-store" });
        if (!res.ok) throw new Error("Image non accessible: " + res.status);

        const blob = await res.blob();
        const ct = (res.headers.get("content-type") || "").toLowerCase();

        let ext = "";
        if (ct.includes("jpeg")) ext = ".jpg";
        else if (ct.includes("png")) ext = ".png";
        else if (ct.includes("webp")) ext = ".webp";
        else {
            // fallback via URL
            const m = (url.split("?")[0].match(/\.\w+$/) || [])[0];
            ext = m || ".jpg";
        }

        return new File([blob], (fallbackName || "img") + ext, { type: blob.type || "image/jpeg" });
    }

    async function copyProductImagesToQueue(productId) {
        // 1) Récupérer les images du produit
        const data = await fetchAdminJson(`/api/admin/products/${productId}?ts=${Date.now()}`);
        const it = data?.item;
        const images = it?.Images || it?.images || [];
        if (!images.length) return;

        // 2) Reset queue
        resetQueue();

        // 3) Télécharger et injecter dans DataTransfer (max 6)
        const urls = images
            .slice() // copie
            .sort((a, b) => ((b.IsMain || b.isMain) ? 1 : 0) - ((a.IsMain || a.isMain) ? 1 : 0))
            .map(x => safeAbsUrl(x.Url || x.url || ""))
            .filter(Boolean)
            .slice(0, state.MAX_IMAGES);

        for (let i = 0; i < urls.length; i++) {
            try {
                const file = await fetchAsFile(urls[i], `copy_${productId}_${i + 1}`);
                state.dt.items.add(file);
            } catch (e) {
                console.warn("copy img fail:", urls[i], e?.message || e);
            }
        }

        syncInputFiles();
        renderQueuePreview();
    }


    // ========= State =========
    const state = {
        bound: false,
        submitting: false,
        loadingProducts: false,

        editingId: null,
        lastProductId: null,
        copyMode: false,

        categories: [],
        catById: new Map(),
        catChildren: new Map(),
        catPathById: new Map(),

        // ✅ NEW: category attributes cache for current category
        catAttrRows: [],          // rows from /api/admin/categories/{id}/attributes
        variantAttrs: [],         // extracted variant attributes (0..2)
        opt1: [],
        opt2: [],
        variants: [],             // [{opt1,opt2,stock}]

        // Images queue
        MAX_IMAGES: 6,
        dt: new DataTransfer(),
        prevUrls: [],

        thumbCache: new Map()
    };

    // ========= Categories (leaf only) =========
    const CATS_TREE_URL = "/api/admin/categories/tree";

    function normCat(c) {
        return {
            id: Number(c?.id ?? c?.Id ?? 0) || 0,
            name: String(c?.name ?? c?.Name ?? "").trim(),
            parentId: (c?.parentId ?? c?.ParentId) == null ? null : Number(c?.parentId ?? c?.ParentId),
            isActive: (c?.isActive ?? c?.IsActive ?? true) === true
        };
    }

    function buildCategoryMaps(items) {
        state.categories = (items || []).map(normCat).filter(x => x.id && x.name);
        state.catById = new Map(state.categories.map(c => [c.id, c]));

        state.catChildren = new Map();
        for (const c of state.categories) {
            const pid = c.parentId ?? 0;
            if (!state.catChildren.has(pid)) state.catChildren.set(pid, []);
            state.catChildren.get(pid).push(c);
        }

        state.catPathById = new Map();
        for (const c of state.categories) state.catPathById.set(c.id, computePath(c.id));

        function computePath(id) {
            const parts = [];
            const seen = new Set();
            let cur = state.catById.get(id);
            while (cur && !seen.has(cur.id)) {
                seen.add(cur.id);
                parts.push(cur.name);
                cur = cur.parentId ? state.catById.get(cur.parentId) : null;
            }
            parts.reverse();
            return parts.join(" > ");
        }
    }

    function isLeafCategory(id) {
        for (const c of state.categories) if (c.parentId === id) return false;
        return true;
    }
    function getFirstLeafDescendant(startId) {
        const seen = new Set();
        const stack = [startId];

        while (stack.length) {
            const id = stack.shift();
            if (!id || seen.has(id)) continue;
            seen.add(id);

            if (isLeafCategory(id)) return id;

            // enfants
            const children = state.catChildren.get(id) || [];
            for (const c of children) {
                if (c.isActive) stack.push(c.id);
            }
        }
        return null;
    }

    function getSelectedCategoryId() {
        const sel = qs("#selProdCategory");
        const id = Number(sel?.value || 0);
        return Number.isFinite(id) && id > 0 ? id : null;
    }
    async function loadCategoriesIntoSelect() {
        const sel = qs("#selProdCategory");
        if (!sel) return;

        sel.innerHTML = `<option value="">— Choisir une catégorie —</option>`;

        let items = [];

        // 1) tree admin
        try {
            const data = await fetchAdminJson(`${CATS_TREE_URL}?ts=${Date.now()}`, { cache: "no-store" });
            items = Array.isArray(data) ? data : (data.items || data.categories || []);
        } catch (e) {
            console.warn("Categories tree KO:", e?.message || e);
        }

        // 2) fallback public
        if (!items.length) {
            try {
                const fn = (window._fetchJson || window.fetchJson);
                if (typeof fn !== "function") throw new Error("fetchJson introuvable");
                const data2 = await fn(`/api/categories?ts=${Date.now()}`, { cache: "no-store" });
                items = Array.isArray(data2) ? data2 : (data2.items || data2.categories || []);
            } catch (e) {
                console.warn("Categories public KO:", e?.message || e);
            }
        }

        if (!items.length) {
            sel.innerHTML = `<option value="">⚠️ Aucune catégorie trouvée</option>`;
            return;
        }

        buildCategoryMaps(items);

        const leaves = state.categories
            .filter(c => c.isActive && isLeafCategory(c.id))
            .sort((a, b) => (state.catPathById.get(a.id) || "").localeCompare(state.catPathById.get(b.id) || ""));

        if (!leaves.length) {
            sel.innerHTML = `<option value="">⚠️ Aucune sous-catégorie active</option>`;
            return;
        }

        sel.innerHTML =
            `<option value="">— Choisir une catégorie —</option>` +
            leaves.map(c => {
                const label = state.catPathById.get(c.id) || c.name;
                return `<option value="${c.id}">${esc(label)}</option>`;
            }).join("");

        const urlRaw =
            Number(getQueryParam("categoryId") || 0) ||
            Number(getQueryParam("catId") || 0);

        let targetId = null;
        if (urlRaw && state.catById.has(urlRaw)) {
            targetId = isLeafCategory(urlRaw) ? urlRaw : getFirstLeafDescendant(urlRaw);
        }

        sel.value = (targetId && leaves.some(x => x.id === targetId))
            ? String(targetId)
            : String(leaves[0].id);

        const info = qs("#catInfoForm");
        if (info) {
            const c = state.catById.get(urlRaw);
            info.textContent = urlRaw
                ? `URL catId=${urlRaw} → ${c ? (state.catPathById.get(urlRaw) || c.name) : "INCONNU"} | sel=${sel.value} → ${(state.catPathById.get(Number(sel.value)) || "")}`
                : "";
        }

        sel.dispatchEvent(new Event("change"));
    }




    function getQueryParam(name) {
        try { return new URL(location.href).searchParams.get(name); } catch { return null; }
    }

    // ========= NEW: Category Attributes =========
    async function fetchCategoryAttributes(catId) {
        const tries = [
            `/api/admin/categories/${catId}/attributes?ts=${Date.now()}`,
            `/api/admin/categories/${catId}/attrs?ts=${Date.now()}`,
            `/api/admin/category-attributes?categoryId=${catId}&ts=${Date.now()}`,
            `/api/admin/categories/${catId}/attribute-links?ts=${Date.now()}`
        ];

        for (const url of tries) {
            try {
                const data = await fetchAdminJson(url, { cache: "no-store" });

                // formats possibles
                const rows =
                    Array.isArray(data) ? data :
                        Array.isArray(data?.items) ? data.items :
                            Array.isArray(data?.rows) ? data.rows :
                                Array.isArray(data?.data) ? data.data :
                                    [];

                if (rows.length) {
                    console.log("✅ ATTR endpoint OK:", url, "rows:", rows.length);
                    window.__lastAttrUrl = url;
                    const normalized = rows.map(normCatAttrRow);
                    return normalized;

                    return rows;
                } else {
                    console.warn("ATTR endpoint empty:", url, data);
                }
            } catch (e) {
                console.warn("ATTR endpoint FAIL:", url, e?.message || e);
            }
        }

        return [];
    }



    function renderDynamicAttributesUI(rows) {
        const box = qs("#dynAttrsBox");
        if (!box) return;

        const nonVariants = (rows || []).filter(r => {
            const a = r?.attribute || r?.Attribute;
            return !(a?.isVariant === true || a?.IsVariant === true);
        });


        if (!nonVariants.length) {
            const vars = extractVariantAttrs(rows);
            box.innerHTML = `
    <div class="muted">
      Aucun attribut “fiche produit” pour cette catégorie.
      <br>Variantes détectées : <b>${vars.map(x => x.name).join(" • ") || "Aucune"}</b>
    </div>`;
            return;
        }


        box.innerHTML = nonVariants.map(r => {
            const a = r.attribute;
            const req = r.isRequired ? " *" : "";

            const dt = String(a.dataType ?? "").toLowerCase();
            const isOptionType = dt === "option" || dt === "1";

            if (isOptionType) {
                const opts = (a.options || [])
                    .map(o => `<option value="${o.id}">${esc(o.value)}</option>`)
                    .join("");
                return `
        <label class="field">
          <div class="lbl">${esc(a.name)}${req}</div>
          <select class="inp" data-attr-id="${a.id}" data-attr-type="option">
            <option value="">—</option>
            ${opts}
          </select>
        </label>
      `;
            }

            return `
      <label class="field">
        <div class="lbl">${esc(a.name)}${req}</div>
        <input class="inp" data-attr-id="${a.id}" data-attr-type="text" placeholder="${esc(a.name)}">
      </label>
    `;
        }).join("");
    }


async function loadVendorsIntoSelect() {
    const sel = qs("#selVendor");
    if (!sel) return;

    sel.innerHTML = `<option value="">— Produit admin / aucune boutique —</option>`;

    try {
        const data = await fetchAdminJson(`/api/admin/vendors?status=-1&ts=${Date.now()}`);
        const items = Array.isArray(data?.items) ? data.items : [];

        const ranitaOnly = items.filter(v =>
            String(v.name || v.Name || "").trim().toLowerCase() === "ranita shop"
        );

        sel.innerHTML += ranitaOnly.map(v => `
            <option value="${Number(v.id)}">${esc(v.name || v.Name || "")}</option>
        `).join("");

        if (ranitaOnly.length === 1) {
            sel.value = String(ranitaOnly[0].id);
        }
    } catch (e) {
        console.warn("Chargement vendeurs KO:", e?.message || e);
    }
}

    function extractVariantAttrs(rows) {
        const vars = (rows || [])
            .map(r => r?.attribute || r?.Attribute)
            .filter(a => a && (a.isVariant === true || a.IsVariant === true));

        const prio = (a) => {
            const code = String(a.code || a.Code || "").toLowerCase();
            if (code === "size" || code === "taille" || code === "pointure") return 1;
            if (code === "color" || code === "couleur") return 2;
            return 50;
        };

        vars.sort((a, b) => prio(a) - prio(b));
        return vars.slice(0, 2);
    }


    function setVariantLabels(label1, label2) {
        const l1 = qs("#lblSizes"); if (l1) l1.textContent = label1 || "Option 1";
        const l2 = qs("#lblColors"); if (l2) l2.textContent = label2 || "Option 2";

        const th1 = qs("#thVar1"); if (th1) th1.textContent = label1 || "Option 1";
        const th2 = qs("#thVar2"); if (th2) th2.textContent = label2 || "Option 2";

        const title = qs("#variantTitle");
        if (title) {
            title.textContent = label2 ? `Stocks par variante (${label1} × ${label2})` : `Stocks par variante (${label1})`;
        }
    }

    function setVariantUIEnabled(enabled, hasSecond) {
        const sizesBlock = qs("#sizesBlock");
        const colorsBlock = qs("#colorsBlock");
        const wrap = qs("#variantsTableWrap");
        const hint = qs("#variantsHint");

        if (!enabled) {
            if (sizesBlock) sizesBlock.style.display = "none";
            if (colorsBlock) colorsBlock.style.display = "none";
            if (wrap) wrap.classList.add("is-hidden");
            if (hint) hint.textContent = "Pas de variantes pour cette catégorie.";
            clearVariants();
            return;
        }

        if (sizesBlock) sizesBlock.style.display = "";
        if (colorsBlock) colorsBlock.style.display = hasSecond ? "" : "none";
        if (wrap) wrap.classList.remove("is-hidden");
    }

    function setDatalistOptions(dlId, values) {
        const dl = qs(dlId);
        if (!dl) return;
        const uniq = Array.from(new Set((values || []).map(v => String(v).trim()).filter(Boolean)));
        dl.innerHTML = uniq.map(v => `<option value="${esc(v)}"></option>`).join("");
    }

    function applyVariantsFromAttributes(variantAttrs) {

        // 🔴 si aucune variante
        if (!variantAttrs.length) {
            setVariantUIEnabled(false, false);
            return;
        }

        const a1 = variantAttrs[0];
        const a2 = variantAttrs[1] || null;

        setVariantLabels(a1?.name || "Option 1", a2?.name || "");

        // ✅ Si tu as un champ legacy "Type de variante"
        const legacy = document.querySelector("#variantType, #txtVariantType, #lblVariantType");
        if (legacy) {
            const txt = (a2 ? `${a1.name} + ${a2.name}` : a1.name);
            if (legacy.tagName === "INPUT") legacy.value = txt;
            else legacy.textContent = txt;
        }

        // ✅ IMPORTANT : même avec 1 seule variante on active l'UI
        setVariantUIEnabled(true, !!a2);

        const s1 = (a1.options || []).map(o => o.value ?? o.Value).filter(Boolean);
        const s2 = a2 ? (a2.options || []).map(o => o.value ?? o.Value).filter(Boolean) : [];


        setDatalistOptions("#dlSizes", s1);
        setDatalistOptions("#dlColors", s2);

        state.opt1 = uniqLower(s1.map(normalizeValue));
        state.opt2 = a2 ? uniqLower(s2.map(normalizeValue)) : [];

        syncHidden();
        renderTags("#sizesBox", "#inpSize", state.opt1, (v) => removeOpt(1, v));
        renderTags("#colorsBox", "#inpColor", state.opt2, (v) => removeOpt(2, v));

      
    }

    function normOpt(o) {
        return {
            id: Number(o?.id ?? o?.Id ?? 0) || 0,
            value: String(o?.value ?? o?.Value ?? "").trim(),
            sortOrder: Number(o?.sortOrder ?? o?.SortOrder ?? 0) || 0,
            isActive: (o?.isActive ?? o?.IsActive ?? true) === true
        };
    }

    function normAttr(a) {
        return {
            id: Number(a?.id ?? a?.Id ?? 0) || 0,
            code: String(a?.code ?? a?.Code ?? "").trim(),
            name: String(a?.name ?? a?.Name ?? "").trim(),
            dataType: String(a?.dataType ?? a?.DataType ?? "Text"),
            isVariant: (a?.isVariant ?? a?.IsVariant ?? false) === true,
            isActive: (a?.isActive ?? a?.IsActive ?? true) === true,
            options: (a?.options ?? a?.Options ?? []).map(normOpt)
        };
    }

    function normCatAttrRow(r) {
        return {
            attributeId: Number(r?.attributeId ?? r?.AttributeId ?? 0) || 0,
            isRequired: (r?.isRequired ?? r?.IsRequired ?? false) === true,
            isFilterable: (r?.isFilterable ?? r?.IsFilterable ?? false) === true,
            sortOrder: Number(r?.sortOrder ?? r?.SortOrder ?? 0) || 0,
            attribute: normAttr(r?.attribute ?? r?.Attribute)
        };
    }


    async function applyCategoryRules() {
        const catId = getSelectedCategoryId();
        if (!catId) return;

        try {
            const rows = await fetchCategoryAttributes(catId);

            state.catAttrRows = Array.isArray(rows) ? rows : [];
            // 🔎 DEBUG (console)
            window.__ap_state = state;
            window.__lastRows = state.catAttrRows;

            // Debug utile
            console.log("ATTR rows len =", state.catAttrRows.length, "catId=", catId, state.catAttrRows);

            renderDynamicAttributesUI(state.catAttrRows);

            state.variantAttrs = extractVariantAttrs(state.catAttrRows);
            applyVariantsFromAttributes(state.variantAttrs);
        } catch (e) {
            console.error("applyCategoryRules ERROR:", e);
            // pour éviter que tout bloque
            state.catAttrRows = [];
            state.variantAttrs = [];
            renderDynamicAttributesUI([]);
            applyVariantsFromAttributes([]);
            showMsg("err", "Attributs non chargés", e?.message || String(e));
        }
    }

    // ========= Tags + Variants =========
    function normalizeValue(v) {
        return String(v || "").trim().replace(/\s+/g, " ");
    }
    function uniqLower(list) {
        const seen = new Set();
        const out = [];
        for (const v of list) {
            const k = v.toLowerCase();
            if (seen.has(k)) continue;
            seen.add(k);
            out.push(v);
        }
        return out;
    }

    function renderTags(boxSel, inputSel, items, onRemove) {
        const box = qs(boxSel);
        if (!box) return;
        box.querySelectorAll(".tag").forEach(t => t.remove());
        const input = qs(inputSel);
        for (const v of items) {
            const el = document.createElement("span");
            el.className = "tag";
            el.innerHTML = `<span>${esc(v)}</span><button type="button" title="Retirer">×</button>`;
            el.querySelector("button").addEventListener("click", () => onRemove(v));
            box.insertBefore(el, input);
        }
    }

    function syncHidden() {
        const h1 = qs("#hidSizes"); if (h1) h1.value = state.opt1.join(";");
        const h2 = qs("#hidColors"); if (h2) h2.value = state.opt2.join(";");
    }

    function addOpt(which, value) {
        value = normalizeValue(value);
        if (!value) return;

        if (which === 1) {
            state.opt1 = uniqLower([...state.opt1, value]);
            renderTags("#sizesBox", "#inpSize", state.opt1, (v) => removeOpt(1, v));
            const inp = qs("#inpSize"); if (inp) inp.value = "";
        } else {
            state.opt2 = uniqLower([...state.opt2, value]);
            renderTags("#colorsBox", "#inpColor", state.opt2, (v) => removeOpt(2, v));
            const inp = qs("#inpColor"); if (inp) inp.value = "";
        }
        syncHidden();
    }

    function removeOpt(which, value) {
        const k = String(value).toLowerCase();
        if (which === 1) {
            state.opt1 = state.opt1.filter(x => x.toLowerCase() !== k);
            renderTags("#sizesBox", "#inpSize", state.opt1, (v) => removeOpt(1, v));
        } else {
            state.opt2 = state.opt2.filter(x => x.toLowerCase() !== k);
            renderTags("#colorsBox", "#inpColor", state.opt2, (v) => removeOpt(2, v));
        }
        syncHidden();
    }

    function clearVariants() {
        state.opt1 = [];
        state.opt2 = [];
        state.variants = [];
        syncHidden();
        renderTags("#sizesBox", "#inpSize", [], () => { });
        renderTags("#colorsBox", "#inpColor", [], () => { });
        renderVariantsTable();
    }

    function recalcTotalStock() {
        const stock = qs("#txtProdStock");
        if (!stock) return;

        // Produit avec variantes : le stock global ne doit pas piloter l'affichage produit
        if (state.variantAttrs.length && state.variants.length) {
            stock.value = "0";
            return;
        }

        const total = state.variants.reduce((sum, v) => sum + (Number(v.stock || 0) || 0), 0);
        stock.value = String(total);
    }

    function renderVariantsTable() {
        const tb = qs("#variantsTbody");
        const hint = qs("#variantsHint");
        if (!tb) return;

        if (!state.variantAttrs.length) {
            tb.innerHTML = "";
            if (hint) hint.textContent = "Pas de variantes pour cette catégorie.";
            return;
        }

        if (!state.variants.length) {
            tb.innerHTML = "";
            if (hint) hint.textContent = "Ajoute les options puis clique sur Générer.";
            recalcTotalStock();
            return;
        }

        if (hint) hint.textContent = `Variantes: ${state.variants.length} (stock total auto)`;

        tb.innerHTML = state.variants.map((v, idx) => `
      <tr data-idx="${idx}">
        <td>${esc(v.opt1)}</td>
        <td>${esc(v.opt2)}</td>
        <td><input type="number" min="0" value="${Number(v.stock || 0)}"
              class="inpVarStock" data-idx="${idx}" style="width:140px"></td>
      </tr>
    `).join("");

        recalcTotalStock();
    }

    function generateVariants() {
        // ✅ pas de variant attr => pas de variantes
        if (!state.variantAttrs.length) {
            state.variants = [];
            renderVariantsTable();
            return;
        }

        const opt1 = state.opt1.slice();
        if (!opt1.length) {
            showMsg("err", "Ajoute au moins une valeur dans " + (state.variantAttrs[0]?.name || "Option 1"));
            state.variants = [];
            renderVariantsTable();
            return;
        }

        const hasSecond = state.variantAttrs.length > 1;
        const opt2 = (hasSecond ? state.opt2.slice() : ["Unique"]);
        const finalOpt2 = opt2.length ? opt2 : ["Unique"];

        const uniq = new Set();
        const out = [];

        for (const a of opt1) {
            const A = normalizeValue(a);
            if (!A) continue;

            for (const b of finalOpt2) {
                const B = normalizeValue(b) || "Unique";
                const key = (A + "__" + B).toLowerCase();
                if (uniq.has(key)) continue;
                uniq.add(key);

                const old = state.variants.find(x =>
                    x.opt1.toLowerCase() === A.toLowerCase() &&
                    x.opt2.toLowerCase() === B.toLowerCase()
                );

                out.push({ opt1: A, opt2: B, stock: old ? Number(old.stock || 0) : 0 });

                if (!hasSecond) break;
            }
        }

        state.variants = out;
        renderVariantsTable();
    }

    async function saveVariants(productId) {
        const payload = state.variants.map(v => ({
            key1: v.opt1 || "Unique",
            key2: v.opt2 || "Unique",
            stock: Math.max(0, Number(v.stock || 0) || 0)
        }));

        return await fetchAdminJson(`/api/admin/products/${productId}/variants`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ items: payload })
        });
    }

    // ========= Dynamic attrs save =========
    function fillDynamicAttrsFromProduct(it) {
        const items = (it?.attributes || it?.Attributes || []).map(v => ({
            attributeId: v.attributeId ?? v.AttributeId,
            optionId: v.optionId ?? v.OptionId,
            valueText: v.valueText ?? v.ValueText ?? ""
        }));

        console.log("ATTRS ADMIN =", items);

        if (!items.length) return;

        for (const v of items) {
            const attrId = Number(v.attributeId || 0);
            if (!attrId) continue;

            const el = qs(`[data-attr-id="${attrId}"]`);
            if (!el) {
                console.warn("Champ introuvable pour attrId =", attrId);
                continue;
            }

            if (el.tagName === "SELECT") {
                const optId = Number(v.optionId || 0);
                const txt = String(v.valueText || "").trim();

                let matched = false;

                if (optId) {
                    const byId = Array.from(el.options).find(o => String(o.value) === String(optId));
                    if (byId) {
                        el.value = String(optId);
                        matched = true;
                    }
                }

                if (!matched && txt) {
                    const byText = Array.from(el.options).find(o =>
                        String(o.text).trim().toLowerCase() === txt.toLowerCase()
                    );
                    if (byText) {
                        el.value = byText.value;
                        matched = true;
                    }
                }

                if (!matched) {
                    console.warn("Aucune option trouvée pour attrId =", attrId, "optId =", optId, "txt =", txt);
                    el.value = "";
                }
            } else {
                el.value = v.valueText ? String(v.valueText) : "";
            }
        }
    }

    function collectDynamicAttrs() {
        const box = qs("#dynAttrsBox");
        if (!box) return [];
        const inputs = qsa("[data-attr-id]", box);

        const items = [];
        for (const el of inputs) {
            const attributeId = Number(el.dataset.attrId || 0);
            if (!attributeId) continue;

            if (el.tagName === "SELECT") {
                const optionId = el.value ? Number(el.value) : null;
                items.push({ attributeId, optionId });
            } else {
                const val = String(el.value || "").trim();
                if (val) items.push({ attributeId, valueText: val });
            }
        }
        return items;
    }

    async function saveDynamicAttrs(productId) {
        const items = collectDynamicAttrs();
        return await fetchAdminJson(`/api/admin/products/${productId}/attributes`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ items })
        });
    }

    // ========= Images Queue (ton code conservé) =========
    function revokePrevUrls() {
        for (const u of state.prevUrls) { try { URL.revokeObjectURL(u); } catch { } }
        state.prevUrls = [];
    }
    function syncInputFiles() {
        const input = qs("#fileProdImages");
        if (!input) return;
        try { input.files = state.dt.files; } catch { }
    }
    function resetQueue() {
        revokePrevUrls();
        state.dt = new DataTransfer();
        syncInputFiles();
        const box = qs("#imgQueue");
        if (box) box.innerHTML = "";
        const c = qs("#galCount");
        if (c) c.textContent = `0 / ${state.MAX_IMAGES}`;
        const prev = qs("#prevImg");
        if (prev) prev.src = "/assets/placeholder.png";
        const input = qs("#fileProdImages");
        if (input) input.disabled = false;
    }
    function renderQueuePreview() {
        const box = qs("#imgQueue");
        if (!box) return;

        revokePrevUrls();

        if (state.dt.files.length > state.MAX_IMAGES) {
            const keep = Array.from(state.dt.files).slice(0, state.MAX_IMAGES);
            state.dt = new DataTransfer();
            keep.forEach(f => state.dt.items.add(f));
            syncInputFiles();
        }

        const files = Array.from(state.dt.files || []);
        const c = qs("#galCount");
        if (c) c.textContent = `${files.length} / ${state.MAX_IMAGES}`;

        if (!files.length) {
            box.innerHTML = "";
            const prev = qs("#prevImg");
            if (prev) prev.src = "/assets/placeholder.png";
            const input = qs("#fileProdImages");
            if (input) input.disabled = false;
            return;
        }

        const prev = qs("#prevImg");
        if (prev && files[0]) {
            const u0 = URL.createObjectURL(files[0]);
            state.prevUrls.push(u0);
            prev.src = u0;
        }

        box.innerHTML = files.map((f, idx) => {
            const u = URL.createObjectURL(f);
            state.prevUrls.push(u);
            return `
        <div class="q-card">
          <img class="q-img" src="${u}" alt="">
          ${idx === 0 ? `<span class="q-badge main">⭐</span>` : ""}
          <button type="button" class="q-del" data-idx="${idx}" title="Retirer">×</button>
        </div>
      `;
        }).join("");

        const input = qs("#fileProdImages");
        if (input) input.disabled = (files.length >= state.MAX_IMAGES);
    }

    function bindQueue() {
        const input = qs("#fileProdImages");
        const box = qs("#imgQueue");

        if (input && input.dataset.bound !== "1") {
            input.dataset.bound = "1";
            input.addEventListener("change", () => {
                const picked = Array.from(input.files || []);
                if (!picked.length) return;

                const remaining = state.MAX_IMAGES - state.dt.files.length;
                if (remaining <= 0) return showMsg("err", `Maximum ${state.MAX_IMAGES} images.`);

                picked.slice(0, remaining).forEach(f => state.dt.items.add(f));
                syncInputFiles();
                renderQueuePreview();
            });
        }

        if (box && box.dataset.bound !== "1") {
            box.dataset.bound = "1";
            box.addEventListener("click", (e) => {
                const btn = e.target.closest(".q-del");
                if (!btn) return;
                e.preventDefault();
                const idx = Number(btn.dataset.idx);
                if (!Number.isFinite(idx) || idx < 0) return;

                const keep = Array.from(state.dt.files).filter((_, i) => i !== idx);
                state.dt = new DataTransfer();
                keep.forEach(f => state.dt.items.add(f));
                syncInputFiles();
                renderQueuePreview();
            }, true);
        }
    }

    async function uploadProductImages(productId, files) {
        const fd = new FormData();
        for (const f of files) fd.append("files", f);
        fd.append("setFirstAsMain", "true");
        return await fetchAdminJson(`/api/admin/products/${productId}/images`, { method: "POST", body: fd });
    }
    async function deleteAllImages(productId) {
        const data = await fetchAdminJson(`/api/admin/products/${productId}`);
        const it = data?.item;
        const images = it?.Images || it?.images || [];
        for (const img of images) {
            const id = Number(img.Id || img.id || 0);
            if (id) await fetchAdminJson(`/api/admin/products/${productId}/images/${id}`, { method: "DELETE" });
        }
    }


    async function loadVendors() {
    const sel = document.querySelector("#selVendor");
    if (!sel) return;

    try {
        const res = await fetch("/api/admin/vendors?status=-1");
        const json = await res.json();

        if (!json.ok) return;

        sel.innerHTML = `
            <option value="">— Produit admin / aucune boutique —</option>
        `;

        json.items.forEach(v => {
            sel.innerHTML += `
                <option value="${v.id}">
                    ${v.shopName || v.name}
                </option>
            `;
        });

    } catch (e) {
        console.error("Erreur chargement vendeurs", e);
    }
}
    // ========= Products =========
    function setSubmitting(isBusy) {
        state.submitting = isBusy;
        const btn = qs("#btnProdCreate");
        if (!btn) return;
        btn.disabled = isBusy;
        if (isBusy) {
            btn.dataset._oldText = btn.textContent;
            btn.textContent = "Enregistrement...";
        } else if (btn.dataset._oldText) {
            btn.textContent = btn.dataset._oldText;
            delete btn.dataset._oldText;
        }
    }

    function setEditMode(id) {
        state.editingId = Number(id || 0) || null;
        state.copyMode = false;
        const btn = qs("#btnProdCreate");
        if (btn) btn.textContent = state.editingId ? "Modifier" : "Créer";
    }
    function safeAbsUrl(u) {
        try { if (typeof absUrl === "function") return absUrl(u); } catch { }
        return u;
    }

    function pickMainUrl(images) {
        if (!Array.isArray(images) || !images.length) return null;
        const main = images.find(x => (x.IsMain || x.isMain)) || images[0];
        return main ? (main.Url || main.url) : null;
    }

    // ✅ NEW: met à jour le preview principal (#prevImg) avec l'image MAIN du produit
    function setMainPreviewFromProduct(it) {
        const prev = qs("#prevImg");
        if (!prev) return;

        const images = it?.Images || it?.images || [];
        const url = pickMainUrl(images);
        prev.src = url ? safeAbsUrl(url) : "/assets/placeholder.png";
    }

    function renderExistingGallery(it) {
        const wrap = qs("#galleryWrap");
        const hint = qs("#galleryHint");
        const box = qs("#imgGallery");
        if (!wrap || !hint || !box) return;

        const images = it?.Images || it?.images || [];
        const pid = Number(it?.id || it?.Id || 0);

        if (!pid) {
            hint.textContent = "Crée un produit pour gérer sa galerie.";
            box.innerHTML = "";
            return;
        }

        if (!images.length) {
            hint.textContent = "Aucune image enregistrée pour ce produit.";
            box.innerHTML = "";
            return;
        }

        hint.textContent = `Images enregistrées: ${images.length}`;

        // tri : main d'abord puis sortOrder
        const sorted = images.slice().sort((a, b) => {
            const am = (a.isMain || a.IsMain) ? 1 : 0;
            const bm = (b.isMain || b.IsMain) ? 1 : 0;
            if (am !== bm) return bm - am;
            return Number(a.sortOrder ?? a.SortOrder ?? 0) - Number(b.sortOrder ?? b.SortOrder ?? 0);
        });

        box.innerHTML = sorted.map(img => {
            const id = Number(img.id || img.Id || 0);
            const url = safeAbsUrl(img.url || img.Url || "");
            const isMain = !!(img.isMain || img.IsMain);
            return `
      <div class="img-card">
        <div class="thumb"><img src="${esc(url)}" alt=""></div>
        <div class="bar">
          <span class="badge-main">${isMain ? "⭐ Principale" : "Image"}</span>
          <div class="img-actions">
            <button class="btn-mini" data-act="mainImg" data-pid="${pid}" data-imgid="${id}">⭐</button>
            <button class="btn-mini danger" data-act="delImg" data-pid="${pid}" data-imgid="${id}">🗑️</button>
          </div>
        </div>
      </div>
    `;
        }).join("");
    }

    async function setMainImage(pid, imgId) {
        await fetchAdminJson(`/api/admin/products/${pid}/images/${imgId}/main`, { method: "POST" });
    }

    async function deleteOneImage(pid, imgId) {
        await fetchAdminJson(`/api/admin/products/${pid}/images/${imgId}`, { method: "DELETE" });
    }

    function resetForm() {
        clearMsg();
        setEditMode(null);
        state.lastProductId = null;
        state.copyMode = false;

        ["#txtProdName", "#txtProdPrice", "#txtProdPricePromo", "#txtProdStock", "#txtProdBrand", "#txtProdSku",
            "#txtProdShort", "#txtProdDesc", "#txtProdLong", "#txtProdHighlights", "#txtProdWeightKg", "#txtProdDimensions"
        ].forEach(id => { const el = qs(id); if (el) el.value = ""; });

        const active = qs("#chkActive"); if (active) active.checked = true;

        clearVariants();
        resetQueue();
        updateNameCount();
    }
    function setMainPreviewFromProduct(it) {
        const prev = qs("#prevImg");
        if (!prev) return;

        const images = it?.Images || it?.images || [];
        const url = pickMainUrl(images);
        prev.src = url ? safeAbsUrl(url) : "/assets/placeholder.png";
    }


    async function loadProductIntoForm(productId) {
        const data = await fetchAdminJson(`/api/admin/products/${productId}`);
        const it = data?.item;
        if (!it) return;
        renderExistingGallery(it);
        setMainPreviewFromProduct(it);
       

        // 1) set cat
        const sel = qs("#selProdCategory");
        const catId = (it.categoryId ?? it.CategoryId ?? 0);
        if (sel && catId) sel.value = String(catId);


        await applyCategoryRules();

        // 🔥 ATTENDRE que le DOM soit bien construit
        await new Promise(r => setTimeout(r, 80));

        // 3) fill dynamic attrs
        fillDynamicAttrsFromProduct(it);

        // fields
        qs("#txtProdName") && (qs("#txtProdName").value = (it.name ?? it.Name ?? ""));
        updateNameCount();
        qs("#txtProdPrice") && (qs("#txtProdPrice").value = (it.price ?? it.Price ?? ""));
        qs("#txtProdPricePromo") && (qs("#txtProdPricePromo").value = (it.pricePromo ?? it.PricePromo ?? ""));
        qs("#txtProdStock") && (qs("#txtProdStock").value = (it.stock ?? it.Stock ?? 0));
        qs("#chkActive") && (qs("#chkActive").checked = !!(it.isActive ?? it.IsActive));

        qs("#txtProdBrand") && (qs("#txtProdBrand").value = (it.brand ?? it.Brand ?? ""));
        qs("#txtProdSku") && (qs("#txtProdSku").value = (it.sku ?? it.Sku ?? ""));
        qs("#selVendor") && (qs("#selVendor").value = String(it.vendorId ?? it.VendorId ?? ""));

        qs("#txtProdShort") && (qs("#txtProdShort").value = (it.shortDescription ?? it.ShortDescription ?? ""));
        qs("#txtProdDesc") && (qs("#txtProdDesc").value = (it.description ?? it.Description ?? ""));
        qs("#txtProdLong") && (qs("#txtProdLong").value = (it.longDescription ?? it.LongDescription ?? ""));
        qs("#txtProdHighlights") && (qs("#txtProdHighlights").value = (it.highlights ?? it.Highlights ?? ""));

        qs("#txtProdWeightKg") && (qs("#txtProdWeightKg").value = (it.weightKg ?? it.WeightKg ?? ""));
        qs("#txtProdDimensions") && (qs("#txtProdDimensions").value = (it.dimensions ?? it.Dimensions ?? ""));


        // ✅ variants from API (key1/key2)
        const vars = it.variants || it.Variants || [];
        if (Array.isArray(vars) && vars.length) {
            state.variants = vars.map(x => ({
                opt1: normalizeValue(x.key1 ?? x.Key1 ?? x.opt1 ?? x.size ?? ""),
                opt2: normalizeValue(x.key2 ?? x.Key2 ?? x.opt2 ?? x.color ?? "Unique") || "Unique",
                stock: Math.max(0, Number(x.stock ?? x.Stock ?? 0) || 0)
            }));
        } else {
            state.variants = [];
        }

        // derive tags
        const derived = deriveOptionsFromVariants(state.variants);
        state.opt1 = derived.opt1;
        state.opt2 = derived.opt2;

        syncHidden();
        renderTags("#sizesBox", "#inpSize", state.opt1, (v) => removeOpt(1, v));
        renderTags("#colorsBox", "#inpColor", state.opt2, (v) => removeOpt(2, v));

        renderVariantsTable();
    }
    function renderDynamicAttributesUI(rows) {
        const box = qs("#dynAttrsBox");
        if (!box) return;

        const nonVariants = (rows || []).filter(r => {
            const a = r?.attribute || r?.Attribute;
            return !(a?.isVariant === true || a?.IsVariant === true);
        });

        if (!nonVariants.length) {
            const vars = extractVariantAttrs(rows);
            box.innerHTML = `
    <div class="muted">
      Aucun attribut “fiche produit” pour cette catégorie.
      <br>Variantes détectées : <b>${vars.map(x => x.name).join(" • ") || "Aucune"}</b>
    </div>`;
            return;
        }

        box.innerHTML = nonVariants.map(r => {
            const a = r.attribute;
            const req = r.isRequired ? " *" : "";
            const dt = String(a.dataType ?? "").toLowerCase();
            const isOptionType = dt === "option" || dt === "1";

            if (isOptionType) {
                const opts = (a.options || [])
                    .map(o => `<option value="${o.id}">${esc(o.value)}</option>`)
                    .join("");

                return `
        <label class="field">
          <div class="lbl">${esc(a.name)}${req}</div>
          <select class="inp" data-attr-id="${a.id}" data-attr-type="option">
            <option value="">—</option>
            ${opts}
          </select>
        </label>
      `;
            }

            return `
      <label class="field">
        <div class="lbl">${esc(a.name)}${req}</div>
        <input class="inp" data-attr-id="${a.id}" data-attr-type="text" placeholder="${esc(a.name)}">
      </label>
    `;
        }).join("");
    }
    function deriveOptionsFromVariants(vars) {
        const opt1 = [];
        const opt2 = [];
        for (const v of (vars || [])) {
            const a = normalizeValue(v?.opt1 ?? "");
            const b = normalizeValue(v?.opt2 ?? "Unique") || "Unique";
            if (a) opt1.push(a);
            if (b) opt2.push(b);
        }
        return { opt1: uniqLower(opt1), opt2: uniqLower(opt2) };
    }

    async function createOrUpdateProduct() {
        if (state.submitting) return;
        clearMsg();

        const catId = getSelectedCategoryId();
        if (!catId) return showMsg("err", "Choisis une catégorie.");

        const name = (qs("#txtProdName")?.value || "").trim();

        if (name.length > PRODUCT_NAME_MAX) {
            return showMsg("err", "Nom trop long.", `Maximum ${PRODUCT_NAME_MAX} caractères.`);
        }
        const price = Number(String(qs("#txtProdPrice")?.value || "").trim().replace(",", ".") || 0);
        const stock = Number(String(qs("#txtProdStock")?.value || "").trim() || 0);
        const isActive = !!qs("#chkActive")?.checked;

        if (!name) return showMsg("err", "Nom obligatoire.");
        if (!Number.isFinite(price) || price <= 0) return showMsg("err", "Prix invalide.");
        if (!Number.isFinite(stock) || stock < 0) return showMsg("err", "Stock invalide.");

        const sku = (qs("#txtProdSku")?.value || "").trim();
        if (!state.editingId && !sku) return showMsg("err", "SKU obligatoire.", "Ex: RN-BASK-NIKE-001");

        const promoRaw = String(qs("#txtProdPricePromo")?.value || "").trim().replace(",", ".");
        const pricePromo = promoRaw ? Number(promoRaw) : null;

        const hasVariants = state.variantAttrs.length && state.variants.length;

        const payload = {
            categoryId: catId,
            vendorId: Number(qs("#selVendor")?.value || 0) || null,
            name,
            price,
            pricePromo: (pricePromo !== null && Number.isFinite(pricePromo) && pricePromo > 0) ? pricePromo : null,
            stock: hasVariants ? 0 : Math.max(0, stock),
            isActive,
            brand: (qs("#txtProdBrand")?.value || "").trim() || null,
            ...(sku ? { sku } : (state.editingId ? {} : { sku: null })),
            shortDescription: (qs("#txtProdShort")?.value || "").trim() || null,
            description: (qs("#txtProdDesc")?.value || "").trim() || null,
            longDescription: (qs("#txtProdLong")?.value || "").trim() || null,
            highlights: (qs("#txtProdHighlights")?.value || "").trim() || null,
            weightKg: Number(qs("#txtProdWeightKg")?.value || "") || null,
            dimensions: (qs("#txtProdDimensions")?.value || "").trim() || null
        };

        setSubmitting(true);

        try {
            let productId = state.editingId;

            if (productId) {
                await fetchAdminJson(`/api/admin/products/${productId}`, {
                    method: "PUT",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload)
                });
            } else {
                const created = await fetchAdminJson("/api/admin/products", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload)
                });
                if (!created?.ok) throw new Error(created?.message || "Création échouée.");
                productId = created.id;
                if (!productId) throw new Error("ID produit manquant.");
            }

            // ✅ variants (new system)
            if (state.variantAttrs.length && state.variants.length) {
                await saveVariants(productId);
            }

            // ✅ dynamic attrs
            await saveDynamicAttrs(productId);

            // ✅ images
            const files = Array.from(state.dt.files || []);
            if (files.length) {
                if (state.editingId) await deleteAllImages(productId);
                await uploadProductImages(productId, files);
                state.thumbCache.delete(productId);
                resetQueue();
            }

            openModal("ok", state.editingId ? "Produit modifié ✅" : "Produit créé ✅", `ID: ${productId}`);
            await loadProducts();
            resetForm();
        } catch (e) {
            console.error(e);
            openModal("err", "Erreur", e?.message || String(e));
        } finally {
            setSubmitting(false);
        }
    }

    async function deleteProduct(id) {
        const pid = Number(id || 0);
        if (!pid) return;
        if (!confirm("Désactiver ce produit ?")) return;

        try {
            await fetchAdminJson(`/api/admin/products/${pid}`, { method: "DELETE" });
            showMsg("ok", "Produit désactivé ✅");
            await loadProducts();
            if (state.editingId === pid) resetForm();
        } catch (e) {
            console.error(e);
            openModal("err", "Suppression impossible", e?.message || String(e));
        }
    }

    async function loadProducts() {
        const box = qs("#productsList");
        if (!box) return;

        if (state.loadingProducts) return;
        state.loadingProducts = true;

        const categoryId = getSelectedCategoryId();
        const params = new URLSearchParams();
        if (categoryId) params.set("categoryId", String(categoryId));

        const url = `/api/admin/products${params.toString() ? "?" + params.toString() : ""}`;
        box.style.opacity = "0.35";
        box.style.pointerEvents = "none";

        try {
            const data = await fetchAdminJson(url);
            const items = Array.isArray(data?.items) ? data.items : [];

            if (!items.length) {
                box.innerHTML = `<div class="muted">Aucun produit.</div>`;
                return;
            }

            box.innerHTML = `
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th style="width:70px">ID</th>
                <th style="width:70px">Photo</th>
                <th>Nom</th>
                <th style="width:140px">Prix</th>
                <th style="width:140px">Promo</th>
                <th style="width:110px">Stock</th>
                <th style="width:90px">Actif</th>
                <th style="width:140px">Actions</th>
              </tr>
            </thead>
            <tbody>
             ${items.map(p0 => {
                 const p = {
                     id: (p0.id ?? p0.Id),
                     name: (p0.name ?? p0.Name ?? ""),
                     price: (p0.price ?? p0.Price ?? 0),
                     pricePromo: (p0.pricePromo ?? p0.PricePromo ?? null),
                     stock: (p0.stock ?? p0.Stock ?? 0),
                     isActive: (p0.isActive ?? p0.IsActive ?? false),
                 };

                 return `
    <tr class="product-row ${p.isActive ? "" : "is-off"}" data-id="${p.id}">
      <td>${p.id}</td>
      <td><img class="prod-thumb" data-thumb-pid="${p.id}" src="/assets/placeholder.png" alt="photo"></td>
      <td><b>${esc(p.name)}</b></td>
      <td>${money(p.price)}</td>
      <td>${(p.pricePromo && Number(p.pricePromo) > 0) ? money(p.pricePromo) : "-"}</td>
      <td>${Number(p.stock || 0)}</td>
      <td>${p.isActive ? "✅" : "⛔"}</td>
<td class="actions-cell">
  <div class="prod-actions">
    <button class="btn-mini" data-act="edit" data-id="${p.id}" title="Modifier">✏️</button>
    <button class="btn-mini" data-act="copy" data-id="${p.id}" title="Copier">📄</button>
    <button class="btn-mini danger" data-act="del" data-id="${p.id}" title="Supprimer">🗑️</button>
  </div>
</td>
    </tr>
  `;
             }).join("")}

            </tbody>
          </table>
        </div>
      `;

            hydrateProductsThumbs(items);
        } catch (e) {
            console.error(e);
            box.innerHTML = `<div class="muted">❌ Erreur: ${esc(e?.message || String(e))}</div>`;
        } finally {
            box.style.opacity = "1";
            box.style.pointerEvents = "";
            state.loadingProducts = false;
        }
    }

    function safeAbsUrl(u) { try { if (typeof absUrl === "function") return absUrl(u); } catch { } return u; }
    function pickMainUrl(images) {
        if (!Array.isArray(images) || !images.length) return null;
        const main = images.find(x => (x.IsMain || x.isMain)) || images[0];
        return main ? (main.Url || main.url) : null;
    }

    async function hydrateProductsThumbs(items) {
        const CONC = 6;
        const queue = items.slice();

        async function worker() {
            while (queue.length) {
                const p = queue.shift();
                const pid = Number(p?.id || 0);
                if (!pid) continue;

                const imgEl = qs(`img[data-thumb-pid="${pid}"]`);
                if (!imgEl) continue;

                if (state.thumbCache.has(pid)) {
                    imgEl.src = state.thumbCache.get(pid);
                    continue;
                }

                try {
                    const data = await fetchAdminJson(`/api/admin/products/${pid}`);
                    const it = data?.item;
                    const images = it?.Images || it?.images || [];
                    const url = pickMainUrl(images);

                    let finalUrl = url ? safeAbsUrl(url) : "/assets/placeholder.png";
                    if (finalUrl && !finalUrl.includes("placeholder.png")) {
                        finalUrl += (finalUrl.includes("?") ? "&" : "?") + "t=" + Date.now();
                    }

                    state.thumbCache.set(pid, finalUrl);
                    imgEl.src = finalUrl;
                } catch {
                    imgEl.src = "/assets/placeholder.png";
                }
            }
        }

        await Promise.all(Array.from({ length: CONC }, () => worker()));
    }
    function bindGalleryActions() {
        const box = qs("#imgGallery");
        if (!box || box.dataset.bound === "1") return;
        box.dataset.bound = "1";

        box.addEventListener("click", async (e) => {
            const btn = e.target.closest("button[data-act]");
            if (!btn) return;

            e.preventDefault();
            e.stopPropagation();

            const act = btn.dataset.act;
            const pid = Number(btn.dataset.pid || 0);
            const imgId = Number(btn.dataset.imgid || 0);

            if (!pid || !imgId) return;

            if (act === "mainImg") {
                await setMainImage(pid, imgId);
                await loadProductIntoForm(pid);
                showMsg("ok", "Image principale mise à jour ✅");
                return;
            }

            if (act === "delImg") {
                if (!confirm("Supprimer cette image ?")) return;
                await deleteOneImage(pid, imgId);
                await loadProductIntoForm(pid);
                showMsg("ok", "Image supprimée ✅");
                return;
            }
        }, true);
    }

    // ========= Bindings =========
    function bindModalOnce() {
        const modal = qs("#appModal");
        if (!modal || modal.dataset.bound === "1") return;
        modal.dataset.bound = "1";

        modal.addEventListener("click", (e) => {
            if (e.target.closest("[data-act='close']")) closeModal();
        });
        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape") closeModal();
        });
    }

    function bindVariantsUI() {
        const gen = qs("#btnGenVariants");
        const save = qs("#btnSaveVariants");
        const tb = qs("#variantsTbody");

        if (gen && gen.dataset.bound !== "1") {
            gen.dataset.bound = "1";
            gen.addEventListener("click", (e) => {
                e.preventDefault();
                generateVariants();
            });
        }

        if (tb && tb.dataset.bound !== "1") {
            tb.dataset.bound = "1";
            tb.addEventListener("input", (e) => {
                const inp = e.target.closest(".inpVarStock");
                if (!inp) return;
                const idx = Number(inp.dataset.idx);
                const v = state.variants[idx];
                if (!v) return;
                v.stock = Math.max(0, Number(inp.value || 0) || 0);
                recalcTotalStock();
            }, true);
        }

        if (save && save.dataset.bound !== "1") {
            save.dataset.bound = "1";
            save.addEventListener("click", async (e) => {
                e.preventDefault();
                const productId = state.editingId || state.lastProductId;
                if (!productId) return showMsg("err", "Sélectionne un produit d'abord.");
                try {
                    await saveVariants(productId);
                    showMsg("ok", "Variantes enregistrées ✅");
                    await loadProducts();
                } catch (err) {
                    console.error(err);
                    showMsg("err", "Erreur enregistrement variantes", err?.message || String(err));
                }
            });
        }
    }

    function bindTagsUI() {
        const inp1 = qs("#inpSize");
        const inp2 = qs("#inpColor");

        if (inp1 && inp1.dataset.bound !== "1") {
            inp1.dataset.bound = "1";
            inp1.addEventListener("keydown", (e) => {
                if (e.key === "Enter") { e.preventDefault(); addOpt(1, inp1.value); }
            });
            inp1.addEventListener("blur", () => { if (inp1.value.trim()) addOpt(1, inp1.value); });
        }

        if (inp2 && inp2.dataset.bound !== "1") {
            inp2.dataset.bound = "1";
            inp2.addEventListener("keydown", (e) => {
                if (e.key === "Enter") { e.preventDefault(); addOpt(2, inp2.value); }
            });
            inp2.addEventListener("blur", () => { if (inp2.value.trim()) addOpt(2, inp2.value); });
        }
    }

    function bindProductsList() {
        const box = qs("#productsList");
        if (!box || box.dataset.bound === "1") return;
        box.dataset.bound = "1";

        box.addEventListener("click", async (e) => { // ✅ async obligatoire
            const actEl = e.target.closest("[data-act]");
            if (actEl) {
                e.preventDefault();
                e.stopPropagation();

                const act = String(actEl.dataset.act || "");
                const id = Number(actEl.dataset.id || 0);

                console.log("ACTION CLICK:", { act, id, el: actEl });

                if (!id && (act === "edit" || act === "copy" || act === "del")) {
                    openModal("err", "Erreur", "ID produit introuvable sur le bouton.");
                    return;
                }

                if (act === "edit") {
                    state.lastProductId = id;
                    setEditMode(id);
                    await loadProductIntoForm(id);
                    await copyProductImagesToQueue(id);

                    showMsg("ok", "Produit chargé ✅", "Tu peux modifier puis enregistrer.");
                    window.scrollTo({ top: 0, behavior: "smooth" });
                    return;
                }

                if (act === "del") {
                    await deleteProduct(id);
                    return;
                }

                if (act === "copy") {
                    // ✅ Copier = charger dans le formulaire et repasser en mode création (SANS appel API)
                    state.lastProductId = id;

                    await loadProductIntoForm(id);

                    setEditMode(null);      // mode création
                    state.copyMode = true;

                    // SKU obligatoire à la création => on le vide
                    const skuEl = qs("#txtProdSku");
                    if (skuEl) skuEl.value = "";

                    // Optionnel : suffixe au nom
                    const nameEl = qs("#txtProdName");
                    if (nameEl && nameEl.value && !nameEl.value.includes("(copie)")) {
                        nameEl.value = nameEl.value.trim() + " (copie)";
                    }

                    showMsg("ok", "Copie prête ✅", "Modifie puis clique sur Créer (SKU obligatoire).");
                    window.scrollTo({ top: 0, behavior: "smooth" });
                    skuEl?.focus();
                    return;
                }

                return; // ✅ important : on ne déclenche pas le clic ligne si bouton action
            }

            // ✅ Click ligne (sélection)
            const row = e.target.closest("tr.product-row");
            if (!row) return;

            const pid = Number(row.dataset.id || 0);
            if (!pid) return;

            state.lastProductId = pid;
            setEditMode(pid);
            await loadProductIntoForm(pid);
            showMsg("ok", "Produit sélectionné ✅");
            window.scrollTo({ top: 0, behavior: "smooth" });
        }, true);
    }



            



    // ========= Init =========
    window.initAdminProductsPage = async function () {
        console.log("✅ initAdminProductsPage (NEW) START");


        if (!state.bound) {
            state.bound = true;
            bindGalleryActions();

            bindModalOnce();
            bindQueue();
            bindTagsUI();
            bindVariantsUI();
            bindProductsList();

            qs("#txtProdName")?.addEventListener("input", () => {
                updateNameCount();
            });

            qs("#btnProdCreate")?.addEventListener("click", async (e) => {
                e.preventDefault();
                try { await createOrUpdateProduct(); }
                catch (err) { console.error(err); showMsg("err", err?.message || String(err)); }
            });

            qs("#btnProdReset")?.addEventListener("click", async (e) => {
                e.preventDefault();
                resetForm();
                await applyCategoryRules();
            });

            qs("#selProdCategory")?.addEventListener("change", async () => {
                await applyCategoryRules();
                await loadProducts();
            });
        }

        document.addEventListener("DOMContentLoaded", () => {
    loadVendors();
});
        await loadCategoriesIntoSelect();
        await loadVendorsIntoSelect();
        await applyCategoryRules();
        await loadProducts();
        updateNameCount();

        // ✅ Auto-open product from URL: /admin-products.html?id=25
        const urlId = Number(getQueryParam("id") || 0);
        if (urlId > 0) {
            try {
                state.lastProductId = urlId;
                setEditMode(urlId);
                await loadProductIntoForm(urlId);
                showMsg("ok", "Produit ouvert ✅", `ID: ${urlId}`);
                window.scrollTo({ top: 0, behavior: "smooth" });
            } catch (e) {
                console.error(e);
                openModal("err", "Impossible d’ouvrir le produit", e?.message || String(e));
            }
        }

        renderQueuePreview();
        renderVariantsTable();
    };

})();
