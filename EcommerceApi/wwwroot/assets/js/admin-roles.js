// /assets/js/admin-roles.js
let _allRoles = [];
let _allPermissions = [];
let _selectedRoleId = null;

function escapeHtml(s) {
    return String(s ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function setRolesMessage(msg) {
    const tbody = qs("#rolesBody");
    if (!tbody) return;
    tbody.innerHTML = `<tr><td colspan="5" class="muted">${escapeHtml(msg)}</td></tr>`;
}

function resetRoleForm() {
    _selectedRoleId = null;

    const txtName = qs("#txtRoleName");
    const txtCode = qs("#txtRoleCode");
    const txtDesc = qs("#txtRoleDescription");
    const btnSave = qs("#btnSaveRole");

    if (txtName) txtName.value = "";
    if (txtCode) {
        txtCode.value = "";
        txtCode.disabled = false;
    }
    if (txtDesc) txtDesc.value = "";

    if (btnSave) btnSave.textContent = "Enregistrer";

    qs("#selectedRoleInfo").textContent = "Sélectionnez un rôle.";
    renderPermissions([]);
}

function renderRoles(list) {
    const tbody = qs("#rolesBody");
    if (!tbody) return;

    if (!list.length) {
        setRolesMessage("Aucun rôle.");
        return;
    }

    tbody.innerHTML = list.map(r => {
        const id = r.id ?? "";
        const name = escapeHtml(r.name ?? "");
        const code = escapeHtml(r.code ?? "");
        const isSystemRole = !!r.isSystemRole;

        return `
            <tr>
                <td>${id}</td>
                <td>${name}</td>
                <td>${code}</td>
                <td>${isSystemRole ? "Oui" : "Non"}</td>
                <td style="display:flex;gap:8px;flex-wrap:wrap">
                    <button class="btn" type="button" data-edit="${id}">Éditer</button>
                    <button class="btn" type="button" data-perms="${id}">Permissions</button>
                    ${isSystemRole ? "" : `<button class="btn" type="button" data-del="${id}">Supprimer</button>`}
                </td>
            </tr>
        `;
    }).join("");

    tbody.querySelectorAll("[data-edit]").forEach(btn => {
        btn.addEventListener("click", () => {
            const id = Number(btn.getAttribute("data-edit"));
            const role = _allRoles.find(x => Number(x.id) === id);
            if (!role) return;

            _selectedRoleId = id;

            const txtName = qs("#txtRoleName");
            const txtCode = qs("#txtRoleCode");
            const txtDesc = qs("#txtRoleDescription");
            const btnSave = qs("#btnSaveRole");

            if (txtName) txtName.value = role.name || "";
            if (txtCode) {
                txtCode.value = role.code || "";
                txtCode.disabled = true;
            }
            if (txtDesc) txtDesc.value = role.description || "";

            if (btnSave) btnSave.textContent = "Modifier";

            // ✅ remonte automatiquement vers le formulaire
            const formCard = txtName?.closest(".card");
            if (formCard) {
                formCard.scrollIntoView({ behavior: "smooth", block: "start" });
            }

            // ✅ focus sur le nom
            txtName?.focus();
        });
    });

    tbody.querySelectorAll("[data-perms]").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = Number(btn.getAttribute("data-perms"));
            await loadRolePermissions(id);
        });
    });

    tbody.querySelectorAll("[data-del]").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = Number(btn.getAttribute("data-del"));
            const role = _allRoles.find(x => Number(x.id) === id);
            if (!role) return;

            if (!confirm(`Supprimer le rôle "${role.name}" ?`)) return;

            try {
                await fetchJson(`/api/admin/roles/${id}`, { method: "DELETE" });
                if (_selectedRoleId === id) resetRoleForm();
                await loadRoles();
            } catch (e) {
                alert("Erreur: " + (e.message || e));
            }
        });
    });
}

function applyRoleSearch() {
    const q = (qs("#txtSearchRole").value || "").trim().toLowerCase();
    const filtered = !q ? _allRoles : _allRoles.filter(r => {
        const t = `${r.id} ${r.name || ""} ${r.code || ""} ${r.description || ""}`.toLowerCase();
        return t.includes(q);
    });
    renderRoles(filtered);
}

function renderPermissions(selectedPermissionIds) {
    const box = qs("#permissionsBox");
    if (!box) return;

    const setIds = new Set((selectedPermissionIds || []).map(Number));

    if (!_allPermissions.length) {
        box.innerHTML = `<div class="muted">Aucune permission chargée.</div>`;
        return;
    }

    box.innerHTML = _allPermissions.map(p => {
        const checked = setIds.has(Number(p.id)) ? "checked" : "";
        return `
            <label class="card" style="padding:12px;display:flex;gap:10px;align-items:flex-start;cursor:pointer;margin:0">
                <input type="checkbox" class="perm-check" value="${p.id}" ${checked} style="margin-top:3px" />
                <div>
                    <div style="font-weight:800">${escapeHtml(p.name || p.code)}</div>
                    <div class="muted">${escapeHtml(p.code || "")}</div>
                    <div class="muted">${escapeHtml(p.category || "")}</div>
                </div>
            </label>
        `;
    }).join("");
}

async function loadRoles() {
    setRolesMessage("Chargement...");
    try {
        const data = await fetchJson("/api/admin/roles?ts=" + Date.now());
        const items = Array.isArray(data?.items) ? data.items : [];
        _allRoles = items;
        applyRoleSearch();
    } catch (e) {
        setRolesMessage("Erreur API: " + (e.message || e));
    }
}

async function loadPermissions() {
    try {
        const data = await fetchJson("/api/admin/permissions?ts=" + Date.now());
        _allPermissions = Array.isArray(data?.items) ? data.items : [];
        renderPermissions([]);
    } catch (e) {
        qs("#permissionsBox").innerHTML = `<div class="muted">Erreur chargement permissions: ${escapeHtml(e.message || e)}</div>`;
    }
}

async function loadRolePermissions(roleId) {
    try {
        const data = await fetchJson(`/api/admin/roles/${roleId}/permissions?ts=` + Date.now());
        const role = data?.role || null;
        const permissionIds = Array.isArray(data?.permissionIds) ? data.permissionIds : [];

        _selectedRoleId = Number(roleId);

        if (role) {
            qs("#txtRoleName").value = role.name || "";
            qs("#txtRoleCode").value = role.code || "";
            qs("#txtRoleDescription").value = role.description || "";
            qs("#txtRoleCode").disabled = true;
            qs("#selectedRoleInfo").textContent = `${role.name} (${role.code})`;
        }

        renderPermissions(permissionIds);
    } catch (e) {
        alert("Erreur: " + (e.message || e));
    }
}

async function saveRole() {
    const name = (qs("#txtRoleName").value || "").trim();
    const code = (qs("#txtRoleCode").value || "").trim();
    const description = (qs("#txtRoleDescription").value || "").trim();

    if (!name) {
        alert("Nom du rôle obligatoire.");
        return;
    }

    try {
        if (_selectedRoleId) {
            await fetchJson(`/api/admin/roles/${_selectedRoleId}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name, description })
            });
        } else {
            if (!code) {
                alert("Code du rôle obligatoire.");
                return;
            }

            await fetchJson("/api/admin/roles", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name, code, description })
            });
        }

        await loadRoles();
        resetRoleForm();
        alert(_selectedRoleId ? "Rôle modifié." : "Rôle créé.");
    } catch (e) {
        alert("Erreur: " + (e.message || e));
    }
}

async function saveRolePermissions() {
    if (!_selectedRoleId) {
        alert("Sélectionnez un rôle.");
        return;
    }

    const permissionIds = qsa(".perm-check:checked").map(x => Number(x.value));

    try {
        await fetchJson(`/api/admin/roles/${_selectedRoleId}/permissions`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ permissionIds })
        });

        alert("Permissions enregistrées.");
    } catch (e) {
        alert("Erreur: " + (e.message || e));
    }
}

async function initAdminRolesPage() {
    await loadAdminPermissions(true);

    if (!requirePermission("roles.manage")) return;

    window.setAdminPageTitle?.("Rôles & permissions", "🛡️");

    qs("#btnSaveRole")?.addEventListener("click", saveRole);
    qs("#btnResetRole")?.addEventListener("click", resetRoleForm);
    qs("#btnSavePermissions")?.addEventListener("click", saveRolePermissions);
    qs("#txtSearchRole")?.addEventListener("input", applyRoleSearch);

    await loadPermissions();
    await loadRoles();
}

window.initAdminRolesPage = initAdminRolesPage;