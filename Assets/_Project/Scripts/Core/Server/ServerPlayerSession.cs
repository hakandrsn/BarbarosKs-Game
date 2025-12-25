using System;
using System.Collections.Generic;
using BarbarosKs.Shared.DTOs;

/// <summary>
/// Sunucuda RAM üzerinde yaşayan, oyuncunun anlık verilerini tutan sınıf.
/// NetworkVariable DEĞİLDİR. Mantıksal hesaplamalar burada yapılır.
/// </summary>
public class ServerPlayerSession
{
    public ulong ClientId { get; private set; } // Netcode client id
    public Guid CharacterId { get; private set; } // BD Ship Id
    public string PlayerName { get; private set; }

    // --- Değişken Oyun Verileri ---
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }

    public Dictionary<int, int> Inventory { get; private set; } = new Dictionary<int, int>();
    public bool IsDirty { get; set; } = false; // Veri değiştimi ? (API'ya gitmeli mi ?)
    public DateTime LastSaveTime { get; set; }

    public ServerPlayerSession(ulong clientId, Guid characterId, string playerName, int currentHealth, int maxHealth)
    {
        ClientId = clientId;
        CharacterId = characterId;
        PlayerName = playerName;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
    }

    public void SetInventory(IEnumerable<InventoryDto> inventoryDtos) 
    {
        Inventory.Clear();
        if (inventoryDtos == null) return;

        foreach (var item in inventoryDtos)
        {
            // Yeni DTO yapında 'ItemCode' veya 'Code' hangisiyse onu kullan.
            // Genelde: item.ItemCode ve item.Quantity
            if (Inventory.ContainsKey(item.ItemCode))
            {
                Inventory[item.ItemCode] = item.Quantity;
            }
            else
            {
                Inventory.Add(item.ItemCode, item.Quantity);
            }
        }
    }

    /// <summary>
    /// Sunucuda RAM üzerinde yaşayan, oyuncunun anlık verilerini tutan sınıf.
    /// NetworkVariable DEĞİLDİR. Mantıksal hesaplamalar burada yapılır.
    /// </summary>
    public bool TryConsumeAmmo(int cannonballCode, int amount = 1)
    {
        if (Inventory.TryGetValue(cannonballCode, out var count) && count >= amount)
        {
            Inventory[cannonballCode] -= amount;
            IsDirty = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Hasar al.
    /// </summary>
    public void ApplyDamage(int damage)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        IsDirty = true;
    }
}