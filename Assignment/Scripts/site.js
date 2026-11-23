// Site-wide behaviour. Loaded by Views/Shared/_Layout.cshtml.
(function () {
    'use strict';

    // Flash messages set with TempData["SuccessMessage"] / TempData["ErrorMessage"].
    document.addEventListener('DOMContentLoaded', function () {
        var flash = document.getElementById('flash');
        if (flash && window.toastr) {
            if (flash.dataset.success) toastr.success(flash.dataset.success, 'Success');
            if (flash.dataset.error) toastr.error(flash.dataset.error, 'Error');
        }
    });

    // Loading overlay for AJAX calls and form posts.
    if (window.jQuery) {
        var overlay = document.getElementById('loaderOverlay');
        var show = function () { if (overlay) overlay.hidden = false; };
        var hide = function () { if (overlay) overlay.hidden = true; };
        $(document).ajaxStart(show).ajaxStop(hide);
        $(document).on('submit', 'form', function () {
            var $form = $(this);
            if (!$form.data('validator') || $form.valid()) show();
        });
        window.addEventListener('pageshow', hide);
    }

    // Buttons with data-confirm ask before submitting their form.
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-confirm]');
        if (!btn) return;
        e.preventDefault();
        var form = btn.form;
        var message = btn.getAttribute('data-confirm');
        var withReason = btn.hasAttribute('data-confirm-reason');
        if (window.Swal) {
            Swal.fire({
                title: 'Are you sure?', text: message, icon: 'warning', showCancelButton: true,
                input: withReason ? 'text' : undefined, inputPlaceholder: 'Reason (optional)',
                confirmButtonText: btn.textContent.trim() || 'Yes',
            }).then(function (result) {
                if (!result.isConfirmed) return;
                if (withReason && form.elements.reason) form.elements.reason.value = result.value || '';
                form.submit();
            });
        } else if (window.confirm(message)) {
            form.submit();
        }
    });
})();
