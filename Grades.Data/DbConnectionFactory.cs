using System.Configuration;
using Microsoft.Data.SqlClient;

namespace Grades.Data;

// Punto único para crear conexiones. Es internal: fuera de la capa de Datos nadie abre conexiones.
internal static class DbConnectionFactory
{
    // La cadena vive en el App.config del proyecto de inicio (WPF), no en esta biblioteca.
    private static string ConnectionString =>
        ConfigurationManager.ConnectionStrings["DB"]?.ConnectionString
        ?? throw new InvalidOperationException(
            "No se encontró la cadena de conexión 'DB' en el App.config del proyecto de inicio.");

    public static SqlConnection Create() => new SqlConnection(ConnectionString);
}
