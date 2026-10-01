using Biblioteca.Datos;
using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Resultado de una devolucion: el libro devuelto y la multa por retraso
// (calculada en Negocio, nunca guardada en la base de datos).
public class ResultadoDevolucion
{
    public decimal Multa { get; set; }
    public int DiasRetraso { get; set; }
    public bool Devuelto { get; set; }
}

// Capa de Negocio de los prestamos. Aqui viven las reglas:
//   1. maximo 3 libros pendientes por socio,
//   2. no se presta a socios dados de baja ni a libros dados de baja,
//   3. no se presta un libro sin ejemplares,
//   4. la multa por retraso es S/ 1.50 por dia.
public class PrestamoNegocio
{
    private readonly IPrestamoRepositorio _prestamos;
    private readonly ISocioRepositorio _socios;
    private readonly ILibroRepositorio _libros;

    public PrestamoNegocio()
        : this(new PrestamoRepositorio(), new SocioRepositorio(), new LibroRepositorio()) { }

    public PrestamoNegocio(IPrestamoRepositorio prestamos, ISocioRepositorio socios, ILibroRepositorio libros)
    {
        _prestamos = prestamos;
        _socios = socios;
        _libros = libros;
    }

    public List<Prestamo> ListarPendientes() => _prestamos.ListarPendientes();

    public List<DetallePrestamo> ListarDetalles(int prestamoId, bool soloPendientes = true) =>
        _prestamos.ListarDetalles(prestamoId, soloPendientes);

    public List<PrestamoReporte> ReportePorIntervalo(DateTime desde, DateTime hasta)
    {
        if (desde > hasta)
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la fecha final.");

        return _prestamos.ReportePorIntervalo(desde, hasta);
    }

    // Calcula la fecha limite con los dias de prestamo configurados.
    public DateTime CalcularFechaLimite(DateTime fechaPrestamo) =>
        fechaPrestamo.Date.AddDays(ParametrosNegocio.DiasPrestamo);

    // -----------------------------------------------------------------
    // REGLA 10: registrar un prestamo con uno o varios libros.
    // Valida todo ANTES de llamar a Datos; si algo falla, no se guarda nada.
    // -----------------------------------------------------------------
    public int Registrar(int socioId, List<int> libroIds, DateTime fechaPrestamo, int diasPrestamo)
    {
        if (socioId <= 0)
            throw new ReglaNegocioException("Debe seleccionar el socio que se lleva el prestamo.");

        if (libroIds == null || libroIds.Count == 0)
            throw new ReglaNegocioException("Debe agregar al menos un libro al prestamo.");

        if (diasPrestamo <= 0)
            throw new ReglaNegocioException("El numero de dias del prestamo debe ser mayor que cero.");

        // El mismo libro no puede repetirse dentro del mismo prestamo.
        if (libroIds.Distinct().Count() != libroIds.Count)
            throw new ReglaNegocioException("Hay libros repetidos en la lista del prestamo.");

        // Regla: no se presta a socios dados de baja.
        var socio = _socios.ObtenerPorId(socioId);
        if (socio == null)
            throw new ReglaNegocioException("El socio seleccionado no existe.");
        if (!socio.Activo)
            throw new ReglaNegocioException($"No se puede prestar a {socio.Nombre}: esta dado de baja.");

        // REGLA: maximo 3 libros pendientes (los que ya tiene + los nuevos).
        int yaPendientes = _prestamos.ContarLibrosPendientes(socioId);
        if (yaPendientes + libroIds.Count > ParametrosNegocio.MaximoLibrosPendientes)
            throw new ReglaNegocioException(
                $"{socio.Nombre} ya tiene {yaPendientes} libro(s) pendiente(s) y el limite es " +
                $"{ParametrosNegocio.MaximoLibrosPendientes}. No puede llevarse {libroIds.Count} libro(s) mas.");

        // Regla: cada libro debe existir, estar activo y tener ejemplares.
        foreach (int libroId in libroIds)
        {
            var libro = _libros.ObtenerPorId(libroId);
            if (libro == null)
                throw new ReglaNegocioException($"El libro seleccionado (id {libroId}) no existe.");

            if (!libro.Activo)
                throw new ReglaNegocioException($"No se puede prestar \"{libro.Titulo}\": esta dado de baja.");

            if (libro.Ejemplares <= 0)
                throw new ReglaNegocioException($"No hay ejemplares disponibles de \"{libro.Titulo}\".");
        }

        // Todo valido: recien aca se escribe. Datos lo guarda en una sola transaccion
        // (cabecera + detalles + descuento de ejemplares).
        var prestamo = new Prestamo
        {
            SocioId = socioId,
            FechaPrestamo = fechaPrestamo.Date,
            FechaLimite = fechaPrestamo.Date.AddDays(diasPrestamo),
            Estado = "Pendiente"
        };

        return _prestamos.RegistrarPrestamo(prestamo, libroIds);
    }

    // -----------------------------------------------------------------
    // REGLA 11: registrar la devolucion y calcular la multa por retraso.
    // -----------------------------------------------------------------
    public ResultadoDevolucion Devolver(int prestamoId, int libroId, DateTime fechaDevolucion)
    {
        if (prestamoId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un prestamo.");

        if (libroId <= 0)
            throw new ReglaNegocioException("Debe seleccionar el libro que se devuelve.");

        var prestamo = _prestamos.ObtenerPorId(prestamoId);
        if (prestamo == null)
            throw new ReglaNegocioException("El prestamo seleccionado no existe.");

        if (prestamo.Estado == "Devuelto")
            throw new ReglaNegocioException($"El prestamo {prestamoId} ya fue devuelto por completo.");

        // El libro debe ser una linea pendiente de ESTE prestamo.
        var detalle = _prestamos.ListarDetalles(prestamoId, soloPendientes: true)
                                .FirstOrDefault(d => d.LibroId == libroId);
        if (detalle == null)
            throw new ReglaNegocioException("Ese libro no esta pendiente en el prestamo seleccionado.");

        if (fechaDevolucion.Date < prestamo.FechaPrestamo.Date)
            throw new ReglaNegocioException("La fecha de devolucion no puede ser anterior al prestamo.");

        // REGLA 11: multa de S/ 1.50 por cada dia de retraso sobre la fecha limite.
        int diasRetraso = Math.Max(0, (fechaDevolucion.Date - prestamo.FechaLimite.Date).Days);
        decimal multa = diasRetraso * ParametrosNegocio.MultaPorDia;

        // Datos guarda la fecha, devuelve el ejemplar al stock y, si ya no queda
        // ningun pendiente, pasa el prestamo a Devuelto (dentro de una transaccion).
        _prestamos.RegistrarDevolucion(prestamoId, libroId, fechaDevolucion);

        return new ResultadoDevolucion
        {
            DiasRetraso = diasRetraso,
            Multa = multa,
            Devuelto = true
        };
    }

    // Calcula la multa sin registrarla: la vista de devolucion la muestra
    // como vista previa antes de confirmar.
    public ResultadoDevolucion CalcularMulta(int prestamoId, DateTime fechaDevolucion)
    {
        if (prestamoId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un prestamo.");

        var prestamo = _prestamos.ObtenerPorId(prestamoId);
        if (prestamo == null)
            throw new ReglaNegocioException("El prestamo seleccionado no existe.");

        int diasRetraso = Math.Max(0, (fechaDevolucion.Date - prestamo.FechaLimite.Date).Days);

        return new ResultadoDevolucion
        {
            DiasRetraso = diasRetraso,
            Multa = diasRetraso * ParametrosNegocio.MultaPorDia,
            Devuelto = false
        };
    }
}
