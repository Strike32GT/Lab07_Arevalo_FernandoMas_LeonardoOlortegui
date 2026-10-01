using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Capa de Datos para los prestamos. Acá viven las transacciones: el Negocio
// decide SI se presta, esta capa garantiza que el guardado sea todo o nada.
public class PrestamoRepositorio : IPrestamoRepositorio
{
    public List<Prestamo> ListarPendientes()
    {
        const string sql = @"
            SELECT P.PrestamoId, P.SocioId, P.FechaPrestamo, P.FechaLimite, P.Estado,
                   S.Nombre, S.DNI
            FROM Prestamos P
            INNER JOIN Socios S ON S.SocioId = P.SocioId
            WHERE P.Estado = 'Pendiente'
            ORDER BY P.FechaPrestamo DESC";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cn.Open();

        var lista = new List<Prestamo>();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            lista.Add(LeerPrestamo(dr));
        }
        return lista;
    }

    public Prestamo ObtenerPorId(int prestamoId)
    {
        const string sql = @"
            SELECT P.PrestamoId, P.SocioId, P.FechaPrestamo, P.FechaLimite, P.Estado,
                   S.Nombre, S.DNI
            FROM Prestamos P
            INNER JOIN Socios S ON S.SocioId = P.SocioId
            WHERE P.PrestamoId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", prestamoId);
        cn.Open();

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? LeerPrestamo(dr) : null;
    }

    public List<DetallePrestamo> ListarDetalles(int prestamoId, bool soloPendientes)
    {
        const string sql = @"
            SELECT D.PrestamoId, D.LibroId, D.FechaDevolucion, L.Titulo
            FROM DetallePrestamo D
            INNER JOIN Libros L ON L.LibroId = D.LibroId
            WHERE D.PrestamoId = @Id
              AND (@SoloPendientes = 0 OR D.FechaDevolucion IS NULL)
            ORDER BY L.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", prestamoId);
        cmd.Parameters.AddWithValue("@SoloPendientes", soloPendientes ? 1 : 0);
        cn.Open();

        var lista = new List<DetallePrestamo>();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            lista.Add(new DetallePrestamo
            {
                PrestamoId = dr.GetInt32(0),
                LibroId = dr.GetInt32(1),
                FechaDevolucion = dr.IsDBNull(2) ? (DateTime?)null : dr.GetDateTime(2),
                Titulo = dr.GetString(3)
            });
        }
        return lista;
    }

    // Reporte por intervalo de fechas. INNER JOIN entre las cuatro tablas,
    // como pide el enunciado. Devuelve entidades, nunca DataTable.
    public List<PrestamoReporte> ReportePorIntervalo(DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT P.PrestamoId, S.Nombre, S.DNI, L.Titulo,
                   P.FechaPrestamo, P.FechaLimite, D.FechaDevolucion,
                   CASE WHEN D.FechaDevolucion IS NULL THEN 'Pendiente' ELSE 'Devuelto' END AS Estado
            FROM Prestamos P
            INNER JOIN DetallePrestamo D ON D.PrestamoId = P.PrestamoId
            INNER JOIN Libros L ON L.LibroId = D.LibroId
            INNER JOIN Socios S ON S.SocioId = P.SocioId
            WHERE P.FechaPrestamo >= @Desde AND P.FechaPrestamo <= @Hasta
            ORDER BY P.FechaPrestamo DESC, L.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Desde", desde.Date);
        cmd.Parameters.AddWithValue("@Hasta", hasta.Date);
        cn.Open();

        var lista = new List<PrestamoReporte>();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            lista.Add(new PrestamoReporte
            {
                PrestamoId = dr.GetInt32(0),
                Socio = dr.GetString(1),
                DNI = dr.GetString(2),
                Libro = dr.GetString(3),
                FechaPrestamo = dr.GetDateTime(4),
                FechaLimite = dr.GetDateTime(5),
                FechaDevolucion = dr.IsDBNull(6) ? (DateTime?)null : dr.GetDateTime(6),
                Estado = dr.GetString(7)
            });
        }
        return lista;
    }

    // Cuenta los libros (no los prestamos) que el socio tiene sin devolver:
    // es el dato que usa la regla de "maximo 3 libros pendientes".
    public int ContarLibrosPendientes(int socioId)
    {
        const string sql = @"SELECT COUNT(1)
                             FROM DetallePrestamo D
                             INNER JOIN Prestamos P ON P.PrestamoId = D.PrestamoId
                             WHERE P.SocioId = @Id AND D.FechaDevolucion IS NULL";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", socioId);
        cn.Open();
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // -----------------------------------------------------------------
    // Transaccion del prestamo: cabecera + detalles + descuento de stock.
    // Si algo falla, el ROLLBACK deja la base de datos como estaba.
    // -----------------------------------------------------------------
    public int RegistrarPrestamo(Prestamo prestamo, List<int> libroIds)
    {
        using var cn = DbConnectionFactory.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        try
        {
            int prestamoId;

            // 1) Cabecera
            using (var cmd = new SqlCommand(
                @"INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
                  VALUES (@SocioId, @FechaPrestamo, @FechaLimite, 'Pendiente');
                  SELECT CAST(SCOPE_IDENTITY() AS INT);", cn, tx))
            {
                cmd.Parameters.AddWithValue("@SocioId", prestamo.SocioId);
                cmd.Parameters.AddWithValue("@FechaPrestamo", prestamo.FechaPrestamo.Date);
                cmd.Parameters.AddWithValue("@FechaLimite", prestamo.FechaLimite.Date);
                prestamoId = (int)cmd.ExecuteScalar();
            }

            // 2) Detalle: un INSERT por cada libro del prestamo
            using (var cmd = new SqlCommand(
                @"INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion)
                  VALUES (@PrestamoId, @LibroId, NULL)", cn, tx))
            {
                cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
                var pLibroId = cmd.Parameters.Add("@LibroId", System.Data.SqlDbType.Int);
                foreach (int libroId in libroIds)
                {
                    pLibroId.Value = libroId;
                    cmd.ExecuteNonQuery();
                }
            }

            // 3) Stock: el WHERE con Ejemplares > 0 evita quedar en negativo
            using (var cmd = new SqlCommand(
                @"UPDATE Libros SET Ejemplares = Ejemplares - 1
                  WHERE LibroId = @LibroId AND Ejemplares > 0", cn, tx))
            {
                var pLibroId = cmd.Parameters.Add("@LibroId", System.Data.SqlDbType.Int);
                foreach (int libroId in libroIds)
                {
                    pLibroId.Value = libroId;
                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"El libro {libroId} no tiene ejemplares disponibles.");
                }
            }

            tx.Commit();
            return prestamoId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // -----------------------------------------------------------------
    // Transaccion de la devolucion: fecha + ejemplar al stock + estado.
    // -----------------------------------------------------------------
    public void RegistrarDevolucion(int prestamoId, int libroId, DateTime fechaDevolucion)
    {
        using var cn = DbConnectionFactory.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        try
        {
            // 1) Fecha de devolucion del detalle
            using (var cmd = new SqlCommand(
                @"UPDATE DetallePrestamo
                  SET FechaDevolucion = @Fecha
                  WHERE PrestamoId = @PrestamoId AND LibroId = @LibroId AND FechaDevolucion IS NULL", cn, tx))
            {
                cmd.Parameters.AddWithValue("@Fecha", fechaDevolucion.Date);
                cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
                cmd.Parameters.AddWithValue("@LibroId", libroId);
                if (cmd.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException("Ese libro ya fue devuelto o no pertenece al prestamo.");
            }

            // 2) El ejemplar vuelve al stock
            using (var cmd = new SqlCommand(
                "UPDATE Libros SET Ejemplares = Ejemplares + 1 WHERE LibroId = @LibroId", cn, tx))
            {
                cmd.Parameters.AddWithValue("@LibroId", libroId);
                cmd.ExecuteNonQuery();
            }

            // 3) Si ya no quedan libros pendientes, el prestamo pasa a Devuelto
            using (var cmd = new SqlCommand(
                @"UPDATE Prestamos
                  SET Estado = 'Devuelto'
                  WHERE PrestamoId = @PrestamoId
                    AND NOT EXISTS (SELECT 1 FROM DetallePrestamo D
                                    WHERE D.PrestamoId = @PrestamoId AND D.FechaDevolucion IS NULL)", cn, tx))
            {
                cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static Prestamo LeerPrestamo(SqlDataReader dr) => new Prestamo
    {
        PrestamoId = dr.GetInt32(0),
        SocioId = dr.GetInt32(1),
        FechaPrestamo = dr.GetDateTime(2),
        FechaLimite = dr.GetDateTime(3),
        Estado = dr.GetString(4),
        NombreSocio = dr.GetString(5),
        DNI = dr.GetString(6)
    };
}
