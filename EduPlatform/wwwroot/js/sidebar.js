document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('sidebar');
    const toggle = document.getElementById('sidebarToggle');
    if (!sidebar || !toggle) return;

    // Restore collapsed/expanded state from the last visit
    if (localStorage.getItem('sidebarCollapsed') === 'true') {
        sidebar.classList.add('collapsed');
    }

    // Collapse/expand button
    toggle.addEventListener('click', function () {
        sidebar.classList.toggle('collapsed');
        localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));

        // Closing the sidebar should also close any open submenu,
        // so it doesn't reopen stuck in the wrong mode
        document.querySelectorAll('.sidebar-item.open').forEach(function (item) {
            item.classList.remove('open');
        });
    });

    // Click-to-toggle submenus (expanded state only — collapsed state uses CSS hover instead)
    document.querySelectorAll('.sidebar-item').forEach(function (item) {
        const submenu = item.querySelector('.sidebar-submenu');
        if (!submenu) return; // leaf items with no children — nothing to toggle

        const link = item.querySelector('.sidebar-link');
        link.addEventListener('click', function (e) {
            if (sidebar.classList.contains('collapsed')) return; // let hover handle collapsed state

            e.preventDefault(); // these parent items have no real page (Url is null), just toggle
            const wasOpen = item.classList.contains('open');

            // Optional: close any other open submenu first, so only one is open at a time
            document.querySelectorAll('.sidebar-item.open').forEach(function (openItem) {
                openItem.classList.remove('open');
            });

            if (!wasOpen) item.classList.add('open');
        });
    });
});