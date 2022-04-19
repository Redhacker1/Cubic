using System.Collections.Generic;

namespace Cubic.Content;

public class Locale
{
    public readonly string Language;

    public readonly Dictionary<string, string> Strings;

    public Locale()
    {
        Language = "LANGUAGE";
        Strings = new Dictionary<string, string>()
        {
            { "StringName", "Enter something here." }
        };
    }
}