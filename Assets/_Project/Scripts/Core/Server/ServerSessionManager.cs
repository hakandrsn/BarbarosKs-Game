using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Tüm oyuncuların RAM datalarını yöneten Singleton servis.
/// Sadece SUNUCU tarafında çalışır.
/// </summary>
public class ServerSessionManager : MonoBehaviour, IGameService
{
    // ClientId -> Session haritası (Hızlı erişim için)
    private readonly Dictionary<ulong, ServerPlayerSession> _activeSessions = new();

    //singleton erişimi (veya serviceLocator üzerinden)
    public static ServerSessionManager Instance { get; private set; }

    [Header("Settings")] [SerializeField] private float _autoSaveInternal = 5.0f;
    private float _timer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        //Toplu güncelleme
        _timer += Time.deltaTime;
        if (_timer >= _autoSaveInternal)
        {
            _timer = 0;
            _ = SaveDirtySessionAsync(); // fire and forget
        }
    }

    /// <summary>
    /// Oyuncu oyuna girdiğinde (SpawnPlayer) çağrılır. Veriyi RAM'e yazar.
    /// </summary>
    public void RegisterSession(ulong clientId, Guid characterId, string name, int currentHp, int maxHp,
        IEnumerable<InventoryDto> inventory) 
    {
        if (_activeSessions.ContainsKey(clientId))
        {
            Debug.LogWarning($"Client {clientId} için zaten bir session var, Üzerine yazılıyor");
            _activeSessions.Remove(clientId);
        }

        var session = new ServerPlayerSession(clientId, characterId, name, currentHp, maxHp);
        
        // Artık session.SetInventory metodu da yeni tipi kabul ettiği için burası çalışacak.
        session.SetInventory(inventory); 
        
        _activeSessions.Add(clientId, session);
        Debug.Log($"[ServerSessionManager] Session RAM'e eklendi: {name} (Client: {clientId})");
    }

    /// <summary>
    /// Oyuncu çıktığında RAM'den siler (önce son bir save yapılmalı).
    /// </summary>
    public void UnRegisterSession(ulong clientId)
    {
        if (_activeSessions.TryGetValue(clientId, out var session))
        {
            // Çıkmadan önce son durumu kaydetmeyi deneyebiliriz (Bloklamadan)
            // SaveSingleSession(session);
            _activeSessions.Remove(clientId);
            Debug.Log($"[ServerSessionManager] Session silindi: Client {clientId}");
        }
    }

    public ServerPlayerSession GetSession(ulong clientId)
    {
        _activeSessions.TryGetValue(clientId, out var session);
        return session;
    }

    /// <summary>
    /// Sadece verisi değişmiş (Dirty) oyuncuları bulup API'ye gönderir.
    /// </summary>
    private async Task SaveDirtySessionAsync()
    {
        // değişiklik olanları filtrele
        var dirtySessions = _activeSessions.Values.Where(s => s.IsDirty).ToList();
        if (dirtySessions.Count == 0) return;
        
        Debug.Log($"[BatchSave] {dirtySessions.Count} adet güncellenmiş oyuncu veriyi API'ye gönderiliyor...");
        
        // TODO: Burası için özel bir "BatchUpdateDto" oluşturacağız.
        // Şimdilik her biri için tek tek API çağırıyormuşuz gibi simüle edelim veya 
        // İleride tek bir POST isteği ile hepsini yollayacağız.
        
        // ÖNEMLİ: Dirty flag'ini hemen false yapıyoruz ki bir sonraki döngüde tekrar almayalım.
        // Eğer API hata verirse veri kaybı riski vardır (MMO trade-off: Performans vs Consistency).
        // Daha güvenli olması için API'den "OK" gelince false yapılabilir ama şimdilik hızlı olması için:
        foreach (var session in dirtySessions)
        {
            session.IsDirty = false; 
            session.LastSaveTime = DateTime.UtcNow;
        }

        // Simülasyon: API Service'e toplu DTO gönderimi burada yapılacak.
        // await ServiceLocator.Current.Get<PlayerApiService>().BatchUpdatePlayers(dtoList);
        await Task.CompletedTask;
    }
}