using System.Globalization;
using System.Text.RegularExpressions;
using CS2_Admin.Utils;
using SwiftlyS2.Shared;

namespace CS2_Admin.Services;

public static class LocalizerHelper
{
    // Pozisyonel placeholder tespiti: {0} {1} ... {9}
    private static readonly Regex PositionalPattern = new(@"\{\d+\}", RegexOptions.Compiled);

    // İsimli placeholder tespiti: {admin} {target} {damage} {duration} ...
    // (sadece harf/alt çizgi ile başlayan; {0} gibi sayısal olanları kapsamaz)
    private static readonly Regex NamedPattern = new(@"\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

    /// <summary>
    /// Bir çeviri metnini string.Format'a uygun pozisyonel ({0},{1},...) hale getirir.
    ///
    /// - Metin zaten pozisyonel placeholder ({0}) içeriyorsa olduğu gibi bırakılır
    ///   (geriye dönük uyumluluk; tr/en dosyaları bu stili kullanır).
    /// - Aksi halde isimli placeholder'lar ({admin}, {target}, {damage}, ...) METİNDE
    ///   İLK GÖRÜNME SIRASINA göre {0}, {1}, {2}, ... ile değiştirilir.
    ///
    /// Bu yaklaşım, kod tarafının argümanları cümledeki sırayla göndermesi sayesinde
    /// hem isimli (hu) hem pozisyonel (tr/en) mesajların güvenle çalışmasını sağlar ve
    /// "{damage} her zaman {2}" gibi kırılgan sabit eşlemelerin yarattığı hataları önler.
    /// </summary>
    private static string NormalizeToPositional(string format)
    {
        if (string.IsNullOrEmpty(format))
            return format;

        // Zaten pozisyonel ise dokunma.
        if (PositionalPattern.IsMatch(format))
            return format;

        if (!NamedPattern.IsMatch(format))
            return format;

        var indexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        return NamedPattern.Replace(format, match =>
        {
            var name = match.Groups["name"].Value;
            if (!indexByName.TryGetValue(name, out var index))
            {
                index = indexByName.Count;
                indexByName[name] = index;
            }
            return "{" + index + "}";
        });
    }

    public static string Get(ISwiftlyCore core, string key)
    {
        // Argümansız çağrı: metni olduğu gibi döndür (placeholder beklenmez).
        return PluginLocalizer.Get(core)[key];
    }

    public static string Get(ISwiftlyCore core, string key, params object[] args)
    {
        try
        {
            var raw = PluginLocalizer.Get(core)[key];
            var format = NormalizeToPositional(raw);
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
        catch
        {
            // Beklenmedik bir biçimlendirme hatasında Swiftly'nin varsayılan formatlayıcısına düş.
            return PluginLocalizer.Get(core)[key, args];
        }
    }

    public static string GetWithFallback(ISwiftlyCore core, string key, string fallback)
    {
        try
        {
            var val = PluginLocalizer.Get(core)[key];
            return string.Equals(val, key, StringComparison.OrdinalIgnoreCase) ? fallback : val;
        }
        catch
        {
            return fallback;
        }
    }

    public static string GetWithFallback(ISwiftlyCore core, string key, string fallback, params object[] args)
    {
        try
        {
            var raw = PluginLocalizer.Get(core)[key];
            if (string.Equals(raw, key, StringComparison.OrdinalIgnoreCase))
                return string.Format(CultureInfo.InvariantCulture, fallback, args);

            var format = NormalizeToPositional(raw);
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
        catch
        {
            return string.Format(fallback, args);
        }
    }
}
