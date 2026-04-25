(async () => {
    const box = document.getElementById("homeCategories");
    if (!box) return;

    try {
        const res = await fetch("/api/categories");
        const json = await res.json();

        if (!json.ok) {
            box.innerHTML = "<p>Erreur chargement catégories</p>";
            return;
        }

        const items = json.items || [];

        if (items.length === 0) {
            box.innerHTML = "<p>Aucune catégorie</p>";
            return;
        }

        box.innerHTML = items.map(c => `
      <a class="cat-card" href="/products.html?category=${encodeURIComponent(c.slug)}">
        <div class="cat-name">${c.name}</div>
      </a>
    `).join("");

    } catch (err) {
        box.innerHTML = "<p>Impossible de charger les catégories</p>";
        console.error(err);
    }
})();
