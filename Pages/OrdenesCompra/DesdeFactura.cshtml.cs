using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Kirkenta.Services.Asistente;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kirkenta.Pages.OrdenesCompra
{
    /// <summary>
    /// Foto de la factura del proveedor → la IA propone proveedor y productos → la persona revisa y corrige →
    /// se crea una orden de compra en Borrador (se recibe después con el flujo normal). Nada se guarda sin revisión.
    /// </summary>
    public class DesdeFacturaModel : PageModel
    {
        private const long MaxBytes = 10 * 1024 * 1024;

        private readonly ApplicationDbContext _db;
        private readonly AsistenteIA _ia;

        public DesdeFacturaModel(ApplicationDbContext db, AsistenteIA ia) { _db = db; _ia = ia; }

        public bool IaActiva { get; set; }
        public bool EsAdmin { get; set; }
        public string ProveedoresJson { get; set; } = "[]";
        public string ProductosJson { get; set; } = "[]";

        public class CrearRequest
        {
            public int ProveedorId { get; set; }
            public string? NumeroFactura { get; set; }
            public DateTime? FechaFactura { get; set; }
            public List<LineaCrear> Lineas { get; set; } = new();
        }

        public class LineaCrear
        {
            public int ProductoId { get; set; }
            public decimal Cantidad { get; set; }
            public decimal Precio { get; set; }
            public decimal Impuesto { get; set; }
        }

        private bool Puede() => PermisoHelper.UsuarioTienePermiso(_db, User.Identity?.Name, "Compras", "OrdenesCreate", "crear");

        public IActionResult OnGet()
        {
            if (!Puede()) { TempData["Error"] = "No tienes permiso para crear órdenes de compra"; return RedirectToPage("/OrdenesCompra/Index"); }
            IaActiva = _ia.Activa;
            var nombre = User.Identity?.Name;
            EsAdmin = _db.Usuarios.Any(u => u.Username == nombre && u.Rol == "Admin");

            ProveedoresJson = JsonSerializer.Serialize(_db.Proveedores.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Nombre)
                .Select(p => new { id = p.Id, nombre = p.Nombre, rtn = p.RTN ?? "" }).ToList());
            ProductosJson = JsonSerializer.Serialize(ProductosLista().Select(p => new
            {
                id = p.Id, nombre = p.Nombre, sku = p.SKU ?? "", precio = p.PrecioCompra, imp = TasaDe(p)
            }));
            return Page();
        }

        private Dictionary<int, decimal>? _tasas;
        private decimal _tasaDefault;
        private decimal TasaDe(Producto p)
        {
            if (_tasas == null)
            {
                var imp = _db.Impuestos.AsNoTracking().Where(i => i.Activo).ToList();
                _tasas = imp.ToDictionary(i => i.Id, i => i.Porcentaje);
                _tasaDefault = imp.FirstOrDefault(i => i.EsPredeterminado)?.Porcentaje ?? 0m;
            }
            return p.ImpuestoId.HasValue && _tasas.TryGetValue(p.ImpuestoId.Value, out var t) ? t : _tasaDefault;
        }

        private List<Producto> ProductosLista() => _db.Productos.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();

        // ---------- 1) Leer la foto ----------
        public async Task<IActionResult> OnPostLeerAsync(IFormFile? imagen)
        {
            if (!Puede()) return new JsonResult(new { ok = false, error = "No tienes permiso para crear órdenes de compra." });
            if (imagen == null || imagen.Length == 0) return new JsonResult(new { ok = false, error = "Selecciona o toma una foto de la factura." });
            if (imagen.Length > MaxBytes) return new JsonResult(new { ok = false, error = "La imagen pesa más de 10 MB." });

            byte[] bytes;
            using (var ms = new MemoryStream()) { await imagen.CopyToAsync(ms); bytes = ms.ToArray(); }
            var mime = Mime(bytes);
            if (mime == null) return new JsonResult(new { ok = false, error = "Formato no válido. Usa una foto JPG, PNG o WebP." });

            var f = await _ia.LeerFacturaAsync(bytes, mime);
            if (f.Error != null) return new JsonResult(new { ok = false, error = f.Error });

            var proveedores = _db.Proveedores.AsNoTracking().Where(p => p.Activo).ToList();
            var prod = ProductosLista();

            // Proveedor: RTN exacto o nombre parecido
            Proveedor? prov = null;
            var rtn = new string((f.Rtn ?? "").Where(char.IsDigit).ToArray());
            if (rtn.Length >= 8) prov = proveedores.FirstOrDefault(p => new string((p.RTN ?? "").Where(char.IsDigit).ToArray()) == rtn);
            prov ??= FacturaProveedorHelper.MejorCoincidencia(proveedores, null, f.Proveedor, p => p.Nombre, _ => null, _ => null, 0.6, out _);

            var lineas = f.Lineas.Select(l =>
            {
                var p = FacturaProveedorHelper.MejorCoincidencia(prod, l.Codigo, l.Descripcion, x => x.Nombre, x => x.SKU, x => x.CodigoBarras, 0.5, out var puntaje);
                return new
                {
                    codigo = l.Codigo ?? "",
                    descripcion = l.Descripcion,
                    cantidad = l.Cantidad,
                    precio = l.Precio,
                    total = l.Total,
                    productoId = p?.Id,
                    impuesto = p != null ? TasaDe(p) : 0m,
                    puntaje = Math.Round(puntaje, 2)
                };
            }).ToList();

            var suma = lineas.Sum(l => Math.Round(l.cantidad * l.precio, 2));
            return new JsonResult(new
            {
                ok = true,
                proveedorLeido = f.Proveedor ?? "",
                rtn = f.Rtn ?? "",
                proveedorId = prov?.Id,
                numero = f.Numero ?? "",
                fecha = f.Fecha?.ToString("yyyy-MM-dd"),
                subtotal = f.Subtotal,
                isv = f.Isv,
                total = f.Total,
                sumaLineas = suma,
                difiere = FacturaProveedorHelper.TotalesDifieren(suma, f.Total, f.Isv),
                lineas
            });
        }

        private static string? Mime(byte[] b)
        {
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
            if (b.Length > 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "image/webp";
            return null;
        }

        // ---------- 2) Crear la orden (ya revisada por la persona) ----------
        public IActionResult OnPostCrear([FromBody] CrearRequest req)
        {
            if (!Puede()) return new JsonResult(new { ok = false, error = "No tienes permiso para crear órdenes de compra." });
            if (req == null) return new JsonResult(new { ok = false, error = "Solicitud no válida." });

            var proveedor = _db.Proveedores.FirstOrDefault(p => p.Id == req.ProveedorId && p.Activo);
            if (proveedor == null) return new JsonResult(new { ok = false, error = "Elige el proveedor." });

            var lineas = (req.Lineas ?? new()).Where(l => l.ProductoId > 0).ToList();
            if (lineas.Count == 0) return new JsonResult(new { ok = false, error = "Ninguna línea tiene producto asignado." });
            if (lineas.Any(l => l.Cantidad <= 0 || l.Precio < 0 || l.Impuesto < 0 || l.Impuesto > 100))
                return new JsonResult(new { ok = false, error = "Revisa cantidades, precios e impuestos: hay valores no válidos." });

            var err = LineasDocumentoHelper.Validar(_db, lineas
                .Select(l => new LineasDocumentoHelper.Linea(l.ProductoId, l.Cantidad, l.Precio, 0m, l.Impuesto)).ToList(), soloActivos: false);
            if (err != null) return new JsonResult(new { ok = false, error = err });

            var numFactura = CatalogoWebHelper.Limpiar(req.NumeroFactura, 40);
            var marca = numFactura.Length > 0 ? $"Factura del proveedor N° {numFactura}" : null;
            if (marca != null)
            {
                var repetida = _db.OrdenesCompra.AsNoTracking()
                    .Where(o => o.ProveedorId == proveedor.Id && o.Notas != null && o.Notas.Contains(marca) && o.Estado != "Cancelada" && o.Estado != "Anulada")
                    .Select(o => o.Numero).FirstOrDefault();
                if (repetida != null)
                    return new JsonResult(new { ok = false, error = $"Esa factura ya fue cargada en la orden {repetida}. Si es otra, cambia el número." });
            }

            var usuario = _db.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            using var tx = _db.Database.BeginTransaction();
            var numero = NumeroDocumentoHelper.GenerarSiguiente(_db, "OrdenCompra");
            decimal subtotal = 0, impuestos = 0, total = 0;
            var detalles = new List<DetalleOrdenCompra>();
            foreach (var l in lineas)
            {
                var bruto = Math.Round(l.Cantidad * l.Precio, 2);
                var iva = Math.Round(bruto * (l.Impuesto / 100m), 2);
                subtotal += bruto; impuestos += iva; total += bruto + iva;
                detalles.Add(new DetalleOrdenCompra
                {
                    ProductoId = l.ProductoId, Cantidad = l.Cantidad, CantidadRecibida = 0, PrecioUnitario = l.Precio,
                    Descuento = 0, ImpuestoPorcentaje = l.Impuesto, Subtotal = bruto, Total = bruto + iva
                });
            }

            var notas = (marca ?? "Factura de proveedor") + (req.FechaFactura.HasValue ? $" (fecha {req.FechaFactura.Value:dd/MM/yyyy})" : "") + ". Datos leídos con IA y revisados por el usuario.";
            var orden = new Models.OrdenCompra
            {
                Numero = numero,
                ProveedorId = proveedor.Id,
                Fecha = DateTime.Now,
                Estado = "Borrador",
                Subtotal = subtotal,
                Descuento = 0,
                Impuestos = impuestos,
                Total = total,
                Saldo = total,
                MonedaId = 1,
                TipoCambio = _db.Monedas.Where(m => m.Id == 1).Select(m => m.TipoCambio).FirstOrDefault() is { } tc && tc > 0 ? tc : 1,
                Notas = notas.Length > 500 ? notas.Substring(0, 500) : notas,
                UsuarioCreoId = usuario?.Id,
                EmpresaId = 1
            };
            _db.OrdenesCompra.Add(orden);
            _db.SaveChanges();
            foreach (var d in detalles) { d.OrdenCompraId = orden.Id; _db.DetalleOrdenesCompra.Add(d); }
            _db.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(_db, usuario?.Id ?? 0, "Crear orden de compra desde foto",
                $"Creó la orden {orden.Numero} por L. {orden.Total:N2} a {proveedor.Nombre} desde una foto de factura",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return new JsonResult(new { ok = true, id = orden.Id, numero = orden.Numero, url = Url.Page("/OrdenesCompra/Details", new { id = orden.Id }) });
        }
    }
}
