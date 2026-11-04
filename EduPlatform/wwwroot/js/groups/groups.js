document.addEventListener('DOMContentLoaded', function () {
    // ── Որոնում և ֆիլտրեր (բրաուզերում) ───────────────────────────────────────
    const search = document.querySelector('.materials-search-input');
    const dropdowns = document.querySelectorAll('.materials-filter-dropdown');
    const applyBtn = document.querySelector('.materials-filter-apply-btn');
    const clearBtn = document.querySelector('.materials-filter-clear-btn');

    function refreshCounts() {
        const count = Array.from(document.querySelectorAll('.group-card'))
            .filter(function (el) { return el.style.display !== 'none'; }).length;
        const el = document.getElementById('groupsCount');
        if (el) el.textContent = count;
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

        document.querySelectorAll('.group-card').forEach(function (card) {
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
