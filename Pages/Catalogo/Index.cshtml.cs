using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace Kirkenta.Pages.Catalogo
{
    /// <summary>
    /// Catálogo en línea PÚBLICO (sin iniciar sesión). El cliente arma su carrito y el pedido se envía por
    /// WhatsApp al negocio; además queda guardado en "Pedidos web". Los precios SIEMPRE se calculan en el
    /// servidor. Solo existe si el administrador lo habilita en Configuración → Catálogo en línea.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context) { _context = context; }

        public bool Disponible { get; set; }
        public string Negocio { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public string? Logo { get; set; }
        public bool MostrarStock { get; set; }
        public decimal PedidoMinimo { get; set; }
        public string ProductosJson { get; set; } = "[]";
        public string CategoriasJson { get; set; } = "[]";

        public class PedidoRequest
        {
            public string? Nombre { get; set; }
            public string? Telefono { get; set; }
            public string? Direccion { get; set; }
            public string? Notas { get; set; }
            public string? Web { get; set; }   // campo trampa para bots: debe llegar vacío
            public List<LineaRequest> Items { get; set; } = new();
        }

        public class LineaRequest
        {
            public int Id { get; set; }
            public decimal Cantidad { get; set; }
        }

        private CatalogoWebConfig? Config() => _context.CatalogoWebConfig.AsNoTracking().FirstOrDefault();

        public IActionResult OnGet()
        {
            var cfg = Config();
            var emp = _context.ConfiguracionEmpresa.AsNoTracking().FirstOrDefault();
            Negocio = emp?.Nombre ?? "Kirkenta";
            Disponible = cfg is { Habilitado: true };
            if (!Disponible) return Page();

            Titulo = string.IsNullOrWhiteSpace(cfg!.Titulo) ? Negocio : cfg.Titulo!;
            Mensaje = cfg.Mensaje ?? "";
            MostrarStock = cfg.MostrarStock;
            PedidoMinimo = cfg.PedidoMinimo;
            Logo = string.IsNullOrWhiteSpace(emp?.LogoPath) ? null : "/" + emp!.LogoPath!.TrimStart('/', '~');

            var impuestos = _context.Impuestos.AsNoTracking().Where(i => i.Activo).ToList();
            var tasas = impuestos.ToDictionary(i => i.Id, i => i.Porcentaje);
            var tasaDefault = impuestos.FirstOrDefault(i => i.EsPredeterminado)?.Porcentaje ?? 0m;
            var categorias = _context.Categorias.AsNoTracking().Where(c => c.Activa).OrderBy(c => c.Nombre).ToList();
            var idsCat = categorias.Select(c => c.Id).ToHashSet();

            var productos = _context.Productos.AsNoTracking()
                .Where(p => p.Activo && p.Stock > 0 && p.PrecioVenta > 0)
                .OrderBy(p => p.Nombre).Take(600).ToList();

            ProductosJson = JsonSerializer.Serialize(productos.Select(p => new
            {
                id = p.Id,
                n = p.Nombre,
                d = p.Descripcion ?? "",
                p = CatalogoWebHelper.PrecioConImpuesto(p.PrecioVenta,
                        p.ImpuestoId.HasValue && tasas.TryGetValue(p.ImpuestoId.Value, out var t) ? t : tasaDefault),
                c = p.CategoriaId.HasValue && idsCat.Contains(p.CategoriaId.Value) ? p.CategoriaId : null,
                s = MostrarStock ? (decimal?)p.Stock : null,
                max = p.Stock,
                u = p.UnidadMedida,
                i = string.IsNullOrWhiteSpace(p.Imagen) ? null : "/" + p.Imagen!.TrimStart('/', '~')
            }));
            var usadas = productos.Where(p => p.CategoriaId.HasValue).Select(p => p.CategoriaId!.Value).ToHashSet();
            CategoriasJson = JsonSerializer.Serialize(categorias.Where(c => usadas.Contains(c.Id)).Select(c => new { id = c.Id, n = c.Nombre }));
            return Page();
        }

        public async Task<IActionResult> OnPostPedidoAsync([FromBody] PedidoRequest req)
        {
            var cfg = Config();
            if (cfg is not { Habilitado: true })
                return new JsonResult(new { ok = false, error = "El catálogo no está disponible en este momento." });
            if (req == null) return new JsonResult(new { ok = false, error = "Pedido no válido." });

            // Bots: si llenaron el campo oculto se responde "ok" sin guardar nada
            if (!string.IsNullOrWhiteSpace(req.Web))
                return new JsonResult(new { ok = true, numero = "PW-0000", whatsapp = (string?)null });

            var errContacto = CatalogoWebHelper.ValidarContacto(req.Nombre, req.Telefono);
            if (errContacto != null) return new JsonResult(new { ok = false, error = errContacto });

            var lineasReq = (req.Items ?? new()).Where(i => i.Cantidad > 0).ToList();
            if (lineasReq.Count == 0) return new JsonResult(new { ok = false, error = "Tu carrito está vacío." });
            if (lineasReq.Count > CatalogoWebHelper.MaxLineas)
                return new JsonResult(new { ok = false, error = "El carrito tiene demasiados productos distintos." });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var desde = DateTime.Now - CatalogoWebHelper.Ventana;
            var recientes = await _context.PedidosWeb.CountAsync(p => p.Ip == ip && p.Fecha >= desde);
            if (recientes >= CatalogoWebHelper.MaxPedidosPorVentana)
                return new JsonResult(new { ok = false, error = "Enviaste varios pedidos seguidos. Espera unos minutos e inténtalo de nuevo." });

            // Precios y existencias: siempre desde la base de datos
            var ids = lineasReq.Select(l => l.Id).Distinct().ToList();
            var prods = await _context.Productos.AsNoTracking().Where(p => ids.Contains(p.Id) && p.Activo).ToDictionaryAsync(p => p.Id);
            var impuestos = await _context.Impuestos.AsNoTracking().Where(i => i.Activo).ToListAsync();
            var tasas = impuestos.ToDictionary(i => i.Id, i => i.Porcentaje);
            var tasaDefault = impuestos.FirstOrDefault(i => i.EsPredeterminado)?.Porcentaje ?? 0m;

            var calculadas = new List<CatalogoWebHelper.LineaCalculada>();
            foreach (var g in lineasReq.GroupBy(l => l.Id))
            {
                if (!prods.TryGetValue(g.Key, out var p) || p.PrecioVenta <= 0)
                    return new JsonResult(new { ok = false, error = "Uno de los productos ya no está disponible. Actualiza la página." });
                var cant = Math.Min(g.Sum(x => x.Cantidad), CatalogoWebHelper.MaxCantidadPorLinea);
                cant = Math.Round(cant, 2);
                if (cant > p.Stock)
                    return new JsonResult(new { ok = false, error = $"De «{p.Nombre}» solo quedan {p.Stock.ToString("0.##", CultureInfo.InvariantCulture)}." });
                var tasa = p.ImpuestoId.HasValue && tasas.TryGetValue(p.ImpuestoId.Value, out var t) ? t : tasaDefault;
                calculadas.Add(new CatalogoWebHelper.LineaCalculada
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Cantidad = cant,
                    Precio = CatalogoWebHelper.PrecioConImpuesto(p.PrecioVenta, tasa)
                });
            }

            var total = calculadas.Sum(l => l.Subtotal);
            if (cfg.PedidoMinimo > 0 && total < cfg.PedidoMinimo)
                return new JsonResult(new { ok = false, error = $"El pedido mínimo es L. {cfg.PedidoMinimo.ToString("N2", CultureInfo.InvariantCulture)}." });

            var nombre = CatalogoWebHelper.Limpiar(req.Nombre, 120);
            var telefono = CatalogoWebHelper.Limpiar(req.Telefono, 30);
            var direccion = CatalogoWebHelper.Limpiar(req.Direccion, 300);
            var notas = CatalogoWebHelper.Limpiar(req.Notas, 500);

            var pedido = new PedidoWeb
            {
                Numero = "PW-" + DateTime.Now.ToString("yyMMdd") + "-" + Random.Shared.Next(1000, 9999),
                Nombre = nombre,
                Telefono = telefono,
                Direccion = direccion.Length == 0 ? null : direccion,
                Notas = notas.Length == 0 ? null : notas,
                ItemsJson = JsonSerializer.Serialize(calculadas.Select(l => new { id = l.Id, nombre = l.Nombre, cantidad = l.Cantidad, precio = l.Precio })),
                Total = total,
                Estado = "Nuevo",
                Ip = ip.Length > 64 ? ip.Substring(0, 64) : ip
            };
            _context.PedidosWeb.Add(pedido);
            await _context.SaveChangesAsync();

            var emp = await _context.ConfiguracionEmpresa.AsNoTracking().FirstOrDefaultAsync();
            var negocio = emp?.Nombre ?? "Kirkenta";
            var destino = string.IsNullOrWhiteSpace(cfg.WhatsApp) ? emp?.Telefono : cfg.WhatsApp;
            var mensaje = CatalogoWebHelper.MensajeWhatsApp(negocio, pedido.Numero, calculadas, total, nombre, telefono, pedido.Direccion, pedido.Notas);
            var enlace = WhatsAppHelper.Enlace(destino, mensaje);

            return new JsonResult(new { ok = true, numero = pedido.Numero, total, whatsapp = enlace });
        }
    }
}
