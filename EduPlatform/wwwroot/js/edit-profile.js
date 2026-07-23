document.addEventListener('DOMContentLoaded', function () {
    const openBtn = document.getElementById('editProfileOpenBtn');
    const modal = document.getElementById('editProfileModal');
    const backdrop = document.getElementById('editProfileBackdrop');
    const closeBtn = document.getElementById('editProfileCloseBtn');
    const cancelBtn = document.getElementById('editProfileCancelBtn');

    function openModal() {
        modal.classList.add('open');
        backdrop.classList.add('open');
    }

    function closeModal() {
        modal.classList.remove('open');
        backdrop.classList.remove('open');
    }

    if (openBtn) openBtn.addEventListener('click', openModal);
    if (closeBtn) closeBtn.addEventListener('click', closeModal);
    if (cancelBtn) cancelBtn.addEventListener('click', closeModal);
    if (backdrop) backdrop.addEventListener('click', closeModal);

    const form = document.getElementById('editProfileForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            closeModal();
        });
    }

    const addPhotoBtn = document.getElementById('editProfileAddPhotoBtn');
    const deletePhotoBtn = document.getElementById('editProfileDeletePhotoBtn');
    const photoInput = document.getElementById('editProfilePhotoInput');
    const avatarPreview = document.getElementById('editProfileAvatarPreview');

    if (addPhotoBtn && photoInput) {
        addPhotoBtn.addEventListener('click', function () {
            photoInput.click();
        });

        photoInput.addEventListener('change', function () {
            const file = photoInput.files && photoInput.files[0];
            if (!file) return;
            const reader = new FileReader();
            reader.onload = function (e) {
                avatarPreview.style.backgroundImage = 'url(' + e.target.result + ')';
            };
            reader.readAsDataURL(file);
        });
    }

    if (deletePhotoBtn && avatarPreview) {
        deletePhotoBtn.addEventListener('click', function () {
            avatarPreview.style.backgroundImage = 'none';
            if (photoInput) photoInput.value = '';
        });
    }
});