using System.Text;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.Migracion;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class MigracionOperacionesTests
    {
        private const string Json = @"{
  ""plan_cuentas"": [
    { ""codigo"": ""1"", ""nombre"": ""ACTIVO"" },
    { ""codigo"": ""1.1"", ""nombre"": ""Caja"" },
    { ""codigo"": ""4"", ""nombre"": ""Ingresos"" }
  ],
  ""formas_pago"": [ { ""nombre"": ""Efectivo"" }, { ""nombre"": ""Tarjeta Visa"" }, { ""nombre"": ""Depósito BAC"" } ],
  ""bancos"": [
    { ""nombre"": ""BAC Cuenta Corriente"", ""banco"": ""BAC"", ""numero_cuenta"": ""123456789"", ""saldo"": 15000 },
    { ""nombre"": ""Caja General"", ""banco"": """", ""numero_cuenta"": """", ""saldo"": 2500 }
  ],
  ""empleados"": [
    { ""codigo"": ""E-1"", ""empleado"": ""Juan Carlos Pérez López"", ""identidad"": ""0801-1990-12345"", ""cargo"": ""Vendedor"", ""salario"": 15000, ""fecha_ingreso"": ""15/01/2020"", ""vacaciones"": 7 },
    { ""codigo"": ""E-2"", ""empleado"": ""María Ruiz"", ""identidad"": ""0801-1992-55555"", ""cargo"": ""Cajera"", ""salario"": 12000, ""fecha_ingreso"": ""01/06/2022"", ""vacaciones"": 0 }
  ],
  ""proveedores"": [ { ""nombre"": ""Distribuidora Sol"", ""rtn"": ""08019999000011"" } ],
  ""productos"": [ { ""codigo"": ""P1"", ""nombre"": ""Clavo"", ""precio"": 5, ""costo"": 2, ""existencia"": 10 } ],
  ""compras"": [
    { ""numero"": ""C-100"", ""fecha"": ""10/03/2023"", ""proveedor"": ""Distribuidora Sol"", ""subtotal"": 200, ""impuestos"": 30, ""total"": 230, ""saldo"": 0 },
    { ""numero"": ""C-101"", ""fecha"": ""12/03/2023"", ""proveedor"": ""Nuevo Proveedor SA"", ""subtotal"": 100, ""impuestos"": 15, ""total"": 115, ""saldo"": 115 }
  ],
  ""detalle_compras"": [ { ""compra"": ""C-100"", ""codigo"": ""P1"", ""cantidad"": 100, ""precio"": 2 } ],
  ""cotizaciones"": [ { ""cotizacion"": ""Q-1"", ""fecha"": ""01/10/2026"", ""cliente"": ""Ana"", ""subtotal"": 50, ""impuestos"": 7.5, ""total"": 57.5, ""valida_hasta"": ""31/12/2099"" } ],
  ""detalle_cotizaciones"": [ { ""cotizacion"": ""Q-1"", ""codigo"": ""P1"", ""cantidad"": 10, ""precio"": 5 } ],
  ""gastos"": [ { ""fecha"": ""05/01/2024"", ""concepto"": ""Pago de luz"", ""monto"": 1200, ""tipo"": ""egreso"", ""categoria"": ""Servicios"" } ]
}";

        private static List<TablaOrigen> Leer() =>
            MigracionLector.Leer("sistema_viejo.json", new MemoryStream(Encoding.UTF8.GetBytes(Json))).Tablas;

        [Fact]
        public void Plan_ReconoceLaOperacionCompleta()
        {
            var tablas = Leer();
            var plan = MigracionAutomaticaHelper.Planificar(tablas);
            PasoMigracion Paso(string nombre) => plan.Single(p => p.TablaNombre == nombre);

            Assert.Equal("PlanCuentas", Paso("plan_cuentas").Destino);
            Assert.Equal("MetodosPago", Paso("formas_pago").Destino);
            Assert.Equal("CuentasFinancieras", Paso("bancos").Destino);
            Assert.Equal("Empleados", Paso("empleados").Destino);
            Assert.Equal("@compras", Paso("compras").Destino);
            Assert.Equal("@cotizaciones", Paso("cotizaciones").Destino);
            Assert.Equal("@movimientos", Paso("gastos").Destino);
            Assert.All(new[] { "plan_cuentas", "formas_pago", "bancos", "empleados", "compras", "cotizaciones", "gastos" },
                n => Assert.True(Paso(n).Incluir, n + ": " + Paso(n).Motivo));

            // cada documento se une con SUS líneas
            Assert.Equal(tablas.FindIndex(t => t.Nombre == "detalle_compras"), Paso("compras").DetalleIndice);
            Assert.Equal(tablas.FindIndex(t => t.Nombre == "detalle_cotizaciones"), Paso("cotizaciones").DetalleIndice);
            Assert.False(Paso("detalle_compras").Incluir);

            // cuentas antes que los movimientos; productos y proveedores antes que las compras
            var orden = plan.Where(p => p.Incluir).Select(p => p.Destino).ToList();
            Assert.True(orden.IndexOf("CuentasFinancieras") < orden.IndexOf("@movimientos"));
            Assert.True(orden.IndexOf("Productos") < orden.IndexOf("@compras"));
        }

        [Fact]
        public void Migrar_OperacionCompleta()
        {
            var ctx = TestDb.Crear();
            var admin = TestDb.AgregarUsuario(ctx, "adm", "Admin");
            var tablas = Leer();
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), admin.Id, true);
            Assert.True(res.Ok, res.ErrorFatal);

            // plan de cuentas con jerarquía
            var caja = ctx.PlanCuentas.Single(p => p.Codigo == "1.1");
            Assert.Equal("1", caja.CodigoPadre);
            Assert.Equal(2, caja.Nivel);
            Assert.True(caja.EsMovimiento);
            Assert.False(ctx.PlanCuentas.Single(p => p.Codigo == "1").EsMovimiento);
            Assert.Equal("Ingreso", ctx.PlanCuentas.Single(p => p.Codigo == "4").Tipo);
            Assert.Equal("Acreedora", ctx.PlanCuentas.Single(p => p.Codigo == "4").Naturaleza);

            // métodos de pago con su tipo
            Assert.Equal("Tarjeta", ctx.MetodosPago.Single(m => m.Nombre == "Tarjeta Visa").Tipo);
            Assert.Equal("Transferencia", ctx.MetodosPago.Single(m => m.Nombre == "Depósito BAC").Tipo);

            // bancos y cajas con su saldo real de arranque (y su apertura)
            var bac = ctx.CuentasFinancieras.Single(c => c.Nombre == "BAC Cuenta Corriente");
            Assert.Equal("Banco", bac.Tipo);
            Assert.Equal(15000m, bac.SaldoActual);
            Assert.Equal("Caja", ctx.CuentasFinancieras.Single(c => c.Nombre == "Caja General").Tipo);
            Assert.Equal(2, ctx.MovimientosFinancieros.Count(m => m.Origen == "Apertura"));

            // empleados: nombre partido, salario, vacaciones del sistema anterior sin volver a acumular
            var juan = ctx.Empleados.Single(e => e.Codigo == "E-1");
            Assert.Equal("Juan Carlos", juan.Nombres);
            Assert.Equal("Pérez López", juan.Apellidos);
            Assert.Equal("Vendedor", juan.PuestoNombre);
            Assert.Equal(15000m, juan.SalarioBase);
            Assert.Equal(new DateTime(2020, 1, 15), juan.FechaIngreso);
            Assert.Equal(7m, juan.DiasVacacionesDisponibles);
            Assert.Equal(Kirkenta.Helpers.RRHH.EmpleadoHelper.CalcularAniosAntiguedad(juan), juan.UltimoAnioVacacionesProcesado);

            // compras con líneas; el proveedor que no existía se crea; no tocan stock ni cuentas
            var c100 = ctx.OrdenesCompra.Single(o => o.Numero == "H-CMP-C-100");
            Assert.Equal(230m, c100.Total);
            var linea = ctx.DetalleOrdenesCompra.Single(d => d.OrdenCompraId == c100.Id);
            Assert.Equal(100m, linea.Cantidad);
            Assert.Equal(15m, linea.ImpuestoPorcentaje);
            Assert.Equal(115m, ctx.OrdenesCompra.Single(o => o.Numero == "H-CMP-C-101").Saldo);
            Assert.Contains(ctx.Proveedores, p => p.Nombre == "Nuevo Proveedor SA");
            Assert.Equal(10m, ctx.Productos.Single(p => p.SKU == "P1").Stock);
            // y no se cuentan en la declaración de ISV (ya se declararon en el sistema anterior)
            Assert.Empty(DeclaracionISVHelper.Compras(ctx, new DateTime(2023, 3, 1), new DateTime(2023, 4, 1)));

            // cotización vigente con su cliente y líneas
            var q = ctx.Cotizaciones.Single();
            Assert.Equal("Enviada", q.Estado);
            Assert.Equal("Ana", ctx.Clientes.Single(c => c.Id == q.ClienteId).Nombre);
            Assert.Single(ctx.DetalleCotizaciones);

            // gasto histórico: en los informes, pero sin mover el saldo de la caja
            var gasto = ctx.MovimientosFinancieros.Single(m => m.Concepto == "Pago de luz");
            Assert.Equal("Egreso", gasto.Tipo);
            Assert.Equal(1200m, gasto.Monto);
            Assert.Contains(SaldoHelper.MarcaSoloContable, gasto.Notas);
            Assert.Equal(2500m, ctx.CuentasFinancieras.Single(c => c.Nombre == "Caja General").SaldoActual);
        }

        [Fact]
        public void Conciliacion_TodoCuadraEIntegridadLimpia()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer();
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true);
            Assert.True(res.Ok, res.ErrorFatal);

            Assert.All(res.Conciliacion, c => Assert.True(c.Cuadra, $"{c.Titulo}: {c.Explicacion}"));
            LineaConciliacion Paso(string tabla) => res.Conciliacion.Single(c => c.Tabla == tabla);
            Assert.Equal(17500m, Paso("bancos").MontoOrigen);
            Assert.Equal(17500m, Paso("bancos").MontoMigrado);
            Assert.Equal(345m, Paso("compras").MontoMigrado);
            Assert.Equal(27000m, Paso("empleados").MontoMigrado);

            Assert.True(res.RevisionesIntegridad > 0);
            Assert.True(res.Integridad.Count == 0, string.Join(" | ", res.Integridad.Select(h => $"{h.Titulo}: {string.Join(", ", h.Ejemplos)}")));
        }

        [Fact]
        public void Conciliacion_ExplicaLasFilasDescartadas()
        {
            var ctx = TestDb.Crear();
            var json = @"{ ""bancos"": [ { ""nombre"": ""BAC"", ""banco"": ""BAC"", ""numero_cuenta"": ""1234"", ""saldo"": 100 },
                                          { ""nombre"": """", ""banco"": ""BAC"", ""numero_cuenta"": ""999"", ""saldo"": 50 } ] }";
            var tablas = MigracionLector.Leer("x.json", new MemoryStream(Encoding.UTF8.GetBytes(json))).Tablas;
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true);

            var c = Assert.Single(res.Conciliacion);
            Assert.Equal(150m, c.MontoOrigen);
            Assert.Equal(100m, c.MontoMigrado);
            Assert.Equal(1, c.Descartados);
            Assert.True(c.Cuadra);                        // la diferencia la explica la fila descartada
            Assert.Contains("descartado", c.Explicacion);
        }

        [Fact]
        public void CajaDelSistemaSinUso_TomaElSaldoDelSistemaAnterior()
        {
            var ctx = TestDb.Crear();
            ctx.CuentasFinancieras.Add(new CuentaFinanciera { Codigo = "CAJA-GRAL", Nombre = "Caja General", Tipo = "Caja", Activa = true });
            ctx.SaveChanges();
            var tablas = Leer();
            Assert.True(MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true).Ok);

            var caja = Assert.Single(ctx.CuentasFinancieras.Where(c => c.Nombre == "Caja General"));
            Assert.Equal("CAJA-GRAL", caja.Codigo);
            Assert.Equal(2500m, caja.SaldoActual);
        }

        [Fact]
        public void Migrar_DosVeces_NoDuplica()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer();
            Assert.True(MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true).Ok);
            Assert.True(MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true).Ok);

            Assert.Equal(2, ctx.Empleados.Count());
            Assert.Equal(2, ctx.CuentasFinancieras.Count());
            Assert.Equal(3, ctx.MetodosPago.Count());
            Assert.Equal(3, ctx.PlanCuentas.Count());
            Assert.Equal(2, ctx.OrdenesCompra.Count());
            Assert.Single(ctx.Cotizaciones);
        }

        [Fact]
        public void DeshacerLote_BorraLoMigradoQueNoSeUso()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer();
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true);
            Assert.True(res.Ok, res.ErrorFatal);

            // como en la pantalla: se deshace del más reciente al más antiguo
            foreach (var lote in res.Pasos.Select(p => p.Res?.Lote).Where(l => l != null).Distinct().Reverse())
                MigracionHistorialHelper.DeshacerLote(ctx, lote!);

            Assert.Empty(ctx.Empleados);
            Assert.Empty(ctx.Cotizaciones);
            Assert.Empty(ctx.DetalleCotizaciones);
            Assert.Empty(ctx.OrdenesCompra);
            Assert.Empty(ctx.DetalleOrdenesCompra);
            Assert.Empty(ctx.MovimientosFinancieros);
            Assert.Empty(ctx.CuentasFinancieras);
            Assert.Empty(ctx.RegistrosMigracion);
        }

        [Fact]
        public void NumeracionFiscal_ContinuaDespuesDeLaUltimaFacturaDelSistemaAnterior()
        {
            var ctx = TestDb.Crear();
            ctx.ConfiguracionEmpresa.Add(new ConfiguracionEmpresa
            {
                Nombre = "X", RangoInicial = "000-001-01-00000001", RangoFinal = "000-001-01-00005000", SiguienteNumeroFiscal = 0
            });
            ctx.SaveChanges();

            var aviso = MigracionOperacionesHelper.ContinuarNumeracionFiscal(ctx,
                new[] { "000-001-01-00000149", "000-001-01-00000150", "000-002-01-00009999", "F-77" });

            Assert.NotNull(aviso);
            Assert.Equal(151, ctx.ConfiguracionEmpresa.Single().SiguienteNumeroFiscal);
            // si ya va más adelante, no retrocede
            Assert.Null(MigracionOperacionesHelper.ContinuarNumeracionFiscal(ctx, new[] { "000-001-01-00000010" }));
        }
    }
}
