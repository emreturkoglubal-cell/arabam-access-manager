/**
 * Tüm .am-fp-date alanlarında gün.ay.yıl gösterimi (değer ISO yyyy-MM-dd kalır).
 * Layout’ta flatpickr + l10n/tr yüklü olmalıdır.
 */
(function () {
    'use strict';

    function trLocale() {
        return (typeof flatpickr !== 'undefined' && flatpickr.l10ns && flatpickr.l10ns.tr)
            ? flatpickr.l10ns.tr
            : undefined;
    }

    function unlockAlt(fp) {
        if (!fp || !fp.altInput) return;
        fp.altInput.removeAttribute('readonly');
        fp.altInput.readOnly = false;
    }

    function altClassFrom(el) {
        var c = el.className.replace(/\bam-fp-date\b/g, '').trim();
        return (c ? c + ' ' : '') + 'am-fp-alt-input';
    }

    function buildOpts(el) {
        var rand = Math.random().toString(36).slice(2, 11);
        return {
            locale: trLocale(),
            dateFormat: 'Y-m-d',
            altInput: true,
            altFormat: 'd.m.Y',
            allowInput: true,
            disableMobile: true,
            clickOpens: true,
            altInputClass: altClassFrom(el),
            onReady: function () {
                unlockAlt(this);
                if (this.altInput) {
                    this._input.setAttribute('tabindex', '-1');
                    var token = 'am-fp-' + rand;
                    [this._input, this.altInput].forEach(function (node) {
                        if (!node) return;
                        node.setAttribute('autocomplete', token);
                        node.setAttribute('data-lpignore', 'true');
                        node.setAttribute('data-1p-ignore', 'true');
                        node.setAttribute('data-bwignore', 'true');
                    });
                }
            },
            onOpen: function () { unlockAlt(this); },
            onClose: function () { unlockAlt(this); },
            onValueUpdate: function () { unlockAlt(this); }
        };
    }

    function initAmFpDateInputs(root) {
        if (typeof flatpickr === 'undefined') return;
        root = root || document;
        var nodes = root.querySelectorAll('input.am-fp-date:not([data-am-fp-init])');
        nodes.forEach(function (el) {
            if (el.disabled) return;
            el.setAttribute('data-am-fp-init', '1');
            flatpickr(el, buildOpts(el));
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initAmFpDateInputs();
    });

    window.AmFlatpickr = {
        init: initAmFpDateInputs,
        unlockAlt: unlockAlt
    };
})();
