document.addEventListener('DOMContentLoaded', function () {
    const timeEl = document.getElementById('appClockTime');
    const dateEl = document.getElementById('appClockDate');
    if (timeEl && dateEl) {
        function update() {
            const now = new Date();
            timeEl.textContent = now.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: true });
            dateEl.textContent = now.toLocaleDateString('en-GB').split('/').join('.');
        }

        update();
        setInterval(update, 30000);
    }

    const themeBtn = document.getElementById('themeToggleBtn');
    if (themeBtn) {
        themeBtn.addEventListener('click', function () {
            const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
            if (isDark) {
                document.documentElement.removeAttribute('data-theme');
                localStorage.setItem('theme', 'light');
            } else {
                document.documentElement.setAttribute('data-theme', 'dark');
                localStorage.setItem('theme', 'dark');
            }
        });
    }
});