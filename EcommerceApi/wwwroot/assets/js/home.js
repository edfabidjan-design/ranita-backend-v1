// JavaScript source code
(function () {
    "use strict";

    let heroTimer = null;
    let heroCurrent = 0;
    const heroDelay = 5000;

    function resolveImg(url) {
        const fallback = "/assets/img/placeholder.jpg";
        if (!url) return fallback;
        if (url.startsWith("http")) return url;
        return url.startsWith("/") ? url : ("/" + url);
    }

    function escapeHtml(str) {
        return String(str ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function applyHighlight(title, highlight, accentColor) {
        const t = String(title || "");
        const h = String(highlight || "").trim();
        const color = accentColor || "#22c55e";

        if (!h) return escapeHtml(t);

        const lowerT = t.toLowerCase();
        const lowerH = h.toLowerCase();
        const idx = lowerT.indexOf(lowerH);

        if (idx === -1) return escapeHtml(t);

        const before = escapeHtml(t.slice(0, idx));
        const middle = escapeHtml(t.slice(idx, idx + h.length));
        const after = escapeHtml(t.slice(idx + h.length));

        return `${before}<span style="color:${escapeHtml(color)}">${middle}</span>${after}`;
    }

    function getHeroEls() {
        return {
            slider: document.getElementById("heroSlider"),
            prev: document.getElementById("heroPrev"),
            next: document.getElementById("heroNext"),
            dotsRoot: document.getElementById("heroDots")
        };
    }

    function getSlides() {
        return Array.from(document.querySelectorAll("#heroSlider .heroSlide"));
    }

    function getDots() {
        return Array.from(document.querySelectorAll("#heroDots .heroDot"));
    }

    function stopHeroAuto() {
        if (heroTimer) {
            clearInterval(heroTimer);
            heroTimer = null;
        }
    }

    function showHeroSlide(index) {
        const slides = getSlides();
        const dots = getDots();

        if (!slides.length) return;

        heroCurrent = (index + slides.length) % slides.length;

        slides.forEach((slide, i) => {
            slide.classList.toggle("active", i === heroCurrent);
        });

        dots.forEach((dot, i) => {
            dot.classList.toggle("active", i === heroCurrent);
        });
    }

    function nextHeroSlide() {
        const slides = getSlides();
        if (slides.length <= 1) return;
        showHeroSlide(heroCurrent + 1);
    }

    function prevHeroSlide() {
        const slides = getSlides();
        if (slides.length <= 1) return;
        showHeroSlide(heroCurrent - 1);
    }

    function startHeroAuto() {
        const slides = getSlides();

        stopHeroAuto();

        if (slides.length <= 1) return;

        heroTimer = setInterval(() => {
            nextHeroSlide();
        }, heroDelay);
    }

    async function loadHeroSlider() {
        const { slider, dotsRoot, prev, next } = getHeroEls();
        if (!slider || !dotsRoot) return;

        try {
            const r = await fetch("/api/hero-slides/active", { cache: "no-store" });
            const slides = await r.json();

            if (!r.ok) {
                throw new Error("Impossible de charger les slides.");
            }

            slider.innerHTML = "";
            dotsRoot.innerHTML = "";
            stopHeroAuto();
            heroCurrent = 0;

            if (!Array.isArray(slides) || slides.length === 0) {
                slider.innerHTML = `
                    <article class="heroSlide active">
                        <div class="heroSlideBg" style="
                            background-color:#08111f;
                            background-image:
                                radial-gradient(900px 420px at 10% 20%, rgba(34,197,94,.12), transparent 55%),
                                radial-gradient(900px 420px at 90% 10%, rgba(96,165,250,.12), transparent 55%);
                        ">
                            <div class="heroIn">
                                <div>
                                    <div class="heroBadgeRow">
                                        <span class="heroBadge">✨ Ranita</span>
                                    </div>

                                    <h1>Bienvenue sur <span style="color:#22c55e;">Ranita</span></h1>

                                    <p>
                                        Découvrez des produits tendance, des promotions, des nouveautés
                                        et une expérience marketplace moderne.
                                    </p>

                                    <div class="heroActions">
                                        <a class="btn btnPri" href="/products.html">Voir les produits</a>
                                        <a class="btn btnGhost" href="/vendor-register.html">Devenir vendeur</a>
                                    </div>
                                </div>

                                <div class="heroSide">
                                    <div class="heroPromoCard">
                                        <div style="font-size:13px;color:#86efac;font-weight:900;">Découverte</div>
                                        <div class="heroSideTitle">
                                            Explorez les catégories et offres Ranita
                                        </div>
                                        <div class="muted heroPromoText">
                                            Ajoutez vos slides dans l’admin pour piloter ici le contenu marketing complet.
                                        </div>
                                        <a class="btn btnPri" href="/products.html" style="margin-top:16px;">Découvrir</a>
                                    </div>

                                    <div class="heroStatCard">
                                        <div style="font-weight:900;margin-bottom:10px;">Pourquoi acheter sur Ranita ?</div>
                                        <div class="statGrid">
                                            <div class="miniStat">
                                                <div class="num heroProductsCount">0+</div>
                                                <div class="lab">Produits</div>
                                            </div>
                                            <div class="miniStat">
                                                <div class="num heroCategoriesCount">0+</div>
                                                <div class="lab">Catégories</div>
                                            </div>
                                            <div class="miniStat">
                                                <div class="num">24/7</div>
                                                <div class="lab">Boutique en ligne</div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </article>
                `;

                if (prev) prev.style.display = "none";
                if (next) next.style.display = "none";
                return;
            }

            if (prev) prev.style.display = slides.length > 1 ? "" : "none";
            if (next) next.style.display = slides.length > 1 ? "" : "none";

            slides.forEach((s, i) => {
                const accent = s.accentColor || "#22c55e";

                const themeText =
                    s.theme === "brand" ? "✨ Marque" :
                    s.theme === "promo" ? "🔥 Promo" :
                    s.theme === "new" ? "🆕 Nouveau" :
                    s.theme === "category" ? "🛍️ Catégorie" :
                    (s.theme || "");

                const bgStyle = s.imageUrl
                    ? `
                        background-color:#08111f;
                        background-image:
                            linear-gradient(90deg, rgba(5,10,20,.12) 0%, rgba(5,10,20,.06) 28%, rgba(5,10,20,.02) 50%, rgba(5,10,20,0) 75%),
                            url('${escapeHtml(resolveImg(s.imageUrl))}');
                      `
                    : `
                        background-color:#08111f;
                        background-image:
                            radial-gradient(900px 420px at 10% 20%, rgba(34,197,94,.18), transparent 55%),
                            radial-gradient(900px 420px at 90% 10%, rgba(96,165,250,.16), transparent 55%);
                      `;

                const rightBadgeText = s.rightBadgeText || s.smallTag || "";
                const rightTitle = s.rightTitle || "";
                const rightSubtitle = s.rightSubtitle || "";
                const rightButtonText = s.rightButtonText || "";
                const rightButtonUrl = s.rightButtonUrl || "#";

                const statsTitle = s.statsTitle || "Pourquoi acheter sur Ranita ?";
                const stat1Value = s.stat1Value || "0+";
                const stat1Label = s.stat1Label || "Produits";
                const stat2Value = s.stat2Value || "0+";
                const stat2Label = s.stat2Label || "Catégories";
                const stat3Value = s.stat3Value || "24/7";
                const stat3Label = s.stat3Label || "Boutique en ligne";

                const slide = document.createElement("article");
                slide.className = "heroSlide" + (i === 0 ? " active" : "");

                slide.innerHTML = `
                    <div class="heroSlideBg" style="${bgStyle}">
                        <div class="heroIn">
                            <div>
                                <div class="heroBadgeRow">
                                    ${s.badgeText ? `<span class="heroBadge">${escapeHtml(s.badgeText)}</span>` : ""}
                                    ${themeText ? `<span class="heroBadge">${escapeHtml(themeText)}</span>` : ""}
                                </div>

                                <h1>${applyHighlight(s.title || "", s.highlightText || "", accent)}</h1>

                                <p>${escapeHtml(s.subtitle || "")}</p>

                                <div class="heroActions">
                                    ${s.primaryButtonText
                                        ? `<a class="btn btnPri" href="${escapeHtml(s.primaryButtonUrl || "#")}">${escapeHtml(s.primaryButtonText)}</a>`
                                        : ""}
                                    ${s.secondaryButtonText
                                        ? `<a class="btn btnGhost" href="${escapeHtml(s.secondaryButtonUrl || "#")}">${escapeHtml(s.secondaryButtonText)}</a>`
                                        : ""}
                                </div>
                            </div>

                            <div class="heroSide">
                                <div class="heroPromoCard">
                                    ${rightBadgeText
                                        ? `<div style="font-size:13px;color:${escapeHtml(accent)};font-weight:900;">${escapeHtml(rightBadgeText)}</div>`
                                        : ""}

                                    <div class="heroSideTitle">
                                        ${rightTitle
                                            ? applyHighlight(rightTitle, s.highlightText || "", accent)
                                            : "Découvrez Ranita"}
                                    </div>

                                    <div class="muted" style="margin-top:10px;">
                                        ${escapeHtml(rightSubtitle || "Explorez les meilleures offres, catégories et nouveautés du moment.")}
                                    </div>

                                    ${rightButtonText
                                        ? `<a class="btn btnPri heroPromoBtn" href="${escapeHtml(rightButtonUrl)}">${escapeHtml(rightButtonText)}</a>`
                                        : ""}
                                </div>

                                <div class="heroStatCard">
                                    <div class="heroStatsTitle">${escapeHtml(statsTitle)}</div>
                                    <div class="statGrid">
                                        <div class="miniStat">
                                            <div class="num ${stat1Value === "0+" ? "heroProductsCount" : ""}">${escapeHtml(stat1Value)}</div>
                                            <div class="lab">${escapeHtml(stat1Label)}</div>
                                        </div>
                                        <div class="miniStat">
                                            <div class="num ${stat2Value === "0+" ? "heroCategoriesCount" : ""}">${escapeHtml(stat2Value)}</div>
                                            <div class="lab">${escapeHtml(stat2Label)}</div>
                                        </div>
                                        <div class="miniStat">
                                            <div class="num">${escapeHtml(stat3Value)}</div>
                                            <div class="lab">${escapeHtml(stat3Label)}</div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                `;

                slider.appendChild(slide);

                const dot = document.createElement("button");
                dot.className = "heroDot" + (i === 0 ? " active" : "");
                dot.type = "button";
                dot.setAttribute("data-slide", i);
                dot.setAttribute("aria-label", `Aller au slide ${i + 1}`);
                dotsRoot.appendChild(dot);
            });
        } catch (e) {
            console.error("Erreur chargement hero slider:", e);

            slider.innerHTML = `
                <article class="heroSlide active">
                    <div class="heroSlideBg" style="
                        background-color:#08111f;
                        background-image:
                            radial-gradient(900px 420px at 10% 20%, rgba(239,68,68,.12), transparent 55%),
                            radial-gradient(900px 420px at 90% 10%, rgba(245,158,11,.10), transparent 55%);
                    ">
                        <div class="heroIn">
                            <div>
                                <div class="heroBadgeRow">
                                    <span class="heroBadge">⚠️ Erreur</span>
                                </div>
                                <h1>Le slider n’a pas pu être chargé</h1>
                                <p>Vérifiez l’endpoint public des slides actifs et les chemins des images.</p>
                                <div class="heroActions">
                                    <a class="btn btnPri" href="/products.html">Continuer</a>
                                </div>
                            </div>
                        </div>
                    </div>
                </article>
            `;

            dotsRoot.innerHTML = "";
            if (prev) prev.style.display = "none";
            if (next) next.style.display = "none";
        }
    }

    function initHeroSlider() {
        const { slider, prev, next, dotsRoot } = getHeroEls();
        if (!slider) return;

        prev?.removeEventListener("click", prevHeroSlide);
        next?.removeEventListener("click", nextHeroSlide);

        prev?.addEventListener("click", () => {
            prevHeroSlide();
            startHeroAuto();
        });

        next?.addEventListener("click", () => {
            nextHeroSlide();
            startHeroAuto();
        });

        dotsRoot?.addEventListener("click", (e) => {
            const btn = e.target.closest(".heroDot");
            if (!btn) return;

            const index = Number(btn.getAttribute("data-slide") || 0);
            showHeroSlide(index);
            startHeroAuto();
        });

        slider.addEventListener("mouseenter", stopHeroAuto);
        slider.addEventListener("mouseleave", startHeroAuto);
        slider.addEventListener("touchstart", stopHeroAuto, { passive: true });
        slider.addEventListener("touchend", startHeroAuto, { passive: true });

        document.addEventListener("visibilitychange", () => {
            if (document.hidden) stopHeroAuto();
            else startHeroAuto();
        });

        showHeroSlide(0);
        startHeroAuto();
    }

    window.loadHeroSlider = loadHeroSlider;
    window.initHeroSlider = initHeroSlider;
})();