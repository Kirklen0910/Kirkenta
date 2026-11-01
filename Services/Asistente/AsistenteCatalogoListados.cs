using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Services.Asistente
{
    /// <summary>
    /// Listados generales: basta con decir la palabra del tema ("productos", "clientes", "proveedores", "usuarios"...)
    /// para ver la lista; si se agrega un nombre ("productos de limpieza") se filtra.
    /// </summary>
    public static partial class AsistenteCatalogo
    {
        private static IEnumerable<AccionDef> Listados() => new List<AccionDef>
        {
            new() { Id = "productos_lista", Modulo = "Inventario", Titulo = "Lista de productos", Descripcion = "Productos con existencias y precio; di un nombre para filtrar",
                Ejemplo = "productos", PermModulo = "Inventario", PermSub = "Productos",
                Frases = new[] { "producto*", "articulo*", "catalogo", "mercaderia" }, Ejecutar = ProductosLista },
            new() { Id = "categorias_lista", Modulo = "Inventario", Titulo = "Categorías", Descripcion = "Categorías de productos y cuántos productos tiene cada una",
                Ejemplo = "categorías", PermModulo = "Inventario", PermSub = "Categorias",
                Frases = new[] { "categoria*" }, Ejecutar = CategoriasLista },
            new() { Id = "clientes_lista", Modulo = "Ventas", Titulo = "Lista de clientes", Descripcion = "Clientes registrados; di un nombre para filtrar",
                Ejemplo = "clientes", PermModulo = "Ventas", PermSub = "Clientes",
                Frases = new[] { "client*" }, Ejecutar = ClientesLista },
            new() { Id = "facturas_lista", Modulo = "Ventas", Titulo = "Facturas emitidas", Descripcion = "Facturas del período (por defecto este mes)",
                Ejemplo = "facturas del mes", UsaFechas = true, PermModulo = "Ventas", PermSub = "Facturas",
                Frases = new[] { "factura*", "emitida*" }, Ejecutar = FacturasLista },
            new() { Id = "proveedores_lista", Modulo = "Compras", Titulo = "Lista de proveedores", Descripcion = "Proveedores registrados; di un nombre para filtrar",
                Ejemplo = "proveedores", PermModulo = "Compras", PermSub = "Proveedores",
                Frases = new[] { "proveedor*" }, Ejecutar = ProveedoresLista },
            new() { Id = "usuarios_lista", Modulo = "Seguridad", Titulo = "Usuarios del sistema", Descripcion = "Usuarios, su rol y último acceso",
                Ejemplo = "usuarios", PermModulo = "Usuarios", PermSub = "Index",
                Frases = new[] { "usuario*" }, Ejecutar = UsuariosLista },
        };

        private static string Pal(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s!;

        private static RespuestaAsistente ProductosLista(ContextoAccion c)
        {
            var q = c.Db.Productos.AsNoTracking().Where(p => p.Activo);
            if (c.Texto != null)
                foreach (var tk in c.Texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = tk;
                    q = q.Where(p => p.Nombre.Contains(t) || (p.SKU != null && p.SKU.Contains(t)) || (p.CodigoBarras != null && p.CodigoBarras.Contains(t))
                                     || (p.Descripcion != null && p.Descripcion.Contains(t)));
                }
            var total = q.Count();
            var lista = q.OrderBy(p => p.Nombre).Take(Limite).ToList();
            var catIds = lista.Where(p => p.CategoriaId.HasValue).Select(p => p.CategoriaId!.Value).Distinct().ToList();
            var cats = c.Db.Categorias.AsNoTracking().Where(x => catIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);
            var filas = lista.Select(p => Fila(p.Nombre, Pal(p.SKU), p.CategoriaId.HasValue ? cats.GetValueOrDefault(p.CategoriaId.Value, "—") : "—",
                p.Stock.ToString("N2", Inv), p.Stock <= p.StockMinimo ? "⚠️ Bajo" : "OK", L(p.PrecioVenta))).ToList();
            var msg = total == 0
                ? (c.Texto == null ? "No hay productos registrados." : $"No encontré productos con “{c.Texto}”.")
                : $"{total} producto(s){(c.Texto == null ? "" : $" con “{c.Texto}”")}{Nota(total)}. Puedes decir un nombre para filtrar, o “productos con stock bajo”.";
            var r = RespuestaAsistente.Tabla(c.Texto == null ? "Productos" : $"Productos: {c.Texto}", msg,
                new[] { "Producto", "SKU", "Categoría", "Stock", "Nivel", "Precio" }, filas, null, new[] { 3, 5 });
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Abrir Productos", Url = "/Productos" } };
            return r;
        }

        private static RespuestaAsistente CategoriasLista(ContextoAccion c)
        {
            var cats = c.Db.Categorias.AsNoTracking().Where(x => x.Activa).OrderBy(x => x.Nombre).ToList();
            var cuentas = c.Db.Productos.AsNoTracking().Where(p => p.Activo && p.CategoriaId != null)
                .GroupBy(p => p.CategoriaId!.Value).Select(g => new { g.Key, N = g.Count() }).ToList()
                .ToDictionary(x => x.Key, x => x.N);
            var filas = cats.Select(x => Fila(x.Nombre, Pal(x.Descripcion), cuentas.GetValueOrDefault(x.Id, 0).ToString())).ToList();
            return RespuestaAsistente.Tabla("Categorías", cats.Count == 0 ? "No hay categorías." : $"{cats.Count} categoría(s).",
                new[] { "Categoría", "Descripción", "Productos" }, filas, null, new[] { 2 });
        }

        private static RespuestaAsistente ClientesLista(ContextoAccion c)
        {
            var q = c.Texto == null ? c.Db.Clientes.AsNoTracking().AsQueryable() : FiltrarClientes(c, c.Texto);
            q = q.Where(x => x.Activo);
            var total = q.Count();
            var lista = q.OrderBy(x => x.Nombre).Take(Limite).ToList();
            var filas = lista.Select(x => Fila(x.Nombre, Pal(x.RTN), Pal(x.Telefono), Pal(x.Ciudad), x.TipoCliente, L(x.LimiteCredito))).ToList();
            var msg = total == 0
                ? (c.Texto == null ? "No hay clientes registrados." : $"No encontré clientes con “{c.Texto}”.")
                : $"{total} cliente(s){(c.Texto == null ? "" : $" con “{c.Texto}”")}{Nota(total)}. Para ver lo que debe uno, di “saldo de <nombre>”.";
            var r = RespuestaAsistente.Tabla(c.Texto == null ? "Clientes" : $"Clientes: {c.Texto}", msg,
                new[] { "Cliente", "RTN", "Teléfono", "Ciudad", "Tipo", "Límite de crédito" }, filas, null, new[] { 5 });
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Abrir Clientes", Url = "/Clientes" } };
            return r;
        }

        private static RespuestaAsistente FacturasLista(ContextoAccion c)
        {
            DateTime d, h; string rango;
            if (c.P.ContainsKey("desde")) { d = c.Desde.Date; h = c.Hasta.Date.AddDays(1); rango = c.RangoTexto; }
            else { d = new DateTime(c.Hoy.Year, c.Hoy.Month, 1); h = c.Hoy.Date.AddDays(1); rango = "este mes"; }

            var fac = c.Db.Facturas.AsNoTracking().Where(f => f.Fecha >= d && f.Fecha < h).OrderByDescending(f => f.Fecha).ToList();
            var ids = fac.Select(f => f.ClienteId).Distinct().ToList();
            var cli = c.Db.Clientes.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);
            var vigentes = fac.Where(f => f.Estado != "Anulada").ToList();
            var filas = fac.Take(Limite).Select(f => Fila(f.Numero, F(f.Fecha), cli.GetValueOrDefault(f.ClienteId, "—"), f.Estado, L(f.Total), L(f.Saldo))).ToList();
            var r = RespuestaAsistente.Tabla($"Facturas {rango}",
                fac.Count == 0 ? $"No hay facturas {rango}." : $"{fac.Count} factura(s) {rango}; vigentes por {L(vigentes.Sum(f => f.Total))}, saldo por cobrar {L(vigentes.Sum(f => f.Saldo))}{Nota(fac.Count)}.",
                new[] { "Factura", "Fecha", "Cliente", "Estado", "Total", "Saldo" }, filas,
                fac.Count > 0 ? Fila("VIGENTES", "", "", vigentes.Count + " fact.", L(vigentes.Sum(f => f.Total)), L(vigentes.Sum(f => f.Saldo))) : null, new[] { 4, 5 });
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Abrir Facturas", Url = "/Facturas" } };
            return r;
        }

        private static RespuestaAsistente ProveedoresLista(ContextoAccion c)
        {
            var q = c.Db.Proveedores.AsNoTracking().Where(p => p.Activo);
            if (c.Texto != null)
                foreach (var tk in c.Texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = tk;
                    q = q.Where(p => p.Nombre.Contains(t) || (p.RazonSocial != null && p.RazonSocial.Contains(t)) || (p.RTN != null && p.RTN.Contains(t))
                                     || (p.Contacto != null && p.Contacto.Contains(t)));
                }
            var total = q.Count();
            var lista = q.OrderBy(p => p.Nombre).Take(Limite).ToList();
            var filas = lista.Select(p => Fila(p.Nombre, Pal(p.Contacto), Pal(p.Telefono ?? p.TelefonoContacto), Pal(p.Ciudad), p.CondicionPago)).ToList();
            var msg = total == 0
                ? (c.Texto == null ? "No hay proveedores registrados." : $"No encontré proveedores con “{c.Texto}”.")
                : $"{total} proveedor(es){(c.Texto == null ? "" : $" con “{c.Texto}”")}{Nota(total)}. Para ver lo que se les debe, di “cuentas por pagar”.";
            var r = RespuestaAsistente.Tabla(c.Texto == null ? "Proveedores" : $"Proveedores: {c.Texto}", msg,
                new[] { "Proveedor", "Contacto", "Teléfono", "Ciudad", "Condición de pago" }, filas);
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Abrir Proveedores", Url = "/Proveedores" } };
            return r;
        }

        private static RespuestaAsistente UsuariosLista(ContextoAccion c)
        {
            var ahora = DateTime.Now;
            var q = c.Db.Usuarios.AsNoTracking().AsQueryable();
            if (c.Texto != null)
                foreach (var tk in c.Texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = tk;
                    q = q.Where(u => u.Username.Contains(t) || (u.NombreCompleto != null && u.NombreCompleto.Contains(t)) || u.Rol.Contains(t));
                }
            var us = q.OrderBy(u => u.Username).Take(Limite).ToList();
            var filas = us.Select(u => Fila(u.Username, Pal(u.NombreCompleto), u.Rol,
                !u.Activo ? "Desactivado" : (u.BloqueadoHasta.HasValue && u.BloqueadoHasta.Value > ahora ? "Bloqueado" : "Activo"),
                u.UltimoAcceso.HasValue ? u.UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm", Inv) : "Nunca")).ToList();
            return RespuestaAsistente.Tabla("Usuarios", us.Count == 0 ? "No encontré usuarios." : $"{us.Count} usuario(s).",
                new[] { "Usuario", "Nombre", "Rol", "Estado", "Último acceso" }, filas);
        }
    }
}
