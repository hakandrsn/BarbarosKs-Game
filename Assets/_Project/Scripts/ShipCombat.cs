// Filename: ShipCombat.cs (Final, Corrected Data Sync)

using System.Collections.Generic;
using BarbarosKs.Shared.DTOs;
using Unity.Netcode;
using UnityEngine;
using System;

public class ShipCombat : NetworkBehaviour
{
    public NetworkVariable<int> EquippedCannonballCode = new NetworkVariable<int>();

// Saldırı hızı kontrolü
    private float _lastAttackTime = -999f;

    // Otomatik saldırı hedefi
    private ulong _autoAttackTargetId = ulong.MaxValue;

    // Referanslar
    private ShipStats _shipStats;
    private Transform _cannonSpawnPoint;

    // RAM Yöneticisi Referansı
    private ServerSessionManager _sessionManager;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _shipStats = GetComponent<ShipStats>();
            // Performans için servisi cache'liyoruz
            _sessionManager = ServiceLocator.Current.Get<ServerSessionManager>();
        }
    }

    public void InitializeForPlayer(int activeCannonballCode, List<ShipCannonballInventoryDto> inventory,
        Transform cannonSpawnPoint)
    {
        if (!IsServer) return;

        EquippedCannonballCode.Value = activeCannonballCode;
        _cannonSpawnPoint = cannonSpawnPoint;

        // NOT: Envanter artık burada değil, ServerSessionManager'da (RAM) tutuluyor.
        // Client'ın UI güncellemesi için RPC gönderiyoruz.
        var inventoryWrapper = new ShipCannonballInventoryWrapper { Inventory = inventory };
        SyncInventoryClientRpc(inventoryWrapper,
            new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } });
    }

    public void InitializeForNpc(int cannonballCode, Transform cannonSpawnPoint)
    {
        if (!IsServer) return;
        EquippedCannonballCode.Value = cannonballCode;
        _cannonSpawnPoint = cannonSpawnPoint;
    }

    /// <summary>
    /// Saldırı emri verir veya durdurur.
    /// </summary>
    public void ToggleAutoAttack(ulong targetId)
    {
        if (!IsServer) return;

        // Eğer aynı hedefe tekrar tıklandıysa veya geçersiz bir id geldiyse saldırıyı durdurma mantığı eklenebilir.
        // Şimdilik doğrudan hedefi güncelliyoruz.
        _autoAttackTargetId = targetId;

        // Hedef geçerli mi kontrol et, değilse saldırıyı iptal et
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(targetId))
        {
            _autoAttackTargetId = ulong.MaxValue;
        }
    }

    private void Update()
    {
        // SADECE SUNUCU BU MANTIĞI ÇALIŞTIRIR
        if (!IsServer || _autoAttackTargetId == ulong.MaxValue) return;

        // 1. Hedef Oyunda mı?
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_autoAttackTargetId,
                out var targetObject))
        {
            _autoAttackTargetId = ulong.MaxValue; // Hedef yok oldu, saldırıyı kes
            return;
        }

        // 2. Cooldown Kontrolü
        if (Time.time < _lastAttackTime + _shipStats.Cooldown.Value) return;

        // 3. Menzil Kontrolü
        var gameDataService = ServiceLocator.Current.Get<GameDataService>();
        var ammoStats = gameDataService.GetCannonballStatsByCode(EquippedCannonballCode.Value);

        // Eğer veri servisinde bu gülle yoksa varsayılan bir menzil uydurmayalım, hata dönelim.
        if (ammoStats == null)
        {
            Debug.LogError($"[ShipCombat] Gülle verisi bulunamadı: {EquippedCannonballCode.Value}");
            return;
        }

        float totalRange = _shipStats.Range.Value + ammoStats.Range;
        if (Vector3.Distance(transform.position, targetObject.transform.position) > totalRange) return;

        // 4. SALDIRIYI GERÇEKLEŞTİR (RAM ÜZERİNDEN)
        ExecuteServerSideAttack(targetObject, ammoStats);
    }

    private void ExecuteServerSideAttack(NetworkObject targetObject, CannonballDto ammoStats)
    {
        // A. Mermi Kontrolü (RAM'den düş)
        // NPC mi Player mı kontrolü:
        bool isPlayer = _sessionManager.GetSession(OwnerClientId) != null;

        if (isPlayer)
        {
            var attackerSession = _sessionManager.GetSession(OwnerClientId);

            // Mermi var mı ve düşülebildi mi?
            if (!attackerSession.TryConsumeAmmo(ammoStats.Code, 1))
            {
                Debug.Log($"[Combat] Client {OwnerClientId} mermisi bitti! Saldırı durduruluyor.");
                _autoAttackTargetId = ulong.MaxValue;
                // Client'a "Mermi Bitti" uyarısı gönderilebilir (Todo)
                return;
            }

            // Client UI'ını güncelle (Gülle sayısı azaldı)
            UpdateClientAmmoUI(ammoStats.Code, attackerSession.Inventory[ammoStats.Code]);
        }

        // B. Saldırı Zamanını Güncelle
        _lastAttackTime = Time.time;

        // C. Hasar Hesapla (Basit Matematik - API YOK!)
        // Formül: (Gülle Hasarı) * Kritik Şans vb.
        int damage = ammoStats.BaseDamage;
        bool isCritical = UnityEngine.Random.value < 0.1f; // %10 Kritik şans
        if (isCritical) damage *= 2;

        // D. Hedefe Hasarı Uygula
        if (targetObject.TryGetComponent<Health>(out var targetHealth))
        {
            // Hedef bir oyuncuysa onun da RAM'deki verisini güncellemeliyiz!
            var targetSession = _sessionManager.GetSession(targetObject.OwnerClientId);
            if (targetSession != null)
            {
                targetSession.ApplyDamage(damage);
            }

            // NetworkVariable (Görsel Can) güncelle
            targetHealth.TakeDamage(damage);
        }

        // E. Görsel Efektler (Mermiyi oluştur)
        SpawnProjectileAndEffects(targetObject, damage, ammoStats.Code);
    }

    private void SpawnProjectileAndEffects(NetworkObject targetObject, int damage, int ammoCode)
    {
        var cannonballDb = GameManager.Instance.CannonballDatabase;
        var template = cannonballDb.GetCannonballByCode(ammoCode);
        if (template == null) return;

        // Muzzle Flash (Efekt - Tüm clientlarda oynar)
        FireEffectsClientRpc(ammoCode);

        // Mermiyi oluştur (Projectile)
        GameObject projectileInstance = Instantiate(template.projectilePrefab, _cannonSpawnPoint.position,
            _cannonSpawnPoint.rotation);
        projectileInstance.GetComponent<NetworkObject>().Spawn(); // Network'e tanıt

        // Mermiyi hedefe kilitle
        projectileInstance.GetComponent<CannonballProjectile>().Initialize(
            targetObject.NetworkObjectId,
            damage,
            20f, // Mermi hızı
            ammoCode
        );
    }

    [ClientRpc]
    private void UpdateSingleInventoryItemClientRpc(int code, int quantity, ClientRpcParams clientRpcParams = default)
    {
        var playerInventory = ServiceLocator.Current.Get<PlayerInventory>();
        // PlayerInventory içinde bu metodun olması lazım (Ekleyeceğiz)
        if (playerInventory is PlayerInventory inventory)
        {
            inventory.SetQuantity(code, quantity);
        }
    }

    private void UpdateClientAmmoUI(int ammoCode, int newQuantity)
    {
        UpdateSingleInventoryItemClientRpc(ammoCode, newQuantity, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    private void SyncInventoryClientRpc(ShipCannonballInventoryWrapper inventoryWrapper,
        ClientRpcParams clientRpcParams = default)
    {
        var playerInventory = ServiceLocator.Current.Get<PlayerInventory>();
        playerInventory.UpdateInventory(inventoryWrapper.Inventory);
    }

    [ClientRpc]
    private void FireEffectsClientRpc(int cannonballCode)
    {
        var cannonballDb = GameManager.Instance.CannonballDatabase;
        var template = cannonballDb.GetCannonballByCode(cannonballCode);
        if (template != null && template.muzzleFlashPrefab != null)
        {
            Instantiate(template.muzzleFlashPrefab, _cannonSpawnPoint.position, _cannonSpawnPoint.rotation);
        }
    }

    [ServerRpc]
    public void EquipCannonballServerRpc(int newCannonballCode)
    {
        // Envanter kontrolü RAM üzerinden yapılmalı
        var session = _sessionManager.GetSession(OwnerClientId);
        if (session != null && session.Inventory.TryGetValue(newCannonballCode, out int qty) && qty > 0)
        {
            EquippedCannonballCode.Value = newCannonballCode;
        }
    }
}

public struct ShipCannonballInventoryWrapper : INetworkSerializable
{
    public List<ShipCannonballInventoryDto> Inventory;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int count = 0;

        // Yazma modu (Server -> Client): Listede kaç eleman var belirle
        if (!serializer.IsReader && Inventory != null)
        {
            count = Inventory.Count;
        }

        // Eleman sayısını gönder/al
        serializer.SerializeValue(ref count);

        // Okuma modu (Client): Listeyi oluştur
        if (serializer.IsReader)
        {
            Inventory = new List<ShipCannonballInventoryDto>(count);
        }

        // Listenin her bir elemanını tek tek gönder/al
        for (int i = 0; i < count; i++)
        {
            // Varsayılan değerler
            int code = 0;
            int quantity = 0;

            // Yazma modu: Değerleri listeden al
            if (!serializer.IsReader)
            {
                code = Inventory[i].CannonballCode;
                quantity = Inventory[i].Quantity;
            }

            // Değerleri serileştir (Ağ üzerinden geçir)
            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref quantity);

            // Okuma modu: Okunan değerleri listeye ekle
            if (serializer.IsReader)
            {
                Inventory.Add(new ShipCannonballInventoryDto
                {
                    CannonballCode = code,
                    Quantity = quantity
                });
            }
        }
    }
}