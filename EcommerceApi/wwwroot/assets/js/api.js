// wwwroot/assets/js/api.js
(() => {
    "use strict";

    async function apiGetProducts(queryString = "") {
        const res = await window.fetchPublicJson("/api/products" + (queryString || ""));
        return res?.items ?? res?.data?.items ?? res?.data ?? res;
    }

    async function apiGetProduct(id) {
        const res = await window.fetchPublicJson("/api/products/" + encodeURIComponent(id));
        return res?.item ?? res?.data ?? res;
    }

    async function apiGetCategories() {
        return await window.fetchPublicJson("/api/categories");
    }

    async function apiCreateOrder(payload) {
        return await window.fetchPublicJson("/api/orders", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload || {})
        });
    }

    window.apiGetProducts = apiGetProducts;
    window.apiGetProduct = apiGetProduct;
    window.apiGetCategories = apiGetCategories;
    window.apiCreateOrder = apiCreateOrder;
})();