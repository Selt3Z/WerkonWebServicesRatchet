window.ratchetAuth = {
    setSession: function (sessionId) {
        const maxAge = 60 * 60 * 24 * 14;
        document.cookie = "Ratchet.Web.Session=" + encodeURIComponent(sessionId) + "; path=/; max-age=" + maxAge + "; SameSite=Lax";
    },
    clearSession: function () {
        document.cookie = "Ratchet.Web.Session=; path=/; max-age=0; SameSite=Lax";
    }
};
