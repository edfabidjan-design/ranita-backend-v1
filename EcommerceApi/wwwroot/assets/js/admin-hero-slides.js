let allSlides = [];

async function loadSlides() {
    try {
        const slides = await fetchJson("/api/admin/hero-slides");
        allSlides = Array.isArray(slides) ? slides : [];

        const tbody = document.querySelector("#slidesTable tbody");
        tbody.innerHTML = "";

        allSlides.forEach(s => {
            const tr = document.createElement("tr");

            tr.innerHTML = `
    <td>${s.id}</td>
    <td>
        ${s.imageUrl
                    ? `<img src="${s.imageUrl}" alt="" class="slideThumb">`
                    : `<div class="thumbEmpty">Aucune image</div>`}
    </td>
    <td>${s.title || ""}</td>
    <td>${s.rightTitle || ""}</td>
    <td>${s.badgeText || ""}</td>
    <td>${s.primaryButtonText || ""}</td>
    <td>${s.displayOrder ?? 0}</td>
    <td>
        <span class="statusBadge ${s.isActive ? "statusActive" : "statusInactive"}">
            ${s.isActive ? "Actif" : "Inactif"}
        </span>
    </td>
    <td>
        <div class="adminActions">
            <button class="btn btnEdit btnSm" onclick="editSlide(${s.id})">✏️ Modifier</button>
            <button class="btn btnDelete btnSm" onclick="deleteSlide(${s.id})">🗑 Supprimer</button>
            <button class="btn ${s.isActive ? "btnDisable" : "btnToggle"} btnSm" onclick="toggleSlide(${s.id})">
                ${s.isActive ? "⛔ Désactiver" : "✅ Activer"}
            </button>
        </div>
    </td>
`;

            tbody.appendChild(tr);
        });
    } catch (e) {
        console.error(e);
        alert("Impossible de charger les slides.");
    }
}

async function deleteSlide(id) {
    if (!confirm("Supprimer ce slide ?")) return;

    try {
        await window.fetchJson("/api/admin/hero-slides/" + id, {
            method: "DELETE"
        });

        await loadSlides();
    } catch (e) {
        console.error(e);
        alert("Impossible de supprimer le slide.");
    }
}

async function toggleSlide(id) {
    try {
        await fetchJson(`/api/admin/hero-slides/${id}/toggle`, {
            method: "PUT"
        });

        await loadSlides();
    } catch (e) {
        console.error(e);
        alert("Impossible de changer le statut du slide.");
    }
}

function editSlide(id) {
    const s = allSlides.find(x => x.id === id);
    if (!s) return;

    document.getElementById("slideId").value = s.id || "";
    document.getElementById("title").value = s.title || "";
    document.getElementById("subtitle").value = s.subtitle || "";
    document.getElementById("badgeText").value = s.badgeText || "";
    document.getElementById("smallTag").value = s.smallTag || "";
    document.getElementById("primaryButtonText").value = s.primaryButtonText || "";
    document.getElementById("primaryButtonUrl").value = s.primaryButtonUrl || "";
    document.getElementById("secondaryButtonText").value = s.secondaryButtonText || "";
    document.getElementById("secondaryButtonUrl").value = s.secondaryButtonUrl || "";
    document.getElementById("theme").value = s.theme || "brand";
    document.getElementById("accentColor").value = s.accentColor || "#22c55e";
    document.getElementById("highlightText").value = s.highlightText || "";
    document.getElementById("displayOrder").value = s.displayOrder ?? 0;
    document.getElementById("isActive").checked = !!s.isActive;
    document.getElementById("imageFile").value = "";

    document.getElementById("rightBadgeText").value = s.rightBadgeText || "";
    document.getElementById("rightTitle").value = s.rightTitle || "";
    document.getElementById("rightSubtitle").value = s.rightSubtitle || "";
    document.getElementById("rightButtonText").value = s.rightButtonText || "";
    document.getElementById("rightButtonUrl").value = s.rightButtonUrl || "";

    document.getElementById("statsTitle").value = s.statsTitle || "";
    document.getElementById("stat1Value").value = s.stat1Value || "";
    document.getElementById("stat1Label").value = s.stat1Label || "";
    document.getElementById("stat2Value").value = s.stat2Value || "";
    document.getElementById("stat2Label").value = s.stat2Label || "";
    document.getElementById("stat3Value").value = s.stat3Value || "";
    document.getElementById("stat3Label").value = s.stat3Label || "";

    const previewWrap = document.getElementById("imagePreviewWrap");
    const preview = document.getElementById("imagePreview");

    if (s.imageUrl) {
        preview.src = s.imageUrl;
        previewWrap.style.display = "block";
    } else {
        preview.src = "";
        previewWrap.style.display = "none";
    }

    document.getElementById("formTitle").textContent = "Modifier le slide";
    window.scrollTo({ top: 0, behavior: "smooth" });
}

function resetForm() {
    document.getElementById("slideForm").reset();
    document.getElementById("slideId").value = "";
    document.getElementById("displayOrder").value = "0";
    document.getElementById("isActive").checked = true;
    document.getElementById("accentColor").value = "#22c55e";
    document.getElementById("formTitle").textContent = "Nouveau slide";

    document.getElementById("rightBadgeText").value = "";
    document.getElementById("rightTitle").value = "";
    document.getElementById("rightSubtitle").value = "";
    document.getElementById("rightButtonText").value = "";
    document.getElementById("rightButtonUrl").value = "";

    document.getElementById("statsTitle").value = "";
    document.getElementById("stat1Value").value = "";
    document.getElementById("stat1Label").value = "";
    document.getElementById("stat2Value").value = "";
    document.getElementById("stat2Label").value = "";
    document.getElementById("stat3Value").value = "";
    document.getElementById("stat3Label").value = "";


    const previewWrap = document.getElementById("imagePreviewWrap");
    const preview = document.getElementById("imagePreview");
    preview.src = "";
    previewWrap.style.display = "none";
}

document.getElementById("btnNew")?.addEventListener("click", () => {
    resetForm();
});

document.getElementById("imageFile")?.addEventListener("change", function () {
    const file = this.files?.[0];
    const previewWrap = document.getElementById("imagePreviewWrap");
    const preview = document.getElementById("imagePreview");

    if (!file) {
        preview.src = "";
        previewWrap.style.display = "none";
        return;
    }

    preview.src = URL.createObjectURL(file);
    previewWrap.style.display = "block";
});

document.getElementById("slideForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const id = document.getElementById("slideId").value.trim();
    const file = document.getElementById("imageFile").files[0];

    const fd = new FormData();
    fd.append("title", document.getElementById("title").value.trim());
    fd.append("subtitle", document.getElementById("subtitle").value.trim());
    fd.append("badgeText", document.getElementById("badgeText").value.trim());
    fd.append("smallTag", document.getElementById("smallTag").value.trim());
    fd.append("primaryButtonText", document.getElementById("primaryButtonText").value.trim());
    fd.append("primaryButtonUrl", document.getElementById("primaryButtonUrl").value.trim());
    fd.append("secondaryButtonText", document.getElementById("secondaryButtonText").value.trim());
    fd.append("secondaryButtonUrl", document.getElementById("secondaryButtonUrl").value.trim());
    fd.append("theme", document.getElementById("theme").value.trim());
    fd.append("accentColor", document.getElementById("accentColor").value.trim());
    fd.append("highlightText", document.getElementById("highlightText").value.trim());
    fd.append("displayOrder", document.getElementById("displayOrder").value || "0");
    fd.append("isActive", document.getElementById("isActive").checked ? "true" : "false");

    fd.append("rightBadgeText", document.getElementById("rightBadgeText").value.trim());
    fd.append("rightTitle", document.getElementById("rightTitle").value.trim());
    fd.append("rightSubtitle", document.getElementById("rightSubtitle").value.trim());
    fd.append("rightButtonText", document.getElementById("rightButtonText").value.trim());
    fd.append("rightButtonUrl", document.getElementById("rightButtonUrl").value.trim());

    fd.append("statsTitle", document.getElementById("statsTitle").value.trim());
    fd.append("stat1Value", document.getElementById("stat1Value").value.trim());
    fd.append("stat1Label", document.getElementById("stat1Label").value.trim());
    fd.append("stat2Value", document.getElementById("stat2Value").value.trim());
    fd.append("stat2Label", document.getElementById("stat2Label").value.trim());
    fd.append("stat3Value", document.getElementById("stat3Value").value.trim());
    fd.append("stat3Label", document.getElementById("stat3Label").value.trim());

    if (file) {
        fd.append("image", file);
    }

    try {
        const token = localStorage.getItem("ranita_admin_token");

        const r = await fetch(id ? `/api/admin/hero-slides/${id}` : `/api/admin/hero-slides`, {
            method: id ? "PUT" : "POST",
            headers: token ? { "Authorization": `Bearer ${token}` } : {},
            body: fd
        });

        const text = await r.text();
        let json = null;
        try { json = text ? JSON.parse(text) : null; } catch { }

        if (!r.ok) {
            throw new Error(json?.message || `HTTP ${r.status}`);
        }

        resetForm();
        await loadSlides();
        alert("Slide enregistré avec succès.");
    } catch (e) {
        console.error(e);
        alert(e.message || "Impossible d'enregistrer le slide.");
    }
});

document.addEventListener("DOMContentLoaded", loadSlides);
window.editSlide = editSlide;
window.deleteSlide = deleteSlide;
window.toggleSlide = toggleSlide;