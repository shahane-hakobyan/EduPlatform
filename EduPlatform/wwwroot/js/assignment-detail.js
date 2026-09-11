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

    // Grading mode tabs (Point vs Formative)
    document.querySelectorAll('[data-grading-tabs]').forEach(function (tabs) {
        const modal = tabs.closest('.answer-modal');
        tabs.querySelectorAll('.answer-grading-tab').forEach(function (tab) {
            tab.addEventListener('click', function () {
                tabs.querySelectorAll('.answer-grading-tab').forEach(function (t) { t.classList.remove('active'); });
                tab.classList.add('active');

                const mode = tab.getAttribute('data-mode');
                modal.querySelectorAll('.answer-grading-panel').forEach(function (panel) {
                    panel.style.display = panel.getAttribute('data-panel') === mode ? 'block' : 'none';
                });
            });
        });
    });

    // Point picker
    document.querySelectorAll('.answer-points-grid').forEach(function (grid) {
        grid.querySelectorAll('.answer-point-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                grid.querySelectorAll('.answer-point-btn').forEach(function (b) { b.classList.remove('active'); });
                btn.classList.add('active');
            });
        });
    });

    // Save grade
    document.querySelectorAll('[data-save-grade]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const modal = btn.closest('.answer-modal');
            const submissionId = btn.getAttribute('data-submission-id');
            const activeTab = modal.querySelector('.answer-grading-tab.active');
            const mode = activeTab ? activeTab.getAttribute('data-mode') : 'Point';

            const payload = { submissionId: parseInt(submissionId, 10), gradingMode: mode };

            if (mode === 'Point') {
                const activePoint = modal.querySelector('.answer-point-btn.active');
                payload.score = activePoint ? parseInt(activePoint.getAttribute('data-score'), 10) : null;
            } else {
                const textarea = modal.querySelector('.answer-protocol-textarea');
                payload.protocolText = textarea ? textarea.value : '';
            }

            fetch('/Teacher/SaveGrade', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            }).then(function () {
                closeModal(modal.id);
            });
        });
    });

    // --- Add pupils modal ---
    const addPupilsSubmitBtn = document.getElementById('addPupilsSubmitBtn');
    const addPupilsSelectedCount = document.getElementById('addPupilsSelectedCount');

    function refreshAddPupilsCounts() {
        const modal = document.getElementById('addPupilsModal');
        if (!modal) return;

        const checked = modal.querySelectorAll('[data-pupil-checkbox]:checked:not(:disabled)');
        addPupilsSelectedCount.textContent = checked.length;
        addPupilsSubmitBtn.disabled = checked.length === 0;

        modal.querySelectorAll('.ca-class-group').forEach(function (group) {
            const groupChecked = group.querySelectorAll('[data-pupil-checkbox]:checked:not(:disabled)').length;
            const groupTotal = group.querySelectorAll('[data-pupil-checkbox]').length;
            const badge = group.querySelector('[data-selected-badge]');
            badge.textContent = groupChecked;
            badge.classList.toggle('show', groupChecked > 0);
            group.querySelector('[data-progress-label]').textContent = groupChecked + ' / ' + groupTotal;
        });
    }

    const addPupilsModal = document.getElementById('addPupilsModal');
    if (addPupilsModal) {
        addPupilsModal.querySelectorAll('[data-pupil-checkbox]').forEach(function (cb) {
            cb.addEventListener('change', refreshAddPupilsCounts);
        });

        addPupilsModal.querySelectorAll('.ca-class-group-head').forEach(function (head) {
            head.addEventListener('click', function () {
                head.closest('.ca-class-group').classList.toggle('open');
            });
        });

        addPupilsModal.querySelectorAll('.ca-select-all-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const group = btn.closest('.ca-class-group');
                const boxes = group.querySelectorAll('[data-pupil-checkbox]:not(:disabled)');
                const allChecked = Array.from(boxes).every(function (b) { return b.checked; });
                boxes.forEach(function (b) { b.checked = !allChecked; });
                refreshAddPupilsCounts();
            });
        });

        const clearAllBtn = document.getElementById('addPupilsClearAllBtn');
        if (clearAllBtn) {
            clearAllBtn.addEventListener('click', function () {
                addPupilsModal.querySelectorAll('[data-pupil-checkbox]:checked:not(:disabled)').forEach(function (cb) { cb.checked = false; });
                refreshAddPupilsCounts();
            });
        }

        const search = document.getElementById('addPupilsSearch');
        if (search) {
            search.addEventListener('input', function () {
                const q = search.value.trim().toLowerCase();
                addPupilsModal.querySelectorAll('.ca-pupil-item').forEach(function (item) {
                    const name = item.querySelector('.ca-pupil-name').textContent.toLowerCase();
                    item.style.display = name.includes(q) ? '' : 'none';
                });
            });
        }

        addPupilsSubmitBtn.addEventListener('click', function () {
            const scriptTag = document.querySelector('script[src*="assignment-detail.js"]');
            const assignmentId = scriptTag ? scriptTag.getAttribute('data-assignment-id') : null;

            const pupilNames = Array.from(addPupilsModal.querySelectorAll('[data-pupil-checkbox]:checked:not(:disabled)'))
                .map(function (cb) { return cb.value; });

            fetch('/Teacher/AddPupilsToAssignment', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ assignmentId: parseInt(assignmentId, 10), pupilNames: pupilNames })
            }).then(function () {
                closeModal('addPupilsModal');
                location.reload();
            });
        });
    }
});