// Filename: PlayerManager.cs (Final Simplified Version)

using System;
using BarbarosKs.Shared.DTOs;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

// Bu script artık bir IGameService olmak zorunda değil.
public class PlayerManager : IGameService
{
    // Bu metot artık sadece GameManager tarafından çağrılıyor.
    public async void SpawnPlayer(ulong clientId, Guid shipIdToSpawn)
    {
        Debug.Log($"[SPAWN] SpawnPlayer çağrıldı | clientId={clientId} shipIdToSpawn={shipIdToSpawn}");
        var playerApiService = ServiceLocator.Current.Get<PlayerApiService>();
        if (playerApiService == null)
        {
            Debug.LogError("[SPAWN] PlayerApiService ServiceLocator üzerinden alınamadı!");
            return;
        }

        Debug.Log("[SPAWN] PlayerApiService alındı, sunucuya bağlanılıyor...");
        await playerApiService.ConnectRequestToServerForData(shipIdToSpawn);
        Debug.Log("[SPAWN] Sunucudan gerekli veriler alındı (ConnectRequestToServerForData tamam)");
        var playerSession = playerApiService.PlayerSession;
        var shipData = playerApiService.ShipData;
        var shipStats = playerApiService.ShipStats;

        if (playerSession == null)
        {
            Debug.LogError("[SPAWN] playerSession null döndü!");
            return;
        }

        if (shipData == null)
        {
            Debug.LogError("[SPAWN] shipData null döndü!");
            return;
        }

        if (shipStats == null)
        {
            Debug.LogError("[SPAWN] shipStats null döndü!");
            return;
        }

        Debug.Log(
            $"[SPAWN] SessionShipId={playerSession.ShipId} | ShipName={shipData.ShipName} | Pos=({shipData.PositionX},{shipData.PositionY},{shipData.PositionZ}) | Rot=({shipData.RotationX},{shipData.RotationY},{shipData.RotationZ},{shipData.RotationW})");
        Debug.Log(
            $"[SPAWN] Stats | MaxHull={shipStats.MaxHull} CurrentHull={shipStats.CurrentHull} Speed={shipStats.Speed} Maneuverability={shipStats.Maneuverability} HitRate={shipStats.HitRate} Range={shipStats.Range} Armor={shipStats.Armor} CurrentVigor={shipStats.CurrentVigor}");
        // Debug.Log($"Veriler alındı: {shipData.ShipName}, Can: {shipDetail.CurrentHull}/{shipStats.MaxHull}");

        // Gemi pozisyonunu ve rotasyonunu sizin DTO'nuzdaki doğru alan adlarıyla alıyoruz.
        var spawnPosition = new Vector3(shipData.PositionX, shipData.PositionY, shipData.PositionZ);
        var spawnRotation = new Quaternion(shipData.RotationX, shipData.RotationY, shipData.RotationZ,
            shipData.RotationW);

        Debug.Log(
            $"[SPAWN] Instantiate başlıyor | Prefab={(GameManager.Instance && GameManager.Instance.ShipPrefab ? GameManager.Instance.ShipPrefab.name : "NULL")} | Pos={spawnPosition} Rot={spawnRotation}");
        var shipInstance = UnityEngine.Object.Instantiate(
            GameManager.Instance.ShipPrefab,
            spawnPosition,
            spawnRotation
        );
        Debug.Log($"[SPAWN] Instantiate tamam | Instance='{shipInstance.name}'");

		var networkObject = shipInstance.GetComponent<NetworkObject>();
		if (!networkObject)
		{
			Debug.LogError("[SPAWN] NetworkObject bileşeni gemi prefab'ında bulunamadı!");
			return;
		}

		networkObject.SpawnAsPlayerObject(clientId, true);
		Debug.Log("[SPAWN] NetworkObject.SpawnAsPlayerObject çağrıldı");
		Debug.Log("[DEBUG-9] Gemi bileşenleri verilerle dolduruluyor (Initialize)...");

		// NetworkVariable yazımlarını SPAWN'dan SONRA yap
		shipInstance.GetComponent<PlayerInfo>().Initialize(shipData.ShipName);
		Debug.Log($"[SPAWN] PlayerInfo.Initialize çağrıldı | ShipName='{shipData.ShipName}'");
		shipInstance.GetComponent<ShipIdentity>().shipId.Value =
			new FixedString128Bytes(playerSession.ShipId.ToString());
		Debug.Log($"[SPAWN] ShipIdentity.shipId atandı | {playerSession.ShipId}");
		shipInstance.GetComponent<Health>().Initialize(shipStats.MaxHull, shipStats.CurrentHull);
		Debug.Log($"[SPAWN] Health.Initialize çağrıldı | MaxHull={shipStats.MaxHull} CurrentHull={shipStats.CurrentHull}");

		var playerController = shipInstance.GetComponent<PlayerController>();
        if (!playerController)
        {
            Debug.LogError("HATA: Gemi prefab'ının üzerinde PlayerController script'i bulunamadı!");
            return;
        }

        Debug.Log("[SPAWN] PlayerController bulundu");

        // yeni bir senkronizer data gelirse buraya ve shipStats ekelenecek
        var statsToSync = new ShipStatsData
        {
            Speed = shipStats.Speed,
            Maneuverability = shipStats.Maneuverability, // Manevra kabiliyetini doğrudan açısal hıza atıyoruz.
            HitRate = shipStats.HitRate,
            Range = shipStats.Range,
            Armor = shipStats.Armor,
            CurrentVigor = shipStats.CurrentVigor,
            Cooldown = shipStats.Cooldown,
        };
        Debug.Log(
            $"[SPAWN] ShipStatsData hazırlanıyor | Speed={statsToSync.Speed} Maneuverability={statsToSync.Maneuverability} HitRate={statsToSync.HitRate} Range={statsToSync.Range} Armor={statsToSync.Armor} CurrentVigor={statsToSync.CurrentVigor}");

		var shipStatsComponent = shipInstance.GetComponent<ShipStats>();
        if (!shipStatsComponent)
        {
            Debug.LogError("[SPAWN] ShipStats bileşeni gemi prefab'ında bulunamadı!");
            return;
        }

        shipStatsComponent.InitializeServerRpc(statsToSync);
        Debug.Log("[SPAWN] ShipStats.InitializeServerRpc çağrıldı");

		if (shipData.ActiveCannonballCode != null)
        {
            shipInstance.GetComponent<ShipCombat>().InitializeForPlayer(
                shipData.ActiveCannonballCode.Value,
                shipData.Cannonballs,
                playerController._cannonSpawnPoint // CannonSpawnPoint referansını PlayerController'dan alıyoruz.
            );
            Debug.Log(
                $"[SPAWN] ShipCombat.InitializeForPlayer çağrıldı | ActiveCannonball='{shipData.ActiveCannonballCode}' CannonballCount={(shipData.Cannonballs != null ? shipData.Cannonballs.Count : 0)} SpawnPointNull={(playerController._cannonSpawnPoint == null)}");
        }

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
        AttackResponseDto result = await playerApiService.ProcessAttackAsync(
            new Guid(attackerIdentity.shipId.Value.ToString()),
            new Guid(targetIdentity.shipId.Value.ToString()));

        if (result == null) return;
        // web api dan gelen hasarı uygula
        if (!targetObject.TryGetComponent<Health>(out Health targetHealth)) return;
        Debug.Log($"API'den hasar sonucu geldi: {result.Damage}, Kritik: {result.IsCritical}. Uygulanıyor...");
        targetHealth.TakeDamage(result.Damage);
    }
}