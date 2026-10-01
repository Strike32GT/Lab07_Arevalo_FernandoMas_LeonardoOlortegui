namespace Biblioteca.Entidades;

// Linea de un prestamo. FechaDevolucion == null significa que el libro sigue prestado.
public class DetallePrestamo
{
    public int PrestamoId { get; set; }
    public int LibroId { get; set; }
    public DateTime? FechaDevolucion { get; set; }

    // Titulo del libro: lo trae el JOIN de la capa de Datos para la grilla de devoluciones.
    public string Titulo { get; set; }
}
