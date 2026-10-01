using System.Configuration;
using System.Globalization;

namespace Biblioteca.Negocio;

// Parametros del negocio leidos desde <appSettings> del App.config del proyecto
// de inicio. Asi se puede cambiar la multa o el limite de libros sin recompilar.
public static class ParametrosNegocio
{
    // Regla: un socio no puede tener mas de 3 libros pendientes.
    public static int MaximoLibrosPendientes { get; } = LeerEntero("MaximoLibrosPendientes", 3);

    // Multa por dia de retraso en la devolucion.
    public static decimal MultaPorDia { get; } = LeerDecimal("MultaPorDia", 1.50m);

    // Dias de prestamo por defecto (la fecha limite se calcula en Negocio).
    public static int DiasPrestamo { get; } = LeerEntero("DiasPrestamo", 14);

    private static int LeerEntero(string clave, int porDefecto)
    {
        string valor = ConfigurationManager.AppSettings[clave];

        // Cultura invariante: en el App.config el separador decimal siempre es el punto.
        return int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int resultado)
            ? resultado
            : porDefecto;
    }

    private static decimal LeerDecimal(string clave, decimal porDefecto)
    {
        string valor = ConfigurationManager.AppSettings[clave];

        return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal resultado)
            ? resultado
            : porDefecto;
    }
}
