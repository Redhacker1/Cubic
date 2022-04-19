using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Cubic.Content;

public static class Localization
{
    public static Locale CurrentLocale;
    
    public static Dictionary<string, string> AvailableLocales;

    static Localization()
    {
        AvailableLocales = new Dictionary<string, string>();
    }

    public static void LoadLocale(string localeName)
    {
        CurrentLocale = JsonConvert.DeserializeObject<Locale>(File.ReadAllText(AvailableLocales[localeName], Encoding.UTF8));
    }

    public static void CreateLocale(string path)
    {
        Locale locale = new Locale();
        File.WriteAllText(path, JsonConvert.SerializeObject(locale, Formatting.Indented));
    }
}