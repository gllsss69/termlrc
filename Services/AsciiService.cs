using System;
using System.Diagnostics;
using System.IO;
using Figgle;
using Figgle.Fonts;

namespace termlrc.Services
{

public class AsciiService
{
    public readonly string Logo = """
  _                      _          
 | |                    | |         
 | |_ ___ _ __ _ __ ___ | |_ __ ___ 
 | __/ _ \ '__| '_ ` _ \| | '__/ __|
 | ||  __/ |  | | | | | | | | | (__ 
  \__\___|_|  |_| |_| |_|_|_|  \___|
""";

    private readonly (string name, Figgle.FiggleFont font)[] _fonts =
    {
        ("Standard",   FiggleFonts.Standard),
        ("Slant",      FiggleFonts.Slant),
        ("Doom",       FiggleFonts.Doom),
        ("Big",        FiggleFonts.Big),
        ("Colossal",   FiggleFonts.Colossal),
        ("Small",      FiggleFonts.Small),
        ("Graceful",   FiggleFonts.Graceful),
        ("Morse",      FiggleFonts.Morse),
        ("Rectangles", FiggleFonts.Rectangles),
        ("FourMax",    FiggleFonts.FourMax),
        ("Puffy",      FiggleFonts.Puffy)
    };

    private int _fontIndex = 0;

    public string CurrentFontName => _fonts[_fontIndex].name;

    public void NextFont()
    {
        _fontIndex = (_fontIndex + 1) % _fonts.Length;
    }

    public string ConvertToAsciiArt(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string rendered = _fonts[_fontIndex].font.Render(Transliterate(text));
            if (string.IsNullOrWhiteSpace(rendered)) return text + "\n";
            return rendered;
        }
        catch
        {
            return text + "\n";
        }
    }

    private string Transliterate(string text)
    {
        string[] cyr = { "а", "б", "в", "г", "д", "е", "є", "ж", "з", "и", "і", "ї", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ь", "ю", "я", "ы", "э", "ъ" };
        string[] lat = { "a", "b", "v", "g", "d", "e", "je", "zh", "z", "i", "i", "ji", "j", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "c", "ch", "sh", "shh", "'", "ju", "ja", "y", "e", "'" };

        for (int i = 0; i < cyr.Length; i++)
        {
            text = text.Replace(cyr[i], lat[i]).Replace(cyr[i].ToUpper(), lat[i].ToUpper());
        }
        return text;
    }

    public List<string> RenderAsciiWrapped(string text, int maxCharsPerLine = 20)
    {
        var lines = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return lines;

        string[] words = text.Split(' ');
        string currentLine = "";

        foreach (var word in words)
        {
            if ((currentLine + word).Length > maxCharsPerLine && !string.IsNullOrEmpty(currentLine))
            {
                lines.Add(ConvertToAsciiArt(currentLine.Trim()));
                currentLine = "";
            }
            currentLine += word + " ";
        }
        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(ConvertToAsciiArt(currentLine.Trim()));
        }

        return lines;
    }
}
}