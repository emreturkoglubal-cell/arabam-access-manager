// AccessManager Web - client scripts

// Personel listesi: satırın tamamına tıklanınca detay sayfasına git
(function () {
  document.querySelectorAll('.am-table-row-clickable[data-href]').forEach(function (row) {
    row.addEventListener('click', function (e) {
      if (e.target.closest('a[href], button')) return;
      var href = row.getAttribute('data-href');
      if (href && href !== '#') window.location.href = href;
    });
    row.addEventListener('keydown', function (e) {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        if (e.target.closest('a[href], button')) return;
        var href = row.getAttribute('data-href');
        if (href && href !== '#') window.location.href = href;
      }
    });
  });
})();

// Arama input'larına otomatik debounce: yazı yazıldıktan bir süre sonra form otomatik submit edilir
(function () {
  function debounce(func, wait) {
    var timeout;
    return function executedFunction() {
      var context = this;
      var args = arguments;
      var later = function () {
        timeout = null;
        func.apply(context, args);
      };
      clearTimeout(timeout);
      timeout = setTimeout(later, wait);
    };
  }

  // Tüm formlardaki type="search" input'larını bul ve debounce ekle
  document.querySelectorAll('form input[type="search"]').forEach(function (searchInput) {
    var form = searchInput.closest('form');
    if (!form) return;

    // Debounce süresi: 500ms (yarım saniye)
    var debouncedSubmit = debounce(function () {
      form.submit();
    }, 500);

    // Input değiştiğinde debounce'u tetikle
    searchInput.addEventListener('input', debouncedSubmit);

    // Enter'a basıldığında hemen submit et (debounce beklemeden)
    searchInput.addEventListener('keydown', function (e) {
      if (e.key === 'Enter') {
        e.preventDefault();
        form.submit();
      }
    });
  });
})();
