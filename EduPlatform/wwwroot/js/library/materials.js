// «Իմ գրադարան»-ի էջերը՝ Materials (մեկ ցուցակ) և OtherBooks (ներդիրներով)։
document.addEventListener('DOMContentLoaded', function () {
    const tabPanes = document.querySelectorAll('.library-tab-pane');
    // ներդիրներ չկան → ամբողջ էջը մեկ «ներդիր» է
    const scopes = tabPanes.length ? Array.from(tabPanes) : [document];

    // ── ներդիրներ (միայն OtherBooks) ──────────────────────────────────────────
    document.querySelectorAll('.library-tab-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.library-tab-btn').forEach(function (b) { b.classList.remove('active'); });
            tabPanes.forEach(function (p) {
                p.classList.remove('active');
                p.style.display = 'none';
            });

            btn.classList.add('active');
            const target = document.getElementById(btn.getAttribute('data-tab-target'));
            if (target) {
                target.classList.add('active');
                target.style.display = 'block';
            }

            const countEl = document.getElementById('libraryTabCount');
            if (countEl) countEl.textContent = btn.getAttribute('data-count') || '0';
        });
    });

    // ── tooltip-ներ ───────────────────────────────────────────────────────────
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        new bootstrap.Tooltip(el);
    });

    // ── շապիկի մեծ նախադիտում ────────────────────────────────────────────────
    const preview = document.getElementById('materialImagePreview');
    if (preview) {
        document.querySelectorAll('.material-card-wide-image[data-full-image]').forEach(function (el) {
            const url = el.getAttribute('data-full-image');
            if (!url) return;

            el.addEventListener('mouseenter', function () {
                // img-ը ստեղծում ենք DOM-ով, ոչ innerHTML-ով՝ URL-ը HTML չի դառնում
                const img = document.createElement('img');
                img.src = url;
                img.alt = '';
                preview.replaceChildren(img);
                preview.style.display = 'flex';
            });

            el.addEventListener('mousemove', function (e) {
                const margin = 20;
                const maxWidth = preview.offsetWidth || 320;
                const maxHeight = preview.offsetHeight || 420;

                let left = e.clientX + margin;
                let top = e.clientY + margin;

                // եթե տեղ չկա՝ նկարը ցույց է տրվում կուրսորի ձախ / վերևի կողմում
                if (left + maxWidth > window.innerWidth) left = e.clientX - margin - maxWidth;
                if (top + maxHeight > window.innerHeight) top = e.clientY - margin - maxHeight;

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

    // ── հեռացում «Իմ գրադարան»-ից ─────────────────────────────────────────────
    document.querySelectorAll('.material-card-delete-btn').forEach(function (btn) {
        btn.addEventListener('click', async function () {
            const confirmText = btn.getAttribute('data-confirm-text') || 'Delete this material?';
            if (!confirm(confirmText)) return;

            btn.disabled = true;
            try {
                const res = await fetch('/Library/RemoveSavedMaterial', {
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
            if (key) {
                document.querySelectorAll('[data-saved-key="' + CSS.escape(key) + '"]')
                    .forEach(function (el) { el.remove(); });
            }

            refreshCounts();
        });
    });

    // ── թվեր և դատարկ վիճակ ──────────────────────────────────────────────────
    function gridCards(scope) {
        return Array.from(scope.querySelectorAll('.materials-grid .material-card-wide'));
    }

    function visibleCount(scope) {
        return gridCards(scope).filter(function (el) { return el.style.display !== 'none'; }).length;
    }

    function refreshEmptyState(content) {
        const total = gridCards(content).length;
        const visible = visibleCount(content);
        const none = content.querySelector('[data-empty="none"]');
        const filtered = content.querySelector('[data-empty="filtered"]');
        if (none) none.style.display = total === 0 ? '' : 'none';
        if (filtered) filtered.style.display = total > 0 && visible === 0 ? '' : 'none';
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

        document.querySelectorAll('.materials-content').forEach(refreshEmptyState);
    }

    // ── որոնում և ֆիլտրեր (բրաուզերում, առանց սերվերի) ───────────────────────
    scopes.forEach(function (scope) {
        initViewToggle(scope);
        EduFilterBar.init({ scope: scope, items: '[data-saved-key]', onApply: refreshCounts });
    });
});
