using System.Text;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.Migracion;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class MigracionContabilidadTests
    {
        private const string Json = @"{
  ""plan_cuentas"": [
    { ""codigo"": ""1"", ""nombre"": ""ACTIVO"" }, { ""codigo"": ""1.1"", ""nombre"": ""Caja"" }, { ""codigo"": ""1.2"", ""nombre"": ""Bancos"" },
    { ""codigo"": ""3"", ""nombre"": ""PATRIMONIO"" }, { ""codigo"": ""3.1"", ""nombre"": ""Capital"" }
  ],
  ""bancos"": [ { ""nombre"": ""Caja General"", ""banco"": """", ""numero_cuenta"": """", ""saldo"": 2500 } ],
  ""balanza"": [
    { ""cuenta"": ""1"", ""nombre"": ""ACTIVO"", ""debe"": 17500, ""haber"": 0 },
    { ""cuenta"": ""1.1"", ""nombre"": ""Caja"", ""debe"": 2500, ""haber"": 0 },
    { ""cuenta"": ""1.2"", ""nombre"": ""Bancos"", ""debe"": 15000, ""haber"": 0 },
    { ""cuenta"": ""3.1"", ""nombre"": ""Capital"", ""debe"": 0, ""haber"": 17500 }
  ],
  ""gastos"": [ { ""fecha"": ""05/01/2024"", ""concepto"": ""Pago de luz"", ""monto"": 1200, ""tipo"": ""egreso"" } ]
}";

        private static List<TablaOrigen> Leer(string json) =>
            MigracionLector.Leer("contabilidad.json", new MemoryStream(Encoding.UTF8.GetBytes(json))).Tablas;

        [Fact]
        public void BalanceDeApertura_IgnoraCuentasMadreYCuadra()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer(Json);
            var plan = MigracionAutomaticaHelper.Planificar(tablas);
            Assert.Equal("@saldoscontables", plan.Single(p => p.TablaNombre == "balanza").Destino);

            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, plan, null, true);
            Assert.True(res.Ok, res.ErrorFatal);

            var asiento = Assert.Single(ctx.AsientosContables);
            var lineas = ctx.DetallesAsiento.Where(d => d.AsientoContableId == asiento.Id).ToList();
            Assert.Equal(3, lineas.Count);                          // la cuenta madre «1 ACTIVO» no se suma dos veces
            Assert.Equal(17500m, lineas.Sum(d => d.Debe));
            Assert.Equal(17500m, lineas.Sum(d => d.Haber));
            Assert.DoesNotContain(ctx.PlanCuentas, p => p.Codigo == MigracionOperacionesHelper.CodigoAjusteApertura);
        }

        [Fact]
        public void CatalogoDistinto_MismoCodigoOtraCuenta_NoSeMezcla()
        {
            var ctx = TestDb.Crear();
            // Catálogo propio de Kirkenta: 1.1 es una cuenta de resumen con otro significado
            ctx.PlanCuentas.AddRange(
                new Kirkenta.Models.PlanCuenta { Codigo = "1", Nombre = "ACTIVO", Tipo = "Activo", Naturaleza = "Deudora", Nivel = 1 },
                new Kirkenta.Models.PlanCuenta { Codigo = "1.1", Nombre = "Activo Corriente", Tipo = "Activo", Naturaleza = "Deudora", Nivel = 2, CodigoPadre = "1" },
                new Kirkenta.Models.PlanCuenta { Codigo = "3.1", Nombre = "Capital", Tipo = "Patrimonio", Naturaleza = "Acreedora", Nivel = 2 },
                new Kirkenta.Models.PlanCuenta { Codigo = "3.1.01", Nombre = "Capital Social", Tipo = "Patrimonio", Naturaleza = "Acreedora", Nivel = 3, CodigoPadre = "3.1", EsMovimiento = true });
            ctx.SaveChanges();

            var tablas = Leer(Json).Where(t => t.Nombre is "plan_cuentas" or "balanza").ToList();
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true);
            Assert.True(res.Ok, res.ErrorFatal);

            // «1.1 Caja» del sistema anterior no se confunde con «1.1 Activo Corriente»
            var caja = ctx.PlanCuentas.Single(p => p.Codigo == "MIG.1.1");
            Assert.Equal("Caja", caja.Nombre);
            Assert.Equal("1", caja.CodigoPadre);                 // «1 ACTIVO» sí es la misma cuenta
            Assert.Equal("Activo Corriente", ctx.PlanCuentas.Single(p => p.Codigo == "1.1").Nombre);

            // el saldo de caja va a MIG.1.1; «3.1 Capital» es resumen en Kirkenta: va a su subcuenta «3.1.01 Capital Social»
            var lineas = ctx.DetallesAsiento.ToList();
            Assert.Equal(2500m, lineas.Single(d => d.PlanCuentaId == caja.Id).Debe);
            Assert.Equal(17500m, lineas.Single(d => d.PlanCuentaId == ctx.PlanCuentas.Single(p => p.Codigo == "3.1.01").Id).Haber);
            Assert.DoesNotContain(lineas, d => d.PlanCuentaId == ctx.PlanCuentas.Single(p => p.Codigo == "1.1").Id);
        }

        [Fact]
        public void BalanceDescuadrado_LaDiferenciaQuedaEnLaCuentaDeAjuste()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer(@"{ ""balanza"": [
                { ""cuenta"": ""1.1"", ""nombre"": ""Caja"", ""debe"": 100, ""haber"": 0 },
                { ""cuenta"": ""3.1"", ""nombre"": ""Capital"", ""debe"": 0, ""haber"": 90 } ] }");
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true);
            Assert.True(res.Ok, res.ErrorFatal);

            var ajuste = ctx.PlanCuentas.Single(p => p.Codigo == MigracionOperacionesHelper.CodigoAjusteApertura);
            var linea = ctx.DetallesAsiento.Single(d => d.PlanCuentaId == ajuste.Id);
            Assert.Equal(10m, linea.Haber);
            Assert.Equal(ctx.DetallesAsiento.Sum(d => d.Debe), ctx.DetallesAsiento.Sum(d => d.Haber));
            Assert.Contains(res.Pasos.Single().Res!.Advertencias, a => a.Contains("no cuadraba"));
        }

        [Fact]
        public void AperturaDeCajaMigradaYGastosHistoricos_NoDuplicanAsientos()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer(Json);
            Assert.True(MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true).Ok);

            var apertura = ctx.MovimientosFinancieros.Single(m => m.Origen == "Apertura");
            Assert.False(AsientoAutomaticoHelper.GeneraAsiento(apertura));
            Assert.False(AsientoAutomaticoHelper.GeneraAsiento(ctx.MovimientosFinancieros.Single(m => m.Concepto == "Pago de luz")));

            AsientoAutomaticoHelper.Sincronizar(ctx);
            // Solo existe el asiento de apertura del balance: ni la caja ni el gasto histórico generan otro
            Assert.Single(ctx.AsientosContables.Where(a => a.Estado == "Activo"));
        }

        [Fact]
        public void DeshacerBalance_DevuelveElAsientoPropioALaApertura()
        {
            var ctx = TestDb.Crear();
            // Primero cajas y plan; el balance llega después, en su propio lote
            var tablas = Leer(Json).Where(t => t.Nombre != "balanza").ToList();
            Assert.True(MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true).Ok);
            Thread.Sleep(1100);
            var balanza = Leer(Json).Where(t => t.Nombre == "balanza").ToList();
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, balanza, MigracionAutomaticaHelper.Planificar(balanza), null, true);
            Assert.True(res.Ok, res.ErrorFatal);
            Assert.False(AsientoAutomaticoHelper.GeneraAsiento(ctx.MovimientosFinancieros.Single(m => m.Origen == "Apertura")));
            var loteBalance = res.Pasos.Single(p => p.Paso.Destino == "@saldoscontables").Res!.Lote!;

            MigracionHistorialHelper.DeshacerLote(ctx, loteBalance);

            Assert.Empty(ctx.AsientosContables);
            Assert.True(AsientoAutomaticoHelper.GeneraAsiento(ctx.MovimientosFinancieros.Single(m => m.Origen == "Apertura")));
        }
    }
}
