using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Facturas
{
    public class RegistrarPagoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegistrarPagoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Factura Factura { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();

        public class InputModel
        {
            public int FacturaId { get; set; }

            [Required(ErrorMessage = "El método de pago es obligatorio")]
            public int MetodoPagoId { get; set; }

            [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
            public decimal Monto { get; set; }

            public string? Referencia { get; set; }
            public string? Notas { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var factura = _context.Facturas.FirstOrDefault(f => f.Id == id);
            if (factura == null)
            {
                TempData["Error"] = "Factura no encontrada";
                return RedirectToPage("/Facturas/Index");
            }

            if (factura.Estado == "Pagada" || factura.Estado == "Anulada")
            {
                TempData["Error"] = "Esta factura no admite más pagos";
                return RedirectToPage("/Facturas/Details", new { id });
            }

            Factura = factura;
            MetodosPago = _context.MetodosPago.Where(m => m.Activo).ToList();

            Input = new InputModel
            {
                FacturaId = factura.Id,
                Monto = factura.Saldo
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var factura = _context.Facturas.FirstOrDefault(f => f.Id == Input.FacturaId);
            if (factura == null)
            {
                TempData["Error"] = "Factura no encontrada";
                return RedirectToPage("/Facturas/Index");
            }

            MetodosPago = _context.MetodosPago.Where(m => m.Activo).ToList();

            if (!ModelState.IsValid)
            {
                Factura = factura;
                return Page();
            }

            if (Input.Monto > factura.Saldo)
            {
                ModelState.AddModelError("Input.Monto", "El monto no puede ser mayor al saldo pendiente");
                Factura = factura;
                return Page();
            }

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);

            // Crear pago
            var pago = new Pago
            {
                FacturaId = factura.Id,
                ClienteId = factura.ClienteId,
                Fecha = DateTime.Now,
                Monto = Input.Monto,
                MetodoPagoId = Input.MetodoPagoId,
                Referencia = Input.Referencia,
                Notas = Input.Notas,
                UsuarioCreoId = currentUser?.Id
            };

            _context.Pagos.Add(pago);

            // Actualizar saldo de la factura
            factura.Saldo -= Input.Monto;
            if (factura.Saldo <= 0)
            {
                factura.Saldo = 0;
                factura.Estado = "Pagada";
            }
            else
            {
                factura.Estado = "PagadaParcial";
            }

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Registrar pago",
                $"Registró pago de L. {pago.Monto:N2} en factura {factura.Numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Pago de L. {pago.Monto:N2} registrado correctamente";
            return RedirectToPage("/Facturas/Details", new { id = factura.Id });
        }
    }
}