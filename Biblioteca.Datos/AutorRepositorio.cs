using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Capa de Datos para los autores.
public class AutorRepositorio : IAutorRepositorio
{
    public List<Autor> Listar(bool incluirInactivos)
    {
        const string sql = @"
            SELECT AutorId, Nombre, Nacionalidad, Activo
            FROM Autores
            WHERE @Todos = 1 OR Activo = 1
            ORDER BY Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Todos", incluirInactivos ? 1 : 0);
        cn.Open();

        var lista = new List<Autor>();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            lista.Add(new Autor
            {
                AutorId = dr.GetInt32(0),
                Nombre = dr.GetString(1),
                Nacionalidad = dr.IsDBNull(2) ? null : dr.GetString(2),
                Activo = dr.GetBoolean(3)
            });
        }
        return lista;
    }
}
