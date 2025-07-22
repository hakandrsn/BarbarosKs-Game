// Filename: NpcManager.cs (Simplified Version)
using UnityEngine;
using Unity.Netcode;

public class NpcManager : IGameService
{
    private NpcData _testNpcData;

    public void Initialize(NpcData testNpcData)
    {
        _testNpcData = testNpcData;
    }

    public void SpawnTestNpcs()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log("Test NPC'leri spawn ediliyor...");
        // NPC'yi NavMesh'i düşünmeden, doğrudan hedef koordinatta yaratıyoruz.
        Vector3 spawnPosition = new Vector3(10, 0, 10); 

        GameObject npcInstance = Object.Instantiate(_testNpcData.shipPrefab, spawnPosition, Quaternion.identity);
        
        // Network'te spawn et.
        npcInstance.GetComponent<NetworkObject>().Spawn();
        
        // Gerekli bileşenleri doldur.
        var npcController = npcInstance.GetComponent<NpcController>();
        var shipCombat = npcInstance.GetComponent<ShipCombat>();
        
        npcController.Initialize(_testNpcData);
        npcInstance.GetComponent<Health>().Initialize(_testNpcData.maxHull, _testNpcData.maxHull);
        shipCombat.InitializeForNpc(_testNpcData.equippedCannonballCode, npcController._cannonSpawnPoint);
    }
}