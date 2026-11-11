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
    const dropzone = document.querySelector('.ca-dropzone');
    if (fileInput) {
        const dt = new DataTransfer();   // ընտրված ֆայլերի ընդհանուր ցուցակը (click + drag & drop)

        function renderFiles() {
            fileNameEl.innerHTML = '';
            Array.from(dt.files).forEach(function (f, i) {
                const chip = document.createElement('span');
                chip.className = 'ca-file-chip';
                chip.textContent = f.name + ' ';
                const x = document.createElement('i');
                x.className = 'bi bi-x ca-file-chip-remove';
                x.addEventListener('click', function (e) {
                    e.preventDefault();      // label-ի ներսում՝ ֆայլի ընտրության պատուհանը չբացվի
                    e.stopPropagation();
                    const next = new DataTransfer();
                    Array.from(dt.files).forEach(function (g, j) { if (j !== i) next.items.add(g); });
                    dt.items.clear();
                    Array.from(next.files).forEach(function (g) { dt.items.add(g); });
                    fileInput.files = dt.files;
                    renderFiles();
                });
                chip.appendChild(x);
                fileNameEl.appendChild(chip);
            });
        }

        function addFiles(list) {
            Array.from(list).forEach(function (f) {
                const exists = Array.from(dt.files).some(function (g) { return g.name === f.name && g.size === f.size; });
                if (!exists) dt.items.add(f);
            });
            fileInput.files = dt.files;
            renderFiles();
        }

        fileInput.addEventListener('change', function () {
            // ընտրվածները ավելացվում են արդեն եղածներին
            const picked = Array.from(fileInput.files);
            fileInput.files = dt.files;
            addFiles(picked);
        });

        if (dropzone) {
            ['dragenter', 'dragover'].forEach(function (ev) {
                dropzone.addEventListener(ev, function (e) {
                    e.preventDefault();
                    dropzone.classList.add('dragover');
                });
            });
            ['dragleave', 'dragend'].forEach(function (ev) {
                dropzone.addEventListener(ev, function () { dropzone.classList.remove('dragover'); });
            });
            dropzone.addEventListener('drop', function (e) {
                e.preventDefault();
                dropzone.classList.remove('dragover');
                if (e.dataTransfer && e.dataTransfer.files.length) addFiles(e.dataTransfer.files);
            });
        }

        // ֆայլը ձևից դուրս գցելիս բրաուզերը այն չի բացում
        ['dragover', 'drop'].forEach(function (ev) {
            window.addEventListener(ev, function (e) {
                if (e.target.closest && e.target.closest('.ca-dropzone')) return;
                e.preventDefault();
            });
        });
    }

    // --- Սովորողների ընտրություն («Իմ դասարանները» / «Իմ խմբերը»)՝ ընդհանուր կոդը pupil-picker.js ---
    const submitBtn = document.getElementById('caSubmitBtn');
    const selectedCountTopEl = document.getElementById('caSelectedCountTop');
    const pickerRoot = document.querySelector('[data-pupil-picker]');

    if (pickerRoot) {
        EduPupilPicker.init(pickerRoot, {
            onChange: function (total) {
                if (selectedCountTopEl) selectedCountTopEl.textContent = total;
                if (submitBtn) submitBtn.disabled = total === 0;
            }
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

            // «Ուղարկել» էջ (շտեմարանից)՝ ձևի դաշտեր չկան, ստուգվում են միայն ժամկետները
            const sendOnly = form.getAttribute('data-mode') === 'SendOnly';

            const ok = sendOnly || (
                has('SubjectIds') &&
                has('GradeLevels') &&
                has('Types') &&
                (!typesHasOther || (otherText && otherText.value.trim() !== '')) &&
                has('GradingType') && form.querySelector('input[name="GradingType"]').value !== '' &&
                (form.querySelector('[name="AllowTextAnswer"]').checked || form.querySelector('[name="AllowFileAnswer"]').checked));

            // Վերջնաժամկետը պետք է լինի սկզբից հետո
            const startEl = form.querySelector('[name="StartDate"]');
            const dueEl = form.querySelector('[name="DueDate"]');
            const datesOk = !(startEl && dueEl && startEl.value && dueEl.value && dueEl.value.slice(0, 10) < startEl.value.slice(0, 10));

            if (!ok || !datesOk) {
                e.preventDefault();
                if (clientError) {
                    clientError.textContent = (ok && !datesOk)
                        ? clientError.getAttribute('data-date-msg')
                        : clientError.getAttribute('data-default-msg');
                    clientError.style.display = '';
                    clientError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
                return;
            }

            // Ամեն ինչ լրացված է՝ ուղարկելուց առաջ հաստատման պատուհան (միայն ուղարկելիս, ոչ շտեմարանի խմբագրման ժամանակ)
            const isEdit = form.getAttribute('data-mode') === 'Edit';
            const confirmModal = document.getElementById('caConfirmModal');
            if (!isEdit && confirmModal && !form.dataset.confirmed) {
                e.preventDefault();
                openConfirm();
            }
        });
    }

    // --- Հաստատման պատուհան՝ հաստատելուց հետո առաջադրանքի տվյալները փոխել հնարավոր չէ ---
    const confirmModal = document.getElementById('caConfirmModal');
    const confirmBackdrop = document.getElementById('caConfirmBackdrop');
    const confirmOk = document.getElementById('caConfirmOk');
    const confirmCancel = document.getElementById('caConfirmCancel');

    function openConfirm() {
        if (confirmModal) confirmModal.classList.add('open');
        if (confirmBackdrop) confirmBackdrop.classList.add('open');
        if (confirmCancel) confirmCancel.focus();
    }

    function closeConfirm() {
        if (confirmModal) confirmModal.classList.remove('open');
        if (confirmBackdrop) confirmBackdrop.classList.remove('open');
    }

    if (confirmModal && form) {
        if (confirmCancel) confirmCancel.addEventListener('click', closeConfirm);
        if (confirmBackdrop) confirmBackdrop.addEventListener('click', closeConfirm);
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeConfirm();
        });

        if (confirmOk) {
            confirmOk.addEventListener('click', function () {
                confirmOk.disabled = true;            // կրկնակի ուղարկում չլինի
                form.dataset.confirmed = '1';
                form.submit();
            });
        }
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

    // --- Task text: looks like a one-line input, grows with the text ---
    const taskText = document.getElementById('caTaskText');
    if (taskText) {
        const grow = function () {
            taskText.style.height = 'auto';
            taskText.style.height = Math.min(taskText.scrollHeight + 2, 200) + 'px';
        };
        taskText.addEventListener('input', grow);
        grow();
    }

});