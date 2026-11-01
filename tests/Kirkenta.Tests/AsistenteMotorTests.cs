using Kirkenta.Services.Asistente;
using Xunit;

namespace Kirkenta.Tests
{
    public class AsistenteMotorTests
    {
        // Domingo 25/10/2026
        private static readonly DateTime Hoy = new(2026, 10, 25);

        [Fact]
        public void Normalizar_quita_acentos_y_signos()
        {
            Assert.Equal("cuanto vendi hoy", AsistenteMotor.Normalizar("¿Cuánto vendí HOY?"));
            Assert.Equal("f-0001", AsistenteMotor.Normalizar("F-0001"));
            Assert.Equal("1,250.50", AsistenteMotor.Normalizar("1,250.50"));
        }

        [Fact]
        public void Rango_hoy_ayer_y_semanas()
        {
            Assert.Equal((Hoy, Hoy), Tup(AsistenteMotor.ExtraerRango("ventas de hoy", Hoy)));
            Assert.Equal((Hoy.AddDays(-1), Hoy.AddDays(-1)), Tup(AsistenteMotor.ExtraerRango("ventas de ayer", Hoy)));
            Assert.Equal((new DateTime(2026, 10, 19), Hoy), Tup(AsistenteMotor.ExtraerRango("esta semana", Hoy)));
            Assert.Equal((new DateTime(2026, 10, 12), new DateTime(2026, 10, 18)), Tup(AsistenteMotor.ExtraerRango("la semana pasada", Hoy)));
            Assert.Equal((new DateTime(2026, 10, 19), Hoy), Tup(AsistenteMotor.ExtraerRango("ultimos 7 dias", Hoy)));
        }

        [Fact]
        public void Rango_meses_y_fechas_explicitas()
        {
            Assert.Equal((new DateTime(2026, 10, 1), Hoy), Tup(AsistenteMotor.ExtraerRango("gastos de este mes", Hoy)));
            Assert.Equal((new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)), Tup(AsistenteMotor.ExtraerRango("gastos del mes pasado", Hoy)));
            Assert.Equal((new DateTime(2026, 8, 1), new DateTime(2026, 8, 31)), Tup(AsistenteMotor.ExtraerRango("ventas de agosto", Hoy)));
            Assert.Equal((new DateTime(2026, 10, 1), new DateTime(2026, 10, 15)),
                Tup(AsistenteMotor.ExtraerRango("ventas del 01/10/2026 al 15/10/2026", Hoy)));
            Assert.Null(AsistenteMotor.ExtraerRango("saldo de Alfredo", Hoy));
        }

        [Fact]
        public void Numero_ignora_fechas_y_acepta_montos()
        {
            Assert.Equal(2350.50m, AsistenteMotor.ExtraerNumero("conté L 2,350.50"));
            Assert.Equal(1500m, AsistenteMotor.ExtraerNumero("1500"));
            Assert.Null(AsistenteMotor.ExtraerNumero("ventas del 01/10/2026"));
            Assert.Null(AsistenteMotor.ExtraerNumero("hola"));
        }

        [Theory]
        [InlineData("cuánto isv debo este mes", "isv_mes")]
        [InlineData("declaración de isv", "isv_mes")]
        [InlineData("cuánto debo de isv del mes pasado", "isv_mes")]
        [InlineData("ventas de hoy", "ventas_resumen")]
        [InlineData("cuanto vendimos ayer", "ventas_resumen")]
        [InlineData("cierra la caja de hoy", "cierre_caja")]
        [InlineData("quiero cerrar caja", "cierre_caja")]
        [InlineData("saldo de Alfredo Nahun", "saldo_cliente")]
        [InlineData("cuanto me debe Alfredo", "saldo_cliente")]
        [InlineData("productos con stock bajo", "stock_bajo")]
        [InlineData("productos mas vendidos del mes", "ventas_top")]
        [InlineData("cuentas por cobrar", "cxc")]
        [InlineData("cuentas por pagar", "cxp")]
        [InlineData("anular la factura 000123", "anular_factura")]
        [InlineData("ultimos cierres de caja", "ultimos_cierres")]
        [InlineData("cuanto hay en caja", "estado_caja")]
        [InlineData("gastos de este mes", "movimientos")]
        [InlineData("cuantos empleados hay", "empleados")]
        [InlineData("abre clientes", "ir_clientes")]
        [InlineData("llevame a nomina", "ir_nomina_pag")]
        [InlineData("ayuda", "ayuda")]
        [InlineData("productos", "productos_lista")]
        [InlineData("proveedores", "proveedores_lista")]
        [InlineData("clientes", "clientes_lista")]
        [InlineData("usuarios", "usuarios_lista")]
        [InlineData("facturas", "facturas_lista")]
        [InlineData("empleados", "empleados")]
        [InlineData("clientes morosos", "cxc")]
        [InlineData("repartidores", "repartidores")]
        [InlineData("ventas por metodo de pago de hoy", "informe_metodos")]
        [InlineData("ventas en efectivo de hoy", "informe_metodos")]
        [InlineData("cobros por transferencia de la semana", "informe_metodos")]
        [InlineData("como andan las entregas en la calle", "rutas_activas")]
        [InlineData("cotisaciones por vencer", "cotizaciones_por_vencer")]
        [InlineData("estado de las rutas", "rutas_activas")]
        [InlineData("envios entregados esta semana", "envios_entregados")]
        [InlineData("envios fallidos", "envios_fallidos")]
        [InlineData("estado del envio ENV-0001", "estado_envio")]
        [InlineData("repartidores activos", "repartidores")]
        [InlineData("mejores clientes del mes", "mejores_clientes")]
        [InlineData("cotizaciones por vencer", "cotizaciones_por_vencer")]
        [InlineData("devoluciones de la semana", "devoluciones_periodo")]
        [InlineData("bajas de inventario del mes", "bajas_periodo")]
        [InlineData("ordenes de compra pendientes", "ordenes_compra_pendientes")]
        [InlineData("permisos pendientes", "permisos_pendientes")]
        [InlineData("usuarios bloqueados", "usuarios_bloqueados")]
        [InlineData("que tengo pendiente", "pendientes")]
        [InlineData("a quien le toca cobrar", "recordatorios_cobro")]
        [InlineData("recordatorios de cobro", "recordatorios_cobro")]
        [InlineData("quien me debe", "recordatorios_cobro")]
        [InlineData("cobrar a clientes", "recordatorios_cobro")]
        [InlineData("cuando fue el ultimo respaldo", "respaldo_estado")]
        [InlineData("backup", "respaldo_estado")]
        [InlineData("que debo comprar", "sugerencia_compras")]
        [InlineData("sugerencia de compras", "sugerencia_compras")]
        [InlineData("como va el negocio", "panel_dueno")]
        [InlineData("panel del dueno", "panel_dueno")]
        [InlineData("cuanto ganamos este mes", "resumen_financiero")]
        [InlineData("aprobar vacaciones de Juan Perez", "aprobar_vacaciones")]
        [InlineData("aprobar la solicitud de vacaciones VAC-2026-0001", "aprobar_vacaciones")]
        [InlineData("aprobar permiso de Maria", "aprobar_permiso")]
        [InlineData("aprobar el vale de Pedro", "aprobar_vale")]
        [InlineData("autoriza el adelanto de Pedro", "aprobar_vale")]
        [InlineData("vales pendientes", "vales_pendientes")]
        [InlineData("vacaciones pendientes", "vacaciones")]
        [InlineData("abre conciliacion bancaria", "ir_conciliacion")]
        public void Interpretar_elige_la_accion_correcta(string mensaje, string esperado)
        {
            var r = AsistenteMotor.Interpretar(mensaje, AsistenteCatalogo.Todas, Hoy);
            Assert.NotNull(r.Accion);
            Assert.Equal(esperado, r.Accion!.Id);
        }

        [Fact]
        public void Interpretar_extrae_el_nombre_del_cliente()
        {
            var r = AsistenteMotor.Interpretar("saldo de Alfredo Nahun", AsistenteCatalogo.Todas, Hoy);
            Assert.Equal("alfredo nahun", r.Texto);
        }

        [Fact]
        public void Interpretar_sin_coincidencia_no_inventa_acciones()
        {
            var r = AsistenteMotor.Interpretar("blablabla xyz", AsistenteCatalogo.Todas, Hoy);
            Assert.Null(r.Accion);
        }

        [Fact]
        public void Cada_ejemplo_del_catalogo_lleva_a_su_propia_accion()
        {
            foreach (var a in AsistenteCatalogo.Todas)
            {
                var r = AsistenteMotor.Interpretar(a.Ejemplo, AsistenteCatalogo.Todas, Hoy);
                Assert.True(r.Accion?.Id == a.Id, $"El ejemplo “{a.Ejemplo}” de {a.Id} se interpretó como {r.Accion?.Id ?? "nada"}");
                Assert.False(r.Ambigua, $"El ejemplo “{a.Ejemplo}” de {a.Id} es ambiguo");
            }
        }

        [Fact]
        public void Catalogo_es_consistente()
        {
            var ids = AsistenteCatalogo.Todas.Select(a => a.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());

            foreach (var a in AsistenteCatalogo.Todas)
            {
                Assert.NotNull(a.Ejecutar);
                if (a.Tipo == TipoAccion.Operacion)
                {
                    Assert.NotNull(a.Preparar);
                    Assert.True(a.RequierePassword, $"{a.Id} cambia datos y debe pedir contraseña");
                    Assert.NotNull(a.PermModulo);
                }
            }
        }

        private static (DateTime, DateTime) Tup((DateTime desde, DateTime hasta, string etiqueta)? r)
        {
            Assert.NotNull(r);
            return (r!.Value.desde, r.Value.hasta);
        }

        [Theory]
        [InlineData("ventas", "Ventas")]
        [InlineData("que puedo hacer en finanzas", "Finanzas")]
        [InlineData("modulo de inventario", "Inventario")]
        [InlineData("recursos humanos", "RRHH")]
        [InlineData("ayuda logistica", "Logística")]
        public void Detecta_cuando_solo_se_nombra_un_modulo(string mensaje, string esperado)
        {
            Assert.Equal(esperado, AsistenteMotor.ModuloMencionado(mensaje));
        }

        [Theory]
        [InlineData("ventas de hoy")]
        [InlineData("valor del inventario")]
        [InlineData("compras pendientes de recibir")]
        [InlineData("productos")]
        public void No_confunde_una_peticion_con_un_modulo(string mensaje)
        {
            Assert.Null(AsistenteMotor.ModuloMencionado(mensaje));
        }
    }
}
