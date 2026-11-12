document.addEventListener('DOMContentLoaded', function () {
    // Երեք կետի մենյու՝ «Խմբագրել» / «Ջնջել» (միայն ադմինին է երևում, սերվերը նույնպես ստուգում է)
    const more = document.getElementById('gdMore');
    const btn = document.getElementById('gdMoreBtn');
    const menu = document.getElementById('gdMenu');
    if (!more || !btn || !menu) return;

    function setOpen(open) {
        menu.hidden = !open;
        btn.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    btn.addEventListener('click', function (e) { e.stopPropagation(); setOpen(menu.hidden); });
    document.addEventListener('click', function (e) { if (!more.contains(e.target)) setOpen(false); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') setOpen(false); });

    // Ջնջման հաստատման գեղեցիկ պատուհան (փոխարինում է confirm()-ին)
    function confirmDialog(message, okLabel, cancelLabel) {
        return new Promise(function (resolve) {
            const backdrop = document.createElement('div');
            backdrop.className = 'gd-dialog-backdrop';
            backdrop.innerHTML =
                '<div class="gd-dialog" role="alertdialog" aria-modal="true">' +
                '<div class="gd-dialog-icon"><i class="bi bi-trash"></i></div>' +
                '<p class="gd-dialog-text"></p>' +
                '<div class="gd-dialog-actions">' +
                '<button type="button" class="gd-dialog-cancel"></button>' +
                '<button type="button" class="gd-dialog-ok"></button>' +
                '</div></div>';
            backdrop.querySelector('.gd-dialog-text').textContent = message;
            backdrop.querySelector('.gd-dialog-cancel').textContent = cancelLabel;
            backdrop.querySelector('.gd-dialog-ok').textContent = okLabel;
            document.body.appendChild(backdrop);

            function close(result) {
                document.removeEventListener('keydown', onKey);
                backdrop.remove();
                resolve(result);
            }
            function onKey(e) { if (e.key === 'Escape') close(false); }

            document.addEventListener('keydown', onKey);
            backdrop.addEventListener('click', function (e) { if (e.target === backdrop) close(false); });
            backdrop.querySelector('.gd-dialog-cancel').addEventListener('click', function () { close(false); });
            backdrop.querySelector('.gd-dialog-ok').addEventListener('click', function () { close(true); });
            backdrop.querySelector('.gd-dialog-cancel').focus();
        });
    }

    const del = document.getElementById('gdDelete');
    del.addEventListener('click', async function () {
        setOpen(false);

        const ok = await confirmDialog(
            del.getAttribute('data-confirm-text'),
            del.getAttribute('data-ok-text'),
            del.getAttribute('data-cancel-text'));
        if (!ok) return;

        del.disabled = true;
        try {
            const res = await fetch('/Groups/Delete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: parseInt(del.getAttribute('data-group-id'), 10) })
            });
            if (res.ok) {
                window.location.href = del.getAttribute('data-redirect-url');
                return;
            }
        } catch (e) { }
        del.disabled = false;
    });
});
