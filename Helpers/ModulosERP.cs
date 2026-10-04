namespace Kirkenta.Helpers
{
    public static class ModulosERP
    {
        public static readonly Dictionary<string, List<string>> Modulos = new()
        {
            { "Usuarios", new List<string> {
                "Index", "Create", "Edit", "Delete",
                "Roles", "RolesCreate", "RolesEdit", "RolesDelete",
                "Perfil", "CambiarPassword", "Actividad"
            } },
            { "Ventas", new List<string> {
                "POS", "Index", "Create", "Edit", "Delete",
                "Cotizaciones", "Pedidos", "Facturas",
                "Clientes", "ClientesCreate", "ClientesEdit", "ClientesDelete",
                "ClientesExport", "ClientesImport",
                "Devoluciones"
            } },
            { "Inventario", new List<string> {
                "Index",
                "Productos", "ProductosCreate", "ProductosEdit", "ProductosDelete",
                "ProductosExport", "ProductosImport",
                "Categorias", "UnidadesMedida",
                "Entradas", "Salidas", "Bajas"
            } },
            { "Compras", new List<string> {
                "Index",
                "Proveedores", "ProveedoresCreate", "ProveedoresEdit", "ProveedoresDelete",
                "ProveedoresExport", "ProveedoresImport",
                "Ordenes", "OrdenesCreate", "OrdenesEdit", "OrdenesDelete", "OrdenesRecibir",
                "Pagos", "PagosCreate",
                "CuentasPorPagar",
                "Reportes"
            } },
            { "Finanzas", new List<string> {
                "Index",
                "Cuentas", "CuentasCreate", "CuentasEdit", "CuentasDelete",
                "Categorias", "CategoriasCreate", "CategoriasEdit", "CategoriasDelete",
                "Movimientos", "MovimientosCreate", "MovimientosEdit", "MovimientosAnular",
                "Aperturas", "AperturasCreate",
                "Cierres", "CierresCreate", "CierresAprobar",
                "Reportes", "FlujoCaja", "EstadoResultados", "BalanceGeneral",
                "Conciliacion", "ISV"
            } },
            { "Produccion", new List<string> { "Index", "Calidad" } },
            { "RRHH", new List<string> {
                "Index",
                "Empleados", "EmpleadosCreate", "EmpleadosEdit", "EmpleadosDelete",
                "Documentos", "DocumentosCreate", "DocumentosDelete",
                "Expedientes", "ExpedientesCreate", "ExpedientesDelete",
                "Vacaciones", "VacacionesCreate", "VacacionesAprobar",
                "Permisos", "PermisosCreate", "PermisosAprobar",
                "Vales", "ValesCreate", "ValesAprobar", "ValesEntregar",
                "Nomina", "NominaCreate", "NominaAprobar", "NominaPagar",
                "Feriados", "FeriadosCreate",
                "Alertas", "AlertasCreate",
                "Reportes"
            } },
            { "Reportes", new List<string> { "Index", "Ventas", "Compras" } },
            { "Logistica", new List<string> { "Index", "Envios" } },
            { "Activos", new List<string> { "Index", "Mantenimiento" } },
            { "Seguridad", new List<string> { "Index", "Auditoria" } },
            { "Configuracion", new List<string> {
                "Index", "Empresa", "Nomenclatura", "Series", "MetodosPago"
            } }
        };

        public static string NombreBonito(string modulo)
        {
            return modulo switch
            {
                "Usuarios" => "Usuarios",
                "Ventas" => "Ventas",
                "Inventario" => "Inventario",
                "Compras" => "Compras",
                "Finanzas" => "Finanzas",
                "Produccion" => "Producción",
                "RRHH" => "Recursos Humanos",
                "Reportes" => "Reportes",
                "Logistica" => "Logística",
                "Activos" => "Activos",
                "Seguridad" => "Seguridad",
                "Configuracion" => "Configuración",
                _ => modulo
            };
        }

        public static string NombreBonitoSub(string submodulo)
        {
            return submodulo switch
            {
                "Index" => "Lista / Vista",
                "Create" => "Crear",
                "Edit" => "Editar",
                "Delete" => "Eliminar",
                "Roles" => "Roles",
                "RolesCreate" => "Nuevo rol",
                "RolesEdit" => "Editar rol",
                "RolesDelete" => "Eliminar rol",
                "Perfil" => "Mi perfil",
                "CambiarPassword" => "Cambiar contraseña",
                "Actividad" => "Historial de actividad",
                "POS" => "Punto de venta",
                "Cotizaciones" => "Cotizaciones",
                "Pedidos" => "Pedidos",
                "Facturas" => "Facturas",
                "Clientes" => "Clientes",
                "ClientesCreate" => "Nuevo cliente",
                "ClientesEdit" => "Editar cliente",
                "ClientesDelete" => "Eliminar cliente",
                "ClientesExport" => "Exportar clientes",
                "ClientesImport" => "Importar clientes",
                "Devoluciones" => "Devoluciones",
                "Productos" => "Productos",
                "ProductosCreate" => "Nuevo producto",
                "ProductosEdit" => "Editar producto",
                "ProductosDelete" => "Eliminar producto",
                "ProductosExport" => "Exportar productos",
                "ProductosImport" => "Importar productos",
                "Categorias" => "Categorías",
                "UnidadesMedida" => "Unidades de medida",
                "Entradas" => "Entradas",
                "Salidas" => "Salidas",
                "Bajas" => "Bajas de inventario",
                "Proveedores" => "Proveedores",
                "ProveedoresCreate" => "Nuevo proveedor",
                "ProveedoresEdit" => "Editar proveedor",
                "ProveedoresDelete" => "Eliminar proveedor",
                "ProveedoresExport" => "Exportar proveedores",
                "ProveedoresImport" => "Importar proveedores",
                "Ordenes" => "Órdenes de compra",
                "OrdenesCreate" => "Nueva orden",
                "OrdenesEdit" => "Editar orden",
                "OrdenesDelete" => "Eliminar orden",
                "OrdenesRecibir" => "Recibir mercancía",
                "Pagos" => "Pagos a proveedores",
                "PagosCreate" => "Registrar pago",
                "CuentasPorPagar" => "Cuentas por pagar",
                // FINANZAS
                "Cuentas" => "Cuentas financieras",
                "CuentasCreate" => "Nueva cuenta",
                "CuentasEdit" => "Editar cuenta",
                "CuentasDelete" => "Eliminar cuenta",
                "CategoriasFinancieras" => "Categorías financieras",
                "CategoriasCreateFin" => "Nueva categoría financiera",
                "CategoriasEditFin" => "Editar categoría financiera",
                "CategoriasDeleteFin" => "Eliminar categoría financiera",
                "Movimientos" => "Movimientos",
                "MovimientosCreate" => "Nuevo movimiento",
                "MovimientosEdit" => "Editar movimiento",
                "MovimientosAnular" => "Anular movimiento",
                "Aperturas" => "Aperturas de caja",
                "AperturasCreate" => "Nueva apertura",
                "Cierres" => "Cierres de caja",
                "CierresCreate" => "Nuevo cierre",
                "CierresAprobar" => "Aprobar cierre",
                "FlujoCaja" => "Flujo de caja",
                "EstadoResultados" => "Estado de resultados",
                "BalanceGeneral" => "Balance general",
                "Conciliacion" => "Conciliación bancaria",
                "ISV" => "ISV / IVA",
                // RRHH
                "Empleados" => "Empleados",
                "EmpleadosCreate" => "Nuevo empleado",
                "EmpleadosEdit" => "Editar empleado",
                "EmpleadosDelete" => "Eliminar empleado",
                "Documentos" => "Documentos",
                "DocumentosCreate" => "Subir documento",
                "DocumentosDelete" => "Eliminar documento",
                "Expedientes" => "Expedientes",
                "ExpedientesCreate" => "Nuevo expediente",
                "ExpedientesDelete" => "Eliminar expediente",
                "Vacaciones" => "Vacaciones",
                "VacacionesCreate" => "Nueva solicitud",
                "VacacionesAprobar" => "Aprobar vacaciones",
                "Permisos" => "Permisos y licencias",
                "PermisosCreate" => "Nueva solicitud",
                "PermisosAprobar" => "Aprobar permiso",
                "Vales" => "Vales a empleados",
                "ValesCreate" => "Nuevo vale",
                "ValesAprobar" => "Aprobar vale",
                "ValesEntregar" => "Entregar vale",
                "Nomina" => "Nómina",
                "NominaCreate" => "Generar nómina",
                "NominaAprobar" => "Aprobar nómina",
                "NominaPagar" => "Pagar nómina",
                "Feriados" => "Feriados",
                "FeriadosCreate" => "Nuevo feriado",
                "Alertas" => "Alertas",
                "AlertasCreate" => "Nueva alerta",
                // Genéricos
                "Ingresos" => "Ingresos",
                "Egresos" => "Egresos",
                "Impuestos" => "Impuestos",
                "Calidad" => "Calidad",
                "Envios" => "Envíos",
                "Mantenimiento" => "Mantenimiento",
                "Auditoria" => "Auditoría",
                "Empresa" => "Datos de la empresa",
                "Nomenclatura" => "Nomenclatura",
                "Series" => "Series de documentos",
                "MetodosPago" => "Métodos de pago",
                _ => submodulo
            };
        }
    }
}