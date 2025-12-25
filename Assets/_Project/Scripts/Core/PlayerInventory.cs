// Filename: PlayerInventory.cs (Client-Side Inventory Cache)
using System.Collections.Generic;
using BarbarosKs.Shared.DTOs; // DLL'inizdeki DTO'ları kullanın
using UnityEngine;

public class PlayerInventory : IGameService
{
    // Hangi gülle kodundan kaç tane var?
    private readonly Dictionary<int, int> _cannonballQuantities = new Dictionary<int, int>();
    public IReadOnlyDictionary<int, int> CannonballQuantities => _cannonballQuantities;

    // API'den gelen envanter listesiyle bu servisi doldururuz.
    public void UpdateInventory(List<ShipCannonballInventoryDto> inventoryItems)
    {
        _cannonballQuantities.Clear();
        foreach (var item in inventoryItems)
        {
            _cannonballQuantities[item.CannonballCode] = item.Quantity; // CannonballTypeId code ile değiştir.
        }
        Debug.Log("Client gülle envanteri güncellendi.");
    }
    
    // Bu metotları client-taraflı UI güncellemeleri için kullanabiliriz.
    public int GetQuantity(int code) => _cannonballQuantities.TryGetValue(code, out int quantity) ? quantity : 0;
    
    public void SetQuantity(int code, int quantity)
    {
        if (_cannonballQuantities.ContainsKey(code))
        {
            _cannonballQuantities[code] = quantity;
        }
        else
        {
            _cannonballQuantities.Add(code, quantity);
        }
    
        // UI'ın güncellenmesi için bir event tetiklemek iyi olurdu ama
        // şimdilik CannonballUIController her açılışta yeniliyor.
        // Eğer anlık UI (HUD'da gülle sayısı) varsa, burada event fırlatmalısın.
    }
}