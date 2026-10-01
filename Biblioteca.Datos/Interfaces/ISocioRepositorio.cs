using Biblioteca.Entidades;

namespace Biblioteca.Datos.Interfaces;

// Contrato de acceso a datos de los socios.
public interface ISocioRepositorio
{
    List<Socio> Listar(string busqueda, bool incluirInactivos);
    Socio ObtenerPorId(int socioId);
    bool ExisteDNI(string dni, int excluirSocioId);
    bool TienePrestamosPendientes(int socioId);
    void Insertar(Socio socio);
    void Actualizar(Socio socio);
    void DarDeBaja(int socioId);
}
