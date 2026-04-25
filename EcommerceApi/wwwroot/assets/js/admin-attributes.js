// wwwroot/assets/js/admin-attributes.js
(() => {
    "use strict";

    const qs = window.qs;
    const esc = (s) => String(s ?? "").replace(/[&<>"']/g, c => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;"
    }[c]));

    function getToken() { return localStorage.getItem("ranita_admin_token") || ""; }

    function withAuth(opts = {}) {
        const t = getToken();
        const h = new Headers(opts.headers || {});
        if (t) h.set("Authorization", `Bearer ${t}`);
        return { ...opts, headers: h };
    }

    async function fetchAdminJson(url, opts = {}) {
        if (typeof window.fetchJson !== "function") throw new Error("fetchJson introuvable (app.js).");
        return await window.fetchJson(url, withAuth(opts));
    }

    const state = { items: [], editingId: null, selected: null };

    function showMsg(type, text) {
        const box = qs("#attrMsg");
        if (!box) return;
        box.style.display = "";
        box.className = "form-msg " + (type === "ok" ? "ok" : "err");
        box.textContent = text;
    }
    function clearMsg() {
        const box = qs("#attrMsg");
        if (!box) return;
        box.style.display = "none";
        box.textContent = "";
        box.className = "form-msg";
    }
    function normalizeDataType(dt) {
        // API peut renvoyer: "Text" / "Option" OU 0 / 1
        if (typeof dt === "number") return dt === 1 ? "Option" : "Text";
        const s = String(dt || "Text").trim();
        if (!s) return "Text";
        return s;
    }

    function setEditMode(id) {
        state.editingId = id ? Number(id) : null;
        qs("#btnDeleteAttr").style.display = state.editingId ? "" : "none";
    }

    function resetForm() {
        clearMsg();
        setEditMode(null);
        state.selected = null;

        qs("#txtAttrCode").value = "";
        qs("#txtAttrName").value = "";
        qs("#selAttrType").value = "Text";
        qs("#chkAttrVariant").checked = false;
        qs("#chkAttrActive").checked = true;

        qs("#optPanel").style.display = "none";
        qs("#optList").innerHTML = "";
        qs("#txtOptValue").value = "";
        qs("#txtOptSort").value = "0";
    }

    function renderList() {
        const box = qs("#attrList");
        if (!box) return;

        if (!state.items.length) {
            box.innerHTML = `<div class="muted">Aucun attribut.</div>`;
            return;
        }

        box.innerHTML = state.items.map(a => `
      <button class="rowitem ${state.editingId === a.id ? "is-on" : ""}" data-id="${a.id}" type="button">
        <div style="display:flex;justify-content:space-between;gap:10px;align-items:center">
          <div>
            <div style="font-weight:900">${esc(a.name)}</div>
           <div class="muted">${esc(a.code)} • ${esc(normalizeDataType(a.dataType))} ${a.isVariant ? "• Variant" : ""}</div>

          </div>
          <div>${a.isActive ? "✅" : "⛔"}</div>
        </div>
      </button>
    `).join("");
    }

    async function loadAll() {
        const q = (qs("#txtSearchAttr")?.value || "").trim();
        const url = `/api/admin/attributes${q ? "?q=" + encodeURIComponent(q) : ""}`;
        const data = await fetchAdminJson(url, { cache: "no-store" });
        state.items = Array.isArray(data?.items) ? data.items : [];
        renderList();
    }

    function renderOptions(options) {
        const box = qs("#optList");
        if (!box) return;

        if (!options.length) {
            box.innerHTML = `<div class="muted">Aucune option.</div>`;
            return;
        }

        box.innerHTML = options.map(o => `
      <div class="rowline" data-optid="${o.id}">
        <div style="display:flex;justify-content:space-between;gap:10px;align-items:center">
          <div><b>${esc(o.value)}</b> <span class="muted">#${o.sortOrder}</span></div>
          <div style="display:flex;gap:8px">
            <button class="btn-mini" data-act="editOpt" type="button">✏️</button>
            <button class="btn-mini danger" data-act="delOpt" type="button">🗑️</button>
          </div>
        </div>
      </div>
    `).join("");
    }

    function normalizeDataType(v) {
        // Accepte: "Option", "option", 0/1/2 (enum), null
        if (v == null) return "Text";

        // Si enum number (selon ton AttributeDataType)
        // adapte l'ordre si ton enum diffère
        if (typeof v === "number") {
            const map = {
                0: "Text",
                1: "Int",
                2: "Decimal",
                3: "Bool",
                4: "Date",
                5: "Option"
            };
            return map[v] || "Text";
        }

        const s = String(v).trim().toLowerCase();
        if (s === "text") return "Text";
        if (s === "int" || s === "integer") return "Int";
        if (s === "decimal" || s === "double" || s === "float") return "Decimal";
        if (s === "bool" || s === "boolean") return "Bool";
        if (s === "date" || s === "datetime") return "Date";
        if (s === "option" || s === "select" || s === "list") return "Option";

        return "Text";
    }



    async function loadOne(id) {
        const data = await fetchAdminJson(`/api/admin/attributes/${id}`, { cache: "no-store" });

        // DEBUG utile (tu verras la vraie forme dans console)
        console.log("GET attribute raw =", data);

        // Accepte plusieurs formats possibles :
        // { item: {...} }  OU  { attribute: {...} }  OU  { data: {...} }  OU directement {...}
        const it =
            data?.item ??
            data?.attribute ??
            data?.data ??
            (data && data.id ? data : null);

        if (!it) {
            // Si l'API renvoie {ok:false, message:"..."} on le montre
            const msg = data?.message || "Attribut introuvable.";
            throw new Error(msg);
        }

        state.selected = it;

        const code = it.code ?? it.Code ?? "";
        const name = it.name ?? it.Name ?? "";
        const rawDt = it.dataType ?? it.DataType ?? it.type ?? it.Type;

        qs("#txtAttrCode").value = code || "";
        qs("#txtAttrName").value = name || "";

        const dt = normalizeDataType(rawDt);
        qs("#selAttrType").value = dt;

        qs("#selAttrType").value = dt;

        qs("#chkAttrVariant").checked = !!it.isVariant;
        qs("#chkAttrActive").checked = !!it.isActive;

        const isOpt = dt.toLowerCase() === "option";
        qs("#optPanel").style.display = isOpt ? "" : "none";

        if (isOpt) {
            renderOptions(it.options || []);
            refreshNextSortUI();
        } else {
            qs("#optList").innerHTML = "";
        }
    }


    async function saveAttr() {
        clearMsg();

        const code = (qs("#txtAttrCode").value || "").trim();
        const name = (qs("#txtAttrName").value || "").trim();
        const dataType = normalizeDataType(qs("#selAttrType").value || "Text");

        const isVariant = !!qs("#chkAttrVariant").checked;
        const isActive = !!qs("#chkAttrActive").checked;

        if (!code) return showMsg("err", "Code obligatoire.");
        if (!name) return showMsg("err", "Nom obligatoire.");

        const payload = { code, name, dataType, isVariant, isActive };

        if (state.editingId) {
            await fetchAdminJson(`/api/admin/attributes/${state.editingId}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });
            showMsg("ok", "Attribut modifié ✅");
            await loadAll();
            await loadOne(state.editingId);
            return;
        }

        const created = await fetchAdminJson(`/api/admin/attributes`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        const id = created?.id;
        if (!created?.ok || !id) throw new Error(created?.message || "Création échouée.");

        showMsg("ok", "Attribut créé ✅");
        await loadAll();
        setEditMode(id);
        await loadOne(id);
    }

    async function deleteAttr() {
        if (!state.editingId) return;
        if (!confirm("Supprimer cet attribut ?")) return;

        await fetchAdminJson(`/api/admin/attributes/${state.editingId}`, { method: "DELETE" });
        showMsg("ok", "Attribut supprimé ✅");
        resetForm();
        await loadAll();
    }

    async function addOption() {
        if (!state.editingId) return showMsg("err", "Enregistre l'attribut d'abord.");
        const v = (qs("#txtOptValue").value || "").trim();

        // ✅ si vide/0 => on met max+1 automatiquement
        let s = Number(qs("#txtOptSort").value || 0) || 0;
        if (s <= 0) s = getNextSortOrder();

        if (!v) return showMsg("err", "Valeur option obligatoire.");

        await fetchAdminJson(`/api/admin/attributes/${state.editingId}/options`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ value: v, sortOrder: s })
        });

        qs("#txtOptValue").value = "";
        await loadOne(state.editingId);
        showMsg("ok", "Option ajoutée ✅");
    }


    async function updateOption(optId) {
        const cur = state.selected?.options?.find(x => Number(x.id) === Number(optId));
        const newVal = prompt("Nouvelle valeur :", cur?.value || "");
        if (newVal == null) return;

        const newSort = prompt("SortOrder :", String(cur?.sortOrder ?? 0));
        if (newSort == null) return;

        await fetchAdminJson(`/api/admin/attributes/${state.editingId}/options/${optId}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ value: String(newVal).trim(), sortOrder: Number(newSort) || 0 })
        });

        await loadOne(state.editingId);
        showMsg("ok", "Option modifiée ✅");
    }

    async function deleteOption(optId) {
        if (!confirm("Supprimer cette option ?")) return;
        await fetchAdminJson(`/api/admin/attributes/${state.editingId}/options/${optId}`, { method: "DELETE" });
        await loadOne(state.editingId);
        showMsg("ok", "Option supprimée ✅");
    }

    function bind() {
        qs("#btnNewAttr")?.addEventListener("click", resetForm);
        qs("#btnResetAttr")?.addEventListener("click", resetForm);
        qs("#btnSaveAttr")?.addEventListener("click", (e) => { e.preventDefault(); saveAttr().catch(err => showMsg("err", err.message)); });
        qs("#btnDeleteAttr")?.addEventListener("click", (e) => { e.preventDefault(); deleteAttr().catch(err => showMsg("err", err.message)); });

        qs("#txtSearchAttr")?.addEventListener("input", () => loadAll().catch(() => { }));

        qs("#selAttrType")?.addEventListener("change", () => {
            const isOpt = (qs("#selAttrType").value || "").toLowerCase() === "option";
            qs("#optPanel").style.display = isOpt ? "" : "none";
        });

        qs("#btnAddOpt")?.addEventListener("click", (e) => { e.preventDefault(); addOption().catch(err => showMsg("err", err.message)); });

        qs("#attrList")?.addEventListener("click", async (e) => {
            const row = e.target.closest("button[data-id]");
            if (!row) return;

            const id = Number(row.dataset.id || 0);
            if (!id) return;

            try {
                clearMsg();
                setEditMode(id);
                await loadOne(id);
                renderList();
            } catch (err) {
                console.error("loadOne failed:", err);
                showMsg("err", err?.message || "Erreur chargement attribut.");
            }
        });


        qs("#optList")?.addEventListener("click", (e) => {
            const line = e.target.closest("[data-optid]");
            if (!line) return;
            const optId = Number(line.dataset.optid || 0);
            if (!optId) return;

            const act = e.target.closest("[data-act]")?.dataset?.act;
            if (act === "editOpt") updateOption(optId).catch(err => showMsg("err", err.message));
            if (act === "delOpt") deleteOption(optId).catch(err => showMsg("err", err.message));
        });
    }
    function getNextSortOrder() {
        const opts = state.selected?.options || [];
        if (!opts.length) return 0;
        const max = Math.max(...opts.map(o => Number(o.sortOrder || 0)));
        return max + 1;
    }

    function refreshNextSortUI() {
        const el = qs("#txtOptSort");
        if (!el) return;
        el.value = String(getNextSortOrder());
    }

    window.initAdminAttributesPage = async function () {
        if (typeof window.renderAdminHeader === "function") window.renderAdminHeader("attributes");
        bind();
        resetForm();
        await loadAll();

        // Boot fallback (si app.js n'appelle pas init automatiquement)
        if (!window.__ranita_attr_inited) {
            window.__ranita_attr_inited = true;
            document.addEventListener("DOMContentLoaded", () => {
                if (typeof window.initAdminAttributesPage === "function") {
                    window.initAdminAttributesPage().catch(err => {
                        console.error(err);
                        const box = document.querySelector("#attrMsg");
                        if (box) {
                            box.style.display = "";
                            box.className = "form-msg err";
                            box.textContent = err?.message || "Erreur init page attributs.";

                        }
                    });
                }
            });
        }

    };
})();
