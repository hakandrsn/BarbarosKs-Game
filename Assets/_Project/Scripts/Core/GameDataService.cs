// Filename: GameDataService.cs
using System.Collections.Generic;
using System.Linq;
using BarbarosKs.Shared.DTOs;

public class GameDataService : IGameService
{
    private readonly Dictionary<int, CannonballTypeDto> _cannonballTypesByCode = new();

    /// <summary>
    /// API'den gelen verilerle kütüphaneyi doldurur.
    /// </summary>
    public void Initialize(List<CannonballTypeDto> allCannonballTypes)
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
    public CannonballTypeDto GetCannonballStatsByCode(int code)
    {
        _cannonballTypesByCode.TryGetValue(code, out var stats);
        return stats; // Bulamazsa null döner.
    }
}