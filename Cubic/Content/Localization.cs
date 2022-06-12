using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Cubic.Content;

/// <summary>
/// Localization tools.
/// </summary>
public static class Localization
{
    /// <summary>
    /// The currently loaded locale. Use <see cref="LoadLocale"/> to load from a file, or load programmatically by
    /// setting this value.
    /// </summary>
    public static Locale CurrentLocale;
    
    /// <summary>
    /// A dictionary of available locales. Use <see cref="GetAllLocales"/> to fill this automatically, or programmatically
    /// add locales.
    /// </summary>
    public static readonly Dictionary<string, string> AvailableLocales;

    static Localization()
    {
        AvailableLocales = new Dictionary<string, string>();
    }

    /// <summary>
    /// Load a locale with the given name.
    /// </summary>
    /// <param name="localeName">The locale name.</param>
    public static void LoadLocale(string localeName)
    {
        CurrentLocale = JsonConvert.DeserializeObject<Locale>(File.ReadAllText(AvailableLocales[localeName], Encoding.UTF8));
    }

    /// <summary>
    /// Create a locale with template.
    /// </summary>
    /// <param name="path">The path to export this locale to.</param>
    public static void CreateLocale(string path)
    {
        Locale locale = new Locale();
        File.WriteAllText(path, JsonConvert.SerializeObject(locale, Formatting.Indented));
    }

    /// <summary>
    /// Get all locales from the given directory, with the given file extension.
    /// </summary>
    /// <param name="directory">The directory which contains the locales.</param>
    /// <param name="fileExtension">The file extension to query.</param>
    public static void GetAllLocales(string directory, string fileExtension = ".locale")
    {
        string[] paths = Directory.GetFiles(directory,
            $"*{(fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension)}");
        foreach (string path in paths)
        {
            Locale locale = JsonConvert.DeserializeObject<Locale>(File.ReadAllText(path));
            AvailableLocales.TryAdd(locale.Language, path);
        }
    }
}