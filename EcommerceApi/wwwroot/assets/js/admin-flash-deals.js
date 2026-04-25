const API = "/api/admin/flash-deals";

function byId(id) {
    return document.getElementById(id);
}

function escapeHtml(str) {
    return String(str ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

let editingFlashDealId = null;
let productsCache = [];

function toLocalDateTimeInputValue(dateStr) {
    if (!dateStr) return "";
    const d = new Date(dateStr);
    if (Number.isNaN(d.getTime())) return "";
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, "0");
    const day = String(d.getDate()).padStart(2, "0");
    const hours = String(d.getHours()).padStart(2, "0");
    const minutes = String(d.getMinutes()).padStart(2, "0");
    return `${year}-${month}-${day}T${hours}:${minutes}`;
}

function resetFlashDealForm() {
    editingFlashDealId = null;

    const product = byId("fdProductId");
    const discount = byId("fdDiscountPercent");
    const start = byId("fdStartAt");
    const end = byId("fdEndAt");
    const order = byId("fdDisplayOrder");
    const active = byId("fdIsActive");
    const btnSave = byId("btnSaveFlashDeal");
    const btnCancel = byId("btnCancelFlashDealEdit");

    if (product) product.value = "";
    if (discount) discount.value = "";
    if (start) start.value = "";
    if (end) end.value = "";
    if (order) order.value = "1";
    if (active) active.checked = true;
    if (btnSave) btnSave.textContent = "Enregistrer";
    if (btnCancel) btnCancel.style.display = "none";
}

function fillFlashDealForm(item) {
    editingFlashDealId = item.id;

    const product = byId("fdProductId");
    const discount = byId("fdDiscountPercent");
    const start = byId("fdStartAt");
    const end = byId("fdEndAt");
    const order = byId("fdDisplayOrder");
    const active = byId("fdIsActive");
    const btnSave = byId("btnSaveFlashDeal");
    const btnCancel = byId("btnCancelFlashDealEdit");

    if (product) product.value = String(item.productId || "");
    if (discount) discount.value = item.discountPercent ?? "";
    if (start) start.value = toLocalDateTimeInputValue(item.startAt);
    if (end) end.value = toLocalDateTimeInputValue(item.endAt);
    if (order) order.value = item.displayOrder ?? 1;
    if (active) active.checked = !!item.isActive;
    if (btnSave) btnSave.textContent = "Modifier";
    if (btnCancel) btnCancel.style.display = "";

    window.scrollTo({ top: 0, behavior: "smooth" });
}

function getPayload() {
    return {
        productId: Number(byId("fdProductId")?.value || 0),
        discountPercent: Number(byId("fdDiscountPercent")?.value || 0),
        startAt: byId("fdStartAt")?.value || "",
        endAt: byId("fdEndAt")?.value || "",
        displayOrder: Number(byId("fdDisplayOrder")?.value || 1),
        isActive: !!byId("fdIsActive")?.checked
    };
}

async function loadProducts() {
    try {
        const data = await window.fetchJson(`${API}/products`);
        productsCache = Array.isArray(data) ? data : [];

        const select = byId("fdProductId");
        if (!select) return;

        select.innerHTML =
            `<option value="">Choisir un produit</option>` +
            productsCache.map(p => `
                <option value="${p.id}">
                    ${escapeHtml(p.name)} — ${Number(p.price || 0).toLocaleString("fr-FR")} FCFA
                </option>
            `).join("");
    } catch (e) {
        console.error("Erreur loadProducts:", e);
    }
}

async function loadFlashDeals() {
    try {
        const data = await window.fetchJson(API);
        const list = byId("flashDealsList");
        if (!list) return;

        if (!Array.isArray(data) || !data.length) {
            list.innerHTML = `
                <div class="box" style="margin-bottom:8px">
                    Aucun Flash Deal pour le moment.
                </div>
            `;
            return;
        }

        list.innerHTML = data.map(x => `
            <div class="box" style="margin-bottom:10px;padding:14px;border:1px solid rgba(255,255,255,.08);border-radius:14px;">
                <div style="display:flex;justify-content:space-between;gap:16px;flex-wrap:wrap;align-items:flex-start;">
                    <div style="display:flex;gap:14px;flex:1;min-width:320px;align-items:flex-start;">
                        ${x.productImageUrl ? `
                            <img
                                src="${escapeHtml(x.productImageUrl)}"
                                alt="${escapeHtml(x.productName || "")}"
                                style="width:88px;height:88px;object-fit:cover;border-radius:12px;border:1px solid rgba(255,255,255,.08);background:#fff;flex:0 0 88px;">
                        ` : `
                            <div style="width:88px;height:88px;border-radius:12px;border:1px dashed rgba(255,255,255,.12);display:flex;align-items:center;justify-content:center;color:#9ca3af;flex:0 0 88px;">
                                Aucun visuel
                            </div>
                        `}

                        <div style="flex:1;min-width:220px;">
                            <div style="font-weight:900;font-size:16px;">
                                ${escapeHtml(x.productName || "")}
                            </div>

                            <div style="font-size:12px;color:#9ca3af;margin-top:6px;line-height:1.5;">
                                Réduction : ${Number(x.discountPercent || 0)}%
                                · Ordre : ${x.displayOrder ?? 1}
                                · ${x.isActive ? "Actif" : "Inactif"}
                            </div>

                            <div style="font-size:12px;color:#9ca3af;margin-top:6px;line-height:1.5;">
                                Début : ${escapeHtml(new Date(x.startAt).toLocaleString("fr-FR"))}
                                <br>
                                Fin : ${escapeHtml(new Date(x.endAt).toLocaleString("fr-FR"))}
                            </div>

                            <div style="font-size:12px;color:#9ca3af;margin-top:8px;line-height:1.5;">
                                Prix : ${Number(x.productPrice || 0).toLocaleString("fr-FR")} FCFA
                                ${x.productPricePromo ? ` · Promo produit : ${Number(x.productPricePromo).toLocaleString("fr-FR")} FCFA` : ""}
                                · Stock : ${x.productStock ?? 0}
                            </div>
                        </div>
                    </div>

                    <div style="display:flex;gap:8px;flex-wrap:wrap;">
                        <button class="btn btnGhost" type="button" onclick="editFlashDeal(${x.id})">Modifier</button>
                        <button class="btn" type="button" onclick="deleteFlashDeal(${x.id})">Supprimer</button>
                    </div>
                </div>
            </div>
        `).join("");

        window.editFlashDeal = function (id) {
            const item = data.find(x => Number(x.id) === Number(id));
            if (item) fillFlashDealForm(item);
        };

        window.deleteFlashDeal = deleteFlashDeal;
    } catch (e) {
        console.error(e);
        const list = byId("flashDealsList");
        if (list) {
            list.innerHTML = `
                <div class="box" style="margin-bottom:8px">
                    Erreur chargement Flash Deals : ${escapeHtml(String(e.message || e))}
                </div>
            `;
        }
    }
}

async function saveFlashDeal() {
    try {
        const payload = getPayload();

        if (!payload.productId) {
            alert("Le produit est obligatoire.");
            return;
        }

        if (!payload.discountPercent || payload.discountPercent <= 0 || payload.discountPercent >= 100) {
            alert("La réduction doit être comprise entre 1 et 99.");
            return;
        }

        if (!payload.startAt || !payload.endAt) {
            alert("Les dates de début et de fin sont obligatoires.");
            return;
        }

        if (new Date(payload.endAt) <= new Date(payload.startAt)) {
            alert("La date de fin doit être supérieure à la date de début.");
            return;
        }

        const url = editingFlashDealId ? `${API}/${editingFlashDealId}` : API;
        const method = editingFlashDealId ? "PUT" : "POST";

        await window.fetchJson(url, {
            method,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        alert(editingFlashDealId ? "Flash Deal modifié." : "Flash Deal ajouté.");
        resetFlashDealForm();
        await loadFlashDeals();
    } catch (e) {
        console.error(e);
        alert("Erreur : " + (e.message || e));
    }
}

async function deleteFlashDeal(id) {
    const ok = confirm("Supprimer ce Flash Deal ?");
    if (!ok) return;

    try {
        await window.fetchJson(`${API}/${id}`, { method: "DELETE" });

        if (editingFlashDealId === id) {
            resetFlashDealForm();
        }

        await loadFlashDeals();
        alert("Flash Deal supprimé.");
    } catch (e) {
        console.error(e);
        alert("Erreur suppression : " + (e.message || e));
    }
}

document.addEventListener("DOMContentLoaded", async () => {
    const btnSave = byId("btnSaveFlashDeal");
    const btnCancel = byId("btnCancelFlashDealEdit");

    if (btnSave) btnSave.addEventListener("click", saveFlashDeal);
    if (btnCancel) btnCancel.addEventListener("click", resetFlashDealForm);

    await loadProducts();
    await loadFlashDeals();
});