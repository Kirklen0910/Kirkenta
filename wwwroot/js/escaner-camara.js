/* Escáner de códigos de barras con la cámara (celular, tablet o laptop).
 * Usa la API nativa BarcodeDetector (Chrome / Edge / Android). Requiere https:// o localhost.
 * Uso: KirkentaEscaner.abrir(function (codigo) { ...; return true /* encontrado */ /* o false; });
 * El escáner queda abierto para leer varios productos seguidos. */
(function () {
    'use strict';
    if (window.KirkentaEscaner) return; // ya cargado (el POS y el layout lo incluyen)

    var overlay = null, video = null, estado = null, stream = null, timer = 0, detector = null;
    var ultimoCodigo = '', ultimoTiempo = 0, alLeer = null, audio = null;

    function pitido(ok) {
        try {
            audio = audio || new (window.AudioContext || window.webkitAudioContext)();
            var o = audio.createOscillator(), g = audio.createGain();
            o.type = 'sine'; o.frequency.value = ok ? 1250 : 330; g.gain.value = 0.08;
            o.connect(g); g.connect(audio.destination); o.start();
            setTimeout(function () { try { o.stop(); } catch (e) { } }, ok ? 110 : 260);
        } catch (e) { /* sin sonido */ }
    }

    function el(tag, css, texto) {
        var e = document.createElement(tag);
        if (css) e.style.cssText = css;
        if (texto) e.textContent = texto;
        return e;
    }

    function mensaje(texto, color) {
        if (!estado) return;
        estado.textContent = texto;
        estado.style.color = color || '#fff';
    }

    function cerrar() {
        if (timer) { clearInterval(timer); timer = 0; }
        if (stream) { stream.getTracks().forEach(function (t) { t.stop(); }); stream = null; }
        if (overlay && overlay.parentNode) overlay.parentNode.removeChild(overlay);
        overlay = video = estado = null; alLeer = null; ultimoCodigo = '';
        document.removeEventListener('keydown', teclaEsc, true);
    }

    function teclaEsc(e) { if (e.key === 'Escape') { e.stopPropagation(); cerrar(); } }

    function construir() {
        overlay = el('div', 'position:fixed;inset:0;z-index:100000;background:rgba(0,0,0,.92);display:flex;flex-direction:column;align-items:center;justify-content:center;padding:16px;gap:12px;');
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-label', 'Escáner de código de barras');
        var titulo = el('div', 'color:#fff;font:600 16px system-ui,sans-serif;', '📷 Apunta la cámara al código de barras');
        video = el('video', 'width:min(92vw,520px);max-height:55vh;border-radius:12px;background:#000;object-fit:cover;');
        video.setAttribute('playsinline', ''); video.muted = true;
        estado = el('div', 'color:#fff;font:14px system-ui,sans-serif;min-height:22px;text-align:center;max-width:90vw;', '');
        var cerrarBtn = el('button', 'background:#374151;color:#fff;border:0;border-radius:8px;padding:10px 22px;font:600 15px system-ui,sans-serif;cursor:pointer;', 'Cerrar escáner');
        cerrarBtn.type = 'button';
        cerrarBtn.addEventListener('click', cerrar);
        overlay.appendChild(titulo); overlay.appendChild(video); overlay.appendChild(estado); overlay.appendChild(cerrarBtn);
        document.body.appendChild(overlay);
        document.addEventListener('keydown', teclaEsc, true);
    }

    function soporte() {
        if (!window.isSecureContext) return 'La cámara solo se puede usar con https:// o desde localhost. Usa la pistola lectora o escribe el código.';
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) return 'Este navegador no permite usar la cámara.';
        if (!('BarcodeDetector' in window)) return 'Este navegador no trae lector de códigos de barras. Prueba con Chrome o Edge (Android o computadora), o usa la pistola lectora.';
        return '';
    }

    async function leerCuadro() {
        if (!video || video.readyState < 2) return;
        var codigos;
        try { codigos = await detector.detect(video); } catch (e) { return; }
        if (!codigos || !codigos.length || !overlay) return;
        var valor = (codigos[0].rawValue || '').trim();
        if (!valor) return;
        var ahora = Date.now();
        if (valor === ultimoCodigo && ahora - ultimoTiempo < 2500) return; // evita leer el mismo producto dos veces seguidas
        ultimoCodigo = valor; ultimoTiempo = ahora;
        var ok = false;
        try { ok = !!(alLeer && alLeer(valor)); } catch (e) { ok = false; }
        pitido(ok);
        mensaje(ok ? '✔ Agregado: ' + valor : '✖ No encontré el producto: ' + valor, ok ? '#86efac' : '#fca5a5');
    }

    async function abrir(callback) {
        if (overlay) return;
        alLeer = callback;
        construir();
        var problema = soporte();
        if (problema) { mensaje(problema, '#fde68a'); return; }
        try {
            var formatos = ['code_128', 'ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_39', 'itf'];
            if (BarcodeDetector.getSupportedFormats) {
                var disp = await BarcodeDetector.getSupportedFormats();
                formatos = formatos.filter(function (f) { return disp.indexOf(f) >= 0; });
            }
            detector = new BarcodeDetector({ formats: formatos });
            stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: 'environment' } }, audio: false });
            if (!overlay) { stream.getTracks().forEach(function (t) { t.stop(); }); return; }
            video.srcObject = stream;
            await video.play();
            mensaje('Listo. Puedes escanear varios productos seguidos.');
            timer = setInterval(leerCuadro, 250);
        } catch (e) {
            var negado = e && (e.name === 'NotAllowedError' || e.name === 'SecurityError');
            mensaje(negado ? 'No diste permiso para usar la cámara. Actívalo en el candado de la barra de direcciones.' : 'No se pudo abrir la cámara: ' + ((e && e.message) || e), '#fca5a5');
        }
    }

    // Botón 📷 automático en todo campo con data-escaner:
    //   data-escaner="llenar" → pone el código en el campo (ej. código de barras al crear un producto)
    //   data-escaner="enviar" → pone el código y envía el formulario (ej. búsquedas)
    function conectarCampos() {
        document.querySelectorAll('input[data-escaner]').forEach(function (inp) {
            if (inp.dataset.escanerListo) return;
            inp.dataset.escanerListo = '1';
            var btn = el('button', 'margin-left:6px;border:1px solid var(--color-border,#d1d5db);background:var(--color-surface-2,#f3f4f6);border-radius:8px;padding:6px 10px;font-size:16px;line-height:1;cursor:pointer;flex:none;', '📷');
            btn.type = 'button'; btn.title = 'Escanear con la cámara'; btn.setAttribute('aria-label', 'Escanear código de barras con la cámara');
            btn.addEventListener('click', function () {
                abrir(function (codigo) {
                    inp.value = codigo;
                    inp.dispatchEvent(new Event('input', { bubbles: true }));
                    inp.dispatchEvent(new Event('change', { bubbles: true }));
                    var enviar = inp.dataset.escaner === 'enviar' && inp.form;
                    setTimeout(function () {
                        cerrar();
                        if (enviar) { if (inp.form.requestSubmit) inp.form.requestSubmit(); else inp.form.submit(); }
                        else inp.focus();
                    }, 350);
                    return true;
                });
            });
            inp.insertAdjacentElement('afterend', btn);
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', conectarCampos); else conectarCampos();

    window.KirkentaEscaner = { abrir: abrir, cerrar: cerrar, soportado: function () { return soporte() === ''; } };
})();
