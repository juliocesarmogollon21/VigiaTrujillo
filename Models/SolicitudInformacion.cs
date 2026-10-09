using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VigiaTrujillo.Models;

// Pedido de información que hace el Supervisor al Personal Municipal sobre una incidencia.
// Guarda la pregunta, la respuesta, quién respondió y cuándo.
public class SolicitudInformacion
{
    public const string EstadoPendiente = "Pendiente";
    public const string EstadoRespondida = "Respondida";
    public const string EstadoSinRespuesta = "Cerrada sin respuesta";

    public int Id { get; set; }

    public int IncidenciaId { get; set; }
    [ForeignKey(nameof(IncidenciaId))]
    public Incidencia? Incidencia { get; set; }

    [Required, StringLength(1000), Display(Name = "Información solicitada")]
    public string Pregunta { get; set; } = string.Empty;

    [Required, StringLength(50), Display(Name = "Solicitado por")]
    public string SolicitadoPor { get; set; } = string.Empty;

    [Display(Name = "Fecha de solicitud")]
    public DateTime FechaSolicitud { get; set; } = DateTime.Now;

    [StringLength(2000), Display(Name = "Respuesta")]
    public string? Respuesta { get; set; }

    [StringLength(50), Display(Name = "Respondido por")]
    public string? RespondidoPor { get; set; }

    [Display(Name = "Fecha de respuesta")]
    public DateTime? FechaRespuesta { get; set; }

    [StringLength(300)]
    public string? ArchivoRuta { get; set; }

    [StringLength(200)]
    public string? ArchivoNombre { get; set; }

    [Required, StringLength(30)]
    public string Estado { get; set; } = EstadoPendiente;

    // Se pone en true cuando el supervisor abre la incidencia y ya vio la respuesta.
    public bool RespuestaVista { get; set; }

    [NotMapped]
    public bool EstaPendiente => Estado == EstadoPendiente;
}
