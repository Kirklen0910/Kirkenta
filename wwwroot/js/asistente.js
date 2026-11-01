/* Asistente del sistema: chat flotante. Todo el texto del servidor se inserta con textContent (nunca innerHTML). */
(function () {
    'use strict';
    var root = document.getElementById('kkAsistRoot');
    if (!root) return;

    var baseUrl = root.getAttribute('data-url') || '/Asistente';
    var tokenInput = root.querySelector('input[name="__RequestVerificationToken"]');
    var STORE = 'kkAsistLog';
    var ctx = null;          // contexto que el servidor pidió devolver (dato faltante)
    var seg = null;          // última consulta respondida (para “¿y ayer?”)
    var busy = false;
    var abierto = false;
    var catalogoCargado = false;

    function el(tag, cls, text) {
        var e = document.createElement(tag);
        if (cls) e.className = cls;
        if (text != null) e.textContent = text;
        return e;
    }
    function ahora() { var d = new Date(); return ('0' + d.getHours()).slice(-2) + ':' + ('0' + d.getMinutes()).slice(-2); }

    // ───────── Interfaz ─────────
    var fab = el('button', 'kk-as-fab');
    fab.type = 'button';
    fab.setAttribute('aria-label', 'Abrir el asistente');
    fab.title = 'Asistente';
    fab.innerHTML = '<svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 15a2 2 0 0 1-2 2H8l-5 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/><path d="M8 9h8M8 13h5"/></svg>';

    var panel = el('section', 'kk-as-panel');
    panel.setAttribute('role', 'dialog');
    panel.setAttribute('aria-label', 'Asistente del sistema');
    panel.hidden = true;

    var head = el('header', 'kk-as-head');
    var htxt = el('div', 'kk-as-head-text');
    htxt.appendChild(el('strong', null, 'Asistente'));
    htxt.appendChild(el('span', null, 'Pídeme reportes, consultas y acciones'));
    var bLimpiar = el('button', 'kk-as-icon', '🗑'); bLimpiar.type = 'button'; bLimpiar.title = 'Limpiar conversación';
    var bCerrar = el('button', 'kk-as-icon', '✕'); bCerrar.type = 'button'; bCerrar.title = 'Cerrar'; bCerrar.setAttribute('aria-label', 'Cerrar');
    head.appendChild(htxt); head.appendChild(bLimpiar); head.appendChild(bCerrar);

    var body = el('div', 'kk-as-body');
    body.setAttribute('aria-live', 'polite');
    var chips = el('div', 'kk-as-chips');

    var form = el('form', 'kk-as-form');
    var input = el('input', 'kk-as-input');
    input.type = 'text'; input.maxLength = 500; input.autocomplete = 'off';
    input.placeholder = 'Escribe: ventas de hoy, saldo de un cliente, cerrar caja…';
    input.setAttribute('aria-label', 'Mensaje para el asistente');
    var bEnviar = el('button', 'kk-as-send', 'Enviar'); bEnviar.type = 'submit';
    form.appendChild(input); form.appendChild(bEnviar);

    panel.appendChild(head); panel.appendChild(body); panel.appendChild(chips); panel.appendChild(form);
    document.body.appendChild(fab); document.body.appendChild(panel);

    // ───────── Persistencia ligera (por pestaña) ─────────
    function guardar() {
        try {
            var html = body.innerHTML;
            while (html.length > 250000 && body.firstChild) { body.removeChild(body.firstChild); html = body.innerHTML; }
            sessionStorage.setItem(STORE, html);
        } catch (e) { /* sin almacenamiento: no pasa nada */ }
    }
    function restaurar() {
        try {
            var html = sessionStorage.getItem(STORE);
            if (!html) return false;
            body.innerHTML = html;
            // Las confirmaciones pendientes ya no sirven tras cambiar de pantalla
            body.querySelectorAll('.kk-as-confirm').forEach(function (f) {
                var n = el('div', 'kk-as-note', 'Esta confirmación venció. Pídeme la acción de nuevo.');
                f.replaceWith(n);
            });
            return body.children.length > 0;
        } catch (e) { return false; }
    }

    // ───────── Mensajes ─────────
    function scrollFin() { body.scrollTop = body.scrollHeight; }

    function burbujaUsuario(texto) {
        var m = el('div', 'kk-as-msg kk-as-user');
        m.appendChild(el('div', 'kk-as-bubble', texto));
        m.appendChild(el('time', 'kk-as-time', ahora()));
        body.appendChild(m); scrollFin();
    }

    function parrafos(contenedor, texto) {
        String(texto || '').split('\n').forEach(function (linea) {
            if (linea.trim() !== '') contenedor.appendChild(el('p', 'kk-as-p', linea));
        });
    }

    function tabla(r) {
        var wrap = el('div', 'kk-as-table');
        wrap.setAttribute('data-payload', JSON.stringify({
            titulo: r.titulo || '', mensaje: r.mensaje || '', empresa: r.empresa || '',
            columnas: r.columnas || [], filas: r.filas || [], totales: r.totales || null, numericas: r.columnasNumericas || []
        }));
        var numericas = r.columnasNumericas || [];
        if (r.filas && r.filas.length) {
            var scroll = el('div', 'kk-as-scroll');
            var t = el('table', 'kk-as-t');
            var thead = el('thead'); var trh = el('tr');
            r.columnas.forEach(function (c, i) { var th = el('th', numericas.indexOf(i) >= 0 ? 'num' : null, c); trh.appendChild(th); });
            thead.appendChild(trh); t.appendChild(thead);
            var tb = el('tbody');
            r.filas.forEach(function (fila) {
                var tr = el('tr');
                fila.forEach(function (v, i) { tr.appendChild(el('td', numericas.indexOf(i) >= 0 ? 'num' : null, v)); });
                tb.appendChild(tr);
            });
            t.appendChild(tb);
            if (r.totales) {
                var tf = el('tfoot'); var trf = el('tr');
                r.totales.forEach(function (v, i) { trf.appendChild(el('td', numericas.indexOf(i) >= 0 ? 'num' : null, v)); });
                tf.appendChild(trf); t.appendChild(tf);
            }
            scroll.appendChild(t); wrap.appendChild(scroll);
            var acc = el('div', 'kk-as-actions');
            var bi = el('button', 'kk-as-btn', '🖨️ Imprimir / PDF'); bi.type = 'button'; bi.setAttribute('data-accion', 'imprimir');
            var bc = el('button', 'kk-as-btn', '⬇️ CSV'); bc.type = 'button'; bc.setAttribute('data-accion', 'csv');
            acc.appendChild(bi); acc.appendChild(bc); wrap.appendChild(acc);
        }
        return wrap;
    }

    function confirmacion(r) {
        var f = el('form', 'kk-as-confirm');
        f.setAttribute('data-token', r.token || '');
        if (r.resumen && r.resumen.length) {
            var ul = el('ul', 'kk-as-resumen');
            r.resumen.forEach(function (x) { ul.appendChild(el('li', null, x)); });
            f.appendChild(ul);
        }
        var pw = el('input', 'kk-as-input'); pw.type = 'password'; pw.name = 'password'; pw.autocomplete = 'off';
        pw.placeholder = 'Tu contraseña'; pw.required = true; pw.setAttribute('aria-label', 'Contraseña para confirmar');
        f.appendChild(pw);
        var otro = el('button', 'kk-as-link-btn', 'Autoriza otro usuario (ej. administrador)'); otro.type = 'button'; otro.setAttribute('data-accion', 'otro');
        f.appendChild(otro);
        var au = el('input', 'kk-as-input kk-as-autor'); au.type = 'text'; au.name = 'autorizador'; au.autocomplete = 'off';
        au.placeholder = 'Usuario que autoriza (su contraseña va arriba)'; au.hidden = true;
        f.appendChild(au);
        var fila = el('div', 'kk-as-actions');
        var ok = el('button', 'kk-as-btn kk-as-primary', 'Confirmar'); ok.type = 'submit';
        var no = el('button', 'kk-as-btn', 'Cancelar'); no.type = 'button'; no.setAttribute('data-accion', 'cancelar');
        fila.appendChild(ok); fila.appendChild(no); f.appendChild(fila);
        f.appendChild(el('div', 'kk-as-error', ''));
        return f;
    }

    // Cuántas opciones mostrar de entrada según el tamaño de la pantalla (el resto queda tras "Ver más")
    function limiteOpciones() {
        var w = panel && panel.clientWidth ? panel.clientWidth : window.innerWidth;
        var h = window.innerHeight;
        if (w < 340 || h < 520) return 3;
        if (w < 420 || h < 640) return 5;
        return 8;
    }

    function burbujaBot(r) {
        var m = el('div', 'kk-as-msg kk-as-bot' + (r.tipo === 'error' ? ' kk-as-err' : ''));
        var b = el('div', 'kk-as-bubble');
        if (r.titulo) b.appendChild(el('div', 'kk-as-title', r.titulo));
        parrafos(b, r.mensaje);
        if (r.origen === 'ia') b.appendChild(el('div', 'kk-as-origen', '🤖 Entendido con IA'));
        else if (r.origen === 'ia-sin-respuesta') b.appendChild(el('div', 'kk-as-origen', '🤖 La IA tampoco pudo entenderlo'));
        else if (r.origen === 'ia-apagada') b.appendChild(el('div', 'kk-as-origen', '⚪ IA apagada: solo entiendo con reglas. Actívala en Configuración → Asistente con IA.'));
        if (r.tipo === 'tabla') b.appendChild(tabla(r));
        if (r.tipo === 'confirmar') b.appendChild(confirmacion(r));
        if (r.enlaces && r.enlaces.length) {
            var ls = el('div', 'kk-as-actions');
            r.enlaces.forEach(function (l) {
                var a = el('a', 'kk-as-btn', l.texto);
                a.href = l.url;
                if (l.externo) { a.target = '_blank'; a.rel = 'noopener noreferrer'; }
                ls.appendChild(a);
            });
            b.appendChild(ls);
        }
        if (r.opciones && r.opciones.length) {
            var os = el('div', 'kk-as-opts');
            var limite = limiteOpciones();
            var ocultas = [];
            r.opciones.forEach(function (o, i) {
                var c = el('button', 'kk-as-chip', o.texto); c.type = 'button'; c.setAttribute('data-send', o.envio);
                if (i >= limite && r.opciones.length > limite + 1) { c.hidden = true; ocultas.push(c); }
                os.appendChild(c);
            });
            if (ocultas.length) {
                var mas = el('button', 'kk-as-chip kk-as-mas', 'Ver ' + ocultas.length + ' más ▾'); mas.type = 'button';
                mas.addEventListener('click', function () { ocultas.forEach(function (c) { c.hidden = false; }); mas.remove(); scrollFin(); });
                os.appendChild(mas);
            }
            b.appendChild(os);
        }
        m.appendChild(b);
        m.appendChild(el('time', 'kk-as-time', ahora()));
        body.appendChild(m); scrollFin(); guardar();
        return m;
    }

    function escribiendo(on) {
        var t = body.querySelector('.kk-as-typing');
        if (on && !t) { t = el('div', 'kk-as-msg kk-as-bot kk-as-typing'); t.appendChild(el('div', 'kk-as-bubble', '…')); body.appendChild(t); scrollFin(); }
        if (!on && t) t.remove();
    }

    // ───────── Comunicación con el servidor ─────────
    function enviarForm(handler, datos) {
        var fd = new FormData();
        Object.keys(datos).forEach(function (k) { fd.append(k, datos[k] == null ? '' : datos[k]); });
        if (tokenInput) fd.append('__RequestVerificationToken', tokenInput.value);
        return fetch(baseUrl + '?handler=' + handler, { method: 'POST', body: fd, credentials: 'same-origin' })
            .then(function (res) {
                if (!res.ok) throw new Error('http ' + res.status);
                return res.json();
            });
    }

    function enviar(texto) {
        texto = (texto || '').trim();
        if (!texto || busy) return;
        busy = true; bEnviar.disabled = true;
        burbujaUsuario(texto);
        input.value = '';
        escribiendo(true);
        var envio = ctx ? JSON.parse(JSON.stringify(ctx)) : {};
        if (seg) { envio.ultimaAccion = seg.ultimaAccion; envio.ultimaParams = seg.ultimaParams; }
        enviarForm('Mensaje', { texto: texto, contexto: (ctx || seg) ? JSON.stringify(envio) : '' })
            .then(function (r) {
                escribiendo(false);
                ctx = (r.tipo === 'faltante' && r.contexto) ? r.contexto : null;
                if (r.seguimiento) seg = r.seguimiento;
                input.placeholder = ctx ? 'Escribe tu respuesta… (o “cancelar”)' : 'Escribe: ventas de hoy, saldo de un cliente, cerrar caja…';
                burbujaBot(r);
            })
            .catch(function () {
                escribiendo(false);
                burbujaBot({ tipo: 'error', mensaje: 'No pude comunicarme con el servidor. Revisa tu conexión e intenta de nuevo.' });
            })
            .then(function () { busy = false; bEnviar.disabled = false; input.focus(); });
    }

    function confirmar(f) {
        var err = f.querySelector('.kk-as-error');
        var pw = f.querySelector('input[name="password"]');
        var au = f.querySelector('input[name="autorizador"]');
        var ok = f.querySelector('button[type="submit"]');
        if (!pw.value) { err.textContent = 'Escribe la contraseña.'; return; }
        ok.disabled = true; err.textContent = '';
        enviarForm('Confirmar', { token: f.getAttribute('data-token'), password: pw.value, autorizador: au && !au.hidden ? au.value : '' })
            .then(function (r) {
                pw.value = '';
                if (r.tipo === 'error') {
                    ok.disabled = false;
                    err.textContent = r.mensaje;
                    return;
                }
                var n = el('div', 'kk-as-note', '✔ Confirmado');
                f.replaceWith(n);
                burbujaBot(r);
            })
            .catch(function () { ok.disabled = false; err.textContent = 'No pude comunicarme con el servidor.'; });
    }

    // ───────── Imprimir / CSV ─────────
    function esc(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }

    function imprimir(p) {
        var w = window.open('', '_blank');
        if (!w) { burbujaBot({ tipo: 'error', mensaje: 'El navegador bloqueó la ventana. Permite las ventanas emergentes para imprimir.' }); return; }
        var num = p.numericas || [];
        var h = '<!doctype html><html lang="es"><head><meta charset="utf-8"><title>' + esc(p.titulo) + '</title><style>' +
            'body{font-family:Segoe UI,Arial,sans-serif;color:#111;margin:24px}h1{font-size:20px;margin:0}h2{font-size:15px;font-weight:600;margin:2px 0 4px}' +
            'p{margin:4px 0 14px;color:#444;font-size:13px}table{width:100%;border-collapse:collapse;font-size:12px}' +
            'th,td{padding:6px 8px;border-bottom:1px solid #ddd;text-align:left}th{background:#f3f4f6;border-bottom:2px solid #999}' +
            '.n{text-align:right}tfoot td{font-weight:700;border-top:2px solid #999}.pie{margin-top:18px;font-size:11px;color:#777}' +
            '@media print{body{margin:10mm}}</style></head><body>' +
            '<h1>' + esc(p.empresa) + '</h1><h2>' + esc(p.titulo) + '</h2><p>' + esc(p.mensaje) + '</p><table><thead><tr>';
        p.columnas.forEach(function (c, i) { h += '<th' + (num.indexOf(i) >= 0 ? ' class="n"' : '') + '>' + esc(c) + '</th>'; });
        h += '</tr></thead><tbody>';
        p.filas.forEach(function (f) {
            h += '<tr>'; f.forEach(function (v, i) { h += '<td' + (num.indexOf(i) >= 0 ? ' class="n"' : '') + '>' + esc(v) + '</td>'; }); h += '</tr>';
        });
        h += '</tbody>';
        if (p.totales) { h += '<tfoot><tr>'; p.totales.forEach(function (v, i) { h += '<td' + (num.indexOf(i) >= 0 ? ' class="n"' : '') + '>' + esc(v) + '</td>'; }); h += '</tr></tfoot>'; }
        h += '</table><div class="pie">Generado por el asistente el ' + esc(new Date().toLocaleString('es-HN')) + '</div></body></html>';
        w.document.open(); w.document.write(h); w.document.close();
        w.focus();
        setTimeout(function () { w.print(); }, 350);
    }

    function csv(p) {
        function celda(v) { v = String(v == null ? '' : v); return /[",\n;]/.test(v) ? '"' + v.replace(/"/g, '""') + '"' : v; }
        var lineas = [p.columnas.map(celda).join(',')];
        p.filas.forEach(function (f) { lineas.push(f.map(celda).join(',')); });
        if (p.totales) lineas.push(p.totales.map(celda).join(','));
        var blob = new Blob(['﻿' + lineas.join('\r\n')], { type: 'text/csv;charset=utf-8' });
        var a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = (p.titulo || 'reporte').toLowerCase().replace(/[^a-z0-9áéíóúñ]+/gi, '-').replace(/^-|-$/g, '') + '.csv';
        document.body.appendChild(a); a.click(); a.remove();
        setTimeout(function () { URL.revokeObjectURL(a.href); }, 2000);
    }

    // ───────── Eventos ─────────
    function abrir() {
        abierto = true; panel.hidden = false; fab.classList.add('kk-as-hide');
        if (!body.children.length && !restaurar()) {
            burbujaBot({ tipo: 'texto', mensaje: 'Hola 👋 Soy el asistente. Pídeme reportes, consultas o acciones: “ventas de hoy”, “saldo de un cliente”, “cerrar caja”… o escribe “ayuda”.' });
        }
        avisoInicial();
        cargarChips();
        scrollFin();
        setTimeout(function () { input.focus(); }, 50);
    }
    // Una vez por sesión del navegador: si hay algo que atender, el asistente lo dice al abrirse
    function avisoInicial() {
        try {
            if (sessionStorage.getItem(STORE + '-aviso')) return;
            sessionStorage.setItem(STORE + '-aviso', '1');
        } catch (e) { return; }
        enviarForm('Mensaje', { texto: 'que tengo pendiente', contexto: '' })
            .then(function (r) { if (r && r.titulo === 'Qué requiere tu atención') burbujaBot(r); })
            .catch(function () { });
    }
    function cerrar() { abierto = false; panel.hidden = true; fab.classList.remove('kk-as-hide'); fab.focus(); }

    function cargarChips() {
        if (catalogoCargado) return;
        catalogoCargado = true;
        fetch(baseUrl + '?handler=Catalogo', { credentials: 'same-origin' })
            .then(function (r) { return r.json(); })
            .then(function (lista) {
                var vistos = {}; var sel = [];
                lista.forEach(function (a) { if (!vistos[a.modulo] && sel.length < 6) { vistos[a.modulo] = 1; sel.push(a); } });
                chips.innerHTML = '';
                sel.forEach(function (a) {
                    var c = el('button', 'kk-as-chip', a.ejemplo); c.type = 'button'; c.setAttribute('data-send', a.ejemplo); chips.appendChild(c);
                });
                var h = el('button', 'kk-as-chip', 'ayuda'); h.type = 'button'; h.setAttribute('data-send', 'ayuda'); chips.appendChild(h);
            })
            .catch(function () { catalogoCargado = false; });
    }

    fab.addEventListener('click', abrir);
    bCerrar.addEventListener('click', cerrar);
    bLimpiar.addEventListener('click', function () {
        body.innerHTML = ''; ctx = null; seg = null; try { sessionStorage.removeItem(STORE); } catch (e) { }
        burbujaBot({ tipo: 'texto', mensaje: 'Conversación limpia. ¿En qué te ayudo?' });
    });
    form.addEventListener('submit', function (e) { e.preventDefault(); enviar(input.value); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && abierto) cerrar(); });

    body.addEventListener('submit', function (e) {
        var f = e.target.closest('.kk-as-confirm');
        if (f) { e.preventDefault(); confirmar(f); }
    });
    panel.addEventListener('click', function (e) {
        var t = e.target.closest('[data-send],[data-accion]');
        if (!t) return;
        if (t.hasAttribute('data-send')) { enviar(t.getAttribute('data-send')); return; }
        var a = t.getAttribute('data-accion');
        if (a === 'imprimir' || a === 'csv') {
            var wrap = t.closest('.kk-as-table');
            var p; try { p = JSON.parse(wrap.getAttribute('data-payload')); } catch (x) { return; }
            if (a === 'imprimir') imprimir(p); else csv(p);
        } else if (a === 'otro') {
            var f = t.closest('form'); var au = f.querySelector('.kk-as-autor');
            au.hidden = !au.hidden; if (!au.hidden) au.focus();
        } else if (a === 'cancelar') {
            var fc = t.closest('form');
            fc.replaceWith(el('div', 'kk-as-note', 'Cancelado. No se hizo ningún cambio.'));
            guardar();
        }
    });

    window.kkAsistente = {
        abrir: abrir,
        preguntar: function (texto) { abrir(); setTimeout(function () { enviar(texto); }, 80); }
    };
})();
