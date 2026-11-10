// «Իմ խմբերը»՝ ընտրել խմբի բոլոր անդամներին կամ միայն որոշներին։
// Օգտագործում են՝ Առաջադրանք ստեղծել, Ավելացնել սովորողներ (առաջադրանքի էջ)։
// Աշակերտների իրական checkbox-ները մնում են դասարանների ցանկում (մեկ աղբյուր)՝ խմբի checkbox-ները միայն արտացոլում են դրանք։
//
// EduGroupPicker.init({
//     root?: element,                    // որտեղ են [data-group-card] քարտերը
//     getBox: function (pupilUserId),    // վերադարձնում է աշակերտի իրական checkbox-ը
//     onChange: function ()              // կանչվում է ընտրությունը փոխվելուց հետո
// })  →  { refresh(), filter(query) }
window.EduGroupPicker = (function () {
    function init(options) {
        const root = options.root || document;
        const getBox = options.getBox;
        const onChange = options.onChange || function () { };
        const cards = Array.from(root.querySelectorAll('[data-group-card]'));

        // միայն ընտրելի անդամները (արդեն ավելացվածները disabled են՝ երևում են, բայց չեն ընտրվում)
        function members(card) {
            return Array.from(card.querySelectorAll('[data-group-member]:not(:disabled)'));
        }

        // խմբի քարտերի վիճակը՝ ըստ իրական checkbox-ների
        function refresh() {
            cards.forEach(function (card) {
                const list = members(card);
                let checked = 0;
                list.forEach(function (m) {
                    const real = getBox(m.value);
                    m.checked = !!(real && real.checked);
                    if (m.checked) checked++;
                });
                const progress = card.querySelector('[data-group-progress]');
                if (progress) progress.textContent = checked + ' / ' + list.length;
                card.classList.toggle('selected', list.length > 0 && checked === list.length);
                card.classList.toggle('partial', checked > 0 && checked < list.length);
                card.classList.toggle('empty', list.length === 0);
            });
        }

        // որոնում՝ քարտը երևում է, եթե համընկնում է խմբի կամ նրա որևէ անդամի անունը
        function filter(query) {
            const q = (query || '').trim().toLowerCase();
            cards.forEach(function (card) {
                const groupMatch = (card.getAttribute('data-group-name') || '').includes(q);
                let anyMember = false;
                card.querySelectorAll('.ca-group-member').forEach(function (label) {
                    const match = groupMatch || (label.getAttribute('data-member-name') || '').includes(q);
                    label.style.display = match ? '' : 'none';
                    if (match) anyMember = true;
                });
                card.style.display = (groupMatch || anyMember) ? '' : 'none';
            });
        }

        cards.forEach(function (card) {
            // խմբի վերնագրի սեղմում՝ ընտրել բոլորին / հանել բոլորին
            const selectBtn = card.querySelector('[data-group-select]');

            if (selectBtn) {
                selectBtn.addEventListener('click', function (e) {

                    e.stopPropagation();

                    const list = members(card);

                    if (!list.length) {
                        return;
                    }

                    const boxes = list
                        .map(function (m) {
                            return getBox(m.value);
                        })
                        .filter(Boolean);

                    const allChecked = boxes.every(function (b) {
                        return b.checked;
                    });

                    list.forEach(function (m) {

                        const real = getBox(m.value);

                        if (real) {
                            real.checked = !allChecked;
                        }

                        m.checked = !allChecked;
                    });

                    refresh();
                    onChange();
                });
            }

            // սլաք՝ բացել / փակել անդամների ցանկը
            const head = card.querySelector('.ca-group-head');

            if (head) {
                head.addEventListener('click', function (e) {

                    if (e.target.closest('[data-group-select]')) {
                        return;
                    }

                    card.classList.toggle('open');
                });
            }

            // առանձին անդամի ընտրություն
            members(card).forEach(function (m) {
                m.addEventListener('change', function () {
                    const real = getBox(m.value);
                    if (real) real.checked = m.checked;
                    onChange();
                });
            });
        });

        return { refresh: refresh, filter: filter };
    }

    return { init: init };
})();
