/* Teléfonos por país: selector con código de área, máscara con guion y validación del largo exacto.
   Se aplica solo a los campos Telefono / TelefonoContacto / TelefonoSecundario / ContactoEmergenciaTelefono.
   Valor guardado: "+504 9999-9999". Debe coincidir con Helpers/TelefonoHelper.cs. */
(function () {
    'use strict';
    var PAISES = [{"i": "HN", "n": "Honduras", "d": "504", "l": [8]}, {"i": "GT", "n": "Guatemala", "d": "502", "l": [8]}, {"i": "SV", "n": "El Salvador", "d": "503", "l": [8]}, {"i": "NI", "n": "Nicaragua", "d": "505", "l": [8]}, {"i": "CR", "n": "Costa Rica", "d": "506", "l": [8]}, {"i": "PA", "n": "Panamá", "d": "507", "l": [7, 8]}, {"i": "BZ", "n": "Belice", "d": "501", "l": [7]}, {"i": "MX", "n": "México", "d": "52", "l": [10]}, {"i": "US", "n": "Estados Unidos", "d": "1", "l": [10]}, {"i": "CA", "n": "Canadá", "d": "1", "l": [10]}, {"i": "DO", "n": "Rep. Dominicana", "d": "1", "l": [10]}, {"i": "PR", "n": "Puerto Rico", "d": "1", "l": [10]}, {"i": "CU", "n": "Cuba", "d": "53", "l": [8]}, {"i": "CO", "n": "Colombia", "d": "57", "l": [10]}, {"i": "VE", "n": "Venezuela", "d": "58", "l": [10]}, {"i": "EC", "n": "Ecuador", "d": "593", "l": [9]}, {"i": "PE", "n": "Perú", "d": "51", "l": [9]}, {"i": "BO", "n": "Bolivia", "d": "591", "l": [8]}, {"i": "CL", "n": "Chile", "d": "56", "l": [9]}, {"i": "AR", "n": "Argentina", "d": "54", "l": [10]}, {"i": "UY", "n": "Uruguay", "d": "598", "l": [8]}, {"i": "PY", "n": "Paraguay", "d": "595", "l": [9]}, {"i": "BR", "n": "Brasil", "d": "55", "l": [10, 11]}, {"i": "ES", "n": "España", "d": "34", "l": [9]}, {"i": "FR", "n": "Francia", "d": "33", "l": [9]}, {"i": "GB", "n": "Reino Unido", "d": "44", "l": [10]}, {"i": "DE", "n": "Alemania", "d": "49", "l": [10, 11]}, {"i": "IT", "n": "Italia", "d": "39", "l": [9, 10]}, {"i": "CN", "n": "China", "d": "86", "l": [11]}];
    var DEFECTO = 'HN';
    var NAME_RE = /(^|\.)(Telefono|TelefonoContacto|TelefonoSecundario|ContactoEmergenciaTelefono)$/;
    var AREA = { '809': 'DO', '829': 'DO', '849': 'DO', '787': 'PR', '939': 'PR' };

    function bandera(iso) {
        return String.fromCodePoint.apply(null, iso.split('').map(function (c) { return 127397 + c.charCodeAt(0); }));
    }
    function porIso(iso) { return PAISES.filter(function (p) { return p.i === iso; })[0]; }

    var GRUPOS = { 7: [3, 4], 8: [4, 4], 9: [3, 3, 3], 10: [3, 3, 4], 11: [2, 5, 4] };
    var ESPECIAL = { MX: { 10: [2, 4, 4] }, AR: { 10: [2, 4, 4] }, BR: { 10: [2, 4, 4], 11: [2, 5, 4] }, CL: { 9: [1, 4, 4] }, CN: { 11: [3, 4, 4] } };

    function patron(p, len) {
        var largos = p.l.slice().sort(function (a, b) { return a - b; });
        var objetivo = largos[largos.length - 1];
        for (var i = 0; i < largos.length; i++) { if (largos[i] >= len) { objetivo = largos[i]; break; } }
        return (ESPECIAL[p.i] && ESPECIAL[p.i][objetivo]) || GRUPOS[objetivo] || [objetivo];
    }
    function formatear(digitos, p) {
        var max = Math.max.apply(null, p.l);
        digitos = digitos.slice(0, max);
        var g = patron(p, digitos.length), partes = [], pos = 0;
        for (var i = 0; i < g.length && pos < digitos.length; i++) {
            partes.push(digitos.substr(pos, g[i]));
            pos += g[i];
        }
        if (pos < digitos.length) partes.push(digitos.substr(pos));
        return partes.join('-');
    }
    function ejemplo(p) {
        var largos = p.l.slice().sort(function (a, b) { return a - b; });
        var len = largos[largos.length - 1], s = '';
        for (var i = 0; i < len; i++) s += '9';
        return formatear(s, p);
    }
    function descripcion(p) {
        return p.l.join(' o ') + ' dígitos';
    }
    function detectar(valor) {
        var v = (valor || '').trim();
        var digitos = v.replace(/\D/g, '');
        if (!digitos) return { p: porIso(DEFECTO), nac: '' };
        if (v.charAt(0) === '+') {
            var orden = PAISES.slice().sort(function (a, b) { return b.d.length - a.d.length; });
            for (var i = 0; i < orden.length; i++) {
                if (digitos.indexOf(orden[i].d) === 0) {
                    var nac = digitos.substr(orden[i].d.length), p = orden[i];
                    if (p.d === '1' && AREA[nac.substr(0, 3)]) p = porIso(AREA[nac.substr(0, 3)]);
                    return { p: p, nac: nac };
                }
            }
        }
        return { p: porIso(DEFECTO), nac: digitos };
    }

    var estiloPuesto = false;
    function poner_estilo() {
        if (estiloPuesto) return; estiloPuesto = true;
        var st = document.createElement('style');
        st.textContent = '.tel-wrap{display:flex;gap:8px;align-items:stretch}.tel-wrap select{flex:0 0 auto;width:auto;max-width:210px}.tel-wrap input{flex:1 1 auto;min-width:0}.tel-hint{display:block;margin-top:4px;font-size:12px;opacity:.75}.tel-hint.tel-error{color:#dc2626;opacity:1}';
        document.head.appendChild(st);
    }

    function mejorar(orig) {
        if (orig.dataset.telefonoListo || orig.dataset.telefono === 'off') return;
        var form = orig.form;
        if (form && (form.getAttribute('method') || 'get').toLowerCase() === 'get') return;
        orig.dataset.telefonoListo = '1';
        poner_estilo();

        var inicial = detectar(orig.value);
        var wrap = document.createElement('div'); wrap.className = 'tel-wrap';
        var sel = document.createElement('select'); sel.className = 'form-input'; sel.setAttribute('aria-label', 'País');
        PAISES.forEach(function (p) {
            var o = document.createElement('option');
            o.value = p.i; o.textContent = bandera(p.i) + ' ' + p.n + ' (+' + p.d + ')';
            sel.appendChild(o);
        });
        sel.value = inicial.p.i;
        var vis = document.createElement('input');
        vis.type = 'tel'; vis.inputMode = 'numeric'; vis.autocomplete = 'off'; vis.className = 'form-input';
        if (orig.required) vis.required = true;
        var hint = document.createElement('small'); hint.className = 'tel-hint';

        orig.type = 'hidden';
        orig.parentNode.insertBefore(wrap, orig.nextSibling);
        wrap.appendChild(sel); wrap.appendChild(vis);
        wrap.parentNode.insertBefore(hint, wrap.nextSibling);

        function paisActual() { return porIso(sel.value); }
        function actualizar(reformatear) {
            var p = paisActual();
            var digitos = vis.value.replace(/\D/g, '').slice(0, Math.max.apply(null, p.l));
            if (reformatear) vis.value = formatear(digitos, p);
            vis.placeholder = ejemplo(p);
            var ok = digitos.length === 0 || p.l.indexOf(digitos.length) >= 0;
            orig.value = digitos ? '+' + p.d + ' ' + formatear(digitos, p) : '';
            var msg = ok ? '' : 'El número de ' + p.n + ' debe tener ' + descripcion(p);
            vis.setCustomValidity(msg);
            hint.className = 'tel-hint' + (ok ? '' : ' tel-error');
            hint.textContent = ok ? ('Formato: +' + p.d + ' ' + ejemplo(p) + ' (' + descripcion(p) + ')') : msg;
        }
        vis.value = inicial.nac;
        actualizar(true);
        vis.addEventListener('input', function () { actualizar(true); });
        sel.addEventListener('change', function () { actualizar(true); });
        vis.addEventListener('blur', function () { actualizar(true); });
    }

    function init(root) {
        (root || document).querySelectorAll('input[name]').forEach(function (el) {
            if (el.type !== 'hidden' && el.type !== 'text' && el.type !== 'tel') return;
            if (NAME_RE.test(el.name)) mejorar(el);
        });
    }
    window.TelefonoInit = init;
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', function () { init(); });
    else init();
})();
