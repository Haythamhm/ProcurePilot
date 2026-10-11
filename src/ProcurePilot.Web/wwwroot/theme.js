// ProcurePilot Theme & Shell Manager
window.ProcurePilotTheme = {
    getTheme: function () {
        return document.documentElement.getAttribute('data-theme') || 'light';
    },
    setTheme: function (theme) {
        var targetTheme = theme === 'dark' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', targetTheme);
        try {
            localStorage.setItem('procurepilot-theme', targetTheme);
        } catch (e) { }
        window.dispatchEvent(new CustomEvent('procurepilot-theme-changed', { detail: { theme: targetTheme } }));
        return targetTheme;
    },
    toggleTheme: function () {
        var current = window.ProcurePilotTheme.getTheme();
        var next = current === 'dark' ? 'light' : 'dark';
        return window.ProcurePilotTheme.setTheme(next);
    },
    getSidebarCollapsed: function () {
        try {
            return localStorage.getItem('procurepilot-sidebar-collapsed') === 'true';
        } catch (e) {
            return false;
        }
    },
    setSidebarCollapsed: function (collapsed) {
        try {
            localStorage.setItem('procurepilot-sidebar-collapsed', collapsed ? 'true' : 'false');
        } catch (e) { }
    }
};
