document.addEventListener('DOMContentLoaded', function () {
    // ── Ջնջել շտեմարանից (արդեն ուղարկվածները մնում են) ───────────────────────
    document.querySelectorAll('[data-bank-delete]').forEach(function (btn) {
        btn.addEventListener('click', async function () {
            if (!confirm(btn.getAttribute('data-confirm-text'))) return;

            btn.disabled = true;
            try {
                const res = await fetch('/Repository/Assignments/Bank/Delete', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ id: parseInt(btn.getAttribute('data-assignment-id'), 10) })
                });
                if (res.ok) {
                    const row = btn.closest('.bank-row');
                    if (row) row.remove();
                    refreshCounts();
                    return;
                }
            } catch (e) { }
            btn.disabled = false;
        });
    });

    // refreshCounts՝ ֆիլտրից հետո և ջնջելուց հետո
    function refreshCounts() {
        const count = Array.from(document.querySelectorAll('.bank-row'))
            .filter(function (el) { return el.style.display !== 'none'; }).length;
        const el = document.getElementById('bankCount');
        if (el) el.textContent = count;
    }

    EduFilterBar.init({ items: '.bank-row', onApply: refreshCounts });
});