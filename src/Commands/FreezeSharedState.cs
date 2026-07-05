namespace CS2_Admin.Commands;

/// <summary>
/// Freeze/Unfreeze komutlarının ORTAK durumu. Önceden her iki komut kendi kopya
/// alanlarını tutuyordu; !unfreeze, FreezeCommand'ın setine değil kendi boş setine
/// baktığı için görsel pulse durmuyor ve viewmodel asla eski haline dönmüyordu.
/// </summary>
internal static class FreezeSharedState
{
    internal static readonly HashSet<int> FrozenPlayers = new();
    internal static readonly HashSet<int> VisualPlayers = new();
    internal static readonly Dictionary<int, float> OriginalViewmodelFov = new();
    internal static readonly Dictionary<int, (float X, float Y, float Z)> OriginalViewmodelOffsets = new();
}
