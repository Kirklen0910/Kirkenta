namespace Kirkenta.Helpers
{
    /// <summary>
    /// Datos de las actualizaciones por internet. Los llena tools\generar-clave-actualizaciones.ps1 (una sola vez).
    /// Repositorio: "usuario/repositorio" de GitHub donde se publican las versiones (Releases). Debe ser PUBLICO.
    /// ClavePublicaXml: clave publica RSA con la que se verifica la firma de cada actualizacion. Sin ella el sistema
    /// NO instala nada descargado (la clave privada solo la tiene quien publica las versiones).
    /// </summary>
    public static class ConfigActualizaciones
    {
        public const string Repositorio = "Kirklen0910/UpdateKirkenta";
        public const string ClavePublicaXml = "<RSAKeyValue><Modulus>2s7dKH2NsWKRMAXe5jobgE1adF8Q65UAikft+II50lYXw9ZWE/YStcMj+Bra5IPrTYGxcAvFVMuT12O04ciYdD7Jt+0zwGH8tp0qbbGuafOEwnBz67lxfV/8qAUKBNBQvMoIssg0IZ3/e9ZCf+mLK/fjq+IjRdiEWoCp181PTcqUQqAADiqfALJi8ZAW7Qzc9j9GNw5rvuSN5MsLARTfgsCmji8dktu+iznoEC7sv6rbEcgOj1PPIBwykp5U2KTcInkOPHssjORkt4RU7iJ+y+VpjGYYeuSGAlNF1PjkXdX1R46LumElL9LgONiamrYBugV2XgKUt/ay6jRcfEG2Jp3OVrDiKOS3RiqeVj+jr8b5TlFvt2y0zAA6h29sEezL3wNAYh54gKVkjaEqRQfOnG/RGXp4+nOXsQFqRrT1R5IsUrXLjVeRPeTXh8bCSL7rFbiN2ATYW2qOJ5MErdJrLgpR3AKwoJpno2Ja74zRzTDdxMDG2Uxi59RisWacdPFh</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
    }
}
