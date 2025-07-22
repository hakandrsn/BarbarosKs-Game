// Filename: PlayerManager.cs (Final Simplified Version)

using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

// Bu script artık bir IGameService olmak zorunda değil.
public class PlayerManager : IGameService
{
    // Bu metot artık sadece GameManager tarafından çağrılıyor.
    public async void SpawnPlayer(ulong clientId, Guid shipIdToSpawn)
    {
        var playerApiService = ServiceLocator.Current.Get<PlayerApiService>();

        // DÜZELTME: Artık API'den ShipDetailResponse bekliyoruz.
        ShipDetailResponse shipDataResponse = await playerApiService.GetShipDetailAsync(shipIdToSpawn);

        // DÜZELTME: Gelen cevabın başarılı olup olmadığını ve verinin null olup olmadığını kontrol ediyoruz.
        if (shipDataResponse is not { success: true } || shipDataResponse.Data == null)
        {
            Debug.LogError(
                $"Gemi spawn edilemedi çünkü {shipIdToSpawn} ID'li geminin verileri alınamadı veya işlem başarısız oldu.");
            return;
        }

        // Asıl gemi verisini response'un içinden alıyoruz.
        var shipDetail = shipDataResponse.Data;

        Debug.Log($"Veriler alındı: {shipDetail.Name}, Can: {shipDetail.CurrentHull}/{shipDetail.MaxHull}");

        // Gemi pozisyonunu ve rotasyonunu sizin DTO'nuzdaki doğru alan adlarıyla alıyoruz.
        Vector3 spawnPosition = new Vector3(shipDetail.PositionX, shipDetail.PositionY, shipDetail.PositionZ);
        Quaternion spawnRotation = new Quaternion(shipDetail.RotationX, shipDetail.RotationY, shipDetail.RotationZ,
            shipDetail.RotationW);

        GameObject shipInstance = UnityEngine.Object.Instantiate(
            GameManager.Instance.ShipPrefab,
            spawnPosition,
            spawnRotation
        );

        var networkObject = shipInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);
        Debug.Log("[DEBUG-9] Gemi bileşenleri verilerle dolduruluyor (Initialize)...");

        var playerController = shipInstance.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("HATA: Gemi prefab'ının üzerinde PlayerController script'i bulunamadı!");
            return;
        }
        
        // yeni bir senkronizer data gelirse buraya ve shipStats ekelenecek
        var statsToSync = new ShipStatsData
        {
            Speed = shipDetail.Speed,
            Maneuverability = shipDetail.Maneuverability, // Manevra kabiliyetini doğrudan açısal hıza atıyoruz.
            AttackRate = shipDetail.AttackRate,
            Range = shipDetail.Range,
            Armor = shipDetail.Armor,
            CurrentVigor = shipDetail.CurrentVigor,
        };

        var shipStats = shipInstance.GetComponent<ShipStats>();
        shipStats.InitializeServerRpc(statsToSync);

        shipInstance.GetComponent<PlayerInfo>().Initialize(shipDetail.Name);
        shipInstance.GetComponent<ShipIdentity>().shipId.Value = new FixedString128Bytes(shipDetail.Id.ToString());
        shipInstance.GetComponent<Health>().Initialize(shipDetail.MaxHull, shipDetail.CurrentHull);
        shipInstance.GetComponent<ShipCombat>().InitializeForPlayer(
            shipDetail.ActiveCannonballCode ?? 0,
            shipDetail.CannonballInventory,
            playerController._cannonSpawnPoint // CannonSpawnPoint referansını PlayerController'dan alıyoruz.
        );

        Debug.Log("[DEBUG-10] SpawnPlayer işlemi başarıyla tamamlandı.");
    }

    public async void ProcessAttack(ulong attackerId, ulong targetId)
    {
        // sunucu, saldıranın ve hedefin kimliğini bulur.
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId,
                out NetworkObject targetObject) ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerId,
                out NetworkObject attackerObject))
        {
            return;
        }

        var attackerIdentity = attackerObject.GetComponent<ShipIdentity>();
        var targetIdentity = targetObject.GetComponent<ShipIdentity>();

        if (attackerIdentity == null || targetIdentity == null) return;

        // Sunucu, ApıManager'ı kullanarak web api dan saldırı sonucunu sorar.
        var playerApiService = ServiceLocator.Current.Get<PlayerApiService>();
        AttackResult result = await playerApiService.ProcessAttackAsync(
            new Guid(attackerIdentity.shipId.Value.ToString()),
            new Guid(targetIdentity.shipId.Value.ToString()));

        if (result == null) return;
        // web api dan gelen hasarı uygula
        if (!targetObject.TryGetComponent<Health>(out Health targetHealth)) return;
        Debug.Log($"API'den hasar sonucu geldi: {result.damage}, Kritik: {result.isCritical}. Uygulanıyor...");
        targetHealth.TakeDamage(result.damage);
    }
}