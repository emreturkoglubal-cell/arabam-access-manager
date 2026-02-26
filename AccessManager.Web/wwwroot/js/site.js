// AccessManager Web - client scripts

// Türkiye telefon maskesi: +90 (XXX) XXX XX XX — "+90 (" sabit yazılı, sadece 10 rakam yazılır
(function () {
  var PREFIX = '+90 (';
  var REQUIRED_DIGITS = 10;

  function getDigits(str) {
    return (str || '').replace(/\D/g, '');
  }

  // Sadece "+90 (" sonrasındaki rakamlar (ülke kodu 90 sayılmaz)
  function getDigitsAfterPrefix(value) {
    if (!value || value.length <= PREFIX.length) return '';
    return getDigits(value.slice(PREFIX.length));
  }

  function formatPhone(digits) {
    digits = digits.slice(0, REQUIRED_DIGITS);
    if (digits.length <= 3) return PREFIX + digits;
    if (digits.length <= 6) return PREFIX + digits.slice(0, 3) + ') ' + digits.slice(3);
    if (digits.length <= 8) return PREFIX + digits.slice(0, 3) + ') ' + digits.slice(3, 6) + ' ' + digits.slice(6);
    return PREFIX + digits.slice(0, 3) + ') ' + digits.slice(3, 6) + ' ' + digits.slice(6, 8) + ' ' + digits.slice(8, 10);
  }

  function applyMask(input) {
    if (input.dataset.amPhoneApplied === '1') return;
    input.dataset.amPhoneApplied = '1';

    var digits = getDigitsAfterPrefix(input.value);
    if (digits.length > 0) {
      input.value = formatPhone(digits);
    } else {
      input.value = PREFIX;
    }

    input.addEventListener('keydown', function (e) {
      if (e.key === 'Backspace') {
        e.preventDefault();
        var digits = getDigitsAfterPrefix(input.value);
        digits = digits.slice(0, -1);
        input.value = digits.length > 0 ? formatPhone(digits) : PREFIX;
        input.setSelectionRange(input.value.length, input.value.length);
        return;
      }
      if (e.key === 'Delete' || e.key === 'Tab' || e.key.startsWith('Arrow') || e.ctrlKey || e.metaKey) return;
      if (!/^\d$/.test(e.key)) {
        e.preventDefault();
        return;
      }
      var digits = getDigitsAfterPrefix(input.value);
      if (digits.length >= REQUIRED_DIGITS) {
        e.preventDefault();
        return;
      }
      e.preventDefault();
      digits = digits + e.key;
      input.value = formatPhone(digits);
      input.setSelectionRange(input.value.length, input.value.length);
    });

    input.addEventListener('paste', function (e) {
      e.preventDefault();
      var pasted = (e.clipboardData || window.clipboardData).getData('text');
      var digits = (getDigitsAfterPrefix(input.value) + getDigits(pasted)).slice(0, REQUIRED_DIGITS);
      input.value = digits.length > 0 ? formatPhone(digits) : PREFIX;
      input.setSelectionRange(input.value.length, input.value.length);
    });

    input.addEventListener('focus', function () {
      if (input.value === '' || getDigitsAfterPrefix(input.value).length === 0) {
        input.value = PREFIX;
      }
    });
  }

  function init() {
    document.querySelectorAll('.am-phone-input').forEach(applyMask);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  document.addEventListener('shown.bs.modal', function (e) {
    var modal = e.target;
    modal.querySelectorAll('.am-phone-input').forEach(applyMask);
  });
})();

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
