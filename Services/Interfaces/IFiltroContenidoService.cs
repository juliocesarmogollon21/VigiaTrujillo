namespace VigiaTrujillo.Services.Interfaces;

public interface IFiltroContenidoService
{
    // Revisa el texto escrito por el ciudadano y devuelve la lista de problemas encontrados.
    // Si la lista está vacía, el texto es aceptado.
    IReadOnlyList<string> Validar(string? texto, string nombreCampo = "La descripción");
}
