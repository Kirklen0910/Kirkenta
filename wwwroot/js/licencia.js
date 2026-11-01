// Botón "Copiar" del código de la computadora (pantalla de licencia).
(function () {
    var btn = document.getElementById('copiarCodigo');
    var campo = document.getElementById('codigoMaquina');
    if (!btn || !campo) return;
    btn.addEventListener('click', function () {
        campo.select();
        var listo = function () { btn.textContent = '¡Copiado!'; setTimeout(function () { btn.textContent = 'Copiar'; }, 1500); };
        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(campo.value).then(listo, function () { document.execCommand('copy'); listo(); });
        } else {
            try { document.execCommand('copy'); listo(); } catch (e) { }
        }
    });
})();
