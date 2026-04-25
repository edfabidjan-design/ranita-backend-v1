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
      <div style="
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
            <div style="font-weight:800;margin-bottom:6px">Pointure / Taille</div>
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

        root.addEventListener("click", (e) => {
            if (e.target === root) hide();
        });
        root.querySelector("#vmClose").addEventListener("click", hide);

        return root;
    }

    function hide() {
        const root = ensureModal();
        root.style.display = "none";
        document.body.style.overflow = "";
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

    function parseList(v) {
        if (window.parseList) return window.parseList(v);
        return String(v ?? "").split(/[,;|]/g).map(x => x.trim()).filter(Boolean);
    }

    function effectivePrice(p) {
        return window.effectivePrice ? window.effectivePrice(p) : Number(p?.price ?? 0);
    }

    // ✅ RECONSTRUIT sizes/colors depuis variants si manquant
    function normalizeVariants(p) {
        p = p || {};
        const vars = Array.isArray(p.variants) ? p.variants : [];

        let sizes = parseList(p.sizes ?? p.Sizes ?? "");
        let colors = parseList(p.colors ?? p.Colors ?? "");
        let singleAxisLabel = "";

        if (vars.length) {
            const sizeSet = new Set();
            const colorSet = new Set();
            const key1Set = new Set();
            const key2Set = new Set();

            vars.forEach(v => {
                const s = String(v.size ?? v.Size ?? "").trim();
                const c = String(v.color ?? v.Color ?? "").trim();
                const k1 = String(v.key1 ?? v.Key1 ?? "").trim();
                const k2 = String(v.key2 ?? v.Key2 ?? "").trim();

                if (s) sizeSet.add(s);
                if (c) colorSet.add(c);
                if (k1) key1Set.add(k1);
                if (k2) key2Set.add(k2);
            });

            const sizeValues = Array.from(sizeSet).map(x => String(x).trim().toLowerCase());
            const colorValues = Array.from(colorSet).map(x => String(x).trim().toLowerCase());
            const key1Values = Array.from(key1Set).map(x => String(x).trim().toLowerCase());
            const key2Values = Array.from(key2Set).map(x => String(x).trim().toLowerCase());

            const onlyTechnicalSize =
                !sizeValues.length || sizeValues.every(x => x === "unique" || x === "-");

            const onlyTechnicalColor =
                !colorValues.length || colorValues.every(x => x === "unique" || x === "-");

            const onlyTechnicalKey2 =
                !key2Values.length || key2Values.every(x => x === "unique" || x === "-");

            const isCapacity =
                key1Values.some(v => v.includes("ml") || v.includes("cl") || v.includes(" litre") || v === "1l" || v === "2l");

            const isSize =
                key1Values.some(v => ["xs", "s", "m", "l", "xl", "xxl", "xxxl"].includes(v));

            // ✅ CAS PARFUM / 1 seul axe réel
            if (key1Set.size && onlyTechnicalKey2 && onlyTechnicalSize && onlyTechnicalColor) {
                sizes = [];
                colors = Array.from(key1Set);

                if (isCapacity) {
                    singleAxisLabel = "Capacité";
                } else if (isSize) {
                    singleAxisLabel = "Taille";
                } else {
                    singleAxisLabel = "Option";
                }
            }
            // Cas standard reconstruit
            else {
                if (!sizes.length && sizeSet.size && !onlyTechnicalSize) {
                    sizes = Array.from(sizeSet);
                }

                if (!colors.length && colorSet.size) {
                    colors = Array.from(colorSet).filter(c => {
                        const v = String(c || "").trim().toLowerCase();
                        return v && v !== "unique" && v !== "-";
                    });
                }
                

                // Cas 2 axes dynamiques
                if (!sizes.length && !colors.length && key1Set.size && key2Set.size && !onlyTechnicalKey2) {
                    sizes = Array.from(key1Set);
                    colors = Array.from(key2Set);
                }
            }
        sizes = sizes.filter(x => {
            const v = String(x || "").trim().toLowerCase();
            return v && v !== "unique" && v !== "-";
        });

        colors = colors.filter(x => {
            const v = String(x || "").trim().toLowerCase();
            return v && v !== "unique" && v !== "-";
        });

            p.sizes = sizes.join(",");
            p.colors = colors.join(",");
            p.variants = vars;
        }

        console.log("NORMALIZE RESULT =", { sizes, colors, singleAxisLabel });

        return { p, sizes, colors, singleAxisLabel };
    }

    function norm(v) {
        return String(v ?? "").trim().toLowerCase();
    }

    function findVariant(p, size, color) {
        const vars = Array.isArray(p?.variants) ? p.variants : [];

        const wantedSize = norm(size).replace('"', '');
        const wantedColor = norm(color);

        return vars.find(v => {
            const s = norm(v.size).replace('"', '');
            const c = norm(v.color);

            // ✅ comparaison souple (IMPORTANT)
            const sizeMatch = !wantedSize || s.includes(wantedSize);
            const colorMatch = !wantedColor || c.includes(wantedColor);

            return sizeMatch && colorMatch;
        }) || null;
    }

    function updateStock(p) {
        const root = ensureModal();

        const size = root.querySelector("#vmSize")?.value || "";
        const color = root.querySelector("#vmColor")?.value || "";

        const v = findVariant(p, size, color);

        const st = root.querySelector("#vmStock");
        const btn = root.querySelector("#vmAdd");

        if (!v) {
            st.textContent = "Choisissez une option";
            st.style.color = "#9ca3af";
            btn.disabled = true;
            return;
        }

        const stock = Number(v.stock || 0);

        if (stock > 0) {
            st.textContent = `✅ Disponible (Stock: ${stock})`;
            st.style.color = "#22c55e";
            btn.disabled = false;
        } else {
            st.textContent = "⛔ Indisponible";
            st.style.color = "#ef4444";
            btn.disabled = true;
        }
    }


    // ✅ Fonction appelée depuis products.html
    window.openVariantsModal = function (product) {
        const root = ensureModal();
        clearMsg();

        const { p, sizes, colors, singleAxisLabel } = normalizeVariants(product);

        console.log("NORMALIZE RESULT =", {
            sizes,
            colors,
            singleAxisLabel
        });

        root.querySelector("#vmTitle").textContent = "Veuillez sélectionner une option";
        const price = effectivePrice(p);
        root.querySelector("#vmPrice").textContent =
            `${p.name || ""} • ${window.fmtFCFA ? window.fmtFCFA(price) : (price + " FCFA")}`;

        const sizeBox = root.querySelector("#vmSizeBox");
        const colorBox = root.querySelector("#vmColorBox");
        const sizeSel = root.querySelector("#vmSize");
        const colorSel = root.querySelector("#vmColor");

        const sizeLabel = sizeBox.querySelector("div");
        const colorLabel = colorBox.querySelector("div");

        sizeBox.style.display = sizes.length ? "block" : "none";
        colorBox.style.display = colors.length ? "block" : "none";

        sizeLabel.textContent =
            p.axis1Label && p.axis1Label.trim()
                ? p.axis1Label
                : "Option";

        colorLabel.textContent =
            p.axis2Label && p.axis2Label.trim()
                ? p.axis2Label
                : (singleAxisLabel || "Option");


        // option "Choisir..." pour forcer la sélection
        if (sizes.length) {
            sizeSel.innerHTML = `<option value="">-- Choisir --</option>` + sizes.map(s => `<option value="${s}">${s}</option>`).join("");
        } else {
            sizeSel.innerHTML = `<option value="">-</option>`;
        }

        if (colors.length) {
            colorSel.innerHTML = `<option value="">-- Choisir --</option>` + colors.map(c => `<option value="${c}">${c}</option>`).join("");
        } else {
            colorSel.innerHTML = `<option value="">-</option>`;
        }

        sizeSel.onchange = () => updateStock(p);
        colorSel.onchange = () => updateStock(p);

        const btnAdd = root.querySelector("#vmAdd");
        btnAdd.onclick = () => {
            clearMsg();

            const size = sizes.length ? String(sizeSel.value || "").trim() : "";
            const color = colors.length ? String(colorSel.value || "").trim() : "";

            if (sizes.length && !size) return showMsg("⚠️ Choisis une pointure / taille.");
            if (colors.length && !color) return showMsg("⚠️ Choisis une couleur.");

            if (typeof window.addToCartOnce !== "function") {
                return showMsg("addToCartOnce introuvable (cart-core.js).");
            }

            const v = findVariant(p, size, color);

            if (!v) {
                return showMsg("Choisissez une option valide.");
            }

            const r = window.addToCartOnce(p, {
                qty: 1,
                variantId: Number(v.id || 0),
                variantLabel: v.label || [v.size, v.color].filter(x => x && norm(x) !== "unique").join(" / "),
                variantStock: Number(v.stock || 0),
                price: effectivePrice(p)
            });

            if (r?.added) {
                window.refreshHeaderCart?.();
                hide();
            } else {
                showMsg("Impossible d'ajouter (déjà dans panier ou stock atteint).");
            }
        };

        root.style.display = "block";
        document.body.style.overflow = "hidden";
        updateStock(p); // affiche stock initial
    };
})();
