// wwwroot/assets/js/modal-variants.js
(() => {
    "use strict";

    function ensureModal() {
        let root = document.getElementById("variantModal");
        if (root) return root;

        root = document.createElement("div");
        root.id = "variantModal";
        root.style.cssText = `
      display:none; position:fixed; inset:0; z-index:99999;
      background:rgba(0,0,0,.6); padding:18px;
    `;

        root.innerHTML = `
      <div id="variantCard" style="
        max-width:520px;margin:40px auto;background:#0b1220;color:#e5e7eb;
        border:1px solid rgba(255,255,255,.14); border-radius:18px;
        box-shadow:0 25px 80px rgba(0,0,0,.5); overflow:hidden;
      ">
        <div style="padding:14px 16px; display:flex; justify-content:space-between; gap:10px; align-items:center;
          background:rgba(17,24,39,.7); border-bottom:1px solid rgba(255,255,255,.10)">
          <div>
            <div id="vmTitle" style="font-weight:900;font-size:15px">Choisir une variante</div>
            <div id="vmPrice" style="opacity:.8;font-size:13px;margin-top:2px"></div>
          </div>
          <button id="vmClose" type="button" style="
            border:1px solid rgba(255,255,255,.16); background:transparent; color:#e5e7eb;
            border-radius:12px; padding:8px 10px; cursor:pointer; font-weight:900
          ">✕</button>
        </div>

        <div style="padding:16px; display:grid; gap:12px">
          <div id="vmSizeBox" style="display:none">
            <div style="font-weight:800;margin-bottom:6px">Taille</div>
            <select id="vmSize" style="width:100%;padding:12px;border-radius:12px;border:1px solid #1f2937;background:#111827;color:#e5e7eb"></select>
          </div>

          <div id="vmColorBox" style="display:none">
            <div style="font-weight:800;margin-bottom:6px">Couleur</div>
            <select id="vmColor" style="width:100%;padding:12px;border-radius:12px;border:1px solid #1f2937;background:#111827;color:#e5e7eb"></select>
          </div>

          <div id="vmStock" style="font-weight:900;font-size:13px"></div>

          <button id="vmAdd" type="button" style="
            width:100%; padding:12px 14px; border-radius:14px; border:0;
            background:linear-gradient(135deg,#22c55e,#16a34a);
            color:#052e16; font-weight:900; cursor:pointer
          ">Ajouter au panier</button>

          <div id="vmMsg" style="display:none; padding:10px; border-radius:12px;
            border:1px solid rgba(239,68,68,.35); background:rgba(15,23,42,.6)"></div>
        </div>
      </div>
    `;

        document.body.appendChild(root);

        // close actions
        root.addEventListener("click", (e) => {
            if (e.target === root) hide();
        });
        root.querySelector("#vmClose").addEventListener("click", hide);

        return root;
    }

    function showMsg(txt) {
        const root = ensureModal();
        const box = root.querySelector("#vmMsg");
        box.style.display = "block";
        box.textContent = txt;
    }
    function clearMsg() {
        const root = ensureModal();
        const box = root.querySelector("#vmMsg");
        box.style.display = "none";
        box.textContent = "";
    }

    function hide() {
        const root = ensureModal();
        root.style.display = "none";
    }

    function parseList(v) {
        if (window.parseList) return window.parseList(v);
        return String(v ?? "").split(/[,;|]/g).map(x => x.trim()).filter(Boolean);
    }

    function effectivePrice(p) {
        return window.effectivePrice ? window.effectivePrice(p) : Number(p?.price ?? 0);
    }

    function updateStock(p) {
        const root = ensureModal();
        const sizeEl = root.querySelector("#vmSize");
        const colorEl = root.querySelector("#vmColor");

        const size = sizeEl ? String(sizeEl.value || "").trim() : "";
        const color = colorEl ? String(colorEl.value || "").trim() : "";

        let stock = null;
        if (window.getVariantStockFromProduct) {
            stock = window.getVariantStockFromProduct(p, size, color);
        }

        // fallback si pas de liste variants : stock global
        if (stock === null || Number.isNaN(Number(stock))) {
            stock = Number(p?.stock ?? 0);
        }

        const st = root.querySelector("#vmStock");
        if (stock <= 0) {
            st.style.color = "#ef4444";
            st.textContent = "⛔ Stock épuisé pour cette variante";
            root.querySelector("#vmAdd").disabled = true;
            root.querySelector("#vmAdd").style.opacity = ".7";
        } else {
            st.style.color = "#22c55e";
            st.textContent = "✅ Stock : " + stock;
            root.querySelector("#vmAdd").disabled = false;
            root.querySelector("#vmAdd").style.opacity = "1";
        }
    }

    // ✅ Fonction globale attendue par products.html
    window.openVariantsModal = function (p) {
        const root = ensureModal();
        clearMsg();

        // title/price
        root.querySelector("#vmTitle").textContent = p?.name ? `Variantes – ${p.name}` : "Choisir une variante";
        const price = effectivePrice(p);
        root.querySelector("#vmPrice").textContent = (window.fmtFCFA ? window.fmtFCFA(price) : (price + " FCFA"));

        // sizes/colors
        const sizes = parseList(p?.sizes ?? p?.Sizes ?? "");
        const colors = parseList(p?.colors ?? p?.Colors ?? "");

        const sizeBox = root.querySelector("#vmSizeBox");
        const colorBox = root.querySelector("#vmColorBox");
        const sizeSel = root.querySelector("#vmSize");
        const colorSel = root.querySelector("#vmColor");

        sizeBox.style.display = sizes.length ? "block" : "none";
        colorBox.style.display = colors.length ? "block" : "none";

        if (sizes.length) {
            sizeSel.innerHTML = sizes.map(s => `<option value="${s}">${s}</option>`).join("");
        } else {
            sizeSel.innerHTML = `<option value="">-</option>`;
        }

        if (colors.length) {
            colorSel.innerHTML = colors.map(c => `<option value="${c}">${c}</option>`).join("");
        } else {
            colorSel.innerHTML = `<option value="">-</option>`;
        }

        // listeners
        sizeSel.onchange = () => updateStock(p);
        colorSel.onchange = () => updateStock(p);

        // add
        const btnAdd = root.querySelector("#vmAdd");
        btnAdd.onclick = () => {
            clearMsg();

            const size = sizes.length ? String(sizeSel.value || "").trim() : "";
            const color = colors.length ? String(colorSel.value || "").trim() : "";

            if (sizes.length && !size) return showMsg("Choisis une taille.");
            if (colors.length && !color) return showMsg("Choisis une couleur.");

            if (typeof window.addToCartOnce !== "function") {
                return showMsg("addToCartOnce introuvable (cart-core.js).");
            }

            const variantKey = (size || color) ? `${size}|${color}` : "";

            const r = window.addToCartOnce({
                id: p.id,
                name: p.name,
                price: effectivePrice(p),
                img: p.mainImageUrl || p.mainImage || p.imageUrl || p.image || p.photo || ""
            }, { variantRequired: true, variantKey, size, color });

            if (r?.added) {
                hide();
                // rafraîchir compteur panier si tu as la fonction
                window.refreshHeaderCart?.();
                // si tu as refreshCount dans products.html, il n’est pas global, donc on ne l’appelle pas ici.
            } else {
                showMsg("Impossible d'ajouter (déjà dans panier ou stock atteint).");
            }
        };

        root.style.display = "block";
        updateStock(p);
    };

})();
