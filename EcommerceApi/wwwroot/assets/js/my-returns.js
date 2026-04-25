(() => {
    "use strict";

    const API = (window.API_BASE || location.origin);
    const token = () => localStorage.getItem("ranita_client_token") || "";

    const qs = (s, r = document) => r.querySelector(s);
    const esc = (s) => String(s ?? "").replace(/[&<>"']/g, c => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[c]));
    const fmt = (n) => (Number(n || 0)).toLocaleString("fr-FR") + " FCFA";
    const fmtDate = (v) => { try { const d = new Date(v); return isNaN(d) ? "—" : d.toLocaleString(); } catch { return "—"; } };

    function statusLabel(s) {
        switch (String(s || "")) {
            case "Requested": return "En attente";
            case "Approved": return "Accepté";
            case "Rejected": return "Refusé";
            case "Received": return "Reçu";
            case "Refunded": return "Remboursé";
            case "Closed": return "Clôturé";
            default: return String(s || "—");
        }
    }

    function statusBadge(s) {
        const st = String(s || "").toLowerCase();
        const base = "display:inline-flex;align-items:center;gap:6px;padding:4px 10px;border-radius:999px;font-size:12px;font-weight:900;border:1px solid rgba(255,255,255,.12)";
        if (st === "requested") return `<span style="${base};background:rgba(251,191,36,.12);color:#fbbf24">⏳ ${statusLabel(s)}</span>`;
        if (st === "approved") return `<span style="${base};background:rgba(34,197,94,.10);color:#22c55e">✅ ${statusLabel(s)}</span>`;
        if (st === "rejected") return `<span style="${base};background:rgba(252,165,165,.10);color:#fca5a5">⛔ ${statusLabel(s)}</span>`;
        if (st === "received") return `<span style="${base};background:rgba(59,130,246,.10);color:#93c5fd">📦 ${statusLabel(s)}</span>`;
        if (st === "refunded") return `<span style="${base};background:rgba(168,85,247,.10);color:#d8b4fe">💸 ${statusLabel(s)}</span>`;
        if (st === "closed") return `<span style="${base};background:rgba(148,163,184,.10);color:#94a3b8">✅ ${statusLabel(s)}</span>`;
        return `<span style="${base}">${esc(statusLabel(s))}</span>`;
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
        if (!res.ok || data?.ok === false) throw new Error(data?.message || ("HTTP " + res.status));
        return data;
    }

    function openModal() { qs("#modal").style.display = "flex"; }
    function closeModal() { qs("#modal").style.display = "none"; }

    function renderList(items) {
        const box = qs("#list");
        if (!items?.length) {
            box.innerHTML = `<div class="muted">Aucun retour.</div>`;
            return;
        }

        box.innerHTML = `
      <div style="display:grid;gap:10px;margin-top:10px">
        ${items.map(r => `
          <button class="btn btnGhost" style="text-align:left;padding:12px;border-radius:14px"
                  data-rid="${r.id}">
            <div style="display:flex;justify-content:space-between;gap:10px;flex-wrap:wrap;align-items:center">
              <div>
                <div style="font-weight:950">Retour #${esc(r.id)} — Commande #${esc(r.orderId)}</div>
                <div class="muted" style="opacity:.8">${fmtDate(r.createdAtUtc || r.requestedAt)}</div>
                <div style="margin-top:6px">${statusBadge(r.status)}</div>
              </div>

              <div style="text-align:right">
                <div style="font-weight:950">${fmt(r.refundAmount || 0)}</div>
                <div class="muted" style="opacity:.8">${esc(r.refundMethod || "")}</div>
                <div class="muted" style="opacity:.75">${Number(r.itemsCount || 0)} article(s) • ${Number(r.imagesCount || 0)} image(s)</div>
              </div>
            </div>
          </button>
        `).join("")}
      </div>
    `;

        box.querySelectorAll("[data-rid]").forEach(btn => {
            btn.addEventListener("click", () => viewReturn(Number(btn.getAttribute("data-rid"))));
        });
    }

    function renderReturnDetail(d) {
        const r = d?.item || d;
        const items = Array.isArray(r?.items) ? r.items : [];
        const images = Array.isArray(r?.images) ? r.images : [];

        const timelineLine = (label, v) => `
      <div style="display:flex;justify-content:space-between;gap:10px;border-bottom:1px solid rgba(255,255,255,.06);padding:8px 0">
        <div class="muted">${esc(label)}</div>
        <div style="font-weight:900">${v ? fmtDate(v) : "—"}</div>
      </div>
    `;

        const imgGrid = !images.length ? `<div class="muted">Aucune image.</div>` : `
      <div style="display:flex;gap:10px;flex-wrap:wrap">
        ${images.map(u => `
          <a href="${esc(u)}" target="_blank" rel="noreferrer">
            <img src="${esc(u)}" style="width:130px;height:130px;object-fit:cover;border-radius:12px;border:1px solid rgba(255,255,255,.12)" />
          </a>
        `).join("")}
      </div>
    `;

        return `
      <div style="display:flex;justify-content:space-between;gap:10px;flex-wrap:wrap;align-items:center">
        <div>
          <div style="font-weight:950;font-size:18px">Retour #${esc(r.id)} — Commande #${esc(r.orderId)}</div>
          <div class="muted" style="opacity:.85">${statusBadge(r.status)}</div>
        </div>
        <div style="text-align:right">
          <div style="font-weight:950;font-size:18px">${fmt(r.refundAmount || 0)}</div>
          <div class="muted" style="opacity:.85">${esc(r.refundMethod || "")}${r.refundReference ? " • " + esc(r.refundReference) : ""}</div>
        </div>
      </div>

      <div>
        <div style="font-weight:900;margin:8px 0">📝 Raison / commentaire</div>
        <div style="border:1px solid rgba(255,255,255,.10);border-radius:12px;padding:10px;background:rgba(255,255,255,.03)">
          ${esc(r.reason || "")}
          ${r.customerComment ? `<div class="muted" style="margin-top:8px;opacity:.85">Client: ${esc(r.customerComment)}</div>` : ""}
          ${r.comment ? `<div class="muted" style="margin-top:8px;opacity:.85">Note: ${esc(r.comment)}</div>` : ""}
        </div>
      </div>

      <div>
        <div style="font-weight:900;margin:8px 0">🛒 Articles</div>
        <div style="display:grid;gap:8px">
          ${items.map(it => `
            <div style="border:1px solid rgba(255,255,255,.08);border-radius:12px;padding:10px;background:rgba(255,255,255,.03);display:flex;justify-content:space-between;gap:10px">
              <div>
                <div class="muted" style="opacity:.85">OrderItemId: ${esc(it.orderItemId)} • ProduitId: ${esc(it.productId)} • VendorId: ${esc(it.vendorId)}</div>
                <div style="margin-top:6px">
                  <span class="muted">Demandé:</span> <b>${Number(it.qtyRequested || 0)}</b>
                  &nbsp;•&nbsp;
                  <span class="muted">Accepté:</span> <b>${Number(it.qtyApproved || 0)}</b>
                  &nbsp;•&nbsp;
                  <span class="muted">Reçu:</span> <b>${Number(it.qtyReceived || 0)}</b>
                </div>
              </div>
              <div style="text-align:right">
                <div style="font-weight:900">${fmt(it.unitPrice || 0)}</div>
                <div class="muted" style="opacity:.85">Remboursé: <b>${fmt(it.refundLineAmount || 0)}</b></div>
              </div>
            </div>
          `).join("")}
        </div>
      </div>

      <div>
        <div style="font-weight:900;margin:8px 0">🖼 Images</div>
        ${imgGrid}
      </div>

      <div>
        <div style="font-weight:900;margin:8px 0">🕒 Historique (dates)</div>
        <div style="border:1px solid rgba(255,255,255,.10);border-radius:12px;padding:10px;background:rgba(255,255,255,.03)">
          ${timelineLine("Créé", r.createdAtUtc)}
          ${timelineLine("Demandé", r.requestedAt)}
          ${timelineLine("Accepté", r.approvedAt)}
          ${timelineLine("Refusé", r.rejectedAt)}
          ${timelineLine("Reçu", r.receivedAt)}
          ${timelineLine("Remboursé", r.refundedAt)}
          ${timelineLine("Clôturé", r.closedAt)}
        </div>
      </div>
    `;
    }

    function applyLocalFilter(items) {
        const q = (qs("#q")?.value || "").trim().toLowerCase();
        if (!q) return items;
        return (items || []).filter(r => {
            const txt = `${r.id} ${r.orderId} ${r.status} ${r.refundMethod}`.toLowerCase();
            return txt.includes(q);
        });
    }

    let ALL = [];

    async function load() {
        const st = (qs("#status")?.value || "").trim();
        const url = st ? `/api/client/returns?status=${encodeURIComponent(st)}&ts=${Date.now()}` : `/api/client/returns?ts=${Date.now()}`;
        const data = await fetchAuth(url);
        ALL = Array.isArray(data?.items) ? data.items : [];
        renderList(applyLocalFilter(ALL));
    }

    async function viewReturn(id) {
        openModal();
        const body = qs("#mBody");
        body.innerHTML = `<div class="muted">Chargement…</div>`;
        try {
            const data = await fetchAuth("/api/client/returns/" + id);
            body.innerHTML = renderReturnDetail(data);
        } catch (e) {
            body.innerHTML = `<div style="color:#fca5a5">❌ ${esc(e.message || "Erreur")}</div>`;
        }
    }

    document.addEventListener("DOMContentLoaded", () => {
        window.renderSiteHeader?.();
        if (!window.requireClientAuth?.()) return;

        qs("#mClose").onclick = closeModal;
        qs("#modal").addEventListener("click", (e) => { if (e.target.id === "modal") closeModal(); });

        qs("#btnReload").onclick = () => load().catch(console.error);
        qs("#status").onchange = () => load().catch(console.error);
        qs("#q").oninput = () => renderList(applyLocalFilter(ALL));

        load().catch(console.error);
    });

})();