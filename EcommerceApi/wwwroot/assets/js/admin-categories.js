// /assets/js/admin-categories.js
(() => {
    "use strict";

    let _cats = [];
    let _tree = [];
    let _editingId = null;

    let _slugTouched = false;
    let _slugAuto = true;

    let _allAttrsCache = [];
    let _mappedAttrIds = new Set();

    const CATS_URL = "/api/admin/categories";
    const ATTRS_URL = "/api/admin/attributes";
    const COMM_URL = "/api/admin/commissions";


    // -------------------------
    // Helpers
    // -------------------------
    function esc(s) {
        return String(s ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function slugify(str) {
        return String(str || "")
            .trim()
            .toLowerCase()
            .normalize("NFD").replace(/[\u0300-\u036f]/g, "")
            .replace(/[^a-z0-9]+/g, "-")
            .replace(/^-+|-+$/g, "");
    }

    function absUrl(path) {
        if (!path) return "";
        if (path.startsWith("http")) return path;
        return location.origin + path;
    }

    function setRowMessage(msg) {
        const body = document.getElementById("catsBody");
        if (!body) return;
        body.innerHTML = `<tr><td colspan="9" class="muted">${esc(msg)}</td></tr>`;
    }

    // -------------------------
    // Auth fetch (Admin JWT)
    // -------------------------
    function getAdminToken() {
        return localStorage.getItem("ranita_admin_token") || "";
    }

    function getFetchJson() {
        return window._fetchJson || window.fetchJson;
    }

    function requireAuthSafe() {
        const fn = window.requireAdminAuth;
        if (typeof fn === "function") fn();
        else console.warn("⚠️ requireAdminAuth introuvable (app.js pas chargé ?).");
    }

    async function fetchAdminJson(url, opts = {}) {
        const fn = getFetchJson();
        if (typeof fn !== "function") throw new Error("fetchJson introuvable (app.js pas chargé avant).");

        const token = getAdminToken();
        const headers = new Headers(opts.headers || {});
        if (token) headers.set("Authorization", `Bearer ${token}`);

        return await fn(url, { ...opts, headers });
    }


    // -------------------------
    // Normalizers
    // -------------------------
    function normalizeCat(c) {
        return {
            id: c?.id ?? c?.Id ?? "",
            name: c?.name ?? c?.Name ?? "",
            slug: c?.slug ?? c?.Slug ?? "",
            parentId: c?.parentId ?? c?.ParentId ?? null,
            sortOrder: c?.sortOrder ?? c?.SortOrder ?? 0,
            isActive: (c?.isActive ?? c?.IsActive ?? true) === true,
            imageUrl: c?.imageUrl ?? c?.ImageUrl ?? "",
            metaTitle: c?.metaTitle ?? c?.MetaTitle ?? "",
            metaDescription: c?.metaDescription ?? c?.MetaDescription ?? "",
            description: c?.description ?? c?.Description ?? "",
            commissionRate: (c?.commissionRate ?? c?.CommissionRate ?? null),
        };
    }

    function percentFromRate(rate) {
        if (rate == null) return null;
        return Math.round(Number(rate) * 10000) / 100; // 0.1234 => 12.34
    }

    function rateFromPercent(p) {
        const n = Number(p);
        if (!isFinite(n)) return null;
        return Math.round((n / 100) * 10000) / 10000;
    }

    // calcule ce qui s'applique vraiment (parent -> global)
    let _globalRate = null;

    // =========================
    // COMMISSION MODAL (GLOBAL)
    // =========================
    let _commCatId = null;

    const commModal = () => document.getElementById("commModal");
    const commModeEl = () => document.getElementById("commMode");
    const commPctWrap = () => document.getElementById("commPercentWrap");
    const commPctEl = () => document.getElementById("commPercent");

    function openCommModal(catId) {
        _commCatId = catId;

        const cRaw = _cats.find(x => String(normalizeCat(x).id) === String(catId));
        const c = normalizeCat(cRaw);

        if (c.commissionRate != null) {
            commModeEl().value = "custom";
            commPctWrap().style.display = "";
            commPctEl().value = String(percentFromRate(c.commissionRate));
        } else {
            commModeEl().value = "inherit";
            commPctWrap().style.display = "none";
            commPctEl().value = "";
        }

        commModal().style.display = "flex";
    }

    function closeCommModal() {
        commModal().style.display = "none";
        _commCatId = null;
    }



    async function loadGlobalRate() {
        try {
            const data = await fetchAdminJson(`${COMM_URL}/global?ts=${Date.now()}`, { cache: "no-store" });
            _globalRate = data?.rate ?? null; // 0.12
        } catch (e) {
            console.warn("Erreur lors du chargement de la commission globale :", e);
            _globalRate = null; // on garde une valeur par défaut
        }
    }

    function effectiveRateForCat(catId) {
        const map = new Map(_cats.map(x => {
            const c = normalizeCat(x);
            return [String(c.id), c];
        }));

        let cur = map.get(String(catId));
        while (cur) {
            if (cur.commissionRate != null) return cur.commissionRate;
            if (!cur.parentId) break;
            cur = map.get(String(cur.parentId));
        }
        return _globalRate; // fallback global
    }

    function commissionLabel(catId) {
        const c = normalizeCat(_cats.find(x => String(normalizeCat(x).id) === String(catId)) || {});
        const eff = effectiveRateForCat(catId);
        const effPct = eff == null ? null : percentFromRate(eff);

        // si la catégorie a son propre taux
        if (c.commissionRate != null) return `${percentFromRate(c.commissionRate)}%`;

        // sinon héritage
        return effPct == null ? "Hérite (—)" : `Hérite (${effPct}%)`;
    }

    function normalizeAttr(a) {
        return {
            id: Number(a?.id ?? a?.Id ?? 0) || 0,
            name: String(a?.name ?? a?.Name ?? "").trim(),
            dataType: String(a?.dataType ?? a?.DataType ?? "").trim(),
            isVariant: (a?.isVariant ?? a?.IsVariant ?? false) === true
        };
    }

    // =========================
    // TREE
    // =========================
    function buildTree(items) {
        const map = new Map();
        const roots = [];

        for (const it of items) {
            const c = normalizeCat(it);
            map.set(Number(c.id), { ...c, children: [] });
        }

        for (const it of items) {
            const c = normalizeCat(it);
            const node = map.get(Number(c.id));
            const pid = c.parentId == null ? null : Number(c.parentId);
            if (pid && map.has(pid)) map.get(pid).children.push(node);
            else roots.push(node);
        }

        const sortRec = (arr) => {
            arr.sort((a, b) => (Number(a.sortOrder) - Number(b.sortOrder)) || a.name.localeCompare(b.name));
            for (const n of arr) sortRec(n.children);
        };
        sortRec(roots);

        return roots;
    }

    function flattenTree(nodes, level = 0, acc = []) {
        for (const n of nodes) {
            acc.push({ node: n, level });
            if (n.children?.length) flattenTree(n.children, level + 1, acc);
        }
        return acc;
    }

    function parentNameById(id) {
        if (!id) return "—";
        const raw = _cats.find(x => String(normalizeCat(x).id) === String(id));
        return raw ? normalizeCat(raw).name : "—";
    }

    function fillParentSelect(excludeId = null) {
        const sel = document.getElementById("selCatParent");
        if (!sel) return;

        const tree = buildTree(_cats || []);
        const flat = flattenTree(tree);

        sel.innerHTML = `<option value="">(Racine)</option>`;
        for (const { node, level } of flat) {
            if (excludeId && String(node.id) === String(excludeId)) continue;
            const opt = document.createElement("option");
            opt.value = String(node.id);
            opt.textContent = `${"—".repeat(level)} ${node.name}`;
            sel.appendChild(opt);
        }
    }

    // =========================
    // ATTRIBUTES
    // =========================
    async function fetchAllAttributes() {
        const data = await fetchAdminJson(`${ATTRS_URL}?ts=${Date.now()}`, { cache: "no-store" });
        const items = Array.isArray(data) ? data : (data.items || data.attributes || []);
        return items.map(normalizeAttr).filter(a => a.id && a.name);
    }

    async function fetchCategoryAttributes(catId) {
        const rows = await fetchAdminJson(`${CATS_URL}/${encodeURIComponent(catId)}/attributes?ts=${Date.now()}`, { cache: "no-store" });
        return Array.isArray(rows) ? rows : (rows.items || rows || []);
    }

    function countSelectedVariant() {
        const box = document.getElementById("catAttrsBox");
        if (!box) return 0;
        return Array.from(box.querySelectorAll("input[type='checkbox'][data-variant='1']"))
            .filter(chk => chk.checked).length;
    }

    function getAttrFilters() {
        const q = (document.getElementById("txtAttrSearch")?.value || "").trim().toLowerCase();
        const onlyVar = document.getElementById("chkAttrOnlyVariant")?.checked === true;
        return { q, onlyVar };
    }

    function setAttrsButtonState(isEdit) {
        const btn = document.getElementById("btnSaveCatAttrs");
        if (!btn) return;

        if (isEdit) {
            btn.disabled = false;
            btn.textContent = "Enregistrer attributs";
        } else {
            btn.disabled = true;
            btn.textContent = "Les attributs seront enregistrés après création";
        }
    }

    function resetAttrFilters() {
        const s = document.getElementById("txtAttrSearch");
        if (s) s.value = "";
        const v = document.getElementById("chkAttrOnlyVariant");
        if (v) v.checked = false;
    }

    function renderCatAttrsUI(allAttrs) {
        const box = document.getElementById("catAttrsBox");
        if (!box) return;

        const { q, onlyVar } = getAttrFilters();

        let list = (allAttrs || []).slice().sort((a, b) => {
            if (a.isVariant !== b.isVariant) return a.isVariant ? -1 : 1;
            return a.name.localeCompare(b.name);
        });

        if (onlyVar) list = list.filter(a => a.isVariant);
        if (q) list = list.filter(a => a.name.toLowerCase().includes(q));
        box.innerHTML = list.map(a => {
            const checked = _mappedAttrIds.has(a.id) ? "checked" : "";
            const vBadge = a.isVariant
                ? `<span class="badge" style="padding:2px 8px;border-radius:999px">Variant</span>`
                : "";

            return `
<div class="cat-attr-row">
  <div class="cat-attr-info">
    <div class="cat-attr-title">${esc(a.name)} ${vBadge}</div>
    <div class="cat-attr-sub">Type: ${esc(a.dataType || "—")}</div>
  </div>

  <div class="cat-attr-check">
    <input type="checkbox"
           data-attr-id="${a.id}"
           data-variant="${a.isVariant ? "1" : "0"}"
           ${checked}>
  </div>
</div>`;
        }).join("");


        // max 2 variants
        box.querySelectorAll("input[type='checkbox'][data-attr-id]").forEach(chk => {
            chk.addEventListener("change", () => {
                const isVar = chk.dataset.variant === "1";
                if (isVar && chk.checked) {
                    const n = countSelectedVariant();
                    if (n > 2) {
                        chk.checked = false;
                        alert("Max 2 attributs Variant par catégorie.");
                    }
                }
            });
        });
    }

    async function clearAttrsUI() {
        const box = document.getElementById("catAttrsBox");
        _mappedAttrIds = new Set();
        if (box) box.innerHTML = `<div class="muted">Chargement des attributs...</div>`;

        _allAttrsCache = await fetchAllAttributes();
        renderCatAttrsUI(_allAttrsCache);
    }

    async function loadAndRenderCategoryAttributes(catId) {
        const box = document.getElementById("catAttrsBox");
        if (box) box.innerHTML = `<div class="muted">Chargement des attributs...</div>`;

        const [all, mapped] = await Promise.all([
            fetchAllAttributes(),
            fetchCategoryAttributes(catId)
        ]);

        _allAttrsCache = all;

        const ids = new Set();
        for (const r of (mapped || [])) {
            const aid = Number(r.attributeId || r.AttributeId || r.attribute?.id || r.attribute?.Id || 0) || 0;
            if (aid) ids.add(aid);
        }
        _mappedAttrIds = ids;

        renderCatAttrsUI(_allAttrsCache);
    }

    function collectSelectedAttrsForSave() {
        const box = document.getElementById("catAttrsBox");
        if (!box) return { items: [], variantsCount: 0 };

        const selected = Array.from(
            box.querySelectorAll("input[type='checkbox'][data-attr-id]")
        )
            .filter(chk => chk.checked)
            .map(chk => ({
                attributeId: Number(chk.dataset.attrId || 0) || 0,
                isRequired: false,
                isFilterable: false,
                sortOrder: 0,
                isVariant: chk.dataset.variant === "1"   // ✅ IMPORTANT
            }))
            .filter(x => x.attributeId);

        const variantsCount = selected.filter(x => x.isVariant).length;

        return { items: selected, variantsCount };
    }

    async function saveCategoryAttributes() {
        if (!_editingId) return alert("Clique sur Modifier d’abord (mode édition).");

        const sel = collectSelectedAttrsForSave();
        if (sel.variantsCount > 2) return alert("Max 2 attributs Variant par catégorie.");

        await fetchAdminJson(`${CATS_URL}/${encodeURIComponent(_editingId)}/attributes`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ items: sel.items })
        });

        alert("Attributs enregistrés ✅");
        await loadAndRenderCategoryAttributes(Number(_editingId));
    }

    // =========================
    // UI MODE
    // =========================
    function setCreateMode() {
        _editingId = null;
        _slugTouched = false;
        _slugAuto = true;

        const nameEl = document.getElementById("txtCatName");
        const slugEl = document.getElementById("txtCatSlug");
        const fileEl = document.getElementById("fileCatImage");
        const parentEl = document.getElementById("selCatParent");
        const activeEl = document.getElementById("selIsActive");
        const btn = document.getElementById("btnCatCreate");

        if (nameEl) nameEl.value = "";
        if (slugEl) slugEl.value = "";
        if (fileEl) fileEl.value = "";
        if (parentEl) parentEl.value = "";
        if (activeEl) activeEl.value = "true";
        if (btn) btn.textContent = "Créer";

        const mt = document.getElementById("txtMetaTitle");
        const md = document.getElementById("txtMetaDesc");
        const cd = document.getElementById("txtCatDesc");
        if (mt) mt.value = "";
        if (md) md.value = "";
        if (cd) cd.value = "";

        // ✅ reset filtres + bouton attributs
        resetAttrFilters();
        setAttrsButtonState(false);

        // ✅ en création: rien coché
        clearAttrsUI().catch(console.error);
    }

    async function setEditMode(raw) {
        const c = normalizeCat(raw);
        _editingId = c.id;

        const nameEl = document.getElementById("txtCatName");
        const slugEl = document.getElementById("txtCatSlug");
        const parentEl = document.getElementById("selCatParent");
        const activeEl = document.getElementById("selIsActive");
        const btn = document.getElementById("btnCatCreate");

        if (nameEl) nameEl.value = c.name;
        if (slugEl) slugEl.value = c.slug;

        fillParentSelect(c.id);
        if (parentEl) parentEl.value = c.parentId ? String(c.parentId) : "";

        if (activeEl) activeEl.value = c.isActive ? "true" : "false";
        if (btn) btn.textContent = "Enregistrer";

        const mt = document.getElementById("txtMetaTitle");
        const md = document.getElementById("txtMetaDesc");
        const cd = document.getElementById("txtCatDesc");
        if (mt) mt.value = c.metaTitle || "";
        if (md) md.value = c.metaDescription || "";
        if (cd) cd.value = c.description || "";

        // ✅ mode edit = bouton attrs actif
        setAttrsButtonState(true);

        await loadAndRenderCategoryAttributes(Number(_editingId));
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    // =========================
    // LIST RENDER
    // =========================
    function renderCats(treeNodes) {
        const body = document.getElementById("catsBody");
        if (!body) return;

        const flat = flattenTree(treeNodes || []);
        if (!flat.length) {
            setRowMessage("Aucune catégorie.");
            return;
        }

        body.innerHTML = flat.map(({ node, level }) => {
            const pad = 10 + level * 18;
            const parentName = node.parentId ? parentNameById(node.parentId) : "—";
            const activeBadge = node.isActive
                ? `<span class="badge badge-delivered">Actif</span>`
                : `<span class="badge badge-cancelled">Inactif</span>`;

            const isFolder = (node.children && node.children.length > 0);

            return `
<tr>
  <td>${esc(node.id)}</td>
  <td style="padding-left:${pad}px">
    <span style="opacity:.85">${isFolder ? "📁" : "📄"}</span>
    <b style="margin-left:6px">${esc(node.name)}</b>
  </td>
  <td class="muted">${esc(node.slug)}</td>
  <td class="muted">${esc(parentName)}</td>
<td class="muted">${esc(commissionLabel(node.id))}</td>
<td class="muted">${esc(node.sortOrder)}</td>
  <td>${activeBadge}</td>
  <td>
    ${node.imageUrl
                    ? `<img src="${absUrl(node.imageUrl)}" style="width:36px;height:36px;object-fit:cover;border-radius:8px;" />`
                    : `<span class="muted">—</span>`
                }
  </td>
  <td style="text-align:right; white-space:nowrap;">
    ${isFolder ? `<span class="muted">—</span>` : `<button class="btn btn-primary" type="button" data-addprod="${esc(node.id)}">➕ Produit</button>`}
    <button class="btn" type="button" data-edit="${esc(node.id)}">Modifier</button>
    <button class="btn" type="button" data-del="${esc(node.id)}">Supprimer</button>
    <button class="btn" type="button" data-comm="${esc(node.id)}">Commission</button>
  </td>
</tr>`;
        }).join("");

        // edit
        body.querySelectorAll("[data-edit]").forEach(btn => {
            btn.addEventListener("click", async () => {
                const id = btn.getAttribute("data-edit");
                const raw = _cats.find(x => String(normalizeCat(x).id) === String(id));
                if (raw) await setEditMode(raw);
            });
        });

        // delete
        body.querySelectorAll("[data-del]").forEach(btn => {
            btn.addEventListener("click", async () => {
                const id = btn.getAttribute("data-del");
                if (!id) return;

                const hasChild = _cats.some(x => String(normalizeCat(x).parentId) === String(id));
                if (hasChild) return alert("Impossible: cette catégorie a des sous-catégories.");

                if (!confirm("Supprimer cette catégorie ?")) return;

                await fetchAdminJson(`${CATS_URL}/${encodeURIComponent(id)}`, { method: "DELETE" });
                await loadCats(document.getElementById("txtSearch")?.value || "");
                if (String(_editingId) === String(id)) setCreateMode();
            });
        });


   

 






        body.querySelectorAll("[data-comm]").forEach(btn => {
            btn.addEventListener("click", () => {
                const id = btn.getAttribute("data-comm");
                if (id) openCommModal(id);
            });
        });
        // ✅ add product
        body.querySelectorAll("[data-addprod]").forEach(btn => {
            btn.addEventListener("click", () => {
                const catId = btn.getAttribute("data-addprod");
                if (!catId) return;
                location.href = `/admin-products.html?catId=${encodeURIComponent(catId)}`;
            });
        });
    }

    function filterCats(q) {
        q = (q || "").trim().toLowerCase();
        if (!q) return _tree;

        const matched = _cats.filter(raw => {
            const c = normalizeCat(raw);
            return (
                String(c.id).toLowerCase().includes(q) ||
                String(c.name).toLowerCase().includes(q) ||
                String(c.slug).toLowerCase().includes(q) ||
                String(parentNameById(c.parentId)).toLowerCase().includes(q)
            );
        });

        return buildTree(matched);
    }

    async function loadCats(search = "") {
        setRowMessage("Chargement...");
        const data = await fetchAdminJson(`${CATS_URL}?ts=${Date.now()}`, { cache: "no-store" });
        const items = Array.isArray(data) ? data : (data.items || []);
        _cats = items;
        _tree = buildTree(_cats);

        if (!_editingId) fillParentSelect(null);
        renderCats(filterCats(search));
    }

    // =========================
    // SAVE CAT
    // =========================
    async function uploadCatImage(catId) {
        const fileEl = document.getElementById("fileCatImage");
        const file = fileEl?.files?.[0];
        if (!file) return null;

        const fd = new FormData();
        fd.append("file", file);

        const data = await fetchAdminJson(`${CATS_URL}/${encodeURIComponent(catId)}/image`, {
            method: "POST",
            body: fd
        });

        fileEl.value = "";
        return data?.imageUrl || null;
    }

    async function saveCat() {
        const name = (document.getElementById("txtCatName")?.value || "").trim();
        let slug = (document.getElementById("txtCatSlug")?.value || "").trim();
        const parentIdRaw = (document.getElementById("selCatParent")?.value || "").trim();
        const parentId = parentIdRaw ? Number(parentIdRaw) : null;
        const isActive = String(document.getElementById("selIsActive")?.value || "true") === "true";

        const metaTitle = (document.getElementById("txtMetaTitle")?.value || "").trim();
        const metaDescription = (document.getElementById("txtMetaDesc")?.value || "").trim();
        const description = (document.getElementById("txtCatDesc")?.value || "").trim();

        if (!name) return alert("Nom obligatoire.");
        if (!slug) slug = slugify(name);

        console.log("DEBUG desc:", document.getElementById("txtCatDesc")?.value);
        console.log("DEBUG payload:", { metaTitle, metaDescription, description });


        const payload = { name, slug, parentId, isActive, metaTitle, metaDescription, description };

        const wasEdit = !!_editingId;
        let savedId = null;

        // CREATE / UPDATE
        if (!wasEdit) {
            const created = await fetchAdminJson(CATS_URL, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });
            savedId = created?.item?.id ?? created?.item?.Id ?? created?.id ?? created?.Id ?? null;

        } else {
            await fetchAdminJson(`${CATS_URL}/${encodeURIComponent(_editingId)}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });
            savedId = _editingId;
        }

        // ATTRS
        const sel = collectSelectedAttrsForSave();
        if (sel.variantsCount > 2) return alert("Max 2 attributs Variant par catégorie.");

        if (savedId) {
            await fetchAdminJson(`${CATS_URL}/${encodeURIComponent(savedId)}/attributes`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ items: sel.items })
            });

            await uploadCatImage(savedId);
        }

        // ✅ TOUJOURS vider après Enregistrer (create + update)
        setCreateMode();

        // reload liste + parent select propre
        await loadCats(document.getElementById("txtSearch")?.value || "");
        fillParentSelect(null);

        // ✅ pas de reload attrs en mode create (car setCreateMode remet _editingId=null)
    }

    // =========================
    // Slug auto
    // =========================
    function wireSlugAuto() {
        const nameEl = document.getElementById("txtCatName");
        const slugEl = document.getElementById("txtCatSlug");
        if (!nameEl || !slugEl) return;

        _slugAuto = true;

        slugEl.addEventListener("input", () => {
            _slugTouched = slugEl.value.trim().length > 0;
            _slugAuto = slugEl.value.trim().length === 0;
        });

        nameEl.addEventListener("input", () => {
            if (!_slugTouched && _slugAuto) slugEl.value = slugify(nameEl.value);
        });
    }

    function resetForm() {
        setCreateMode();
        const searchEl = document.getElementById("txtSearch");
        if (searchEl) searchEl.value = "";
        fillParentSelect(null);
        loadCats("");
    }

    // =========================
    // INIT
    // =========================
    async function initAdminCategoriesPage() {
        requireAuthSafe();

        if (initAdminCategoriesPage._done) return;
        initAdminCategoriesPage._done = true;


        document.getElementById("btnCatCreate")?.addEventListener("click", saveCat);
        document.getElementById("btnCatReset")?.addEventListener("click", resetForm);
        document.getElementById("btnSaveCatAttrs")?.addEventListener("click", saveCategoryAttributes);

        // filtres attributs (live)
        document.getElementById("txtAttrSearch")?.addEventListener("input", () => renderCatAttrsUI(_allAttrsCache));
        document.getElementById("chkAttrOnlyVariant")?.addEventListener("change", () => renderCatAttrsUI(_allAttrsCache));

        document.getElementById("txtSearch")?.addEventListener("input", (e) => {
            renderCats(filterCats(e.target.value));
        });

        // ✅ modal events (1 seule fois)
        document.getElementById("commMode")?.addEventListener("change", () => {
            const isCustom = commModeEl().value === "custom";
            commPctWrap().style.display = isCustom ? "" : "none";
        });

        document.getElementById("commCancel")?.addEventListener("click", closeCommModal);

        document.getElementById("commSave")?.addEventListener("click", async () => {
            if (!_commCatId) return;

            const mode = commModeEl().value;
            let payload;

            if (mode === "inherit") {
                payload = { isActive: false, percent: 0 };
            } else {
                const p = Number(commPctEl().value);
                if (!isFinite(p) || p < 0 || p > 100) return alert("Percent doit être entre 0 et 100.");
                payload = { isActive: true, percent: p };
            }

            await fetchAdminJson(`${COMM_URL}/category/${encodeURIComponent(_commCatId)}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            closeCommModal();
            await loadCats(document.getElementById("txtSearch")?.value || "");
        });

        wireSlugAuto();
        setCreateMode();

        // ✅ ordre correct
        await loadGlobalRate();
        await loadCats("");
        fillParentSelect(null);
    }



    window.initAdminCategoriesPage = initAdminCategoriesPage;

    document.addEventListener("DOMContentLoaded", () => {
        const title = document.getElementById("pageTitle");
        if (title) {
            title.innerHTML = `
            <span class="pageTitleIcon">📦</span>
            Catégories
        `;
        }

        if (document.getElementById("btnCatCreate")) {
            window.initAdminCategoriesPage?.();
        }
    });

})();
