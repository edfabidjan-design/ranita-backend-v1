const API = "/api/admin/home";
const EVENTS_API = "/api/admin/home/events";

const PERMS = {
    promoEdit: "homecms.promo.edit",
    promoPublish: "homecms.promo.publish",

    reviewsEdit: "homecms.reviews.edit",
    reviewsDelete: "homecms.reviews.delete",

    sectionsEdit: "homecms.sections.edit",
    sectionsDelete: "homecms.sections.delete",

    itemsEdit: "homecms.items.edit",
    itemsDelete: "homecms.items.delete",

    eventsEdit: "homecms.events.edit",
    eventsDelete: "homecms.events.delete",
    eventsPublish: "homecms.events.publish"
};

function can(permission) {
    return !!window.hasPermission?.(permission);
}

function deny(permission) {
    alert(`Accès refusé. Permission requise : ${permission}`);
    return false;
}

function setDisabledIn(containerSelector, selectors, disabled) {
    const box = qs(containerSelector);
    if (!box) return;

    selectors.forEach(sel => {
        box.querySelectorAll(sel).forEach(el => {
            el.disabled = !!disabled;
        });
    });
}

function setHidden(selector, hidden) {
    const el = qs(selector);
    if (el) el.style.display = hidden ? "none" : "";
}

function applyHomeCmsActionPermissions() {
    const canPromoEdit = can(PERMS.promoEdit);
    const canPromoPublish = can(PERMS.promoPublish);

    const canReviewsEdit = can(PERMS.reviewsEdit);

    const canSectionsEdit = can(PERMS.sectionsEdit);
    const canItemsEdit = can(PERMS.itemsEdit);

    const canEventsEdit = can(PERMS.eventsEdit);
    const canEventsPublish = can(PERMS.eventsPublish);

    setHidden("#btnUploadPromoBg", !canPromoEdit);
    setHidden("#btnSavePromo", !canPromoEdit);

    setHidden("#btnUploadAvatar", !canReviewsEdit);
    setHidden("#btnSaveReview", !canReviewsEdit);
    setHidden("#btnCancelReviewEdit", !canReviewsEdit);

    setHidden("#btnSaveSection", !canSectionsEdit);
    setHidden("#btnCancelSectionEdit", !canSectionsEdit);

    setHidden("#btnUploadSectionItemImage", !canItemsEdit);
    setHidden("#btnSaveSectionItem", !canItemsEdit);
    setHidden("#btnCancelSectionItemEdit", !canItemsEdit);

    setHidden("#btnUploadEventDesktop", !canEventsEdit);
    setHidden("#btnSaveEvent", !canEventsEdit);
    setHidden("#btnCancelEventEdit", !canEventsEdit);

    setDisabledIn("#promoPanel", [
        "input", "textarea", "select", "button"
    ], !canPromoEdit);

    setDisabledIn("#reviewsPanel", [
        "input", "textarea", "select", "button#btnUploadAvatar", "button#btnSaveReview", "button#btnCancelReviewEdit"
    ], !canReviewsEdit);

    setDisabledIn("#sectionsPanel", [
        "input", "textarea", "select", "button#btnSaveSection", "button#btnCancelSectionEdit"
    ], !canSectionsEdit);

    setDisabledIn("#sectionItemsPanel", [
        "input", "textarea", "select", "button#btnUploadSectionItemImage", "button#btnSaveSectionItem", "button#btnCancelSectionItemEdit"
    ], !canItemsEdit);

    setDisabledIn("#eventsPanel", [
        "input", "textarea", "select", "button#btnUploadEventDesktop", "button#btnSaveEvent", "button#btnCancelEventEdit"
    ], !canEventsEdit);

    if (!canPromoPublish) {
        qs("#promoIsActive") && (qs("#promoIsActive").disabled = true);
        qs("#promoStartAt") && (qs("#promoStartAt").disabled = true);
        qs("#promoEndAt") && (qs("#promoEndAt").disabled = true);
    }

    if (!canEventsPublish) {
        qs("#eventIsActive") && (qs("#eventIsActive").disabled = true);
        qs("#eventIsFeatured") && (qs("#eventIsFeatured").disabled = true);
        qs("#eventStartDate") && (qs("#eventStartDate").disabled = true);
        qs("#eventEndDate") && (qs("#eventEndDate").disabled = true);
        qs("#eventIsSeasonal") && (qs("#eventIsSeasonal").disabled = true);
        qs("#eventAutoActivate") && (qs("#eventAutoActivate").disabled = true);
        qs("#eventSeasonKey") && (qs("#eventSeasonKey").disabled = true);
        qs("#eventAutoScheduleType") && (qs("#eventAutoScheduleType").disabled = true);
        qs("#eventMonthStart") && (qs("#eventMonthStart").disabled = true);
        qs("#eventDayStart") && (qs("#eventDayStart").disabled = true);
        qs("#eventMonthEnd") && (qs("#eventMonthEnd").disabled = true);
        qs("#eventDayEnd") && (qs("#eventDayEnd").disabled = true);
        qs("#eventPriority") && (qs("#eventPriority").disabled = true);
    }
}
function byId(id) {
    return document.getElementById(id);
}

function qs(selector, root = document) {
    if (!selector) return null;
    if (selector.startsWith("#")) return root.querySelector(selector);
    return root.querySelector(`#${selector}`) || root.querySelector(selector);
}

function qsa(selector, root = document) {
    return Array.from(root.querySelectorAll(selector));
}

function on(target, event, handler) {
    const el = typeof target === "string" ? qs(target) : target;
    if (el) el.addEventListener(event, handler);
    return el;
}

function escapeHtml(str) {
    return String(str ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function toDateTimeLocal(value) {
    if (!value) return "";
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return "";
    const pad = n => String(n).padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

/* =========================================================
   TABS
========================================================= */


function show(id) {
    ["promoPanel", "reviewsPanel", "sectionsPanel", "sectionItemsPanel", "eventsPanel"].forEach(x => {
        const el = byId(x);
        if (el) el.style.display = (x === id ? "" : "none");
    });

    document.querySelectorAll(".homeTabBtn").forEach(btn => {
        btn.classList.remove("active");
    });

    const activeMap = {
        promoPanel: "tabPromo",
        reviewsPanel: "tabReviews",
        sectionsPanel: "tabSections",
        sectionItemsPanel: "tabSectionItems",
        eventsPanel: "tabEvents"
    };

    const activeBtn = byId(activeMap[id]);
    if (activeBtn) activeBtn.classList.add("active");
}

/* =========================================================
   PROMO
========================================================= */

async function loadPromo() {
    try {
        const r = await fetch(`${API}/promo-banner`, { cache: "no-store" });
        if (!r.ok) return;

        const text = await r.text();
        if (!text) return;

        let data = null;
        try {
            data = JSON.parse(text);
        } catch {
            console.warn("Réponse promo-banner non JSON :", text);
            return;
        }

        if (!data) return;

        byId("promoTitle").value = data.title || "";
        byId("promoSubtitle").value = data.subTitle || "";
        byId("promoDescription").value = data.description || "";
        byId("promoCode").value = data.promoCode || "";

        byId("promoBtn1Text").value = data.primaryButtonText || "";
        byId("promoBtn1Url").value = data.primaryButtonLink || "";

        byId("promoBtn2Text").value = data.secondaryButtonText || "";
        byId("promoBtn2Url").value = data.secondaryButtonLink || "";
        byId("promoSideTitle").value = data.sideTitle || "";
        byId("promoSideText").value = data.sideText || "";

        byId("promoBackgroundImageUrl").value = data.backgroundImageUrl || "";
        byId("promoTheme").value = data.theme || "promo";
        byId("promoAccentColor").value = data.accentColor || "#22c55e";
        byId("promoPosition").value = data.position || "after-hero";
        byId("promoPriority").value = data.priority || 1;
        byId("promoStartAt").value = toDateTimeLocal(data.startAt);
        byId("promoEndAt").value = toDateTimeLocal(data.endAt);
        byId("promoIsActive").checked = !!data.isActive;

        const btn1Url = (data.primaryButtonLink || "").trim();
        const typeEl = qs("promoLinkType");
        const categoryEl = qs("promoCategoryValue");
        const searchEl = qs("promoSearchValue");

        if (categoryEl) categoryEl.value = "";
        if (searchEl) searchEl.value = "";

        if (typeEl) {
            try {
                const u = new URL(btn1Url, location.origin);
                const promo = u.searchParams.get("promo") === "1";
                const category = u.searchParams.get("category") || "";
                const q = u.searchParams.get("q") || "";

                if (promo && q) {
                    typeEl.value = "promo-search";
                    if (searchEl) searchEl.value = q;
                } else if (promo && category) {
                    typeEl.value = "promo-category";
                    if (categoryEl) categoryEl.value = category;
                } else if (promo) {
                    typeEl.value = "promo";
                } else if (category) {
                    typeEl.value = "category";
                    if (categoryEl) categoryEl.value = category;
                } else if (q) {
                    typeEl.value = "search";
                    if (searchEl) searchEl.value = q;
                } else {
                    typeEl.value = "custom";
                }
            } catch {
                typeEl.value = "custom";
            }

            updatePromoLinkTypeUI();

            if (typeEl.value === "custom") {
                qs("promoBtn1Url").value = btn1Url;
            }
        }
    } catch (e) {
        console.error("Erreur loadPromo:", e);
    }
}

async function uploadEventDesktop() {

    if (!can(PERMS.eventsEdit)) return deny(PERMS.eventsEdit);
    try {
        const file = byId("eventDesktopImageFile")?.files?.[0];
        if (!file) {
            alert("Choisis une image.");
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        const r = await fetch(`${API}/upload-event-image`, {
            method: "POST",
            body: formData
        });

        const text = await r.text();
        let data = null;

        try {
            data = text ? JSON.parse(text) : null;
        } catch { }

        if (!r.ok) {
            throw new Error(data?.message || text || "Erreur upload image événement.");
        }

        byId("eventDesktopImageUrl").value = data?.url || "";
        setEventPreview(data?.url || "");

        alert("Image événement uploadée avec succès.");
    } catch (e) {
        console.error(e);
        alert("Erreur upload : " + (e.message || e));
    }
}




/* =========================================================
   REVIEWS
========================================================= */
let editingReviewId = null;
let editingSectionId = null;

async function savePromo() {

    if (!can(PERMS.promoEdit)) return deny(PERMS.promoEdit);
    if ((qs("promoIsActive")?.checked || qs("promoStartAt")?.value || qs("promoEndAt")?.value) && !can(PERMS.promoPublish)) {
        return deny(PERMS.promoPublish);
    }
    try {
        const promoLinkType = qs("promoLinkType")?.value || "custom";

        if (promoLinkType !== "custom") {
            buildPromoPrimaryLink();
        }

        const payload = {
            title: qs("promoTitle")?.value.trim() || "",
            subTitle: qs("promoSubtitle")?.value.trim() || "",
            description: qs("promoDescription")?.value.trim() || "",
            promoCode: qs("promoCode")?.value.trim() || "",

            primaryButtonText: qs("promoBtn1Text")?.value.trim() || "",
            primaryButtonLink: qs("promoBtn1Url")?.value.trim() || "",

            secondaryButtonText: qs("promoBtn2Text")?.value.trim() || "",
            secondaryButtonLink: qs("promoBtn2Url")?.value.trim() || "",

            sideTitle: qs("promoSideTitle")?.value.trim() || "",
            sideText: qs("promoSideText")?.value.trim() || "",

            backgroundImageUrl: qs("promoBackgroundImageUrl")?.value.trim() || "",
            theme: qs("promoTheme")?.value || "promo",
            accentColor: qs("promoAccentColor")?.value.trim() || "#22c55e",
            position: qs("promoPosition")?.value || "after-hero",
            priority: Number(qs("promoPriority")?.value || 1),

            startAt: qs("promoStartAt")?.value || null,
            endAt: qs("promoEndAt")?.value || null,

            isActive: !!qs("promoIsActive")?.checked
        };

        const r = await fetch(`${API}/promo-banner`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Erreur sauvegarde bannière promo.");
        }

        alert("Promo sauvegardée.");
        await loadPromo();
    } catch (e) {
        console.error(e);
        alert("Erreur : " + (e.message || e));
    }
}

async function uploadAvatar() {

    if (!can(PERMS.reviewsEdit)) return deny(PERMS.reviewsEdit);
    try {
        const file = qs("reviewAvatarFile")?.files?.[0];
        if (!file) {
            alert("Choisis une image.");
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        const r = await fetch(`${API}/upload-avatar`, {
            method: "POST",
            body: formData
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Erreur upload avatar.");
        }

        const data = await r.json();
        qs("reviewAvatarUrl").value = data.url || "";

        alert("Avatar uploadé avec succès.");
    } catch (e) {
        console.error(e);
        alert("Erreur upload avatar : " + (e.message || e));
    }
}

async function saveReview() {

    if (!can(PERMS.reviewsEdit)) return deny(PERMS.reviewsEdit);
    try {
        const payload = getReviewPayload();

        if (!payload.customerName) {
            alert("Le nom client est obligatoire.");
            return;
        }

        if (!payload.reviewText) {
            alert("Le texte de l'avis est obligatoire.");
            return;
        }

        const url = editingReviewId
            ? `${API}/reviews/${editingReviewId}`
            : `${API}/reviews`;

        const method = editingReviewId ? "PUT" : "POST";

        const r = await fetch(url, {
            method,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Erreur enregistrement avis.");
        }

        alert(editingReviewId ? "Avis modifié." : "Avis ajouté.");
        resetReviewForm();
        await loadReviews();
    } catch (e) {
        console.error(e);
        alert("Erreur : " + (e.message || e));
    }
}


async function saveSection() {

    if (!can(PERMS.sectionsEdit)) return deny(PERMS.sectionsEdit);
    try {
        const payload = {
            sectionKey: qs("sectionKey")?.value.trim() || "",
            title: qs("sectionTitle")?.value.trim() || "",
            subTitle: qs("sectionSubTitle")?.value.trim() || "",
            description: qs("sectionDescription")?.value.trim() || "",
            displayOrder: Number(qs("sectionDisplayOrder")?.value || 1),
            layoutType: qs("sectionLayoutType")?.value || "services-grid",
            isActive: !!qs("sectionIsActive")?.checked
        };

        if (!payload.sectionKey) {
            alert("Le SectionKey est obligatoire.");
            return;
        }

        if (!payload.title) {
            alert("Le titre est obligatoire.");
            return;
        }

        const url = editingSectionId
            ? `${API}/sections/${editingSectionId}`
            : `${API}/sections`;

        const method = editingSectionId ? "PUT" : "POST";

        const r = await fetch(url, {
            method,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Erreur ajout / modification section.");
        }

        alert(editingSectionId ? "Section modifiée." : "Section ajoutée.");
        resetSectionForm();
        await loadSections();
        await loadSectionOptions();
    } catch (e) {
        console.error(e);
        alert("Erreur : " + (e.message || e));
    }
}


async function uploadSectionItemImage() {

    if (!can(PERMS.itemsEdit)) return deny(PERMS.itemsEdit);
    try {
        const file = qs("itemImageFile")?.files?.[0];
        if (!file) {
            alert("Choisis une image.");
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        const r = await fetch(`${API}/upload-section-item-image`, {
            method: "POST",
            body: formData
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Erreur upload image.");
        }

        const data = await r.json();
        qs("itemImageUrl").value = data.url || "";

        alert("Image uploadée.");
    } catch (e) {
        console.error(e);
        alert("Erreur upload image : " + (e.message || e));
    }
}

async function saveSectionItem() {

    if (!can(PERMS.itemsEdit)) return deny(PERMS.itemsEdit);
    try {
        const payload = getSectionItemPayload();

        if (!payload.homeSectionId) {
            alert("Choisis une section.");
            return;
        }

        if (!payload.title) {
            alert("Le titre est obligatoire.");
            return;
        }

        const currentSection = await getSectionById(payload.homeSectionId);

        if (currentSection?.layoutType === "double-blocks" && !editingSectionItemId) {
            const itemsRes = await fetch(`${API}/section-items`, { cache: "no-store" });
            const items = await itemsRes.json();

            const countForSection = Array.isArray(items)
                ? items.filter(x => Number(x.homeSectionId) === Number(payload.homeSectionId)).length
                : 0;

            if (countForSection >= 4) {
                alert("Cette section de type 2 blocs promo accepte au maximum 4 items.");
                return;
            }
        }

        const url = editingSectionItemId
            ? `${API}/section-items/${editingSectionItemId}`
            : `${API}/section-items`;

        const method = editingSectionItemId ? "PUT" : "POST";

        const r = await fetch(url, {
            method,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Erreur enregistrement item.");
        }

        alert(editingSectionItemId ? "Item modifié." : "Item ajouté.");
        resetSectionItemForm();
        await loadSectionItems();
    } catch (e) {
        console.error(e);
        alert("Erreur : " + (e.message || e));
    }
}

async function saveEvent() {

    if (!can(PERMS.eventsEdit)) return deny(PERMS.eventsEdit);
    if (
        (qs("eventIsActive")?.checked ||
            qs("eventIsFeatured")?.checked ||
            qs("eventStartDate")?.value ||
            qs("eventEndDate")?.value ||
            qs("eventIsSeasonal")?.checked) &&
        !can(PERMS.eventsPublish)
    ) {
        return deny(PERMS.eventsPublish);
    }
    try {
        const payload = getEventPayload();

        if (!payload.title) {
            alert("Le titre est obligatoire.");
            return;
        }

        const url = editingEventId
            ? `${EVENTS_API}/${editingEventId}`
            : EVENTS_API;

        const method = editingEventId ? "PUT" : "POST";

        const r = await fetch(url, {
            method,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Erreur enregistrement campagne.");
        }

        alert(editingEventId ? "Campagne modifiée." : "Campagne ajoutée.");
        resetEventForm();
        await loadEvents();
    } catch (e) {
        console.error(e);
        alert("Erreur : " + (e.message || e));
    }
}

function getReviewPayload() {
    return {
        customerName: qs("reviewCustomerName").value.trim(),
        customerRole: qs("reviewCustomerRole").value.trim(),
        city: qs("reviewCity").value.trim(),
        avatarUrl: qs("reviewAvatarUrl").value.trim(),
        rating: Number(qs("reviewRating").value || 5),
        reviewText: qs("reviewText").value.trim(),
        displayOrder: Number(qs("reviewDisplayOrder").value || 1),
        isVerified: qs("reviewIsVerified").checked,
        isActive: qs("reviewIsActive").checked
    };
}

function resetReviewForm() {
    editingReviewId = null;

    qs("reviewCustomerName").value = "";
    qs("reviewCustomerRole").value = "";
    qs("reviewCity").value = "";
    qs("reviewAvatarUrl").value = "";
    qs("reviewRating").value = "5";
    qs("reviewText").value = "";
    qs("reviewDisplayOrder").value = "1";
    qs("reviewIsVerified").checked = false;
    qs("reviewIsActive").checked = true;

    const fileInput = qs("reviewAvatarFile");
    if (fileInput) fileInput.value = "";

    qs("btnSaveReview").textContent = "Enregistrer";
    qs("btnCancelReviewEdit").style.display = "none";
}

function fillReviewForm(review) {
    editingReviewId = review.id;

    qs("reviewCustomerName").value = review.customerName || "";
    qs("reviewCustomerRole").value = review.customerRole || "";
    qs("reviewCity").value = review.city || "";
    qs("reviewAvatarUrl").value = review.avatarUrl || "";
    qs("reviewRating").value = review.rating || 5;
    qs("reviewText").value = review.reviewText || "";
    qs("reviewDisplayOrder").value = review.displayOrder || 1;
    qs("reviewIsVerified").checked = !!review.isVerified;
    qs("reviewIsActive").checked = !!review.isActive;

    qs("btnSaveReview").textContent = "Modifier";
    qs("btnCancelReviewEdit").style.display = "";
    window.scrollTo({ top: 0, behavior: "smooth" });
}



async function deleteReview(id) {

    if (!can(PERMS.reviewsDelete)) return deny(PERMS.reviewsDelete);
    const ok = confirm("Voulez-vous vraiment supprimer cet avis ?");
    if (!ok) return;

    try {
        const r = await fetch(`${API}/reviews/${id}`, {
            method: "DELETE"
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Suppression impossible.");
        }

        if (editingReviewId === id) {
            resetReviewForm();
        }

        await loadReviews();
        alert("Avis supprimé.");
    } catch (e) {
        console.error(e);
        alert("Erreur suppression : " + (e.message || e));
    }
}

async function loadReviews() {
    try {
        const r = await fetch(`${API}/reviews`, { cache: "no-store" });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Impossible de charger les avis.");
        }

        const data = await r.json();

        if (!Array.isArray(data) || !data.length) {
            qs("reviewList").innerHTML = `<div class="box" style="margin-bottom:8px">Aucun avis pour le moment.</div>`;
            return;
        }

        qs("reviewList").innerHTML = data.map(x => `
            <div class="box" style="margin-bottom:10px;padding:14px;border:1px solid rgba(255,255,255,.08);border-radius:12px;">
                <div style="display:flex;justify-content:space-between;gap:14px;align-items:flex-start;flex-wrap:wrap;">
                    <div style="flex:1;min-width:260px;">
                        <div style="font-weight:900;font-size:16px;display:flex;gap:8px;align-items:center;flex-wrap:wrap;">
                            <span>#${x.id}</span>
                            <span>${escapeHtml(x.customerName || "")}</span>
                            <span>⭐${Number(x.rating || 0)}</span>
                        </div>

                        <div style="margin-top:8px;line-height:1.5;">
                            ${escapeHtml(x.reviewText || "")}
                        </div>

                        <div style="font-size:12px;color:#9ca3af;margin-top:8px;line-height:1.5;">
                            ${escapeHtml(x.customerRole || "")}
                            ${x.city ? " · " + escapeHtml(x.city) : ""}
                            ${x.isVerified ? " · Vérifié" : " · Non vérifié"}
                            ${x.isActive ? " · Actif" : " · Inactif"}
                            ${x.avatarUrl ? " · Avatar OK" : " · Sans avatar"}
                        </div>
                    </div>

<div style="display:flex;gap:8px;flex-wrap:wrap;">
    ${can(PERMS.reviewsEdit) ? `<button class="btn btnGhost" type="button" onclick="editReview(${x.id})">Modifier</button>` : ""}
    ${can(PERMS.reviewsDelete) ? `<button class="btn" type="button" onclick="deleteReview(${x.id})">Supprimer</button>` : ""}
</div>
                </div>
            </div>
        `).join("");

        window.editReview = function (id) {
            const review = data.find(x => x.id === id);
            if (review) fillReviewForm(review);
        };

        window.deleteReview = deleteReview;
    } catch (e) {
        console.error(e);
        qs("reviewList").innerHTML = `
            <div class="box" style="margin-bottom:8px">
                Erreur chargement avis : ${escapeHtml(String(e.message || e))}
            </div>
        `;
    }
}

/* =========================================================
   SECTIONS
========================================================= */


function resetSectionForm() {
    editingSectionId = null;

    qs("sectionKey").value = "";
    qs("sectionTitle").value = "";
    qs("sectionSubTitle").value = "";
    qs("sectionDescription").value = "";
    qs("sectionDisplayOrder").value = "1";
    qs("sectionIsActive").checked = true;

    const layout = qs("sectionLayoutType");
    if (layout) layout.value = "services-grid";

    qs("btnSaveSection").textContent = "Enregistrer";
    qs("btnCancelSectionEdit").style.display = "none";
}

function fillSectionForm(section) {
    editingSectionId = section.id;

    qs("sectionKey").value = section.sectionKey || "";
    qs("sectionTitle").value = section.title || "";
    qs("sectionSubTitle").value = section.subTitle || "";
    qs("sectionDescription").value = section.description || "";
    qs("sectionDisplayOrder").value = section.displayOrder || 1;
    qs("sectionIsActive").checked = !!section.isActive;

    const layout = qs("sectionLayoutType");
    if (layout) layout.value = section.layoutType || "services-grid";

    qs("btnSaveSection").textContent = "Modifier";
    qs("btnCancelSectionEdit").style.display = "";
    window.scrollTo({ top: 0, behavior: "smooth" });

}

async function deleteSection(id) {

    if (!can(PERMS.sectionsDelete)) return deny(PERMS.sectionsDelete);
    const ok = confirm("Supprimer cette section ?");
    if (!ok) return;

    try {
        const r = await fetch(`${API}/sections/${id}`, {
            method: "DELETE"
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Suppression impossible.");
        }

        if (editingSectionId === id) {
            resetSectionForm();
        }

        await loadSections();
        await loadSectionOptions();
        await loadSectionItems();
        alert("Section supprimée.");
    } catch (e) {
        console.error(e);
        alert("Erreur suppression : " + (e.message || e));
    }
}

async function loadSections() {
    try {
        const r = await fetch(`${API}/sections`, { cache: "no-store" });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Impossible de charger les sections.");
        }

        const data = await r.json();

        await loadSectionOptions();

        qs("sectionList").innerHTML = (Array.isArray(data) && data.length)
            ? data.map(x => `
                <div class="box" style="margin-bottom:10px;padding:14px;border:1px solid rgba(255,255,255,.08);border-radius:12px;">
                    <div style="display:flex;justify-content:space-between;gap:14px;align-items:flex-start;flex-wrap:wrap;">
                        <div style="flex:1;min-width:260px;">
                            <div style="font-weight:900;font-size:16px;">
                                ${escapeHtml(x.title || "")}
                            </div>

                            <div style="font-size:12px;color:#9ca3af;margin-top:6px;line-height:1.5;">
                                Key : ${escapeHtml(x.sectionKey || "")}
                                ${x.subTitle ? " · " + escapeHtml(x.subTitle) : ""}
                                ${x.layoutType ? " · Layout : " + escapeHtml(x.layoutType) : ""}
                                ${typeof x.displayOrder !== "undefined" ? " · Ordre : " + x.displayOrder : ""}
                                ${x.isActive ? " · Active" : " · Inactive"}
                            </div>

                            ${x.description ? `
                                <div style="margin-top:8px;line-height:1.5;">
                                    ${escapeHtml(x.description)}
                                </div>
                            ` : ""}
                        </div>

<div style="display:flex;gap:8px;flex-wrap:wrap;">
    ${can(PERMS.sectionsEdit) ? `<button class="btn btnGhost" type="button" onclick="editSection(${x.id})">Modifier</button>` : ""}
    ${can(PERMS.sectionsDelete) ? `<button class="btn" type="button" onclick="deleteSection(${x.id})">Supprimer</button>` : ""}
</div>
                    </div>
                </div>
            `).join("")
            : `<div class="box">Aucune section.</div>`;

        window.editSection = function (id) {
            const section = data.find(x => x.id === id);
            if (section) fillSectionForm(section);
        };

        window.deleteSection = deleteSection;

    } catch (e) {
        console.error("Erreur loadSections:", e);
        qs("sectionList").innerHTML = `<div class="box">Erreur chargement sections.</div>`;
    }
}

async function getSectionById(sectionId) {
    try {
        const r = await fetch(`${API}/sections`, { cache: "no-store" });
        const sections = await r.json();
        return Array.isArray(sections) ? sections.find(x => Number(x.id) === Number(sectionId)) : null;
    } catch {
        return null;
    }
}

/* =========================================================
   SECTION ITEMS
========================================================= */
let editingSectionItemId = null;

async function loadSectionOptions() {
    try {
        const r = await fetch(`${API}/sections`, { cache: "no-store" });
        const data = await r.json();

        const select = qs("itemSectionId");
        if (!select) return;

        select.innerHTML = `<option value="">Choisir une section</option>` + data.map(x => `
            <option value="${x.id}">
                ${escapeHtml(x.title)} (${escapeHtml(x.sectionKey)})${x.layoutType ? " - " + escapeHtml(x.layoutType) : ""}
            </option>
        `).join("");
    } catch (e) {
        console.error("Erreur loadSectionOptions:", e);
    }
}


function getSectionItemPayload() {
    return {
        homeSectionId: Number(qs("itemSectionId").value || 0),
        itemType: "image-card",
        title: qs("itemTitle").value.trim(),
        subTitle: qs("itemSubTitle").value.trim(),
        description: qs("itemDescription").value.trim(),
        imageUrl: qs("itemImageUrl").value.trim(),
        buttonText: qs("itemButtonText").value.trim(),
        buttonLink: qs("itemButtonLink").value.trim(),
        badgeText: qs("itemBadgeText").value.trim(),
        priceText: qs("itemPriceText").value.trim(),
        metaText: qs("itemMetaText").value.trim(),
        displayOrder: Number(qs("itemDisplayOrder").value || 1),
        isActive: qs("itemIsActive").checked
    };
}

function resetSectionItemForm() {
    editingSectionItemId = null;

    qs("itemSectionId").value = "";
    qs("itemTitle").value = "";
    qs("itemSubTitle").value = "";
    qs("itemDescription").value = "";
    qs("itemImageUrl").value = "";
    qs("itemButtonText").value = "";
    qs("itemButtonLink").value = "";
    qs("itemBadgeText").value = "";
    qs("itemPriceText").value = "";
    qs("itemMetaText").value = "";
    qs("itemDisplayOrder").value = "1";
    qs("itemIsActive").checked = true;

    const fileInput = qs("itemImageFile");
    if (fileInput) fileInput.value = "";

    qs("btnSaveSectionItem").textContent = "Enregistrer";
    qs("btnCancelSectionItemEdit").style.display = "none";
}

function fillSectionItemForm(item) {
    editingSectionItemId = item.id;

    qs("itemSectionId").value = item.homeSectionId || "";
    qs("itemTitle").value = item.title || "";
    qs("itemSubTitle").value = item.subTitle || "";
    qs("itemDescription").value = item.description || "";
    qs("itemImageUrl").value = item.imageUrl || "";
    qs("itemButtonText").value = item.buttonText || "";
    qs("itemButtonLink").value = item.buttonLink || "";
    qs("itemBadgeText").value = item.badgeText || "";
    qs("itemPriceText").value = item.priceText || "";
    qs("itemMetaText").value = item.metaText || "";
    qs("itemDisplayOrder").value = item.displayOrder || 1;
    qs("itemIsActive").checked = !!item.isActive;

    qs("btnSaveSectionItem").textContent = "Modifier";
    qs("btnCancelSectionItemEdit").style.display = "";
    window.scrollTo({ top: 0, behavior: "smooth" });
}





async function deleteSectionItem(id) {

    if (!can(PERMS.itemsDelete)) return deny(PERMS.itemsDelete);
    const ok = confirm("Supprimer cet item ?");
    if (!ok) return;

    try {
        const r = await fetch(`${API}/section-items/${id}`, {
            method: "DELETE"
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Suppression impossible.");
        }

        if (editingSectionItemId === id) {
            resetSectionItemForm();
        }

        await loadSectionItems();
        alert("Item supprimé.");
    } catch (e) {
        console.error(e);
        alert("Erreur suppression : " + (e.message || e));
    }
}

async function loadSectionItems() {
    try {
        const [itemsRes, sectionsRes] = await Promise.all([
            fetch(`${API}/section-items`, { cache: "no-store" }),
            fetch(`${API}/sections`, { cache: "no-store" })
        ]);

        if (!itemsRes.ok) {
            const msg = await itemsRes.text();
            throw new Error(msg || "Impossible de charger les items.");
        }

        if (!sectionsRes.ok) {
            const msg = await sectionsRes.text();
            throw new Error(msg || "Impossible de charger les sections.");
        }

        const items = await itemsRes.json();
        const sections = await sectionsRes.json();

        const sectionMap = new Map(sections.map(x => [x.id, x]));

        if (!Array.isArray(items) || !items.length) {
            qs("sectionItemList").innerHTML = `
                <div class="box" style="margin-bottom:8px">Aucun item pour le moment.</div>
            `;
            return;
        }

        qs("sectionItemList").innerHTML = items.map(x => {
            const section = sectionMap.get(x.homeSectionId);
            const sectionTitle = section?.title || "Inconnue";
            const imageSrc = x.imageUrl
                ? (x.imageUrl.startsWith("http") ? x.imageUrl : x.imageUrl)
                : "";

            return `
                <div class="box" style="margin-bottom:10px;padding:14px;border:1px solid rgba(255,255,255,.08);border-radius:14px;">
                    <div style="display:flex;justify-content:space-between;gap:16px;flex-wrap:wrap;align-items:flex-start;">

                        <div style="display:flex;gap:14px;flex:1;min-width:320px;align-items:flex-start;">
                            ${imageSrc ? `
                                <img 
                                    src="${escapeHtml(imageSrc)}" 
                                    alt="${escapeHtml(x.title || "")}"
                                    style="width:88px;height:88px;object-fit:cover;border-radius:12px;border:1px solid rgba(255,255,255,.08);background:#fff;flex:0 0 88px;">
                            ` : `
                                <div style="width:88px;height:88px;border-radius:12px;border:1px dashed rgba(255,255,255,.12);display:flex;align-items:center;justify-content:center;color:#9ca3af;flex:0 0 88px;">
                                    Aucun visuel
                                </div>
                            `}

                            <div style="flex:1;min-width:220px;">
                                <div style="font-weight:900;font-size:16px;">
                                    ${escapeHtml(x.title || "")}
                                </div>

                                <div style="font-size:12px;color:#9ca3af;margin-top:6px;line-height:1.5;">
                                    Section : ${escapeHtml(sectionTitle)}
                                    ${x.subTitle ? " · " + escapeHtml(x.subTitle) : ""}
                                    ${typeof x.displayOrder !== "undefined" ? " · Ordre : " + x.displayOrder : ""}
                                    ${x.isActive ? " · Actif" : " · Inactif"}
                                </div>

                                ${x.description ? `
                                    <div style="margin-top:8px;line-height:1.5;">
                                        ${escapeHtml(x.description)}
                                    </div>
                                ` : ""}

                                <div style="font-size:12px;color:#9ca3af;margin-top:8px;line-height:1.5;">
                                    ${x.buttonText ? `Bouton : ${escapeHtml(x.buttonText)}` : ""}
                                    ${x.buttonLink ? ` · ${escapeHtml(x.buttonLink)}` : ""}
                                </div>

                                <div style="font-size:12px;color:#9ca3af;margin-top:6px;line-height:1.5;">
                                    ${x.badgeText ? `Badge : ${escapeHtml(x.badgeText)}` : ""}
                                    ${x.priceText ? ` · Prix : ${escapeHtml(x.priceText)}` : ""}
                                    ${x.metaText ? ` · Meta : ${escapeHtml(x.metaText)}` : ""}
                                </div>
                            </div>
                        </div>

<div style="display:flex;gap:8px;flex-wrap:wrap;">
    ${can(PERMS.itemsEdit) ? `<button class="btn btnGhost" type="button" onclick="editSectionItem(${x.id})">Modifier</button>` : ""}
    ${can(PERMS.itemsDelete) ? `<button class="btn" type="button" onclick="deleteSectionItem(${x.id})">Supprimer</button>` : ""}
</div>
                    </div>
                </div>
            `;
        }).join("");

        window.editSectionItem = function (id) {
            const item = items.find(x => x.id === id);
            if (item) {
                show("sectionItemsPanel");
                fillSectionItemForm(item);
            }
        };

        window.deleteSectionItem = deleteSectionItem;

    } catch (e) {
        console.error(e);
        qs("sectionItemList").innerHTML = `
            <div class="box" style="margin-bottom:8px">
                Erreur chargement items : ${escapeHtml(String(e.message || e))}
            </div>
        `;
    }
}



function setPromoLinkValueFields(type) {
    const categoryEl = qs("promoCategoryValue");
    const searchEl = qs("promoSearchValue");

    if (categoryEl) {
        categoryEl.style.display =
            (type === "category" || type === "promo-category") ? "" : "none";
    }

    if (searchEl) {
        searchEl.style.display =
            (type === "search" || type === "promo-search") ? "" : "none";
    }
}

function buildPromoPrimaryLink() {
    const typeEl = qs("promoLinkType");
    const categoryEl = qs("promoCategoryValue");
    const searchEl = qs("promoSearchValue");
    const btn1Url = qs("promoBtn1Url");

    if (!typeEl || !btn1Url) return;

    const type = typeEl.value;
    const category = (categoryEl?.value || "").trim().toLowerCase();
    const search = (searchEl?.value || "").trim();

    if (type === "promo") {
        btn1Url.value = "/products.html?promo=1";
        return;
    }

    if (type === "category") {
        btn1Url.value = category
            ? `/products.html?category=${encodeURIComponent(category)}`
            : "";
        return;
    }

    if (type === "search") {
        btn1Url.value = search
            ? `/products.html?q=${encodeURIComponent(search)}`
            : "";
        return;
    }

    if (type === "promo-search") {
        btn1Url.value = search
            ? `/products.html?promo=1&q=${encodeURIComponent(search)}`
            : "/products.html?promo=1";
        return;
    }

    if (type === "promo-category") {
        btn1Url.value = category
            ? `/products.html?promo=1&category=${encodeURIComponent(category)}`
            : "/products.html?promo=1";
        return;
    }
}


function updatePromoLinkTypeUI() {
    const typeEl = qs("promoLinkType");
    const btn1Url = qs("promoBtn1Url");

    if (!typeEl) return;

    setPromoLinkValueFields(typeEl.value);

    if (btn1Url) {
        btn1Url.readOnly = typeEl.value !== "custom";
    }

    if (typeEl.value !== "custom") {
        buildPromoPrimaryLink();
    }
}
function bindPromoLinkBuilder() {
    const typeEl = qs("promoLinkType");
    const categoryEl = qs("promoCategoryValue");
    const searchEl = qs("promoSearchValue");

    if (!typeEl) return;

    typeEl.addEventListener("change", () => {
        updatePromoLinkTypeUI();
    });

    if (categoryEl) {
        categoryEl.addEventListener("input", () => {
            const currentType = typeEl.value;
            if (currentType === "category" || currentType === "promo-category") {
                buildPromoPrimaryLink();
            }
        });
    }

    if (searchEl) {
        searchEl.addEventListener("input", () => {
            const currentType = typeEl.value;
            if (currentType === "search" || currentType === "promo-search") {
                buildPromoPrimaryLink();
            }
        });
    }
}

/* =========================================================
   HOME EVENTS / FÊTES & ÉVÉNEMENTS
========================================================= */
let editingEventId = null;
let eventsCache = [];

function getEventPayload() {
    return {
        title: qs("eventTitle").value.trim(),
        subtitle: qs("eventSubtitle").value.trim(),
        badgeText: qs("eventBadgeText").value.trim(),

        desktopImageUrl: qs("eventDesktopImageUrl").value.trim(),
        mobileImageUrl: qs("eventMobileImageUrl").value.trim(),

        buttonText: qs("eventButtonText").value.trim(),
        buttonLink: qs("eventButtonLink").value.trim(),

        targetType: qs("eventTargetType").value,
        categoryId: qs("eventCategoryId").value ? Number(qs("eventCategoryId").value) : null,

        backgroundColor: qs("eventBackgroundColor").value.trim(),
        textColor: qs("eventTextColor").value.trim(),

        displayOrder: Number(qs("eventDisplayOrder").value || 1),
        isActive: qs("eventIsActive").checked,
        isFeatured: qs("eventIsFeatured").checked,

        startDate: qs("eventStartDate").value || null,
        endDate: qs("eventEndDate").value || null,

        isSeasonal: qs("eventIsSeasonal").checked,
        seasonKey: qs("eventSeasonKey").value || null,
        autoScheduleType: qs("eventAutoScheduleType").value || null,
        monthStart: qs("eventMonthStart").value ? Number(qs("eventMonthStart").value) : null,
        dayStart: qs("eventDayStart").value ? Number(qs("eventDayStart").value) : null,
        monthEnd: qs("eventMonthEnd").value ? Number(qs("eventMonthEnd").value) : null,
        dayEnd: qs("eventDayEnd").value ? Number(qs("eventDayEnd").value) : null,
        priority: Number(qs("eventPriority").value || 1),
        autoActivate: qs("eventAutoActivate").checked
    };
}

function getEventPreviewUrl(itemOrUrl) {
    if (!itemOrUrl) return "";

    if (typeof itemOrUrl === "string") {
        return itemOrUrl.trim();
    }

    return (
        String(itemOrUrl.desktopImageUrl || "").trim() ||
        String(itemOrUrl.mobileImageUrl || "").trim()
    );
}
function updateEventSeasonalUI() {
    const isSeasonal = !!qs("eventIsSeasonal")?.checked;
    const autoActivate = !!qs("eventAutoActivate")?.checked;
    const scheduleType = qs("eventAutoScheduleType")?.value || "";

    const seasonalFields = qs("eventSeasonalFields");
    const fixedDateFields = qs("eventFixedDateFields");
    const publicationBlock = qs("eventPublicationBlock");

    const startDate = qs("eventStartDate");
    const endDate = qs("eventEndDate");

    if (seasonalFields) {
        seasonalFields.style.display = isSeasonal ? "" : "none";
    }

    if (fixedDateFields) {
        fixedDateFields.style.display =
            (isSeasonal && scheduleType === "yearly-fixed") ? "" : "none";
    }

    const hidePublication =
        isSeasonal &&
        autoActivate &&
        scheduleType === "yearly-fixed";

    if (publicationBlock) {
        publicationBlock.style.display = hidePublication ? "none" : "";
    }

    if (startDate) {
        startDate.disabled = hidePublication;
        if (hidePublication) startDate.value = "";
    }

    if (endDate) {
        endDate.disabled = hidePublication;
        if (hidePublication) endDate.value = "";
    }
}
function setEventPreview(itemOrUrl) {
    const formPreview = qs("eventFormPreviewImage");
    const sidePreview = qs("eventSidePreviewImage");
    const sideBox = qs("eventSidePreviewBox");

    const cleanUrl = getEventPreviewUrl(itemOrUrl);

    if (!cleanUrl) {
        if (formPreview) {
            formPreview.removeAttribute("src");
            formPreview.style.display = "none";
        }

        if (sidePreview) {
            sidePreview.removeAttribute("src");
            sidePreview.style.display = "none";
        }

        if (sideBox) {
            sideBox.style.display = "none";
        }

        return;
    }

    const finalUrl = cleanUrl + (cleanUrl.includes("?") ? "&" : "?") + "v=" + Date.now();

    if (formPreview) {
        formPreview.src = finalUrl;
        formPreview.style.display = "block";
    }

    if (sidePreview) {
        sidePreview.src = finalUrl;
        sidePreview.style.display = "block";
    }

    if (sideBox) {
        sideBox.style.display = "block";
    }
}

function resetEventForm() {
    editingEventId = null;

    qs("eventTitle").value = "";
    qs("eventSubtitle").value = "";
    qs("eventBadgeText").value = "";

    qs("eventDesktopImageUrl").value = "";
    qs("eventMobileImageUrl").value = "";

    qs("eventButtonText").value = "";
    qs("eventButtonLink").value = "";

    qs("eventTargetType").value = "url";
    qs("eventCategoryId").value = "";

    qs("eventBackgroundColor").value = "";
    qs("eventTextColor").value = "";
    qs("eventDisplayOrder").value = "1";

    qs("eventStartDate").value = "";
    qs("eventEndDate").value = "";

    qs("eventIsActive").checked = true;
    qs("eventIsFeatured").checked = false;

    qs("eventIsSeasonal").checked = false;
    qs("eventAutoActivate").checked = true;
    qs("eventSeasonKey").value = "";
    qs("eventAutoScheduleType").value = "";
    qs("eventMonthStart").value = "";
    qs("eventDayStart").value = "";
    qs("eventMonthEnd").value = "";
    qs("eventDayEnd").value = "";
    qs("eventPriority").value = "1";

    const desktopFile = qs("eventDesktopImageFile");
    if (desktopFile) desktopFile.value = "";

    updateEventSeasonalUI();
    setEventPreview("");

    qs("btnSaveEvent").textContent = "Enregistrer";
    qs("btnCancelEventEdit").style.display = "none";
}

function fillEventForm(item) {
    if (!item) return;

    editingEventId = Number(item.id);

    qs("eventTitle").value = item.title || "";
    qs("eventSubtitle").value = item.subtitle || "";
    qs("eventBadgeText").value = item.badgeText || "";

    qs("eventDesktopImageUrl").value = item.desktopImageUrl || "";
    qs("eventMobileImageUrl").value = item.mobileImageUrl || "";

    qs("eventButtonText").value = item.buttonText || "";
    qs("eventButtonLink").value = item.buttonLink || "";

    qs("eventTargetType").value = item.targetType || "url";
    qs("eventCategoryId").value = item.categoryId != null ? String(item.categoryId) : "";

    qs("eventBackgroundColor").value = item.backgroundColor || "";
    qs("eventTextColor").value = item.textColor || "";
    qs("eventDisplayOrder").value = item.displayOrder ?? 1;

    qs("eventStartDate").value = toDateTimeLocal(item.startDate);
    qs("eventEndDate").value = toDateTimeLocal(item.endDate);

    qs("eventIsActive").checked = !!item.isActive;
    qs("eventIsFeatured").checked = !!item.isFeatured;

    qs("eventIsSeasonal").checked = !!item.isSeasonal;
    qs("eventAutoActivate").checked = item.autoActivate !== false;
    qs("eventSeasonKey").value = item.seasonKey || "";
    qs("eventAutoScheduleType").value = item.autoScheduleType || "";
    qs("eventMonthStart").value = item.monthStart ?? "";
    qs("eventDayStart").value = item.dayStart ?? "";
    qs("eventMonthEnd").value = item.monthEnd ?? "";
    qs("eventDayEnd").value = item.dayEnd ?? "";
    qs("eventPriority").value = item.priority ?? 1;

    const desktopFile = qs("eventDesktopImageFile");
    if (desktopFile) desktopFile.value = "";

    updateEventSeasonalUI();
    setEventPreview(item);

    qs("btnSaveEvent").textContent = "Modifier";
    qs("btnCancelEventEdit").style.display = "";

    show("eventsPanel");
    window.scrollTo({ top: 0, behavior: "smooth" });
}

function renderEventCard(x) {
    const imageUrl = getEventPreviewUrl(x);
    const imageHtml = imageUrl
        ? `
            <img 
                src="${escapeHtml(imageUrl)}?v=${Date.now()}" 
                alt="${escapeHtml(x.title || "")}"
                style="width:140px;height:100px;object-fit:cover;border-radius:12px;border:1px solid rgba(255,255,255,.08);background:#fff;flex:0 0 140px;">
        `
        : `
            <div style="width:140px;height:100px;border-radius:12px;border:1px dashed rgba(255,255,255,.12);display:flex;align-items:center;justify-content:center;color:#9ca3af;flex:0 0 140px;">
                Aucun visuel
            </div>
        `;

    return `
        <div class="box" style="margin-bottom:10px;padding:14px;border:1px solid rgba(255,255,255,.08);border-radius:12px;">
            <div style="display:flex;justify-content:space-between;gap:16px;align-items:flex-start;flex-wrap:wrap;">

                <div style="display:flex;gap:16px;flex:1;min-width:320px;align-items:flex-start;">
                    ${imageHtml}

                    <div style="flex:1;min-width:220px;">
                        <div style="font-weight:900;font-size:16px;display:flex;gap:8px;align-items:center;flex-wrap:wrap;">
                            <span>#${x.id}</span>
                            <span>${escapeHtml(x.title || "")}</span>
                            ${x.isFeatured ? `<span style="padding:3px 8px;border-radius:999px;background:rgba(34,197,94,.12);border:1px solid rgba(34,197,94,.28);font-size:11px;">Featured</span>` : ""}
                            ${x.isActive
            ? `<span style="padding:3px 8px;border-radius:999px;background:rgba(59,130,246,.12);border:1px solid rgba(59,130,246,.28);font-size:11px;">Active</span>`
            : `<span style="padding:3px 8px;border-radius:999px;background:rgba(239,68,68,.12);border:1px solid rgba(239,68,68,.28);font-size:11px;">Inactive</span>`}
                        </div>

                        <div style="font-size:12px;color:#9ca3af;margin-top:8px;line-height:1.6;">
                            ${x.badgeText ? `Badge : ${escapeHtml(x.badgeText)} · ` : ""}
                            Target : ${escapeHtml(x.targetType || "url")}
                            ${x.categoryName ? ` · Catégorie : ${escapeHtml(x.categoryName)}` : ""}
                            ${typeof x.displayOrder !== "undefined" ? ` · Ordre : ${x.displayOrder}` : ""}
                        </div>

                        ${x.subtitle ? `
                            <div style="margin-top:8px;line-height:1.5;">
                                ${escapeHtml(x.subtitle)}
                            </div>
                        ` : ""}

                        <div style="font-size:12px;color:#9ca3af;margin-top:8px;line-height:1.6;">
                            ${x.buttonText ? `Bouton : ${escapeHtml(x.buttonText)}` : ""}
                            ${x.buttonLink ? ` · Lien : ${escapeHtml(x.buttonLink)}` : ""}
                        </div>

                        <div style="font-size:12px;color:#9ca3af;margin-top:6px;line-height:1.6;">
                            ${x.startDate ? `Début : ${escapeHtml(new Date(x.startDate).toLocaleString("fr-FR"))}` : "Début : non défini"}
                            ${x.endDate ? ` · Fin : ${escapeHtml(new Date(x.endDate).toLocaleString("fr-FR"))}` : " · Fin : non définie"}
                        </div>
                    </div>
                </div>

<div style="display:flex;gap:8px;flex-wrap:wrap;">
    ${can(PERMS.eventsEdit) ? `<button class="btn btnGhost" type="button" onclick="editEvent(${x.id})">Modifier</button>` : ""}
    ${can(PERMS.eventsDelete) ? `<button class="btn" type="button" onclick="deleteEvent(${x.id})">Supprimer</button>` : ""}
</div>
            </div>
        </div>
    `;
}

async function loadEvents() {
    try {
        const r = await fetch(EVENTS_API, { cache: "no-store" });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Impossible de charger les campagnes.");
        }

        const data = await r.json();
        eventsCache = Array.isArray(data) ? data : [];

        if (!eventsCache.length) {
            qs("eventList").innerHTML = `<div class="box">Aucune campagne pour le moment.</div>`;
            return;
        }

        qs("eventList").innerHTML = eventsCache.map(renderEventCard).join("");
    } catch (e) {
        console.error(e);
        qs("eventList").innerHTML = `
            <div class="box">
                Erreur chargement campagnes : ${escapeHtml(String(e.message || e))}
            </div>
        `;
    }
}

function editEvent(id) {
    const item = eventsCache.find(x => Number(x.id) === Number(id));

    if (!item) {
        alert("Campagne introuvable.");
        return;
    }

    fillEventForm(item);
}

async function deleteEvent(id) {

    if (!can(PERMS.eventsDelete)) return deny(PERMS.eventsDelete);
    const ok = confirm("Supprimer cette campagne ?");
    if (!ok) return;

    try {
        const r = await fetch(`${EVENTS_API}/${id}`, {
            method: "DELETE"
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Suppression impossible.");
        }

        if (editingEventId === Number(id)) {
            resetEventForm();
        }

        await loadEvents();
        alert("Campagne supprimée.");
    } catch (e) {
        console.error(e);
        alert("Erreur suppression : " + (e.message || e));
    }
}

async function uploadPromoBg() {

    if (!can(PERMS.promoEdit)) return deny(PERMS.promoEdit);
    try {
        const file = qs("promoBackgroundFile")?.files?.[0];
        if (!file) {
            alert("Choisis une image de fond.");
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        const r = await fetch(`${API}/upload-promo-background`, {
            method: "POST",
            body: formData
        });

        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Erreur upload image fond.");
        }

        const data = await r.json();
        qs("promoBackgroundImageUrl").value = data.url || "";

        alert("Image de fond uploadée avec succès.");
    } catch (e) {
        console.error(e);
        alert("Erreur upload image fond : " + (e.message || e));
    }
}

window.editEvent = editEvent;
window.deleteEvent = deleteEvent;
window.deleteReview = deleteReview;
window.deleteSection = deleteSection;
window.deleteSectionItem = deleteSectionItem;

async function loadEventCategoryOptions() {
    try {
        const r = await fetch("/api/categories", { cache: "no-store" });
        if (!r.ok) {
            const msg = await r.text();
            throw new Error(msg || "Impossible de charger les catégories.");
        }

        const data = await r.json();

        const items = Array.isArray(data)
            ? data
            : (Array.isArray(data.items) ? data.items : []);

        const select = qs("eventCategoryId");
        if (!select) return;

        const currentValue = select.value || "";

        select.innerHTML =
            `<option value="">Choisir une catégorie (optionnel)</option>` +
            items.map(x => `
                <option value="${x.id}">
                    ${escapeHtml(x.name || x.title || ("Catégorie " + x.id))}
                </option>
            `).join("");

        if (currentValue) {
            select.value = currentValue;
        }
    } catch (e) {
        console.error("Erreur loadEventCategoryOptions:", e);
    }
}
/* =========================================================
   INIT
========================================================= */
document.addEventListener("DOMContentLoaded", async () => {
    on("tabPromo", "click", () => show("promoPanel"));
    on("tabReviews", "click", () => show("reviewsPanel"));
    on("tabSections", "click", () => show("sectionsPanel"));
    on("tabSectionItems", "click", () => show("sectionItemsPanel"));
    on("tabEvents", "click", () => show("eventsPanel"));

    on("btnUploadEventDesktop", "click", uploadEventDesktop);
    on("btnUploadPromoBg", "click", uploadPromoBg);
    on("btnSavePromo", "click", savePromo);

    on("btnCancelSectionEdit", "click", () => {
        resetSectionForm();
        const btn = byId("btnCancelSectionEdit");
        if (btn) btn.style.display = "none";
    });

    on("btnCancelReviewEdit", "click", resetReviewForm);
    on("btnUploadAvatar", "click", uploadAvatar);
    on("btnSaveReview", "click", saveReview);

    on("btnSaveSection", "click", saveSection);

    on("btnUploadSectionItemImage", "click", uploadSectionItemImage);
    on("btnCancelSectionItemEdit", "click", resetSectionItemForm);
    on("btnSaveSectionItem", "click", saveSectionItem);

    on("btnSaveEvent", "click", saveEvent);
    on("btnCancelEventEdit", "click", resetEventForm);

    on("eventDesktopImageUrl", "input", () => {
        const desktopUrl = byId("eventDesktopImageUrl")?.value || "";
        const mobileUrl = byId("eventMobileImageUrl")?.value || "";
        setEventPreview(desktopUrl || mobileUrl);
    });

    on("eventMobileImageUrl", "input", () => {
        const desktopUrl = byId("eventDesktopImageUrl")?.value || "";
        const mobileUrl = byId("eventMobileImageUrl")?.value || "";
        setEventPreview(desktopUrl || mobileUrl);
    });

    on("eventIsSeasonal", "change", updateEventSeasonalUI);
    on("eventAutoScheduleType", "change", updateEventSeasonalUI);
    on("eventAutoActivate", "change", updateEventSeasonalUI);

    show("promoPanel");
    bindPromoLinkBuilder();
    updatePromoLinkTypeUI();
    updateEventSeasonalUI();
    applyHomeCmsActionPermissions();

    await loadPromo();
    await loadReviews();
    await loadSections();
    await loadSectionOptions();
    await loadSectionItems();
    await loadEventCategoryOptions();
    await loadEvents();
});