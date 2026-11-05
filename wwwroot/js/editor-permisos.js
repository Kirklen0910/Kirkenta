// Editor de permisos del rol (Usuarios/_EditorPermisos): plantillas por puesto, copiar de otro rol,
// nivel por módulo (sin acceso / solo ver / trabajar / todo), buscador y resumen en palabras claras.
// Los campos son los mismos de siempre (Input.Permisos[...]), así el guardado no cambia.
(function () {
    var raiz = document.getElementById('editorPermisos');
    if (!raiz) return;
    var datos = JSON.parse(document.getElementById('epDatos').textContent || '{}');
    var NIVEL = [[0, 0, 0, 0], [1, 0, 0, 0], [1, 1, 1, 0], [1, 1, 1, 1]];
    var NOMBRE_NIVEL = ['Sin acceso', 'Solo ver', 'Trabajar', 'Todo'];
    var secciones = [].slice.call(raiz.querySelectorAll('.ep-mod'));

    function filas(sec) { return [].slice.call(sec.querySelectorAll('.ep-op')); }
    function cajas(fila) { return [].slice.call(fila.querySelectorAll('.ep-sub')); }
    function flags(fila) { return cajas(fila).map(function (c) { return c.checked ? 1 : 0; }); }
    function igual(a, b) { return a.every(function (x, i) { return x === b[i]; }); }
    function ponerFila(fila, f) { cajas(fila).forEach(function (c, i) { c.checked = !!f[i]; }); }

    function nivelDe(sec) {
        var fs = filas(sec).map(flags);
        for (var n = 0; n < 4; n++) if (fs.every(function (f) { return igual(f, NIVEL[n]); })) return n;
        return -1;   // personalizado
    }

    function sincronizar(sec) {
        var fs = filas(sec).map(flags);
        // el permiso del módulo en sí: tiene cada acción si alguna de sus opciones la tiene
        sec.querySelectorAll('.ep-padre').forEach(function (p) {
            var i = +p.dataset.i;
            p.checked = fs.some(function (f) { return f[i]; });
        });
        var n = nivelDe(sec);
        sec.querySelectorAll('.ep-niveles button').forEach(function (b) { b.classList.toggle('is-on', +b.dataset.nivel === n); b.setAttribute('aria-pressed', +b.dataset.nivel === n); });
        sec.querySelector('.ep-personalizado').hidden = n !== -1;
        var con = fs.filter(function (f) { return f[0] || f[1] || f[2] || f[3]; }).length;
        sec.querySelector('.ep-mod-cuenta').textContent = con === 0 ? 'sin acceso' : con + ' de ' + fs.length + ' opciones';
        sec.classList.toggle('is-activo', con > 0);
    }

    function todo() {
        secciones.forEach(sincronizar);
        document.getElementById('epTotal').textContent = raiz.querySelectorAll('.ep-sub:checked').length;
        resumen();
    }

    function nivelModulo(sec, n) { filas(sec).forEach(function (f) { ponerFila(f, NIVEL[n]); }); }

    // estado: «Módulo/Opción» → nivel (plantillas) o → [ver, crear, editar, eliminar] (otro rol)
    function aplicar(estado) {
        secciones.forEach(function (sec) {
            filas(sec).forEach(function (f) {
                var v = estado[f.dataset.clave];
                ponerFila(f, v == null ? NIVEL[0] : typeof v === 'number' ? NIVEL[v] : v.map(function (x) { return x ? 1 : 0; }));
            });
        });
        todo();
    }

    function hayAlgo() { return raiz.querySelectorAll('.ep-sub:checked').length > 0; }

    // ── resumen en palabras claras ──
    function resumen() {
        var box = document.getElementById('epResumen');
        box.textContent = '';
        var partes = [];
        secciones.forEach(function (sec) {
            var n = nivelDe(sec), nombre = sec.querySelector('.ep-mod-nombre strong').textContent;
            if (n === 0) return;
            if (n !== -1) { partes.push(nombre + ': ' + NOMBRE_NIVEL[n].toLowerCase()); return; }
            // personalizado: qué opciones tiene, y si solo puede verlas
            var ops = filas(sec).map(function (f) {
                var x = flags(f);
                if (!(x[0] || x[1] || x[2] || x[3])) return null;
                return f.cells[0].firstChild.textContent.trim() + (x[1] || x[2] || x[3] ? '' : ' (solo ver)');
            }).filter(function (t) { return t; });
            partes.push(nombre + ': ' + ops.slice(0, 8).join(', ') + (ops.length > 8 ? ' y ' + (ops.length - 8) + ' más' : ''));
        });
        var h = document.createElement('strong');
        h.textContent = partes.length ? 'Este rol puede usar' : 'Este rol todavía no tiene ningún permiso';
        box.appendChild(h);
        if (partes.length) {
            var ul = document.createElement('ul');
            partes.forEach(function (t) { var li = document.createElement('li'); li.textContent = t; ul.appendChild(li); });
            box.appendChild(ul);
        }
        // opciones delicadas que tiene, y en qué módulos puede eliminar
        var delicadas = [], eliminar = [];
        secciones.forEach(function (sec) {
            var nombre = sec.querySelector('.ep-mod-nombre strong').textContent, elimina = false;
            filas(sec).forEach(function (f) {
                var x = flags(f);
                if (x[3]) elimina = true;
                if (f.dataset.delicada === '1' && (x[0] || x[1] || x[2] || x[3])) delicadas.push(f.cells[0].firstChild.textContent.trim() + ' (' + nombre + ')');
            });
            if (elimina) eliminar.push(nombre);
        });
        if (delicadas.length || eliminar.length) {
            var p = document.createElement('p');
            p.className = 'ep-alerta';
            p.textContent = '⚠ ' + (delicadas.length ? 'Opciones delicadas: ' + delicadas.slice(0, 10).join(', ') + (delicadas.length > 10 ? ' y ' + (delicadas.length - 10) + ' más' : '') + '. ' : '')
                + (eliminar.length ? 'Puede eliminar registros en: ' + eliminar.join(', ') + '.' : '');
            box.appendChild(p);
        }
    }

    // ── eventos ──
    secciones.forEach(function (sec) {
        sec.querySelectorAll('.ep-niveles button').forEach(function (b) {
            b.addEventListener('click', function () { nivelModulo(sec, +b.dataset.nivel); todo(); });
        });
        var btn = sec.querySelector('.ep-detalle-btn'), det = sec.querySelector('.ep-detalle');
        btn.addEventListener('click', function () {
            det.hidden = !det.hidden;
            btn.setAttribute('aria-expanded', !det.hidden);
            btn.textContent = det.hidden ? 'Detalle ▾' : 'Ocultar ▴';
        });
        sec.querySelectorAll('.ep-sub').forEach(function (c) {
            c.addEventListener('change', function () {
                var fila = c.closest('.ep-op'), cs = cajas(fila);
                // crear, editar o eliminar sin poder ver no tiene sentido: «ver» va con ellos
                if (c.checked && +c.dataset.i > 0) cs[0].checked = true;
                if (!c.checked && +c.dataset.i === 0) cs.forEach(function (x) { x.checked = false; });
                todo();
            });
        });
    });

    var sel = document.getElementById('epPlantilla'), desc = document.getElementById('epDescPlantilla');
    sel.addEventListener('change', function () {
        var p = (datos.plantillas || []).filter(function (x) { return x.clave === sel.value; })[0];
        if (!p) { desc.hidden = true; return; }
        if (hayAlgo() && !confirm('Esto reemplaza los permisos marcados por los de «' + p.nombre + '». ¿Continuar?')) { sel.value = ''; return; }
        aplicar(p.niveles || {});
        desc.hidden = false;
        desc.textContent = p.nombre + ': ' + p.descripcion + ' Revisa y ajusta lo que necesites.';
        var nombre = document.querySelector('input[name="Input.Nombre"]');
        if (nombre && !nombre.value.trim() && !nombre.readOnly) nombre.value = p.nombre;
    });

    var copiar = document.getElementById('epCopiar');
    if (copiar) copiar.addEventListener('change', function () {
        var r = (datos.roles || {})[copiar.value];
        if (!r) return;
        if (hayAlgo() && !confirm('Esto reemplaza los permisos marcados por los del rol «' + copiar.value + '». ¿Continuar?')) { copiar.value = ''; return; }
        aplicar(r);
        sel.value = ''; desc.hidden = false;
        desc.textContent = 'Permisos copiados de «' + copiar.value + '». Ajusta lo que sea distinto.';
    });

    document.getElementById('epTodo').addEventListener('click', function () { secciones.forEach(function (s) { nivelModulo(s, 3); }); todo(); });
    document.getElementById('epNada').addEventListener('click', function () { secciones.forEach(function (s) { nivelModulo(s, 0); }); todo(); });

    // ── buscador: deja solo las opciones que coinciden y abre su detalle ──
    function norm(s) { return String(s || '').toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, ''); }
    var buscar = document.getElementById('epBuscar');
    buscar.addEventListener('input', function () {
        var q = norm(buscar.value.trim());
        secciones.forEach(function (sec) {
            var hay = 0;
            filas(sec).forEach(function (f) {
                var ok = !q || norm(f.dataset.texto).indexOf(q) >= 0;
                f.hidden = !ok; if (ok) hay++;
            });
            sec.hidden = q && hay === 0;
            var det = sec.querySelector('.ep-detalle'), btn = sec.querySelector('.ep-detalle-btn');
            if (q) { det.hidden = false; btn.textContent = 'Ocultar ▴'; }
            else { det.hidden = true; btn.textContent = 'Detalle ▾'; }
        });
    });
    buscar.addEventListener('keydown', function (e) { if (e.key === 'Enter') e.preventDefault(); });

    // plantilla pedida al llegar (por ejemplo, al crear un rol desde «Nuevo usuario»)
    if (datos.inicial && !hayAlgo()) { sel.value = datos.inicial; sel.dispatchEvent(new Event('change')); }
    todo();
})();
