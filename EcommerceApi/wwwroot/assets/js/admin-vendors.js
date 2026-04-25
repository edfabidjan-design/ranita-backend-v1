(() => {
    "use strict";

    const qs = (s, r = document) => r.querySelector(s);
    const esc = (s) => String(s ?? "").replace(/[&<>"']/g, c => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[c]));

    const deleteVendorModal = qs("#deleteVendorModal");
    const deleteVendorPassword = qs("#deleteVendorPassword");
    const deleteVendorError = qs("#deleteVendorError");
    const deleteVendorTarget = qs("#deleteVendorTarget");
    const btnCancelDeleteVendor = qs("#btnCancelDeleteVendor");
    const btnConfirmDeleteVendor = qs("#btnConfirmDeleteVendor");

    let pendingDeleteVendorId = 0;

    let vendorsAutoRefreshTimer = null;
    let isProcessingAction = false;

    function startVendorsAutoRefresh() {
        stopVendorsAutoRefresh();

        vendorsAutoRefreshTimer = setInterval(async () => {

            if (document.hidden) return;
            if (isProcessingAction) return;
            if (deleteVendorModal && deleteVendorModal.style.display === "flex") return;

            try {
                await loadVendors();
            } catch { }

        }, 5000);
    }

    function stopVendorsAutoRefresh() {
        if (vendorsAutoRefreshTimer) {
            clearInterval(vendorsAutoRefreshTimer);
            vendorsAutoRefreshTimer = null;
        }
    }

    function openDeleteVendorModal(id, name) {
        pendingDeleteVendorId = id;
        deleteVendorTarget.textContent = name || "";
        deleteVendorPassword.value = "";
        deleteVendorError.style.display = "none";
        deleteVendorModal.style.display = "flex";
    }

    function closeDeleteVendorModal() {
        deleteVendorModal.style.display = "none";
    }


    function getStatus() {
        const sel = qs("#selVendorStatus");
        return sel ? Number(sel.value) : -1;
    }

    function statusLabel(st) {
        st = Number(st);
        if (st === 0) return "En attente de validation";
        if (st === 1) return "Validé";
        if (st === 2) return "Refusé";
        if (st === 3) return "Retiré";
        return String(st);
    }

    function statusBadge(st) {
        st = Number(st);

        if (st === 0) {
            return `<span style="
                display:inline-flex;
                align-items:center;
                padding:5px 10px;
                border-radius:999px;
                font-size:12px;
                font-weight:800;
                background:rgba(250,204,21,.14);
                color:#facc15;
                border:1px solid rgba(250,204,21,.35);
            ">En attente validation</span>`;
        }

        if (st === 1) {
            return `<span style="
                display:inline-flex;
                align-items:center;
                padding:5px 10px;
                border-radius:999px;
                font-size:12px;
                font-weight:800;
                background:rgba(34,197,94,.14);
                color:#86efac;
                border:1px solid rgba(34,197,94,.35);
            ">Validé</span>`;
        }

        if (st === 2) {
            return `<span style="
                display:inline-flex;
                align-items:center;
                padding:5px 10px;
                border-radius:999px;
                font-size:12px;
                font-weight:800;
                background:rgba(239,68,68,.14);
                color:#fca5a5;
                border:1px solid rgba(239,68,68,.35);
            ">Refusé</span>`;
        }

        if (st === 3) {
            return `<span style="
                display:inline-flex;
                align-items:center;
                padding:5px 10px;
                border-radius:999px;
                font-size:12px;
                font-weight:800;
                background:rgba(148,163,184,.14);
                color:#cbd5e1;
                border:1px solid rgba(148,163,184,.35);
            ">Retiré</span>`;
        }

        return `<span>${st}</span>`;
    }


    function yesNoBadge(value, yesText, noText) {
        if (value) {
            return `<span style="
            display:inline-flex;
            align-items:center;
            padding:5px 10px;
            border-radius:999px;
            font-size:12px;
            font-weight:800;
            background:rgba(34,197,94,.14);
            color:#86efac;
            border:1px solid rgba(34,197,94,.35);
        ">${yesText}</span>`;
        }

        return `<span style="
        display:inline-flex;
        align-items:center;
        padding:5px 10px;
        border-radius:999px;
        font-size:12px;
        font-weight:800;
        background:rgba(239,68,68,.14);
        color:#fca5a5;
        border:1px solid rgba(239,68,68,.35);
    ">${noText}</span>`;
    }



    async function loadVendors() {
        const tbody = qs("#vendorsBody");
        if (!tbody) return;

        const status = getStatus();

        tbody.innerHTML = `<tr><td colspan="4" class="muted" style="padding:12px 8px">Chargement...</td></tr>`;

        try {
            const data = await fetchJson(`/api/admin/vendors?status=${encodeURIComponent(status)}&ts=${Date.now()}`);
            const items = data?.items || [];

            if (!items.length) {
                tbody.innerHTML = `<tr><td colspan="4" class="muted" style="padding:12px 8px">Aucun vendeur.</td></tr>`;
                return;
            }

            tbody.innerHTML = items.map(v => {
                const createdTxt = v.createdAt ? new Date(v.createdAt).toLocaleString("fr-FR") : "-";
                const st = Number(v.status ?? v.Status ?? 0);

                // =======================
                // 1) ACTIONS CONSULTATION
                // =======================
                let viewActions = `
                <a class="btn" href="/admin-vendor-products.html?vendorId=${encodeURIComponent(v.id)}">
                    Voir produits
                </a>
            `;

                if (v.contractPdfPath) {
                    viewActions += `
                    <a class="btn" href="${encodeURI(v.contractPdfPath)}" target="_blank" rel="noopener">
                        📄 Contrat
                    </a>
                `;
                }

                if (v.signedContractPath) {
                    viewActions += `
                    <a class="btn" href="${encodeURI(v.signedContractPath)}" target="_blank" rel="noopener">
                        📎 Contrat signé
                    </a>
                `;
                }

                // =======================
                // 2) ACTIONS GESTION
                // =======================
                let manageActions = "";

                const canApproveNow =
                    (st === 0 && !!v.signedContractPath) ||
                    (st === 2 && !!v.signedContractReceivedAt);

                const canRejectNow =
                    (st === 0 || st === 2);

                if (canApproveNow) {
                    manageActions += `
        <button class="btn btnGreen" data-act="approve" data-id="${v.id}">
            Valider
        </button>
    `;
                }

                if (canRejectNow) {
                    manageActions += `
        <button class="btn" data-act="reject" data-id="${v.id}">
            Refuser
        </button>

        <button class="btn" data-act="resend" data-id="${v.id}">
            📩 Renvoyer
        </button>
    `;
                }

                if (st === 0 && !v.signedContractPath) {
                    manageActions = `
        <button class="btn" disabled style="opacity:.55;cursor:not-allowed">
            Contrat signé requis
        </button>

        <button class="btn" data-act="reject" data-id="${v.id}">
            Refuser
        </button>

        <button class="btn" data-act="resend" data-id="${v.id}">
            📩 Renvoyer
        </button>
    `;
                }

                if (st === 1) {
                    manageActions += `
        <button class="btn" data-act="disable" data-id="${v.id}">
            Retirer
        </button>
    `;
                }

                if (st === 3) {
                    manageActions += `
        <button class="btn btnGreen" data-act="enable" data-id="${v.id}">
            Réactiver
        </button>
    `;
                }
                

                // =======================
                // 3) ACTION DANGER
                // =======================
                let dangerAction = `
                <button class="btn" style="background:#ef4444;color:white"
                    data-act="delete" data-id="${v.id}">
                    🗑 Supprimer
                </button>
            `;

                // =======================
                // 4) RENDU FINAL ACTIONS
                // =======================
                let actions = `
                <div class="vendor-actions-row">
                    <div class="vendor-actions-group vendor-actions-view">
                        ${viewActions}
                    </div>

                    <div class="vendor-actions-group vendor-actions-manage">
                        ${manageActions}
                    </div>

                    <div class="vendor-actions-group vendor-actions-danger">
                        ${dangerAction}
                    </div>
                </div>
            `;

                return `
<tr style="border-top:1px solid rgba(148,163,184,.15)">
    <td style="padding:10px 8px">
        <div style="
            display:flex;
            align-items:center;
            justify-content:space-between;
            gap:10px;
            flex-wrap:wrap;
        ">
            <strong>${esc(v.name || v.Name || "")}</strong>
            ${statusBadge(st)}
        </div>

${st === 0 ? `
    <div style="
        font-size:12px;
        margin-top:6px;
        color:#9ca3af;
        line-height:1.4;
    ">
        ⚠️ À valider après retour de l’accord signé
    </div>
` : ""}

${st === 2 && v.signedContractReceivedAt ? `
    <div style="
        font-size:12px;
        margin-top:6px;
        color:#facc15;
        line-height:1.4;
        font-weight:700;
    ">
        📩 Contrat renvoyé par le vendeur — dossier à revoir
    </div>
` : ""}

    </td>

    <td style="padding:10px 8px">${esc(v.email || v.Email || "")}</td>

    <td style="padding:10px 8px">
        ${yesNoBadge(v.termsEmailSent, "Envoyé", "Non envoyé")}
        ${v.termsEmailSentAt ? `
            <div style="font-size:11px;color:#94a3b8;margin-top:6px">
                ${esc(new Date(v.termsEmailSentAt).toLocaleString("fr-FR"))}
            </div>
        ` : ""}
    </td>

<td style="padding:10px 8px">
    ${yesNoBadge(!!v.signedContractReceivedAt, "Reçu", "En attente")}
    ${v.signedContractReceivedAt ? `
        <div style="font-size:11px;color:#94a3b8;margin-top:6px">
            ${esc(new Date(v.signedContractReceivedAt).toLocaleString("fr-FR"))}
        </div>
    ` : ""}
</td>

    <td style="padding:10px 8px">${esc(createdTxt)}</td>

    <td style="padding:10px 8px">
        ${actions}
    </td>
</tr>
`;
            }).join("");

        } catch (e) {
            tbody.innerHTML = `<tr><td colspan="4" class="muted" style="padding:12px 8px">${esc(e.message || "Erreur")}</td></tr>`;
        }
    }

    async function actVendor(id, act) {

        if (act === "approve") {
            await fetchJson(`/api/admin/vendors/${id}/approve`, { method: "POST" });
        }

        else if (act === "reject") {
            await fetchJson(`/api/admin/vendors/${id}/reject`, { method: "POST" });
        }

        else if (act === "disable") {
            await fetchJson(`/api/admin/vendors/${id}/disable?disableProducts=true`, { method: "POST" });
        }

        else if (act === "enable") {
            await fetchJson(`/api/admin/vendors/${id}/enable?enableProducts=true`, { method: "POST" });
        }

        else if (act === "resend") {
            await fetchJson(`/api/admin/vendors/${id}/resend-terms`, { method: "POST" });
        }

        // ❌ ON NE GÈRE PAS delete ici
    }

    window.initAdminVendorsPage = function () {
        loadVendors();
        startVendorsAutoRefresh();

        qs("#selVendorStatus")?.addEventListener("change", loadVendors);

        document.addEventListener("click", async (e) => {
            const btn = e.target.closest("button[data-act]");
            if (!btn) return;

            const id = Number(btn.dataset.id);
            const act = btn.dataset.act;

            if (act === "approve") {
                if (!confirm("Valider cette boutique ? Vérifie que le vendeur a bien renvoyé l’accord signé.")) return;
            }

            if (act === "reject") {
                if (!confirm("Refuser cette boutique ?")) return;
            }

            if (act === "disable") {
                if (!confirm("Retirer ce vendeur ? Ses produits seront désactivés.")) return;
            }

            if (act === "resend") {
                if (!confirm("Renvoyer les conditions au vendeur ?")) return;
            }


            if (act === "delete") {
                const row = btn.closest("tr");
                const name = row?.querySelector("strong")?.textContent || "";

                openDeleteVendorModal(id, name);
                return;
            }

            try {
                isProcessingAction = true;

                await actVendor(id, act);

                if (act === "resend") {
                    alert("Email renvoyé au vendeur.");
                }

                await loadVendors();

            } catch (err) {
                alert(err.message || "Erreur");
            }
            finally {
                isProcessingAction = false;
            }
        });
    };
    window.addEventListener("beforeunload", stopVendorsAutoRefresh);

    btnCancelDeleteVendor?.addEventListener("click", closeDeleteVendorModal);

btnConfirmDeleteVendor?.addEventListener("click", async () => {
    const password = deleteVendorPassword.value;

    if (!password) {
        deleteVendorError.style.display = "block";
        deleteVendorError.textContent = "Mot de passe requis";
        return;
    }

    try {
        await fetchJson(`/api/admin/vendors/${pendingDeleteVendorId}`, {
            method: "DELETE",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ password })
        });

        closeDeleteVendorModal();
        alert("Supprimé !");
        loadVendors();
    } catch (e) {
        deleteVendorError.style.display = "block";
       deleteVendorError.textContent =
    [e?.message, e?.detail].filter(Boolean).join(" | ") || "Erreur serveur";
        console.error("DELETE VENDOR ERROR:", e);
    }
});
})();