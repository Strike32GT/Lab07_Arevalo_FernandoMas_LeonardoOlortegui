namespace Biblioteca.Negocio;

// Excepcion propia de la capa de Negocio: es la UNICA excepcion que
// la capa WPF conoce. La presentacion nunca captura SqlException.
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}
