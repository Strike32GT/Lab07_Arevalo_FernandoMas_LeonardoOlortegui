using System.Configuration;
using System.Globalization;

namespace Grades.Business;

// Parámetros del negocio leídos desde <appSettings> del App.config del proyecto de inicio.
// Así se puede cambiar, por ejemplo, la nota aprobatoria sin recompilar.
// Si la clave no existe o no es válida, se usa el valor por defecto.
public static class BusinessSettings
{
    public static decimal MinScore { get; } = Read("MinScore", 0m);
    public static decimal MaxScore { get; } = Read("MaxScore", 20m);
    public static decimal PassingScore { get; } = Read("PassingScore", 13m);

    private static decimal Read(string key, decimal defaultValue)
    {
        string value = ConfigurationManager.AppSettings[key];

        // Cultura invariante: en el App.config el separador decimal siempre es el punto (10.5).
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }
}
