/**
 * Currency mask — Brazilian Real (R$)
 * Applied to all inputs with [data-money] attribute.
 * Formats display as "1.234,56"; converts back to "1234.56" before form submit.
 */
(function () {
  'use strict';

  function toDisplay(raw) {
    // raw = digits only string
    if (!raw) return '';
    const num = parseInt(raw, 10);
    if (isNaN(num)) return '';
    return (num / 100).toLocaleString('pt-BR', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }

  function toDecimal(display) {
    // "1.234,56" -> "1234.56"
    return display.replace(/\./g, '').replace(',', '.');
  }

  function initInput(input) {
    // Convert initial server value (e.g. "1234.56" or "1234,56") to display format
    const raw = input.value.trim();
    if (raw !== '') {
      const n = parseFloat(raw.replace(',', '.'));
      if (!isNaN(n)) {
        input.value = n.toLocaleString('pt-BR', {
          minimumFractionDigits: 2,
          maximumFractionDigits: 2,
        });
      }
    }

    input.addEventListener('input', function () {
      const digits = this.value.replace(/\D/g, '');
      this.value = toDisplay(digits || '0');
    });

    input.addEventListener('focus', function () {
      if (!this.value || this.value === '0,00') {
        this.value = '';
      }
      // Place cursor at end
      requestAnimationFrame(() => {
        this.setSelectionRange(this.value.length, this.value.length);
      });
    });

    input.addEventListener('blur', function () {
      const digits = this.value.replace(/\D/g, '');
      if (!digits || digits === '0' || digits === '00') {
        this.value = '';
      }
    });
  }

  function attachSubmitConverters() {
    document.querySelectorAll('form').forEach(function (form) {
      form.addEventListener('submit', function () {
        form.querySelectorAll('[data-money]').forEach(function (input) {
          if (input.value) {
            input.value = toDecimal(input.value);
          }
        });
      }, { capture: true });
    });
  }

  function init() {
    document.querySelectorAll('[data-money]').forEach(initInput);
    attachSubmitConverters();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
