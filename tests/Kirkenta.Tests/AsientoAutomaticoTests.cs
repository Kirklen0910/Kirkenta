using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class AsientoAutomaticoTests
    {
        private static (ApplicationDbContext ctx, CuentaFinanciera caja) Escenario()
        {
            var ctx = TestDb.Crear();

            ctx.PlanCuentas.AddRange(
                new PlanCuenta { Codigo = "1.1.01.001", Nombre = "Caja General", Tipo = "Activo" },
                new PlanCuenta { Codigo = "3.1.01", Nombre = "Capital Social", Tipo = "Patrimonio" },
                new PlanCuenta { Codigo = "4.2.02", Nombre = "Otros Ingresos", Tipo = "Ingreso" },
                new PlanCuenta { Codigo = "6.3.02", Nombre = "Gastos Diversos", Tipo = "Gasto" });
            var caja = new CuentaFinanciera { Codigo = "C1", Nombre = "Caja", Tipo = "Caja", SaldoInicial = 0, SaldoActual = 0, Activa = true };
            ctx.CuentasFinancieras.Add(caja);
            ctx.SaveChanges();
            return (ctx, caja);
        }

        private static MovimientoFinanciero Mov(int cuentaId, string tipo, decimal monto, string numero) => new()
        {
            Numero = numero,
            Tipo = tipo,
            CuentaId = cuentaId,
            Monto = monto,
            TipoCambio = 1,
            Estado = "Activo",
            Origen = "Manual",
            Concepto = "Prueba " + numero,
            Fecha = DateTime.Today
        };

        [Fact]
        public void Devolucion_a_saldo_favor_debita_devoluciones_y_acredita_pasivo_del_cliente()
        {
            var (ctx, caja) = Escenario();
            using var _ = ctx;
            ctx.PlanCuentas.AddRange(
                new PlanCuenta { Codigo = "2.1.06", Nombre = "Saldos a favor de clientes", Tipo = "Pasivo" },
                new PlanCuenta { Codigo = "4.1.03", Nombre = "Devoluciones en Ventas", Tipo = "Ingreso" },
                new PlanCuenta { Codigo = "1.1.03.001", Nombre = "Mercadería", Tipo = "Activo" });
            ctx.SaveChanges();
            var devPlan = ctx.PlanCuentas.First(p => p.Codigo == "4.1.03");
            var cat = new CategoriaFinanciera { Tipo = "Egreso", Nombre = "Devoluciones en ventas", PlanCuentaId = devPlan.Id, Activa = true };
            ctx.CategoriasFinancieras.Add(cat);
            ctx.SaveChanges();

            var mov = Mov(caja.Id, "Egreso", 115m, "M-DEV");
            mov.Origen = "Devolucion";
            mov.OrigenId = 999;
            mov.CategoriaId = cat.Id;
            mov.FormaPago = "Saldo a favor";
            mov.Notas = SaldoHelper.MarcaSoloContable;
            ctx.MovimientosFinancieros.Add(mov);
            ctx.SaveChanges();

            AsientoAutomaticoHelper.Sincronizar(ctx);

            var asiento = Assert.Single(ctx.AsientosContables.ToList());
            var lineas = ctx.DetallesAsiento.Where(d => d.AsientoContableId == asiento.Id).ToList();
            var idSaldo = ctx.PlanCuentas.First(p => p.Codigo == "2.1.06").Id;
            Assert.Equal(115m, lineas.Where(l => l.PlanCuentaId == idSaldo).Sum(l => l.Haber));
            Assert.Equal(115m, lineas.Where(l => l.PlanCuentaId == devPlan.Id).Sum(l => l.Debe));
            Assert.Equal(lineas.Sum(l => l.Debe), lineas.Sum(l => l.Haber));
        }

        [Fact]
        public void Crea_un_asiento_balanceado_por_movimiento()
        {
            var (ctx, caja) = Escenario();
            using var _ = ctx;
            ctx.MovimientosFinancieros.Add(Mov(caja.Id, "Ingreso", 250m, "M-1"));
            ctx.SaveChanges();

            var r = AsientoAutomaticoHelper.Sincronizar(ctx);

            Assert.Equal(1, r.Creados);
            var asiento = Assert.Single(ctx.AsientosContables.ToList());
            var lineas = ctx.DetallesAsiento.Where(d => d.AsientoContableId == asiento.Id).ToList();
            Assert.Equal(2, lineas.Count);
            Assert.Equal(250m, lineas.Sum(l => l.Debe));
            Assert.Equal(250m, lineas.Sum(l => l.Haber));
        }

        [Fact]
        public void Es_idempotente()
        {
            var (ctx, caja) = Escenario();
            using var _ = ctx;
            ctx.MovimientosFinancieros.Add(Mov(caja.Id, "Egreso", 40m, "M-2"));
            ctx.SaveChanges();

            AsientoAutomaticoHelper.Sincronizar(ctx);
            var segunda = AsientoAutomaticoHelper.Sincronizar(ctx);

            Assert.Equal(0, segunda.Creados);
            Assert.Single(ctx.AsientosContables.ToList());
        }

        [Fact]
        public void Movimiento_anulado_anula_su_asiento()
        {
            var (ctx, caja) = Escenario();
            using var _ = ctx;
            var mov = Mov(caja.Id, "Ingreso", 100m, "M-3");
            ctx.MovimientosFinancieros.Add(mov);
            ctx.SaveChanges();
            AsientoAutomaticoHelper.Sincronizar(ctx);

            mov.Estado = "Anulado";
            ctx.SaveChanges();
            var r = AsientoAutomaticoHelper.Sincronizar(ctx);

            Assert.Equal(1, r.Anulados);
            Assert.All(ctx.AsientosContables.ToList(), a => Assert.Equal("Anulado", a.Estado));
        }

        [Fact]
        public void Movimiento_editado_reemplaza_el_asiento()
        {
            var (ctx, caja) = Escenario();
            using var _ = ctx;
            var mov = Mov(caja.Id, "Ingreso", 100m, "M-4");
            ctx.MovimientosFinancieros.Add(mov);
            ctx.SaveChanges();
            AsientoAutomaticoHelper.Sincronizar(ctx);

            mov.Monto = 175m;
            ctx.SaveChanges();
            var r = AsientoAutomaticoHelper.Sincronizar(ctx);

            Assert.Equal(1, r.Reemplazados);
            var activos = ctx.AsientosContables.Where(a => a.Estado == "Activo").ToList();
            var vigente = Assert.Single(activos);
            Assert.Equal(175m, ctx.DetallesAsiento.Where(d => d.AsientoContableId == vigente.Id).Sum(d => d.Debe));
        }

        [Fact]
        public void Reversion_de_costo_con_monto_negativo_genera_el_asiento_inverso()
        {
            var (ctx, caja) = Escenario();
            using var _ = ctx;
            var inv = new PlanCuenta { Codigo = "1.1.03.001", Nombre = "Inventario", Tipo = "Activo" };
            ctx.PlanCuentas.Add(inv);
            var mov = Mov(caja.Id, "Egreso", -30m, "M-5");
            mov.Notas = SaldoHelper.MarcaSoloContable;
            ctx.MovimientosFinancieros.Add(mov);
            ctx.SaveChanges();

            var r = AsientoAutomaticoHelper.Sincronizar(ctx);

            Assert.Equal(1, r.Creados);
            var asiento = Assert.Single(ctx.AsientosContables.ToList());
            var lineas = ctx.DetallesAsiento.Where(d => d.AsientoContableId == asiento.Id).ToList();
            Assert.Equal(30m, lineas.Single(l => l.PlanCuentaId == inv.Id).Debe);   // el inventario vuelve a subir
            Assert.Equal(30m, lineas.Sum(l => l.Haber));
        }

        [Fact]
        public void Venta_a_credito_se_asienta_al_facturar_con_isv_y_el_cobro_cancela_la_cuenta_por_cobrar()
        {
            var (ctx, caja) = Escenario();
            using var _ = ctx;
            var cxc = new PlanCuenta { Codigo = "1.1.02.001", Nombre = "Clientes", Tipo = "Activo" };
            var ventas = new PlanCuenta { Codigo = "4.1.01", Nombre = "Ventas", Tipo = "Ingreso" };
            var isv = new PlanCuenta { Codigo = "2.1.02.001", Nombre = "ISV por Pagar", Tipo = "Pasivo" };
            ctx.PlanCuentas.AddRange(cxc, ventas, isv);
            var venta = new Venta { Numero = "V-1", Fecha = DateTime.Today, Subtotal = 100m, Impuestos = 15m, Total = 115m, EsCredito = true, SaldoPendiente = 115m, Estado = "Completada" };
            ctx.Ventas.Add(venta);
            ctx.SaveChanges();

            var cat = new CategoriaFinanciera { Tipo = "Ingreso", Nombre = "Ventas al crédito (cobros)", Activa = true, PlanCuentaId = ventas.Id };
            ctx.CategoriasFinancieras.Add(cat);
            ctx.SaveChanges();

            // Al facturar: Debe Clientes 115 / Haber Ventas 100 + ISV 15
            AsientoAutomaticoHelper.Sincronizar(ctx);
            var devengo = Assert.Single(ctx.AsientosContables.Where(a => a.Referencia == "DEV:V" + venta.Id).ToList());
            var l1 = ctx.DetallesAsiento.Where(d => d.AsientoContableId == devengo.Id).ToList();
            Assert.Equal(115m, l1.Single(l => l.PlanCuentaId == cxc.Id).Debe);
            Assert.Equal(100m, l1.Single(l => l.PlanCuentaId == ventas.Id).Haber);
            Assert.Equal(15m, l1.Single(l => l.PlanCuentaId == isv.Id).Haber);

            // Al cobrar: Debe Caja 115 / Haber Clientes 115 (no vuelve a ser ingreso)
            var cobro = Mov(caja.Id, "Ingreso", 115m, "M-6");
            cobro.Origen = "Venta";
            cobro.OrigenId = venta.Id;
            cobro.CategoriaId = cat.Id;
            cobro.FormaPago = "Efectivo";
            ctx.MovimientosFinancieros.Add(cobro);
            ctx.SaveChanges();
            AsientoAutomaticoHelper.Sincronizar(ctx);

            var asientoCobro = Assert.Single(ctx.AsientosContables.Where(a => a.Referencia == "MOV:" + cobro.Id).ToList());
            var l2 = ctx.DetallesAsiento.Where(d => d.AsientoContableId == asientoCobro.Id).ToList();
            Assert.Equal(115m, l2.Single(l => l.PlanCuentaId == cxc.Id).Haber);
            Assert.DoesNotContain(l2, l => l.PlanCuentaId == ventas.Id);
        }
    }
}
