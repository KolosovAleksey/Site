(function () {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    async function post(url, data) {
        const res = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: new URLSearchParams(data)
        });
        if (res.status === 429) throw new Error('Слишком много запросов, подождите.');
        if (!res.ok) throw new Error('HTTP ' + res.status);
        return res.text();
    }

    async function refreshBadge() {
        const res = await fetch('/Cart/Count');
        if (res.ok) {
            const { count } = await res.json();
            document.getElementById('cartBadge').textContent = count;
        }
    }

    async function refreshSummary() {
        const res = await fetch('/Cart/Summary');
        if (res.ok) {
            document.getElementById('cartSummary').innerHTML = await res.text();
        }
    }

    // Начальная загрузка сайдбара корзины
    document.addEventListener('DOMContentLoaded', () => {
        refreshSummary();
        refreshBadge();
    });

    // AJAX-фильтр каталога
    const filterForm = document.getElementById('filterForm');
    if (filterForm) {
        filterForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const params = new URLSearchParams(new FormData(filterForm));
            const res = await fetch('/Catalog/Grid?' + params.toString(), {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (res.ok) {
                document.getElementById('productGrid').innerHTML = await res.text();
            }
        });
    }

    // Клики по AJAX-кнопкам (глобально)
    document.addEventListener('click', async (e) => {
        const addBtn = e.target.closest('.add-to-cart');
        if (addBtn) {
            try {
                const html = await post('/Cart/Add', {
                    productId: addBtn.dataset.id,
                    quantity: 1
                });
                document.getElementById('cartSummary').innerHTML = html;
                refreshBadge();
            } catch (err) { alert(err.message); }
            return;
        }

        const removeBtn = e.target.closest('.remove-item');
        if (removeBtn) {
            try {
                const html = await post('/Cart/Remove', { id: removeBtn.dataset.id });
                document.getElementById('cartSummary').innerHTML = html;
                refreshBadge();
            } catch (err) { alert(err.message); }
        }
    });
})();