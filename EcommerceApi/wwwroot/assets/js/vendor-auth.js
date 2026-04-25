(() => {
    "use strict";

    const VENDOR_TOKEN_KEY = "ranita_vendor_token";
    const VENDOR_USER_KEY = "ranita_vendor_user";

    function qs(s, r = document) {
        return r.querySelector(s);
    }

    function showMsg(type, text) {
        const box = qs("#msg");
        if (!box) return;

        box.style.display = "block";
        box.style.padding = "10px 12px";
        box.style.borderRadius = "12px";
        box.style.margin = "0 0 12px";
        box.style.border = "1px solid #334155";
        box.style.background = type === "ok"
            ? "rgba(34,197,94,.12)"
            : "rgba(239,68,68,.10)";
        box.style.color = "#e5e7eb";
        box.innerHTML = text;
    }

    async function postJson(url, body) {
        const res = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
            cache: "no-store"
        });

        const data = await res.json().catch(() => null);

        if (!res.ok || data?.ok === false) {
            throw new Error(data?.message || "Erreur serveur");
        }

        return data;
    }

    async function doRegister() {
        const vendorName = (qs("#vendorName")?.value || "").trim();
        const email = (qs("#email")?.value || "").trim();
        const phone = (qs("#phone")?.value || "").trim();
        const password = (qs("#pwd")?.value || "");

        const data = await postJson("/api/vendor-auth/register", {
            vendorName,
            email,
            password,
            phone
        });

        localStorage.setItem(VENDOR_TOKEN_KEY, data.token);
        localStorage.setItem(VENDOR_USER_KEY, JSON.stringify(data.user));

        const st = Number(data?.user?.vendorStatus ?? 0);

        if (st !== 1) {
            showMsg("ok", "✅ Demande envoyée.<br>Vérifiez votre email, signez les conditions, puis attendez la validation admin.");
            setTimeout(() => {
                location.href = "/vendor-pending.html";
            }, 1200);
            return;
        }

        location.href = "/vendor-dashboard.html";
    }

    async function doLogin() {
        const email = (qs("#email")?.value || "").trim();
        const password = (qs("#pwd")?.value || "");

        const data = await postJson("/api/vendor-auth/login", { email, password });

        localStorage.setItem(VENDOR_TOKEN_KEY, data.token);
        localStorage.setItem(VENDOR_USER_KEY, JSON.stringify(data.user));

        const st = Number(data?.user?.vendorStatus ?? 0);

        if (st !== 1) {
            location.href = "/vendor-pending.html";
            return;
        }

        location.href = "/vendor-dashboard.html";
    }

    document.addEventListener("DOMContentLoaded", () => {
        const frm = qs("#frm");
        if (!frm) return;

        frm.addEventListener("submit", async (e) => {
            e.preventDefault();

            try {
                showMsg("ok", "Traitement...");

                const path = (location.pathname || "").toLowerCase();

                if (path.includes("vendor-register")) {
                    await doRegister();
                } else {
                    await doLogin();
                }
            } catch (err) {
                showMsg("err", err?.message || "Erreur");
            }
        });
    });
})();