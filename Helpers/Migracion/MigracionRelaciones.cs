using static Kirkenta.Helpers.Migracion.MigracionHistorialHelper;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Une las tablas que se relacionan por su id interno: muchas bases guardan en la venta «cliente_id = 17»
    /// en vez del código del cliente. Se arma el mapa id viejo → código (o nombre) del padre y se agrega a la
    /// tabla hija una columna con ese valor, que es el que Kirkenta usa para encontrarlo.
    /// Solo se aplica si al menos el 90 % de los valores de la hija existe entre los ids del padre.
    /// </summary>
    public static class MigracionRelaciones
    {
        private record Padre(string Destino, string[] Raices, string CampoClave, string CampoNombre, string Nombre);

        private static readonly Padre[] Padres =
        {
            new("Clientes", new[] { "cliente", "customer", "client" }, "Codigo", "Nombre", "clientes"),
            new("Proveedores", new[] { "proveedor", "supplier", "vendor", "provider" }, "Codigo", "Nombre", "proveedores"),
            new("Productos", new[] { "producto", "articulo", "item", "product" }, "SKU", "Nombre", "productos"),
            new("@ventas", new[] { "venta", "factura", "sale", "invoice", "ticket", "documento" }, "Documento", "Documento", "ventas"),
            new("@compras", new[] { "compra", "purchase", "orden", "documento" }, "Documento", "Documento", "compras"),
            new("@cotizaciones", new[] { "cotizacion", "presupuesto", "quote", "proforma", "documento" }, "Documento", "Documento", "cotizaciones"),
        };

        /// <summary>Dónde busca cada historial a su cliente / proveedor / producto (campo por código, campo por nombre).</summary>
        private static IEnumerable<(string padre, bool detalle, string campoCodigo, string campoNombre)> Hijos(PasoMigracion p)
        {
            switch (p.Destino)
            {
                case "@ventas": case "@cxc": case "@cotizaciones":
                    yield return ("Clientes", false, "ClienteCodigo", "Cliente"); break;
                case "@compras": case "@cxp":
                    yield return ("Proveedores", false, "ProveedorCodigo", "Proveedor"); break;
                case "@kardex":
                    yield return ("Productos", false, "ProductoCodigo", "ProductoNombre"); break;
            }
            if (p.DetalleIndice.HasValue)
            {
                yield return ("Productos", true, "ProductoCodigo", "ProductoNombre");
                yield return (p.Destino, true, "Documento", "Documento");
            }
        }

        public static void Resolver(List<TablaOrigen> tablas, List<PasoMigracion> plan)
        {
            foreach (var hijo in plan.Where(p => p.Destino.Length > 0))
            foreach (var (destinoPadre, detalle, campoCodigo, campoNombre) in Hijos(hijo).ToList())
            {
                var pasoPadre = destinoPadre == hijo.Destino ? hijo : plan.FirstOrDefault(p => p.Destino == destinoPadre && p.Mapa.Count > 0);
                if (pasoPadre == null) continue;
                var def = Padres.First(x => x.Destino == destinoPadre);
                var tPadre = tablas[pasoPadre.TablaIndice];
                var tHijo = tablas[detalle ? hijo.DetalleIndice!.Value : hijo.TablaIndice];
                if (ReferenceEquals(tPadre, tHijo)) continue;
                var mapaHijo = detalle ? hijo.MapaDetalle : hijo.Mapa;

                // id interno del padre
                var colId = ColumnaId(tPadre.Headers, def.Raices);
                if (colId == null) continue;
                pasoPadre.Mapa.TryGetValue(def.CampoClave, out var colClave);
                if (colClave == colId) continue;      // el id ya es el código que se migra: se unen solos
                var colValor = colClave ?? pasoPadre.Mapa.GetValueOrDefault(def.CampoNombre);
                if (colValor == null) continue;

                var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in tPadre.Rows)
                    if (f.TryGetValue(colId, out var id) && !string.IsNullOrWhiteSpace(id) && f.TryGetValue(colValor, out var val) && !string.IsNullOrWhiteSpace(val))
                        ids.TryAdd(id.Trim(), val.Trim());
                if (ids.Count == 0) continue;

                // si lo que ya se usa en la hija coincide con la clave del padre, no hay nada que arreglar
                var claves = new HashSet<string>(ids.Values, StringComparer.OrdinalIgnoreCase);
                var actual = mapaHijo.GetValueOrDefault(colClave != null ? campoCodigo : campoNombre);
                if (actual != null && Coincide(tHijo, actual, claves) >= 0.9) continue;

                // columna de la hija que guarda el id del padre
                var candidatas = tHijo.Headers.Where(h => EsFk(Norm(h), def.Raices)).ToList();
                if (actual != null && !candidatas.Contains(actual)) candidatas.Add(actual);
                var mapaCodigos = mapaHijo.GetValueOrDefault(campoCodigo);
                if (mapaCodigos != null && !candidatas.Contains(mapaCodigos)) candidatas.Add(mapaCodigos);
                var fk = candidatas.Select(c => (c, r: Coincide(tHijo, c, ids.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase))))
                                   .Where(x => x.r >= 0.9).OrderByDescending(x => x.r).Select(x => x.c).FirstOrDefault();
                if (fk == null) continue;

                var nueva = $"{fk} → {def.Nombre}";
                if (!tHijo.Headers.Contains(nueva)) tHijo.Headers.Add(nueva);
                int unidas = 0, sinPadre = 0;
                foreach (var f in tHijo.Rows)
                {
                    if (!f.TryGetValue(fk, out var v) || string.IsNullOrWhiteSpace(v)) { f[nueva] = ""; continue; }
                    if (ids.TryGetValue(v.Trim(), out var clave)) { f[nueva] = clave; unidas++; }
                    else { f[nueva] = ""; sinPadre++; }
                }
                var campo = colClave != null ? campoCodigo : campoNombre;
                mapaHijo[campo] = nueva;
                var otro = campo == campoCodigo ? campoNombre : campoCodigo;     // que el id viejo no se confunda con un código
                if (otro != campo && mapaHijo.GetValueOrDefault(otro) == fk) mapaHijo.Remove(otro);
                hijo.Relaciones.Add($"{(detalle ? "Líneas" : "Filas")} unidas a {def.Nombre} por «{fk}»: {unidas:N0}" + (sinPadre > 0 ? $" ({sinPadre:N0} con un id que no existe)" : ""));
            }
        }

        private static string? ColumnaId(List<string> headers, string[] raices)
        {
            var exacto = headers.FirstOrDefault(h => Norm(h) == "id");
            return exacto ?? headers.FirstOrDefault(h => EsFk(Norm(h), raices));
        }

        private static bool EsFk(string n, string[] raices) =>
            raices.Any(r => n == r + "id" || n == "id" + r || n == r + "sid" || n == "id" + r + "s");

        private static double Coincide(TablaOrigen t, string col, HashSet<string> valores)
        {
            int total = 0, si = 0;
            foreach (var f in t.Rows)
            {
                if (!f.TryGetValue(col, out var v) || string.IsNullOrWhiteSpace(v)) continue;
                total++;
                if (valores.Contains(v.Trim())) si++;
            }
            return total == 0 ? 0 : si / (double)total;
        }
    }
}
