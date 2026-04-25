// /assets/js/vendor-product-create-pro.js
(() => {
    "use strict";
    const PRODUCT_NAME_MAX = 60;

    function truncateText(text, max) {
        const s = String(text || "").trim();
        if (s.length <= max) return s;
        return s.slice(0, max).trim() + "…";
    }

    function updateNameCount() {
        const input = qs("#name");
        const box = qs("#nameCount");
        if (!input || !box) return;

        const len = String(input.value || "").length;
        box.textContent = `${len} / ${PRODUCT_NAME_MAX}`;
        box.style.color = len > PRODUCT_NAME_MAX ? "#fca5a5" : "";
    }

    // ========= Helpers =========
    const qs = (s, r = document) => r.querySelector(s);
    const qsa = (s, r = document) => Array.from(r.querySelectorAll(s));


    const esc = (s) =>
        String(s ?? "").replace(/[&<>"']/g, (c) => ({
            "&": "&amp;",
            "<": "&lt;",
            ">": "&gt;",
            '"': "&quot;",
            "'": "&#039;",
        }[c]));

    const money = (n) => (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";

    function showMsg(type, title, desc = "") {
        const box = qs("#msg");
        if (!box) return;
        box.style.display = "block";
        box.className = "vp-msg " + (type === "ok" ? "ok" : "err");
        box.innerHTML = `
      <div style="font-weight:900;margin-bottom:${desc ? "4px" : "0"}">${esc(title)}</div>
      ${desc ? `<div>${esc(desc)}</div>` : ""}`;
    }

    function clearMsg() {
        const box = qs("#msg");
        if (!box) return;
        box.style.display = "none";
        box.className = "vp-msg";
        box.textContent = "";
    }

    // ========= API wrappers =========
    async function fetchVendor(url, opts = {}) {
        if (typeof window.fetchVendorJson === "function") return await window.fetchVendorJson(url, opts);

        const res = await fetch(url, { ...opts, cache: "no-store" });
        const raw = await res.text();
        let data = null;
        try { data = raw ? JSON.parse(raw) : null; } catch { }
        if (!res.ok) throw new Error((data && (data.message || data.Message)) || raw || "Erreur serveur.");
        return data;
    }

    async function fetchPublic(url) {
        const fn = window._fetchJson || window.fetchJson;
        if (typeof fn === "function") return await fn(url, { cache: "no-store" });

        const res = await fetch(url, { cache: "no-store" });
        const raw = await res.text();
        let data = null;
        try { data = raw ? JSON.parse(raw) : null; } catch { }
        if (!res.ok) throw new Error(raw || "Erreur public.");
        return data;
    }

    // ========= State =========
    const state = {
        submitting: false,

        categories: [],
        catById: new Map(),
        catChildren: new Map(),
        catPathById: new Map(),

        catAttrRows: [],
        variantAttrs: [], // [{ attribute, ...rowProps }]

        dynMeta: new Map(), // attributeId -> { required, type, name }

        opt1: [],
        opt2: [],
        variants: [], // [{opt1,opt2,stock}]
    };
    let EDIT_ID = null;

    function getQueryId() {
        const u = new URL(location.href);
        const id = u.searchParams.get("id");
        const n = id ? Number(id) : null;
        return Number.isFinite(n) && n > 0 ? n : null;
    }

    // ⚠️ pour afficher les images déjà existantes (URLs)
    // ⚠️ pour afficher les images déjà existantes (URLs)
    const existingImgState = {
        urls: []
    };

    // ========= Images (preview + queue) =========
    const imgState = {
        files: [],
        urls: []
    };

    function revokeImgUrls() {
        for (const u of imgState.urls) {
            try { URL.revokeObjectURL(u); } catch { }
        }
        imgState.urls = [];
    }

    function setGalCount() {
        const el = qs("#galCount");
        if (!el) return;
        const total = (existingImgState.urls?.length || 0) + (imgState.files?.length || 0);
        el.textContent = `${total} / 6`;
    }

    function renderExistingImagesUI() {
        renderImagesUI();
    }

    function renderImagesUI() {
        const prev = qs("#prevImg");
        const box = qs("#imgQueue");
        if (!prev || !box) return;

        revokeImgUrls();

        const existing = existingImgState.urls || [];
        const fresh = imgState.files || [];

        const total = existing.length + fresh.length;
        setGalCount();

        if (!total) {
            prev.src = "/assets/placeholder.png";
            box.innerHTML = "";
            return;
        }

        if (existing.length) {
            prev.src = existing[0];
        } else {
            const u0 = URL.createObjectURL(fresh[0]);
            imgState.urls.push(u0);
            prev.src = u0;
        }

        const existingHtml = existing.map((u, idx) => `
    <div class="vp-qCard" data-kind="existing" data-idx="${idx}">
        <img src="${esc(u)}" alt="">
        ${idx === 0 && !fresh.length ? `<span class="vp-qBadge">Principale</span>` : ``}
        <button type="button" class="vp-qDel" title="Supprimer">×</button>
    </div>
`).join("");

        const freshHtml = fresh.map((f, idx) => {
            const u = URL.createObjectURL(f);
            imgState.urls.push(u);

            const isMain = !existing.length && idx === 0;

            return `
            <div class="vp-qCard" data-kind="new" data-idx="${idx}">
                <img src="${u}" alt="">
                ${isMain ? `<span class="vp-qBadge">Principale</span>` : ``}
                <button type="button" class="vp-qDel" title="Supprimer">×</button>
            </div>
        `;
        }).join("");

        box.innerHTML = existingHtml + freshHtml;

        qsa(".vp-qCard[data-kind='new'] .vp-qDel", box).forEach(btn => {
            btn.addEventListener("click", () => {
                const card = btn.closest(".vp-qCard");
                const idx = Number(card?.dataset?.idx || -1);
                if (idx < 0) return;

                imgState.files.splice(idx, 1);
                renderImagesUI();
            });
        });

        qsa(".vp-qCard[data-kind='existing'] .vp-qDel", box).forEach(btn => {
            btn.addEventListener("click", async () => {
                const card = btn.closest(".vp-qCard");
                const idx = Number(card?.dataset?.idx || -1);
                if (idx < 0 || !EDIT_ID) return;

                if (!confirm("Supprimer cette image ?")) return;

                try {
                    const url = existingImgState.urls[idx];
                    await fetchVendor(`/api/vendor/products/${EDIT_ID}/images/by-url`, {
                        method: "DELETE",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({ url })
                    });

                    existingImgState.urls.splice(idx, 1);
                    renderImagesUI();
                    showMsg("ok", "Image supprimée ✅");
                } catch (e) {
                    showMsg("bad", "Suppression impossible", e?.message || String(e));
                }
            });
        });

        qsa(".vp-qCard[data-kind='new'] img", box).forEach(img => {
            img.addEventListener("click", () => {
                const card = img.closest(".vp-qCard");
                const idx = Number(card?.dataset?.idx || -1);
                if (idx <= 0) return;

                const [picked] = imgState.files.splice(idx, 1);
                imgState.files.unshift(picked);
                renderImagesUI();
            });
        });
    }

    function bindImagesInput() {
        const input = qs("#fileProdImages");
        if (!input || input.dataset.bound === "1") return;
        input.dataset.bound = "1";

        input.addEventListener("change", (e) => {
            const list = Array.from(e.target.files || []);
            if (!list.length) return;

            const merged = [...imgState.files, ...list];

            const seen = new Set();
            imgState.files = merged.filter(f => {
                const key = `${f.name}|${f.size}|${f.lastModified}`;
                if (seen.has(key)) return false;
                seen.add(key);
                return true;
            }).slice(0, 6);

            renderImagesUI();
            input.value = "";
        });
    }

    // ========= Preview =========
    function setPreview() {
        const name = (qs("#name")?.value || "").trim();
        const price = Number(String(qs("#price")?.value || "").trim().replace(",", ".") || 0);
        const stock = Number(String(qs("#stock")?.value || "").trim() || 0);

        const brand = (qs("#brand")?.value || "").trim();

        const shortDesc = (qs("#txtProdShort")?.value || "").trim();
        const desc = (qs("#txtProdDesc")?.value || "").trim();
        const highlights = (qs("#txtProdHighlights")?.value || "").trim();
        const previewDesc = shortDesc || desc || highlights;

        qs("#prevName") && (qs("#prevName").textContent = truncateText(name || "Nom du produit", PRODUCT_NAME_MAX));
        qs("#prevPrice") && (qs("#prevPrice").textContent = money(price || 0));
        qs("#prevStock") && (qs("#prevStock").textContent = `Stock: ${stock || 0}`);
        qs("#prevBrand") && (qs("#prevBrand").textContent = `Marque: ${brand || "—"}`);
        qs("#prevSku") && (qs("#prevSku").textContent = `SKU: ${(qs("#sku")?.value || "…").trim() || "…"}`);
        qs("#prevDesc") && (qs("#prevDesc").textContent = previewDesc || "Description…");

        const catId = getSelectedCategoryId();
        const label = catId ? (state.catPathById.get(catId) || "") : "…";
        qs("#prevCat") && (qs("#prevCat").textContent = `Catégorie: ${label || "…"}`);

        const pricePromo = Number(String(qs("#pricePromo")?.value || "").trim().replace(",", ".") || 0);

        if (qs("#prevPrice")) {
            if (pricePromo > 0 && pricePromo < price) {
                qs("#prevPrice").textContent = `${money(pricePromo)} (Promo)`;
            } else {
                qs("#prevPrice").textContent = money(price || 0);
            }
        }
    }

    // ========= Categories =========
    function normCat(c) {
        return {
            id: Number(c?.id ?? c?.Id ?? 0) || 0,
            name: String(c?.name ?? c?.Name ?? "").trim(),
            parentId: (c?.parentId ?? c?.ParentId) == null ? null : Number(c?.parentId ?? c?.ParentId),
            isActive: (c?.isActive ?? c?.IsActive ?? true) === true,
        };
    }

    function buildCategoryMaps(items) {
        state.categories = (items || []).map(normCat).filter((x) => x.id && x.name);
        state.catById = new Map(state.categories.map((c) => [c.id, c]));

        state.catChildren = new Map();
        for (const c of state.categories) {
            const pid = c.parentId ?? 0;
            if (!state.catChildren.has(pid)) state.catChildren.set(pid, []);
            state.catChildren.get(pid).push(c);
        }

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

        state.catPathById = new Map();
        for (const c of state.categories) state.catPathById.set(c.id, computePath(c.id));
    }

    function isLeafCategory(id) {
        for (const c of state.categories) if (c.parentId === id) return false;
        return true;
    }

    function getSelectedCategoryId() {
        const sel = qs("#selProdCategory");
        const id = Number(sel?.value || 0);
        return Number.isFinite(id) && id > 0 ? id : null;
    }

    async function loadCategoriesIntoSelect() {
        const sel = qs("#selProdCategory");
        if (!sel) return;

        sel.innerHTML = `<option value="">Chargement…</option>`;

        const data = await fetchPublic(`/api/categories?ts=${Date.now()}`);
        const items = Array.isArray(data) ? data : (data?.items || []);
        if (!items.length) {
            sel.innerHTML = `<option value="">⚠️ Aucune catégorie</option>`;
            return;
        }

        buildCategoryMaps(items);

        const leaves = state.categories
            .filter((c) => c.isActive && isLeafCategory(c.id))
            .sort((a, b) => (state.catPathById.get(a.id) || "").localeCompare(state.catPathById.get(b.id) || ""));

        if (!leaves.length) {
            sel.innerHTML = `<option value="">⚠️ Aucune sous-catégorie active</option>`;
            return;
        }

        sel.innerHTML =
            `<option value="">— Choisir une catégorie —</option>` +
            leaves.map((c) => `<option value="${c.id}">${esc(state.catPathById.get(c.id) || c.name)}</option>`).join("");
    }

    // ========= Category Attributes =========
    function normOpt(o) {
        return {
            id: Number(o?.id ?? o?.Id ?? 0) || 0,
            value: String(o?.value ?? o?.Value ?? "").trim(),
            isActive: (o?.isActive ?? o?.IsActive ?? true) === true,
            sortOrder: Number(o?.sortOrder ?? o?.SortOrder ?? 0) || 0,
        };
    }

    function normAttr(a) {
        return {
            id: Number(a?.id ?? a?.Id ?? 0) || 0,
            code: String(a?.code ?? a?.Code ?? "").trim(),
            name: String(a?.name ?? a?.Name ?? "").trim(),
            dataType: String(a?.dataType ?? a?.DataType ?? "Text"),
            isVariant: (a?.isVariant ?? a?.IsVariant ?? false) === true,
            options: (a?.options ?? a?.Options ?? []).map(normOpt).filter((x) => x.id && x.value),
        };
    }

    // ✅ IMPORTANT : on lit AUSSI IsVariant du mapping (CategoryAttributes)
    function normCatAttrRow(r) {
        const attribute = normAttr(r?.attribute ?? r?.Attribute);
        const rowIsVariant = (r?.isVariant ?? r?.IsVariant) === true; // venant de CategoryAttributes

        return {
            attributeId: Number(r?.attributeId ?? r?.AttributeId ?? 0) || attribute.id || 0,
            isRequired: (r?.isRequired ?? r?.IsRequired ?? false) === true,
            isFilterable: (r?.isFilterable ?? r?.IsFilterable ?? false) === true,
            sortOrder: Number(r?.sortOrder ?? r?.SortOrder ?? 0) || 0,

            // ✅ priorité au mapping, sinon Attribute.IsVariant
            isVariant: rowIsVariant || attribute.isVariant === true,

            attribute,
        };
    }

    async function fetchCategoryAttributes(catId) {
        const data = await fetchPublic(`/api/categories/${catId}/attributes?ts=${Date.now()}`);
        const rows = Array.isArray(data) ? data : (data?.items || data?.rows || []);
        return (rows || []).map(normCatAttrRow).sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0));
    }

    // ========= Dynamic attributes UI =========
    function renderDynamicAttributesUI(rows) {
        const box = qs("#dynAttrsBox");
        if (!box) return;

        state.dynMeta = new Map();

        // ✅ non-variants = ceux dont row.isVariant = false
        const nonVariants = (rows || []).filter((r) => r.isVariant !== true);

        if (!nonVariants.length) {
            box.innerHTML = `<div class="muted">Aucun attribut pour cette catégorie.</div>`;
            return;
        }

        box.innerHTML = nonVariants.map((r) => {
            const a = r.attribute;
            const required = !!r.isRequired;
            const reqMark = required ? ` <span style="color:#86efac">*</span>` : "";
            const type = String(a.dataType || "Text").toLowerCase();

            state.dynMeta.set(a.id, { required, type, name: a.name });

            if (type === "option") {
                const opts = (a.options || [])
                    .slice()
                    .sort((x, y) => (x.sortOrder || 0) - (y.sortOrder || 0))
                    .map((o) => `<option value="${o.id}">${esc(o.value)}</option>`)
                    .join("");

                return `
          <div class="field">
            <label>${esc(a.name)}${reqMark}</label>
            <select data-attr-id="${a.id}" data-attr-type="option" ${required ? "data-required='1'" : ""}>
              <option value="">— Choisir —</option>
              ${opts}
            </select>
          </div>
        `;
            }

            return `
        <div class="field">
          <label>${esc(a.name)}${reqMark}</label>
          <input data-attr-id="${a.id}" data-attr-type="text" ${required ? "data-required='1'" : ""} placeholder="${esc(a.name)}" />
        </div>
      `;
        }).join("");

        // auto-select si une seule option
        for (const sel of qsa("select[data-attr-type='option']", box)) {
            const options = qsa("option", sel).filter((o) => o.value && o.value !== "");
            if (options.length === 1) sel.value = options[0].value;
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
                const optionId = Number(el.value || 0) || null;
                if (optionId) items.push({ attributeId, optionId });
            } else {
                const val = String(el.value || "").trim();
                if (val) items.push({ attributeId, valueText: val });
            }
        }
        return items;
    }

    function validateDynamicRequired() {
        const box = qs("#dynAttrsBox");
        if (!box) return { ok: true };

        const requiredEls = qsa("[data-required='1'][data-attr-id]", box);
        for (const el of requiredEls) {
            const id = Number(el.dataset.attrId || 0);
            const meta = state.dynMeta.get(id);
            const label = meta?.name || `Attribut ${id}`;

            if (el.tagName === "SELECT") {
                if (!String(el.value || "").trim()) return { ok: false, message: `Choisis: ${label}` };
            } else {
                if (!String(el.value || "").trim()) return { ok: false, message: `Renseigne: ${label}` };
            }
        }
        return { ok: true };
    }

    async function saveDynamicAttrs(productId) {
        const items = collectDynamicAttrs();
        return await fetchVendor(`/api/vendor/products/${productId}/attributes`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ items }),
        });
    }

    // ========= Variants =========
    function normalizeValue(v) { return String(v ?? "").trim(); }
    function uniqLower(list) {
        const out = [];
        const seen = new Set();
        for (const x of list || []) {
            const s = normalizeValue(x);
            if (!s) continue;
            const k = s.toLowerCase();
            if (seen.has(k)) continue;
            seen.add(k);
            out.push(s);
        }
        return out;
    }
    function setDatalistOptions(datalistSel, values) {
        const dl = qs(datalistSel);
        if (!dl) return;
        dl.innerHTML = (values || []).map(v => `<option value="${esc(v)}"></option>`).join("");
    }
    function setVariantLabels(opt1Label, opt2Label) {
        qs("#lblSizes") && (qs("#lblSizes").textContent = opt1Label || "Option 1");
        qs("#lblColors") && (qs("#lblColors").textContent = opt2Label || "Option 2");
        qs("#thVar1") && (qs("#thVar1").textContent = opt1Label || "Option 1");
        qs("#thVar2") && (qs("#thVar2").textContent = opt2Label || "Option 2");
    }
    function setVariantUIEnabled(enabled, hasSecond) {
        const sizesBlock = qs("#sizesBlock");
        const colorsBlock = qs("#colorsBlock");
        const hint = qs("#variantsHint");
        const wrap = qs("#variantsTableWrap");

        if (!enabled) {
            sizesBlock && (sizesBlock.style.display = "none");
            colorsBlock && (colorsBlock.style.display = "none");
            wrap && (wrap.style.display = "none");
            hint && (hint.textContent = "Aucune variante pour cette catégorie.");
            state.opt1 = [];
            state.opt2 = [];
            state.variants = [];
            return;
        }

        sizesBlock && (sizesBlock.style.display = "");
        colorsBlock && (colorsBlock.style.display = hasSecond ? "" : "none");
        hint && (hint.textContent = hasSecond
            ? "Ajoute les valeurs puis clique sur Générer variantes."
            : "Une seule option de variante : ajoute les valeurs puis Générer variantes."
        );

        const th2 = qs("#thVar2");
        if (th2) th2.style.display = hasSecond ? "" : "none";
    }

    function renderTags(boxSel, inputSel, values, onRemove) {
        const box = qs(boxSel);
        const inp = qs(inputSel);
        if (!box || !inp) return;

        box.innerHTML = "";
        for (const v of values || []) {
            const tag = document.createElement("span");
            tag.className = "tag";
            tag.innerHTML = `<span>${esc(v)}</span><button type="button" aria-label="Supprimer">×</button>`;
            tag.querySelector("button").addEventListener("click", () => onRemove?.(v));
            box.appendChild(tag);
        }
        box.appendChild(inp);
    }

    function removeOpt(which, value) {
        const v = String(value || "");
        if (which === 1) state.opt1 = state.opt1.filter(x => x.toLowerCase() !== v.toLowerCase());
        if (which === 2) state.opt2 = state.opt2.filter(x => x.toLowerCase() !== v.toLowerCase());
        renderTags("#sizesBox", "#inpSize", state.opt1, (x) => removeOpt(1, x));
        renderTags("#colorsBox", "#inpColor", state.opt2, (x) => removeOpt(2, x));
    }

    function renderVariantsTable() {
        const wrap = qs("#variantsTableWrap");
        const tb = qs("#variantsTbody");
        if (!wrap || !tb) return;

        const hasSecond = state.variantAttrs.length > 1;

        const th2 = qs("#thVar2");
        if (th2) th2.style.display = hasSecond ? "" : "none";

        if (!state.variants.length) {
            wrap.style.display = "none";
            tb.innerHTML = "";
            return;
        }

        wrap.style.display = "";
        tb.innerHTML = state.variants.map((v, idx) => `
      <tr data-idx="${idx}">
        <td>${esc(v.opt1)}</td>
        ${hasSecond ? `<td>${esc(v.opt2)}</td>` : ``}
        <td>
          <input type="number" min="0" value="${Number(v.stock || 0)}"
            class="inpVarStock" data-idx="${idx}" style="width:140px">
        </td>
      </tr>
    `).join("");

        qsa(".inpVarStock", tb).forEach(inp => {
            inp.addEventListener("input", () => {
                const i = Number(inp.dataset.idx || 0);
                const val = Math.max(0, Number(inp.value || 0) || 0);
                if (state.variants[i]) state.variants[i].stock = val;
                refreshTotalStockFromVariants();
            });
        });

        refreshTotalStockFromVariants();
    }

    function refreshTotalStockFromVariants() {
        const total = (state.variants || []).reduce((sum, x) => sum + (Number(x.stock || 0) || 0), 0);
        const stockEl = qs("#stock");
        if (stockEl) stockEl.value = String(total);
        setPreview();
    }

    function genVariants() {
        const hasSecond = state.variantAttrs.length > 1;

        if (!state.opt1.length) return showMsg("bad", "Ajoute au moins une valeur dans Option 1.");
        if (hasSecond && !state.opt2.length) return showMsg("bad", "Ajoute au moins une valeur dans Option 2.");

        const variants = [];
        if (!hasSecond) {
            for (const a of state.opt1) variants.push({ opt1: a, opt2: "", stock: 0 });
        } else {
            for (const a of state.opt1) for (const b of state.opt2) variants.push({ opt1: a, opt2: b, stock: 0 });
        }

        state.variants = variants;
        renderVariantsTable();
        clearMsg();
    }

    function clearVariants() {
        state.opt1 = [];
        state.opt2 = [];
        state.variants = [];
        renderTags("#sizesBox", "#inpSize", state.opt1, (x) => removeOpt(1, x));
        renderTags("#colorsBox", "#inpColor", state.opt2, (x) => removeOpt(2, x));
        renderVariantsTable();
        refreshTotalStockFromVariants();
    }

    function extractVariantAttrs(rows) {
        // ✅ on prend les rows où row.isVariant = true
        const vars = (rows || []).filter(r => r.isVariant === true);

        // role detection (Taille/Couleur)
        function role(a) {
            const code = String(a.code || "").toLowerCase();
            const name = String(a.name || "").toLowerCase();
            const txt = `${code} ${name}`;

            if (txt.includes("color") || txt.includes("couleur")) return "color";
            if (txt.includes("size") || txt.includes("taille") || txt.includes("pointure")) return "size";
            return "other";
        }

        const sizeOnes = vars.filter(r => role(r.attribute) === "size");
        const colorOnes = vars.filter(r => role(r.attribute) === "color");
        const others = vars.filter(r => role(r.attribute) === "other");

        const ordered = [];
        if (sizeOnes[0]) ordered.push(sizeOnes[0]);
        if (colorOnes[0]) ordered.push(colorOnes[0]);
        for (const r of others) {
            if (ordered.length >= 2) break;
            if (!ordered.includes(r)) ordered.push(r);
        }
        return (ordered.length ? ordered : vars).slice(0, 2);
    }

    function applyVariantsFromAttributes(variantRows) {
        state.variantAttrs = (variantRows || []).slice(0, 2);

        if (!state.variantAttrs.length) {
            setVariantUIEnabled(false, false);
            return;
        }

        const r1 = state.variantAttrs[0];
        const r2 = state.variantAttrs[1] || null;

        const a1 = r1.attribute;
        const a2 = r2 ? r2.attribute : null;

        setVariantLabels(a1?.name || "Option 1", a2?.name || "");
        setVariantUIEnabled(true, !!a2);

        const s1 = (a1?.options || []).map(o => o.value).filter(Boolean);
        const s2 = a2 ? (a2.options || []).map(o => o.value).filter(Boolean) : [];

        setDatalistOptions("#dlSizes", s1);
        setDatalistOptions("#dlColors", s2);

        state.opt1 = uniqLower(s1);
        state.opt2 = a2 ? uniqLower(s2) : [];

        renderTags("#sizesBox", "#inpSize", state.opt1, (v) => removeOpt(1, v));
        renderTags("#colorsBox", "#inpColor", state.opt2, (v) => removeOpt(2, v));
    }

    async function applyCategoryRules() {
        const catId = getSelectedCategoryId();
        if (!catId) return;

        try {
            const rows = await fetchCategoryAttributes(catId);

            state.catAttrRows = rows;

            renderDynamicAttributesUI(rows);

            const variantRows = extractVariantAttrs(rows);
            applyVariantsFromAttributes(variantRows);

            setPreview();
            clearMsg();
        } catch (e) {
            state.catAttrRows = [];
            state.variantAttrs = [];
            renderDynamicAttributesUI([]);
            setVariantUIEnabled(false, false);
            showMsg("bad", "Attributs non chargés", e?.message || String(e));
        }
    }


    function fillDynamicAttrsUIFromSaved(savedAttrs) {
        const box = qs("#dynAttrsBox");
        if (!box) return;

        const byAttr = new Map();
        for (const a of (savedAttrs || [])) {
            const attributeId = Number(a.attributeId ?? a.AttributeId ?? 0);
            if (!attributeId) continue;
            byAttr.set(attributeId, {
                optionId: Number(a.optionId ?? a.OptionId ?? 0) || null,
                valueText: (a.valueText ?? a.ValueText ?? "") || null
            });
        }

        for (const el of qsa("[data-attr-id]", box)) {
            const id = Number(el.dataset.attrId || 0);
            if (!id) continue;
            const v = byAttr.get(id);
            if (!v) continue;

            if (el.tagName === "SELECT") {
                if (v.optionId) el.value = String(v.optionId);
            } else {
                if (v.valueText) el.value = String(v.valueText);
            }
        }
    }

    function fillVariantsFromSaved(savedVariants) {
        // savedVariants: [{key1,key2,stock}]
        const vars = (savedVariants || []).map(v => ({
            opt1: String(v.key1 ?? v.Key1 ?? "").trim(),
            opt2: String(v.key2 ?? v.Key2 ?? "").trim(),
            stock: Number(v.stock ?? v.Stock ?? 0) || 0
        })).filter(x => x.opt1);

   

        state.variants = vars;

        // tags opt1/opt2 depuis variants
        const hasSecond = state.variantAttrs.length > 1;
        state.opt1 = uniqLower(vars.map(v => v.opt1));
        state.opt2 = hasSecond ? uniqLower(vars.map(v => v.opt2).filter(x => x && x !== "Unique")) : [];

        renderTags("#sizesBox", "#inpSize", state.opt1, (x) => removeOpt(1, x));
        renderTags("#colorsBox", "#inpColor", state.opt2, (x) => removeOpt(2, x));
        renderVariantsTable();
    }

    async function saveVariants(productId) {
        const hasSecond = state.variantAttrs.length > 1;

        const items = (state.variants || []).map(v => ({
            key1: String(v.opt1 || "").trim(),
            key2: hasSecond ? String(v.opt2 || "").trim() : "Unique",
            stock: Math.max(0, Number(v.stock || 0) || 0)
        }))
            .filter(x => x.key1); // sécurité

        return await fetchVendor(`/api/vendor/products/${productId}/variants`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ items })
        });
    }

    // ========= Create =========
    function setSubmitting(isBusy) {
        state.submitting = isBusy;
        const btn = qs("#btnSave");
        if (!btn) return;

        btn.disabled = isBusy;

        const label = EDIT_ID ? "💾 Enregistrer" : "✅ Créer";
        btn.textContent = isBusy ? "Enregistrement..." : label;
    }
    function resetForm() {
        clearMsg();

        const ids = ["#name", "#price", "#pricePromo", "#stock",  "#sku", "#txtProdShort", "#txtProdDesc", "#txtProdHighlights"];
        for (const id of ids) qs(id) && (qs(id).value = "");

        const box = qs("#dynAttrsBox");
        if (box) box.innerHTML = `<div class="muted">Choisis une catégorie…</div>`;

        imgState.files = [];
        revokeImgUrls();
        qs("#fileProdImages") && (qs("#fileProdImages").value = "");
        renderImagesUI();

        clearVariants();
        setPreview();
    }

    async function upsertProductFlow() {
        if (state.submitting) return;
        clearMsg();

        if (typeof window.requireVendorAuth === "function" && !window.requireVendorAuth()) return;

        const catId = getSelectedCategoryId();
        if (!catId) return showMsg("bad", "Choisis une catégorie.");

        const name = (qs("#name")?.value || "").trim();
        if (name.length > PRODUCT_NAME_MAX) {
            return showMsg("bad", "Nom trop long.", `Maximum ${PRODUCT_NAME_MAX} caractères.`);
        }

        const sku = (qs("#sku")?.value || "").trim();
        const price = Number(String(qs("#price")?.value || "").trim().replace(",", ".") || 0);
        const pricePromoRaw = Number(String(qs("#pricePromo")?.value || "").trim().replace(",", ".") || 0);
        const pricePromo = pricePromoRaw > 0 ? pricePromoRaw : null;
        const brand = (qs("#brand")?.value || "").trim();

        if (pricePromo != null && pricePromo >= price) {
            return showMsg("bad", "Prix promo invalide.", "Le prix promo doit être inférieur au prix.");
        }

        const stock = Number(String(qs("#stock")?.value || "").trim() || 0);

        const shortDescription = (qs("#txtProdShort")?.value || "").trim();
        const description = (qs("#txtProdDesc")?.value || "").trim();
        const highlights = (qs("#txtProdHighlights")?.value || "").trim();

        if (!name) return showMsg("bad", "Nom obligatoire.");
        if (!EDIT_ID && !sku) return showMsg("bad", "SKU obligatoire."); // create
        if (!(price > 0)) return showMsg("bad", "Prix invalide.");

        // ✅ validation required dyn attrs
        const req = validateDynamicRequired();
        if (!req.ok) return showMsg("bad", "Champs manquants", req.message);

        const payload = {
            categoryId: catId,
            name,
            brand: brand || null,
            price,
            pricePromo,
            stock: Math.max(0, stock),
            sku: sku || null,
            shortDescription: shortDescription || null,
            description: description || null,
            highlights: highlights || null
        };
        setSubmitting(true);

        try {
            let productId = EDIT_ID;

            if (!EDIT_ID) {
                // CREATE
                const created = await fetchVendor("/api/vendor/products", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload),
                });

                productId = created?.id ?? created?.item?.id ?? created?.product?.id;
                if (!productId) throw new Error("ID produit manquant (API vendor).");
            } else {
                // UPDATE
                await fetchVendor(`/api/vendor/products/${EDIT_ID}`, {
                    method: "PUT",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload),
                });
            }

            // ✅ save attrs + variants
await saveDynamicAttrs(productId);

// ✅ ne pas écraser les variantes par vide en édition
if (state.variantAttrs.length > 0 && state.variants.length > 0) {
    await saveVariants(productId);
}

            // ✅ upload images si nouvelles sélectionnées
if (imgState.files.length) {
    const fd = new FormData();
    for (const f of imgState.files) {
        fd.append("files", f);
    }
    fd.append("setFirstAsMain", "true");

    await fetchVendor(`/api/vendor/products/${productId}/images`, {
        method: "POST",
        body: fd
    });
}

            showMsg("ok", EDIT_ID ? "Produit modifié ✅" : "Produit créé ✅", `ID: ${productId} — Statut: Pending`);
            setTimeout(() => (location.href = "/vendor-products.html"), 700);
        } catch (e) {
            showMsg("bad", "Erreur", e?.message || String(e));
        } finally {
            setSubmitting(false);
        }
    }

    // ========= Init =========
    async function init() {
        if (typeof window.requireVendorAuth === "function" && !window.requireVendorAuth()) return;

        qs("#name")?.addEventListener("input", () => {
            updateNameCount();
            setPreview();
        });

        qs("#btnSave")?.addEventListener("click", (e) => { e.preventDefault(); upsertProductFlow(); });
        qs("#btnBack")?.addEventListener("click", () => { location.href = "/vendor-products.html"; });
        qs("#btnReset")?.addEventListener("click", (e) => { e.preventDefault(); resetForm(); });

        qs("#selProdCategory")?.addEventListener("change", async () => {
            await applyCategoryRules();
            setPreview();
        });

        await loadCategoriesIntoSelect();
        updateNameCount();


        EDIT_ID = getQueryId();

        if (EDIT_ID) {
            // UI : titre + bouton
            qs("#pageTitle") && (qs("#pageTitle").textContent = "Modifier un produit");
            qs("#btnSave") && (qs("#btnSave").textContent = "💾 Enregistrer");

            // Charger le produit
            // Charger le produit
            const data = await fetchVendor(`/api/vendor/products/${EDIT_ID}`, { method: "GET" });
            const p0 = data?.item || data?.product || data;

            // ✅ normalisation robuste (camelCase + PascalCase)
            const p = {
                name: p0?.name ?? p0?.Name ?? "",
                price: p0?.price ?? p0?.Price ?? 0,
                pricePromo: p0?.pricePromo ?? p0?.PricePromo ?? "",
                stock: p0?.stock ?? p0?.Stock ?? 0,
                sku: p0?.sku ?? p0?.Sku ?? "",
                shortDescription: p0?.shortDescription ?? p0?.ShortDescription ?? "",
                description: p0?.description ?? p0?.Description ?? "",
                highlights: p0?.highlights ?? p0?.Highlights ?? "",
                categoryId: p0?.categoryId ?? p0?.CategoryId ?? null,
                images: p0?.images ?? p0?.Images ?? [],
                variants: p0?.variants ?? p0?.Variants ?? [],
                attributes: p0?.attributes ?? p0?.Attributes ?? [],
                brand: p0?.brand ?? p0?.Brand ?? "",
            };

            // Remplir champs
            qs("#name") && (qs("#name").value = p.name);
            qs("#price") && (qs("#price").value = p.price);
            qs("#pricePromo") && (qs("#pricePromo").value = p.pricePromo ?? "");
            qs("#stock") && (qs("#stock").value = p.stock);
            qs("#brand") && (qs("#brand").value = p.brand);
            qs("#sku") && (qs("#sku").value = p.sku);

            qs("#txtProdShort") && (qs("#txtProdShort").value = p.shortDescription);
            qs("#txtProdDesc") && (qs("#txtProdDesc").value = p.description);
            qs("#txtProdHighlights") && (qs("#txtProdHighlights").value = p.highlights);

            // Catégorie
            qs("#selProdCategory") && (qs("#selProdCategory").value = String(p.categoryId ?? ""));

            // Recharge règles + UI dynamique
            await applyCategoryRules();

            // Attributs / Variantes
            fillDynamicAttrsUIFromSaved(p.attributes || []);
            fillVariantsFromSaved(p.variants || []);

            // ✅ Images existantes (camelCase + PascalCase)
            existingImgState.urls = (p.images || [])
                .map(x => x?.url ?? x?.Url)
                .filter(Boolean);

            renderExistingImagesUI();
            setPreview();
        } else {
  
        }
        // ✅ si tu veux une catégorie par défaut, tu peux la mettre ici (sinon laisse vide)
        // qs("#selProdCategory").value = "45";


        bindImagesInput();

        // ✅ IMPORTANT : en édition on affiche les images existantes,
        // et on n’écrase pas avec placeholder
        if (EDIT_ID) renderExistingImagesUI();
        else renderImagesUI();


        qs("#inpSize")?.addEventListener("keydown", (e) => {
            if (e.key !== "Enter") return;
            e.preventDefault();
            const v = normalizeValue(e.target.value);
            if (!v) return;
            state.opt1 = uniqLower([...state.opt1, v]);
            e.target.value = "";
            renderTags("#sizesBox", "#inpSize", state.opt1, (x) => removeOpt(1, x));
        });

        qs("#inpColor")?.addEventListener("keydown", (e) => {
            if (e.key !== "Enter") return;
            e.preventDefault();
            const v = normalizeValue(e.target.value);
            if (!v) return;
            state.opt2 = uniqLower([...state.opt2, v]);
            e.target.value = "";
            renderTags("#colorsBox", "#inpColor", state.opt2, (x) => removeOpt(2, x));
        });

        qs("#btnDashboard")?.addEventListener("click", () => {
            location.href = "/vendor-dashboard.html";
        });

        qs("#btnBack")?.addEventListener("click", () => {
            location.href = "/vendor-products.html";
        });




        qs("#btnGenVariants")?.addEventListener("click", genVariants);
        qs("#btnClearVariants")?.addEventListener("click", clearVariants);
    }

    document.addEventListener("DOMContentLoaded", init);
})();