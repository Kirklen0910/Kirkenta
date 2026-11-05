// Enlaces automáticos: cualquier número de documento que aparezca en pantalla (VEN-0012, OC-0003,
// 000-001-01-00000120…) se vuelve un enlace a su detalle, y en las columnas «Cliente» / «Proveedor» el
// nombre lleva a su ficha. El servidor (/Ir?handler=Resolver) solo confirma lo que existe y el usuario
// puede abrir, así nunca queda un enlace roto. También cubre lo que las pantallas pintan después
// (informes, tablas que se filtran).
(function () {
    if (window.__kkEnlaces) return;
    window.__kkEnlaces = true;

    var RX = /\b(\d{3}-\d{3}-\d{2}-\d{8}|[A-Z][A-Z0-9]{0,11}(?:-[A-Z0-9]{1,12})*-\d{2,12})\b/g;
    var NO = 'A,BUTTON,INPUT,SELECT,TEXTAREA,SCRIPT,STYLE,OPTION,LABEL,CODE,PRE,SVG,CANVAS,NOSCRIPT,H1,H2';
    var noEntrar = {}; NO.split(',').forEach(function (t) { noEntrar[t] = true; });
    var conocidos = { docs: {}, clientes: {}, proveedores: {} };   // lo ya preguntado: url o null
    var aqui = location.pathname.toLowerCase();

    function raiz() { return document.querySelector('.erp-main') || document.querySelector('main'); }

    function saltar(n) {
        // el rango autorizado del SAR («Rango: 000-001-01-00001000 — …») no es un documento: queda como texto
        if (n.parentNode && /\brango\b/i.test(n.parentNode.textContent || '')) return true;
        for (var p = n.parentNode; p && p.nodeType === 1; p = p.parentNode) {
            if (noEntrar[p.tagName] || p.isContentEditable || p.classList.contains('no-autolink') ||
                p.classList.contains('module-header') || p.classList.contains('kk-as-panel')) return true;
        }
        return false;
    }

    function textos(r) {
        var w = document.createTreeWalker(r, NodeFilter.SHOW_TEXT, {
            acceptNode: function (n) { return n.nodeValue && /\d/.test(n.nodeValue) && !saltar(n) ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT; }
        });
        var out = []; var n;
        while ((n = w.nextNode())) out.push(n);
        return out;
    }

    // celdas de las columnas «Cliente» / «Proveedor» (el nombre ocupa toda la celda o su primera línea)
    function celdasTerceros(r) {
        var res = [];
        r.querySelectorAll('table').forEach(function (t) {
            var ths = t.querySelectorAll('thead th');
            if (!ths.length) return;
            var cols = {};
            ths.forEach(function (th, i) {
                var x = (th.textContent || '').trim().toLowerCase();
                if (/^cliente/.test(x)) cols[i] = 'clientes';
                else if (/^proveedor/.test(x)) cols[i] = 'proveedores';
            });
            if (!Object.keys(cols).length) return;
            t.querySelectorAll('tbody tr').forEach(function (tr) {
                Object.keys(cols).forEach(function (i) {
                    var td = tr.children[i];
                    if (!td || td.querySelector('a,button,input,select')) return;
                    var nodo = null;
                    for (var c = td.firstChild; c; c = c.nextSibling) {
                        if (c.nodeType === 3 && c.nodeValue.trim()) { nodo = c; break; }
                        if (c.nodeType === 1 && (c.tagName === 'STRONG' || c.tagName === 'SPAN' || c.tagName === 'B') && c.childNodes.length === 1 && c.firstChild.nodeType === 3) { nodo = c.firstChild; break; }
                    }
                    if (nodo) res.push({ nodo: nodo, tipo: cols[i], nombre: nodo.nodeValue.trim() });
                });
            });
        });
        return res;
    }

    function enlace(texto, url) {
        var a = document.createElement('a');
        a.href = url; a.className = 'kk-doc-link'; a.textContent = texto;
        a.title = 'Abrir ' + texto;
        return a;
    }

    function aplicar(r) {
        // documentos
        textos(r).forEach(function (n) {
            var s = n.nodeValue; RX.lastIndex = 0;
            var m, ult = 0, frag = null;
            while ((m = RX.exec(s))) {
                var url = conocidos.docs[m[1]];
                if (!url || url.toLowerCase() === aqui) continue;
                frag = frag || document.createDocumentFragment();
                frag.appendChild(document.createTextNode(s.slice(ult, m.index)));
                frag.appendChild(enlace(m[1], url));
                ult = m.index + m[1].length;
            }
            if (frag) { frag.appendChild(document.createTextNode(s.slice(ult))); n.parentNode.replaceChild(frag, n); }
        });
        // clientes y proveedores
        celdasTerceros(r).forEach(function (c) {
            var url = conocidos[c.tipo][c.nombre];
            if (!url || url.toLowerCase() === aqui || !c.nodo.parentNode) return;
            var v = c.nodo.nodeValue, i = v.indexOf(c.nombre);
            var frag = document.createDocumentFragment();
            frag.appendChild(document.createTextNode(v.slice(0, i)));
            frag.appendChild(enlace(c.nombre, url));
            frag.appendChild(document.createTextNode(v.slice(i + c.nombre.length)));
            c.nodo.parentNode.replaceChild(frag, c.nodo);
        });
    }

    var trabajando = false;
    function revisar() {
        var r = raiz();
        if (!r || trabajando) return;
        var nuevos = { docs: [], clientes: [], proveedores: [] };
        textos(r).forEach(function (n) {
            RX.lastIndex = 0; var m;
            while ((m = RX.exec(n.nodeValue))) if (!(m[1] in conocidos.docs)) { conocidos.docs[m[1]] = null; nuevos.docs.push(m[1]); }
        });
        celdasTerceros(r).forEach(function (c) {
            if (!(c.nombre in conocidos[c.tipo])) { conocidos[c.tipo][c.nombre] = null; nuevos[c.tipo].push(c.nombre); }
        });
        if (!nuevos.docs.length && !nuevos.clientes.length && !nuevos.proveedores.length) { aplicar(r); return; }

        trabajando = true;
        var lotes = [];
        for (var i = 0; i < Math.max(nuevos.docs.length, nuevos.clientes.length, nuevos.proveedores.length); i += 120)
            lotes.push({ d: nuevos.docs.slice(i, i + 120), c: nuevos.clientes.slice(i, i + 120), p: nuevos.proveedores.slice(i, i + 120) });
        Promise.all(lotes.map(function (l) {
            var q = '/Ir?handler=Resolver&docs=' + encodeURIComponent(l.d.join('|')) +
                '&clientes=' + encodeURIComponent(l.c.join('|')) + '&proveedores=' + encodeURIComponent(l.p.join('|'));
            return fetch(q, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } })
                .then(function (x) { return x.ok ? x.json() : null; })
                .then(function (j) {
                    if (!j) return;
                    ['docs', 'clientes', 'proveedores'].forEach(function (k) {
                        Object.keys(j[k] || {}).forEach(function (n) { conocidos[k][n] = j[k][n]; });
                    });
                }).catch(function () { });
        })).then(function () { trabajando = false; aplicar(r); });
    }

    var espera = null;
    function pronto() { clearTimeout(espera); espera = setTimeout(revisar, 350); }

    function iniciar() {
        var r = raiz();
        if (!r) return;
        revisar();
        new MutationObserver(function (cambios) {
            // solo lo que pinta la pantalla, no los enlaces que pone este mismo script
            for (var i = 0; i < cambios.length; i++) {
                var a = cambios[i].addedNodes;
                for (var j = 0; j < a.length; j++) if (!(a[j].nodeType === 1 && a[j].classList && a[j].classList.contains('kk-doc-link'))) { pronto(); return; }
            }
        }).observe(r, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', iniciar);
    else iniciar();
})();
