document.addEventListener('DOMContentLoaded', function () {
    EduFilterBar.init({
        items: '.group-card',
        onApply: function () {
            const count = Array.from(document.querySelectorAll('.group-card'))
                .filter(function (el) { return el.style.display !== 'none'; }).length;
            const el = document.getElementById('groupsCount');
            if (el) el.textContent = count;
        }
    });
});
