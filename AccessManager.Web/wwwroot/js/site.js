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
