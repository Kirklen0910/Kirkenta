using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages
{
    /// <summary>
    /// Busqueda global: un solo cuadro para encontrar clientes, productos, proveedores,
    /// facturas, ventas y pedidos. Cada grupo solo aparece si el usuario tiene permiso de "ver".
    /// </summary>
    public class BuscarModel : PageModel
    {
        private const int MaxPorGrupo = 8;
        private readonly ApplicationDbContext _context;

        public BuscarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        public class Resultado
        {
            public string Titulo { get; set; } = "";
            public string Detalle { get; set; } = "";
            public string Url { get; set; } = "";
        }

        public class Grupo
        {
            public string Nombre { get; set; } = "";
            public string UrlVerTodos { get; set; } = "";
            public List<Resultado> Items { get; set; } = new();
        }

        public List<Grupo> Grupos { get; set; } = new();
        public bool BusquedaMuyCorta { get; set; }

        private bool Puede(string modulo, string sub) =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, modulo, sub, "ver");

        public IActionResult OnGet()
        {
            var t = (Q ?? "").Trim();
            if (t.Length == 0) return Page();
            if (t.Length < 2)
            {
                BusquedaMuyCorta = true;
                return Page();
            }
            var enc = Uri.EscapeDataString(t);

            if (Puede("Ventas", "Clientes"))
            {
                var items = _context.Clientes.AsNoTracking()
                    .Where(c => c.Nombre.Contains(t) || (c.RTN != null && c.RTN.Contains(t))
                             || (c.Telefono != null && c.Telefono.Contains(t)) || (c.Email != null && c.Email.Contains(t)))
                    .OrderBy(c => c.Nombre).Take(MaxPorGrupo).ToList()
                    .Select(c => new Resultado
                    {
                        Titulo = c.Nombre,
                        Detalle = string.Join(" · ", new[] { c.RTN, c.Telefono }.Where(x => !string.IsNullOrWhiteSpace(x))),
                        Url = $"/Clientes?Q={Uri.EscapeDataString(c.Nombre)}"
                    }).ToList();
                if (items.Count > 0) Grupos.Add(new Grupo { Nombre = "Clientes", UrlVerTodos = $"/Clientes?Q={enc}", Items = items });
            }

            if (Puede("Inventario", "Productos"))
            {
                var items = _context.Productos.AsNoTracking()
                    .Where(p => p.Nombre.Contains(t) || (p.SKU != null && p.SKU.Contains(t))
                             || (p.CodigoBarras != null && p.CodigoBarras.Contains(t)))
                    .OrderBy(p => p.Nombre).Take(MaxPorGrupo).ToList()
                    .Select(p => new Resultado
                    {
                        Titulo = p.Nombre,
                        Detalle = $"{(string.IsNullOrWhiteSpace(p.SKU) ? "sin SKU" : p.SKU)} · stock {p.Stock:0.##} · L. {p.PrecioVenta:N2}",
                        Url = $"/Productos?Q={Uri.EscapeDataString(p.Nombre)}"
                    }).ToList();
                if (items.Count > 0) Grupos.Add(new Grupo { Nombre = "Productos", UrlVerTodos = $"/Productos?Q={enc}", Items = items });
            }

            if (Puede("Compras", "Proveedores"))
            {
                var items = _context.Proveedores.AsNoTracking()
                    .Where(p => p.Nombre.Contains(t) || (p.RTN != null && p.RTN.Contains(t))
                             || (p.Telefono != null && p.Telefono.Contains(t)))
                    .OrderBy(p => p.Nombre).Take(MaxPorGrupo).ToList()
                    .Select(p => new Resultado
                    {
                        Titulo = p.Nombre,
                        Detalle = string.Join(" · ", new[] { p.RTN, p.Telefono }.Where(x => !string.IsNullOrWhiteSpace(x))),
                        Url = $"/Proveedores?Q={Uri.EscapeDataString(p.Nombre)}"
                    }).ToList();
                if (items.Count > 0) Grupos.Add(new Grupo { Nombre = "Proveedores", UrlVerTodos = $"/Proveedores?Q={enc}", Items = items });
            }

            if (Puede("Ventas", "Facturas"))
            {
                var idsCliente = _context.Clientes.Where(c => c.Nombre.Contains(t)).Select(c => c.Id);
                var lista = _context.Facturas.AsNoTracking()
                    .Where(f => f.Numero.Contains(t) || idsCliente.Contains(f.ClienteId))
                    .OrderByDescending(f => f.Fecha).Take(MaxPorGrupo).ToList();
                var ids = lista.Select(f => f.ClienteId).Distinct().ToList();
                var nombres = _context.Clientes.AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);
                var items = lista.Select(f => new Resultado
                {
                    Titulo = f.Numero,
                    Detalle = $"{nombres.GetValueOrDefault(f.ClienteId, "—")} · {f.Fecha:dd/MM/yyyy} · L. {f.Total:N2} · {f.Estado}",
                    Url = $"/Facturas/Details?id={f.Id}"
                }).ToList();
                if (items.Count > 0) Grupos.Add(new Grupo { Nombre = "Facturas", UrlVerTodos = $"/Facturas?Q={enc}", Items = items });
            }

            if (Puede("Ventas", "Index"))
            {
                var idsCliente = _context.Clientes.Where(c => c.Nombre.Contains(t)).Select(c => (int?)c.Id);
                var lista = _context.Ventas.AsNoTracking()
                    .Where(v => v.Numero.Contains(t) || (v.ClienteId != null && idsCliente.Contains(v.ClienteId)))
                    .OrderByDescending(v => v.Fecha).Take(MaxPorGrupo).ToList();
                var ids = lista.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
                var nombres = _context.Clientes.AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);
                var items = lista.Select(v => new Resultado
                {
                    Titulo = v.Numero,
                    Detalle = $"{(v.ClienteId.HasValue ? nombres.GetValueOrDefault(v.ClienteId.Value, "—") : "Consumidor final")} · {v.Fecha:dd/MM/yyyy} · L. {v.Total:N2}",
                    Url = $"/Ventas/Details?id={v.Id}"
                }).ToList();
                if (items.Count > 0) Grupos.Add(new Grupo { Nombre = "Ventas", UrlVerTodos = $"/Ventas?Q={enc}", Items = items });
            }

            if (Puede("Ventas", "Pedidos"))
            {
                var idsCliente = _context.Clientes.Where(c => c.Nombre.Contains(t)).Select(c => c.Id);
                var lista = _context.Pedidos.AsNoTracking()
                    .Where(p => p.Numero.Contains(t) || idsCliente.Contains(p.ClienteId))
                    .OrderByDescending(p => p.Fecha).Take(MaxPorGrupo).ToList();
                var ids = lista.Select(p => p.ClienteId).Distinct().ToList();
                var nombres = _context.Clientes.AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);
                var items = lista.Select(p => new Resultado
                {
                    Titulo = p.Numero,
                    Detalle = $"{nombres.GetValueOrDefault(p.ClienteId, "—")} · {p.Fecha:dd/MM/yyyy} · L. {p.Total:N2} · {p.Estado}",
                    Url = $"/Pedidos/Details?id={p.Id}"
                }).ToList();
                if (items.Count > 0) Grupos.Add(new Grupo { Nombre = "Pedidos", UrlVerTodos = $"/Pedidos?Q={enc}", Items = items });
            }

            return Page();
        }
    }
}
