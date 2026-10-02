// Residence history step of the rental application (Views/Shared/Components/ResidenceHistory).
// The prior-residence modal lives inside the wizard form, so nothing here talks to the server:
//
//   [data-residence-add]          opens the modal empty
//   [data-residence-edit="i"]     opens the modal filled from row i's hidden Residences[i].* inputs
//   #residence-modal[data-show-modal="true"]   set by the server to reopen the modal with its errors
(function () {
    'use strict';

    const modalElement = document.getElementById('residence-modal');
    if (!modalElement) return;

    const form = modalElement.closest('form');
    const draftPrefix = 'ResidenceDraft.';

    function draftFields() {
        return modalElement.querySelectorAll(`[name^="${draftPrefix}"]`);
    }

    function clearErrors() {
        modalElement.querySelectorAll('.field-validation-error').forEach(span => { span.textContent = ''; });
        modalElement.querySelectorAll('.input-validation-error').forEach(input => input.classList.remove('input-validation-error'));
    }

    function open(index, valueFor) {
        draftFields().forEach(field => { field.value = valueFor(field.name.slice(draftPrefix.length)); });
        form.elements['ResidenceDraftIndex'].value = index;
        clearErrors();
        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }

    document.addEventListener('click', (event) => {
        if (event.target.closest('[data-residence-add]')) {
            open('', () => '');
            return;
        }

        const edit = event.target.closest('[data-residence-edit]');
        if (edit) {
            const index = edit.dataset.residenceEdit;
            open(index, name => form.elements[`Residences[${index}].${name}`]?.value ?? '');
        }
    });

    // Enter would trigger the first submit button in the form (a row's Remove), so route it explicitly.
    form.addEventListener('keydown', (event) => {
        if (event.key !== 'Enter' || event.target.tagName !== 'INPUT') return;

        event.preventDefault();
        if (modalElement.contains(event.target)) {
            modalElement.querySelector('[data-residence-save]').click();
        }
    });

    if (modalElement.dataset.showModal === 'true') {
        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }
})();
