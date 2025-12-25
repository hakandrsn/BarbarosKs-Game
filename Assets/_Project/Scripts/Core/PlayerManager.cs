// Filename: PlayerManager.cs (Final Clean Version)

using System;
using System.Collections.Generic;
using System.Linq;
using BarbarosKs.Shared.DTOs;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerManager : IGameService
{
    // Bu metot, GameManager tarafından sahne yüklendiğinde çağrılır.
    // ReSharper disable Unity.PerformanceAnalysis
    public async void SpawnPlayer(ulong clientId, Guid shipIdToSpawn)
    {
        Debug.Log($"[Spawn] Client {clientId} için {shipIdToSpawn} gemisi verisi isteniyor...");

        // 1. Backend'den Veriyi Çek (API)
        var playerApiService = ServiceLocator.Current.Get<PlayerApiService>();
        await playerApiService.ConnectRequestToServerForData(shipIdToSpawn);
        
        var playerSession = playerApiService.PlayerSession; // Session ID ve Temel Bilgiler
        var shipData = playerApiService.ShipData;           // Konum, Gülleler, Skinler
        var shipStats = playerApiService.ShipStats;         // Hız, Zırh, Can (Hesaplanmış)

        // Veri bütünlüğü kontrolü
        if (playerSession == null || shipData == null || shipStats == null)
        {
            Debug.LogError($"[Spawn] HATA: Client {clientId} için API verisi EKSİK geldi! Spawn iptal.");
            return;
        }
        
        // 2. Gemiyi Oluştur (Instantiate)
        var spawnPos = new Vector3(shipData.PositionX, shipData.PositionY, shipData.PositionZ);
        var spawnRot = new Quaternion(shipData.RotationX, shipData.RotationY, shipData.RotationZ, shipData.RotationW);
        
        var shipInstance = UnityEngine.Object.Instantiate(GameManager.Instance.ShipPrefab, spawnPos, spawnRot);
        var networkObject = shipInstance.GetComponent<NetworkObject>();
        
        // Sahipliği Client'a ver
        networkObject.SpawnAsPlayerObject(clientId, true);
        Debug.Log($"[Spawn] Gemi oluşturuldu: {shipData.ShipName} (Owner: {clientId})");

        // 3. Görsel ve Temel Bileşenleri Doldur (Senkronizasyon)
        shipInstance.GetComponent<PlayerInfo>().Initialize(shipData.ShipName);
        shipInstance.GetComponent<ShipIdentity>().shipId.Value = new FixedString128Bytes(playerSession.ShipId.ToString());
        shipInstance.GetComponent<Health>().Initialize(shipStats.MaxHull, shipStats.CurrentHull);
        
        // --- DÖNÜŞTÜRME İŞLEMİ ---
        // Shared DTO'daki ICollection<InventoryDto>'yu Unity'nin beklediği formata çeviriyoruz.
        // Eğer Cannonballs null gelirse boş liste oluşturuyoruz.
        List<InventoryDto> inventoryList = shipData.Cannonballs != null 
            ? new List<InventoryDto>(shipData.Cannonballs) 
            : new List<InventoryDto>();
        
        // 4. KRİTİK: Sunucu RAM'ine (SessionManager) Kayıt
        // Burası ateş etme sisteminin "Güllem var mı?" diye soracağı yerdir.
        if (NetworkManager.Singleton.IsServer)
        {
            var sessionManager = ServiceLocator.Current.Get<ServerSessionManager>();
        
            sessionManager.RegisterSession(
                clientId, 
                playerSession.ShipId, 
                shipData.ShipName, 
                shipStats.CurrentHull, 
                shipStats.MaxHull, 
                inventoryList // API'den gelen envanter listesi RAM'e yazılır
            );
            Debug.Log($"[Spawn] Sunucu RAM Session oluşturuldu: {shipData.ShipName}");
        }
        
        // 5. Statları Senkronize Et (Hız, Manevra vb.)
        var statsToSync = new ShipStatsData
        {
            Speed = shipStats.Speed,
            Maneuverability = shipStats.Maneuverability,
            HitRate = shipStats.HitRate,
            Range = shipStats.Range,
            Armor = shipStats.Armor,
            CurrentVigor = shipStats.CurrentVigor,
            Cooldown = shipStats.Cooldown,
        };
        
        var shipStatsComponent = shipInstance.GetComponent<ShipStats>();
        if (shipStatsComponent != null)
        {
            shipStatsComponent.InitializeServerRpc(statsToSync);
        }
        else
        {
            Debug.LogError("[Spawn] ShipStats bileşeni bulunamadı!");
        }

// 6. Savaş Sistemini Hazırla
        if (shipData.ActiveCannonballCode.HasValue)
        {
            var playerController = shipInstance.GetComponent<PlayerController>();
            var shipCombat = shipInstance.GetComponent<ShipCombat>();
            
            if (playerController != null && shipCombat != null)
            {
                // ShipCombat eski ShipCannonballInventoryDto istiyor olabilir,
                // Onu da ShipCombat.cs içinde güncellemiştik ama emin olmak için buraya 
                // eski tipe dönüştürme mantığı ekleyebiliriz VEYA ShipCombat'ı InventoryDto alacak şekilde güncellediysek direkt veririz.
                
                // Eğer ShipCombat hala eski ShipCannonballInventoryDto listesi istiyorsa burayı şöyle güncelle:
                // Şimdilik 'inventoryList' (InventoryDto listesi) gönderiyorum. 
                // Eğer ShipCombat.cs hala eski tipteyse orayı da güncellememiz gerekir (Aşağıda belirttim).
                
                // NOT: Önceki adımda ShipCombat.InitializeForPlayer metodunu güncellememiştik.
                // Bu yüzden burada geçici bir çeviri (Map) yapıyorum ki kod patlamasın.
                // İdeal olan ShipCombat'ı da InventoryDto'ya geçirmektir.
                
                var combatInventory = inventoryList.Select(x => new ShipCannonballInventoryDto 
                { 
                    CannonballCode = x.ItemCode, // Veya x.Code
                    Quantity = x.Quantity 
                }).ToList();

                shipCombat.InitializeForPlayer(
                    shipData.ActiveCannonballCode.Value,
                    combatInventory, // Çevrilmiş liste
                    playerController._cannonSpawnPoint
                );
                
                Debug.Log($"[Spawn] Combat sistemi hazırlandı. Aktif Gülle: {shipData.ActiveCannonballCode.Value}");
            }
        }

        Debug.Log("[Spawn] İşlem Başarıyla Tamamlandı.");
    }
}