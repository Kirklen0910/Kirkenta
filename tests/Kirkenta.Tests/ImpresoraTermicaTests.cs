using System.Text;
using Kirkenta.Helpers;
using Kirkenta.Services;
using Xunit;

namespace Kirkenta.Tests
{
    public class ImpresoraTermicaTests
    {
        private static int Contar(byte[] datos, params byte[] patron)
        {
            int n = 0;
            for (int i = 0; i <= datos.Length - patron.Length; i++)
            {
                var igual = true;
                for (int j = 0; j < patron.Length; j++) if (datos[i + j] != patron[j]) { igual = false; break; }
                if (igual) n++;
            }
            return n;
        }

        private static TicketDatos Ejemplo() => new()
        {
            Empresa = "Mi Tienda",
            Cabecera = new List<string> { "RTN: 0801-1999-123456", "Col. Centro, Tegucigalpa" },
            Titulo = "FACTURA 000-001-01-00000005",
            Datos = new List<string> { "Fecha: 25/10/2026 10:30", "Cliente: Consumidor final" },
            Lineas = new List<TicketLinea> { new("Coca Cola 600ml", 2, 25m, 50m), new("Producto con un nombre bastante largo para obligar a partir la línea", 1, 100m, 100m) },
            Totales = new List<(string, string)> { ("Subtotal", "130.43"), ("ISV", "19.57") },
            TotalValor = "150.00",
            Pie = new List<string> { "¡Gracias por su compra!" }
        };

        [Fact]
        public void Envolver_parte_en_lineas_sin_pasarse_del_ancho()
        {
            var l = EscPosBuilder.Envolver("Producto con un nombre bastante largo para obligar a partir la línea", 20);
            Assert.True(l.Count >= 3);
            Assert.All(l, x => Assert.True(x.Length <= 20));
            Assert.Equal("Producto con un", l[0]);
        }

        [Fact]
        public void Envolver_corta_palabras_gigantes_y_quita_caracteres_de_control()
        {
            var l = EscPosBuilder.Envolver(new string('A', 50), 20);
            Assert.All(l, x => Assert.True(x.Length <= 20));
            Assert.DoesNotContain('\u001B', EscPosBuilder.Limpiar("hola\u001B@mundo"));
        }

        [Fact]
        public void Fila_alinea_a_la_derecha_y_respeta_el_ancho()
        {
            var b = new EscPosBuilder(24).Fila("Total", "150.00");
            var texto = Encoding.ASCII.GetString(b.ToArray());
            Assert.Contains("Total" + new string(' ', 24 - 5 - 6) + "150.00", texto);
        }

        [Fact]
        public void Construir_incluye_gaveta_solo_si_esta_activa_y_un_corte_por_copia()
        {
            var sin = TicketTermico.Construir(Ejemplo(), new ImpresoraConfig { Columnas = 42, AbrirGaveta = false, Copias = 1 });
            Assert.Equal(0, Contar(sin, 0x1B, 0x70, 0x00, 0x19, 0xFA));
            Assert.Equal(1, Contar(sin, 0x1D, 0x56, 0x42, 0x03));

            var con = TicketTermico.Construir(Ejemplo(), new ImpresoraConfig { Columnas = 42, AbrirGaveta = true, Copias = 2 });
            Assert.Equal(1, Contar(con, 0x1B, 0x70, 0x00, 0x19, 0xFA)); // la gaveta se abre una sola vez
            Assert.Equal(2, Contar(con, 0x1D, 0x56, 0x42, 0x03));       // un corte por copia
        }

        [Fact]
        public void Construir_arranca_con_reinicio_de_impresora_y_contiene_los_datos()
        {
            var bytes = TicketTermico.Construir(Ejemplo(), new ImpresoraConfig { Columnas = 32 });
            Assert.Equal(0x1B, bytes[0]);
            Assert.Equal(0x40, bytes[1]);
            var texto = Encoding.ASCII.GetString(bytes);
            Assert.Contains("Mi Tienda", texto);
            Assert.Contains("150.00", texto);
        }

        [Fact]
        public void Configuracion_se_normaliza_y_se_valida()
        {
            var c = ImpresoraConfigStore.Normalizar(new ImpresoraConfig { Modo = "raro", Auto = "x", Columnas = 500, Copias = 9, Puerto = 0 });
            Assert.Equal("ninguna", c.Modo);
            Assert.Equal("ninguno", c.Auto);
            Assert.Equal(64, c.Columnas);
            Assert.Equal(3, c.Copias);
            Assert.Equal(9100, c.Puerto);

            Assert.NotNull(ImpresoraConfigStore.Validar(new ImpresoraConfig { Modo = "red", Host = "" }));
            Assert.NotNull(ImpresoraConfigStore.Validar(new ImpresoraConfig { Modo = "windows", Impresora = " " }));
            Assert.Null(ImpresoraConfigStore.Validar(new ImpresoraConfig { Modo = "red", Host = "192.168.1.50" }));
        }
    }
}
