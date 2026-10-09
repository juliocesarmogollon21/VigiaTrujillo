namespace VigiaTrujillo.Models;

// Configuración del filtro de contenido. Los valores se leen de la sección
// "FiltroContenido" del appsettings.json, así la lista se puede ampliar sin tocar código.
public class FiltroContenidoOptions
{
    // Palabras o frases que no se aceptan en los textos del ciudadano.
    public List<string> PalabrasNoPermitidas { get; set; } = new();

    // Frases típicas de spam (publicidad, apuestas, etc.).
    public List<string> FrasesSpam { get; set; } = new();

    // Palabras muy comunes que no cuentan para la regla de "palabra dominante".
    public List<string> PalabrasComunes { get; set; } = new();

    // Mínimo de palabras con sentido que debe tener la descripción.
    public int MinimoPalabras { get; set; } = 4;

    // Una misma palabra no puede aparecer seguida estas veces o más ("bache bache").
    public int MaximoRepeticionSeguida { get; set; } = 2;

    // Una palabra (que no sea común) domina el texto si aparece al menos MinimoApariciones
    // veces y además es ProporcionMaxima o más de las palabras con contenido (0.40 = 40 %).
    public int MinimoApariciones { get; set; } = 3;
    public double ProporcionMaxima { get; set; } = 0.40;

    // Si el texto tiene muchas palabras pero casi todas son las mismas.
    public double DiversidadMinima { get; set; } = 0.35;

    // Una letra o signo repetido seguido estas veces o más se considera relleno ("aaaa", "!!!!").
    public int MaximoCaracterRepetido { get; set; } = 4;

    // Si es false, no se aceptan enlaces web en la descripción.
    public bool PermitirEnlaces { get; set; } = false;
}
