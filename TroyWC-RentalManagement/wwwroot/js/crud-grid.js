// Reusable grid + CRUD modal behaviour, driven by data attributes.
// See Views/Shared/_Grid.cshtml and Views/Shared/_CrudModal.cshtml.
//
//   <div data-grid data-grid-url="...">     grid container; reloaded from data-grid-url after a save
//   <a data-grid-nav href="...">            sort/page link; swaps the grid in place
//   <button data-crud-modal="url"           loads url into #crud-modal
//           data-grid-target="#selector">   grid to refresh (defaults to the closest [data-grid])
//
// Forms inside the modal are posted with fetch. The server answers with JSON { success: true }
// to close the modal and refresh the grid, or with the form partial again to show errors.
(function () {
    'use strict';

    let activeGrid = null;

    function modalElement() {
        return document.getElementById('crud-modal');
    }

    async function request(url, options = {}) {
        const response = await fetch(url, {
            credentials: 'same-origin',
            ...options,
            headers: { 'X-Requested-With': 'XMLHttpRequest', ...options.headers }
        });

        // Signed out or lost the role: reload so the server can redirect to login.
        if (response.status === 401 || response.status === 403) {
            window.location.reload();
            throw new Error('unauthorized');
        }
        if (!response.ok) {
            throw new Error(`Request to ${url} failed with ${response.status}`);
        }
        return response;
    }

    async function loadGrid(grid, url) {
        const response = await request(url);
        grid.innerHTML = await response.text();
        grid.dataset.gridUrl = url;
    }

    function setModalContent(html) {
        const modal = modalElement();
        modal.querySelector('.modal-content').innerHTML = html;

        const form = modal.querySelector('form');
        if (form && window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
            jQuery.validator.unobtrusive.parse(form);
        }
        return modal;
    }

    async function openModal(url, grid) {
        const response = await request(url);
        activeGrid = grid;
        const modal = setModalContent(await response.text());
        bootstrap.Modal.getOrCreateInstance(modal).show();
    }

    async function submitForm(form) {
        if (window.jQuery && jQuery.fn.valid && !jQuery(form).valid()) {
            return;
        }

        const submit = form.querySelector('[type="submit"]');
        if (submit) submit.disabled = true;

        try {
            const response = await request(form.action, { method: 'POST', body: new FormData(form) });
            const contentType = response.headers.get('Content-Type') || '';

            if (contentType.includes('application/json')) {
                const result = await response.json();
                if (result.success) {
                    bootstrap.Modal.getOrCreateInstance(modalElement()).hide();
                    if (activeGrid) {
                        await loadGrid(activeGrid, activeGrid.dataset.gridUrl);
                    }
                }
                return;
            }

            setModalContent(await response.text());
        } finally {
            if (submit && submit.isConnected) submit.disabled = false;
        }
    }

    function showError(error) {
        if (error.message === 'unauthorized') return;
        console.error(error);
        alert('Something went wrong. Please try again.');
    }

    document.addEventListener('click', (event) => {
        const nav = event.target.closest('[data-grid] a[data-grid-nav]');
        if (nav) {
            event.preventDefault();
            if (nav.closest('.disabled')) return;

            const grid = nav.closest('[data-grid]');
            loadGrid(grid, nav.href)
                .then(() => history.replaceState(null, '', nav.href))
                .catch(showError);
            return;
        }

        const trigger = event.target.closest('[data-crud-modal]');
        if (trigger) {
            event.preventDefault();
            const grid = trigger.dataset.gridTarget
                ? document.querySelector(trigger.dataset.gridTarget)
                : trigger.closest('[data-grid]');
            openModal(trigger.dataset.crudModal, grid).catch(showError);
        }
    });

    document.addEventListener('submit', (event) => {
        const form = event.target;
        if (!form.closest('#crud-modal')) return;

        event.preventDefault();
        submitForm(form).catch(showError);
    });
})();
