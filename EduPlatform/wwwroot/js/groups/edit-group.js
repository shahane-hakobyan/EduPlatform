document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('editGroupForm');
    if (!form) return;

    const nameInput = document.getElementById('egName');
    const clientError = document.getElementById('egClientError');

    function showError(kind) {
        clientError.textContent = clientError.getAttribute(kind === 'image' ? 'data-image-msg' : 'data-form-msg');
        clientError.style.display = '';
        clientError.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
    function hideError() { clientError.style.display = 'none'; }

    // ── նկար՝ նախադիտում և ստուգում ───────────────────────────────────────────
    const avatar = document.getElementById('egAvatar');
    const imageInput = document.getElementById('egImageInput');
    const removeImage = document.getElementById('egRemoveImage');
    const originalImage = avatar.getAttribute('data-original');
    let previewUrl = null;

    function showOriginal() {
        if (previewUrl) { URL.revokeObjectURL(previewUrl); previewUrl = null; }
        avatar.innerHTML = originalImage ? '<img alt="" />' : '<i class="bi bi-people"></i>';
        if (originalImage) avatar.querySelector('img').src = originalImage;
    }

    imageInput.addEventListener('change', function () {
        hideError();
        const f = imageInput.files[0];
        if (!f) { showOriginal(); return; }

        const okType = /^image\/(jpeg|png)$/.test(f.type);
        const okSize = f.size <= 5 * 1024 * 1024;
        if (!okType || !okSize) {
            imageInput.value = '';
            showOriginal();
            showError('image');
            return;
        }
        if (previewUrl) URL.revokeObjectURL(previewUrl);
        previewUrl = URL.createObjectURL(f);
        avatar.innerHTML = '<img alt="" />';
        avatar.querySelector('img').src = previewUrl;
        if (removeImage) removeImage.checked = false;   // նոր նկար ընտրելիս «հեռացնել»-ը անտեսվում է
    });

    if (removeImage) {
        removeImage.addEventListener('change', function () {
            if (removeImage.checked) {
                imageInput.value = '';
                if (previewUrl) { URL.revokeObjectURL(previewUrl); previewUrl = null; }
                avatar.innerHTML = '<i class="bi bi-people"></i>';
            } else {
                showOriginal();
            }
        });
    }

    // ── անդամների հեռացում ───────────────────────────────────────────────────
    const removeInputs = document.getElementById('egRemoveInputs');
    const membersCount = document.getElementById('egMembersCount');
    const totalMembers = document.querySelectorAll('.eg-member').length;

    function refreshMembers() {
        const removed = document.querySelectorAll('.eg-member.is-removed').length;
        membersCount.textContent = totalMembers - removed;
    }

    document.querySelectorAll('[data-remove-member]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const row = btn.closest('.eg-member');
            const id = row.getAttribute('data-user-id');
            const removed = row.classList.toggle('is-removed');

            // հեռացվածները ուղարկվում են RemoveMemberIds-ով, վերականգնվածները՝ ոչ
            const existing = removeInputs.querySelector('[data-id="' + CSS.escape(id) + '"]');
            if (removed && !existing) {
                const h = document.createElement('input');
                h.type = 'hidden';
                h.name = 'RemoveMemberIds';
                h.value = id;
                h.setAttribute('data-id', id);
                removeInputs.appendChild(h);
            } else if (!removed && existing) {
                existing.remove();
            }
            btn.querySelector('i').className = removed ? 'bi bi-arrow-counterclockwise' : 'bi bi-person-dash';
            refreshMembers();
        });
    });

    // ── նոր անդամներ ավելացնել ───────────────────────────────────────────────
    const items = Array.from(document.querySelectorAll('.eg-pick-list .cg-pick-item'));
    const search = document.getElementById('egPupilSearch');
    const classFilter = document.getElementById('egClassFilter');
    const addCount = document.getElementById('egAddCount');

    function cb(item) { return item.querySelector('input[type="checkbox"]'); }
    function refreshAdd() {
        addCount.textContent = items.filter(function (i) { return cb(i).checked; }).length;
    }
    function applyFilter() {
        const q = search ? search.value.trim().toLowerCase() : '';
        const cls = classFilter ? classFilter.value : '';
        items.forEach(function (item) {
            const okText = !q || item.getAttribute('data-search').indexOf(q) !== -1;
            const okClass = !cls || item.getAttribute('data-class') === cls;
            item.style.display = okText && okClass ? '' : 'none';
        });
    }

    if (search) search.addEventListener('input', applyFilter);
    if (classFilter) classFilter.addEventListener('change', applyFilter);
    items.forEach(function (i) { cb(i).addEventListener('change', refreshAdd); });

    const selVisible = document.getElementById('egSelectVisible');
    if (selVisible) {
        selVisible.addEventListener('click', function () {
            const visible = items.filter(function (i) { return i.style.display !== 'none'; });
            const all = visible.every(function (i) { return cb(i).checked; });
            visible.forEach(function (i) { cb(i).checked = !all; });
            refreshAdd();
        });
    }
    const clearAll = document.getElementById('egClearAll');
    if (clearAll) {
        clearAll.addEventListener('click', function () {
            items.forEach(function (i) { cb(i).checked = false; });
            refreshAdd();
        });
    }

    // Enter-ով ձևը պատահաբար չի ուղարկվում
    form.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && e.target.tagName === 'INPUT' && e.target.type !== 'submit') e.preventDefault();
    });

    form.addEventListener('submit', function (e) {
        if (!nameInput.value.trim()) {
            e.preventDefault();
            showError('form');
        }
    });

    refreshAdd();
    refreshMembers();
});
