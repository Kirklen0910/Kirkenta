/* Kirkenta – ventas sin internet.
 * Registra el service worker y maneja la cola de ventas pendientes (IndexedDB).
 * API: KirkentaOffline.encolar(payload, resumen), .sincronizar(), .contar(), .listar(), .nuevoId()
 * El POS manda la venta normal; si falla la red, la deja en cola con el MISMO offlineId
 * y el servidor evita duplicarla al sincronizar. */
(function () {
    'use strict';
    if (window.KirkentaOffline) return;

    var DB = 'kirkenta-offline', ST = 'ventas', LS_SYNC = 'kk_offline_sync', LS_TOKEN = 'kk_antiforgery';
    var sincronizando = false, sesionExpirada = false, oyentes = [];

    // ---------- service worker ----------
    if ('serviceWorker' in navigator && (location.protocol === 'https:' || location.hostname === 'localhost' || location.hostname === '127.0.0.1')) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js', { scope: '/' }).catch(function () { });
        });
    }

    // ---------- IndexedDB ----------
    function abrir() {
        return new Promise(function (res, rej) {
            if (!window.indexedDB) return rej(new Error('Este navegador no permite guardar ventas sin conexión.'));
            var rq = indexedDB.open(DB, 1);
            rq.onupgradeneeded = function () { rq.result.createObjectStore(ST, { keyPath: 'offlineId' }); };
            rq.onsuccess = function () { res(rq.result); };
            rq.onerror = function () { rej(rq.error); };
        });
    }
    function tx(modo, fn) {
        return abrir().then(function (db) {
            return new Promise(function (res, rej) {
                var t = db.transaction(ST, modo), out = fn(t.objectStore(ST));
                t.oncomplete = function () { db.close(); res(out ? out.result : undefined); };
                t.onerror = t.onabort = function () { db.close(); rej(t.error); };
            });
        });
    }
    function listar() {
        return tx('readonly', function (s) { return s.getAll(); }).then(function (l) {
            return (l || []).sort(function (a, b) { return a.creado < b.creado ? -1 : 1; });
        }).catch(function () { return []; });
    }
    function guardar(rec) { return tx('readwrite', function (s) { return s.put(rec); }); }
    function borrar(id) { return tx('readwrite', function (s) { return s.delete(id); }); }

    function nuevoId() {
        if (window.crypto && crypto.randomUUID) return crypto.randomUUID();
        return 'k' + Date.now().toString(36) + Math.random().toString(36).slice(2, 12);
    }

    function encolar(payload, resumen) {
        var u = document.querySelector('.pos-user-name');
        var rec = {
            offlineId: payload.offlineId || nuevoId(),
            payload: payload,
            resumen: resumen || {},
            creado: new Date().toISOString(),
            estado: 'pendiente',
            intentos: 0,
            error: '',
            usuario: u ? u.textContent.trim() : ''
        };
        rec.payload.offlineId = rec.offlineId;
        return guardar(rec).then(function () { notificar(); return rec; });
    }

    // ---------- token antifalsificación ----------
    function tokenPagina() {
        var i = document.querySelector('input[name="__RequestVerificationToken"]');
        if (i && i.value) { try { localStorage.setItem(LS_TOKEN, i.value); } catch (e) { } return i.value; }
        try { return localStorage.getItem(LS_TOKEN) || ''; } catch (e) { return ''; }
    }
    function refrescarToken() {
        return fetch('/POS', { credentials: 'same-origin', cache: 'no-store' }).then(function (r) {
            if (r.redirected && /\/Auth\/Login/i.test(r.url)) throw { sesion: true };
            return r.text();
        }).then(function (h) {
            var m = h.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/) || h.match(/value="([^"]+)"[^>]*name="__RequestVerificationToken"/);
            if (!m) throw { sinToken: true };
            try { localStorage.setItem(LS_TOKEN, m[1]); } catch (e) { }
            return m[1];
        });
    }

    // ---------- envío ----------
    function post(rec, tok) {
        return fetch('/POS?handler=RegistrarVenta', {
            method: 'POST', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': tok },
            body: JSON.stringify(rec.payload)
        }).then(function (r) {
            if (r.redirected || /\/Auth\/Login/i.test(r.url) || r.status === 401 || r.status === 403) return { tipo: 'sesion' };
            if (r.status === 400) return { tipo: 'token' };
            if (r.status >= 500) return { tipo: 'servidor', msg: 'El servidor respondió con error (' + r.status + ').' };
            return r.json().then(function (d) {
                return d && d.success ? { tipo: 'ok', data: d } : { tipo: 'rechazo', msg: String((d && d.error) || 'La venta fue rechazada.').replace(/^STOCK_INSUFICIENTE\|/, '') };
            }, function () { return { tipo: 'servidor', msg: 'Respuesta no válida del servidor.' }; });
        });
    }

    function intentar(rec) {
        var tok = tokenPagina();
        var primero = tok ? post(rec, tok) : refrescarToken().then(function (t) { return post(rec, t); });
        return primero.then(function (r) {
            if (r.tipo !== 'token') return r;
            return refrescarToken().then(function (t) { return post(rec, t); }).then(function (r2) {
                return r2.tipo === 'token' ? { tipo: 'rechazo', msg: 'El servidor no aceptó la solicitud (400).' } : r2;
            });
        }).then(function (r) {
            if (r.tipo === 'ok') {
                sesionExpirada = false;
                registrarSincronizada(rec, r.data);
                return borrar(rec.offlineId).then(function () { return 'ok'; });
            }
            if (r.tipo === 'rechazo') {
                rec.estado = 'rechazada'; rec.error = r.msg;
                return guardar(rec).then(function () { return 'siguiente'; });
            }
            if (r.tipo === 'sesion') { sesionExpirada = true; return 'detener'; }
            rec.intentos = (rec.intentos || 0) + 1; rec.error = r.msg || '';
            return guardar(rec).then(function () { return 'detener'; });
        }).catch(function (e) {
            if (e && e.sesion) sesionExpirada = true;
            return 'detener';   // sin red: se queda pendiente
        });
    }

    function registrarSincronizada(rec, d) {
        try {
            var l = JSON.parse(localStorage.getItem(LS_SYNC) || '[]');
            l.unshift({ numero: d.numero, ventaId: d.ventaId, total: d.total, cuando: new Date().toISOString() });
            localStorage.setItem(LS_SYNC, JSON.stringify(l.slice(0, 15)));
        } catch (e) { }
        ultimas.push(d);
    }
    var ultimas = [];

    function sincronizar() {
        if (sincronizando || !navigator.onLine) return Promise.resolve(0);
        sincronizando = true; ultimas = [];
        return listar().then(function (lista) {
            var pend = lista.filter(function (r) { return r.estado === 'pendiente'; }), i = 0, ok = 0;
            function sig() {
                if (i >= pend.length) return Promise.resolve();
                return intentar(pend[i++]).then(function (r) {
                    if (r === 'ok') ok++;
                    return r === 'detener' ? null : sig();
                });
            }
            return sig().then(function () { return ok; });
        }).then(function (ok) {
            sincronizando = false; notificar();
            if (ok) avisar(ok);
            return ok;
        }, function () { sincronizando = false; return 0; });
    }

    // ---------- interfaz ----------
    var css = '.kk-pill{position:fixed;left:14px;bottom:14px;z-index:1085;display:none;align-items:center;gap:8px;border:0;border-radius:999px;padding:9px 16px;font:600 13px/1 system-ui,-apple-system,Segoe UI,sans-serif;color:#fff;background:#b45309;box-shadow:0 6px 18px rgba(0,0,0,.25);cursor:pointer}' +
        '.kk-pill.is-on{display:inline-flex}.kk-pill.is-err{background:#b91c1c}.kk-pill.is-ok{background:#047857}' +
        '.kk-ov{position:fixed;inset:0;z-index:1090;background:rgba(0,0,0,.5);display:none;align-items:center;justify-content:center;padding:16px}.kk-ov.is-open{display:flex}' +
        '.kk-box{width:100%;max-width:560px;max-height:88vh;overflow:auto;background:var(--color-surface,#fff);color:var(--color-text,#1f2937);border:1px solid var(--color-border,#e5e7eb);border-radius:16px;padding:20px;font:14px/1.45 system-ui,-apple-system,Segoe UI,sans-serif}' +
        '.kk-box h3{margin:0 0 4px;font-size:18px}.kk-box small{color:var(--color-muted,#6b7280)}' +
        '.kk-it{border:1px solid var(--color-border,#e5e7eb);border-radius:12px;padding:12px;margin-top:10px}' +
        '.kk-it .r{display:flex;justify-content:space-between;gap:8px;align-items:center}' +
        '.kk-b{display:inline-block;border-radius:999px;padding:2px 9px;font-size:11.5px;font-weight:700;background:#fef3c7;color:#92400e}.kk-b.e{background:#fee2e2;color:#991b1b}' +
        '.kk-btn{border:1px solid var(--color-border,#d1d5db);background:transparent;color:inherit;border-radius:9px;padding:6px 12px;font-size:13px;font-weight:600;cursor:pointer;margin-right:6px}' +
        '.kk-btn.p{background:var(--color-primary,#4f46e5);color:#fff;border-color:transparent}' +
        '.kk-toast{position:fixed;left:50%;bottom:22px;transform:translateX(-50%);z-index:1095;background:#047857;color:#fff;border-radius:12px;padding:12px 18px;font:600 14px system-ui,sans-serif;box-shadow:0 8px 24px rgba(0,0,0,.3);max-width:92vw}.kk-toast a{color:#fff;text-decoration:underline;margin-left:8px}';
    var pill, ov, box;

    function dinero(n) { return 'L. ' + (Number(n) || 0).toLocaleString('es-HN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function esc(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }

    function montar() {
        if (pill) return;
        var st = document.createElement('style'); st.textContent = css; document.head.appendChild(st);
        pill = document.createElement('button'); pill.type = 'button'; pill.className = 'kk-pill';
        pill.addEventListener('click', abrirPanel); document.body.appendChild(pill);
        ov = document.createElement('div'); ov.className = 'kk-ov';
        box = document.createElement('div'); box.className = 'kk-box'; ov.appendChild(box);
        ov.addEventListener('click', function (e) { if (e.target === ov) ov.classList.remove('is-open'); });
        document.body.appendChild(ov);
    }

    function pintarPill(lista) {
        montar();
        var pend = lista.filter(function (r) { return r.estado === 'pendiente'; }).length;
        var rech = lista.length - pend, off = !navigator.onLine;
        var txt = '', cls = 'kk-pill is-on';
        if (rech > 0) { txt = '⚠️ ' + rech + (rech === 1 ? ' venta con error' : ' ventas con error'); cls += ' is-err'; }
        else if (pend > 0 && sesionExpirada) { txt = '🔒 Inicia sesión para enviar ' + pend + (pend === 1 ? ' venta' : ' ventas'); cls += ' is-err'; }
        else if (pend > 0) { txt = (off ? '📡 Sin conexión · ' : '⏳ ') + pend + (pend === 1 ? ' venta por enviar' : ' ventas por enviar'); }
        else if (off) { txt = '📡 Sin conexión'; }
        else { cls = 'kk-pill'; }
        pill.className = cls; pill.textContent = txt;
    }

    function notificar() {
        return listar().then(function (l) {
            pintarPill(l);
            if (ov && ov.classList.contains('is-open')) pintarPanel(l);
            oyentes.forEach(function (f) { try { f(l); } catch (e) { } });
            return l;
        });
    }

    function pintarPanel(lista) {
        var h = '<h3>Ventas sin internet</h3><small>' + (navigator.onLine ? 'Hay conexión.' : 'Sin conexión: se enviarán solas al volver.') + '</small>';
        if (!lista.length) h += '<p style="margin:14px 0 0">No hay ventas pendientes. ✅</p>';
        lista.forEach(function (r) {
            var rs = r.resumen || {}, lineas = (rs.lineas || []).map(function (l) { return esc(l.c) + ' × ' + esc(l.n); }).join(', ');
            h += '<div class="kk-it"><div class="r"><strong>' + dinero(rs.total) + '</strong>' +
                '<span class="kk-b' + (r.estado === 'rechazada' ? ' e' : '') + '">' + (r.estado === 'rechazada' ? 'Rechazada' : 'Pendiente') + '</span></div>' +
                '<small>' + new Date(r.creado).toLocaleString('es-HN') + (r.usuario ? ' · ' + esc(r.usuario) : '') + (rs.cliente ? ' · ' + esc(rs.cliente) : '') + '</small>' +
                (lineas ? '<div style="margin-top:6px">' + lineas + '</div>' : '') +
                (r.error ? '<div style="margin-top:6px;color:#b91c1c">' + esc(r.error) + '</div>' : '') +
                '<div style="margin-top:10px">' +
                (r.estado === 'rechazada' ? '<button class="kk-btn p" data-a="reint" data-id="' + esc(r.offlineId) + '">Reintentar</button>' : '') +
                '<button class="kk-btn" data-a="desc" data-id="' + esc(r.offlineId) + '">Descartar</button></div></div>';
        });
        var sync = [];
        try { sync = JSON.parse(localStorage.getItem(LS_SYNC) || '[]'); } catch (e) { }
        if (sync.length) {
            h += '<h4 style="font-size:14px;margin:18px 0 4px">Enviadas recientemente</h4>';
            sync.slice(0, 6).forEach(function (s) {
                h += '<div style="display:flex;justify-content:space-between;gap:8px;padding:5px 0;border-top:1px solid var(--color-border,#e5e7eb)"><span>' + esc(s.numero) + ' · ' + dinero(s.total) +
                    '</span><a href="/Facturas/CreateFromVenta?ventaId=' + encodeURIComponent(s.ventaId) + '">Facturar</a></div>';
            });
        }
        h += '<div style="margin-top:16px;text-align:right"><button class="kk-btn p" data-a="sync">Enviar ahora</button><button class="kk-btn" data-a="cerrar">Cerrar</button></div>';
        box.innerHTML = h;
    }

    function abrirPanel() { montar(); listar().then(function (l) { pintarPanel(l); ov.classList.add('is-open'); }); }

    document.addEventListener('click', function (e) {
        var b = e.target.closest && e.target.closest('.kk-box [data-a]');
        if (!b) return;
        var a = b.getAttribute('data-a'), id = b.getAttribute('data-id');
        if (a === 'cerrar') ov.classList.remove('is-open');
        else if (a === 'sync') sincronizar().then(notificar);
        else if (a === 'desc') {
            if (confirm('¿Descartar esta venta? No se registrará en el sistema.')) borrar(id).then(notificar);
        } else if (a === 'reint') {
            listar().then(function (l) {
                var r = l.filter(function (x) { return x.offlineId === id; })[0];
                if (!r) return;
                r.estado = 'pendiente'; r.error = '';
                guardar(r).then(function () { return sincronizar(); }).then(notificar);
            });
        }
    });

    function avisar(n) {
        montar();
        var t = document.createElement('div'); t.className = 'kk-toast';
        var u = ultimas[0];
        t.innerHTML = '✔ ' + (n === 1 ? 'Venta enviada' : n + ' ventas enviadas') + (u && u.numero ? ' (' + esc(u.numero) + ')' : '') +
            (n === 1 && u && u.ventaId ? '<a href="/Facturas/CreateFromVenta?ventaId=' + encodeURIComponent(u.ventaId) + '">Facturar</a>' : '');
        document.body.appendChild(t);
        setTimeout(function () { t.remove(); }, 9000);
    }

    // ---------- arranque ----------
    function iniciar() {
        montar(); notificar();
        setTimeout(function () { sincronizar(); }, 2500);
        setInterval(function () { listar().then(function (l) { if (l.some(function (r) { return r.estado === 'pendiente'; })) sincronizar(); }); }, 30000);
    }
    window.addEventListener('online', function () { notificar(); sincronizar(); });
    window.addEventListener('offline', notificar);
    document.addEventListener('visibilitychange', function () { if (!document.hidden) { notificar(); sincronizar(); } });
    tokenPagina();
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', iniciar); else iniciar();

    window.KirkentaOffline = {
        encolar: encolar, sincronizar: sincronizar, listar: listar, nuevoId: nuevoId,
        contar: function () { return listar().then(function (l) { return l.length; }); },
        onCambio: function (f) { oyentes.push(f); }
    };
})();
