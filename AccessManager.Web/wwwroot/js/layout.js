/**
 * AccessManager – Layout & global davranışlar
 * Tüm sayfalarda kullanılan: sidebar toggle, onay diyalogları vb.
 */
(function () {
  'use strict';

  // ----- Sidebar toggle (mobil) -----
  function initSidebar() {
    var toggle = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('appSidebar');
    var overlay = document.getElementById('sidebarOverlay');

    if (toggle && sidebar) {
      toggle.addEventListener('click', function () {
        sidebar.classList.toggle('open');
        if (overlay) overlay.classList.toggle('show');
      });
    }

    if (overlay && sidebar) {
      overlay.addEventListener('click', function () {
        sidebar.classList.remove('open');
        overlay.classList.remove('show');
      });
    }
  }

  // ----- Form onay (data-am-confirm) -----
  // Kullanım: <form data-am-confirm="..."> veya <button type="submit" data-am-confirm="...">
  function initConfirmSubmit() {
    document.addEventListener('submit', function (e) {
      var form = e.target;
      if (form.tagName !== 'FORM') return;
      var msg = form.getAttribute('data-am-confirm');
      if (!msg && e.submitter) msg = e.submitter.getAttribute('data-am-confirm');
      if (msg && !window.confirm(msg)) e.preventDefault();
    }, true);
  }

  // Sayfa yüklendiğinde
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
      initSidebar();
      initConfirmSubmit();
    });
  } else {
    initSidebar();
    initConfirmSubmit();
  }
})();
