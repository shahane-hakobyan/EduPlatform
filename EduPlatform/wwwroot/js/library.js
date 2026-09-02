document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.library-tab-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.library-tab-btn').forEach(function (b) { b.classList.remove('active'); });
            document.querySelectorAll('.library-tab-pane').forEach(function (p) {
                p.classList.remove('active');
                p.style.display = 'none';
            });

            btn.classList.add('active');
            const target = document.getElementById(btn.getAttribute('data-tab-target'));
            if (target) {
                target.classList.add('active');
                target.style.display = 'block';
            }

            const countEl = document.getElementById('libraryTabCount');
            if (countEl) {
                countEl.textContent = btn.getAttribute('data-count') || '0';
            }
        });
    });

    document.querySelectorAll('.library-tab-pane').forEach(function (pane) {
        const gridBtn = pane.querySelector('.materials-view-btn[data-view="grid"]');
        const listBtn = pane.querySelector('.materials-view-btn[data-view="list"]');
        const gridView = pane.querySelector('.materials-grid');
        const listView = pane.querySelector('.materials-list-view');

        if (gridBtn && listBtn && gridView && listView) {
            gridBtn.addEventListener('click', function () {
                gridBtn.classList.add('active');
                listBtn.classList.remove('active');
                gridView.style.display = 'grid';
                listView.style.display = 'none';
            });

            listBtn.addEventListener('click', function () {
                listBtn.classList.add('active');
                gridBtn.classList.remove('active');
                gridView.style.display = 'none';
                listView.style.display = 'block';
            });
        }
    });

    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        new bootstrap.Tooltip(el);
    });

    const preview = document.getElementById('materialImagePreview');
    if (preview) {
        document.querySelectorAll('.material-card-wide-image[data-full-image]').forEach(function (el) {
            const url = el.getAttribute('data-full-image');
            if (!url) return;

            el.addEventListener('mouseenter', function () {
                preview.innerHTML = '<img src="' + url + '" alt="" />';
                preview.style.display = 'block';
            });
            el.addEventListener('mousemove', function (e) {
                preview.style.left = (e.clientX + 20) + 'px';
                preview.style.top = (e.clientY + 20) + 'px';
            });
            el.addEventListener('mouseleave', function () {
                preview.style.display = 'none';
            });
        });
    }

    document.querySelectorAll('.material-card-delete-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const confirmText = btn.getAttribute('data-confirm-text') || 'Delete this material?';
            if (!confirm(confirmText)) return;
            const card = btn.closest('.material-card-wide') || btn.closest('tr');
            if (card) card.remove();
        });
    });
});