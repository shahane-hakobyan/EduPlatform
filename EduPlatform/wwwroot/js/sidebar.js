document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('sidebar');
    const toggle = document.getElementById('sidebarToggle');
    if (!sidebar || !toggle) return;

    // Restore collapsed/expanded state from the last visit
    if (localStorage.getItem('sidebarCollapsed') === 'true') {
        sidebar.classList.add('collapsed');
    }

    // Enable the width transition only after the initial state above is
    // applied, so restoring from localStorage doesn't visibly animate —
    // it snaps directly to the right width, then future clicks animate normally.
    requestAnimationFrame(function () {
        sidebar.classList.add('sidebar-transitions-enabled');
    });

    // Collapse/expand button
    toggle.addEventListener('click', function () {
        sidebar.classList.toggle('collapsed');
        const isNowCollapsed = sidebar.classList.contains('collapsed');
        localStorage.setItem('sidebarCollapsed', isNowCollapsed);

        if (isNowCollapsed) {
            // Collapsing: close any open submenu so it doesn't stay stuck open
            document.querySelectorAll('.sidebar-item.open').forEach(function (item) {
                item.classList.remove('open');
            });
        } else {
            // Expanding: restore the active section's submenu being open,
            // same as what the server originally rendered on page load.
            document.querySelectorAll('.sidebar-item').forEach(function (item) {
                if (item.querySelector('.sidebar-submenu a.active')) {
                    item.classList.add('open');
                }
            });
        }
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