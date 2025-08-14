// Filename: GameDataService.cs
using System.Collections.Generic;
using System.Linq;
using BarbarosKs.Shared.DTOs;

public class GameDataService : IGameService
{
    private readonly Dictionary<int, CannonballDto> _cannonballTypesByCode = new();

    /// <summary>
    /// API'den gelen verilerle kütüphaneyi doldurur.
    /// </summary>
    public void Initialize(List<CannonballDto> allCannonballTypes)
    {
        _cannonballTypesByCode.Clear();
        if (allCannonballTypes == null) return;
        
        foreach (var type in allCannonballTypes)
        {
            _cannonballTypesByCode[type.Code] = type;
        }
    }

    /// <summary>
    /// Code'a göre bir güllenin tüm API verilerini getirir.
    /// </summary>
    public CannonballDto GetCannonballStatsByCode(int code)
    {
        _cannonballTypesByCode.TryGetValue(code, out var stats);
        return stats; // Bulamazsa null döner.
    }
}