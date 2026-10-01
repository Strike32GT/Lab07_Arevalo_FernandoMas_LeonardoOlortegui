namespace Biblioteca.Entidades;

// Fila del reporte de prestamos por intervalo de fechas.
// Se arma con INNER JOIN entre Prestamos, DetallePrestamo, Libros y Socios.
public class PrestamoReporte
{
    public int PrestamoId { get; set; }
    public string Socio { get; set; }
    public string DNI { get; set; }
    public string Libro { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaLimite { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string Estado { get; set; }
}
