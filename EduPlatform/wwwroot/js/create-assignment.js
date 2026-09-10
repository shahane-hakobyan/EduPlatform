document.addEventListener('DOMContentLoaded', function () {
    // --- Chip inputs (subject / grade) ---
    document.querySelectorAll('[data-chip-input]').forEach(function (wrap) {
        const search = wrap.querySelector('.ca-chip-search');
        const list = wrap.querySelector('.ca-chip-list');
        const dropdown = wrap.querySelector('.ca-chip-dropdown');
        const fieldName = wrap.getAttribute('data-field-name');

        function addChip(value, label) {
            if (wrap.querySelector('[data-chip-value="' + value + '"]')) return;

            const chip = document.createElement('span');
            chip.className = 'ca-chip';
            chip.setAttribute('data-chip-value', value);
            chip.innerHTML = label + ' <i class="bi bi-x ca-chip-remove"></i>' +
                '<input type="hidden" name="' + fieldName + '" value="' + value + '">';
            chip.querySelector('.ca-chip-remove').addEventListener('click', function (e) {
                e.stopPropagation();
                chip.remove();
                const opt = dropdown.querySelector('[data-value="' + value + '"]');
                if (opt) opt.classList.remove('selected');
            });
            list.appendChild(chip);

            const opt = dropdown.querySelector('[data-value="' + value + '"]');
            if (opt) opt.classList.add('selected');
        }

        search.addEventListener('focus', function () { wrap.classList.add('open'); });
        document.addEventListener('click', function (e) {
            if (!wrap.contains(e.target)) wrap.classList.remove('open');
        });

        search.addEventListener('input', function () {
            const q = search.value.trim().toLowerCase();
            dropdown.querySelectorAll('.ca-chip-option').forEach(function (opt) {
                const label = opt.getAttribute('data-label').toLowerCase();
                opt.style.display = label.includes(q) ? '' : 'none';
            });
        });

        dropdown.querySelectorAll('.ca-chip-option').forEach(function (opt) {
            opt.addEventListener('click', function () {
                if (opt.classList.contains('selected')) return;
                addChip(opt.getAttribute('data-value'), opt.getAttribute('data-label'));
                search.value = '';
            });
        });
    });

    // --- Single-select pill / radio rows (assignment type, grading type) ---
    document.querySelectorAll('[data-single-select]').forEach(function (row) {
        const fieldName = row.getAttribute('data-field-name');
        let hidden = row.parentElement.querySelector('input[name="' + fieldName + '"]');
        if (!hidden) {
            hidden = document.createElement('input');
            hidden.type = 'hidden';
            hidden.name = fieldName;
            row.parentElement.appendChild(hidden);
        }

        row.querySelectorAll('button').forEach(function (btn) {
            btn.addEventListener('click', function () {
                row.querySelectorAll('button').forEach(function (b) { b.classList.remove('active'); });
                btn.classList.add('active');
                hidden.value = btn.getAttribute('data-value') || btn.textContent.trim();
            });
        });
    });

    // --- File dropzone ---
    const fileInput = document.getElementById('caFileInput');
    const fileNameEl = document.getElementById('caFileName');
    if (fileInput) {
        fileInput.addEventListener('change', function () {
            fileNameEl.textContent = fileInput.files.length ? fileInput.files[0].name : '';
        });
    }

    // --- Class/group tabs ---
    document.querySelectorAll('[data-group-tab]').forEach(function (tab) {
        tab.addEventListener('click', function () {
            document.querySelectorAll('[data-group-tab]').forEach(function (t) { t.classList.remove('active'); });
            tab.classList.add('active');
            // Only "classes" has data behind it right now — extend here once
            // "My Groups" has its own data source from the backend.
        });
    });

    // --- Class group expand/collapse ---
    document.querySelectorAll('.ca-class-group-head').forEach(function (head) {
        head.addEventListener('click', function () {
            head.closest('.ca-class-group').classList.toggle('open');
        });
    });

    // --- Pupil selection ---
    const submitBtn = document.getElementById('caSubmitBtn');
    const selectedCountEl = document.getElementById('caSelectedCount');
    const selectedCountTopEl = document.getElementById('caSelectedCountTop');

    function refreshCounts() {
        const checked = document.querySelectorAll('[data-pupil-checkbox]:checked');
        const total = checked.length;
        selectedCountEl.textContent = total;
        selectedCountTopEl.textContent = total;
        submitBtn.disabled = total === 0;

        document.querySelectorAll('.ca-class-group').forEach(function (group) {
            const groupChecked = group.querySelectorAll('[data-pupil-checkbox]:checked').length;
            const groupTotal = group.querySelectorAll('[data-pupil-checkbox]').length;
            const badge = group.querySelector('[data-selected-badge]');
            badge.textContent = groupChecked;
            badge.classList.toggle('show', groupChecked > 0);
            group.querySelector('[data-progress-label]').textContent = groupChecked + ' / ' + groupTotal;
        });
    }

    document.querySelectorAll('[data-pupil-checkbox]').forEach(function (cb) {
        cb.addEventListener('change', refreshCounts);
    });

    document.querySelectorAll('.ca-select-all-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const group = btn.closest('.ca-class-group');
            const boxes = group.querySelectorAll('[data-pupil-checkbox]');
            const allChecked = Array.from(boxes).every(function (b) { return b.checked; });
            boxes.forEach(function (b) { b.checked = !allChecked; });
            refreshCounts();
        });
    });

    const clearAllBtn = document.getElementById('caClearAllBtn');
    if (clearAllBtn) {
        clearAllBtn.addEventListener('click', function () {
            document.querySelectorAll('[data-pupil-checkbox]:checked').forEach(function (cb) { cb.checked = false; });
            refreshCounts();
        });
    }

    // --- Pupil search filter ---
    const pupilSearch = document.getElementById('caPupilSearch');
    if (pupilSearch) {
        pupilSearch.addEventListener('input', function () {
            const q = pupilSearch.value.trim().toLowerCase();
            document.querySelectorAll('.ca-pupil-item').forEach(function (item) {
                const name = item.querySelector('.ca-pupil-name').textContent.toLowerCase();
                item.style.display = name.includes(q) ? '' : 'none';
            });
        });
    }

    refreshCounts();
});