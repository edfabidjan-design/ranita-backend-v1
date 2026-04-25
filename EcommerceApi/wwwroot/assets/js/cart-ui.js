(() => {
    "use strict";

    function fmtFCFA(n) {
        if (typeof window.fmtFCFA === "function") return window.fmtFCFA(n);
        return Number(n || 0).toLocaleString("fr-FR") + " FCFA";
    }

    function escapeHtml(s) {
        return String(s ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;");
    }

    let _catalogLoaded = false;
    let _stockById = new Map();
    let _variantStockById = new Map();

    async function apiGetProducts() {
        const res = await fetch("/api/products", { cache: "no-store" });
        const json = await res.json();
        return json.items || json || [];
    }

    async function apiGetProductById(id) {
        const res = await fetch("/api/products/" + encodeURIComponent(id), { cache: "no-store" });
        const json = await res.json();
        return json.item || json;
    }

    function getStock(it) {
        const variantId = Number(it.variantId || 0);
        const productId = Number(it.id || 0);

        if (variantId > 0) {
            if (_variantStockById.has(variantId)) {
                return Number(_variantStockById.get(variantId) || 0);
            }

            if (Number(it.variantStock || 0) > 0) {
                return Number(it.variantStock || 0);
            }
        }

        if (_stockById.has(productId)) {
            return Number(_stockById.get(productId) || 0);
        }

        return Number(it.stock || it.productStock || 0);
    }

    function clampCartToStock() {
        const cart = window.getCart ? window.getCart() : [];
        let changed = false;

        for (let i = cart.length - 1; i >= 0; i--) {
            const it = cart[i];
            const stock = getStock(it);

            if (stock <= 0) {
                cart.splice(i, 1);
                changed = true;
                continue;
            }

            if (Number(it.qty || 0) > stock) {
                it.qty = stock;
                changed = true;
            }

            if (Number(it.qty || 0) < 1) {
                it.qty = 1;
                changed = true;
            }

            if (Number(it.variantId || 0) > 0) {
                it.variantStock = stock;
            } else {
                it.stock = stock;
            }
        }

        if (changed && window.saveCart) {
            window.saveCart(cart);
        }
    }

    window.loadLiveCatalogOnce = async function () {
        if (_catalogLoaded) return;

        _stockById.clear();
        _variantStockById.clear();

        const list = await apiGetProducts();
        for (const p of list) {
            _stockById.set(
                Number(p.id),
                Number(p.stock ?? p.Stock ?? 0)
            );
        }

        const cart = window.getCart ? window.getCart() : [];
        const uniqueIds = [...new Set(cart.map(x => Number(x.id || 0)).filter(Boolean))];

        for (const id of uniqueIds) {
            const p = await apiGetProductById(id);

            _stockById.set(
                Number(p.id || id),
                Number(p.stock ?? p.Stock ?? 0)
            );

            const vars = Array.isArray(p.variants) ? p.variants
                : Array.isArray(p.Variants) ? p.Variants
                    : [];

            for (const v of vars) {
                const variantId = Number(v.id || 0);
                if (!variantId) continue;

                _variantStockById.set(
                    variantId,
                    Number(v.stock ?? v.Stock ?? 0)
                );
            }
        }

        clampCartToStock();
        _catalogLoaded = true;

        console.log("✅ LIVE STOCK READY");
    };

    window.renderCartPage = function () {
        const lines = document.getElementById("lines");
        if (!lines) {
            console.error("❌ #lines introuvable");
            return;
        }

        const cart = window.getCart ? window.getCart() : [];
        lines.innerHTML = "";

        if (!cart.length) {
            lines.innerHTML = `<div style="padding:20px">Aucun produit</div>`;
            const sub = document.getElementById("sub");
            if (sub) sub.textContent = fmtFCFA(0);
            return;
        }

        for (const it of cart) {
            const stock = getStock(it);
            const variantText = String(it.variantLabel || "").trim();
            const img = String(it.img || "/assets/img/placeholder.jpg");

            const div = document.createElement("div");
            div.className = "line";
            div.dataset.id = String(Number(it.id || 0));
            div.dataset.variantId = String(Number(it.variantId || 0));

            div.innerHTML = `
            <img
                class="img"
                src="${escapeHtml(img)}"
                alt="${escapeHtml(it.name || "Produit")}"
                onerror="this.onerror=null;this.src='/assets/img/placeholder.jpg'"
            >

            <div>
                <div style="font-weight:900">${escapeHtml(it.name || "Produit")}</div>
                <div>${fmtFCFA(it.price || 0)}</div>
                ${variantText ? `<div class="muted">${escapeHtml(variantText)}</div>` : ""}
                <div class="muted">Stock dispo: ${stock}</div>
            </div>

            <div class="qtyBox">
                <button class="qtyBtn" data-dec>-</button>
                <span class="qtyVal">${Number(it.qty || 0)}</span>
                <button class="qtyBtn" data-inc ${Number(it.qty || 0) >= stock ? "disabled" : ""}>+</button>
                <button class="qtyBtn" data-del title="Supprimer">🗑</button>
            </div>
        `;

            lines.appendChild(div);
        }

        const sub = document.getElementById("sub");
        if (sub && typeof window.cartTotals === "function") {
            const t = window.cartTotals();
            sub.textContent = fmtFCFA(t.total || 0);
        }
    };

    window.bindCartEvents = function (lines) {
        if (!lines || lines.dataset.bound === "1") return;
        lines.dataset.bound = "1";

        lines.addEventListener("click", (e) => {
            const btn = e.target.closest("button");
            if (!btn) return;

            const line = btn.closest(".line");
            if (!line) return;

            const id = Number(line.dataset.id || 0);
            const variantId = Number(line.dataset.variantId || 0);

            if (btn.hasAttribute("data-inc")) {
                if (typeof window.incQty === "function") {
                    window.incQty(id, variantId);
                }
            }

            if (btn.hasAttribute("data-dec")) {
                if (typeof window.decQty === "function") {
                    window.decQty(id, variantId);
                }
            }

            if (btn.hasAttribute("data-del")) {
                if (typeof window.removeFromCart === "function") {
                    window.removeFromCart(id, variantId);
                }
            }

            window.renderCartPage();
            if (window.updateCartBadge) window.updateCartBadge();
        });
    };

    window.addEventListener("load", async () => {
        const lines = document.getElementById("lines");
        await window.loadLiveCatalogOnce();
        window.renderCartPage();
        window.bindCartEvents(lines);
        if (window.updateCartBadge) window.updateCartBadge();
    });
})();