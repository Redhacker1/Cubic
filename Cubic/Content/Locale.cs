using System.Collections.Generic;

namespace Cubic.Content;

/// <summary>
/// A locale contains a dictionary of strings that can be loaded and accessed at any point.
/// Useful for allowing support for multiple languages.
/// </summary>
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