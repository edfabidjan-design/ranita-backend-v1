
console.log("JS OK FIN");
        function escapeHtml(s) {
            return String(s ?? "")
                .replaceAll("&", "&amp;")
                .replaceAll("<", "&lt;")
                .replaceAll(">", "&gt;")
                .replaceAll('"', "&quot;")
                .replaceAll("'", "&#039;");
        }

        function getId() {
            return new URL(location.href).searchParams.get("id");
        }

        function refreshCount() {
            const el = document.getElementById("cc");
            if (!el) return;
            el.textContent = (window.cartTotals ? cartTotals().count : 0);
        }


        function lockBtn(btn, text) {
            btn.disabled = true;
            btn.textContent = text || "✅ Déjà ajouté";
        }

        // ✅ fallback si fmtFCFA n'existe pas
        function fmtMoney(v) {
            if (typeof window.fmtFCFA === "function") return window.fmtFCFA(v);
            const n = Number(v || 0);
            return n.toLocaleString("fr-FR") + " FCFA";
        }

        // ✅ récupère un produit quel que soit le format de réponse
async function apiGetProductSafe(id) {
    if (typeof window.apiGetProduct === "function") {
        return await window.apiGetProduct(id);
    }

    const url = "/api/products/" + encodeURIComponent(id) + "?t=" + Date.now();

    const res = await fetch(url, {
        cache: "no-store",
        headers: {
            "Cache-Control": "no-cache, no-store, must-revalidate",
            "Pragma": "no-cache",
            "Expires": "0"
        }
    });

    const text = await res.text();
    let json = null;

    try {
        json = text ? JSON.parse(text) : null;
    } catch {
        json = null;
    }

    if (!res.ok) {
        throw new Error((json && json.message) ? json.message : ("HTTP " + res.status));
    }

    return json;
}


        function niceLabel(code) {
            return String(code || "")
                .replaceAll("_", " ")
                .replaceAll("-", " ")
                .replace(/[^\w\s]/g, "")        // vire virgule etc.
                .replace(/\b\w/g, c => c.toUpperCase());
        }



        const CLIENT_TOKEN_KEY = "ranita_client_token";

        function getClientToken() {
            return localStorage.getItem(CLIENT_TOKEN_KEY) || "";
        }

        async function canReviewProduct(productId) {
            const t = getClientToken();
            if (!t) return { canReview: false };

            const r = await fetch(`/api/products/${encodeURIComponent(productId)}/can-review`, {
                headers: { Authorization: "Bearer " + t },
                cache: "no-store"
            });

            const data = await r.json().catch(() => null);
            if (!r.ok) return { canReview: false };
            return data || { canReview: false };
        }

        async function fetchJson(url, opt) {
            const r = await fetch(url, opt || {});
            const text = await r.text();
            let json = null;
            try { json = text ? JSON.parse(text) : null; } catch { json = null; }
            if (!r.ok) throw new Error(json?.message || ("HTTP " + r.status));
            return json;
        }

        function stars(n) {
            n = Number(n || 0);
            let s = "";
            for (let i = 1; i <= 5; i++) s += (i <= n ? "★" : "☆");
            return s;
        }

        let rvPage = 1;
        let rvStars = 0;
        let rvSort = "recent";
        const rvPageSize = 2; // ✅ compact sous boutique

        function openRvModal() {
            document.getElementById("rvMsg").textContent = "";
            document.getElementById("rvModal").classList.add("show");
        }
        function closeRvModal() {
            document.getElementById("rvModal").classList.remove("show");
        }

        function renderRvSummary(sum) {
            const box = document.getElementById("reviewsBox");
            if (!box) return;

            const avg = Number(sum?.avg || 0).toFixed(1);
            const count = Number(sum?.count || 0);

            document.getElementById("rvSummary").textContent = `${avg}/5 • ${count} avis`;

            // histogramme compact
            const rows = [5, 4, 3, 2, 1].map(k => {
                const cnt = sum["star" + k] || 0;
                const pct = count ? Math.round((cnt * 100) / count) : 0;

                return `
                    <div style="display:flex;align-items:center;gap:6px">
                        <div class="muted" style="width:26px;font-size:12px">${k}★</div>

                        <div style="flex:1;height:5px;background:rgba(255,255,255,.08);
                                    border-radius:999px;overflow:hidden">
                            <div style="height:5px;width:${pct}%;background:var(--accent)"></div>
                        </div>

                        <div class="muted" style="width:24px;text-align:right;font-size:12px">${cnt}</div>
                    </div>
                    `;
            }).join("");

            document.getElementById("rvHistogram").innerHTML =
                `<div style="display:grid;gap:4px">${rows}</div>`;

            box.style.display = "block";
        }

        function renderRvFiltersOnce() {
            const wrap = document.getElementById("rvStarsFilter");
            if (!wrap || wrap.dataset.ready === "1") return;
            wrap.dataset.ready = "1";

            const mk = (val, label) => {
                const b = document.createElement("button");
                b.type = "button";
                b.className = "badge";
                b.textContent = label;
                b.onclick = async () => {
                    rvStars = val;
                    [...wrap.children].forEach(x => x.classList.remove("active"));
                    b.classList.add("active");
                    rvPage = 1;
                    await loadReviews(true);
                };
                return b;
            };

            wrap.appendChild(mk(0, "Tous"));
            for (let k = 5; k >= 1; k--) wrap.appendChild(mk(k, `${k}★`));
            wrap.firstChild.classList.add("active");
        }

        function renderRvItems(items, reset) {
            const list = document.getElementById("rvList");
            if (!list) return;
            if (reset) list.innerHTML = "";

            (items || []).forEach(r => {
                const verified = r.verifiedPurchase ? `<span class="muted" style="margin-left:6px">✅</span>` : "";
                const div = document.createElement("div");
                div.style.border = "1px solid var(--border)";
                div.style.borderRadius = "14px";
                div.style.padding = "10px";
                div.style.background = "rgba(0,0,0,.10)";

                div.innerHTML = `
                  <div style="font-weight:900">${stars(r.rating)}${verified}
                    <span class="muted" style="margin-left:8px;font-weight:800">${escapeHtml(r.customerName || "Client")}</span>
                  </div>
                  ${r.title ? `<div style="margin-top:6px;font-weight:850">${escapeHtml(r.title)}</div>` : ""}
                  ${r.comment ? `<div class="muted" style="margin-top:6px">${escapeHtml(r.comment)}</div>` : ""}
                  <div class="muted" style="margin-top:6px;font-size:12px">${new Date(r.createdAtUtc).toLocaleString()}</div>
                `;
                list.appendChild(div);
            });
        }

        async function loadReviews(reset) {
            console.log("LOAD REVIEWS CALLED");
            const productId = getId();
            if (!productId) return;

            if (reset) rvPage = 1;

            const url = `/api/products/${encodeURIComponent(productId)}/reviews?sort=${encodeURIComponent(rvSort)}&page=${rvPage}&pageSize=${rvPageSize}` + (rvStars ? `&stars=${rvStars}` : "");
            const data = await fetchJson(url);

            renderRvFiltersOnce();
            renderRvSummary(data.summary);
            renderRvItems(data.items, reset);

            const more = (data.total > rvPage * rvPageSize);
            document.getElementById("rvMore").style.display = more ? "" : "none";

            // ✅ bouton visible seulement si le client a acheté
            const btn = document.getElementById("btnWriteReview");
            if (btn) {
                btn.style.display = "none";

                const token = getClientToken();
                if (token) {
                    try {
                        const reviewState = await canReviewProduct(productId);
                        if (reviewState?.canReview === true) {
                            btn.style.display = "";
                        }
                    } catch {
                        btn.style.display = "none";
                    }
                }
            }
        }

        async function submitReview() {
            console.log("SUBMIT REVIEW CLICKED");

            const token = getClientToken();
            console.log("TOKEN REVIEW =", token);

            if (!token) {
                document.getElementById("rvMsg").textContent = "Connecte-toi pour laisser un avis.";
                return;
            }

            const productId = getId();
            const body = {
                rating: parseInt(document.getElementById("rvRating").value, 10),
                title: document.getElementById("rvTitle").value || null,
                comment: document.getElementById("rvComment").value || null
            };

            console.log("BODY REVIEW =", body);

            try {
                const result = await fetchJson(`/api/products/${encodeURIComponent(productId)}/reviews`, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "Authorization": "Bearer " + token
                    },
                    body: JSON.stringify(body)
                });

                console.log("POST REVIEW RESULT =", result);

                closeRvModal();
                await loadReviews(true);
            }
            catch (e) {
                console.error("POST REVIEW ERROR =", e);
                document.getElementById("rvMsg").textContent =
                    e?.message || "Erreur serveur";
            }
        }


        async function apiGetPopularSafe(minClients = 2, take = 8) {
            const r = await fetch(`/api/products/popular?minClients=${encodeURIComponent(minClients)}&take=${encodeURIComponent(take)}`, { cache: "no-store" });
            if (!r.ok) return [];
            const json = await r.json().catch(() => null);
            return json?.items || [];
        }


        function renderPopular(list) {
            const box = document.getElementById("popularBox");
            const grid = document.getElementById("popularGrid");
            if (!box || !grid) return;

            if (!list || !list.length) {
                box.style.display = "none";
                grid.innerHTML = "";
                return;
            }

            grid.innerHTML = list.map(p => {
                const img = pickImage(p) || "/assets/img/placeholder.jpg";
                const name = escapeHtml(p?.name || p?.titre || "Produit");
                const basePrice = Number(p?.price ?? p?.prix ?? 0);
                const promo = Number(p?.pricePromo ?? p?.PricePromo ?? 0);
                const effective = (promo > 0 && promo < basePrice) ? promo : basePrice;

                return `
                  <a class="hcard" href="/product.html?id=${encodeURIComponent(p.id)}">
                    <img class="himg" src="${img}" alt="${name}" loading="lazy"
                         onerror="this.onerror=null;this.src='/assets/img/placeholder.jpg'">
                    <div class="hname">${name}</div>

<div class="hprice">${promoHtml(basePrice, promo)}</div>
                  </a>
                `;
            }).join("");

            box.style.display = "block";
        }

        function pickProduct(res) {
            // accepte: {item}, {data}, {product}, ou objet direct
            return (res && (res.item || res.data || res.product)) || res || null;
        }

        function pickImage(p) {
            // supporte plusieurs noms
            const raw =
                p?.mainImageUrl || p?.mainImage || p?.imageUrl || p?.image ||
                (Array.isArray(p?.images) ? (p.images[0]?.url || p.images[0]) : "") ||
                "";

            if (!raw) return "";

            // absUrl existe dans app.js
            if (typeof window.absUrl === "function") return window.absUrl(raw);

            if (raw.startsWith("http://") || raw.startsWith("https://")) return raw;
            return raw.startsWith("/") ? raw : ("/" + raw);
        }

        let product = null;
        let selectedSize = "";
        let selectedColor = "";
        let variants = [];
        let axis1Label = "option";
        let axis2Label = "option";
        let sizesList = [];
        let colorsList = [];
        let selectedVariantId = 0;
        let selectedVariantLabel = "";
        let selectedVariantPrice = 0;



        function pickImages(p) {
            // récupère la galerie si elle existe dans la réponse
            const list = p?.images || p?.productImages || p?.photos || [];
            if (!Array.isArray(list)) return [];
            return list.map(x => {
                const u = (typeof x === "string") ? x : (x.url || x.imageUrl || x.path || "");
                return u ? (window.absUrl ? absUrl(u) : u) : "";
            }).filter(Boolean);
        }


async function apiGetProductImagesSafe(id) {
    try {
        const res = await fetch("/api/products/" + encodeURIComponent(id) + "/images", {
            cache: "no-store"
        });

        if (!res.ok) return [];

        const json = await res.json().catch(() => null);

        if (!json) return [];

        const list = Array.isArray(json)
            ? json
            : (json.items || json.images || []);

        if (!Array.isArray(list)) return [];

        return list.map(x => {
            const u = (typeof x === "string") ? x : (x.url || x.imageUrl || x.path || "");
            return u ? (window.absUrl ? absUrl(u) : u) : "";
        }).filter(Boolean);

    } catch (e) {
        console.warn("Galerie lente :", e);
        return [];
    }
}
        function promoHtml(basePrice, promoPrice) {
            const price = Number(basePrice || 0);
            const promo = Number(promoPrice || 0);
            const hasPromo = promo > 0 && promo < price;

            if (!hasPromo) {
                return `<div class="priceNow">${fmtMoney(price)}</div>`;
            }

            const pct = Math.round((1 - (promo / price)) * 100);
            return `
                <div class="priceRow">
                  <div class="priceNow">${fmtMoney(promo)}</div>
                  <div class="priceOld">${fmtMoney(price)}</div>
                  <span class="promoBadge">-${pct}%</span>
                </div>
              `;
        }

        function norm(v) {
            return String(v ?? "")
                .toLowerCase()
                .trim()
                .replace(/\s+/g, " ")
                .replace(/["'”“]/g, "")
                .replace("pouces", "")
                .replace("inch", "");
        }

        function getVariantValues(v) {
            const a = norm(v.key1 ?? v.Key1 ?? v.size ?? v.Size ?? v.color ?? v.Color ?? "");
            const b = norm(v.key2 ?? v.Key2 ?? v.color ?? v.Color ?? v.size ?? v.Size ?? "");

            return { a, b };
        }

        function getVariantStock(size, color) {
            if (!Array.isArray(variants) || !variants.length) return 0;

            const wantedSize = norm(size);
            const wantedColor = norm(color);

            return variants
                .filter(v => {
                    const { a, b } = getVariantValues(v);

                    // couleur seule
                    if (!wantedSize && wantedColor) {
                        return a === wantedColor || b === wantedColor;
                    }

                    // taille seule
                    if (wantedSize && !wantedColor) {
                        return a === wantedSize || b === wantedSize;
                    }

                    // taille + couleur
                    if (wantedSize && wantedColor) {
                        return (
                            (a === wantedSize && b === wantedColor) ||
                            (a === wantedColor && b === wantedSize)
                        );
                    }

                    return false;
                })
                .reduce((sum, v) => sum + Number(v.stock ?? v.Stock ?? 0), 0);
        }


function findSelectedVariant() {
    if (!Array.isArray(variants) || !variants.length) return null;

    const wantedSize = norm(selectedSize);
    const wantedColor = norm(selectedColor);

    return variants.find(v => {
        const a = norm(v.key1 ?? v.Key1 ?? v.size ?? v.Size ?? "");
        const b = norm(v.key2 ?? v.Key2 ?? v.color ?? v.Color ?? "");

        // ✅ taille + couleur
        if (wantedSize && wantedColor) {
            return (
                (a === wantedSize && b === wantedColor) ||
                (a === wantedColor && b === wantedSize)
            );
        }

        // ✅ couleur seule
        if (!wantedSize && wantedColor) {
            return a === wantedColor || b === wantedColor;
        }

        // ✅ taille seule
        if (wantedSize && !wantedColor) {
            return a === wantedSize || b === wantedSize;
        }

        return false;
    }) || null;
}


        function validateVariants() {
            const btn = document.getElementById("btnAdd");
            const qtyInput = document.getElementById("qty");
            const stockEl = document.getElementById("stock");
            const msg = document.getElementById("msg");

            console.log("SELECTED SIZE:", selectedSize);
            console.log("SELECTED COLOR:", selectedColor);
            console.log("VARIANTS:", variants);

            if (!btn || !qtyInput || !stockEl || !msg) return;

            // produit sans variantes
            if (!variants || !variants.length) {
                const s = Number(product?.stock ?? 0);
                selectedVariantId = 0;
                selectedVariantLabel = "";
                selectedVariantPrice = Number(product?.pricePromo || product?.price || 0);

                if (s > 0) {
                    btn.disabled = false;
                    qtyInput.disabled = false;
                    qtyInput.max = String(s);
                    stockEl.textContent = "✅ Stock : " + s;
                    msg.textContent = "";
                } else {
                    btn.disabled = true;
                    qtyInput.disabled = true;
                    stockEl.textContent = "⛔ Indisponible";
                    msg.textContent = "Ce produit est en rupture.";
                }
                return;
            }

            const v = findSelectedVariant();

            if (!v) {
                selectedVariantId = 0;
                selectedVariantLabel = "";
                selectedVariantPrice = 0;
                btn.disabled = true;
                qtyInput.disabled = true;
                stockEl.textContent = "Choisissez une option";
                msg.textContent = "Sélectionnez une option.";
                return;
            }

            const stock = Number(v.stock ?? v.Stock ?? 0);
            selectedVariantId = Number(v.id || 0);
           selectedVariantLabel = String(
    v.label || `${v.key1 || v.size || ""} ${v.key2 || v.color || ""}`.trim()
).trim();
            selectedVariantPrice = Number(
                v.price ?? v.Price ??
                product?.pricePromo ?? product?.PricePromo ??
                product?.price ?? product?.Price ?? 0
            );

            qtyInput.max = String(Math.max(1, stock));

            if (stock > 0) {
                btn.disabled = false;
                qtyInput.disabled = false;
                stockEl.textContent = "✅ Stock : " + stock;
                msg.textContent = "";
            } else {
                btn.disabled = true;
                qtyInput.disabled = true;
                stockEl.textContent = "⛔ Indisponible";
                msg.textContent = "Cette variante est en rupture.";
            }
        }


        function pickCategoryId(p) {
            return p?.categoryId ?? p?.CategoryId ?? p?.category?.id ?? null;
        }


        async function apiGetRelatedProductsSafe(categoryId, excludeId, take = 6) {
            // ✅ Si tu as déjà une fonction apiGetProducts dans app.js, on l’utilise
            if (typeof window.apiGetProducts === "function") {
                // adapte selon ton apiGetProducts si besoin
                const res = await window.apiGetProducts({ categoryId, take });
                const arr = Array.isArray(res) ? res : (res?.items || res?.data || []);
                return (arr || []).filter(x => String(x.id) !== String(excludeId)).slice(0, take);
            }

            // ✅ Fallback: endpoint simple
            // 1) essaie /api/products?categoryId=...
            let url = `/api/products?categoryId=${encodeURIComponent(categoryId)}&take=${encodeURIComponent(take + 1)}`;
            let r = await fetch(url, { cache: "no-store" });

            // 2) si ton API ne supporte pas categoryId, on fait un fallback /api/products (et on filtre côté client)
            if (!r.ok) {
                url = `/api/products`;
                r = await fetch(url, { cache: "no-store" });
            }

            if (!r.ok) return [];

            const json = await r.json().catch(() => null);
            const arr = Array.isArray(json) ? json : (json?.items || json?.data || json?.products || []);
            const list = (arr || []).filter(x => String(x.id) !== String(excludeId));

            // si on a categoryId dans chaque item, filtre
            const filtered = list.filter(x => {
                const cid = x?.categoryId ?? x?.CategoryId ?? x?.categorieId ?? null;
                return categoryId ? String(cid) === String(categoryId) : true;
            });

            return filtered.slice(0, take);
        }

        function renderBundle(list) {
            const box = document.getElementById("bundleBox");
            const grid = document.getElementById("bundleGrid");
            if (!box || !grid) return;

            if (!list || !list.length) {
                box.style.display = "none";
                grid.innerHTML = "";
                return;
            }

            grid.innerHTML = list.map(p => {
                const img = pickImage(p) || "/assets/img/placeholder.jpg";
                const name = escapeHtml(p?.name || p?.titre || "Produit");
                const basePrice = Number(p?.price ?? p?.prix ?? 0);
                const promo = Number(p?.pricePromo ?? p?.PricePromo ?? 0);


                return `
                  <a class="hcard" href="/product.html?id=${encodeURIComponent(p.id)}">
                    <img class="himg" src="${img}" alt="${name}" loading="lazy"
                         onerror="this.onerror=null;this.src='/assets/img/placeholder.jpg'">
                    <div class="hname">${name}</div>

<div class="hprice">${promoHtml(basePrice, promo)}</div>
                  </a>
                `;
            }).join("");

            box.style.display = "block";
        }

        const RECENT_KEY = "ranita_recent_products";

        function pushRecent(p) {
            try {
                if (!p?.id) return;

                let list = [];
                try { list = JSON.parse(localStorage.getItem(RECENT_KEY) || "[]"); } catch { list = []; }

                list = (list || []).filter(x => String(x.id || x) !== String(p.id));
                list.unshift({ id: p.id, ts: Date.now() });
                list = list.slice(0, 20);

                localStorage.setItem(RECENT_KEY, JSON.stringify(list));
            } catch { }
        }

        function getRecentIds(excludeId, take = 10) {
            try {
                let list = [];
                try { list = JSON.parse(localStorage.getItem(RECENT_KEY) || "[]"); } catch { list = []; }

                return (list || [])
                    .filter(x => String(x.id || x) !== String(excludeId))
                    .slice(0, take)
                    .map(x => x.id || x);
            } catch {
                return [];
            }
        }


async function apiGetProductsByIdsSafe(ids) {
    if (!Array.isArray(ids) || !ids.length) return [];

    // limite mobile
    const trimmed = ids.slice(0, 4);

    const results = await Promise.allSettled(
        trimmed.map(id => apiGetProductSafe(id))
    );

    return results
        .filter(r => r.status === "fulfilled")
        .map(r => pickProduct(r.value))
        .filter(Boolean);
}

        function renderRecent(list) {
            const box = document.getElementById("recentBox");
            const grid = document.getElementById("recentGrid");
            if (!box || !grid) return;

            if (!list || !list.length) {
                box.style.display = "none";
                grid.innerHTML = "";
                return;
            }

            grid.innerHTML = list.map(p => {
                const img = pickImage(p) || "/assets/img/placeholder.jpg";
                const name = escapeHtml(p.name || p.titre || "Produit");

                const basePrice = Number(p.price ?? p.prix ?? 0);
                const promo = Number(p.pricePromo ?? p.PricePromo ?? 0);

                return `
              <a class="hcard" href="/product.html?id=${encodeURIComponent(p.id)}">
                <img class="himg" src="${img}" alt="${name}" loading="lazy"
                     onerror="this.onerror=null;this.src='/assets/img/placeholder.jpg'">
                <div class="hname">${name}</div>
                <div class="hprice">${promoHtml(basePrice, promo)}</div>
              </a>
            `;
            }).join("");

            box.style.display = "block";
        }

        function renderRelated(products) {
            const box = document.getElementById("relatedBox");
            const grid = document.getElementById("relatedGrid");
            if (!box || !grid) return;

            if (!products || !products.length) {
                box.style.display = "none";
                grid.innerHTML = "";
                return;
            }

            grid.innerHTML = products.map(p => {
                const img = pickImage(p) || "/assets/img/placeholder.jpg";
                const name = escapeHtml(p?.name || p?.titre || "Produit");
                const basePrice = Number(p?.price ?? p?.prix ?? 0);
                const promo = Number(p?.pricePromo ?? p?.PricePromo ?? 0);
                const effective = (promo > 0 && promo < basePrice) ? promo : basePrice;

                return `
                      <a class="related-card" href="/product.html?id=${encodeURIComponent(p.id)}">
                        <img class="related-img" src="${img}" alt="${name}" loading="lazy"
                             onerror="this.onerror=null;this.src='/assets/img/placeholder.jpg'">
                        <div class="related-name">${name}</div>
                       <div class="related-price">${promoHtml(basePrice, promo)}</div>
                      </a>
                    `;
            }).join("");

            box.style.display = "block";
        }

        function renderGallery(urls, activeUrl) {
            const box = document.getElementById("gallery");
            if (!box) return;

            if (!urls.length) {
                box.innerHTML = "";
                return;
            }

            box.innerHTML = urls.map(u => {
                const cls = (u === activeUrl) ? "thumb active" : "thumb";
                return `<img class="${cls}" src="${u}" data-url="${u}" alt="" loading="lazy" decoding="async">`;
            }).join("");

            box.querySelectorAll("img").forEach(img => {
                img.addEventListener("click", () => {
                    const u = img.getAttribute("data-url");
                    const main = document.getElementById("mainImg");
                    if (main) main.src = u;

                    box.querySelectorAll("img").forEach(x => x.classList.remove("active"));
                    img.classList.add("active");
                });
            });
        }

        function articleForLabel(label) {
            const s = String(label || "").toLowerCase().trim();

            if (
                s.startsWith("pointure") ||
                s.startsWith("taille") ||
                s.startsWith("couleur") ||
                s.startsWith("capacité")
            ) return "une";

            return "un";
        }

        function detectVariantLabel(values, axisIndex) {
            const arr = (values || []).map(x => String(x || "").trim()).filter(Boolean);
            const joined = arr.join(" ").toLowerCase();

            if (!arr.length) return "";

            if (axisIndex === 1) {
                // 📺 TV / tailles écran : à tester AVANT les pointures
                const isScreenSize = arr.every(v => {
                    const val = String(v).toLowerCase().replace(/\s/g, "");
                    return (
                        /^\d{2,3}("|”)?$/.test(val) ||   // 32" 55"
                        /^\d{2,3}p$/.test(val) ||        // 32p
                        /^\d{2,3}$/.test(val)            // 32
                    );
                });

                if (isScreenSize) {
                    return "Tailles écran disponibles";
                }

                // 👟 pointures
                if (/\b(36|37|38|39|40|41|42|43|44|45|46)\b/.test(joined)) {
                    return "Pointures disponibles";
                }

                // 👕 tailles vêtements
                if (/\b(xs|s|m|l|xl|xxl|xxxl)\b/.test(joined)) {
                    return "Tailles disponibles";
                }

                // 🧳 formats
                if (/\b(cabine|moyen|grand|small|medium|large)\b/.test(joined)) {
                    return "Formats disponibles";
                }

                // 💾 capacités
                if (/\b(32go|64go|128go|256go|512go|1to|2to|4go|8go|16go)\b/.test(joined)) {
                    return "Capacités disponibles";
                }

                // 🎨 couleurs
                if (/\b(noir|blanc|bleu|gris|rouge|vert|jaune|rose|argent|doré|inox|marron|violet)\b/.test(joined)) {
                    return "Couleurs disponibles";
                }

                return "Options disponibles";
            }

            if (/\b(noir|blanc|bleu|gris|rouge|vert|jaune|rose|argent|doré|inox|marron|violet)\b/.test(joined)) {
                return "Couleurs disponibles";
            }

            return "Options disponibles";
        }

        function normalize(v) {
            return (v ?? "").toString().trim().toLowerCase();
        }






        function uniq(arr) {
            return Array.from(new Set((arr || []).map(x => String(x || "").trim()).filter(Boolean)));
        }

        function deriveAxis1FromVariants(vars) {
            return uniq(
                vars.map(v =>
                    String(v.key1 ?? v.Key1 ?? v.size ?? v.Size ?? "").trim()
                )
            ).filter(Boolean);
        }

        function deriveAxis2FromVariants(vars) {
            return uniq(
                vars.map(v =>
                    String(v.key2 ?? v.Key2 ?? v.color ?? v.Color ?? "").trim()
                )
            ).filter(Boolean);
        }

        function totalStockFromVariants(vars) {
            return (vars || []).reduce((sum, v) => sum + (Number(v.stock ?? v.Stock ?? 0) || 0), 0);
        }


        function pickAttributes(p) {
            // ✅ 1) Format conseillé (groupé) :
            // attributes: [{ code, name, values: ["Noir","Rouge"] }]
            // OU attributes: [{ name, values: [...] }]
            let arr = (p && (p.attributes || p.Attributes)) || null;

            // ✅ 2) Autres noms possibles (selon API)
            if (!arr) arr = p?.productAttributes || p?.ProductAttributes || p?.attributeValues || p?.AttributeValues || [];

            if (!Array.isArray(arr)) return [];

            const out = [];

            for (const a of arr) {
                // --- FORMAT GROUPÉ ---
                // a.attribute = { code, name } ; a.values = [...]
                const code =
                    a?.code || a?.Code ||
                    a?.attributeCode || a?.AttributeCode ||
                    a?.attribute?.code || a?.attribute?.Code ||
                    a?.attribute?.name || a?.attribute?.Name ||
                    a?.name || a?.Name ||
                    "";

                // values[] peut être strings OU objets { value }
                let values = [];

                if (Array.isArray(a?.values)) {
                    values = a.values.map(x => (typeof x === "string" ? x : (x?.value || x?.Value || x?.label || x?.Label || ""))).filter(Boolean);
                }

                // --- FORMAT “FLAT” (admin-like) ---
                // a.ValueText / ValueInt / Option.Value...
                const flatValue =
                    a?.value ?? a?.Value ??
                    a?.valueText ?? a?.ValueText ??
                    a?.valueInt ?? a?.ValueInt ??
                    a?.valueDecimal ?? a?.ValueDecimal ??
                    a?.valueBool ?? a?.ValueBool ??
                    a?.valueDate ?? a?.ValueDate ??
                    a?.option?.value ?? a?.option?.Value ??
                    a?.Option?.Value ??
                    "";

                if (!values.length && flatValue !== null && flatValue !== undefined && String(flatValue).trim() !== "") {
                    values = [String(flatValue).trim()];
                }

                const cleanCode = String(code || "").trim();
                values = values.map(v => String(v || "").trim()).filter(Boolean);

                if (cleanCode && values.length) {
                    out.push({ code: cleanCode, value: values.join(", ") });
                }
            }

            return out;
        }

        async function apiGetBoughtTogetherSafe(productId, take = 8) {
            const r = await fetch(`/api/products/${encodeURIComponent(productId)}/bought-together?take=${encodeURIComponent(take)}`, { cache: "no-store" });
            if (!r.ok) return [];
            const json = await r.json().catch(() => null);
            return (json && (json.items || json.data)) ? (json.items || json.data) : (Array.isArray(json) ? json : []);
        }



        function splitList(raw) {
            return String(raw || "")
                .split(/[;,]/)
                .map(x => x.trim())
                .filter(Boolean);
        }


async function loadDeferredSections(product, id, mainImgUrl) {
    const runWhenIdle = (fn, timeout = 1200) => {
        if ("requestIdleCallback" in window) {
            requestIdleCallback(fn, { timeout });
        } else {
            setTimeout(fn, 250);
        }
    };

    // 1) galerie presque tout de suite
    runWhenIdle(async () => {
        try {
            const galleryUrls = await apiGetProductImagesSafe(id);
            const finalUrls = galleryUrls.length
                ? galleryUrls
                : (mainImgUrl ? [mainImgUrl] : []);
            renderGallery(finalUrls, mainImgUrl || finalUrls[0] || "");
        } catch (e) {
            console.warn("Galerie lente :", e);
        }
    }, 500);

    // 2) sections commerciales après un petit délai
    setTimeout(async () => {
        try {
            const catId = pickCategoryId(product);

            const [relatedRes, bundleRes, popularRes] = await Promise.allSettled([
                catId ? apiGetRelatedProductsSafe(catId, product.id, 12) : Promise.resolve([]),
                apiGetBoughtTogetherSafe(product.id, 8),
                apiGetPopularSafe(2, 8)
            ]);

            let related = [];
            if (relatedRes.status === "fulfilled") {
                related = relatedRes.value || [];
                renderRelated(related.slice(0, 8));
            } else {
                renderRelated([]);
            }

            let bundle = [];
            if (bundleRes.status === "fulfilled") {
                bundle = bundleRes.value || [];
            }
            if (!bundle.length) bundle = related.slice(8, 16);
            renderBundle(bundle);

            if (popularRes.status === "fulfilled") {
                const popular = popularRes.value || [];
                renderPopular(popular.filter(x => String(x.id) !== String(product.id)));
            }
        } catch (e) {
            console.warn("Sections commerciales lentes :", e);
        }
    }, 900);

    // 3) récents encore plus tard
    setTimeout(async () => {
        try {
            const recentIds = getRecentIds(product.id, 6);
            const recentProducts = await apiGetProductsByIdsSafe(recentIds);
            renderRecent(recentProducts || []);
        } catch (e) {
            console.warn("Récents lents :", e);
        }
    }, 1400);

    // 4) avis en dernier
    setTimeout(async () => {
        try {
            await loadReviews(true);
        } catch (e) {
            console.warn("Avis lents :", e);
        }
    }, 1800);
}


        async function load() {
            refreshCount();

            const id = getId();
            const err = document.getElementById("err");
            const content = document.getElementById("content");
            const btn = document.getElementById("btnAdd");
            const qtyInput = document.getElementById("qty");

            if (!id) {
                err.textContent = "ID manquant.";
                return;
            }

            err.textContent = "";
            content.style.display = "none"; // 🔥 IMPORTANT

            document.getElementById("name").textContent = "Chargement du produit...";
            document.getElementById("price").textContent = "";
            document.getElementById("promo").innerHTML = "";
            document.getElementById("stock").textContent = "Chargement...";
            document.getElementById("msg").textContent = "";

            try {
                const res = await apiGetProductSafe(id);
                product = pickProduct(res);

                console.log("PRODUCT PRICE:", product?.price, product?.Price, product?.pricePromo, product?.PricePromo);
                console.log("PRODUCT STOCK:", product?.stock, product?.Stock);
                // ==========================
                // ✅ Boutique (Vendor)
                // ==========================
                const vendorId =
                    product.vendorId ??
                    product.VendorId ??
                    product.shopVendorId ??
                    product.ShopVendorId ??
                    product.vendor?.id ??
                    product.vendor?.Id ??
                    product.Vendor?.id ??
                    product.Vendor?.Id ??
                    null;

                const vendorName =
                    product.vendorShopName ??
                    product.VendorShopName ??
                    product.vendorName ??
                    product.VendorName ??
                    product.shopName ??
                    product.ShopName ??
                    product.vendor?.shopName ??
                    product.vendor?.ShopName ??
                    product.vendor?.name ??
                    product.vendor?.Name ??
                    product.Vendor?.shopName ??
                    product.Vendor?.ShopName ??
                    product.Vendor?.name ??
                    product.Vendor?.Name ??
                    "";

                const shopBar = document.getElementById("shopBar");
                const shopLink = document.getElementById("shopLink");
                const shopNameEl = document.getElementById("shopName");

                // ✅ produit admin = pas de boutique
                if (shopBar && shopLink && shopNameEl && vendorId) {
                    shopNameEl.textContent = vendorName || "Boutique";
                    shopLink.href = "/shop.html?vendorId=" + encodeURIComponent(vendorId);
                    shopBar.style.display = "flex";
                } else if (shopBar) {
                    shopBar.style.display = "none";
                }

   
                console.log("PRODUCT RAW:", product);
                console.log("ATTR SOURCE attributes:", product?.attributes || product?.Attributes);
                console.log("ATTR SOURCE productAttributes:", product?.productAttributes || product?.ProductAttributes);


                // ================== FICHE TECHNIQUE ==================
                const techBox = document.getElementById("techSpecsBox");
                const techDiv = document.getElementById("techSpecs");
                if (techDiv) techDiv.innerHTML = "";
                if (techBox) techBox.style.display = "none";

                const attrs = pickAttributes(product);

                if (attrs.length) {
                    const labels = {
                        resolution: "Résolution",
                        screen_size: "Taille écran",
                        panel_type: "Technologie écran",
                        smart_tv: "Smart TV",
                        os: "Système",
                        hdmi_ports: "HDMI",
                        usb_ports: "USB",
                        wifi: "WiFi",
                        bluetooth: "Bluetooth"
                    };

                    const rows = attrs.map(a => {
                        const label = labels[a.code] || niceLabel(a.code);

                        return `
                                        <div class="spec-item">
                                          <span class="spec-k">${escapeHtml(label)}</span>
                                          <span class="spec-v" title="${escapeHtml(a.value)}">${escapeHtml(a.value)}</span>
                                        </div>
                                      `;
                    }).join("");

                    if (rows && techDiv && techBox) {
                        techDiv.innerHTML = `<div class="specs-grid">${rows}</div>`;
                        techBox.style.display = "block";
                    }

                }





                if (!product || !product.id) {
                    err.textContent = "❌ Produit introuvable.";
                    content.style.display = "none"; // 🔥 IMPORTANT
                    return;
                }
                content.style.display = "block"; // ✅ afficher seulement si OK


                // ✅ variantes : accepte variants OU Variants
                variants = Array.isArray(product.variants) ? product.variants
                    : Array.isArray(product.Variants) ? product.Variants
                        : [];

                // ✅ normalise champs AVANT d'utiliser sizesRaw/colorsRaw
                const sizesRaw = product.sizes ?? product.Sizes ?? "";   // ex: "36;37;38"
                const colorsRaw = product.colors ?? product.Colors ?? ""; // ex: "Noir;Blanc"




                // ✅ listes tailles/couleurs
                sizesList = splitList(sizesRaw);
                colorsList = splitList(colorsRaw);

                function looksLikeColor(v) {
                    const s = String(v || "").trim().toLowerCase();
                    return [
                        "noir", "blanc", "bleu", "rouge", "vert", "jaune",
                        "marron", "gris", "rose", "violet", "argent", "doré", "inox"
                    ].includes(s);
                }

                function looksLikeSize(v) {
                    const s = String(v || "").trim().toLowerCase();

                    // pointures + tailles vêtement
                    if (/^\d+$/.test(s)) return true;
                    if (["xs", "s", "m", "l", "xl", "xxl", "xxxl"].includes(s)) return true;

                    // tailles écran TV : 32, 43, 55", 65 pouces, 43p
                    if (/^\d{2,3}("|”)?$/.test(s)) return true;
                    if (/^\d{2,3}p$/.test(s)) return true;
                    if (/^\d{2,3}\s*(pouces|inch)$/.test(s)) return true;

                    return false;
                }

                function isMostlyColorList(list) {
                    return Array.isArray(list) && list.length > 0 && list.every(looksLikeColor);
                }

                function isMostlySizeList(list) {
                    return Array.isArray(list) && list.length > 0 && list.every(looksLikeSize);
                }

                // ✅ corrige si le backend a inversé tailles et couleurs
                if (isMostlyColorList(sizesList) && isMostlySizeList(colorsList)) {
                    const tmp = sizesList;
                    sizesList = colorsList;
                    colorsList = tmp;
                }

                // ✅ ne dérive les axes depuis variants QUE si les 2 sont vides
                if (!sizesList.length && !colorsList.length) {
                    sizesList = deriveAxis1FromVariants(variants);
                    colorsList = deriveAxis2FromVariants(variants);
                }

                // ✅ CAS SPÉCIAL : si axe1 est numérique (32/40/43) et axe2 = Unique,
                // alors c'est un produit à axe unique "taille écran"
                const axis1LooksLikeScreenSize =
                    sizesList.length > 0 &&
                    sizesList.every(looksLikeSize);

                const axis2OnlyUnique =
                    colorsList.length === 1 &&
                    String(colorsList[0] || "").trim().toLowerCase() === "unique";

                if (axis1LooksLikeScreenSize && axis2OnlyUnique) {
                    colorsList = [];
                }
                // ✅ détecte axe unique (ex: Blanc / Unique)
                const isSingleAxis =
                    sizesList.length > 0 &&
                    colorsList.length === 1 &&
                    String(colorsList[0]).toLowerCase() === "unique";

                if (isSingleAxis) {
                    colorsList = [];
                }
                // ✅ détecte si la seule "couleur" est Unique
                const rawColorsList = [...colorsList];
                const onlyUniqueColor =
                    rawColorsList.length === 1 &&
                    String(rawColorsList[0]).trim().toLowerCase() === "unique";

                // ✅ ne pas afficher "Unique" dans l'UI
                sizesList = sizesList.filter(x => String(x).trim().toLowerCase() !== "unique");
                colorsList = colorsList.filter(x => String(x).trim().toLowerCase() !== "unique");

                // ✅ si couleur technique = Unique, on la sélectionne automatiquement
                if (onlyUniqueColor) {
                    selectedColor = "Unique";
                }

                // ✅ titres dynamiques
                const axis1Title =
                    axis1LooksLikeScreenSize ? "Tailles écran disponibles" : detectVariantLabel(sizesList, 1);

                const axis2Title =
                    (onlyUniqueColor || axis2OnlyUnique) ? "" : detectVariantLabel(colorsList, 2);


                axis1Label = (axis1Title || "option").replace(" disponibles", "").toLowerCase();
                axis2Label = (axis2Title || "option").replace(" disponibles", "").toLowerCase();

                // ✅ détection axes
                const axis1IsColor = axis1Title === "Couleurs disponibles";
                const axis2IsColor = axis2Title === "Couleurs disponibles";

                // RESET UI
const sizesRow = document.getElementById("sizesRow");
const colorsRow = document.getElementById("colorsRow");

selectedSize = "";
if (!onlyUniqueColor) selectedColor = "";

// ✅ CAS A : produit couleur seule
if ((axis1IsColor && !colorsList.length) || (sizesList.length > 0 && !colorsList.length && axis1Title === "Couleurs disponibles")) {
    renderBadges("sizesRow", "Couleurs disponibles", sizesList, "color");

    if (colorsRow) {
        colorsRow.style.display = "none";
        colorsRow.innerHTML = "";
    }

    selectedColor = sizesList[0] || "";
    selectedSize = "";

    requestAnimationFrame(() => {
        document.querySelector("#sizesRow .badge")?.classList.add("active");
        validateVariants();
    });
}

// ✅ CAS B : axe1 = taille, axe2 = couleur
else if (sizesList.length > 0 && colorsList.length > 0) {
    renderBadges("sizesRow", axis1Title || "Options disponibles", sizesList, "size");
    renderBadges("colorsRow", axis2Title || "Couleurs disponibles", colorsList, "color");

    selectedSize = sizesList[0] || "";
    selectedColor = colorsList[0] || "";

    requestAnimationFrame(() => {
        document.querySelector("#sizesRow .badge")?.classList.add("active");
        document.querySelector("#colorsRow .badge")?.classList.add("active");
        validateVariants();
    });
}

// ✅ CAS C : taille seule
else if (sizesList.length > 0 && !colorsList.length) {
    renderBadges("sizesRow", axis1Title || "Options disponibles", sizesList, "size");

    if (colorsRow) {
        colorsRow.style.display = "none";
        colorsRow.innerHTML = "";
    }

    selectedSize = sizesList[0] || "";
    selectedColor = "";

    requestAnimationFrame(() => {
        document.querySelector("#sizesRow .badge")?.classList.add("active");
        validateVariants();
    });
}

// ✅ CAS D : couleur seule stockée dans colorsList
else if (!sizesList.length && colorsList.length) {
    renderBadges("sizesRow", axis2Title || "Couleurs disponibles", colorsList, "color");

    if (colorsRow) {
        colorsRow.style.display = "none";
        colorsRow.innerHTML = "";
    }

    selectedColor = colorsList[0] || "";
    selectedSize = "";

    requestAnimationFrame(() => {
        document.querySelector("#sizesRow .badge")?.classList.add("active");
        validateVariants();
    });
}

// ✅ fallback
else {
    if (sizesRow) {
        sizesRow.style.display = "none";
        sizesRow.innerHTML = "";
    }

    if (colorsRow) {
        colorsRow.style.display = "none";
        colorsRow.innerHTML = "";
    }

    validateVariants();
}
                // ✅ normalise champs (NOUVEAUX CHAMPS)
                const name = product.name || product.titre || "Produit";
                const price = Number(product?.price ?? product?.Price ?? product?.prix ?? 0);
                const pricePromo = Number(product?.pricePromo ?? product?.PricePromo ?? 0);
                const s = Number(product?.stock ?? product?.Stock ?? 0);

                console.log("DETAIL PRODUCT FINAL:", product);
                console.log("DETAIL PRICE USED:", price);
                console.log("DETAIL PROMO USED:", pricePromo);
                console.log("DETAIL STOCK USED:", s);


                const shortDesc = product.shortDescription ?? product.ShortDescription ?? "";
                const descCompat = product.description ?? product.Description ?? ""; // compat
                const longDesc = product.longDescription ?? product.LongDescription ?? "";

                const brand = product.brand ?? product.Brand ?? "";
                const sku = product.sku ?? product.Sku ?? "";
                const weightKg = product.weightKg ?? product.WeightKg ?? null;
                const dimensions = product.dimensions ?? product.Dimensions ?? "";
                const highlightsRaw = product.highlights ?? product.Highlights ?? "";


                // ✅ UI
                document.getElementById("name").textContent = name;

                // promo
                // promo (PRO + %)
                const priceEl = document.getElementById("price");
                const promoEl = document.getElementById("promo");

                if (pricePromo && Number(pricePromo) > 0 && Number(pricePromo) < price) {
                    priceEl.textContent = fmtMoney(pricePromo);

                    const pct = Math.round((1 - (Number(pricePromo) / Number(price))) * 100);

                    promoEl.innerHTML = `
                <div class="priceRow">
                  <div class="priceOld">${fmtMoney(price)}</div>
                  <span class="promoBadge">-${pct}%</span>
                </div>
              `;
                } else {
                    priceEl.textContent = fmtMoney(price);
                    promoEl.innerHTML = "";
                }


                // champs meta
                document.getElementById("brand").textContent = brand ? ("Marque: " + brand) : "";
                document.getElementById("sku").textContent = sku ? ("SKU: " + sku) : "";
                document.getElementById("dims").textContent = dimensions ? ("Dimensions: " + dimensions) : "";
                document.getElementById("weight").textContent =
                    (weightKg != null && weightKg !== "") ? ("Poids: " + weightKg + " kg") : "";





                function renderBadges(containerId, title, values, kind) {
                    const el = document.getElementById(containerId);
                    if (!el) return;

                    if (!values.length) {
                        el.style.display = "none";
                        el.innerHTML = "";
                        return;
                    }

                    el.style.display = "block";
                    el.innerHTML = `
            <div class="muted" style="font-weight:700;margin-bottom:6px">${escapeHtml(title)}</div>
            <div class="badges" data-kind="${kind}">
              ${values.map(v => `
                <button type="button" class="badge" data-val="${escapeHtml(v)}">${escapeHtml(v)}</button>
              `).join("")}
            </div>
        `;

                    const wrap = el.querySelector(".badges");

                    wrap.querySelectorAll("button.badge").forEach(b => {
                        b.addEventListener("click", () => {
                            wrap.querySelectorAll("button.badge").forEach(x => x.classList.remove("active"));
                            b.classList.add("active");

                            const val = b.dataset.val || "";

                            if (kind === "size") {
                                selectedSize = val;
                            }

                            if (kind === "color") {
                                selectedColor = val;
                            }

                            console.log("CLICK VARIANT =>", { kind, val, selectedSize, selectedColor, variants });

                            validateVariants();
                        });
                    });
                }





                // descriptions
                const shortBox = document.getElementById("shortDescBox");
                const shortEl = document.getElementById("shortDesc");

                if (shortDesc) {
                    shortEl.textContent = shortDesc;
                    shortBox.style.display = "block";
                } else {
                    shortBox.style.display = "none";
                }

                // ✅ Description longue (priorité au longDesc, sinon description “compat”)
                const longEl = document.getElementById("longDesc");
                if (longEl) longEl.textContent = (longDesc || descCompat || "—");


                // highlights -> liste
                const ul = document.getElementById("highlights");
                ul.innerHTML = "";
                const lines = String(highlightsRaw || "")
                    .split(/\r?\n|•/g)
                    .map(x => x.trim())
                    .filter(Boolean);

                ul.innerHTML = lines.length
                    ? lines.map(x => `<li>${escapeHtml(x)}</li>`).join("")
                    : `<li class="muted">—</li>`;

                err.textContent = "";


                // image
                const img = pickImage(product);
                const im = document.getElementById("mainImg");
                if (img) {
                    im.src = img;
                    im.style.display = "";
                    im.onerror = () => im.style.display = "none";
                } else {
                    im.style.display = "none";
                }


                // ✅ afficher la fiche tout de suite
                content.style.display = "block";

                // ✅ mémoriser le produit vu
                pushRecent(product);

                // ✅ charger le reste sans bloquer l'affichage
                loadDeferredSections(product, id, img);

                // stock + quantité
                // stock + quantité (par défaut)
                qtyInput.value = 1;

                if (variants.length > 0) {
                    const total = totalStockFromVariants(variants);
                    document.getElementById("stock").textContent = total > 0 ? ("✅ Stock total : " + total) : "⛔ Stock épuisé";
                    btn.disabled = true;
                    qtyInput.disabled = true;
                    document.getElementById("msg").textContent = "";
                }
                else {
                    // ✅ produit sans variantes -> stock global
                    qtyInput.max = String(Math.max(1, s));

                    if (s <= 0) {
                        document.getElementById("stock").textContent = "⛔ Stock épuisé";
                        btn.disabled = true;
                        btn.textContent = "⛔ Stock épuisé";
                        qtyInput.disabled = true;
                        document.getElementById("msg").textContent = "Ce produit est actuellement indisponible.";
                    } else {
                        document.getElementById("stock").textContent = "✅ Stock : " + s;
                        btn.disabled = false;
                        btn.textContent = "➕ Ajouter au panier";
                        qtyInput.disabled = false;
                        document.getElementById("msg").textContent = "";
                    }
                }

                // déjà panier
                const key = (selectedSize || selectedColor) ? `${selectedSize}|${selectedColor}` : "";
                if (window.isInCart && window.isInCart(product.id, key)) {
						lockBtn(btn, "✅ Déjà dans le panier");
						document.getElementById("msg").textContent = "Ce produit est déjà dans votre panier.";
						}
						console.log("DEBUG VARIANTS", {
						variants,
						sizesList,
						colorsList,
						selectedSize,
						selectedColor,
						axis1Label,
						axis2Label
						});
						validateVariants();




						// ✅ AUTO OUVERTURE MODAL AVIS (APRES LOAD)
                // AUTO OUVERTURE MODAL AVIS
                try {
                    const qs = new URLSearchParams(location.search);
                    if (qs.get("review") === "1") {

                        const token = getClientToken();

                        if (!token) {
                            const returnUrl = location.pathname + location.search;
                            location.href = "/client-login.html?returnUrl=" + encodeURIComponent(returnUrl);
                            return;
                        }

                        const state = await canReviewProduct(product.id);

                        if (state?.canReview === true) {
                            setTimeout(() => openRvModal(), 300);
                        } else {
                            setTimeout(() => {
                                openRvModal();
                                document.getElementById("rvMsg").textContent =
                                    "⚠️ Vous devez avoir acheté ce produit pour laisser un avis.";
                            }, 300);
                        }

                        const url = new URL(location.href);
                        url.searchParams.delete("review");
                        history.replaceState({}, "", url.toString());
                    }

                } catch (e) {
                    err.textContent = "❌ " + (e?.message || e);
                    content.style.display = "none";
                }

            } catch (e) {
                err.textContent = "❌ " + (e?.message || e);
                content.style.display = "none";
            }
        }
                            function getCartCountSafe() {
                                try {
                                    if (typeof window.cartTotals === "function") {
                                        return Number(cartTotals().count || 0);
                                    }

                                    const raw = JSON.parse(localStorage.getItem("ranita_cart") || "[]");
                                    return Array.isArray(raw)
                                        ? raw.reduce((sum, x) => sum + Number(x.qty || 0), 0)
                                        : 0;
                                } catch {
                                    return 0;
                                }
                            }

						function updateStickyCart() {
						const countEl = document.getElementById("stickyCartCount");
						if (countEl) countEl.textContent = String(getCartCountSafe());
						}

						let stickyCartTimer = null;

						function showStickyCart() {
						const el = document.getElementById("stickyCart");
						if (!el) return;

						updateStickyCart();
						el.classList.add("show");

						if (stickyCartTimer) clearTimeout(stickyCartTimer);
						}

						function hideStickyCart() {
						const el = document.getElementById("stickyCart");
						if (!el) return;
						el.classList.remove("show");
						}



                document.addEventListener("DOMContentLoaded", () => {
                    console.log("LOAD PRODUCT RUN", new Date().toISOString());
                    load();

                    window.addEventListener("pageshow", () => {
                        load();
                    });

                    window.addEventListener("focus", () => {
                        load();
                    });

                    document.addEventListener("visibilitychange", () => {
                        if (!document.hidden) {
                            load();
                        }
                    });


              

                    const btn = document.getElementById("btnAdd");
                    const qtyInput = document.getElementById("qty");

                    const mainImg = document.getElementById("mainImg");

                    let zoomed = false;
                    mainImg?.addEventListener("click", () => {
                        zoomed = !zoomed;
                        mainImg.style.transform = zoomed ? "scale(1.8)" : "scale(1)";
                        mainImg.style.cursor = zoomed ? "zoom-out" : "zoom-in";
                    });

                    const zoomBox = document.querySelector(".img-zoom-box");
                    const img = document.getElementById("mainImg");

                    if (zoomBox && img) {
                        zoomBox.addEventListener("mousemove", (e) => {
                            const r = zoomBox.getBoundingClientRect();
                            const x = (e.clientX - r.left) / r.width;
                            const y = (e.clientY - r.top) / r.height;

                            const px = Math.max(0, Math.min(1, x)) * 100;
                            const py = Math.max(0, Math.min(1, y)) * 100;

                            img.style.transformOrigin = `${px}% ${py}%`;
                            img.style.transform = "scale(1.8)";
                            img.style.cursor = "zoom-out";
                        });

                        zoomBox.addEventListener("mouseleave", () => {
                            img.style.transform = "scale(1)";
                            img.style.transformOrigin = "center center";
                            img.style.cursor = "zoom-in";
                        });
                    }

                    document.getElementById("stickyCartGo")?.addEventListener("click", () => {
                        location.href = "/cart.html";
                    });

                    document.getElementById("stickyCartClear")?.addEventListener("click", () => {
                        if (confirm("Vider le panier ?")) {
                            localStorage.removeItem("ranita_cart");
                            if (window.updateCartBadge) updateCartBadge();
                            refreshCount();
                            updateStickyCart();
                            hideStickyCart();
                        }
                    });

                    updateStickyCart();
                    if (window.updateCartBadge) updateCartBadge();

                    btn?.addEventListener("click", () => {
                        if (!product) return;

                        const msgEl = document.getElementById("msg");
                        const qty = Math.max(1, Number(qtyInput?.value || 1));

                        if (variants.length > 0) {
                            const v = findSelectedVariant();

                            if (!v) {
                                if (msgEl) msgEl.textContent = "⚠️ Sélectionnez une option.";
                                return;
                            }

                            const stock = Number(v.stock ?? v.Stock ?? 0);

                            if (stock <= 0) {
                                if (msgEl) msgEl.textContent = "⛔ Stock épuisé.";
                                return;
                            }

                            if (qty > stock) {
                                if (msgEl) msgEl.textContent = `⛔ Stock disponible : ${stock}`;
                                return;
                            }

                            const basePrice = Number(product.price ?? product.prix ?? 0);
                            const promo = Number(product.pricePromo ?? product.PricePromo ?? 0);
                            const effectivePrice = (promo > 0 && promo < basePrice) ? promo : basePrice;

                            const r = window.addToCartOnce(product, {
                                qty,
                                variantId: Number(v.id || 0),
                                variantLabel: v.label || `${v.key1 || v.size || ""} ${v.key2 || v.color || ""}`.trim(),
                                price: Number(v.price ?? effectivePrice),
                                variantStock: stock
                            });

                            if (r?.added) {
                                refreshCount();
                                updateStickyCart();
                                showStickyCart();
                                if (window.updateCartBadge) updateCartBadge();
                                lockBtn(btn, "✅ Ajouté");
                                if (msgEl) msgEl.textContent = "✅ Ajouté au panier.";
                            }

                            return;
                        }

                        const stockSimple = Number(product?.stock ?? 0);

                        if (stockSimple <= 0) {
                            if (msgEl) msgEl.textContent = "⛔ Stock épuisé.";
                            return;
                        }

                        if (qty > stockSimple) {
                            if (msgEl) msgEl.textContent = `⛔ Stock disponible : ${stockSimple}`;
                            return;
                        }

                        const basePrice = Number(product.price ?? product.prix ?? 0);
                        const promo = Number(product.pricePromo ?? product.PricePromo ?? 0);
                        const effectivePrice = (promo > 0 && promo < basePrice) ? promo : basePrice;

                        const r = window.addToCartOnce(product, {
                            qty,
                            variantId: 0,
                            variantLabel: "",
                            price: effectivePrice
                        });

                        if (r?.added) {
                            refreshCount();
                            updateStickyCart();
                            showStickyCart();
                            if (window.updateCartBadge) updateCartBadge();
                            lockBtn(btn, "✅ Ajouté");
                            if (msgEl) msgEl.textContent = "✅ Ajouté au panier.";
                        }
                    });

                    document.getElementById("rvSort")?.addEventListener("change", async (e) => {
                        rvSort = e.target.value;
                        await loadReviews(true);
                    });

                    document.getElementById("rvMore")?.addEventListener("click", async () => {
                        rvPage++;
                        await loadReviews(false);
                    });

                    document.getElementById("btnWriteReview")?.addEventListener("click", openRvModal);
                    document.getElementById("rvClose")?.addEventListener("click", closeRvModal);
                    document.getElementById("rvSubmit")?.addEventListener("click", submitReview);

                    document.getElementById("rvModal")?.addEventListener("click", (e) => {
                        if (e.target === document.getElementById("rvModal")) closeRvModal();
                    });
                });

                // ✅ rendre les fonctions accessibles au HTML
                window.submitReview = submitReview;
                window.openRvModal = openRvModal;
                window.closeRvModal = closeRvModal;

