document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('createGroupForm');
    if (!form) return;

    const step1 = document.getElementById('cgStep1');
    const step2 = document.getElementById('cgStep2');
    const title = document.getElementById('cgTitle');
    const defaultTitle = title.textContent;
    const selectedTop = document.getElementById('cgSelectedTop');
    const selectedTopCount = document.getElementById('cgSelectedTopCount');
    const clientError = document.getElementById('cgClientError');
    const nameInput = document.getElementById('cgName');
    const imageInput = document.getElementById('cgImageInput');
    const avatar = document.getElementById('cgAvatar');
    const pickPane = document.getElementById('cgPickPane');
    const otherPane = document.getElementById('cgOtherPane');
    const submitBtn = document.getElementById('cgSubmit');

    function showError(kind) {
        clientError.textContent = clientError.getAttribute(kind === 'image' ? 'data-image-msg' : 'data-form-msg');
        clientError.style.display = '';
        clientError.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
    function hideError() { clientError.style.display = 'none'; }

    function selectedType() {
        const r = form.querySelector('input[name="Type"]:checked');
        return r ? r.value : '';
    }

    // ── նկար՝ նախադիտում և ստուգում ───────────────────────────────────────────
    let avatarUrl = null;
    function resetAvatar() {
        if (avatarUrl) { URL.revokeObjectURL(avatarUrl); avatarUrl = null; }
        avatar.innerHTML = '<i class="bi bi-people"></i>';
    }
    imageInput.addEventListener('change', function () {
        hideError();
        const f = imageInput.files[0];
        if (!f) { resetAvatar(); return; }

        const okType = /^image\/(jpeg|png)$/.test(f.type);
        const okSize = f.size <= 5 * 1024 * 1024;
        if (!okType || !okSize) {
            imageInput.value = '';
            resetAvatar();
            showError('image');
            return;
        }
        resetAvatar();
        avatarUrl = URL.createObjectURL(f);
        avatar.innerHTML = '<img alt="" />';
        avatar.querySelector('img').src = avatarUrl;
    });

    // ── անդամների ընտրություն ─────────────────────────────────────────────────
    const items = Array.from(document.querySelectorAll('.cg-pick-item'));
    const search = document.getElementById('cgPupilSearch');
    const classFilter = document.getElementById('cgClassFilter');

    function checkboxOf(item) { return item.querySelector('input[type="checkbox"]'); }

    function refreshCount() {
        const n = items.filter(function (i) { return checkboxOf(i).checked; }).length;
        selectedTopCount.textContent = n;
        // ստեղծողը նույնպես խմբի անդամ է՝ +1
        submitBtn.textContent = submitBtn.getAttribute('data-template').replace('{0}', n + 1);
    }

    function applyFilter() {
        const q = search.value.trim().toLowerCase();
        const cls = classFilter.value;
        items.forEach(function (item) {
            const okText = !q || item.getAttribute('data-search').indexOf(q) !== -1;
            const okClass = !cls || item.getAttribute('data-class') === cls;
            item.style.display = okText && okClass ? '' : 'none';
        });
    }

    search.addEventListener('input', applyFilter);
    classFilter.addEventListener('change', applyFilter);
    items.forEach(function (item) { checkboxOf(item).addEventListener('change', refreshCount); });

    // «Ընտրել բոլորը»՝ միայն երևացողների մեջ (օր.՝ ամբողջ դասարանը)
    document.getElementById('cgSelectVisible').addEventListener('click', function () {
        const visible = items.filter(function (i) { return i.style.display !== 'none'; });
        const all = visible.every(function (i) { return checkboxOf(i).checked; });
        visible.forEach(function (i) { checkboxOf(i).checked = !all; });
        refreshCount();
    });

    document.getElementById('cgClearAll').addEventListener('click', function () {
        items.forEach(function (i) { checkboxOf(i).checked = false; });
        refreshCount();
    });

    // ── քայլերի փոխարկում ─────────────────────────────────────────────────────
    function goStep2() {
        const name = nameInput.value.trim();
        const type = selectedType();
        if (!name || !type) { showError('form'); return; }
        hideError();

        const isPick = type === '1' || type === '2';
        const isParents = type === '2';
        pickPane.style.display = isPick ? '' : 'none';
        otherPane.style.display = isPick ? 'none' : '';
        selectedTop.style.display = isPick ? '' : 'none';

        document.getElementById('cgBannerPupils').style.display = isParents ? 'none' : '';
        document.getElementById('cgBannerParents').style.display = isParents ? '' : 'none';
        document.querySelectorAll('.cg-sub-pupil').forEach(function (e) { e.style.display = isParents ? 'none' : ''; });
        document.querySelectorAll('.cg-sub-parent').forEach(function (e) { e.style.display = isParents ? '' : 'none'; });

        // ընտրվող Id-ն՝ սովորողինը կամ նրա ծնողինը
        items.forEach(function (item) {
            const cb = checkboxOf(item);
            cb.value = isParents ? cb.getAttribute('data-parent-id') : cb.getAttribute('data-pupil-id');
        });

        title.textContent = title.getAttribute('data-step2') || title.textContent;
        step1.style.display = 'none';
        step2.style.display = '';
        refreshCount();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function goStep1() {
        hideError();
        title.textContent = defaultTitle;
        selectedTop.style.display = 'none';
        step2.style.display = 'none';
        step1.style.display = '';
    }

    document.getElementById('cgContinue').addEventListener('click', goStep2);

    // վերևի սլաքը 2-րդ քայլում վերադարձնում է 1-ին քայլ, 1-ինում՝ դուրս է գալիս
    document.getElementById('cgBack').addEventListener('click', function (e) {
        if (step2.style.display !== 'none') {
            e.preventDefault();
            goStep1();
        }
    });

    // Enter-ով ձևը չի ուղարկվում առաջին քայլում
    form.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && e.target.tagName === 'INPUT') {
            e.preventDefault();
            if (step2.style.display === 'none') goStep2();
        }
    });

    form.addEventListener('submit', function (e) {
        if (!nameInput.value.trim() || !selectedType()) {
            e.preventDefault();
            goStep1();
            showError('form');
        }
    });

    refreshCount();
});
