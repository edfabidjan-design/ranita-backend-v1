(() => {
    "use strict";

    function getClientToken() {
        return localStorage.getItem("ranita_client_token") || "";
    }

    window.requireClientAuth = function requireClientAuth() {
        const t = getClientToken();
        if (!t) {
            const returnUrl = encodeURIComponent(location.pathname + location.search);
            location.replace("/client-login.html?returnUrl=" + returnUrl);
            return false;
        }
        return true;
    };
})();
