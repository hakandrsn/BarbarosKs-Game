// Filename: ShipCombat.cs (Final, Corrected Data Sync)

using System.Collections.Generic;
using BarbarosKs.Shared.DTOs;
using Unity.Netcode;
using UnityEngine;

public class ShipCombat : NetworkBehaviour
{
    public NetworkVariable<int> EquippedCannonballCode = new NetworkVariable<int>();

    private float _lastAttackTime = -999f;
    private ulong _autoAttackTargetId = ulong.MaxValue;

    private ShipStats _shipStats;
    private Transform _cannonSpawnPoint;
    private readonly Dictionary<int, int> _cannonballInventory = new();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _shipStats = GetComponent<ShipStats>();
        }
    }

    public void InitializeForPlayer(int activeCannonballCode, List<ShipCannonballInventoryDto> inventory, Transform cannonSpawnPoint)
    {
        if (!IsServer) return;
        
        EquippedCannonballCode.Value = activeCannonballCode;
        _cannonSpawnPoint = cannonSpawnPoint; // Referansı dışarıdan alıyoruz.
        
        _cannonballInventory.Clear();
        if (inventory != null)
        {
            foreach (var item in inventory)
            {
                _cannonballInventory[item.CannonballCode] = item.Quantity;
            }
        }
        
        var inventoryWrapper = new ShipCannonballInventoryWrapper { Inventory = inventory };
        SyncInventoryClientRpc(inventoryWrapper, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } });
    }

    public void InitializeForNpc(int cannonballCode, Transform cannonSpawnPoint)
    {
        if (!IsServer) return;
        EquippedCannonballCode.Value = cannonballCode;
        _cannonSpawnPoint = cannonSpawnPoint; // Referansı dışarıdan alıyoruz.
        
        // NPC'lerin şimdilik sonsuz mühimmatı olsun.
        _cannonballInventory[cannonballCode] = 9999;
    }

    [ClientRpc]
    private void SyncInventoryClientRpc(ShipCannonballInventoryWrapper inventoryWrapper,
        ClientRpcParams clientRpcParams = default)
    {
        // Bu kod sadece bu geminin sahibi olan client'ta çalışır.
        Debug.Log($"Client tarafında envanter verisi alındı: {inventoryWrapper.Inventory.Count} çeşit gülle.");
        var playerInventory = ServiceLocator.Current.Get<PlayerInventory>();
        playerInventory.UpdateInventory(inventoryWrapper.Inventory);
    }

    public void ToggleAutoAttack(ulong targetId)
    {
        if (!IsServer) return;
        _autoAttackTargetId = (_autoAttackTargetId == targetId) ? ulong.MaxValue : targetId;
    }

    private void Update()
    {
        if (!IsServer || _autoAttackTargetId == ulong.MaxValue) return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_autoAttackTargetId,
                out var targetObject))
        {
            _autoAttackTargetId = ulong.MaxValue;
            return;
        }

        var gameDataService = ServiceLocator.Current.Get<GameDataService>();
        CannonballTypeDto equippedCannonballStats =
            gameDataService.GetCannonballStatsByCode(EquippedCannonballCode.Value);

        if (equippedCannonballStats == null) return;

        float totalRange = _shipStats.Range.Value + equippedCannonballStats.RangeBonus;
        if (Vector3.Distance(transform.position, targetObject.transform.position) > totalRange) return;
        if (Time.time < _lastAttackTime + _shipStats.AttackRate.Value) return;

        if (!_cannonballInventory.TryGetValue(EquippedCannonballCode.Value, out int count) || count <= 0)
        {
            _autoAttackTargetId = ulong.MaxValue;
            return;
        }

        var cannonballDb = GameManager.Instance.CannonballDatabase;
        CannonballData equippedCannonballTemplate = cannonballDb.GetCannonballByCode(EquippedCannonballCode.Value);
        if (equippedCannonballTemplate == null) return;

        PerformAttack(targetObject, equippedCannonballTemplate, equippedCannonballStats);
    }

    private void PerformAttack(NetworkObject targetObject, CannonballData cannonballTemplate,
        CannonballTypeDto cannonballStats)
    {
        _cannonballInventory[cannonballStats.Code]--;
        _lastAttackTime = Time.time;

        FireEffectsClientRpc(cannonballStats.Code);

        GameObject projectileInstance = Instantiate(cannonballTemplate.projectilePrefab, _cannonSpawnPoint.position,
            _cannonSpawnPoint.rotation);
        projectileInstance.GetComponent<NetworkObject>().Spawn();
        projectileInstance.GetComponent<CannonballProjectile>().Initialize(
            targetObject.NetworkObjectId,
            cannonballStats.BaseDamage,
            30,
            cannonballTemplate.cannonballCode
        );
    }

    [ClientRpc]
    private void FireEffectsClientRpc(int cannonballCode)
    {
        var cannonballDb = GameManager.Instance.CannonballDatabase;
        CannonballData cannonballTemplate = cannonballDb.GetCannonballByCode(cannonballCode);
        if (cannonballTemplate != null && cannonballTemplate.muzzleFlashPrefab != null)
        {
            Instantiate(cannonballTemplate.muzzleFlashPrefab, _cannonSpawnPoint.position, _cannonSpawnPoint.rotation);
        }
    }

    [ServerRpc]
    public void EquipCannonballServerRpc(int newCannonballCode)
    {
        if (_cannonballInventory.ContainsKey(newCannonballCode) && _cannonballInventory[newCannonballCode] > 0)
        {
            EquippedCannonballCode.Value = newCannonballCode;
        }
    }
}

// Netcode'un RPC üzerinden bir liste gönderebilmesi için, onu bir struct içinde sarmalamamız gerekir.
public struct ShipCannonballInventoryWrapper : INetworkSerializable
{
    public List<ShipCannonballInventoryDto> Inventory;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int count = 0;
        if (!serializer.IsReader && Inventory != null) count = Inventory.Count;
        serializer.SerializeValue(ref count);

        if (serializer.IsReader) Inventory = new List<ShipCannonballInventoryDto>(count);

        for (int i = 0; i < count; i++)
        {
            int code = 0;
            int quantity = 0;

            if (!serializer.IsReader)
            {
                code = Inventory[i].CannonballCode;
                quantity = Inventory[i].Quantity;
            }

            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref quantity);

            if (serializer.IsReader)
            {
                Inventory.Add(new ShipCannonballInventoryDto { CannonballCode = code, Quantity = quantity });
            }
        }
    }
}