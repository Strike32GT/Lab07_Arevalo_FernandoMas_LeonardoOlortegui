using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Capa de Datos para los libros. Solo SQL parametrizado: ninguna regla de negocio aqui.
public class LibroRepositorio : ILibroRepositorio
{
    // INNER JOIN con Autores para traer el nombre del autor en la misma consulta.
    private const string SelectBase =
        @"SELECT L.LibroId, L.Titulo, L.ISBN, L.AutorId, L.Ejemplares, L.Activo, A.Nombre
          FROM Libros L
          INNER JOIN Autores A ON A.AutorId = L.AutorId";

    // busqueda: vacio devuelve todos; si no, filtra por titulo o por nombre del autor.
    public List<Libro> Listar(string busqueda, bool incluirInactivos)
    {
        const string sql = SelectBase + @"
          WHERE (@Todos = 1 OR L.Activo = 1)
            AND (@Busqueda = '' OR L.Titulo LIKE @Patron OR A.Nombre LIKE @Patron)
          ORDER BY L.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Todos", incluirInactivos ? 1 : 0);
        cmd.Parameters.AddWithValue("@Busqueda", (busqueda ?? string.Empty).Trim());
        cmd.Parameters.AddWithValue("@Patron", "%" + (busqueda ?? string.Empty).Trim() + "%");
        cn.Open();
        return LeerTodos(cmd);
    }

    // Trae el libro aunque este dado de baja: lo necesita Negocio para validar.
    public Libro ObtenerPorId(int libroId)
    {
        const string sql = SelectBase + " WHERE L.LibroId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", libroId);
        cn.Open();

        var lista = LeerTodos(cmd);
        return lista.Count > 0 ? lista[0] : null;
    }

    // El ISBN es unico en la BD; esta consulta solo acorta el mensaje de error
    // para que WPF muestre un aviso de negocio y no una SqlException.
    public bool ExisteISBN(string isbn, int excluirLibroId)
    {
        const string sql = "SELECT COUNT(1) FROM Libros WHERE ISBN = @ISBN AND LibroId <> @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@ISBN", isbn);
        cmd.Parameters.AddWithValue("@Id", excluirLibroId);
        cn.Open();
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    // Un libro con prestamos pendientes (detalle sin FechaDevolucion) no se puede dar de baja.
    public bool TienePrestamosPendientes(int libroId)
    {
        const string sql = @"SELECT COUNT(1)
                             FROM DetallePrestamo D
                             INNER JOIN Prestamos P ON P.PrestamoId = D.PrestamoId
                             WHERE D.LibroId = @Id AND D.FechaDevolucion IS NULL";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", libroId);
        cn.Open();
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public void Insertar(Libro libro)
    {
        const string sql = @"INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares, Activo)
                             VALUES (@Titulo, @ISBN, @AutorId, @Ejemplares, @Activo)";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, libro);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    public void Actualizar(Libro libro)
    {
        const string sql = @"UPDATE Libros
                             SET Titulo = @Titulo, ISBN = @ISBN, AutorId = @AutorId,
                                 Ejemplares = @Ejemplares, Activo = @Activo
                             WHERE LibroId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, libro);
        cmd.Parameters.AddWithValue("@Id", libro.LibroId);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    // Eliminacion logica: Activo = 0. Nunca un DELETE fisico.
    public void DarDeBaja(int libroId)
    {
        const string sql = "UPDATE Libros SET Activo = 0 WHERE LibroId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", libroId);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    private static void AgregarParametros(SqlCommand cmd, Libro libro)
    {
        cmd.Parameters.AddWithValue("@Titulo", libro.Titulo);
        cmd.Parameters.AddWithValue("@ISBN", libro.ISBN);
        cmd.Parameters.AddWithValue("@AutorId", libro.AutorId);
        cmd.Parameters.AddWithValue("@Ejemplares", libro.Ejemplares);
        cmd.Parameters.AddWithValue("@Activo", libro.Activo);
    }

    private static List<Libro> LeerTodos(SqlCommand cmd)
    {
        var lista = new List<Libro>();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            lista.Add(new Libro
            {
                LibroId = dr.GetInt32(0),
                Titulo = dr.GetString(1),
                ISBN = dr.GetString(2),
                AutorId = dr.GetInt32(3),
                Ejemplares = dr.GetInt32(4),
                Activo = dr.GetBoolean(5),
                NombreAutor = dr.GetString(6)
            });
        }
        return lista;
    }
}
