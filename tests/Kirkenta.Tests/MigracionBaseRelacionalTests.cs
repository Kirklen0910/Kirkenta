using System.Text;
using Kirkenta.Helpers.Migracion;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class MigracionBaseRelacionalTests
    {
        private const string Sql = @"CREATE DATABASE IF NOT EXISTS ferreteria;
USE ferreteria;

CREATE TABLE categorias(id INT PRIMARY KEY,nombre VARCHAR(100));
CREATE TABLE marcas(id INT PRIMARY KEY,nombre VARCHAR(100));
CREATE TABLE proveedores(id INT PRIMARY KEY,nombre VARCHAR(100));
CREATE TABLE clientes(id INT PRIMARY KEY,nombre VARCHAR(100));
CREATE TABLE empleados(id INT PRIMARY KEY,nombre VARCHAR(100));
CREATE TABLE sucursales(id INT PRIMARY KEY,nombre VARCHAR(100));
CREATE TABLE productos(id INT PRIMARY KEY,nombre VARCHAR(100),categoria_id INT,marca_id INT,precio DECIMAL(10,2));
CREATE TABLE inventario(id INT PRIMARY KEY,producto_id INT,sucursal_id INT,stock INT);
CREATE TABLE compras(id INT PRIMARY KEY,proveedor_id INT,fecha DATE);
CREATE TABLE detalle_compras(id INT PRIMARY KEY,compra_id INT,producto_id INT,cantidad INT,costo DECIMAL(10,2));
CREATE TABLE ventas(id INT PRIMARY KEY,cliente_id INT,empleado_id INT,fecha DATE);
CREATE TABLE detalle_ventas(id INT PRIMARY KEY,venta_id INT,producto_id INT,cantidad INT,precio DECIMAL(10,2));
CREATE TABLE pagos(id INT PRIMARY KEY,venta_id INT,monto DECIMAL(10,2));
CREATE TABLE facturas(id INT PRIMARY KEY,venta_id INT,numero VARCHAR(30));
CREATE TABLE roles(id INT PRIMARY KEY,nombre VARCHAR(50));
CREATE TABLE usuarios(id INT PRIMARY KEY,empleado_id INT,rol_id INT,usuario VARCHAR(50));
CREATE TABLE bodegas(id INT PRIMARY KEY,nombre VARCHAR(100));
CREATE TABLE movimientos_inventario(id INT PRIMARY KEY,producto_id INT,tipo VARCHAR(20),cantidad INT);
CREATE TABLE devoluciones(id INT PRIMARY KEY,venta_id INT,motivo VARCHAR(150));
CREATE TABLE auditoria(id INT PRIMARY KEY,tabla_afectada VARCHAR(50),accion VARCHAR(50));

INSERT INTO categorias VALUES (1,'Herramientas'),(2,'Pinturas'),(3,'Electricidad');
INSERT INTO marcas VALUES (1,'Truper'),(2,'Stanley'),(3,'Pretul');
INSERT INTO proveedores VALUES (1,'Proveedor Norte'),(2,'Proveedor Centro'),(3,'Proveedor Sur');
INSERT INTO clientes VALUES (1,'Juan Perez'),(2,'Maria Lopez'),(3,'Carlos Gomez');
INSERT INTO empleados VALUES (1,'Ana Diaz'),(2,'Luis Ramos'),(3,'Pedro Cruz');
INSERT INTO sucursales VALUES (1,'Sucursal SPS'),(2,'Sucursal Tegucigalpa');
INSERT INTO productos VALUES
(1,'Martillo',1,1,12.50),
(2,'Taladro',1,2,95.00),
(3,'Pintura Blanca',2,3,22.75),
(4,'Cable Electrico',3,1,15.00),
(5,'Destornillador',1,2,8.99);
INSERT INTO inventario VALUES (1,1,1,100),(2,2,1,35),(3,3,1,50),(4,4,2,80),(5,5,2,120);
INSERT INTO compras VALUES (1,1,'2026-01-10'),(2,2,'2026-02-15'),(3,3,'2026-03-20');
INSERT INTO detalle_compras VALUES (1,1,1,50,10.00),(2,2,2,20,80.00),(3,3,3,40,18.00);
INSERT INTO ventas VALUES (1,1,1,'2026-04-01'),(2,2,2,'2026-04-03'),(3,3,3,'2026-04-05');
INSERT INTO detalle_ventas VALUES (1,1,1,2,12.50),(2,2,2,1,95.00),(3,3,3,3,22.75);
INSERT INTO pagos VALUES (1,1,25.00),(2,2,95.00),(3,3,68.25);
INSERT INTO facturas VALUES (1,1,'F001'),(2,2,'F002'),(3,3,'F003');
INSERT INTO roles VALUES (1,'Administrador'),(2,'Vendedor'),(3,'Bodeguero');
INSERT INTO usuarios VALUES (1,1,1,'admin'),(2,2,2,'luis'),(3,3,3,'pedro');
INSERT INTO bodegas VALUES (1,'Bodega Principal'),(2,'Bodega Secundaria');
INSERT INTO movimientos_inventario VALUES (1,1,'ENTRADA',50),(2,2,'SALIDA',5),(3,3,'ENTRADA',20);
INSERT INTO devoluciones VALUES (1,1,'Producto defectuoso'),(2,2,'Error de compra');
INSERT INTO auditoria VALUES (1,'productos','INSERT'),(2,'ventas','UPDATE');
";

        /// <summary>Base relacional típica (todo por ids, totales en las líneas, número en otra tabla, stock por sucursal).</summary>
        [Fact]
        public void BaseRelacional_SeMigraCompletaYCuadra()
        {
            var ctx = TestDb.Crear();
            var tablas = MigracionLector.Leer("ferreteria.sql", new MemoryStream(Encoding.UTF8.GetBytes(Sql))).Tablas;
            var plan = MigracionAutomaticaHelper.Planificar(tablas);
            Assert.Equal("", plan.Single(p => p.TablaNombre == "sucursales").Destino);
            Assert.Equal("", plan.Single(p => p.TablaNombre == "inventario").Destino);
            Assert.True(plan.Single(p => p.TablaNombre == "ventas").Incluir);
            Assert.True(plan.Single(p => p.TablaNombre == "compras").Incluir);

            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, plan, null, true);
            Assert.True(res.Ok, res.ErrorFatal);
            Assert.All(res.Conciliacion, c => Assert.True(c.Cuadra, c.Tabla + ": " + c.Explicacion));
            Assert.All(res.Verificacion, v => Assert.True(v.Exacto, v.Tabla + ": " + string.Join(" | ", v.Diferencias)));

            var martillo = ctx.Productos.Single(p => p.Nombre == "Martillo");
            Assert.Equal(12.50m, martillo.PrecioVenta);
            Assert.Equal(10.00m, martillo.PrecioCompra);                 // última compra
            Assert.Equal(100m, martillo.Stock);                          // inventario por sucursal
            Assert.Equal("Herramientas", ctx.Categorias.Single(c => c.Id == martillo.CategoriaId).Nombre);

            var f2 = ctx.Ventas.Single(v => v.Numero == "H-F002");      // número tomado de «facturas»
            Assert.Equal(95.00m, f2.Total);                              // total = suma de sus líneas
            Assert.Equal("Maria Lopez", ctx.Clientes.Single(c => c.Id == f2.ClienteId).Nombre);
            Assert.Equal(2820.00m, ctx.OrdenesCompra.Sum(o => o.Total));
            Assert.Equal(3, ctx.Empleados.Select(e => e.Codigo).Distinct().Count());   // códigos sin repetir
            Assert.Equal("Diaz", ctx.Empleados.Single(e => e.Nombres == "Ana").Apellidos);

            // «Así se ve tu negocio»: los números que el dueño reconoce
            var n = res.Negocio!;
            Assert.Equal(3, n.Facturas);
            Assert.Equal(188.25m, n.TotalVendido);
            Assert.Equal("Taladro", n.TopProductos.First().Nombre);
            Assert.Equal(95.00m, n.TopProductos.First().Monto);
            Assert.Equal(4700.00m, n.InventarioCosto);
            Assert.Equal(5, n.Productos);
            Assert.Equal(3, n.Clientes);
            Assert.Single(n.VentasPorMes);
        }
    }
}
