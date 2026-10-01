using Biblioteca.Entidades;

namespace Biblioteca.Datos.Interfaces;

// Contrato de acceso a datos de los autores (solo lectura: la biblioteca
// no tiene un mantenimiento de autores en la interfaz).
public interface IAutorRepositorio
{
    List<Autor> Listar(bool incluirInactivos);
}
