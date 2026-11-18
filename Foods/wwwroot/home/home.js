// Foods - Home page interactions (favorite toggle and meal quantity stepper)
(function () {
    document.querySelectorAll('.meal .fav').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var on = btn.classList.toggle('is-on');
            btn.setAttribute('aria-pressed', on ? 'true' : 'false');
        });
    });

    document.querySelectorAll('.meal .qty').forEach(function (box) {
        var val = box.querySelector('.qty-val');
        function set(n) {
            n = Math.max(0, n);
            box.dataset.qty = n;
            val.textContent = n;
            box.classList.toggle('has-qty', n > 0);
        }
        box.querySelector('.qty-plus').addEventListener('click', function () { set(+box.dataset.qty + 1); });
        box.querySelector('.qty-minus').addEventListener('click', function () { set(+box.dataset.qty - 1); });
    });
})();
