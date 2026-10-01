using Biblioteca.Datos;
using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Capa de Negocio de los libros. Aqui viven las reglas:
//   1. el ISBN no se repite,
//   2. el titulo es obligatorio y el stock no puede ser negativo,
//   3. no se puede dar de baja un libro con prestamos pendientes.
// No abre conexiones ni escribe SQL: solo valida y despues llama a Datos.
public class LibroNegocio
{
    private readonly ILibroRepositorio _libros;
    private readonly IAutorRepositorio _autores;

    // Constructor por defecto: usa las implementaciones con SQL Server.
    public LibroNegocio() : this(new LibroRepositorio(), new AutorRepositorio()) { }

    // Punto extra del enunciado: se recibe el repositorio por constructor,
    // asi la clase se puede probar sin base de datos.
    public LibroNegocio(ILibroRepositorio libros, IAutorRepositorio autores)
    {
        _libros = libros;
        _autores = autores;
    }

    // Busqueda por titulo o por nombre del autor (la consulta usa el JOIN con Autores).
    public List<Libro> Listar(string busqueda, bool incluirInactivos) =>
        _libros.Listar(busqueda, incluirInactivos);

    // Solo autores activos: es lo unico que se puede asignar a un libro nuevo.
    public List<Autor> ListarAutores() => _autores.Listar(incluirInactivos: false);

    public void Insertar(Libro libro)
    {
        Validar(libro);

        // Regla: ISBN unico.
        if (_libros.ExisteISBN(libro.ISBN, excluirLibroId: 0))
            throw new ReglaNegocioException($"El ISBN {libro.ISBN} ya esta registrado en otro libro.");

        _libros.Insertar(libro);
    }

    public void Actualizar(Libro libro)
    {
        if (libro.LibroId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un libro de la lista para actualizar.");

        Validar(libro);

        // Regla: ISBN unico, excluyendo el propio libro que se esta editando.
        if (_libros.ExisteISBN(libro.ISBN, excluirLibroId: libro.LibroId))
            throw new ReglaNegocioException($"El ISBN {libro.ISBN} ya esta registrado en otro libro.");

        _libros.Actualizar(libro);
    }

    // Eliminacion LOGICA: se llama DarDeBaja en Datos (Activo = 0), nunca DELETE.
    public void DarDeBaja(int libroId)
    {
        if (libroId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un libro de la lista.");

        var libro = _libros.ObtenerPorId(libroId);
        if (libro == null)
            throw new ReglaNegocioException("El libro ya no existe.");

        if (!libro.Activo)
            throw new ReglaNegocioException($"El libro {libro.Titulo} ya esta dado de baja.");

        // Regla: no se da de baja un libro que aun esta prestado.
        if (_libros.TienePrestamosPendientes(libroId))
            throw new ReglaNegocioException(
                $"No se puede dar de baja \"{libro.Titulo}\" porque tiene prestamos pendientes.");

        _libros.DarDeBaja(libroId);
    }

    private void Validar(Libro libro)
    {
        libro.Titulo = libro.Titulo?.Trim();
        if (string.IsNullOrWhiteSpace(libro.Titulo))
            throw new ReglaNegocioException("El titulo del libro es obligatorio.");

        if (libro.Titulo.Length > 150)
            throw new ReglaNegocioException("El titulo no puede superar los 150 caracteres.");

        libro.ISBN = libro.ISBN?.Trim();
        if (string.IsNullOrWhiteSpace(libro.ISBN))
            throw new ReglaNegocioException("El ISBN es obligatorio.");

        if (libro.ISBN.Length > 20)
            throw new ReglaNegocioException("El ISBN no puede superar los 20 caracteres.");

        // Regla: el autor debe existir y estar activo.
        if (libro.AutorId <= 0)
            throw new ReglaNegocioException("Debe seleccionar el autor del libro.");

        var autor = _autores.Listar(incluirInactivos: false).FirstOrDefault(a => a.AutorId == libro.AutorId);
        if (autor == null)
            throw new ReglaNegocioException("El autor seleccionado no existe o esta dado de baja.");

        // Regla: no se admiten stocks negativos.
        if (libro.Ejemplares < 0)
            throw new ReglaNegocioException("El numero de ejemplares no puede ser negativo.");
    }
}
