using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using VigiaTrujillo.Models;
using VigiaTrujillo.Services.Interfaces;

namespace VigiaTrujillo.Services;

// Filtro que revisa lo que escribe el ciudadano antes de guardar la incidencia.
// Revisa tres cosas: palabras no permitidas (insultos, groserías, spam),
// palabras repetidas y relleno de caracteres ("aaaa", "!!!!").
public class FiltroContenidoService : IFiltroContenidoService
{
    // Números o símbolos que se usan para disfrazar letras (t0nt0, 1d10t4, c4r4j0).
    private static readonly Dictionary<char, char> LetrasDisfrazadas = new()
    {
        ['4'] = 'a', ['@'] = 'a', ['3'] = 'e', ['1'] = 'i', ['0'] = 'o',
        ['5'] = 's', ['$'] = 's', ['7'] = 't'
    };

    private const string SufijosPermitidos = "(?:s|es|azo|aza|azos|azas|ote|ota|otes|otas|ito|ita|itos|itas)?";

    private readonly FiltroContenidoOptions _opciones;
    private readonly List<Regex> _palabrasNoPermitidas;
    private readonly List<Regex> _frasesSpam;
    private readonly HashSet<string> _palabrasComunes;

    private static readonly Regex RegexPalabra = new(@"\p{L}+", RegexOptions.Compiled);
    private static readonly Regex RegexEnlace = new(@"https?://|www\.|\b[a-z0-9\-]+\.(?:com|net|org|xyz|info|ly|io|biz)\b", RegexOptions.Compiled);
    private static readonly Regex RegexSilabaRepetida = new(@"(\p{L}{2,4})\1{2,}", RegexOptions.Compiled);

    public FiltroContenidoService(IOptions<FiltroContenidoOptions> opciones)
    {
        _opciones = opciones.Value;
        _palabrasNoPermitidas = _opciones.PalabrasNoPermitidas
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => CrearPatron(p, conSufijos: true))
            .ToList();
        _frasesSpam = _opciones.FrasesSpam
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => CrearPatron(p, conSufijos: false))
            .ToList();
        _palabrasComunes = _opciones.PalabrasComunes
            .Select(QuitarTildes)
            .ToHashSet();
    }

    public IReadOnlyList<string> Validar(string? texto, string nombreCampo = "La descripción")
    {
        var errores = new List<string>();
        if (string.IsNullOrWhiteSpace(texto)) return errores; // de esto se encarga [Required]

        var minusculas = texto.ToLowerInvariant();
        var sinTildes = QuitarTildes(minusculas);

        // 1) Palabras no permitidas (también con letras disfrazadas o separadas)
        var paraBuscar = UnirLetrasSeparadas(ReemplazarLetrasDisfrazadas(sinTildes));
        foreach (var patron in _palabrasNoPermitidas)
        {
            var m = patron.Match(paraBuscar);
            if (m.Success)
            {
                errores.Add($"{nombreCampo} contiene lenguaje ofensivo o no permitido («{Enmascarar(m.Value)}»). Redacta tu reporte con respeto.");
                break;
            }
        }

        // 2) Spam y enlaces
        if (_frasesSpam.Any(p => p.IsMatch(paraBuscar)))
            errores.Add($"{nombreCampo} parece publicidad o spam. Describe solo lo que observaste en la obra.");
        if (!_opciones.PermitirEnlaces && RegexEnlace.IsMatch(sinTildes))
            errores.Add($"{nombreCampo} no puede contener enlaces web. Si tienes fotos o documentos, adjúntalos como evidencia.");

        // 3) Relleno de caracteres: "aaaa", "!!!!", "jajaja"
        var n = Math.Max(2, _opciones.MaximoCaracterRepetido);
        var letraRepetida = Regex.Match(sinTildes, $@"(\p{{L}})\1{{{n - 1},}}");
        if (letraRepetida.Success)
            errores.Add($"{nombreCampo} tiene letras repetidas sin sentido («{Recortar(letraRepetida.Value)}»). Escribe las palabras de forma normal.");

        var signoRepetido = Regex.Match(texto, $@"([^\p{{L}}\p{{N}}\s])\1{{{n - 1},}}");
        if (signoRepetido.Success)
            errores.Add($"{nombreCampo} tiene signos repetidos («{Recortar(signoRepetido.Value)}»). Usa la puntuación normal.");

        var silaba = RegexSilabaRepetida.Match(sinTildes);
        if (silaba.Success && !letraRepetida.Success)
            errores.Add($"{nombreCampo} tiene sílabas repetidas sin sentido («{Recortar(silaba.Value)}»).");

        // 4) Análisis de palabras
        var palabras = RegexPalabra.Matches(sinTildes).Select(m => m.Value).Where(p => p.Length >= 2).ToList();

        if (palabras.Count < _opciones.MinimoPalabras)
        {
            errores.Add($"{nombreCampo} es muy corta. Usa al menos {_opciones.MinimoPalabras} palabras para explicar lo que pasa en la obra.");
            return errores.Distinct().ToList();
        }

        // 4a) La misma palabra seguida ("bache bache")
        var seguidas = 1;
        for (var i = 1; i < palabras.Count; i++)
        {
            seguidas = palabras[i] == palabras[i - 1] ? seguidas + 1 : 1;
            if (seguidas >= _opciones.MaximoRepeticionSeguida)
            {
                errores.Add($"{nombreCampo} repite la palabra «{palabras[i]}» varias veces seguidas.");
                break;
            }
        }

        // 4b) Una palabra que domina el texto. Se cuenta solo entre las palabras "con contenido"
        //     (se dejan fuera artículos, preposiciones y otras palabras comunes).
        var conContenido = palabras.Where(p => p.Length >= 3 && !_palabrasComunes.Contains(p)).ToList();
        var dominante = conContenido
            .GroupBy(p => p)
            .Select(g => new { Palabra = g.Key, Veces = g.Count() })
            .OrderByDescending(x => x.Veces)
            .FirstOrDefault();
        if (dominante != null
            && dominante.Veces >= _opciones.MinimoApariciones
            && (double)dominante.Veces / conContenido.Count >= _opciones.ProporcionMaxima)
        {
            errores.Add($"{nombreCampo} usa demasiadas veces la palabra «{dominante.Palabra}» ({dominante.Veces} veces). Explica el problema con más detalle.");
        }
        else if (palabras.Count >= 8 && (double)palabras.Distinct().Count() / palabras.Count < _opciones.DiversidadMinima)
        {
            errores.Add($"{nombreCampo} repite casi siempre las mismas palabras. Explica el problema con más detalle.");
        }

        // 4c) Texto sin sentido, por ejemplo "asdfgh" (palabra larga sin vocales)
        var sinVocales = palabras.FirstOrDefault(p => p.Length >= 6 && !p.Any(c => "aeiouy".Contains(c)));
        if (sinVocales != null)
            errores.Add($"{nombreCampo} contiene texto sin sentido («{Recortar(sinVocales)}»).");

        return errores.Distinct().ToList();
    }

    // Arma la expresión regular de una palabra prohibida:
    // - cada letra puede repetirse ("tontoooo")
    // - entre palabras de una frase puede haber cualquier separador
    // - se busca como palabra completa para no bloquear palabras normales
    private static Regex CrearPatron(string entrada, bool conSufijos)
    {
        var partes = QuitarTildes(entrada.Trim().ToLowerInvariant())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(PatronDePalabra);
        var cuerpo = string.Join(@"[^\p{L}]*", partes);
        var sufijo = conSufijos ? SufijosPermitidos : string.Empty;
        return new Regex($@"(?<!\p{{L}}){cuerpo}{sufijo}(?!\p{{L}})", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private static string PatronDePalabra(string palabra)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i < palabra.Length)
        {
            var c = palabra[i];
            var largo = 1;
            while (i + largo < palabra.Length && palabra[i + largo] == c) largo++;
            var letra = Regex.Escape(c.ToString());
            sb.Append(largo == 1 ? $"{letra}+" : $"{letra}{{{largo},}}");
            i += largo;
        }
        return sb.ToString();
    }

    // Cambia números por letras solo dentro de palabras que mezclan letras y números (t0nt0 -> tonto).
    private static string ReemplazarLetrasDisfrazadas(string texto) =>
        Regex.Replace(texto, @"[\p{L}\d@$]+", m =>
        {
            var token = m.Value;
            if (!token.Any(char.IsLetter) || !token.Any(c => LetrasDisfrazadas.ContainsKey(c))) return token;
            return new string(token.Select(c => LetrasDisfrazadas.TryGetValue(c, out var l) ? l : c).ToArray());
        });

    // Junta letras sueltas separadas por puntos, guiones o espacios (t.o.n.t.o / t o n t o -> tonto).
    private static string UnirLetrasSeparadas(string texto)
    {
        texto = Regex.Replace(texto, @"(?<!\p{L})(?:\p{L}[\.\-_\*\+,/\\|~]+){2,}\p{L}(?!\p{L})",
            m => new string(m.Value.Where(char.IsLetter).ToArray()));
        texto = Regex.Replace(texto, @"(?<!\p{L})(?:\p{L}\s+){3,}\p{L}(?!\p{L})",
            m => new string(m.Value.Where(char.IsLetter).ToArray()));
        return texto;
    }

    private static string QuitarTildes(string texto)
    {
        var formD = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // No mostramos la palabra ofensiva completa: "idiota" -> "i****a".
    private static string Enmascarar(string palabra)
    {
        var limpia = new string(palabra.Where(char.IsLetter).ToArray());
        if (limpia.Length <= 2) return new string('*', limpia.Length);
        return limpia[0] + new string('*', Math.Min(limpia.Length - 2, 8)) + limpia[^1];
    }

    private static string Recortar(string valor) => valor.Length > 12 ? valor[..12] + "…" : valor;
}
