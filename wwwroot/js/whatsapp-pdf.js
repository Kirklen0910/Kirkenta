// "PDF por WhatsApp" (parcial Shared/_WhatsAppPdf): genera el PDF del documento, lo descarga y abre el chat
// de WhatsApp con el mensaje en texto; en el chat se adjunta el archivo descargado.
// - Con data-doc-url (ej. Facturas/Carta): esa página expone window.kkGenerarPdf().
// - Sin data-doc-url: la sección [data-pdf-doc] de esta página se copia a una hoja en blanco (iframe con
//   los mismos estilos, tema claro, sin menú, barras ni botones) y esa hoja se convierte a PDF.
(function () {
    if (window.__kkWhatsAppPdf) return;
    window.__kkWhatsAppPdf = true;

    var LIB = '/lib/html2pdf/html2pdf.bundle.min.js';
    // En el PDF se ocultan botones y navegación; los elementos .pdf-only (ocultos en pantalla) sí se muestran
    var OCULTAR = '.module-header-right, .back-link, .no-pdf, .wa-pdf, form, button, .kk-as-fab, .kk-as-panel { display: none !important; } .pdf-only { display: block !important; }';

    function iframeOculto() {
        var f = document.createElement('iframe');
        f.setAttribute('aria-hidden', 'true');
        // Ancho = página carta menos márgenes (~756 px): si la ventana es más ancha, html2pdf deja una franja gris a la derecha
        f.style.cssText = 'position:fixed;left:-10000px;top:0;width:760px;height:1600px;border:0;';
        return f;
    }

    function cargarIframe(src) {
        return new Promise(function (resolve, reject) {
            var f = iframeOculto();
            f.onload = function () { resolve(f); };
            f.onerror = function () { f.remove(); reject(new Error('No se pudo cargar el documento.')); };
            f.src = src;
            document.body.appendChild(f);
        });
    }

    // Hoja en blanco con los estilos del sistema y solo el documento
    function hojaConDocumento(el) {
        var estilos = Array.prototype.map.call(document.querySelectorAll('link[rel="stylesheet"]'), function (l) {
            return '<link rel="stylesheet" href="' + l.href + '">';
        }).join('');
        var html = '<!DOCTYPE html><html lang="es" data-theme="light"><head><meta charset="utf-8"><base href="' + location.origin + '/">' + estilos +
            '<style>html,body{background:#fff !important;margin:0;padding:0;}' + OCULTAR + '</style></head>' +
            '<body>' + el.outerHTML + '</body></html>';
        return new Promise(function (resolve) {
            var f = iframeOculto();
            f.onload = function () { resolve(f); };
            f.srcdoc = html;
            document.body.appendChild(f);
        });
    }

    function cargarLibreria(w) {
        if (w.html2pdf) return Promise.resolve();
        return new Promise(function (resolve, reject) {
            var s = w.document.createElement('script');
            s.src = LIB;
            s.onload = function () { resolve(); };
            s.onerror = function () { reject(new Error('No se pudo cargar el generador de PDF.')); };
            w.document.head.appendChild(s);
        });
    }

    function pdfDeSeccion(w, nombre) {
        var el = w.document.querySelector('[data-pdf-doc]');
        if (!el) return Promise.reject(new Error('Esta página no tiene un documento para convertir.'));
        return cargarLibreria(w).then(function () {
            // Las opciones se crean en la ventana del iframe: html2pdf valida con "instanceof Array"
            // y un arreglo creado en esta ventana no pasa esa validación
            var opciones = w.JSON.parse(JSON.stringify({
                margin: [8, 8, 10, 8],
                filename: nombre,
                image: { type: 'jpeg', quality: 0.95 },
                html2canvas: { scale: 2, useCORS: true, backgroundColor: '#ffffff' },
                jsPDF: { unit: 'mm', format: 'letter', orientation: 'portrait' },
                pagebreak: { mode: ['css', 'legacy'], avoid: 'tr' }
            }));
            return w.html2pdf().set(opciones).from(el).outputPdf('blob');
        });
    }

    function generarPdf(btn) {
        var docUrl = btn.dataset.docUrl;
        var seccion = docUrl ? null : document.querySelector('[data-pdf-doc]');
        if (!docUrl && !seccion) return Promise.reject(new Error('Esta página no tiene un documento para convertir.'));
        return pdfDe(docUrl || seccion, btn.dataset.archivo || 'Documento.pdf');
    }

    /// origen: dirección de una página (con [data-pdf-doc] o window.kkGenerarPdf) o un elemento de esta página
    function pdfDe(origen, nombre) {
        var hoja = typeof origen === 'string'
            // otra página: si no genera su propio PDF, su documento se pasa a una hoja limpia (tema claro, sin menú)
            ? cargarIframe(origen).then(function (f) {
                var w = f.contentWindow, el = w.document.querySelector('[data-pdf-doc]');
                if (typeof w.kkGenerarPdf === 'function' || !el) return f;
                var copia = document.importNode(el, true);
                f.remove();
                return hojaConDocumento(copia);
            })
            : hojaConDocumento(origen);
        return hoja.then(function (f) {
            var w = f.contentWindow;
            var p = typeof w.kkGenerarPdf === 'function'
                ? w.kkGenerarPdf().then(function (blob) { nombre = w.kkNombrePdf || nombre; return blob; })
                : pdfDeSeccion(w, nombre);
            return p.then(function (blob) { f.remove(); return { blob: blob, nombre: nombre }; },
                          function (e) { f.remove(); throw e; });
        });
    }

    function descargar(pdf) {
        var a = document.createElement('a');
        a.href = URL.createObjectURL(pdf.blob);
        a.download = pdf.nombre;
        document.body.appendChild(a); a.click(); a.remove();
        setTimeout(function () { URL.revokeObjectURL(a.href); }, 10000);
    }

    function numeroWhatsApp(btn, tel) {
        var d = (tel.value || '').replace(/\D/g, '');
        if (!d) return '';
        var largo = parseInt(btn.dataset.paisLargo || '8', 10);
        return d.length === largo ? (btn.dataset.paisCodigo || '504') + d : d;
    }

    // Para otras páginas: kkPdf.generar(urlOElemento, 'Nombre.pdf').then(kkPdf.descargar)
    window.kkPdf = { generar: pdfDe, descargar: descargar };

    document.addEventListener('click', function (e) {
        var btn = e.target.closest && e.target.closest('.wa-pdf-btn');
        if (!btn || btn.disabled) return;
        var caja = btn.closest('.wa-pdf');
        var tel = caja.querySelector('.wa-pdf-tel');
        var aviso = caja.querySelector('.wa-pdf-aviso');
        var etiqueta = btn.querySelector('span');
        var texto = etiqueta.textContent;
        var avisar = function (msg, error) { aviso.textContent = msg || ''; aviso.className = 'wa-pdf-aviso' + (error ? ' is-error' : ''); };

        avisar('');
        // La pestaña se abre dentro del clic para que el navegador no la bloquee; la dirección se pone al terminar el PDF
        var chat = window.open('', '_blank');
        etiqueta.textContent = 'Generando PDF…';
        btn.disabled = true;
        generarPdf(btn).then(function (pdf) {
            descargar(pdf);
            var url = 'https://wa.me/' + numeroWhatsApp(btn, tel) + '?text=' + encodeURIComponent(btn.dataset.mensaje || '');
            if (chat) { chat.opener = null; chat.location.href = url; } else { window.open(url, '_blank', 'noopener'); }
            avisar('PDF descargado (' + pdf.nombre + '). Adjúntalo en el chat con el clip 📎.');
        }).catch(function (err) {
            if (chat) chat.close();
            avisar('No se pudo generar el PDF: ' + (err && err.message ? err.message : err), true);
        }).then(function () {
            etiqueta.textContent = texto;
            btn.disabled = false;
        });
    });
})();
