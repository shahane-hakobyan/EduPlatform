document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.assignment-tab-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.assignment-tab-btn').forEach(function (b) { b.classList.remove('active'); });
            document.querySelectorAll('.assignment-tab-pane').forEach(function (p) {
                p.classList.remove('active');
                p.style.display = 'none';
            });

            btn.classList.add('active');
            const target = document.getElementById(btn.getAttribute('data-tab-target'));
            if (target) {
                target.classList.add('active');
                target.style.display = 'block';
            }
        });
    });

    document.querySelectorAll('.assignment-archive-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const confirmText = btn.getAttribute('data-confirm-text') || 'Archive this assignment?';
            if (!confirm(confirmText)) return;
            const card = btn.closest('.assignment-card');
            if (card) card.remove();
        });
    });
});