using Biblioteca.Entidades;

namespace Biblioteca.Datos.Interfaces;

// Contrato de acceso a datos de los prestamos y sus lineas de detalle.
// Los dos metodos que escriben (RegistrarPrestamo y RegistrarDevolucion) son
// transaccionales: cabecera, detalles y stock se guardan juntos o no se guarda nada.
public interface IPrestamoRepositorio
{
    List<Prestamo> ListarPendientes();
    Prestamo ObtenerPorId(int prestamoId);
    List<DetallePrestamo> ListarDetalles(int prestamoId, bool soloPendientes);
    List<PrestamoReporte> ReportePorIntervalo(DateTime desde, DateTime hasta);
    int ContarLibrosPendientes(int socioId);

    // Una sola transaccion: cabecera + detalle + descuento de ejemplares.
    // Devuelve el PrestamoId generado.
    int RegistrarPrestamo(Prestamo prestamo, List<int> libroIds);

    // Una sola transaccion: fecha de devolucion + devolucion del ejemplar al stock
    // + actualizacion del estado del prestamo cuando ya no quedan pendientes.
    void RegistrarDevolucion(int prestamoId, int libroId, DateTime fechaDevolucion);
}
