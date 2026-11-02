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
                preview.style.display = 'flex';
            });

            el.addEventListener('mousemove', function (e) {
                const margin = 20;
                const maxWidth = preview.offsetWidth || 320;
                const maxHeight = preview.offsetHeight || 420;

                let left = e.clientX + margin;
                let top = e.clientY + margin;

                // եթե տեղ չկա՝ նկարը ցույց է տրվում կուրսորի ձախ / վերևի կողմում
                if (left + maxWidth > window.innerWidth) {
                    left = e.clientX - margin - maxWidth;
                }
                if (top + maxHeight > window.innerHeight) {
                    top = e.clientY - margin - maxHeight;
                }

                preview.style.left = Math.max(0, left) + 'px';
                preview.style.top = Math.max(0, top) + 'px';
            });

            el.addEventListener('mouseleave', function () {
                preview.style.display = 'none';
            });
        });
    }

    // ── ցանց / ցուցակ (յուրաքանչյուր ներդիրում առանձին, ընտրությունը պահվում է) ──
    const VIEW_KEY = 'eduMaterialsView';
    let savedView = null;
    try { savedView = localStorage.getItem(VIEW_KEY); } catch (e) { }

    function initViewToggle(scope) {
        const gridBtn = scope.querySelector('.materials-view-btn[data-view="grid"]');
        const listBtn = scope.querySelector('.materials-view-btn[data-view="list"]');
        const gridView = scope.querySelector('.materials-grid');
        const listView = scope.querySelector('.materials-list-view');
        if (!(gridBtn && listBtn && gridView && listView)) return;

        function setView(view) {
            const isList = view === 'list';
            listBtn.classList.toggle('active', isList);
            gridBtn.classList.toggle('active', !isList);
            gridView.style.display = isList ? 'none' : 'grid';
            listView.style.display = isList ? 'block' : 'none';
        }

        function choose(view) {
            setView(view);
            try { localStorage.setItem(VIEW_KEY, view); } catch (e) { }
        }

        gridBtn.addEventListener('click', function () { choose('grid'); });
        listBtn.addEventListener('click', function () { choose('list'); });

        if (savedView === 'list') setView('list');
    }

    const viewPanes = document.querySelectorAll('.library-tab-pane');
    if (viewPanes.length) viewPanes.forEach(initViewToggle);
    else initViewToggle(document);

    document.querySelectorAll('.material-card-delete-btn').forEach(function (btn) {
        btn.addEventListener('click', async function () {
            const confirmText = btn.getAttribute('data-confirm-text') || 'Delete this material?';
            if (!confirm(confirmText)) return;

            btn.disabled = true;
            try {
                const res = await fetch('/Teacher/RemoveSavedMaterial', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ materialType: btn.dataset.type, id: parseInt(btn.dataset.id, 10) })
                });
                if (!res.ok) { btn.disabled = false; return; }
            } catch (e) {
                btn.disabled = false;
                return;
            }

            // ջնջում ենք նյութը և grid-ում, և list-ում
            const key = btn.closest('[data-saved-key]')?.getAttribute('data-saved-key');
            document.querySelectorAll('[data-saved-key="' + key + '"]').forEach(function (el) { el.remove(); });

            refreshCounts();
        });
    });

    // ── որոնում և ֆիլտրեր (բրաուզերում, առանց սերվերի) ───────────────────────
    function visibleCount(scope) {
        return Array.from(scope.querySelectorAll('.materials-grid .material-card-wide'))
            .filter(function (el) { return el.style.display !== 'none'; }).length;
    }

    function refreshCounts() {
        const total = document.querySelector('.materials-count-value');
        if (total) total.textContent = visibleCount(document);

        document.querySelectorAll('.library-tab-btn').forEach(function (tabBtn) {
            const pane = document.getElementById(tabBtn.getAttribute('data-tab-target'));
            if (!pane) return;
            const count = visibleCount(pane);
            tabBtn.setAttribute('data-count', count);
            if (tabBtn.classList.contains('active')) {
                const countEl = document.getElementById('libraryTabCount');
                if (countEl) countEl.textContent = count;
            }
        });
    }

    function initFilters(scope) {
        const search = scope.querySelector('.materials-search-input');
        const dropdowns = scope.querySelectorAll('.materials-filter-dropdown');
        const applyBtn = scope.querySelector('.materials-filter-apply-btn');
        const clearBtn = scope.querySelector('.materials-filter-clear-btn');
        if (!search && !dropdowns.length) return;

        // ընտրված արժեքները՝ { subject: ['Մաթեմատիկա', ...], grade: ['7'], ... }
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

            scope.querySelectorAll('[data-saved-key]').forEach(function (el) {
                let ok = !q || (el.getAttribute('data-f-search') || '').indexOf(q) !== -1;

                // նույն ֆիլտրի ներսում՝ «կամ», տարբեր ֆիլտրերի միջև՝ «և»
                Object.keys(selected).forEach(function (key) {
                    if (!ok) return;
                    const values = (el.getAttribute('data-f-' + key) || '').split('|');
                    ok = selected[key].some(function (value) { return values.indexOf(value) !== -1; });
                });

                el.style.display = ok ? '' : 'none';
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
            dropdown.querySelectorAll('.materials-filter-pill-input').forEach(function (input) {
                input.addEventListener('change', function () {
                    const count = dropdown.querySelectorAll('.materials-filter-pill-input:checked').length;
                    if (badge) {
                        badge.textContent = count;
                        badge.style.display = count ? '' : 'none';
                    }
                });
            });
        });

        // Մաքրել միայն այս ֆիլտրը (մյուսները և որոնումը մնում են)
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
                scope.querySelectorAll('.materials-filter-pill-input').forEach(function (input) { input.checked = false; });
                if (search) search.value = '';
                apply();
            });
        }
    }

    const tabPanes = document.querySelectorAll('.library-tab-pane');
    if (tabPanes.length) tabPanes.forEach(initFilters);
    else initFilters(document);
});