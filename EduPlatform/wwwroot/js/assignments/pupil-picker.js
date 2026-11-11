// Սովորողների ընտրության ընդհանուր տրամաբանություն՝ «Իմ դասարանները» / «Իմ խմբերը»։
// Օգտագործում են՝ Առաջադրանք ստեղծել, Շտեմարանից ուղարկել, Ավելացնել սովորողներ (պատուհան)։
// Նշագրումը՝ Views/Shared/Repository/Assignments/Form/_PupilPicker.cshtml
// Պահանջում է՝ group-picker.js (ավելի վաղ միացված)
//
// EduPupilPicker.init(rootElement, {
//     onChange: function (selectedCount) { }   // կանչվում է ամեն ընտրությունից հետո և սկզբում
// })  →  { refresh(), selectedIds(), count() }
window.EduPupilPicker = (function () {
    function init(root, options) {
        options = options || {};
        const onChange = options.onChange || function () { };

        const boxes = Array.from(root.querySelectorAll('[data-pupil-checkbox]'));
        const boxByValue = {};
        boxes.forEach(function (b) { boxByValue[b.value] = b; });

        const countEl = root.querySelector('[data-selected-count]');
        const classesPane = root.querySelector('[data-classes-pane]');
        const groupsPane = root.querySelector('[data-groups-pane]');
        let groupPicker = null;

        // ընտրելի են միայն չանջատված checkbox-ները (արդեն ավելացվածները disabled են)
        function selectable(scope) {
            return Array.from((scope || root).querySelectorAll('[data-pupil-checkbox]:not(:disabled)'));
        }

        function selectedIds() {
            const ids = [];
            selectable().forEach(function (b) {
                if (b.checked && ids.indexOf(b.value) === -1) ids.push(b.value);
            });
            return ids;
        }

        function refresh() {
            const total = selectedIds().length;
            if (countEl) countEl.textContent = total;

            root.querySelectorAll('.ca-class-group').forEach(function (group) {
                const list = selectable(group);
                const checked = list.filter(function (b) { return b.checked; }).length;
                const badge = group.querySelector('[data-selected-badge]');
                if (badge) {
                    badge.textContent = checked;
                    badge.classList.toggle('show', checked > 0);
                }
                const progress = group.querySelector('[data-progress-label]');
                if (progress) progress.textContent = checked + ' / ' + list.length;

                // «Ընտրել բոլորը»՝ երբ դասարանի բոլորն ընտրված են, վանդակը լցված է
                const allBtn = group.querySelector('.ca-select-all-btn');
                if (allBtn) {
                    const all = list.length > 0 && checked === list.length;
                    allBtn.classList.toggle('is-all', all);
                    const icon = allBtn.querySelector('i');
                    if (icon) {
                        icon.classList.toggle('bi-check-square-fill', all);
                        icon.classList.toggle('bi-square', !all);
                    }
                }
            });

            if (groupPicker) groupPicker.refresh();
            onChange(total);
        }

        // «Իմ խմբերը»՝ ընդհանուր կոդը group-picker.js-ում
        groupPicker = EduGroupPicker.init({
            root: root,
            getBox: function (value) { return boxByValue[value]; },
            onChange: refresh
        });

        // ներդիրներ
        root.querySelectorAll('[data-group-tab]').forEach(function (tab) {
            tab.addEventListener('click', function () {
                root.querySelectorAll('[data-group-tab]').forEach(function (t) { t.classList.remove('active'); });
                tab.classList.add('active');
                const showGroups = tab.getAttribute('data-group-tab') === 'groups';
                if (classesPane) classesPane.style.display = showGroups ? 'none' : '';
                if (groupsPane) groupsPane.style.display = showGroups ? '' : 'none';
            });
        });

        // դասարանի բացել / փակել
        root.querySelectorAll('.ca-class-group-head').forEach(function (head) {
            head.addEventListener('click', function () {
                head.closest('.ca-class-group').classList.toggle('open');
            });
        });

        // աշակերտի checkbox
        boxes.forEach(function (b) { b.addEventListener('change', refresh); });

        // «Ընտրել բոլորը» դասարանում
        root.querySelectorAll('.ca-class-group .ca-select-all-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const list = selectable(btn.closest('.ca-class-group'));
                if (!list.length) return;
                const allChecked = list.every(function (b) { return b.checked; });
                list.forEach(function (b) { b.checked = !allChecked; });
                refresh();
            });
        });

        // «Մաքրել բոլորը»
        const clearAll = root.querySelector('[data-clear-all]');
        if (clearAll) {
            clearAll.addEventListener('click', function () {
                selectable().forEach(function (b) { b.checked = false; });
                refresh();
            });
        }

        // որոնում՝ դասարանների ցանկում և խմբերում
        const search = root.querySelector('[data-pupil-search]');
        if (search) {
            search.addEventListener('input', function () {
                const q = search.value.trim().toLowerCase();
                root.querySelectorAll('.ca-pupil-item').forEach(function (item) {
                    const name = item.querySelector('.ca-pupil-name').textContent.toLowerCase();
                    item.style.display = name.includes(q) ? '' : 'none';
                });
                groupPicker.filter(q);
            });
        }

        refresh();   // սկզբնական թվերը (նախալրացված և արդեն ավելացված սովորողներ)

        return { refresh: refresh, selectedIds: selectedIds, count: function () { return selectedIds().length; } };
    }

    return { init: init };
})();
