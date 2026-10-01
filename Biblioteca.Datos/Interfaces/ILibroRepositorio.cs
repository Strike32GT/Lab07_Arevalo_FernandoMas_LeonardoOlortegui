using Biblioteca.Entidades;

namespace Biblioteca.Datos.Interfaces;

// Contrato de acceso a datos de los libros. Lo consume la capa de Negocio
// mediante inyeccion por constructor (punto extra del enunciado).
public interface ILibroRepositorio
{
    List<Libro> Listar(string busqueda, bool incluirInactivos);
    Libro ObtenerPorId(int libroId);
    bool ExisteISBN(string isbn, int excluirLibroId);
    bool TienePrestamosPendientes(int libroId);
    void Insertar(Libro libro);
    void Actualizar(Libro libro);
    void DarDeBaja(int libroId);
}
