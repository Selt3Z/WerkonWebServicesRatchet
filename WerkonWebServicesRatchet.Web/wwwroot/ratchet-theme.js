window.ratchetTheme = {
    set: function (theme) {
        var allowed = ['light', 'dark', 'gray', 'gray-retro', 'pink'];
        var normalized = allowed.indexOf(theme) >= 0 ? theme : 'light';
        document.documentElement.setAttribute('data-theme', normalized);
    },
    closeMobileNav: function () {
        var toggle = document.getElementById('nav-menu-toggle');
        if (toggle) {
            toggle.checked = false;
        }
    }
};
