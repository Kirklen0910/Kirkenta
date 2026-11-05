/* Informe por método de pago (cierres de caja): un selector (Todo / cada método / Crédito) que recalcula
   el resumen, la distribución, las salidas y el detalle sin recargar la página.
   Las secciones marcadas con [data-kk-inf-todo] (descuentos, saldo a favor) solo se ven en "Todo".
   Toda la información se pinta con textContent (sin innerHTML). */
(function () {
    'use strict';
    if (window.kkInformeLoaded) { return; }
    window.kkInformeLoaded = true;

    function el(tag, cls, txt) {
        var e = document.createElement(tag);
        if (cls) e.className = cls;
        if (txt !== undefined && txt !== null) e.textContent = txt;
        return e;
    }
    function money(n) {
        n = Number(n) || 0;
        var s = Math.abs(n).toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
        return (n < 0 ? '-L. ' : 'L. ') + s;
    }
    function norm(s) {
        return String(s || '').toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
    }
    function pct(a, b) { return b > 0 ? Math.round(a / b * 1000) / 10 : 0; }
    function esEfectivo(m) { return /efectivo|cash/.test(norm(m.nombre) + ' ' + norm(m.tipo)); }

    // Colores de la barra de distribución (se repiten si hay muchos métodos)
    var COLORES = ['var(--color-primary)', '#10b981', '#f59e0b', '#06b6d4', '#ec4899', '#8b5cf6', '#64748b'];

    function stat(etiqueta, valor, nota, alerta) {
        var s = el('div', 'kk-inf-stat');
        s.appendChild(el('span', 'kk-inf-etq', etiqueta));
        s.appendChild(el('strong', '', valor));
        if (nota) s.appendChild(el('small', alerta ? 'is-alerta' : '', nota));
        return s;
    }

    function tabla(cols, filas, pie, numCols) {
        var wrap = el('div', 'kk-inf-box');
        var t = el('table', 'kk-inf-table');
        var thead = el('thead'), tr = el('tr');
        cols.forEach(function (c, i) { tr.appendChild(el('th', numCols.indexOf(i) >= 0 ? 'num' : '', c)); });
        thead.appendChild(tr); t.appendChild(thead);
        var tb = el('tbody');
        filas.forEach(function (f) {
            var r = el('tr');
            f.forEach(function (v, i) { r.appendChild(el('td', numCols.indexOf(i) >= 0 ? 'num' : '', v)); });
            tb.appendChild(r);
        });
        t.appendChild(tb);
        if (pie) {
            var tf = el('tfoot'), r2 = el('tr');
            pie.forEach(function (v, i) { r2.appendChild(el('td', numCols.indexOf(i) >= 0 ? 'num' : '', v)); });
            tf.appendChild(r2); t.appendChild(tf);
        }
        wrap.appendChild(t);
        return wrap;
    }

    function seccion(root, titulo, sub) {
        var h = el('div', 'kk-inf-h');
        h.appendChild(el('h3', '', titulo));
        if (sub) h.appendChild(el('span', '', sub));
        root.appendChild(h);
    }

    function render(root, d, sel) {
        root.textContent = '';
        var metodos = d.metodos || [];
        var todas = d.todas || [];
        var m = metodos.filter(function (x) { return x.clave === sel; })[0];
        if (sel === 'saldofavor' && !(d.saldoFavor > 0)) sel = 'todo';
        if (sel !== 'todo' && sel !== 'credito' && sel !== 'saldofavor' && !m) sel = 'todo';
        var totalVentas = (d.ventasContado || 0) + (d.ventasCredito || 0);
        var irA = function (clave) { return function () { render(root, d, clave); }; };

        // Secciones que solo aplican a la vista completa (descuentos, saldo a favor)
        var doc = root.ownerDocument;
        [].forEach.call(doc.querySelectorAll('[data-kk-inf-todo]'), function (n) { n.hidden = sel !== 'todo'; });
        try {
            var u = new URL(window.location.href);
            if (sel === 'todo') u.searchParams.delete('metodo'); else u.searchParams.set('metodo', sel);
            if (/\/Cierres\/(Informe|Details|Aprobar)/.test(u.pathname)) window.history.replaceState(null, '', u);
        } catch (e) { /* sin URL */ }

        // ── Selector ──
        var tabs = el('div', 'kk-inf-tabs');
        tabs.setAttribute('role', 'tablist');
        function tab(clave, texto, monto) {
            var b = el('button', 'kk-inf-tab' + (sel === clave ? ' is-on' : ''));
            b.type = 'button'; b.setAttribute('role', 'tab'); b.setAttribute('aria-selected', sel === clave ? 'true' : 'false');
            b.appendChild(el('span', '', texto));
            b.appendChild(el('small', '', money(monto)));
            b.addEventListener('click', irA(clave));
            tabs.appendChild(b);
        }
        tab('todo', 'Todo', d.totalCobrado);
        metodos.forEach(function (x) { tab(x.clave, x.nombre, x.total); });
        tab('credito', 'Crédito', d.ventasCredito);
        if (d.saldoFavor > 0) tab('saldofavor', 'Saldo a favor', d.saldoFavor);
        root.appendChild(tabs);

        // ── Resumen de la vista elegida ──
        var hero = el('section', 'kk-inf-hero');
        var principal = el('div', 'kk-inf-principal');
        var stats = el('div', 'kk-inf-stats');
        var cajaTexto = d.efectivo ? 'inicial ' + money(d.efectivo.saldoInicial) + ' + entradas ' + money(d.efectivo.ingresos) + ' − salidas ' + money(d.efectivo.egresos) : '';
        var nSalidas = (d.egresos || []).reduce(function (a, e) { return a + e.cantidad; }, 0);

        if (sel === 'todo') {
            principal.appendChild(el('span', 'kk-inf-etq', 'Total cobrado'));
            principal.appendChild(el('span', 'kk-inf-monto', money(d.totalCobrado)));
            principal.appendChild(el('span', 'kk-inf-nota', d.totalTransacciones + ' cobro(s) en el período'));
            stats.appendChild(stat('Ventas', money(totalVentas), 'contado ' + money(d.ventasContado) + ' · crédito ' + money(d.ventasCredito)));
            stats.appendChild(stat('Salidas', money(d.totalEgresos), nSalidas + ' movimiento(s)'));
            stats.appendChild(stat('Crédito por cobrar', money(d.saldoCredito), d.cantidadCredito + ' venta(s) a crédito', d.saldoCredito > 0));
            if (d.saldoFavor > 0) stats.appendChild(stat('Pagado con saldo a favor', money(d.saldoFavor), 'ya cobrado antes: no entra a caja'));
            if (d.efectivo) stats.appendChild(stat('Efectivo esperado en caja', money(d.efectivo.esperado), cajaTexto));
        } else if (sel === 'saldofavor') {
            principal.appendChild(el('span', 'kk-inf-etq', 'Pagado con saldo a favor'));
            principal.appendChild(el('span', 'kk-inf-monto', money(d.saldoFavor)));
            principal.appendChild(el('span', 'kk-inf-nota', d.cantidadSaldoFavor + ' pago(s) · dinero que ya se había cobrado (devoluciones): no suma a lo cobrado ni a la caja'));
            stats.appendChild(stat('Peso en las ventas', pct(d.saldoFavor, totalVentas) + '%', 'de ' + money(totalVentas)));
        } else if (sel === 'credito') {
            principal.appendChild(el('span', 'kk-inf-etq', 'Vendido a crédito'));
            principal.appendChild(el('span', 'kk-inf-monto', money(d.ventasCredito)));
            principal.appendChild(el('span', 'kk-inf-nota', d.cantidadCredito + ' venta(s) · no entra a caja hasta que se cobre'));
            stats.appendChild(stat('Por cobrar', money(d.saldoCredito), 'de estas ventas', d.saldoCredito > 0));
            stats.appendChild(stat('Ya abonado', money(Math.max(0, d.ventasCredito - d.saldoCredito)), ''));
            stats.appendChild(stat('Peso en las ventas', pct(d.ventasCredito, totalVentas) + '%', 'de ' + money(totalVentas)));
            stats.appendChild(stat('Promedio por venta', money(d.cantidadCredito ? d.ventasCredito / d.cantidadCredito : 0), ''));
        } else {
            principal.appendChild(el('span', 'kk-inf-etq', 'Cobrado con ' + m.nombre));
            principal.appendChild(el('span', 'kk-inf-monto', money(m.total)));
            principal.appendChild(el('span', 'kk-inf-nota', m.cantidad + ' cobro(s)'));
            stats.appendChild(stat('Peso en lo cobrado', pct(m.total, d.totalCobrado) + '%', 'de ' + money(d.totalCobrado)));
            stats.appendChild(stat('Ticket promedio', money(m.cantidad ? m.total / m.cantidad : 0), ''));
            if (d.efectivo && esEfectivo(m)) stats.appendChild(stat('Efectivo esperado en caja', money(d.efectivo.esperado), cajaTexto));
        }
        hero.appendChild(principal);
        hero.appendChild(stats);
        root.appendChild(hero);

        // ── Distribución (solo en Todo): una barra y la lista de métodos; clic = ver ese método ──
        if (sel === 'todo' && (d.totalCobrado > 0 || d.ventasCredito > 0)) {
            seccion(root, 'Cómo se cobró', 'toca un método para ver su detalle');
            var caja = el('div', 'kk-inf-box kk-inf-dist');
            var barra = el('div', 'kk-inf-barra');
            var lista = el('ul', 'kk-inf-leyenda');
            var conCobro = metodos.filter(function (x) { return x.total > 0; });
            conCobro.forEach(function (x, i) {
                var color = COLORES[i % COLORES.length];
                var seg = el('i'); seg.style.width = pct(x.total, d.totalCobrado) + '%'; seg.style.background = color; seg.title = x.nombre;
                barra.appendChild(seg);
                var li = el('li');
                var b = el('button', ''); b.type = 'button'; b.addEventListener('click', irA(x.clave));
                var punto = el('span', 'kk-inf-punto'); punto.style.background = color;
                b.appendChild(punto);
                b.appendChild(el('span', 'kk-inf-leyenda-nombre', x.nombre));
                b.appendChild(el('span', 'kk-inf-leyenda-pct', pct(x.total, d.totalCobrado) + '%'));
                b.appendChild(el('strong', '', money(x.total)));
                li.appendChild(b); lista.appendChild(li);
            });
            if (d.ventasCredito > 0) {
                var li2 = el('li', 'is-aparte');
                var b2 = el('button', ''); b2.type = 'button'; b2.addEventListener('click', irA('credito'));
                var p2 = el('span', 'kk-inf-punto is-hueco');
                b2.appendChild(p2);
                b2.appendChild(el('span', 'kk-inf-leyenda-nombre', 'Crédito (aún no cobrado)'));
                b2.appendChild(el('span', 'kk-inf-leyenda-pct', ''));
                b2.appendChild(el('strong', '', money(d.saldoCredito)));
                li2.appendChild(b2); lista.appendChild(li2);
            }
            if (conCobro.length) caja.appendChild(barra);
            caja.appendChild(lista);
            root.appendChild(caja);
        }

        // ── Salidas (según la vista) ──
        var egr = d.egresos || [];
        if (sel === 'credito') egr = [];
        else if (sel !== 'todo') {
            var buscar = [norm(m.nombre), norm(m.tipo)].filter(function (s) { return s; });
            egr = egr.filter(function (e) { var n = norm(e.nombre); return buscar.some(function (b) { return n.indexOf(b) >= 0 || b.indexOf(n) >= 0; }); });
        }
        if (egr.length) {
            var tot = egr.reduce(function (a, e) { return a + e.total; }, 0);
            seccion(root, 'Salidas', money(tot));
            root.appendChild(tabla(['Forma de pago', 'Movimientos', 'Total'],
                egr.map(function (e) { return [e.nombre, String(e.cantidad), money(e.total)]; }),
                null, [1, 2]));
        }

        // ── Detalle (sin la columna de método cuando ya se filtró por uno) ──
        var filas = sel === 'todo' ? todas : todas.filter(function (x) { return x.clave === sel; });
        var conMetodo = sel === 'todo';
        var titulo = sel === 'todo' ? 'Todas las transacciones' : sel === 'credito' ? 'Ventas a crédito' : sel === 'saldofavor' ? 'Pagado con saldo a favor' : 'Cobros con ' + m.nombre;
        var totalL = filas.reduce(function (a, x) { return a + x.monto; }, 0);
        seccion(root, titulo, filas.length ? filas.length + ' · ' + money(totalL) : '');
        if (!filas.length) {
            var vacio = el('div', 'kk-inf-box'); vacio.appendChild(el('p', 'kk-inf-empty', 'No hay transacciones para esta vista en el período.'));
            root.appendChild(vacio);
        } else {
            var cols = conMetodo ? ['Fecha', 'Documento', 'Cliente', 'Método', 'Referencia', 'Monto'] : ['Fecha', 'Documento', 'Cliente', 'Referencia', 'Monto'];
            var datos = filas.slice(0, 500).map(function (x) {
                return conMetodo ? [x.fecha, x.doc, x.cliente, x.metodo, x.ref || '—', money(x.monto)]
                                 : [x.fecha, x.doc, x.cliente, x.ref || '—', money(x.monto)];
            });
            var pie = conMetodo ? ['Total', '', '', '', '', money(totalL)] : ['Total', '', '', '', money(totalL)];
            root.appendChild(tabla(cols, datos, pie, [cols.length - 1]));
            if (filas.length > 500) root.appendChild(el('p', 'kk-inf-nota', 'Se muestran las primeras 500 transacciones.'));
        }
    }

    function init(cont) {
        if (!cont) return;
        var nodos = cont.matches && cont.matches('[data-kk-informe]') ? [cont] : [].slice.call(cont.querySelectorAll('[data-kk-informe]'));
        nodos.forEach(function (n) {
            var script = n.querySelector('script.kk-inf-data');
            var root = n.querySelector('.kk-inf-root');
            if (!script || !root) return;
            var d; try { d = JSON.parse(script.textContent); } catch (e) { return; }
            n.kkData = d;
            var inicial = 'todo';
            try {
                var q = new URLSearchParams(window.location.search).get('metodo');
                if (q) {
                    q = norm(q);
                    var claves = (d.metodos || []).map(function (x) { return x.clave; }).concat(['credito', 'saldofavor']);
                    var exacta = claves.filter(function (c) { return c === q; })[0];
                    var parecida = claves.filter(function (c) { return c.indexOf(q) >= 0; })[0];
                    inicial = exacta || parecida || 'todo';
                }
            } catch (e) { /* sin filtro inicial */ }
            render(root, d, inicial);
            n.dispatchEvent(new CustomEvent('kk-informe-listo', { bubbles: true, detail: d }));
        });
    }

    window.kkInformeInit = init;
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', function () { init(document); });
    else init(document);
})();
