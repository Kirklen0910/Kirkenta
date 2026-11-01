/* Orden de compra desde foto de factura (Kirkenta) */
(function () {
    'use strict';
    var datos = document.getElementById('dfData');
    if (!datos) return;
    var PROV = JSON.parse(datos.getAttribute('data-proveedores') || '[]');
    var PROD = JSON.parse(datos.getAttribute('data-productos') || '[]');
    var $ = function (id) { return document.getElementById(id); };
    var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenEl ? tokenEl.value : '';
    var archivo = null, lineas = [];

    function esc(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }
    function L(n) { return 'L. ' + (Number(n) || 0).toLocaleString('es-HN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function etiqueta(p) { return p.nombre + (p.sku ? ' [' + p.sku + ']' : ''); }
    function porId(id) { for (var i = 0; i < PROD.length; i++) if (PROD[i].id === id) return PROD[i]; return null; }
    function porEtiqueta(t) { t = (t || '').trim().toLowerCase(); for (var i = 0; i < PROD.length; i++) if (etiqueta(PROD[i]).toLowerCase() === t) return PROD[i]; return null; }
    function mostrarError(el, msg) { el.textContent = msg; el.style.display = msg ? 'block' : 'none'; }

    // lista de productos para el buscador
    $('dfLista').innerHTML = PROD.map(function (p) { return '<option value="' + esc(etiqueta(p)) + '"></option>'; }).join('');
    $('dfProv').innerHTML = '<option value="">— Elige el proveedor —</option>' + PROV.map(function (p) { return '<option value="' + p.id + '">' + esc(p.nombre) + '</option>'; }).join('');

    // ----- foto: se reduce en el navegador antes de enviarla -----
    $('dfArchivo').addEventListener('change', function () {
        var f = this.files && this.files[0]; if (!f) return;
        mostrarError($('dfErr'), '');
        reducir(f, 1800, function (blob) {
            archivo = blob;
            $('dfPrev').src = URL.createObjectURL(blob); $('dfPrev').style.display = 'block';
            $('dfLeer').disabled = false;
        });
    });

    function reducir(file, max, cb) {
        var img = new Image(), url = URL.createObjectURL(file);
        img.onload = function () {
            var w = img.width, h = img.height, k = Math.min(1, max / Math.max(w, h));
            var c = document.createElement('canvas'); c.width = Math.round(w * k); c.height = Math.round(h * k);
            c.getContext('2d').drawImage(img, 0, 0, c.width, c.height);
            URL.revokeObjectURL(url);
            c.toBlob(function (b) { cb(b || file); }, 'image/jpeg', 0.85);
        };
        img.onerror = function () { URL.revokeObjectURL(url); cb(file); };
        img.src = url;
    }

    $('dfLeer').addEventListener('click', function () {
        if (!archivo) return;
        var btn = this; btn.disabled = true; $('dfSpin').style.display = 'block'; mostrarError($('dfErr'), '');
        var fd = new FormData(); fd.append('imagen', archivo, 'factura.jpg');
        fetch('?handler=Leer', { method: 'POST', headers: { 'RequestVerificationToken': token }, body: fd })
            .then(function (r) { return r.json(); })
            .then(function (d) {
                if (!d.ok) { mostrarError($('dfErr'), d.error || 'No se pudo leer la factura.'); return; }
                cargar(d);
            })
            .catch(function () { mostrarError($('dfErr'), 'No se pudo comunicar con el servidor.'); })
            .then(function () { $('dfSpin').style.display = 'none'; btn.disabled = false; });
    });

    function cargar(d) {
        lineas = d.lineas.map(function (l) {
            return { leido: l.descripcion, codigo: l.codigo, cantidad: l.cantidad, precio: l.precio, impuesto: l.impuesto, productoId: l.productoId, puntaje: l.puntaje };
        });
        $('dfProv').value = d.proveedorId || '';
        $('dfProvLeido').textContent = d.proveedorLeido ? 'Leído: ' + d.proveedorLeido + (d.rtn ? ' · RTN ' + d.rtn : '') + (d.proveedorId ? '' : ' (no está en tus proveedores: elígelo o regístralo primero)') : '';
        $('dfNum').value = d.numero || '';
        $('dfFecha').value = d.fecha || '';
        var dif = $('dfDif');
        if (d.difiere) {
            dif.innerHTML = '⚠️ La suma de las líneas leídas (<strong>' + L(d.sumaLineas) + '</strong>) no coincide con el total de la factura' + (d.total ? ' (<strong>' + L(d.total) + '</strong>)' : '') + '. Puede que falte o sobre alguna línea: revisa con la foto.';
            dif.style.display = 'block';
        } else { dif.style.display = 'none'; }
        pintar();
        $('dfPaso1').style.display = 'none'; $('dfPaso2').style.display = 'block';
    }

    function estado(l) {
        if (!l.productoId) return '<span class="df-nada">Sin producto</span>';
        if (l.puntaje >= 0.9) return '<span class="df-ok">✔ Coincide</span>';
        if (l.puntaje > 0) return '<span class="df-dudoso">Revisar</span>';
        return '';
    }

    function pintar() {
        $('dfBody').innerHTML = lineas.map(function (l, i) {
            var p = l.productoId ? porId(l.productoId) : null;
            return '<tr data-i="' + i + '">' +
                '<td><div>' + esc(l.leido) + '</div>' + (l.codigo ? '<div class="df-leido">Cód. ' + esc(l.codigo) + '</div>' : '') + '<div>' + estado(l) + '</div></td>' +
                '<td><input class="form-control" list="dfLista" data-f="prod" value="' + esc(p ? etiqueta(p) : '') + '" placeholder="Buscar producto…" /></td>' +
                '<td><input class="form-control" type="number" step="0.01" min="0" data-f="cantidad" value="' + l.cantidad + '" style="width:90px" /></td>' +
                '<td><input class="form-control" type="number" step="0.01" min="0" data-f="precio" value="' + l.precio + '" style="width:110px" /></td>' +
                '<td><input class="form-control" type="number" step="0.01" min="0" max="100" data-f="impuesto" value="' + l.impuesto + '" style="width:80px" /></td>' +
                '<td data-sub>' + L(l.cantidad * l.precio) + '</td>' +
                '<td><button type="button" class="btn btn-secondary" data-quitar="' + i + '" title="Quitar línea">✕</button></td></tr>';
        }).join('');
        totales();
    }

    function totales() {
        var sub = 0, isv = 0;
        lineas.forEach(function (l) {
            if (!l.productoId) return;
            var b = Math.round(l.cantidad * l.precio * 100) / 100; sub += b; isv += Math.round(b * l.impuesto) / 100;
        });
        $('dfSub').textContent = L(sub); $('dfIsv').textContent = L(isv); $('dfTot').textContent = L(sub + isv);
    }

    $('dfBody').addEventListener('input', function (e) {
        var tr = e.target.closest('tr'); if (!tr) return;
        var l = lineas[parseInt(tr.getAttribute('data-i'), 10)], f = e.target.getAttribute('data-f');
        if (f === 'prod') {
            var p = porEtiqueta(e.target.value);
            if (p) { l.productoId = p.id; l.puntaje = 1; if (!l.impuesto) l.impuesto = p.imp; tr.querySelector('[data-f=impuesto]').value = l.impuesto; }
            else { l.productoId = null; l.puntaje = 0; }
        } else { l[f] = parseFloat(e.target.value) || 0; }
        tr.querySelector('[data-sub]').textContent = L(l.cantidad * l.precio);
        totales();
    });
    $('dfBody').addEventListener('change', function (e) { if (e.target.getAttribute('data-f') === 'prod') pintar(); });
    $('dfBody').addEventListener('click', function (e) {
        var b = e.target.closest('[data-quitar]'); if (!b) return;
        lineas.splice(parseInt(b.getAttribute('data-quitar'), 10), 1); pintar();
    });

    $('dfOtra').addEventListener('click', function () {
        archivo = null; lineas = []; $('dfArchivo').value = ''; $('dfPrev').style.display = 'none'; $('dfLeer').disabled = true;
        $('dfPaso2').style.display = 'none'; $('dfPaso1').style.display = 'block';
    });

    $('dfCrear').addEventListener('click', function () {
        var err = $('dfErr2'); mostrarError(err, '');
        var prov = parseInt($('dfProv').value, 10);
        if (!prov) { mostrarError(err, 'Elige el proveedor.'); $('dfProv').focus(); return; }
        var validas = lineas.filter(function (l) { return l.productoId; });
        var sinProducto = lineas.length - validas.length;
        if (!validas.length) { mostrarError(err, 'Ninguna línea tiene producto del sistema asignado.'); return; }
        if (sinProducto > 0 && !confirm(sinProducto + ' línea(s) sin producto NO se incluirán en la orden. ¿Continuar?')) return;
        var btn = this; btn.disabled = true;
        fetch('?handler=Crear', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify({
                proveedorId: prov, numeroFactura: $('dfNum').value, fechaFactura: $('dfFecha').value || null,
                lineas: validas.map(function (l) { return { productoId: l.productoId, cantidad: l.cantidad, precio: l.precio, impuesto: l.impuesto }; })
            })
        }).then(function (r) { return r.json(); }).then(function (d) {
            if (!d.ok) { mostrarError(err, d.error || 'No se pudo crear la orden.'); btn.disabled = false; return; }
            window.location.href = d.url;
        }).catch(function () { mostrarError(err, 'No se pudo comunicar con el servidor.'); btn.disabled = false; });
    });
})();
