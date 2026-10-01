namespace Biblioteca.Entidades;

// Capa de Entidades: solo propiedades, sin acceso a datos ni reglas de negocio.
public class Libro
{
    public int LibroId { get; set; }
    public string Titulo { get; set; }
    public string ISBN { get; set; }
    public int AutorId { get; set; }
    public int Ejemplares { get; set; }
    public bool Activo { get; set; }

    // Nombre del autor: lo trae el JOIN de la capa de Datos para que el
    // formulario y la grilla no tengan que consultar Autores por separado.
    public string NombreAutor { get; set; }
}
