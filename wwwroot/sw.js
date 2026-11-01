/* Kirkenta – service worker (PWA + POS sin internet)
 * - Guarda en caché los archivos estáticos (css, js, imágenes) para que el POS abra sin internet.
 * - Guarda la última versión de /POS y /Movil que se cargó con internet y la usa si no hay conexión.
 * - NUNCA toca las peticiones POST (las ventas offline las encola kirkenta-offline.js en IndexedDB).
 * - Al cerrar sesión borra las páginas guardadas (no quedan datos en equipos compartidos).
 */
const VERSION = 'v2';
const ESTATICO = 'kk-estatico-' + VERSION;
const PAGINAS = 'kk-paginas-' + VERSION;
const PRECACHE = ['/offline.html', '/images/pwa/icon-192.png', '/images/logo.png'];
const PAGINAS_OFFLINE = ['/POS', '/Movil'];
const ESPERA_RED_MS = 8000;

self.addEventListener('install', function (e) {
    e.waitUntil(
        caches.open(ESTATICO)
            .then(function (c) { return Promise.all(PRECACHE.map(function (u) { return c.add(u).catch(function () { }); })); })
            .then(function () { return self.skipWaiting(); })
    );
});

self.addEventListener('activate', function (e) {
    e.waitUntil(
        caches.keys()
            .then(function (claves) {
                return Promise.all(claves.filter(function (k) {
                    return k.indexOf('kk-') === 0 && k !== ESTATICO && k !== PAGINAS;
                }).map(function (k) { return caches.delete(k); }));
            })
            .then(function () { return self.clients.claim(); })
    );
});

function esEstatico(url) {
    return /^\/(css|js|lib|images)\//.test(url.pathname) || /\.(css|js|png|jpg|jpeg|svg|ico|woff2?|webmanifest)$/i.test(url.pathname);
}

self.addEventListener('fetch', function (e) {
    var req = e.request;
    if (req.method !== 'GET') return;
    var url = new URL(req.url);
    if (url.origin !== self.location.origin) return;

    if (/^\/Auth\/Logout/i.test(url.pathname)) {
        e.waitUntil(caches.delete(PAGINAS));
        return;
    }
    if (url.pathname === '/sw.js') return;

    if (req.mode === 'navigate') {
        e.respondWith(navegacion(req, url));
    } else if (esEstatico(url)) {
        e.respondWith(estatico(req));
    }
});

function estatico(req) {
    var url = new URL(req.url);
    var versionado = url.searchParams.has('v');   // asp-append-version: ?v= es el hash del contenido
    return caches.open(ESTATICO).then(function (cache) {
        return cache.match(req).then(function (exacto) {
            // Archivo versionado y ya guardado con ese mismo hash: no cambió, se usa la copia
            if (exacto && versionado) return exacto;

            var red = fetch(req).then(function (r) {
                if (r && r.ok && r.type === 'basic') {
                    var copia = r.clone();
                    // Se reemplazan las versiones anteriores del mismo archivo (no se acumulan en el caché)
                    cache.keys().then(function (claves) {
                        return Promise.all(claves.filter(function (k) {
                            return new URL(k.url).pathname === url.pathname && k.url !== req.url;
                        }).map(function (k) { return cache.delete(k); }));
                    }).then(function () { return cache.put(req, copia); });
                }
                return r;
            }).catch(function () {
                // Sin internet: cualquier versión guardada del archivo sirve para que el POS abra
                return exacto || cache.match(req, { ignoreSearch: true });
            });

            // Sin versión (?v=): se responde con lo guardado y se actualiza en segundo plano
            return (exacto && !versionado) ? exacto : red;
        });
    });
}

function conEspera(req, ms) {
    var ctl = typeof AbortController !== 'undefined' ? new AbortController() : null;
    var t = ctl ? setTimeout(function () { ctl.abort(); }, ms) : null;
    return fetch(req, ctl ? { signal: ctl.signal } : undefined).then(function (r) { if (t) clearTimeout(t); return r; },
        function (err) { if (t) clearTimeout(t); throw err; });
}

function navegacion(req, url) {
    var guardable = PAGINAS_OFFLINE.indexOf(url.pathname) >= 0 && url.search === '';
    var enCache = PAGINAS_OFFLINE.indexOf(url.pathname) >= 0;
    return conEspera(req, enCache ? ESPERA_RED_MS : 60000).then(function (r) {
        if (guardable && r.ok && !r.redirected && r.type === 'basic') {
            var copia = r.clone();
            caches.open(PAGINAS).then(function (c) { c.put(url.pathname, copia); });
        }
        return r;
    }).catch(function () {
        return caches.open(PAGINAS).then(function (c) { return c.match(url.pathname); }).then(function (hit) {
            if (hit) return hit;
            return caches.match('/offline.html').then(function (off) { return off || Response.error(); });
        });
    });
}
