namespace Biblioteca.Entidades;

// Capa de Entidades: solo propiedades, sin acceso a datos ni reglas de negocio.
public class Autor
{
    public int AutorId { get; set; }
    public string Nombre { get; set; }
    public string Nacionalidad { get; set; }
    public bool Activo { get; set; }
}
