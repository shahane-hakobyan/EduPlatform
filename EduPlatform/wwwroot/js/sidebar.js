document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('sidebar');
    const toggle = document.getElementById('sidebarToggle');
    if (!sidebar || !toggle) return;
    if (localStorage.getItem('sidebarCollapsed') === 'true') {
        sidebar.classList.add('collapsed');
    }

    requestAnimationFrame(function () {
        sidebar.classList.add('sidebar-transitions-enabled');
    });

    toggle.addEventListener('click', function () {
        sidebar.classList.toggle('collapsed');
        const isNowCollapsed = sidebar.classList.contains('collapsed');
        localStorage.setItem('sidebarCollapsed', isNowCollapsed);

        if (isNowCollapsed) {
            document.querySelectorAll('.sidebar-item.open').forEach(function (item) {
                item.classList.remove('open');
            });
        } else {
            document.querySelectorAll('.sidebar-item').forEach(function (item) {
                if (item.querySelector('.sidebar-submenu a.active')) {
                    item.classList.add('open');
                }
            });
        }
    });

    document.querySelectorAll('.sidebar-item').forEach(function (item) {
        const submenu = item.querySelector('.sidebar-submenu');
        if (!submenu) return; 

        const link = item.querySelector('.sidebar-link');
        link.addEventListener('click', function (e) {
            if (sidebar.classList.contains('collapsed')) return; 

            e.preventDefault();
            const wasOpen = item.classList.contains('open');
          
            document.querySelectorAll('.sidebar-item.open').forEach(function (openItem) {
                openItem.classList.remove('open');
            });

            if (!wasOpen) item.classList.add('open');
        });
    });
});