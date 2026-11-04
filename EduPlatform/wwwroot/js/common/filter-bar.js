// Ընդհանուր որոնում և ֆիլտրեր (բրաուզերում, առանց սերվերի)։
// Օգտագործում են՝ առաջադրանքներ, շտեմարան, խմբեր, գրադարանի նյութեր։
// Օգտագործում՝ EduFilterBar.init({ scope?: element, items: '.css-selector', onApply?: function () { ...թվերի թարմացում } })
window.EduFilterBar = (function () {
    function init(options) {
        const scope = options.scope || document;
        const itemSelector = options.items;
        const onApply = options.onApply || function () { };

        const search = scope.querySelector('.materials-search-input');
        const dropdowns = scope.querySelectorAll('.materials-filter-dropdown');
        const applyBtn = scope.querySelector('.materials-filter-apply-btn');
        const clearBtn = scope.querySelector('.materials-filter-clear-btn');
        if (!search && !dropdowns.length) return null;

        // ընտրված արժեքները՝ { subject: ['Մաթեմատիկա'], grade: ['7 դասարան'], ... }
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

        function updateBadge(dropdown) {
            const badge = dropdown.querySelector('.materials-filter-badge');
            const count = dropdown.querySelectorAll('.materials-filter-pill-input:checked').length;
            if (badge) {
                badge.textContent = count;
                badge.style.display = count ? '' : 'none';
            }
            const clearOne = dropdown.querySelector('.materials-filter-clear-one');
            if (clearOne) clearOne.style.display = count ? '' : 'none';
        }

        function apply() {
            const q = (search ? search.value : '').trim().toLowerCase();
            const selected = selectedValues();

            scope.querySelectorAll(itemSelector).forEach(function (el) {
                let ok = !q || (el.getAttribute('data-f-search') || '').indexOf(q) !== -1;

                // նույն ֆիլտրի ներսում՝ «կամ», տարբեր ֆիլտրերի միջև՝ «և»
                Object.keys(selected).forEach(function (key) {
                    if (!ok) return;
                    const values = (el.getAttribute('data-f-' + key) || '').split('|');
                    ok = selected[key].some(function (value) { return values.indexOf(value) !== -1; });
                });

                el.style.display = ok ? '' : 'none';
            });

            dropdowns.forEach(updateBadge);

            if (clearBtn) {
                clearBtn.style.display = (q || Object.keys(selected).length) ? '' : 'none';
            }

            onApply();
        }

        if (applyBtn) applyBtn.addEventListener('click', apply);

        // badge-ը և «Մաքրել այս ֆիլտրը» հղումը թարմացվում են հենց ընտրելուց, արդյունքները՝ «Կիրառել»-ից հետո
        dropdowns.forEach(function (dropdown) {
            const inputs = dropdown.querySelectorAll('.materials-filter-pill-input');
            const clearOne = dropdown.querySelector('.materials-filter-clear-one');

            // ցանկի scroll-ը չի թռնում վերև, երբ տարբերակ ես ընտրում
            const menu = dropdown.querySelector('.materials-filter-menu');
            let savedTop = 0;
            dropdown.querySelectorAll('.materials-filter-pill').forEach(function (pill) {
                pill.addEventListener('mousedown', function (e) {
                    savedTop = menu ? menu.scrollTop : 0;
                    e.preventDefault();               // թաքնված checkbox-ին ֆոկուս չի գնում
                });
            });

            inputs.forEach(function (input) {
                input.addEventListener('change', function () {
                    updateBadge(dropdown);
                    if (menu) menu.scrollTop = savedTop;   // եթե բրաուզերը թռցրեց՝ վերադարձնում ենք
                });
            });

            // Մաքրել միայն այս ֆիլտրը (մյուսները և որոնումը մնում են)
            if (clearOne) {
                clearOne.addEventListener('click', function (e) {
                    e.preventDefault();
                    inputs.forEach(function (input) { input.checked = false; });
                    apply();
                });
            }
        });

        if (search) {
            search.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') { e.preventDefault(); apply(); }
            });
        }

        if (clearBtn) {
            clearBtn.addEventListener('click', function (e) {
                e.preventDefault();
                scope.querySelectorAll('.materials-filter-pill-input').forEach(function (input) { input.checked = false; });
                if (search) search.value = '';
                apply();
            });
        }

        return { apply: apply };
    }

    return { init: init };
})();
