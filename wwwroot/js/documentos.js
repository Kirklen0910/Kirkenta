/* RTN (14 dígitos, 0000-0000-000000) e Identidad (13 dígitos, 0000-0000-00000) de Honduras.
   Pone los guiones solos mientras se escribe y valida el largo exacto. Los campos son opcionales:
   vacío es válido (el "obligatorio" lo decide cada formulario). Para excluir un campo: data-documento="off". */
(function () {
    'use strict';
    var TIPOS = [
        { re: /(^|\.)RTN$/, art: 'El', nombre: 'RTN', largo: 14, grupos: [4, 4, 6], ejemplo: '0801-1990-123456' },
        { re: /(^|\.)Cedula$/, art: 'La', nombre: 'identidad', largo: 13, grupos: [4, 4, 5], ejemplo: '0801-1990-12345' }
    ];
    function formatear(digitos, t) {
        var partes = [], pos = 0;
        for (var i = 0; i < t.grupos.length && pos < digitos.length; i++) {
            partes.push(digitos.substr(pos, t.grupos[i]));
            pos += t.grupos[i];
        }
        return partes.join('-');
    }
    function mejorar(el, t) {
        if (el.dataset.documentoListo || el.dataset.documento === 'off') return;
        var form = el.form;
        if (form && (form.getAttribute('method') || 'get').toLowerCase() === 'get') return;
        el.dataset.documentoListo = '1';
        el.inputMode = 'numeric';
        el.autocomplete = 'off';
        el.maxLength = t.largo + t.grupos.length - 1;
        el.placeholder = t.ejemplo;
        var hint = document.createElement('small');
        hint.className = 'tel-hint';
        el.parentNode.insertBefore(hint, el.nextSibling);
        function actualizar() {
            var d = el.value.replace(/\D/g, '').slice(0, t.largo);
            el.value = formatear(d, t);
            var ok = d.length === 0 || d.length === t.largo;
            var msg = ok ? '' : t.art + ' ' + t.nombre + ' debe tener exactamente ' + t.largo + ' dígitos (lleva ' + d.length + ')';
            el.setCustomValidity(msg);
            hint.className = 'tel-hint' + (ok ? '' : ' tel-error');
            hint.textContent = ok ? ('Formato: ' + t.ejemplo + ' (' + t.largo + ' dígitos)') : msg;
        }
        el.addEventListener('input', actualizar);
        el.addEventListener('blur', actualizar);
        actualizar();
    }
    function estilo() {
        if (document.getElementById('tel-hint-style')) return;
        var st = document.createElement('style');
        st.id = 'tel-hint-style';
        st.textContent = '.tel-hint{display:block;margin-top:4px;font-size:12px;opacity:.75}.tel-hint.tel-error{color:#dc2626;opacity:1}';
        document.head.appendChild(st);
    }
    function init(root) {
        estilo();
        (root || document).querySelectorAll('input[name]').forEach(function (el) {
            if (el.type !== 'text' && el.type !== 'tel') return;
            for (var i = 0; i < TIPOS.length; i++) { if (TIPOS[i].re.test(el.name)) { mejorar(el, TIPOS[i]); break; } }
        });
    }
    window.DocumentosInit = init;
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', function () { init(); });
    else init();
})();
