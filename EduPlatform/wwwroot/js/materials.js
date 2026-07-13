document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        new bootstrap.Tooltip(el);
    });

    const preview = document.getElementById('materialImagePreview');
    if (!preview) return;

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
});