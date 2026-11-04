document.addEventListener('DOMContentLoaded', function () {
    // Ջնջել խումբը՝ միայն ադմինին է երևում, սերվերը նույնպես ստուգում է
    const del = document.getElementById('gdDelete');
    if (!del) return;

    del.addEventListener('click', async function () {
        if (!confirm(del.getAttribute('data-confirm-text'))) return;

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
