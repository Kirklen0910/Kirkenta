namespace Kirkenta.Services
{
    /// <summary>
    /// Datos que envía el POS al registrar una venta.
    /// Los nombres de las propiedades coinciden con el JSON que arma Pages/POS/Index.cshtml,
    /// así que no hay que tocar el JavaScript de la vista.
    /// </summary>
    public class VentaRequest
    {
        public int? ClienteId { get; set; }
        public int MetodoPagoId { get; set; }
        public decimal Monto { get; set; }
        public decimal Cambio { get; set; }
        public string? Referencia { get; set; }
        /// <summary>Descuento general en MONTO (formato anterior; el POS nuevo usa el porcentaje).</summary>
        public decimal Descuento { get; set; }
        /// <summary>
        /// Descuento general en porcentaje (0-100) sobre toda la venta. El servidor lo reparte entre las
        /// líneas cuyo producto admite descuento; el envío nunca se descuenta.
        /// </summary>
        public decimal DescuentoGeneralPorcentaje { get; set; }
        public int? CotizacionId { get; set; }
        /// <summary>Identificador generado por el POS cuando la venta se hizo sin internet; evita duplicarla al reintentar.</summary>
        public string? OfflineId { get; set; }
        /// <summary>Pedido del catálogo en línea que se está cobrando; al registrarse la venta pasa a Atendido.</summary>
        public int? PedidoWebId { get; set; }
        public List<ItemVentaRequest> Items { get; set; } = new();

        public bool RequiereEnvio { get; set; } = false;
        public decimal MontoEnvio { get; set; } = 0;
        public string? EnvioDireccion { get; set; }
        public string? EnvioReferencia { get; set; }
        public string? EnvioContactoNombre { get; set; }
        public string? EnvioContactoTelefono { get; set; }
        public string? EnvioCiudad { get; set; }
        public int? EnvioZonaId { get; set; }

        /// <summary>Supervisor que autoriza vender más de lo que hay en existencia (el stock queda negativo).</summary>
        public string? AutorizadorUsuario { get; set; }
        public string? AutorizadorClave { get; set; }

        /// <summary>Parte de la venta que se paga con el saldo a favor del cliente (por devoluciones o cambios).</summary>
        public decimal SaldoFavorAplicar { get; set; }

        /// <summary>Controlar la entrega en bodega por partes (comprobantes parciales).</summary>
        public bool ControlEntrega { get; set; }
    }

    public class ItemVentaRequest
    {
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal ImpuestoPorcentaje { get; set; }
        public decimal Descuento { get; set; }
    }

    /// <summary>
    /// Resultado de registrar una venta. Si Exito es false, Error trae el mensaje para el cajero.
    /// </summary>
    public class ResultadoVenta
    {
        public bool Exito { get; set; }
        public string? Error { get; set; }

        public string? Numero { get; set; }
        public int VentaId { get; set; }
        public decimal Total { get; set; }
        public decimal? MontoRecibido { get; set; }
        public decimal Cambio { get; set; }
        public bool EsCredito { get; set; }
        public bool RequiereEnvio { get; set; }
        public string? EnvioNumero { get; set; }
        public bool MovimientoRegistrado { get; set; }
        public bool CostoRegistrado { get; set; }
        public decimal CostoTotalVendido { get; set; }

        public static ResultadoVenta Falla(string error) => new() { Exito = false, Error = error };
    }
}
