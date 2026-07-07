document.addEventListener('DOMContentLoaded', function () {
    const timeEl = document.getElementById('appClockTime');
    const dateEl = document.getElementById('appClockDate');
    if (!timeEl || !dateEl) return;

    function update() {
        const now = new Date();
        timeEl.textContent = now.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: true });
        dateEl.textContent = now.toLocaleDateString('en-GB').split('/').join('.');
    }

    update();
    setInterval(update, 30000); 
});