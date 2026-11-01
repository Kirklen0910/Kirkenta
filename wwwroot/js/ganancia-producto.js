// Calculadora de ganancia en Crear/Editar producto.
// % de ganancia = sobre el COSTO: precio venta = costo × (1 + %/100). El ISV se suma aparte en la venta.
// Si el último campo editado fue el %, al cambiar el costo se recalcula el precio; si fue el precio, se recalcula el %.
(function () {
    var costo = document.getElementById('Input_PrecioCompra');
    var venta = document.getElementById('Input_PrecioVenta');
    var pct = document.getElementById('gananciaPct');
    var info = document.getElementById('gananciaInfo');
    if (!costo || !venta || !pct || !info) return;

    var fijo = 'venta';
    var num = function (el) { var v = parseFloat(el.value); return isNaN(v) ? 0 : v; };
    var l = function (v) { return 'L. ' + v.toLocaleString('es-HN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); };

    function mostrar() {
        var c = num(costo), v = num(venta);
        if (c <= 0 || v <= 0) { info.textContent = 'Ingresa costo y precio para ver la ganancia.'; info.className = 'form-help'; return; }
        var g = v - c;
        info.textContent = 'Ganancia ' + l(g) + ' por unidad · margen sobre la venta ' + (g / v * 100).toFixed(1) + '%';
        info.className = 'form-help' + (g < 0 ? ' text-danger' : '');
    }

    function pctDesdePrecio() {
        var c = num(costo);
        pct.value = c > 0 && num(venta) > 0 ? ((num(venta) - c) / c * 100).toFixed(2) : '';
    }

    function precioDesdePct() {
        var c = num(costo);
        if (c > 0 && pct.value !== '') venta.value = (Math.round(c * (1 + num(pct) / 100) * 100) / 100).toFixed(2);
    }

    pct.addEventListener('input', function () { fijo = 'pct'; precioDesdePct(); mostrar(); });
    venta.addEventListener('input', function () { fijo = 'venta'; pctDesdePrecio(); mostrar(); });
    costo.addEventListener('input', function () { if (fijo === 'pct') precioDesdePct(); else pctDesdePrecio(); mostrar(); });

    pctDesdePrecio();
    mostrar();
})();
