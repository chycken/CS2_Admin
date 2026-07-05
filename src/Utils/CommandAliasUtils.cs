namespace CS2_Admin.Utils;

public static class CommandAliasUtils
{
    public static string[] NormalizeCommandArgs(string[] args, IReadOnlyList<string> aliases)
    {
        var normalized = args
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .ToList();

        if (normalized.Count == 0)
        {
            return [];
        }

        var first = normalized[0].TrimStart('!', '/');
        if (IsAlias(first, aliases))
        {
            normalized.RemoveAt(0);
        }

        return [.. normalized];
    }

    // Menüler komutları admin.ExecuteCommand("sw_<ad>") ile çalıştırır. Konsol komutu olarak
    // SADECE RegisterCommands'taki sabit (kanonik) İngilizce adların "sw_" hâli kayıtlıdır
    // (örn. "slap" -> "sw_slap"). Config alias'ları (ve EnsureInternalMenuAliases'in eklediği
    // "cs2a_*" adları) konsol komutu olarak ASLA kayıt edilmez; bu yüzden çalıştırmada config'e
    // bakmak "sw_cs2a_slap" gibi var olmayan bir komut üretip menüyü tamamen bozuyordu.
    // Bu nedenle çalıştırma adını her zaman kanonik fallback'ten türetiyoruz.
    public static string GetPreferredExecutionAlias(IReadOnlyList<string> aliases, string fallback)
    {
        return ToSwAlias(fallback);
    }

    public static string ToSwAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return string.Empty;
        }

        var trimmed = alias.Trim();
        return trimmed.StartsWith("sw_", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"sw_{trimmed}";
    }

    private static bool IsAlias(string value, IReadOnlyList<string> aliases)
    {
        foreach (var alias in aliases.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()))
        {
            if (alias.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var swAlias = ToSwAlias(alias);
            if (swAlias.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
