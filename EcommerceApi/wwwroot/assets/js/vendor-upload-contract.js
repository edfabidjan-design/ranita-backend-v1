(() => {
    "use strict";
    const API = window.API_BASE || location.origin;
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

    function setBtnState(btn, path) {
        if (!btn) return;

        const p = String(path || "").trim();

        if (!p) {
            btn.removeAttribute("href");
            btn.style.opacity = ".6";
            btn.style.pointerEvents = "none";
            return;
        }

        btn.href = p.startsWith("http") ? p : `${location.origin}${p}`;
        btn.style.opacity = "1";
        btn.style.pointerEvents = "auto";
    }

    async function loadVendorInfo() {
        const raw = localStorage.getItem(VENDOR_USER_KEY);

        const contractBtn = qs("#btnViewContract");
        const signedBtn = qs("#btnViewSignedContract");

        if (!raw) {
            setBtnState(contractBtn, "");
            setBtnState(signedBtn, "");
            return;
        }

        try {
            const user = JSON.parse(raw);

            setBtnState(contractBtn, user?.contractPdfPath || "");
            setBtnState(signedBtn, user?.signedContractPath || "");
        } catch {
            setBtnState(contractBtn, "");
            setBtnState(signedBtn, "");
        }
    }

    document.addEventListener("DOMContentLoaded", () => {
        loadVendorInfo();

        const form = qs("#frmUpload");
        if (!form) return;

        form.addEventListener("submit", async (e) => {
            e.preventDefault();

            const token = localStorage.getItem(VENDOR_TOKEN_KEY);
            if (!token) {
                showMsg("err", "Session vendeur introuvable.");
                return;
            }

            const fileInput = qs("#contractFile");
            const file = fileInput?.files?.[0];

            if (!file) {
                showMsg("err", "Sélectionne un fichier PDF.");
                return;
            }

            if (file.type !== "application/pdf" && !file.name.toLowerCase().endsWith(".pdf")) {
                showMsg("err", "Le fichier doit être un PDF.");
                return;
            }

            const fd = new FormData();
            fd.append("file", file);

            try {
                const res = await fetch(`${API}/api/vendor-auth/upload-signed-contract`, {
                    method: "POST",
                    headers: {
                        Authorization: `Bearer ${token}`
                    },
                    body: fd
                });

                const rawText = await res.text();
                let data = null;

                try {
                    data = rawText ? JSON.parse(rawText) : null;
                } catch {
                    data = { message: rawText };
                }

                if (!res.ok || data?.ok === false) {
                    throw new Error(data?.message || `HTTP ${res.status}`);
                }

                showMsg("ok", "✅ Contrat signé envoyé avec succès. Ton dossier est prêt pour validation.");

                const raw = localStorage.getItem(VENDOR_USER_KEY);
                if (raw && data?.path) {
                    try {
                        const user = JSON.parse(raw);
                        user.signedContractPath = data.path;
                        localStorage.setItem(VENDOR_USER_KEY, JSON.stringify(user));
                    } catch { }
                }

                loadVendorInfo();
                form.reset();

            } catch (err) {
                showMsg("err", err?.message || "Erreur");
            }
        });
    });
})();