// /assets/js/client-notifications.js
(() => {
    "use strict";

    const API = location.origin;
    const token = localStorage.getItem("ranita_client_token") || "";

    if (!token) {
        location.href = "/client-login.html";
        return;
    }

    function escapeHtml(s) {
        return String(s ?? "").replace(/[&<>"']/g, c => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;"
        }[c]));
    }

    function fmtDate(d) {
        try { return new Date(d).toLocaleString("fr-FR"); }
        catch { return ""; }
    }

    async function fetchNotif() {
        const res = await fetch(API + "/api/client/notifications?ts=" + Date.now(), {
            headers: { Authorization: "Bearer " + token },
            cache: "no-store"
        });

        if (res.status === 401) {
            localStorage.removeItem("ranita_client_token");
            localStorage.removeItem("ranita_client_user");
            location.href = "/client-login.html";
            return;
        }

        const json = await res.json();
        render(Array.isArray(json.items) ? json.items : []);
    }

    function render(items) {
        const box = document.getElementById("list");
        if (!box) return;

        box.innerHTML = "";

        if (!items.length) {
            box.innerHTML = "<div class='empty'>Aucune notification</div>";
            return;
        }

        items.forEach(n => {
            const div = document.createElement("div");
            div.className = "card " + (!n.isRead ? "unread" : "");

            const title = (n.title ?? "Notification");
            const body =
                n.body ??
                n.message ??
                n.text ??
                n.content ??
                n.description ??
                "";

            const when = n.createdAtUtc || n.createdAt || n.dateUtc || n.date;

            div.innerHTML = `
        <div class="title">${escapeHtml(title)}</div>
        <div style="margin-top:6px">${escapeHtml(body)}</div>
        <div class="muted" style="margin-top:8px">${when ? fmtDate(when) : ""}</div>
      `;

            div.addEventListener("click", () => markRead(n.id, div));
            box.appendChild(div);
        });
    }

    async function markRead(id, el) {
        try {
            const res = await fetch(API + "/api/client/notifications/" + id + "/read", {
                method: "POST",
                headers: { Authorization: "Bearer " + token }
            });

            if (res.status === 401) {
                localStorage.removeItem("ranita_client_token");
                localStorage.removeItem("ranita_client_user");
                location.href = "/client-login.html";
                return;
            }

            el.classList.remove("unread");

            // ✅ met à jour le badge dans le header (si client-auth.js est chargé sur d'autres pages)
            window.refreshClientNotifCount?.();
        } catch (e) {
            console.error("markRead error:", e);
        }
    }

    fetchNotif();
})();