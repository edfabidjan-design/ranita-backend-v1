// /assets/js/admin-users.js
let _allUsers = [];
let _allRoles = [];

function escapeHtml(s) {
    return String(s ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function setRowMessage(msg) {
    const tbody = qs("#usersBody");
    if (!tbody) return;
    tbody.innerHTML = `<tr><td colspan="5" class="muted">${escapeHtml(msg)}</td></tr>`;
}

async function fillRoles() {
    const sel = qs("#selRole");
    if (!sel) return;

    sel.innerHTML = `<option value="">Chargement...</option>`;

    try {
        const data = await fetchJson("/api/admin/roles?ts=" + Date.now());
        const items = Array.isArray(data?.items) ? data.items : [];
        _allRoles = items;

        if (!items.length) {
            sel.innerHTML = `<option value="">Aucun rôle</option>`;
            return;
        }

        sel.innerHTML = items.map(r => `
            <option value="${r.id}">${escapeHtml(r.name)} (${escapeHtml(r.code)})</option>
        `).join("");

        sel.value = String(items[0].id);
    } catch (e) {
        console.error(e);
        sel.innerHTML = `<option value="">Erreur chargement rôles</option>`;
    }
}

function resetForm() {
    _editingUserId = null;

    qs("#txtUsername").value = "";
    qs("#txtPassword").value = "";

    const sel = qs("#selRole");
    if (sel && _allRoles.length > 0) {
        sel.value = String(_allRoles[0].id);
    }

    const btnCreate = qs("#btnCreate");
    if (btnCreate) btnCreate.textContent = "Créer";
}

function renderUsers(list) {
    const tbody = qs("#usersBody");
    if (!tbody) return;

    if (!list.length) {
        setRowMessage("Aucun utilisateur.");
        return;
    }

    const canEditUsers = hasPermission("users.edit");
    const canToggleUsers = hasPermission("users.edit");

    tbody.innerHTML = list.map(u => {
        const id = u.id ?? "";
        const username = escapeHtml(u.username ?? u.Username ?? u.userName ?? "");
        const role = escapeHtml(u.role ?? u.Role ?? "");
        const roleCode = escapeHtml(u.roleCode ?? u.RoleCode ?? "");
        const actif = !!(u.isActive ?? u.IsActive);

        return `
            <tr>
                <td>${id}</td>
                <td>${username}</td>
                <td>
                    <div style="font-weight:700">${role}</div>
                    ${roleCode ? `<div class="muted" style="font-size:12px">${roleCode}</div>` : ""}
                </td>
                <td>${actif ? "Oui" : "Non"}</td>
 <td>
    <div style="display:flex;gap:8px;flex-wrap:wrap">
        ${canEditUsers ? `
            <button class="btn btnEditUser" type="button" data-edit-id="${id}">
                Modifier
            </button>

            <button class="btn btnRole" type="button" data-role-id="${id}">
                Changer rôle
            </button>
        ` : ""}

        ${canEditUsers ? `
            <button class="btn" type="button" data-delete-id="${id}">
                Supprimer
            </button>
        ` : ""}

        ${canToggleUsers ? `
            <button class="btn" type="button" data-id="${id}" data-next="${actif ? "false" : "true"}">
                ${actif ? "Désactiver" : "Activer"}
            </button>
        ` : ""}

        ${!canEditUsers && !canToggleUsers ? `<span class="muted">—</span>` : ""}
    </div>
</td>
            </tr>
        `;
    }).join("");

    tbody.querySelectorAll("button[data-id]").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            const next = btn.getAttribute("data-next") === "true";

            try {
                await fetchJson(`/api/admin/users/${encodeURIComponent(id)}/active?active=${next}`, {
                    method: "PATCH"
                });
                await loadUsers();
            } catch (e) {
                alert("Erreur: " + (e.message || e));
            }
        });
    });

    tbody.querySelectorAll("button[data-role-id]").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-role-id");
            await changeUserRole(id);
        });
    });

    tbody.querySelectorAll("button[data-edit-id]").forEach(btn => {
        btn.addEventListener("click", () => {
            const id = btn.getAttribute("data-edit-id");
            startEditUser(id);
        });
    });

    tbody.querySelectorAll("button[data-delete-id]").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-delete-id");
            await deleteUser(id);
        });
    });
}

let _editingUserId = null;

function startEditUser(userId) {
    const user = _allUsers.find(x => String(x.id) === String(userId));
    if (!user) {
        alert("Utilisateur introuvable.");
        return;
    }

    _editingUserId = Number(user.id);

    qs("#txtUsername").value = user.username || "";
    qs("#txtPassword").value = "";

    const sel = qs("#selRole");
    if (sel && user.roleId) {
        sel.value = String(user.roleId);
    }

    const btnCreate = qs("#btnCreate");
    if (btnCreate) btnCreate.textContent = "Enregistrer modification";
}

function applySearch() {
    const q = (qs("#txtSearch").value || "").trim().toLowerCase();

    const filtered = !q
        ? _allUsers
        : _allUsers.filter(u => {
            const t = `${u.id} ${u.username || u.userName || ""} ${u.role || ""} ${u.roleCode || ""}`.toLowerCase();
            return t.includes(q);
        });

    renderUsers(filtered);
}

async function changeUserRole(userId) {
    const user = _allUsers.find(x => String(x.id) === String(userId));
    if (!user) {
        alert("Utilisateur introuvable.");
        return;
    }

    if (!_allRoles.length) {
        await fillRoles();
    }

    const currentRoleId = Number(user.roleId || 0);

    const optionsText = _allRoles.map(r => {
        const selected = Number(r.id) === currentRoleId ? " (actuel)" : "";
        return `${r.id} - ${r.name} (${r.code})${selected}`;
    }).join("\n");

    const answer = prompt(
        `Choisissez le nouvel ID de rôle pour ${user.username} :\n\n${optionsText}`,
        currentRoleId || ""
    );

    if (answer === null) return;

    const roleId = Number(answer);
    if (!roleId) {
        alert("ID rôle invalide.");
        return;
    }

    try {
        await fetchJson(`/api/admin/users/${encodeURIComponent(userId)}/role`, {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ roleId })
        });

        await loadUsers();
        alert("Rôle mis à jour.");
    } catch (e) {
        alert("Erreur: " + (e.message || e));
    }
}

async function loadUsers() {
    setRowMessage("Chargement...");

    try {
        const data = await fetchJson("/api/admin/users?ts=" + Date.now(), { cache: "no-store" });
        const items = data?.items || data?.data?.items || data?.data || [];

        console.log("[ADMIN-USERS] API raw =", data);
        console.log("[ADMIN-USERS] items =", items);

        _allUsers = Array.isArray(items) ? items : [];
        applySearch();
    } catch (e) {
        console.error(e);
        setRowMessage("Erreur API: " + (e.message || e));
    }
}

async function createUser() {
    const username = (qs("#txtUsername").value || "").trim();
    const password = (qs("#txtPassword").value || "").trim();
    const roleId = Number(qs("#selRole").value || 0);

    if (!username) {
        alert("Username obligatoire.");
        return;
    }

    if (!_editingUserId && !password) {
        alert("Mot de passe obligatoire pour un nouvel utilisateur.");
        return;
    }

    if (!roleId) {
        alert("Sélectionnez un rôle.");
        return;
    }

    const btn = qs("#btnCreate");
    const oldText = btn?.textContent || "";

    try {
        if (btn) {
            btn.disabled = true;
            btn.textContent = _editingUserId ? "Modification..." : "Création...";
        }

        if (_editingUserId) {
            await fetchJson(`/api/admin/users/${encodeURIComponent(_editingUserId)}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password, roleId })
            });

            alert("Utilisateur modifié.");
        } else {
            await fetchJson("/api/admin/users", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password, roleId })
            });

            alert("Utilisateur créé.");
        }

        resetForm();
        await fillRoles();
        await loadUsers();
    } catch (e) {
        alert("Erreur: " + (e.message || e));
    } finally {
        if (btn) {
            btn.disabled = false;
            btn.textContent = _editingUserId ? "Enregistrer modification" : "Créer";
        }
    }
}

async function deleteUser(userId) {
    const user = _allUsers.find(x => String(x.id) === String(userId));
    if (!user) {
        alert("Utilisateur introuvable.");
        return;
    }

    const ok = confirm(`Supprimer l'utilisateur "${user.username}" ?`);
    if (!ok) return;

    try {
        await fetchJson(`/api/admin/users/${encodeURIComponent(userId)}`, {
            method: "DELETE"
        });

        if (_editingUserId === Number(userId)) {
            resetForm();
        }

        await loadUsers();
        alert("Utilisateur supprimé.");
    } catch (e) {
        alert("Erreur: " + (e.message || e));
    }
}


async function initAdminUsersPage() {
    requireAdminAuth();

    // ❌ SUPPRIMER CETTE PARTIE
    // const h1 = document.getElementById("pageTitle");
    // if (h1) h1.textContent = "Utilisateurs";

    // ✅ garder uniquement ça
    window.setAdminPageTitle?.("Utilisateurs", "👤");

    const bar = document.querySelector(".admin-pagebar");
    if (bar) {
        bar.style.display = "block";
        bar.style.visibility = "visible";
        bar.style.height = "auto";
        bar.style.opacity = "1";
    }

    await loadAdminPermissions(true);

    if (!requirePermission("users.view")) return;

    await fillRoles();

const btnCreate = qs("#btnCreate");
if (btnCreate) {
    if (hasPermission("users.create") || hasPermission("users.edit")) {
        btnCreate.addEventListener("click", createUser);
    } else {
        btnCreate.style.display = "none";
    }
}

    qs("#btnReset")?.addEventListener("click", resetForm);
    qs("#txtSearch")?.addEventListener("input", applySearch);

    await loadUsers();
}
window.initAdminUsersPage = initAdminUsersPage;