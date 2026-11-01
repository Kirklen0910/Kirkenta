/* Catálogo en línea público de Kirkenta */
(function () {
    var datos = document.getElementById('catData');
    var PRODUCTOS = JSON.parse(datos.getAttribute('data-productos') || '[]');
    var CATEGORIAS = JSON.parse(datos.getAttribute('data-categorias') || '[]');
    var MINIMO = parseFloat(datos.getAttribute('data-minimo')) || 0;
    var MOSTRAR_STOCK = datos.getAttribute('data-stock') === 'true';
    var KEY = 'kk_catalogo_carrito';
    var carrito = {}, cat = '', filtro = '';

    function esc(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }
    function L(n) { return 'L. ' + (Number(n) || 0).toLocaleString('es-HN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function porId(id) { for (var i = 0; i < PRODUCTOS.length; i++) if (PRODUCTOS[i].id === id) return PRODUCTOS[i]; return null; }
    function norm(s) { return String(s || '').toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, ''); }

    try { carrito = JSON.parse(localStorage.getItem(KEY) || '{}') || {}; } catch (e) { carrito = {}; }
    Object.keys(carrito).forEach(function (k) { if (!porId(parseInt(k, 10))) delete carrito[k]; });
    function guardar() { try { localStorage.setItem(KEY, JSON.stringify(carrito)); } catch (e) { } }

    function totales() {
        var n = 0, t = 0;
        Object.keys(carrito).forEach(function (k) { var p = porId(parseInt(k, 10)); if (p) { n += carrito[k]; t += carrito[k] * p.p; } });
        return { n: n, t: Math.round(t * 100) / 100 };
    }

    function cambiar(id, d) {
        var p = porId(id); if (!p) return;
        var nuevo = (carrito[id] || 0) + d;
        if (nuevo > p.max) { nuevo = p.max; }
        if (nuevo <= 0) delete carrito[id]; else carrito[id] = nuevo;
        guardar(); pintar(); pintarBarra(); if (ov.classList.contains('on')) pintarCarrito();
    }

    function pintarCats() {
        var h = '<button class="' + (cat === '' ? 'on' : '') + '" data-c="">Todo</button>';
        CATEGORIAS.forEach(function (c) { h += '<button class="' + (String(c.id) === cat ? 'on' : '') + '" data-c="' + c.id + '">' + esc(c.n) + '</button>'; });
        document.getElementById('cats').innerHTML = CATEGORIAS.length ? h : '';
    }

    function pintar() {
        var q = norm(filtro), h = '', n = 0;
        PRODUCTOS.forEach(function (p) {
            if (cat !== '' && String(p.c) !== cat) return;
            if (q && norm(p.n + ' ' + p.d).indexOf(q) < 0) return;
            n++;
            var enCarrito = carrito[p.id] || 0;
            h += '<article class="p"><div class="ph">' + (p.i ? '<img loading="lazy" src="' + esc(p.i) + '" alt="" onerror="this.parentNode.textContent=\'' + esc(p.n.charAt(0).toUpperCase()) + '\'">' : esc(p.n.charAt(0).toUpperCase())) + '</div>' +
                '<div class="bd"><h3>' + esc(p.n) + '</h3>' + (p.d ? '<div class="ds">' + esc(p.d) + '</div>' : '') +
                '<div class="pr">' + L(p.p) + '</div>' +
                (MOSTRAR_STOCK && p.s != null ? '<div class="st">Disponibles: ' + p.s + '</div>' : '') +
                (enCarrito ? '<div class="qty"><button data-m="' + p.id + '">−</button><b>' + enCarrito + '</b><button data-a="' + p.id + '">+</button></div>'
                    : '<button class="add" data-a="' + p.id + '">Agregar</button>') +
                '</div></article>';
        });
        document.getElementById('grid').innerHTML = h;
        document.getElementById('vacio').style.display = n ? 'none' : 'block';
    }

    var bar = document.getElementById('bar'), ov = document.getElementById('ov');
    function pintarBarra() {
        var t = totales();
        bar.classList.toggle('on', t.n > 0);
        document.getElementById('barN').textContent = t.n;
        document.getElementById('barT').textContent = L(t.t);
    }

    function pintarCarrito() {
        var h = '', t = totales();
        Object.keys(carrito).forEach(function (k) {
            var p = porId(parseInt(k, 10)); if (!p) return;
            h += '<div class="it"><div><div class="nm">' + esc(p.n) + '</div><small>' + L(p.p) + ' c/u · ' + L(p.p * carrito[k]) + '</small></div>' +
                '<div class="qty"><button data-m="' + p.id + '">−</button><b>' + carrito[k] + '</b><button data-a="' + p.id + '">+</button></div></div>';
        });
        document.getElementById('items').innerHTML = h || '<p style="color:var(--mut)">Tu pedido está vacío.</p>';
        document.getElementById('tot').textContent = L(t.t);
        var em = document.getElementById('errMin');
        if (MINIMO > 0 && t.t < MINIMO && t.n > 0) { em.textContent = 'El pedido mínimo es ' + L(MINIMO) + '.'; em.style.display = 'block'; } else { em.style.display = 'none'; }
        if (t.n === 0) ov.classList.remove('on');
    }

    document.addEventListener('click', function (e) {
        var b = e.target.closest('[data-a],[data-m],[data-c]'); if (!b) return;
        if (b.hasAttribute('data-a')) cambiar(parseInt(b.getAttribute('data-a'), 10), 1);
        else if (b.hasAttribute('data-m')) cambiar(parseInt(b.getAttribute('data-m'), 10), -1);
        else if (b.closest('#cats')) { cat = b.getAttribute('data-c'); pintarCats(); pintar(); }
    });
    document.getElementById('q').addEventListener('input', function () { filtro = this.value; pintar(); });
    document.getElementById('verCarrito').addEventListener('click', function () {
        document.getElementById('paso1').style.display = ''; document.getElementById('paso2').style.display = 'none';
        pintarCarrito(); ov.classList.add('on');
    });
    document.getElementById('seguir').addEventListener('click', function () { ov.classList.remove('on'); });
    ov.addEventListener('click', function (e) { if (e.target === ov) ov.classList.remove('on'); });

    // recordar los datos del cliente en su propio navegador
    ['nombre', 'tel', 'dir'].forEach(function (id) {
        try { var v = localStorage.getItem('kk_cat_' + id); if (v) document.getElementById(id).value = v; } catch (e) { }
    });

    var enviando = false;
    document.getElementById('enviar').addEventListener('click', function () {
        if (enviando) return;
        var err = document.getElementById('err'); err.style.display = 'none';
        var t = totales();
        if (t.n === 0) return;
        if (MINIMO > 0 && t.t < MINIMO) { err.textContent = 'El pedido mínimo es ' + L(MINIMO) + '.'; err.style.display = 'block'; return; }
        var nombre = document.getElementById('nombre').value.trim(), tel = document.getElementById('tel').value.trim();
        if (nombre.length < 2) { err.textContent = 'Escribe tu nombre.'; err.style.display = 'block'; document.getElementById('nombre').focus(); return; }
        if (tel.replace(/\D/g, '').length < 8) { err.textContent = 'Escribe un teléfono válido para poder contactarte.'; err.style.display = 'block'; document.getElementById('tel').focus(); return; }
        try { localStorage.setItem('kk_cat_nombre', nombre); localStorage.setItem('kk_cat_tel', tel); localStorage.setItem('kk_cat_dir', document.getElementById('dir').value.trim()); } catch (e) { }

        var items = Object.keys(carrito).map(function (k) { return { id: parseInt(k, 10), cantidad: carrito[k] }; });
        var btn = this; enviando = true; btn.disabled = true; btn.textContent = 'Enviando…';
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        fetch('?handler=Pedido', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token ? token.value : '' },
            body: JSON.stringify({ nombre: nombre, telefono: tel, direccion: document.getElementById('dir').value, notas: document.getElementById('notas').value, web: document.getElementById('web').value, items: items })
        }).then(function (r) { return r.json(); }).then(function (d) {
            if (!d.ok) { err.textContent = d.error || 'No se pudo enviar el pedido.'; err.style.display = 'block'; return; }
            carrito = {}; guardar(); pintar(); pintarBarra();
            document.getElementById('okNum').textContent = d.numero;
            var wa = document.getElementById('okWa');
            if (d.whatsapp) { wa.href = d.whatsapp; wa.style.display = ''; } else { wa.style.display = 'none'; }
            document.getElementById('paso1').style.display = 'none'; document.getElementById('paso2').style.display = '';
            if (d.whatsapp) { window.location.href = d.whatsapp; }
        }).catch(function () {
            err.textContent = 'Sin conexión. Revisa tu internet e inténtalo otra vez.'; err.style.display = 'block';
        }).then(function () { enviando = false; btn.disabled = false; btn.textContent = '💬 Enviar pedido por WhatsApp'; });
    });
    document.getElementById('nuevo').addEventListener('click', function () { ov.classList.remove('on'); window.scrollTo(0, 0); });

    pintarCats(); pintar(); pintarBarra();
})();
