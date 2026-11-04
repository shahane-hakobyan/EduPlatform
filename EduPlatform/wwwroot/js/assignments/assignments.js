document.addEventListener('DOMContentLoaded', function () {
    // ── Ընթացիկ / Արխիվացված ներդիրներ ───────────────────────────────────────
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

    // ── Արխիվացնել՝ իրական (բազայում) ─────────────────────────────────────────
    document.querySelectorAll('.assignment-archive-btn').forEach(function (btn) {
        btn.addEventListener('click', async function () {
            const confirmText = btn.getAttribute('data-confirm-text') || 'Archive this assignment?';
            if (!confirm(confirmText)) return;

            btn.disabled = true;
            try {
                const res = await fetch('/Repository/Assignments/Archive', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ id: parseInt(btn.getAttribute('data-assignment-id'), 10) })
                });
                if (res.ok) {
                    window.location.reload();   // առաջադրանքը անցնում է «Արխիվացված» ներդիր
                    return;
                }
            } catch (e) { }
            btn.disabled = false;
        });
    });

    // ── Թվերի թարմացում ֆիլտրից հետո ──────────────────────────────────────────
    function visibleCount(paneId) {
        const pane = document.getElementById(paneId);
        if (!pane) return 0;
        return Array.from(pane.querySelectorAll('.assignment-card'))
            .filter(function (el) { return el.style.display !== 'none'; }).length;
    }

    function refreshCounts() {
        const active = visibleCount('assignments-tab-active');
        const archived = visibleCount('assignments-tab-archived');

        const tabCounts = document.querySelectorAll('.assignment-tab-count');
        if (tabCounts[0]) tabCounts[0].textContent = active;
        if (tabCounts[1]) tabCounts[1].textContent = archived;

        const activeEl = document.getElementById('assignmentsActiveCount');
        const archivedEl = document.getElementById('assignmentsArchivedCount');
        if (activeEl) activeEl.textContent = active;
        if (archivedEl) archivedEl.textContent = archived;
    }

    EduFilterBar.init({ items: '.assignment-card', onApply: refreshCounts });
});