// Toma física: ubicar el producto escaneando (pistola o cámara) o buscando, y contar sin recargar.
(function () {
    'use strict';
    var raiz = document.getElementById('tfContar');
    var form = document.getElementById('tfForm');
    var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenEl ? tokenEl.value : '';
    function $(id) { return document.getElementById(id); }
    function num(v) { return v == null ? '—' : (Math.round(v * 100) / 100).toLocaleString('es-HN', { maximumFractionDigits: 2 }); }
    function lps(v) { return 'L. ' + Number(v).toLocaleString('es-HN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }

    // ── guardar un conteo ──
    var urlContar = raiz ? raiz.dataset.contar : (form ? form.getAttribute('action').replace(/handler=Guardar/, 'handler=Contar') : '');
    function contar(detalleId, cantidad, sumar) {
        var fd = new FormData();
        fd.append('detalleId', detalleId);
        fd.append('cantidad', cantidad == null ? '' : String(cantidad));
        fd.append('sumar', sumar ? 'true' : 'false');
        fd.append('__RequestVerificationToken', token);
        return fetch(urlContar, { method: 'POST', body: fd, credentials: 'same-origin' })
            .then(function (r) { return r.json().catch(function () { return { error: 'No se pudo guardar (' + r.status + ')' }; }); })
            .then(function (d) {
                if (d.error) throw new Error(d.error);
                totales(d);
                fila(d.item);
                return d.item;
            });
    }

    function totales(d) {
        if (!$('tfContados')) return;
        $('tfContados').textContent = d.contados;
        $('tfTotal').textContent = d.total;
        var pct = d.total ? Math.round(d.contados * 100 / d.total) : 0;
        $('tfPct').textContent = pct + '%';
        $('tfBarra').style.width = pct + '%';
        $('tfPend').textContent = d.total - d.contados;
        $('tfDifs').textContent = d.conDiferencia;
        var imp = $('tfImpacto');
        imp.textContent = lps(d.impacto);
        imp.className = d.impacto < 0 ? 'tf-neg' : d.impacto > 0 ? 'tf-pos' : '';
    }

    function difTexto(it) {
        if (it.contado == null) return '';
        var d = it.contado - it.sistema;
        return d === 0 ? 'Cuadra' : (d > 0 ? '+' : '') + num(d);
    }
    function difClase(it) {
        if (it.contado == null) return 'tf-dif';
        var d = it.contado - it.sistema;
        return 'tf-dif ' + (d > 0 ? 'tf-dif-pos' : d < 0 ? 'tf-dif-neg' : 'tf-dif-ok');
    }

    // actualiza la fila de la lista si está en pantalla
    function fila(it) {
        var tr = document.querySelector('tr[data-detalle="' + it.id + '"]');
        if (!tr) return;
        var inp = tr.querySelector('.tf-celda');
        if (inp && document.activeElement !== inp) inp.value = it.contado == null ? '' : String(it.contado);
        var s = tr.querySelector('.tf-dif');
        if (s) { s.className = difClase(it); s.textContent = difTexto(it); }
        // verde si cuadra, rojo si no, sin color mientras no se cuente
        tr.classList.toggle('is-cuadra', it.contado != null && it.contado === it.sistema);
        tr.classList.toggle('is-difiere', it.contado != null && it.contado !== it.sistema);
        tr.classList.remove('is-flash'); void tr.offsetWidth; tr.classList.add('is-flash');
    }

    // ── aplicar: pide autorización y muestra lo que va a pasar ──
    var dialogo = $('tfDialogo');
    if (dialogo) {
        $('tfAbrirAplicar').addEventListener('click', function () {
            $('tfDlgContados').textContent = $('tfContados').textContent;
            $('tfDlgDifs').textContent = $('tfDifs').textContent;
            $('tfDlgPend').textContent = $('tfPend').textContent;
            dialogo.showModal();
            $('tfAutUsuario').focus();
        });
        $('tfCerrarDialogo').addEventListener('click', function () { $('tfAutClave').value = ''; dialogo.close(); });
    }

    // ── la lista se guarda sola ──
    if (form) {
        form.classList.add('tf-js');
        form.addEventListener('change', function (e) {
            var inp = e.target;
            if (!inp.classList || !inp.classList.contains('tf-celda')) return;
            var tr = inp.closest('tr');
            var v = inp.value.trim().replace(',', '.');
            if (v !== '' && (isNaN(Number(v)) || Number(v) < 0)) { inp.classList.add('is-error'); inp.title = 'Escribe una cantidad válida'; return; }
            inp.classList.remove('is-error'); inp.classList.add('is-guardando');
            contar(tr.dataset.detalle, v === '' ? null : Number(v), false)
                .then(function () { inp.classList.remove('is-guardando'); inp.classList.add('is-ok'); setTimeout(function () { inp.classList.remove('is-ok'); }, 1200); })
                .catch(function (err) { inp.classList.remove('is-guardando'); inp.classList.add('is-error'); inp.title = err.message; });
        });
        // Enter en la lista: pasa al siguiente producto en vez de enviar el formulario
        form.addEventListener('keydown', function (e) {
            if (e.key !== 'Enter' || !e.target.classList.contains('tf-celda')) return;
            e.preventDefault();
            var celdas = [].slice.call(form.querySelectorAll('.tf-celda'));
            var i = celdas.indexOf(e.target);
            e.target.dispatchEvent(new Event('change', { bubbles: true }));
            if (celdas[i + 1]) { celdas[i + 1].focus(); celdas[i + 1].select(); }
        });
    }

    if (!raiz) return;

    // ── panel de conteo ──
    var codigos = {};
    try { codigos = JSON.parse($('tfCodigos').textContent || '{}'); } catch (e) { }
    var q = $('tfQ'), sug = $('tfSug'), ficha = $('tfFicha'), cant = $('tfCant'), sumarChk = $('tfSumar'), msg = $('tfMsg');
    var modo = 'fijar', actual = null, opciones = [], marcado = -1, temporizador = 0, pedido = 0;
    var recientes = [];

    function urlBuscar(params) {
        var u = raiz.dataset.buscar;
        return u + (u.indexOf('?') >= 0 ? '&' : '?') + params;
    }
    function traer(params) {
        return fetch(urlBuscar(params), { credentials: 'same-origin' }).then(function (r) { return r.json(); }).then(function (d) { return d.items || []; });
    }

    function aviso(texto, tipo) {
        msg.textContent = texto || '';
        msg.className = 'tf-msg' + (tipo ? ' tf-msg-' + tipo : '');
    }

    function mostrar(it, enfocar) {
        actual = it;
        ficha.hidden = false;
        $('tfNombre').textContent = it.nombre;
        $('tfMeta').textContent = [it.sku, it.codigo && it.codigo !== it.sku ? it.codigo : null, it.unidad].filter(Boolean).join(' · ');
        $('tfSistema').textContent = num(it.sistema);
        $('tfContado').textContent = it.contado == null ? 'Sin contar' : num(it.contado);
        var d = $('tfDif');
        d.textContent = it.contado == null ? '—' : difTexto(it);
        d.className = it.contado == null ? '' : difClase(it).replace('tf-dif ', '');
        // con algo ya contado, lo natural es sumar (otra caja); si no, escribir la cantidad
        sumarChk.checked = false;
        cant.value = '';
        cant.placeholder = it.contado == null ? '0' : num(it.contado);
        ficha.classList.remove('is-flash'); void ficha.offsetWidth; ficha.classList.add('is-flash');
        if (enfocar) { cant.focus(); }
    }

    function cerrarFicha() { ficha.hidden = true; actual = null; aviso(''); }

    function reciente(it, texto) {
        recientes = recientes.filter(function (r) { return r.it.id !== it.id; });
        recientes.unshift({ it: it, texto: texto });
        recientes = recientes.slice(0, 6);
        var box = $('tfRecientes'), ul = box.querySelector('ul');
        ul.textContent = '';
        recientes.forEach(function (r) {
            var li = document.createElement('li');
            var b = document.createElement('button');
            b.type = 'button';
            var n = document.createElement('span'); n.className = 'tf-r-nombre'; n.textContent = r.it.nombre;
            var c = document.createElement('span'); c.className = 'tf-r-cant'; c.textContent = num(r.it.contado);
            var df = document.createElement('span'); df.className = difClase(r.it); df.textContent = difTexto(r.it);
            b.appendChild(n); b.appendChild(c); b.appendChild(df);
            b.title = 'Volver a este producto';
            b.addEventListener('click', function () { mostrar(r.it, true); });
            li.appendChild(b); ul.appendChild(li);
        });
        box.hidden = recientes.length === 0;
    }

    function guardar() {
        if (!actual) return;
        var v = cant.value.trim().replace(',', '.');
        if (v === '') { cant.focus(); aviso('Escribe la cantidad contada.', 'error'); return; }
        var n = Number(v);
        if (isNaN(n)) { aviso('Escribe una cantidad válida.', 'error'); return; }
        var sumar = sumarChk.checked;
        if (!sumar && n < 0) { aviso('La cantidad no puede ser negativa.', 'error'); return; }
        $('tfGuardar').disabled = true;
        contar(actual.id, n, sumar).then(function (it) {
            mostrar(it, false);
            aviso('✓ Guardado: ' + num(it.contado) + ' ' + (it.unidad || '').toLowerCase(), 'ok');
            reciente(it);
            listoParaOtro();
        }).catch(function (e) { aviso(e.message, 'error'); })
          .then(function () { $('tfGuardar').disabled = false; });
    }

    function listoParaOtro() { q.value = ''; ocultarSug(); q.focus(); }

    // escaneo «suma 1»: cuenta al instante y queda listo para el siguiente
    function sumarUno(detalleId) {
        return contar(detalleId, 1, true).then(function (it) {
            mostrar(it, false);
            aviso('✓ +1 · van ' + num(it.contado), 'ok');
            reciente(it);
            return it;
        });
    }

    // código exacto (pistola o cámara) → producto
    function porCodigo(texto) {
        var k = (texto || '').trim().toLowerCase();
        return k && Object.prototype.hasOwnProperty.call(codigos, k) ? codigos[k] : null;
    }

    function elegir(it) {
        ocultarSug();
        if (modo === 'sumar') {
            sumarUno(it.id).then(listoParaOtro).catch(function (e) { aviso(e.message, 'error'); });
        } else {
            q.value = '';
            mostrar(it, true);
            aviso('');
        }
    }

    function elegirDetalle(id) {
        return traer('detalle=' + encodeURIComponent(id)).then(function (items) {
            if (items[0]) elegir(items[0]);
            return items[0];
        });
    }

    // ── sugerencias del buscador ──
    function ocultarSug() { sug.hidden = true; sug.textContent = ''; opciones = []; marcado = -1; q.setAttribute('aria-expanded', 'false'); }
    function pintarSug(items, texto) {
        opciones = items; marcado = items.length ? 0 : -1;
        sug.textContent = '';
        if (!items.length) {
            var li = document.createElement('li'); li.className = 'tf-sug-vacio';
            li.textContent = 'No encontré «' + texto + '» en esta toma.';
            sug.appendChild(li);
        }
        items.forEach(function (it, i) {
            var li = document.createElement('li');
            li.setAttribute('role', 'option'); li.id = 'tfSug' + i;
            li.className = i === 0 ? 'is-marcado' : '';
            var n = document.createElement('span'); n.className = 'tf-sug-nombre'; n.textContent = it.nombre;
            var m = document.createElement('span'); m.className = 'tf-sug-meta'; m.textContent = [it.sku, it.codigo && it.codigo !== it.sku ? it.codigo : null].filter(Boolean).join(' · ');
            var e = document.createElement('span'); e.className = 'tf-sug-estado' + (it.contado == null ? '' : ' is-contado');
            e.textContent = it.contado == null ? 'Sistema ' + num(it.sistema) : 'Contado ' + num(it.contado);
            li.appendChild(n); li.appendChild(m); li.appendChild(e);
            li.addEventListener('mousedown', function (ev) { ev.preventDefault(); elegir(it); });
            sug.appendChild(li);
        });
        sug.hidden = false;
        q.setAttribute('aria-expanded', 'true');
    }
    function marcar(i) {
        if (!opciones.length) return;
        marcado = (i + opciones.length) % opciones.length;
        [].forEach.call(sug.children, function (li, j) { li.classList.toggle('is-marcado', j === marcado); });
        q.setAttribute('aria-activedescendant', 'tfSug' + marcado);
        var li = sug.children[marcado]; if (li && li.scrollIntoView) li.scrollIntoView({ block: 'nearest' });
    }

    function buscar(texto) {
        var mio = ++pedido;
        return traer('q=' + encodeURIComponent(texto)).then(function (items) {
            if (mio !== pedido) return null;   // llegó una búsqueda más nueva
            return items;
        });
    }

    q.addEventListener('input', function () {
        clearTimeout(temporizador);
        var t = q.value.trim();
        if (t.length < 2) { ocultarSug(); return; }
        // una pistola lectora escribe muy rápido y termina con Enter: no hace falta sugerir
        temporizador = setTimeout(function () {
            buscar(t).then(function (items) { if (items && q.value.trim() === t) pintarSug(items, t); });
        }, 180);
    });

    q.addEventListener('keydown', function (e) {
        if (e.key === 'ArrowDown') { e.preventDefault(); marcar(marcado + 1); return; }
        if (e.key === 'ArrowUp') { e.preventDefault(); marcar(marcado - 1); return; }
        if (e.key === 'Escape') { ocultarSug(); return; }
        if (e.key !== 'Enter') return;
        e.preventDefault();
        clearTimeout(temporizador);
        var t = q.value.trim();
        if (!t) return;
        var id = porCodigo(t);
        if (id != null) { elegirDetalle(id); return; }
        if (opciones.length && marcado >= 0 && !sug.hidden) { elegir(opciones[marcado]); return; }
        buscar(t).then(function (items) {
            if (!items) return;
            if (items.length === 1) elegir(items[0]);
            else pintarSug(items, t);
        });
    });
    q.addEventListener('blur', function () { setTimeout(ocultarSug, 150); });

    // ── ficha ──
    $('tfGuardar').addEventListener('click', guardar);
    cant.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); guardar(); }
        if (e.key === 'Escape') { cerrarFicha(); q.focus(); }
    });
    $('tfCerrar').addEventListener('click', function () { cerrarFicha(); q.focus(); });
    [].forEach.call(raiz.querySelectorAll('.tf-paso'), function (b) {
        b.addEventListener('click', function () {
            var v = Number((cant.value || cant.placeholder || '0').replace(',', '.')) || 0;
            var n = Math.max(0, v + Number(b.dataset.paso));
            cant.value = String(Math.round(n * 10000) / 10000);
            cant.focus();
        });
    });

    // ── modo ──
    var ayudas = {
        fijar: 'Escanea o elige el producto y escribe cuántos hay.',
        sumar: 'Cada vez que escaneas un producto se suma 1. Ideal para contar pieza por pieza.'
    };
    [].forEach.call(raiz.querySelectorAll('.tf-seg button'), function (b) {
        b.addEventListener('click', function () {
            modo = b.dataset.modo;
            [].forEach.call(raiz.querySelectorAll('.tf-seg button'), function (x) {
                x.classList.toggle('is-on', x === b); x.setAttribute('aria-pressed', x === b);
            });
            $('tfAyuda').textContent = ayudas[modo];
            try { localStorage.setItem('kk-tf-modo', modo); } catch (e) { }
            q.focus();
        });
    });
    try {
        var guardado = localStorage.getItem('kk-tf-modo');
        if (guardado === 'sumar') raiz.querySelector('.tf-seg button[data-modo="sumar"]').click();
    } catch (e) { }

    // ── cámara ──
    $('tfCam').addEventListener('click', function () {
        if (!window.KirkentaEscaner) { aviso('El escáner de cámara no está disponible.', 'error'); return; }
        window.KirkentaEscaner.abrir(function (codigo) {
            var id = porCodigo(codigo);
            if (id == null) return false;
            if (modo === 'sumar') {
                // la cámara queda abierta para seguir contando
                sumarUno(id).catch(function (e) { aviso(e.message, 'error'); });
                return '✔ +1 · ' + codigo;
            }
            window.KirkentaEscaner.cerrar();
            elegirDetalle(id);
            return true;
        });
    });

    q.focus();
})();
