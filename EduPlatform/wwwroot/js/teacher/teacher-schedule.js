document.addEventListener('DOMContentLoaded', function () {
    initTooltips();
    wireDayRows();
    wireEditOpen();

    const DAY_START_MINUTES = 8 * 60 + 15;
    const PERIOD_MINUTES = 45;
    const SHORT_BREAK_MINUTES = 5;
    const LONG_BREAK_MINUTES = 20;
    const LONG_BREAK_BEFORE_PERIOD = 4;

    function breakBeforePeriod(periodNumber) {
        return periodNumber === LONG_BREAK_BEFORE_PERIOD ? LONG_BREAK_MINUTES : SHORT_BREAK_MINUTES;
    }

    function toMinutes(hhmm) {
        const parts = hhmm.split(':').map(Number);
        return parts[0] * 60 + parts[1];
    }

    function toHhmm(minutes) {
        minutes = ((minutes % 1440) + 1440) % 1440;
        return String(Math.floor(minutes / 60)).padStart(2, '0') + ':' + String(minutes % 60).padStart(2, '0');
    }

    function absoluteStartForPeriod(n) {
        let start = DAY_START_MINUTES;
        for (let p = 2; p <= n; p++) {
            start += PERIOD_MINUTES + breakBeforePeriod(p);
        }
        return start;
    }

    function suggestTimeForPeriod(periodNumber, rowsContainer) {
        const rows = rowsContainer.querySelectorAll('.schedule-edit-row:not(.schedule-edit-row-template)');

        let previousEnd = null;
        rows.forEach(function (row) {
            const p = parseInt(row.getAttribute('data-period'), 10);
            const end = row.querySelector('.schedule-edit-end-time').value;
            if (p < periodNumber && end) {
                const mins = toMinutes(end);
                if (previousEnd === null || mins > previousEnd) previousEnd = mins;
            }
        });

        const startMinutes = previousEnd !== null
            ? previousEnd + breakBeforePeriod(periodNumber)
            : absoluteStartForPeriod(periodNumber);

        return {
            start: toHhmm(startMinutes),
            end: toHhmm(startMinutes + PERIOD_MINUTES)
        };
    }

    function renumberRows(rowsContainer) {
        const rows = rowsContainer.querySelectorAll('.schedule-edit-row:not(.schedule-edit-row-template)');
        rows.forEach(function (row, index) {
            const periodNumber = index + 1;
            row.setAttribute('data-period', periodNumber);
            row.querySelector('.schedule-edit-period-badge').textContent = periodNumber;
        });
    }

    document.addEventListener('click', function (e) {
        if (!e.target.closest('.time-range-picker')) {
            document.querySelectorAll('.time-picker-dropdown.open').forEach(function (d) {
                d.classList.remove('open');
            });
        }
    });

    document.addEventListener('click', function (e) {
        const gradeEl = e.target.closest('.schedule-card-grade-name');
        if (!gradeEl) return;

        const gradeId = gradeEl.getAttribute('data-grade-id');
        if (!gradeId) return;

        fetch('/Schedule/GradeRoster?gradeId=' + encodeURIComponent(gradeId))
            .then(r => r.text())
            .then(html => {
                document.getElementById('gradeRosterContainer').innerHTML = html;
                requestAnimationFrame(function () {
                    const modal = document.getElementById('gradeRosterModal');
                    const backdrop = document.getElementById('gradeRosterBackdrop');
                    if (modal) modal.classList.add('open');
                    if (backdrop) backdrop.classList.add('open');
                });
            });
    });

    document.addEventListener('click', function (e) {
        if (!e.target.closest('#gradeRosterBackdrop') && !e.target.closest('.grade-roster-close-btn')) return;

        const modal = document.getElementById('gradeRosterModal');
        const backdrop = document.getElementById('gradeRosterBackdrop');
        if (modal) modal.classList.remove('open');
        if (backdrop) backdrop.classList.remove('open');
        setTimeout(function () {
            const container = document.getElementById('gradeRosterContainer');
            if (container) container.innerHTML = '';
        }, 300);
    });

    document.addEventListener('click', function (e) {
        const toggle = e.target.closest('.ms-dropdown-toggle');
        const option = e.target.closest('.ms-option');

        if (toggle) {
            const dropdown = toggle.closest('.ms-dropdown');
            document.querySelectorAll('.ms-dropdown.open').forEach(function (d) {
                if (d !== dropdown) d.classList.remove('open');
            });
            dropdown.classList.toggle('open');
            dropdown.classList.remove('field-invalid');
            return;
        }

        if (option) {
            option.classList.toggle('selected');
            const dropdown = option.closest('.ms-dropdown');
            updateMsLabel(dropdown);
            dropdown.classList.remove('field-invalid');
            return;
        }

        if (!e.target.closest('.ms-dropdown')) {
            document.querySelectorAll('.ms-dropdown.open').forEach(d => d.classList.remove('open'));
        }
    });

    document.addEventListener('input', function (e) {
        if (e.target.classList.contains('schedule-edit-room-input')) {
            e.target.classList.remove('field-invalid');
        }
    });

    function initTooltips() {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            new bootstrap.Tooltip(el);
        });
    }

    function updateMsLabel(dropdown) {
        const label = dropdown.querySelector('.ms-dropdown-label');
        const selected = Array.from(dropdown.querySelectorAll('.ms-option.selected'))
            .map(o => o.querySelector('.ms-option-text').textContent.trim());

        if (selected.length) {
            label.textContent = selected.join(', ');
            label.classList.add('has-value');
        } else {
            label.textContent = label.getAttribute('data-placeholder');
            label.classList.remove('has-value');
        }
    }

    function msSelectedIds(dropdown) {
        return Array.from(dropdown.querySelectorAll('.ms-option.selected'))
            .map(o => parseInt(o.getAttribute('data-id'), 10));
    }

    function wireDayRows() {
        document.querySelectorAll('.schedule-week-row').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const day = btn.getAttribute('data-day');
                fetch('/Schedule/DayContent?day=' + encodeURIComponent(day))
                    .then(r => r.text())
                    .then(html => {
                        document.getElementById('teacherScheduleCard').outerHTML = html;
                        wireDayRows();
                        wireEditOpen();
                        initTooltips();
                    });
            });
        });
    }

    function wireEditOpen() {
        const openBtn = document.getElementById('scheduleEditOpenBtn');
        if (!openBtn) return;

        openBtn.addEventListener('click', function () {
            const day = openBtn.getAttribute('data-day');
            fetch('/Schedule/Edit?day=' + encodeURIComponent(day))
                .then(r => r.text())
                .then(html => {
                    document.getElementById('scheduleEditModalContainer').innerHTML = html;
                    wireEditModal();
                    requestAnimationFrame(function () {
                        document.getElementById('scheduleEditModal').classList.add('open');
                        document.getElementById('scheduleEditBackdrop').classList.add('open');
                    });
                });
        });
    }

    function initTimeRangePicker(picker) {
        if (picker.dataset.wired) return;
        picker.dataset.wired = 'true';

        const startInput = picker.querySelector('.schedule-edit-start-time');
        const endInput = picker.querySelector('.schedule-edit-end-time');
        const displayBtn = picker.querySelector('.time-picker-display');
        const displayText = picker.querySelector('.time-picker-text');
        const dropdown = picker.querySelector('.time-picker-dropdown');
        const hourDisplay = picker.querySelector('.time-picker-hour-display');
        const minuteDisplay = picker.querySelector('.time-picker-minute-display');
        const toggleBtns = picker.querySelectorAll('.time-range-toggle-btn');
        const ampmBtns = picker.querySelectorAll('.time-picker-ampm-btn');

        let start = { hour: 8, minute: 15, period: 'AM' };
        let end = { hour: 9, minute: 0, period: 'AM' };
        let editing = 'start';

        function pad(n) { return n.toString().padStart(2, '0'); }
        function current() { return editing === 'start' ? start : end; }

        function from24(value) {
            const parts = value.split(':').map(Number);
            const h24 = parts[0], minute = parts[1];
            const period = h24 >= 12 ? 'PM' : 'AM';
            let hour = h24 % 12;
            if (hour === 0) hour = 12;
            return { hour: hour, minute: minute, period: period };
        }

        function to24(t) {
            let h24 = t.hour % 12;
            if (t.period === 'PM') h24 += 12;
            return h24;
        }

        if (startInput.value) start = from24(startInput.value);
        if (endInput.value) end = from24(endInput.value);

        function refreshSteppers() {
            const t = current();
            hourDisplay.value = pad(t.hour);
            minuteDisplay.value = pad(t.minute);
            ampmBtns.forEach(b => b.classList.toggle('active', b.getAttribute('data-ampm') === t.period));
        }

        function refreshDisplay() {
            startInput.value = pad(to24(start)) + ':' + pad(start.minute);
            endInput.value = pad(to24(end)) + ':' + pad(end.minute);
            displayText.textContent =
                start.hour + ':' + pad(start.minute) + ' ' + start.period +
                ' – ' +
                end.hour + ':' + pad(end.minute) + ' ' + end.period;
            displayBtn.classList.add('has-value');
            displayBtn.classList.remove('field-invalid');
        }

        if (startInput.value || endInput.value) refreshDisplay();

        function step(field, delta) {
            const t = current();
            if (field === 'hour') {
                t.hour += delta;
                if (t.hour > 12) t.hour = 1;
                if (t.hour < 1) t.hour = 12;
            } else {
                t.minute += delta;
                if (t.minute > 59) t.minute = 0;
                if (t.minute < 0) t.minute = 59;
            }
            refreshSteppers();
            refreshDisplay();
        }

        displayBtn.addEventListener('click', function () {
            document.querySelectorAll('.time-picker-dropdown.open').forEach(function (d) {
                if (d !== dropdown) d.classList.remove('open');
            });
            refreshSteppers();
            const opening = !dropdown.classList.contains('open');
            dropdown.classList.toggle('open');
            if (opening) refreshDisplay();
        });

        toggleBtns.forEach(function (btn) {
            btn.addEventListener('click', function () {
                editing = btn.getAttribute('data-target');
                toggleBtns.forEach(b => b.classList.toggle('active', b === btn));
                refreshSteppers();
            });
        });

        ampmBtns.forEach(function (btn) {
            btn.addEventListener('click', function () {
                current().period = btn.getAttribute('data-ampm');
                refreshSteppers();
                refreshDisplay();
            });
        });

        picker.querySelector('.time-picker-hour-up').addEventListener('click', () => step('hour', 1));
        picker.querySelector('.time-picker-hour-down').addEventListener('click', () => step('hour', -1));
        picker.querySelector('.time-picker-minute-up').addEventListener('click', () => step('minute', 1));
        picker.querySelector('.time-picker-minute-down').addEventListener('click', () => step('minute', -1));

        hourDisplay.addEventListener('wheel', function (e) {
            e.preventDefault();
            step('hour', e.deltaY < 0 ? 1 : -1);
        }, { passive: false });

        minuteDisplay.addEventListener('wheel', function (e) {
            e.preventDefault();
            step('minute', e.deltaY < 0 ? 1 : -1);
        }, { passive: false });

        function wireTyping(el, field, min, max) {
            el.addEventListener('input', function () {
                el.value = el.value.replace(/[^0-9]/g, '').slice(0, 2);
            });
            el.addEventListener('blur', function () {
                let v = parseInt(el.value, 10);
                if (isNaN(v)) { refreshSteppers(); return; }
                if (v < min) v = min;
                if (v > max) v = max;
                current()[field] = v;
                refreshSteppers();
                refreshDisplay();
            });
            el.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') { e.preventDefault(); el.blur(); }
                if (e.key === 'ArrowUp') { e.preventDefault(); step(field, 1); }
                if (e.key === 'ArrowDown') { e.preventDefault(); step(field, -1); }
            });
        }

        wireTyping(hourDisplay, 'hour', 1, 12);
        wireTyping(minuteDisplay, 'minute', 0, 59);
    }

    function wireEditModal() {
        const backdrop = document.getElementById('scheduleEditBackdrop');
        const closeBtn = document.getElementById('scheduleEditCloseBtn');
        const cancelBtn = document.getElementById('scheduleEditCancelBtn');
        const addPeriodBtn = document.getElementById('scheduleAddPeriodBtn');
        const rowsContainer = document.getElementById('scheduleEditRows');
        const form = document.getElementById('scheduleEditForm');

        document.querySelectorAll('.time-range-picker').forEach(initTimeRangePicker);
        document.querySelectorAll('.ms-dropdown').forEach(updateMsLabel);

        function closeModal() {
            document.getElementById('scheduleEditModal').classList.remove('open');
            backdrop.classList.remove('open');
            setTimeout(function () {
                document.getElementById('scheduleEditModalContainer').innerHTML = '';
            }, 300);
        }

        closeBtn.addEventListener('click', closeModal);
        cancelBtn.addEventListener('click', closeModal);
        backdrop.addEventListener('click', closeModal);

        function wireRow(row) {
            row.querySelector('.schedule-edit-remove-btn').addEventListener('click', function () {
                row.remove();
                renumberRows(rowsContainer);
            });
            row.querySelector('.schedule-edit-add-class-btn').addEventListener('click', function () {
                row.classList.remove('is-free');
            });
            row.querySelector('.schedule-edit-make-free-btn').addEventListener('click', function () {
                row.classList.add('is-free');
                row.querySelectorAll('.field-invalid').forEach(el => el.classList.remove('field-invalid'));
            });
        }

        rowsContainer.querySelectorAll('.schedule-edit-row:not(.schedule-edit-row-template)').forEach(wireRow);

        addPeriodBtn.addEventListener('click', function () {
            const existingRows = rowsContainer.querySelectorAll('.schedule-edit-row:not(.schedule-edit-row-template)');
            const nextPeriod = existingRows.length
                ? Math.max(...Array.from(existingRows).map(r => parseInt(r.getAttribute('data-period'), 10))) + 1
                : 1;

            const suggested = suggestTimeForPeriod(nextPeriod, rowsContainer);

            const templateRow = rowsContainer.querySelector('.schedule-edit-row-template');
            const clone = templateRow.cloneNode(true);
            clone.classList.remove('schedule-edit-row-template');
            clone.style.display = '';
            clone.setAttribute('data-period', nextPeriod);
            clone.querySelector('.schedule-edit-period-badge').textContent = nextPeriod;
            clone.querySelector('.schedule-edit-start-time').value = suggested.start;
            clone.querySelector('.schedule-edit-end-time').value = suggested.end;

            rowsContainer.appendChild(clone);
            wireRow(clone);

            clone.querySelectorAll('.time-range-picker').forEach(function (picker) {
                delete picker.dataset.wired;
                initTimeRangePicker(picker);
            });
            clone.querySelectorAll('.ms-dropdown').forEach(updateMsLabel);

            clone.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });

        form.addEventListener('submit', function (e) {
            e.preventDefault();

            const day = form.getAttribute('data-day');
            let hasError = false;

            const rows = Array.from(rowsContainer.querySelectorAll('.schedule-edit-row:not(.schedule-edit-row-template)')).map(function (row) {
                const isFree = row.classList.contains('is-free');

                const startInput = row.querySelector('.schedule-edit-start-time');
                const endInput = row.querySelector('.schedule-edit-end-time');
                const displayBtn = row.querySelector('.time-picker-display');
                const subjectDd = row.querySelector('.schedule-edit-subject-ms');
                const gradeDd = row.querySelector('.schedule-edit-grade-ms');
                const roomInput = row.querySelector('.schedule-edit-room-input');
                const notesInput = row.querySelector('.schedule-edit-notes-input');

                const subjectIds = msSelectedIds(subjectDd);
                const gradeIds = msSelectedIds(gradeDd);

                if (!startInput.value || !endInput.value) {
                    displayBtn.classList.add('field-invalid');
                    hasError = true;
                }

                if (!isFree) {
                    if (!subjectIds.length) { subjectDd.classList.add('field-invalid'); hasError = true; }
                    if (!gradeIds.length) { gradeDd.classList.add('field-invalid'); hasError = true; }
                    if (!roomInput.value.trim()) { roomInput.classList.add('field-invalid'); hasError = true; }
                }

                return {
                    periodNumber: parseInt(row.getAttribute('data-period'), 10),
                    startTime: startInput.value,
                    endTime: endInput.value,
                    isFree: isFree,
                    subjectIds: isFree ? [] : subjectIds,
                    gradeIds: isFree ? [] : gradeIds,
                    roomNumber: isFree ? null : roomInput.value.trim(),
                    notes: notesInput ? notesInput.value : null
                };
            });

            if (hasError) {
                const firstBad = rowsContainer.querySelector('.field-invalid');
                if (firstBad) firstBad.scrollIntoView({ behavior: 'smooth', block: 'center' });
                return;
            }

            fetch('/Schedule/Save', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ day: day, rows: rows })
            })
                .then(r => r.json())
                .then(function () {
                    closeModal();
                    return fetch('/Schedule/DayContent?day=' + encodeURIComponent(day));
                })
                .then(r => r.text())
                .then(html => {
                    document.getElementById('teacherScheduleCard').outerHTML = html;
                    wireDayRows();
                    wireEditOpen();
                    initTooltips();
                });
        });
    }
});