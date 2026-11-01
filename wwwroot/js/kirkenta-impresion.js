/* Impresión directa en la impresora térmica: botones con data-imprimir-directo="<url del handler>". */
(function () {
    'use strict';

    function token() {
        var t = document.querySelector('input[name="__RequestVerificationToken"]');
        return t ? t.value : '';
    }

    function aviso(ok, texto) {
        var a = document.getElementById('kkImprAviso');
        if (!a) {
            a = document.createElement('div');
            a.id = 'kkImprAviso';
            a.setAttribute('role', 'status');
            a.style.cssText = 'position:fixed;left:50%;bottom:24px;transform:translateX(-50%);max-width:90vw;padding:10px 16px;border-radius:10px;font-size:14px;z-index:9999;box-shadow:0 4px 16px rgba(0,0,0,.25);';
            document.body.appendChild(a);
        }
        a.style.background = ok ? 'var(--color-success-bg, #dcfce7)' : 'var(--color-danger-bg, #fee2e2)';
        a.style.color = ok ? 'var(--color-success-text, #166534)' : 'var(--color-danger-text, #991b1b)';
        a.textContent = (ok ? '🖨️ ' : '⚠️ ') + texto;
        a.style.display = 'block';
        clearTimeout(a._t);
        a._t = setTimeout(function () { a.style.display = 'none'; }, ok ? 3500 : 7000);
    }

    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-imprimir-directo]');
        if (!btn) return;
        e.preventDefault();
        if (btn.disabled) return;
        btn.disabled = true;
        fetch(btn.getAttribute('data-imprimir-directo'), { method: 'POST', headers: { 'RequestVerificationToken': token() } })
            .then(function (r) { return r.json(); })
            .then(function (d) { aviso(!!d.ok, d.mensaje || (d.ok ? 'Enviado a la impresora.' : 'No se pudo imprimir.')); })
            .catch(function () { aviso(false, 'No se pudo comunicar con el servidor.'); })
            .then(function () { btn.disabled = false; });
    });
})();
