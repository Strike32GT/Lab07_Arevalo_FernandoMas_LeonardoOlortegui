namespace Biblioteca.Entidades;

// Capa de Entidades: solo propiedades, sin acceso a datos ni reglas de negocio.
public class Socio
{
    public int SocioId { get; set; }
    public string DNI { get; set; }
    public string Nombre { get; set; }
    public string Email { get; set; }
    public bool Activo { get; set; }

    // Cantidad de libros que el socio tiene pendientes de devolucion.
    // La calcula la capa de Negocio contando los DetallePrestamo sin devolver.
    public int LibrosPendientes { get; set; }
}
