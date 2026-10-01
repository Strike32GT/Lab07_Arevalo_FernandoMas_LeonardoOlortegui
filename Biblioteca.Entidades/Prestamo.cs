namespace Biblioteca.Entidades;

// Cabecera de un prestamo: un socio toma uno o varios libros.
public class Prestamo
{
    public int PrestamoId { get; set; }
    public int SocioId { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaLimite { get; set; }
    public string Estado { get; set; }

    // Datos que trae el JOIN de la capa de Datos para el formulario de devolucion.
    public string NombreSocio { get; set; }
    public string DNI { get; set; }
}
