using System.Configuration;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Punto unico para crear conexiones. Es internal: fuera de la capa de Datos nadie abre conexiones.
internal static class DbConnectionFactory
{
    // La cadena vive en el App.config del proyecto de inicio (WPF), no en esta biblioteca.
    private static string ConnectionString =>
        ConfigurationManager.ConnectionStrings["BibliotecaDB"]?.ConnectionString
        ?? throw new InvalidOperationException(
            "No se encontro la cadena de conexion 'BibliotecaDB' en el App.config del proyecto de inicio.");

    public static SqlConnection Create() => new SqlConnection(ConnectionString);
}
