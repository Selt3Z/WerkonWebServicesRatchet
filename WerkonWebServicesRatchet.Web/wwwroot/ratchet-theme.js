window.ratchetTheme = {
    set: function (theme) {
        var normalized = theme === 'dark' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', normalized);
    }
};
