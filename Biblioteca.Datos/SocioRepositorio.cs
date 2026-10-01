using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Capa de Datos para los socios. Solo SQL parametrizado: ninguna regla de negocio aqui.
public class SocioRepositorio : ISocioRepositorio
{
    public List<Socio> Listar(string busqueda, bool incluirInactivos)
    {
        // LibrosPendientes viene de un LEFT JOIN con los detalles sin devolver:
        // asi el conteo se trae en la misma consulta y no en un segundo round-trip.
        const string sql = @"
            SELECT S.SocioId, S.DNI, S.Nombre, S.Email, S.Activo,
                   (SELECT COUNT(1)
                      FROM DetallePrestamo D
                      INNER JOIN Prestamos P ON P.PrestamoId = D.PrestamoId
                     WHERE P.SocioId = S.SocioId AND D.FechaDevolucion IS NULL) AS Pendientes
            FROM Socios S
            WHERE (@Todos = 1 OR S.Activo = 1)
              AND (@Busqueda = '' OR S.Nombre LIKE @Patron OR S.DNI LIKE @Patron)
            ORDER BY S.Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Todos", incluirInactivos ? 1 : 0);
        cmd.Parameters.AddWithValue("@Busqueda", (busqueda ?? string.Empty).Trim());
        cmd.Parameters.AddWithValue("@Patron", "%" + (busqueda ?? string.Empty).Trim() + "%");
        cn.Open();

        var lista = new List<Socio>();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            lista.Add(new Socio
            {
                SocioId = dr.GetInt32(0),
                DNI = dr.GetString(1),
                Nombre = dr.GetString(2),
                Email = dr.IsDBNull(3) ? null : dr.GetString(3),
                Activo = dr.GetBoolean(4),
                LibrosPendientes = dr.GetInt32(5)
            });
        }
        return lista;
    }

    public Socio ObtenerPorId(int socioId)
    {
        const string sql = "SELECT SocioId, DNI, Nombre, Email, Activo FROM Socios WHERE SocioId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", socioId);
        cn.Open();

        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return null;

        return new Socio
        {
            SocioId = dr.GetInt32(0),
            DNI = dr.GetString(1),
            Nombre = dr.GetString(2),
            Email = dr.IsDBNull(3) ? null : dr.GetString(3),
            Activo = dr.GetBoolean(4)
        };
    }

    public bool ExisteDNI(string dni, int excluirSocioId)
    {
        const string sql = "SELECT COUNT(1) FROM Socios WHERE DNI = @DNI AND SocioId <> @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@DNI", dni);
        cmd.Parameters.AddWithValue("@Id", excluirSocioId);
        cn.Open();
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public bool TienePrestamosPendientes(int socioId)
    {
        const string sql = @"SELECT COUNT(1)
                             FROM DetallePrestamo D
                             INNER JOIN Prestamos P ON P.PrestamoId = D.PrestamoId
                             WHERE P.SocioId = @Id AND D.FechaDevolucion IS NULL";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", socioId);
        cn.Open();
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public void Insertar(Socio socio)
    {
        const string sql = @"INSERT INTO Socios (DNI, Nombre, Email, Activo)
                             VALUES (@DNI, @Nombre, @Email, @Activo)";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, socio);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    public void Actualizar(Socio socio)
    {
        const string sql = @"UPDATE Socios
                             SET DNI = @DNI, Nombre = @Nombre, Email = @Email, Activo = @Activo
                             WHERE SocioId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, socio);
        cmd.Parameters.AddWithValue("@Id", socio.SocioId);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    // Eliminacion logica: Activo = 0. Nunca un DELETE fisico.
    public void DarDeBaja(int socioId)
    {
        const string sql = "UPDATE Socios SET Activo = 0 WHERE SocioId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", socioId);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    private static void AgregarParametros(SqlCommand cmd, Socio socio)
    {
        cmd.Parameters.AddWithValue("@DNI", socio.DNI);
        cmd.Parameters.AddWithValue("@Nombre", socio.Nombre);
        cmd.Parameters.AddWithValue("@Email", (object)socio.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Activo", socio.Activo);
    }
}
