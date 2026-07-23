document.addEventListener('DOMContentLoaded', function () {
    initTooltips();
    wireDayRows();
    wireEditOpen();

    document.addEventListener('click', function (e) {
        if (!e.target.closest('.time-range-picker')) {
            document.querySelectorAll('.time-picker-dropdown.open').forEach(d => d.classList.remove('open'));
        }
    });

    function initTooltips() {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            new bootstrap.Tooltip(el);
        });
    }

    function wireDayRows() {
        document.querySelectorAll('.schedule-week-row').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const day = btn.getAttribute('data-day');
                fetch('/Teacher/ScheduleDayContent?day=' + encodeURIComponent(day))
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
            fetch('/Teacher/ScheduleEdit?day=' + encodeURIComponent(day))
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

        let start = { hour: 9, minute: 0, period: 'AM' };
        let end = { hour: 9, minute: 45, period: 'AM' };
        let editing = 'start';

        function from24(value) {
            const p = value.split(':').map(Number);
            const h24 = p[0], minute = p[1];
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

        function pad(n) { return n.toString().padStart(2, '0'); }
        function current() { return editing === 'start' ? start : end; }

        function refreshSteppers() {
            const t = current();
            hourDisplay.textContent = t.hour;
            minuteDisplay.textContent = pad(t.minute);
            ampmBtns.forEach(b => b.classList.toggle('active', b.getAttribute('data-ampm') === t.period));
        }

        function refreshDisplay() {
            startInput.value = pad(to24(start)) + ':' + pad(start.minute);
            endInput.value = pad(to24(end)) + ':' + pad(end.minute);
            displayText.textContent = start.hour + ':' + pad(start.minute) + ' ' + start.period + ' – ' + end.hour + ':' + pad(end.minute) + ' ' + end.period;
            displayBtn.classList.add('has-value');
        }

        if (startInput.value || endInput.value) refreshDisplay();

        displayBtn.addEventListener('click', function () {
            document.querySelectorAll('.time-picker-dropdown.open').forEach(function (d) {
                if (d !== dropdown) d.classList.remove('open');
            });
            refreshSteppers();
            dropdown.classList.toggle('open');
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

        picker.querySelector('.time-picker-hour-up').addEventListener('click', function () {
            const t = current();
            t.hour = t.hour === 12 ? 1 : t.hour + 1;
            refreshSteppers();
            refreshDisplay();
        });
        picker.querySelector('.time-picker-hour-down').addEventListener('click', function () {
            const t = current();
            t.hour = t.hour === 1 ? 12 : t.hour - 1;
            refreshSteppers();
            refreshDisplay();
        });
        picker.querySelector('.time-picker-minute-up').addEventListener('click', function () {
            const t = current();
            t.minute = t.minute === 59 ? 0 : t.minute + 1;
            refreshSteppers();
            refreshDisplay();
        });
        picker.querySelector('.time-picker-minute-down').addEventListener('click', function () {
            const t = current();
            t.minute = t.minute === 0 ? 59 : t.minute - 1;
            refreshSteppers();
            refreshDisplay();
        });
    }

    function wireEditModal() {
        const backdrop = document.getElementById('scheduleEditBackdrop');
        const closeBtn = document.getElementById('scheduleEditCloseBtn');
        const cancelBtn = document.getElementById('scheduleEditCancelBtn');
        const addPeriodBtn = document.getElementById('scheduleAddPeriodBtn');
        const rowsContainer = document.getElementById('scheduleEditRows');
        const form = document.getElementById('scheduleEditForm');

        document.querySelectorAll('.time-range-picker').forEach(initTimeRangePicker);

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
            });
            row.querySelector('.schedule-edit-add-class-btn').addEventListener('click', function () {
                row.classList.remove('is-free');
            });
            row.querySelector('.schedule-edit-make-free-btn').addEventListener('click', function () {
                row.classList.add('is-free');
            });
        }

        rowsContainer.querySelectorAll('.schedule-edit-row:not(.schedule-edit-row-template)').forEach(wireRow);

        addPeriodBtn.addEventListener('click', function () {
            const existingRows = rowsContainer.querySelectorAll('.schedule-edit-row:not(.schedule-edit-row-template)');
            const nextPeriod = existingRows.length
                ? Math.max(...Array.from(existingRows).map(r => parseInt(r.getAttribute('data-period'), 10))) + 1
                : 1;

            const templateRow = rowsContainer.querySelector('.schedule-edit-row-template');
            const clone = templateRow.cloneNode(true);
            clone.classList.remove('schedule-edit-row-template');
            clone.style.display = '';
            clone.setAttribute('data-period', nextPeriod);
            clone.querySelector('.schedule-edit-period-badge').textContent = nextPeriod;

            rowsContainer.appendChild(clone);
            wireRow(clone);
            clone.querySelectorAll('.time-range-picker').forEach(function (picker) {
                delete picker.dataset.wired;
                initTimeRangePicker(picker);
            });
            clone.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });

        form.addEventListener('submit', function (e) {
            e.preventDefault();

            const day = form.getAttribute('data-day');
            const rows = Array.from(rowsContainer.querySelectorAll('.schedule-edit-row:not(.schedule-edit-row-template)')).map(function (row) {
                const isFree = row.classList.contains('is-free');

                const startInput = row.querySelector('.schedule-edit-start-time');
                const endInput = row.querySelector('.schedule-edit-end-time');
                const startTime = startInput.value || '09:00';
                const endTime = endInput.value || '09:45';

                const subjectSelect = row.querySelector('.schedule-edit-subject-select');
                const classSelect = row.querySelector('.schedule-edit-class-select');
                const roomInput = row.querySelector('.schedule-edit-room-input');
                const notesInput = row.querySelector('.schedule-edit-notes-input');

                return {
                    periodNumber: parseInt(row.getAttribute('data-period'), 10),
                    startTime: startTime,
                    endTime: endTime,
                    isFree: isFree,
                    subjectId: subjectSelect && subjectSelect.value ? parseInt(subjectSelect.value, 10) : null,
                    schoolClassId: classSelect && classSelect.value ? parseInt(classSelect.value, 10) : null,
                    roomNumber: roomInput ? roomInput.value : null,
                    notes: notesInput ? notesInput.value : null
                };
            });

            fetch('/Teacher/ScheduleSave', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ day: day, rows: rows })
            })
                .then(r => r.json())
                .then(function () {
                    closeModal();
                    return fetch('/Teacher/ScheduleDayContent?day=' + encodeURIComponent(day));
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