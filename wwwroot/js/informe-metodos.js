/* Informe por método de pago: pestañas (Todo / método / Crédito) que recalculan tarjetas, salidas y detalle.
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

    function kpi(label, value, sub) {
        var c = el('div', 'kk-inf-card');
        c.appendChild(el('div', 'kk-inf-label', label));
        c.appendChild(el('div', 'kk-inf-value', value));
        if (sub) c.appendChild(el('div', 'kk-inf-sub', sub));
        return c;
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

    function render(root, d, sel) {
        root.textContent = '';
        var metodos = d.metodos || [];
        var todas = d.todas || [];
        var conteo = function (clave) { return todas.filter(function (x) { return x.clave === clave; }).length; };

        // ── Pestañas ──
        var tabs = el('div', 'kk-inf-tabs');
        tabs.setAttribute('role', 'tablist');
        function tab(clave, texto, sub) {
            var b = el('button', 'kk-inf-tab' + (sel === clave ? ' is-on' : ''));
            b.type = 'button'; b.setAttribute('role', 'tab'); b.setAttribute('aria-selected', sel === clave ? 'true' : 'false');
            b.appendChild(document.createTextNode(texto));
            if (sub) b.appendChild(el('small', '', sub));
            b.addEventListener('click', function () { render(root, d, clave); });
            tabs.appendChild(b);
        }
        tab('todo', 'Todo', money(d.totalCobrado));
        metodos.forEach(function (m) { tab(m.clave, m.nombre, money(m.total)); });
        tab('credito', 'Crédito', money(d.ventasCredito));
        root.appendChild(tabs);

        var m = metodos.filter(function (x) { return x.clave === sel; })[0];
        var totalVentas = (d.ventasContado || 0) + (d.ventasCredito || 0);

        // ── Tarjetas resumen (cambian según la pestaña) ──
        var k = el('div', 'kk-inf-kpis');
        if (sel === 'todo') {
            k.appendChild(kpi('Total cobrado', money(d.totalCobrado), d.totalTransacciones + ' cobro(s)'));
            k.appendChild(kpi('Ventas del período', money(totalVentas), 'Contado ' + money(d.ventasContado) + ' • Crédito ' + money(d.ventasCredito)));
            k.appendChild(kpi('Salidas (egresos)', money(d.totalEgresos), (d.egresos || []).reduce(function (a, e) { return a + e.cantidad; }, 0) + ' movimiento(s)'));
            k.appendChild(kpi('Crédito por cobrar', money(d.saldoCredito), d.cantidadCredito + ' venta(s) a crédito'));
            if (d.efectivo) k.appendChild(kpi('Efectivo esperado en caja', money(d.efectivo.esperado),
                'Inicial ' + money(d.efectivo.saldoInicial) + ' + ingresos ' + money(d.efectivo.ingresos) + ' − egresos ' + money(d.efectivo.egresos)));
        } else if (sel === 'credito') {
            k.appendChild(kpi('Ventas a crédito', money(d.ventasCredito), d.cantidadCredito + ' venta(s)'));
            k.appendChild(kpi('Saldo por cobrar', money(d.saldoCredito), 'De las ventas a crédito de este período'));
            k.appendChild(kpi('Peso en las ventas', pct(d.ventasCredito, totalVentas) + '%', 'Del total vendido: ' + money(totalVentas)));
            k.appendChild(kpi('Promedio por venta', money(d.cantidadCredito ? d.ventasCredito / d.cantidadCredito : 0), ''));
        } else if (m) {
            k.appendChild(kpi('Cobrado con ' + m.nombre, money(m.total), m.cantidad + ' cobro(s)'));
            k.appendChild(kpi('Peso en lo cobrado', pct(m.total, d.totalCobrado) + '%', 'Del total cobrado: ' + money(d.totalCobrado)));
            k.appendChild(kpi('Ticket promedio', money(m.cantidad ? m.total / m.cantidad : 0), ''));
            if (d.efectivo && /efectivo/.test(norm(m.nombre) + ' ' + norm(m.tipo)))
                k.appendChild(kpi('Efectivo esperado en caja', money(d.efectivo.esperado),
                    'Inicial ' + money(d.efectivo.saldoInicial) + ' + ingresos ' + money(d.efectivo.ingresos) + ' − egresos ' + money(d.efectivo.egresos)));
        }
        root.appendChild(k);

        // ── Tarjetas por método (clic = cambia de pestaña) ──
        root.appendChild(el('h3', 'kk-inf-h', 'Cómo se mueve el negocio'));
        var g = el('div', 'kk-inf-kpis');
        metodos.forEach(function (x) {
            var c = el('button', 'kk-inf-card' + (sel === x.clave ? ' is-on' : '')); c.type = 'button';
            c.appendChild(el('div', 'kk-inf-label', x.nombre));
            c.appendChild(el('div', 'kk-inf-value', money(x.total)));
            c.appendChild(el('div', 'kk-inf-sub', x.cantidad + ' cobro(s) • ' + pct(x.total, d.totalCobrado) + '%'));
            var bar = el('div', 'kk-inf-bar'), i = el('i'); i.style.width = Math.min(100, pct(x.total, d.totalCobrado)) + '%';
            bar.appendChild(i); c.appendChild(bar);
            c.addEventListener('click', function () { render(root, d, x.clave); });
            g.appendChild(c);
        });
        var cc = el('button', 'kk-inf-card' + (sel === 'credito' ? ' is-on' : '')); cc.type = 'button';
        cc.appendChild(el('div', 'kk-inf-label', 'Crédito (ventas)'));
        cc.appendChild(el('div', 'kk-inf-value', money(d.ventasCredito)));
        cc.appendChild(el('div', 'kk-inf-sub', d.cantidadCredito + ' venta(s) • por cobrar ' + money(d.saldoCredito)));
        var bar2 = el('div', 'kk-inf-bar'), i2 = el('i'); i2.style.width = Math.min(100, pct(d.ventasCredito, totalVentas)) + '%';
        bar2.appendChild(i2); cc.appendChild(bar2);
        cc.addEventListener('click', function () { render(root, d, 'credito'); });
        g.appendChild(cc);
        root.appendChild(g);

        // ── Salidas (se ajustan a la pestaña) ──
        var egr = d.egresos || [];
        if (sel !== 'todo' && sel !== 'credito' && m) {
            var buscar = [norm(m.nombre), norm(m.tipo)].filter(function (s) { return s; });
            egr = egr.filter(function (e) { var n = norm(e.nombre); return buscar.some(function (b) { return n.indexOf(b) >= 0 || b.indexOf(n) >= 0; }); });
        } else if (sel === 'credito') { egr = []; }
        if (egr.length) {
            root.appendChild(el('h3', 'kk-inf-h', 'Salidas por forma de pago'));
            var tot = egr.reduce(function (a, e) { return a + e.total; }, 0);
            root.appendChild(tabla(['Forma de pago', 'Movimientos', 'Total', '%'],
                egr.map(function (e) { return [e.nombre, String(e.cantidad), money(e.total), pct(e.total, tot) + '%']; }),
                ['Total', String(egr.reduce(function (a, e) { return a + e.cantidad; }, 0)), money(tot), ''], [1, 2, 3]));
        }

        // ── Detalle filtrado ──
        var lista = sel === 'todo' ? todas : todas.filter(function (x) { return x.clave === sel; });
        var titulo = sel === 'todo' ? 'Todas las transacciones' : sel === 'credito' ? 'Ventas a crédito' : (m ? m.nombre : 'Detalle');
        root.appendChild(el('h3', 'kk-inf-h', 'Detalle: ' + titulo));
        if (!lista.length) {
            var vacio = el('div', 'kk-inf-box'); vacio.appendChild(el('p', 'kk-inf-empty', 'No hay transacciones para esta vista en el período.'));
            root.appendChild(vacio);
        } else {
            var totalL = lista.reduce(function (a, x) { return a + x.monto; }, 0);
            var filas = lista.slice(0, 500).map(function (x) {
                return [x.fecha, x.doc, x.cliente, x.metodo, x.ref || '—', money(x.monto)];
            });
            root.appendChild(tabla(['Fecha', 'Documento', 'Cliente', 'Método', 'Referencia', 'Monto'], filas,
                ['Total (' + lista.length + ')', '', '', '', '', money(totalL)], [5]));
            if (lista.length > 500) root.appendChild(el('p', 'kk-inf-sub', 'Se muestran las primeras 500 transacciones.'));
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
                    var claves = (d.metodos || []).map(function (x) { return x.clave; }).concat(['credito']);
                    if (claves.indexOf(q) >= 0) inicial = q;
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
