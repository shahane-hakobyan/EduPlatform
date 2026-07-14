document.addEventListener('DOMContentLoaded', function () {
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

    const gridBtn = document.querySelector('.materials-view-btn[data-view="grid"]');
    const listBtn = document.querySelector('.materials-view-btn[data-view="list"]');
    const gridView = document.querySelector('.materials-grid');
    const listView = document.querySelector('.materials-list-view');

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

    document.querySelectorAll('.material-card-delete-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const confirmText = btn.getAttribute('data-confirm-text') || 'Delete this material?';
            if (!confirm(confirmText)) return;

            // TODO: replace with a real POST to /Teacher/Materials/Delete/{id} once
            // the "saved to my page" backend exists — for now this just removes
            // the card/row from the current view without persisting anything.
            const card = btn.closest('.material-card-wide') || btn.closest('tr');
            if (card) card.remove();
        });
    });
});