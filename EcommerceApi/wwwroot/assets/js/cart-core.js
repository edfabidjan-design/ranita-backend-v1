(() => {
    "use strict";

    const CART_KEY = "ranita_cart";

    function getCart() {
        try {
            return JSON.parse(localStorage.getItem(CART_KEY) || "[]");
        } catch {
            return [];
        }
    }

    function saveCart(cart) {
        localStorage.setItem(CART_KEY, JSON.stringify(cart));
    }

    function cartTotals() {
        const cart = getCart();
        let count = 0;
        let total = 0;

        for (const it of cart) {
            const qty = Number(it.qty || 0);
            const price = Number(it.price || 0);

            count += qty;
            total += qty * price;
        }

        return {
            count,
            total,
            subTotal: total
        };
    }

    function updateCartBadge() {
        const t = cartTotals();

        const a = document.getElementById("cartCount");
        if (a) a.textContent = String(t.count || 0);

        const b = document.getElementById("cc");
        if (b) b.textContent = String(t.count || 0);

        const barCount = document.getElementById("stickyCartCount");
        if (barCount) barCount.textContent = String(t.count || 0);

        const barTotal = document.getElementById("stickyCartTotal");
        if (barTotal) {
            barTotal.textContent = Number(t.total || 0).toLocaleString("fr-FR") + " FCFA";
        }
    }

    function addToCartOnce(p, opts = {}) {
        const id = Number(p.id || 0);
        if (!id) return { added: false };

        const variantId = Number(opts.variantId ?? 0);
        const qty = Math.max(1, Number(opts.qty || 1));

        const cart = getCart();

        const found = cart.find(x =>
            Number(x.id) === id &&
            Number(x.variantId || 0) === variantId
        );

        const resolvedPrice = Number(
            opts.price ??
            p.pricePromo ??
            p.price ??
            p.prix ??
            p.Price ??
            0
        ) || 0;

        const item = {
            id,
            variantId,
            variantLabel: opts.variantLabel || "",
            variantStock: Number(opts.variantStock || 0),
            price: resolvedPrice,
            name: p.name || p.titre || "Produit",
            img: p.mainImageUrl || p.mainImage || p.image || p.img || "",
            qty
        };

        if (found) {
            const stock = Number(item.variantStock || found.variantStock || 0);

            found.qty = stock > 0
                ? Math.min(Number(found.qty || 0) + qty, stock)
                : Number(found.qty || 0) + qty;

            found.variantLabel = item.variantLabel;
            found.variantStock = item.variantStock;
            found.img = item.img || found.img || "";
            found.price = resolvedPrice;
            found.name = item.name || found.name || "Produit";
        } else {
            cart.push(item);
        }

        saveCart(cart);
        updateCartBadge();

        console.log("🛒 CART SAVED =", cart);

        return { added: true };
    }

    function incQty(id, variantId = 0) {
        const cart = getCart();

        const it = cart.find(x =>
            Number(x.id) === Number(id) &&
            Number(x.variantId || 0) === Number(variantId || 0)
        );

        if (!it) return;

        const stock = Number(it.variantStock || 0);

        if (stock > 0) {
            it.qty = Math.min(Number(it.qty || 0) + 1, stock);
        } else {
            it.qty = Number(it.qty || 0) + 1;
        }

        saveCart(cart);
        updateCartBadge();
    }

    function decQty(id, variantId = 0) {
        const cart = getCart();

        const it = cart.find(x =>
            Number(x.id) === Number(id) &&
            Number(x.variantId || 0) === Number(variantId || 0)
        );

        if (!it) return;

        it.qty = Math.max(1, Number(it.qty || 0) - 1);

        saveCart(cart);
        updateCartBadge();
    }

    function removeFromCart(id, variantId = 0) {
        const cart = getCart().filter(x =>
            !(Number(x.id) === Number(id) && Number(x.variantId || 0) === Number(variantId || 0))
        );

        saveCart(cart);
        updateCartBadge();
    }

    window.getCart = getCart;
    window.saveCart = saveCart;
    window.cartTotals = cartTotals;
    window.addToCartOnce = addToCartOnce;
    window.incQty = incQty;
    window.decQty = decQty;
    window.removeFromCart = removeFromCart;
    window.updateCartBadge = updateCartBadge;
})();