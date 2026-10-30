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

    // --- Multi-select pills (assignment type) ---
    document.querySelectorAll('[data-multi-select]').forEach(function (row) {
        const fieldName = row.getAttribute('data-field-name');
        const container = row.parentElement;
        const otherValue = '32';   // AssignmentTypes.Other
        const otherWrap = document.getElementById('caOtherTypeWrap');

        row.querySelectorAll('button').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const value = btn.getAttribute('data-value');
                const nowActive = !btn.classList.contains('active');
                btn.classList.toggle('active', nowActive);

                const existing = container.querySelector('input[type="hidden"][name="' + fieldName + '"][value="' + value + '"]');
                if (nowActive && !existing) {
                    const hidden = document.createElement('input');
                    hidden.type = 'hidden';
                    hidden.name = fieldName;
                    hidden.value = value;
                    container.appendChild(hidden);
                } else if (!nowActive && existing) {
                    existing.remove();
                }

                // «Այլ» ընտրելիս բացվում է տեքստի դաշտը
                if (value === otherValue && otherWrap) {
                    otherWrap.style.display = nowActive ? '' : 'none';
                }
            });
        });
    });

    // --- File dropzone (մի քանի ֆայլ) ---
    const fileInput = document.getElementById('caFileInput');
    const fileNameEl = document.getElementById('caFileName');
    if (fileInput) {
        fileInput.addEventListener('change', function () {
            fileNameEl.textContent = Array.from(fileInput.files).map(function (f) { return f.name; }).join(', ');
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

    // --- Ուղարկելուց առաջ՝ պարտադիր դաշտերի ստուգում ---
    const form = document.getElementById('createAssignmentForm');
    const clientError = document.getElementById('caClientError');
    if (form) {
        form.addEventListener('submit', function (e) {
            const has = function (name) { return form.querySelectorAll('input[type="hidden"][name="' + name + '"]').length > 0; };
            const typesHasOther = !!form.querySelector('input[type="hidden"][name="Types"][value="32"]');
            const otherText = form.querySelector('input[name="OtherTypeText"]');

            const ok =
                has('SubjectIds') &&
                has('GradeLevels') &&
                has('Types') &&
                (!typesHasOther || (otherText && otherText.value.trim() !== '')) &&
                has('GradingType') && form.querySelector('input[name="GradingType"]').value !== '' &&
                (form.querySelector('[name="AllowTextAnswer"]').checked || form.querySelector('[name="AllowFileAnswer"]').checked);

            if (!ok) {
                e.preventDefault();
                if (clientError) {
                    clientError.style.display = '';
                    clientError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            }
        });
    }

    // --- Շտեմարանից բացելիս՝ նախալրացում (նույն կլիկներով, ինչ ձեռքով ընտրելիս) ---
    (function () {
        const el = document.getElementById('caPrefill');
        if (!el) return;
        let d;
        try { d = JSON.parse(el.textContent); } catch (e) { return; }

        function click(selector) {
            const node = document.querySelector(selector);
            if (node) node.click();
        }

        (d.subjectIds || []).forEach(function (v) {
            click('[data-field-name="SubjectIds"] .ca-chip-option[data-value="' + v + '"]');
        });
        (d.gradeLevels || []).forEach(function (v) {
            click('[data-field-name="GradeLevels"] .ca-chip-option[data-value="' + v + '"]');
        });
        (d.types || []).forEach(function (v) {
            click('[data-multi-select] button[data-value="' + v + '"]');
        });
        if (d.gradingType) {
            click('[data-single-select] button[data-value="' + d.gradingType + '"]');
        }
    })();

    refreshCounts();
});