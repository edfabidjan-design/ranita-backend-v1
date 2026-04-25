const API = window.API_BASE || location.origin;
const token = () => localStorage.getItem("ranita_client_token") || "";

function esc(s) {
    return String(s ?? "").replace(/[&<>"']/g, c => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        '"': "&quot;",
        "'": "&#39;"
    }[c]));
}

function fmt(n) {
    return Number(n || 0).toLocaleString("fr-FR") + " FCFA";
}

function fmtDate(v) {
    try {
        const d = new Date(v);
        return isNaN(d) ? "" : d.toLocaleString();
    } catch {
        return "";
    }
}

function imgUrl(it) {
    const src = it?.imageUrl || it?.image || it?.mainImageUrl || "";
    if (!src) return "/assets/img/placeholder.jpg";
    if (String(src).startsWith("http")) return src;
    return src.startsWith("/") ? src : "/" + src;
}

function normStatus(status) {
    return String(status || "").toLowerCase().trim();
}

function canCancelOrder(status) {
    return normStatus(status) === "enattente";
}

function canDeleteOrder(status) {
    const s = normStatus(status);
    return s === "annulee" || s === "cancelled";
}

function openModal() {
    document.getElementById("modal").style.display = "flex";
}

function closeModal() {
    document.getElementById("modal").style.display = "none";
}

function openReturnModal() {
    document.getElementById("returnModal").style.display = "flex";
}

function closeReturnModal() {
    document.getElementById("returnModal").style.display = "none";
}

function openReviewModal() {
    document.getElementById("reviewModal").style.display = "flex";
}

function closeReviewModal() {
    document.getElementById("reviewModal").style.display = "none";
}

async function fetchAuth(url, opt = {}) {
    const t = token();

    if (!t) {
        localStorage.removeItem("ranita_client_token");
        location.href = "/client-login.html?reason=login";
        return;
    }

    const res = await fetch(API + url, {
        ...opt,
        headers: {
            ...(opt.headers || {}),
            Authorization: "Bearer " + t
        },
        cache: "no-store"
    });

    if (res.status === 401) {
        localStorage.removeItem("ranita_client_token");
        alert("Votre session a expiré. Veuillez vous reconnecter.");
        location.href = "/client-login.html?reason=expired";
        return;
    }

    const data = await res.json().catch(() => null);
    if (!res.ok || data?.ok === false) {
        throw new Error(data?.message || ("HTTP " + res.status));
    }

    return data;
}

function statusBadge(status) {
    const s = normStatus(status);

    if (s === "enattente") {
        return '<span style="display:inline-flex;align-items:center;padding:4px 10px;border-radius:999px;background:rgba(245,158,11,.14);color:#f59e0b;font-weight:900;font-size:12px;border:1px solid rgba(245,158,11,.24)">En attente</span>';
    }

    if (s === "livree" || s === "delivered") {
        return '<span style="display:inline-flex;align-items:center;padding:4px 10px;border-radius:999px;background:rgba(34,197,94,.14);color:#22c55e;font-weight:900;font-size:12px;border:1px solid rgba(34,197,94,.24)">Livrée</span>';
    }

    if (s === "annulee" || s === "cancelled") {
        return '<span style="display:inline-flex;align-items:center;padding:4px 10px;border-radius:999px;background:rgba(239,68,68,.14);color:#ef4444;font-weight:900;font-size:12px;border:1px solid rgba(239,68,68,.24)">Annulée</span>';
    }

    if (s === "enpreparation") {
        return '<span style="display:inline-flex;align-items:center;padding:4px 10px;border-radius:999px;background:rgba(59,130,246,.14);color:#60a5fa;font-weight:900;font-size:12px;border:1px solid rgba(59,130,246,.24)">En préparation</span>';
    }

    if (s === "enlivraison") {
        return '<span style="display:inline-flex;align-items:center;padding:4px 10px;border-radius:999px;background:rgba(168,85,247,.14);color:#c084fc;font-weight:900;font-size:12px;border:1px solid rgba(168,85,247,.24)">En livraison</span>';
    }

    return '<span style="display:inline-flex;align-items:center;padding:4px 10px;border-radius:999px;background:rgba(148,163,184,.14);color:#cbd5e1;font-weight:900;font-size:12px;border:1px solid rgba(148,163,184,.24)">' + esc(status) + '</span>';
}

async function cancelOrder(id) {
    return await fetchAuth("/api/client/orders/" + id + "/cancel", { method: "POST" });
}

async function deleteOrder(id) {
    return await fetchAuth("/api/client/orders/" + id, { method: "DELETE" });
}

function renderList(items) {
    const box = document.getElementById("list");

    if (!items?.length) {
        box.innerHTML = '<div class="muted">Aucune commande.</div>';
        return;
    }

    box.innerHTML = `
        <div style="display:grid;gap:10px;margin-top:10px">
            ${items.map(o => `
                <button class="btn btnGhost orderCard ${String(o.status || "").toLowerCase()}"
                        style="text-align:left;padding:14px;border-radius:18px;background:linear-gradient(180deg,rgba(255,255,255,.03),rgba(255,255,255,.02))"
                        data-oid="${o.id}">
                    <div style="display:flex;justify-content:space-between;gap:10px;flex-wrap:wrap;align-items:center">
                        <div>
                            <div style="font-weight:950">Commande #${o.id}</div>
                            <div class="muted" style="opacity:.8">${fmtDate(o.createdAt || o.createdAtUtc)}</div>
                            <div style="margin-top:4px">${statusBadge(o.status || "")}</div>
                        </div>
                        <div style="font-weight:950">${fmt(o.total)}</div>
                    </div>
                </button>
            `).join("")}
        </div>
    `;

    box.querySelectorAll("[data-oid]").forEach(btn => {
        btn.addEventListener("click", () => viewOrder(Number(btn.getAttribute("data-oid"))));
    });
}

function renderOrderDetail(d) {
    const order = d?.item || d?.order || d;
    const lines = order?.items || order?.Items || d?.items || [];
    const status = String(order?.status || "").trim();
    const statusNorm = status.toLowerCase();
    const isDelivered = statusNorm === "livree" || statusNorm === "delivered";
    const canCancel = canCancelOrder(status);
    const canDelete = canDeleteOrder(status);

    const delivered = new Date(
        order?.deliveredAt || order?.deliveredAtUtc || order?.createdAt || order?.createdAtUtc
    );
    const in7Days = ((Date.now() - delivered) / (1000 * 60 * 60 * 24)) <= 7;

    const actionButtons =
        (canCancel ? '<button class="btn btnGhost" type="button" id="btnCancelOrder" data-oid="' + order?.id + '">Annuler la commande</button>' : '') +
        (canDelete ? '<button class="btn btnGhost" type="button" id="btnDeleteOrder" data-oid="' + order?.id + '">Supprimer la commande</button>' : '') +
        ((isDelivered && in7Days) ? '<button class="btn btnYellow" type="button" id="btnAskReturn" data-oid="' + order?.id + '">Demander retour</button>' : '');

    const header = `
        <div class="detailHero">
            <div class="detailHeroLeft">
                <div class="detailOrderTitle">Commande #${order?.id ?? ""}</div>
                <div class="detailMeta">📅 ${fmtDate(order?.createdAt || order?.createdAtUtc)}</div>
                <div style="margin-top:8px">${statusBadge(order?.status || "")}</div>
            </div>
            <div class="detailHeroRight">
                <div class="detailTotal">${fmt(order?.total)}</div>
                ${actionButtons}
            </div>
        </div>
    `;

    const grid = `
        <div class="detailItemsGrid">
            ${lines.map(it => {
                const pid = Number(it?.productId || it?.ProductId || 0);
                const title = esc(it?.name || it?.productName || it?.title || "Produit");
                const qty = Number(it?.qty || it?.quantity || 0);
                const price = Number(it?.price || it?.unitPrice || 0);
                const totalLine = Number(it?.lineTotal ?? (qty * price));

                return `
                    <div class="detailItemCard">
                        <img src="${imgUrl(it)}" class="detailItemImage" />
                        <div class="detailItemBody">
                            <div class="detailItemName">${title}</div>
                            <div class="detailItemMeta">
                                Qté: ${qty} • PU: ${fmt(price)}
                                ${it?.size ? " • Taille: " + esc(it.size) : ""}
                                ${it?.color ? " • Couleur: " + esc(it.color) : ""}
                            </div>
                        </div>
                        <div class="detailItemSide">
                            <div class="detailItemPrice">${fmt(totalLine)}</div>
                            ${(isDelivered && pid) ? `
                                <button class="btn btnGhost btnReview"
                                        type="button"
                                        data-pid="${pid}"
                                        data-pname="${esc(title)}"
                                        data-oid="${order?.id}">
                                    ⭐ Noter le vendeur
                                </button>
                            ` : ""}
                        </div>
                    </div>
                `;
            }).join("")}
        </div>
    `;

    return header + '<div class="detailSectionTitle">🛒 Articles</div>' + grid;
}

async function viewOrder(id) {
    openModal();
    const body = document.getElementById("mBody");
    body.innerHTML = '<div class="muted">Chargement…</div>';

    try {
        const data = await fetchAuth("/api/client/orders/" + id);
        body.innerHTML = renderOrderDetail(data);
    } catch (e) {
        body.innerHTML = '<div style="color:#fca5a5">❌ ' + e.message + '</div>';
    }
}

async function load() {
    const data = await fetchAuth("/api/client/orders");
    renderList(data.items || []);
}

let lastOrderDetail = null;
let rvProductId = 0;
let rvRating = 0;

function renderReturnItems(order) {
    const rItems = document.getElementById("rItems");
    const lines = order?.items || order?.Items || [];

    rItems.innerHTML = lines.map(it => {
        const qty = Number(it?.qty || it?.quantity || 0);
        const refunded = Number(it?.refundedQty ?? it?.refundedQuantity ?? 0);
        const remaining = Math.max(0, qty - refunded);
        const oiId = Number(it?.orderItemId || 0);
        const title = esc(it?.name || it?.productName || it?.title || "Produit");

        return `
            <div class="returnCard">
                <img src="${imgUrl(it)}" class="detailItemImage" />
                <div class="detailItemBody">
                    <div class="detailItemName">${title}</div>
                    <div class="detailItemMeta">Acheté: ${qty} • Restant: ${remaining}</div>
                </div>
                <div class="returnQtyBox">
                    <div class="muted" style="opacity:.85;font-weight:800">Qté à retourner</div>
                    <input
                        data-oi="${oiId}"
                        type="number"
                        min="0"
                        max="${remaining}"
                        value="${remaining > 0 ? 1 : 0}"
                        ${remaining <= 0 ? "disabled" : ""}
                        class="returnQtyInput" />
                </div>
            </div>
        `;
    }).join("");
}

async function postReturn(orderId) {
    const rMsg = document.getElementById("rMsg");
    const reason = document.getElementById("rReason").value || "";
    const files = Array.from(document.getElementById("rImages")?.files || []).slice(0, 2);

    const inputs = Array.from(document.querySelectorAll("#rItems input[data-oi]"));
    const selected = inputs
        .map(inp => ({ orderItemId: Number(inp.dataset.oi), quantity: Number(inp.value || 0) }))
        .filter(x => x.orderItemId && x.quantity > 0);

    if (!selected.length) {
        rMsg.textContent = "Choisis au moins 1 produit et une quantité.";
        return;
    }

    rMsg.textContent = "Envoi...";

    const fd = new FormData();
    fd.append("orderId", String(orderId));
    fd.append("reason", reason);
    fd.append("comment", reason);
    fd.append("itemsJson", JSON.stringify(selected));
    files.forEach(f => fd.append("images", f));

    const res = await fetch(API + "/api/returns", {
        method: "POST",
        headers: { Authorization: "Bearer " + token() },
        body: fd
    });

    const data = await res.json().catch(() => null);
    if (!res.ok || data?.ok === false) throw new Error(data?.message || ("HTTP " + res.status));
    return data;
}

async function canReview(productId) {
    const t = token();
    const r = await fetch(API + "/api/products/" + productId + "/can-review", {
        headers: { Authorization: "Bearer " + t },
        cache: "no-store"
    });

    const data = await r.json().catch(() => null);
    if (!r.ok) throw new Error(data?.message || ("HTTP " + r.status));
    return data;
}

async function postReview(productId, body) {
    const t = token();
    const r = await fetch(API + "/api/products/" + productId + "/reviews", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            Authorization: "Bearer " + t
        },
        body: JSON.stringify(body),
        cache: "no-store"
    });

    const data = await r.json().catch(() => null);
    if (!r.ok || data?.ok === false) throw new Error(data?.message || ("HTTP " + r.status));
    return data;
}

document.addEventListener("DOMContentLoaded", async () => {
    if (!window.requireClientAuth?.()) return;

    document.getElementById("mClose").onclick = closeModal;
    document.getElementById("modal").addEventListener("click", e => {
        if (e.target.id === "modal") closeModal();
    });

    document.getElementById("rClose").onclick = closeReturnModal;
    document.getElementById("rCancel").onclick = closeReturnModal;
    document.getElementById("returnModal").addEventListener("click", e => {
        if (e.target.id === "returnModal") closeReturnModal();
    });

    document.getElementById("rvClose").onclick = closeReviewModal;
    document.getElementById("rvCancel").onclick = closeReviewModal;
    document.getElementById("reviewModal").addEventListener("click", e => {
        if (e.target.id === "reviewModal") closeReviewModal();
    });

    document.getElementById("btnReload").onclick = () => load().catch(console.error);

    document.getElementById("rSend").onclick = async () => {
        try {
            const orderId = Number(lastOrderDetail?.id || 0);
            if (!orderId) return;

            document.getElementById("rSend").disabled = true;
            const data = await postReturn(orderId);

            alert(data?.message || "✅ Votre demande de retour a bien été envoyée.");
            closeReturnModal();
            await window.refreshClientNotifCount?.();
            await load().catch(() => {});
        } catch (err) {
            document.getElementById("rMsg").textContent = "❌ " + (err.message || "Erreur");
        } finally {
            document.getElementById("rSend").disabled = false;
        }
    };

    document.getElementById("rvSend").onclick = async () => {
        try {
            if (!rvProductId) return;
            if (rvRating < 1 || rvRating > 5) {
                document.getElementById("rvMsg").textContent = "Choisis une note (1 à 5).";
                return;
            }

            const title = document.getElementById("rvTitle").value || "";
            const comment = document.getElementById("rvComment").value || "";

            document.getElementById("rvSend").disabled = true;
            document.getElementById("rvMsg").textContent = "Envoi…";

            await postReview(rvProductId, { rating: rvRating, title, comment });

            alert("✅ Merci ! Ton avis a été envoyé.");
            closeReviewModal();
        } catch (err) {
            document.getElementById("rvMsg").textContent = "❌ " + (err?.message || "Erreur");
        } finally {
            document.getElementById("rvSend").disabled = false;
        }
    };

    load().catch(console.error);
});

document.addEventListener("click", async e => {
    const cancelBtn = e.target.closest("#btnCancelOrder");
    if (cancelBtn) {
        const orderId = Number(cancelBtn.getAttribute("data-oid") || 0);
        if (!orderId) return;
        if (!confirm("Voulez-vous vraiment annuler cette commande ?")) return;

        try {
            cancelBtn.disabled = true;
            await cancelOrder(orderId);
            alert("✅ Commande annulée.");
            closeModal();
            await load().catch(() => {});
        } catch (err) {
            alert(err?.message || "Erreur lors de l'annulation.");
        } finally {
            cancelBtn.disabled = false;
        }
        return;
    }

    const deleteBtn = e.target.closest("#btnDeleteOrder");
    if (deleteBtn) {
        const orderId = Number(deleteBtn.getAttribute("data-oid") || 0);
        if (!orderId) return;
        if (!confirm("Voulez-vous vraiment supprimer cette commande de votre liste ?")) return;

        try {
            deleteBtn.disabled = true;
            await deleteOrder(orderId);
            alert("✅ Commande supprimée.");
            closeModal();
            await load().catch(() => {});
        } catch (err) {
            alert(err?.message || "Erreur lors de la suppression.");
        } finally {
            deleteBtn.disabled = false;
        }
        return;
    }

    const returnBtn = e.target.closest("#btnAskReturn");
    if (returnBtn) {
        const orderId = Number(returnBtn.getAttribute("data-oid") || 0);
        if (!orderId) return;

        try {
            const data = await fetchAuth("/api/client/orders/" + orderId);
            const order = data?.item || data?.order || data;
            lastOrderDetail = order;

            document.getElementById("rInfo").textContent = "Commande #" + orderId + " — Statut: " + (order?.status || "");
            document.getElementById("rReason").value = "";
            document.getElementById("rMsg").textContent = "";

            renderReturnItems(order);
            openReturnModal();
        } catch (err) {
            alert(err.message || "Erreur");
        }
        return;
    }

    const reviewBtn = e.target.closest(".btnReview");
    if (reviewBtn) {
        const pid = Number(reviewBtn.getAttribute("data-pid") || 0);
        const pname = reviewBtn.getAttribute("data-pname") || "Produit";
        const oid = Number(reviewBtn.getAttribute("data-oid") || 0);
        if (!pid) return;

        try {
            document.getElementById("rvMsg").textContent = "Vérification…";
            const ok = await canReview(pid);

            if (!ok?.canReview) {
                alert("Impossible de noter : " + (ok?.reason || "non autorisé"));
                return;
            }

            rvProductId = pid;
            rvRating = 0;
            document.getElementById("rvInfo").textContent = "Produit: " + pname + " — Commande #" + oid + " — Achat vérifié ✅";
            document.getElementById("rvTitle").value = "";
            document.getElementById("rvComment").value = "";
            document.getElementById("rvMsg").textContent = "";
            document.querySelectorAll(".rvStar").forEach(x => x.classList.remove("btnGreen"));
            openReviewModal();
        } catch (err) {
            alert(err?.message || "Erreur");
        }
        return;
    }

    const starBtn = e.target.closest(".rvStar");
    if (starBtn) {
        rvRating = Number(starBtn.getAttribute("data-n") || 0);
        document.querySelectorAll(".rvStar").forEach(x => x.classList.remove("btnGreen"));
        document.querySelectorAll(".rvStar").forEach(x => {
            if (Number(x.getAttribute("data-n")) <= rvRating) x.classList.add("btnGreen");
        });
    }
});
