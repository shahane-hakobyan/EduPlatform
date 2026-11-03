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

    // ── Որոնում և ֆիլտրեր (բրաուզերում) ───────────────────────────────────────
    const search = document.querySelector('.materials-search-input');
    const dropdowns = document.querySelectorAll('.materials-filter-dropdown');
    const applyBtn = document.querySelector('.materials-filter-apply-btn');
    const clearBtn = document.querySelector('.materials-filter-clear-btn');

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

    function selectedValues() {
        const result = {};
        dropdowns.forEach(function (dropdown) {
            const key = dropdown.getAttribute('data-filter-key');
            const values = Array.from(dropdown.querySelectorAll('.materials-filter-pill-input:checked'))
                .map(function (input) { return input.value; });
            if (values.length) result[key] = values;
        });
        return result;
    }

    function apply() {
        const q = (search ? search.value : '').trim().toLowerCase();
        const selected = selectedValues();

        document.querySelectorAll('.assignment-card').forEach(function (card) {
            let ok = !q || (card.getAttribute('data-f-search') || '').indexOf(q) !== -1;

            // նույն ֆիլտրի ներսում՝ «կամ», տարբեր ֆիլտրերի միջև՝ «և»
            Object.keys(selected).forEach(function (key) {
                if (!ok) return;
                const values = (card.getAttribute('data-f-' + key) || '').split('|');
                ok = selected[key].some(function (value) { return values.indexOf(value) !== -1; });
            });

            card.style.display = ok ? '' : 'none';
        });

        dropdowns.forEach(function (dropdown) {
            const badge = dropdown.querySelector('.materials-filter-badge');
            const count = dropdown.querySelectorAll('.materials-filter-pill-input:checked').length;
            if (badge) {
                badge.textContent = count;
                badge.style.display = count ? '' : 'none';
            }

            const clearOne = dropdown.querySelector('.materials-filter-clear-one');
            if (clearOne) clearOne.style.display = count ? '' : 'none';
        });

        if (clearBtn) {
            clearBtn.style.display = (q || Object.keys(selected).length) ? '' : 'none';
        }

        refreshCounts();
    }

    if (applyBtn) applyBtn.addEventListener('click', apply);

    // ընտրված ֆիլտրերի թիվը (badge) թարմացվում է անմիջապես, արդյունքները՝ «Կիրառել»-ից հետո
    dropdowns.forEach(function (dropdown) {
        const badge = dropdown.querySelector('.materials-filter-badge');
        const inputs = dropdown.querySelectorAll('.materials-filter-pill-input');
        inputs.forEach(function (input) {
            input.addEventListener('change', function () {
                const count = dropdown.querySelectorAll('.materials-filter-pill-input:checked').length;
                if (badge) {
                    badge.textContent = count;
                    badge.style.display = count ? '' : 'none';
                }
            });
        });
    });

    // Մաքրել միայն այս ֆիլտրը (մյուսները մնում են)
    dropdowns.forEach(function (dropdown) {
        const clearOne = dropdown.querySelector('.materials-filter-clear-one');
        if (!clearOne) return;
        const inputs = dropdown.querySelectorAll('.materials-filter-pill-input');

        // հղումը երևում է հենց ընտրելուց, առանց «Կիրառել»-ի սպասելու
        inputs.forEach(function (input) {
            input.addEventListener('change', function () {
                const any = Array.from(inputs).some(function (i) { return i.checked; });
                clearOne.style.display = any ? '' : 'none';
            });
        });

        clearOne.addEventListener('click', function (e) {
            e.preventDefault();
            inputs.forEach(function (input) { input.checked = false; });
            apply();
        });
    });

    if (search) {
        search.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); apply(); }
        });
    }

    if (clearBtn) {
        clearBtn.addEventListener('click', function (e) {
            e.preventDefault();
            document.querySelectorAll('.materials-filter-pill-input').forEach(function (input) { input.checked = false; });
            if (search) search.value = '';
            apply();
        });
    }
});