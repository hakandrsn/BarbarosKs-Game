// Filename: GameDataService.cs

using System.Collections.Generic;
using System.Linq;
using BarbarosKs.Shared.DTOs;
using UnityEngine;

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

        foreach (var type in allCannonballTypes.Where(type => !_cannonballTypesByCode.ContainsKey(type.Code)))
        {
            _cannonballTypesByCode.Add(type.Code, type);
        }

        Debug.Log($"[GameDataService] {allCannonballTypes.Count} adet gülle tipi hafızaya alındı.");
    }

    /// <summary>
    /// Code'a göre bir güllenin tüm API verilerini getirir.
    /// </summary>
    public CannonballDto GetCannonballStatsByCode(int code)
    {
        if (_cannonballTypesByCode.TryGetValue(code, out var stats))
        {
            return stats;
        }

        Debug.LogWarning($"[GameDataService] Gülle kodu bulunamadı: {code}");
        return null;
    }
}