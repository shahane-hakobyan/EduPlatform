document.addEventListener('DOMContentLoaded', function () {
    function openModal(id) {
        const modal = document.getElementById(id);
        const backdrop = document.getElementById(id + '-backdrop');
        if (modal) modal.classList.add('open');
        if (backdrop) backdrop.classList.add('open');
    }

    function closeModal(id) {
        const modal = document.getElementById(id);
        const backdrop = document.getElementById(id + '-backdrop');
        if (modal) modal.classList.remove('open');
        if (backdrop) backdrop.classList.remove('open');
    }

    document.querySelectorAll('[data-open-modal]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            openModal(btn.getAttribute('data-open-modal'));
        });
    });

    document.querySelectorAll('[data-close-modal]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            closeModal(btn.getAttribute('data-close-modal'));
        });
    });

    document.querySelectorAll('.answer-modal-backdrop').forEach(function (backdrop) {
        backdrop.addEventListener('click', function () {
            closeModal(backdrop.id.replace('-backdrop', ''));
        });
    });

    // Point picker (կրկին սեղմելով՝ միավորը հանվում է)
    document.querySelectorAll('.answer-points-grid').forEach(function (grid) {
        grid.querySelectorAll('.answer-point-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const wasActive = btn.classList.contains('active');
                grid.querySelectorAll('.answer-point-btn').forEach(function (b) { b.classList.remove('active'); });
                if (!wasActive) btn.classList.add('active');
            });
        });
    });

    // Save grade
    document.querySelectorAll('[data-save-grade]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const modal = btn.closest('.answer-modal');
            const type = modal.getAttribute('data-grading-type');
            const errBox = modal.querySelector('.answer-error');
            const feedback = modal.querySelector('.answer-protocol-textarea').value.trim();
            const payload = { submissionId: parseInt(btn.getAttribute('data-submission-id'), 10), feedback: feedback, score: null, maxScore: null };

            function fail(msg) { errBox.textContent = msg; errBox.style.display = 'block'; }
            errBox.style.display = 'none';

            if (type === 'CurrentScore') {
                const active = modal.querySelector('.answer-point-btn.active');
                payload.score = active ? parseInt(active.getAttribute('data-score'), 10) : null;
            } else if (type === 'Diagnostic') {
                // «Առավելագույն միավոր» և «Նախնական միավոր»՝ ազատ թվեր (ընտրության դաշտ չկա)
                const maxVal = modal.querySelector('[data-max-score]').value;
                const initVal = modal.querySelector('[data-initial-score]').value;
                payload.maxScore = maxVal === '' ? null : parseFloat(maxVal);
                payload.score = initVal === '' ? null : parseFloat(initVal);

                if (payload.maxScore !== null && !(payload.maxScore > 0)) return fail(errBox.getAttribute('data-msg') || '*');
                if (payload.score !== null && payload.score < 0) return fail(errBox.getAttribute('data-msg') || '*');
                if (payload.score !== null && payload.maxScore !== null && payload.score > payload.maxScore) {
                    return fail(errBox.getAttribute('data-msg-exceeds') || '*');
                }
            }

            // Ընթացիկ՝ միավոր և/կամ հետադարձ կապ (առնվազն մեկը), Հայտորոշիչ՝ երեք դաշտերից առնվազն մեկը, Ձևավորող՝ հետադարձ կապը պարտադիր է
            const needsFeedbackOnly = type === 'Formative';
            const nothingFilled = payload.score === null && payload.maxScore === null && !feedback;
            if ((needsFeedbackOnly && !feedback) || (!needsFeedbackOnly && nothingFilled)) {
                return fail(errBox.getAttribute('data-msg') || '*');
            }

            btn.disabled = true;
            fetch('/Repository/Assignments/Grade', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            }).then(function (r) {
                if (r.ok) { location.reload(); }
                else { btn.disabled = false; fail('Error ' + r.status); }
            }).catch(function () { btn.disabled = false; fail('Error'); });
        });
    });

    // Archive
    const archiveBtn = document.getElementById('adArchiveBtn');
    if (archiveBtn) {
        archiveBtn.addEventListener('click', async function () {
            if (!confirm(archiveBtn.getAttribute('data-confirm-text'))) return;

            archiveBtn.disabled = true;
            try {
                const res = await fetch('/Repository/Assignments/Archive', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ id: parseInt(archiveBtn.getAttribute('data-assignment-id'), 10) })
                });
                if (res.ok) {
                    window.location.href = archiveBtn.getAttribute('data-redirect-url');
                    return;
                }
            } catch (e) { }
            archiveBtn.disabled = false;
        });
    }

    // Change deadline
    const dueInput = document.getElementById('adDueInput');
    if (dueInput) {
        const savedBox = document.getElementById('adDueSaved');
        dueInput.addEventListener('change', async function () {
            const value = dueInput.value;
            const start = dueInput.getAttribute('data-start');
            if (!value || (start && value.slice(0, 10) < start.slice(0, 10))) {
                alert(dueInput.getAttribute('data-invalid-msg'));
                dueInput.value = dueInput.getAttribute('data-original');
                return;
            }

            try {
                const res = await fetch('/Repository/Assignments/ChangeDueDate', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ id: parseInt(dueInput.getAttribute('data-assignment-id'), 10), dueDate: value })
                });
                if (res.ok) {
                    dueInput.setAttribute('data-original', value);
                    if (savedBox) {
                        savedBox.style.display = '';
                        setTimeout(function () { savedBox.style.display = 'none'; }, 2500);
                    }
                    return;
                }
            } catch (e) { }
            alert(dueInput.getAttribute('data-invalid-msg'));
            dueInput.value = dueInput.getAttribute('data-original');
        });
    }

    // --- Pupil list filters ---
    (function () {
        const rows = document.querySelectorAll('.ad-submission-row');
        const search = document.getElementById('adSearch');
        const grade = document.getElementById('adGradeFilter');
        const group = document.getElementById('adGroupFilter');
        const status = document.getElementById('adStatusFilter');
        if (!search) return;

        function apply() {
            const q = search.value.trim().toLowerCase();
            rows.forEach(function (r) {
                const ok =
                    (!q || r.dataset.name.includes(q)) &&
                    (!grade.value || r.dataset.grade === grade.value) &&
                    (!group || !group.value || (r.dataset.group || '').split('|').indexOf(group.value) !== -1) &&
                    (!status.value || r.dataset.status === status.value);
                r.style.display = ok ? '' : 'none';
            });
        }

        [search, grade, group, status].forEach(function (el) {
            if (el) el.addEventListener(el === search ? 'input' : 'change', apply);
        });
    })();

    // --- Add pupils modal (ընտրության տրամաբանությունը՝ pupil-picker.js) ---
    const addPupilsSubmitBtn = document.getElementById('addPupilsSubmitBtn');
    const addPupilsModal = document.getElementById('addPupilsModal');

    if (addPupilsModal) {
        const picker = EduPupilPicker.init(addPupilsModal.querySelector('[data-pupil-picker]'), {
            onChange: function (total) { addPupilsSubmitBtn.disabled = total === 0; }
        });

        addPupilsSubmitBtn.addEventListener('click', function () {
            const scriptTag = document.querySelector('script[src*="assignment-detail.js"]');
            const assignmentId = scriptTag ? scriptTag.getAttribute('data-assignment-id') : null;

            const pupilIds = picker.selectedIds();

            addPupilsSubmitBtn.disabled = true;
            fetch('/Repository/Assignments/AddPupils', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ assignmentId: parseInt(assignmentId, 10), pupilIds: pupilIds })
            }).then(function (r) {
                if (r.ok) {
                    closeModal('addPupilsModal');
                    location.reload();
                } else {
                    addPupilsSubmitBtn.disabled = false;
                    alert('Error ' + r.status);
                }
            }).catch(function () { addPupilsSubmitBtn.disabled = false; });
        });
    }
});