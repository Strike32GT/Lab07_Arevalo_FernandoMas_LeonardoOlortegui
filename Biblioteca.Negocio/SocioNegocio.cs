using Biblioteca.Datos;
using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Capa de Negocio de los socios. Aqui viven las reglas:
//   1. el DNI no se repite y tiene 8 digitos,
//   2. el nombre es obligatorio y el email tiene formato valido,
//   3. no se puede dar de baja un socio con prestamos pendientes.
public class SocioNegocio
{
    private readonly ISocioRepositorio _socios;

    public SocioNegocio() : this(new SocioRepositorio()) { }

    public SocioNegocio(ISocioRepositorio socios)
    {
        _socios = socios;
    }

    // Busqueda por nombre o por DNI.
    public List<Socio> Listar(string busqueda, bool incluirInactivos) =>
        _socios.Listar(busqueda, incluirInactivos);

    public void Insertar(Socio socio)
    {
        Validar(socio);

        // Regla: DNI unico.
        if (_socios.ExisteDNI(socio.DNI, excluirSocioId: 0))
            throw new ReglaNegocioException($"El DNI {socio.DNI} ya esta registrado para otro socio.");

        _socios.Insertar(socio);
    }

    public void Actualizar(Socio socio)
    {
        if (socio.SocioId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un socio de la lista para actualizar.");

        Validar(socio);

        // Regla: DNI unico, excluyendo el propio socio que se esta editando.
        if (_socios.ExisteDNI(socio.DNI, excluirSocioId: socio.SocioId))
            throw new ReglaNegocioException($"El DNI {socio.DNI} ya esta registrado para otro socio.");

        _socios.Actualizar(socio);
    }

    // Eliminacion LOGICA: se llama DarDeBaja en Datos (Activo = 0), nunca DELETE.
    public void DarDeBaja(int socioId)
    {
        if (socioId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un socio de la lista.");

        var socio = _socios.ObtenerPorId(socioId);
        if (socio == null)
            throw new ReglaNegocioException("El socio ya no existe.");

        if (!socio.Activo)
            throw new ReglaNegocioException($"El socio {socio.Nombre} ya esta dado de baja.");

        // Regla: no se da de baja a un socio que aun tiene libros prestados.
        if (_socios.TienePrestamosPendientes(socioId))
            throw new ReglaNegocioException(
                $"No se puede dar de baja a {socio.Nombre} porque tiene prestamos pendientes.");

        _socios.DarDeBaja(socioId);
    }

    private static void Validar(Socio socio)
    {
        socio.Nombre = socio.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(socio.Nombre))
            throw new ReglaNegocioException("El nombre del socio es obligatorio.");

        if (socio.Nombre.Length > 100)
            throw new ReglaNegocioException("El nombre no puede superar los 100 caracteres.");

        socio.DNI = socio.DNI?.Trim();
        if (string.IsNullOrWhiteSpace(socio.DNI))
            throw new ReglaNegocioException("El DNI es obligatorio.");

        // Regla: el DNI peruano tiene 8 digitos numericos.
        if (socio.DNI.Length != 8 || !socio.DNI.All(char.IsDigit))
            throw new ReglaNegocioException("El DNI debe tener 8 digitos numericos.");

        if (!string.IsNullOrWhiteSpace(socio.Email))
        {
            socio.Email = socio.Email.Trim();
            if (socio.Email.Length > 100)
                throw new ReglaNegocioException("El email no puede superar los 100 caracteres.");

            // Regla: el email, si se escribe, debe tener un formato valido.
            if (!socio.Email.Contains('@') || socio.Email.StartsWith("@") || socio.Email.EndsWith("@"))
                throw new ReglaNegocioException("El email no tiene un formato valido.");
        }
    }
}
