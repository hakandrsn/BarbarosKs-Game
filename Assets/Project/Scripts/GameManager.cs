using System;
using BarbarosKs.Shared.DTOs;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // **DEPRECATED** - PlayerDataManager kullanın
    // Eski sistemle uyumluluk için kısa süre tutulacak
    [System.Obsolete("PlayerDataManager.Instance kullanın")]
    public CharacterSelectionDto CharacterData => PlayerDataManager.Instance?.PlayerProfile != null ? 
        new CharacterSelectionDto { PlayerProfile = PlayerDataManager.Instance.PlayerProfile, Ships = PlayerDataManager.Instance.OwnedShips } : null;

    [System.Obsolete("PlayerDataManager.Instance.ActiveShip kullanın")]
    public ShipSummaryDto ActiveShip => PlayerDataManager.Instance?.ActiveShip;

    // Kolay erişim için kısayollar
    [System.Obsolete("PlayerDataManager.Instance.PlayerProfile kullanın")]
    public PlayerProfileDto CurrentPlayerProfile => PlayerDataManager.Instance?.PlayerProfile;
    
    [System.Obsolete("PlayerDataManager.Instance.PlayerProfile?.Id kullanın")]
    public Guid? LocalPlayerId => PlayerDataManager.Instance?.PlayerProfile?.Id;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    ///     Login veya Register sonrası API'den gelen veriyi işler ve doğru sahneye yönlendirir.
    /// </summary>
    public void OnCharacterDataReceived(CharacterSelectionDto characterData)
    {
        if (characterData == null)
        {
            Debug.LogError("❌ Karakter verisi alınamadı veya eksik!");
            return;
        }

        // Null control ekliyoruz
        if (characterData.PlayerProfile == null)
        {
            Debug.LogError("❌ PlayerProfile null! CharacterData içinde player profili bulunamadı.");
            return;
        }

        if (characterData.Ships == null)
        {
            Debug.LogError("❌ Ships listesi null! CharacterData içinde gemi listesi bulunamadı.");
            return;
        }

        // PlayerDataManager kontrolü
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("❌ PlayerDataManager Instance null! PlayerDataManager başlatılmamış.");
            return;
        }

        // Debug için detaylı log
        Debug.Log($"🎯 CharacterData alındı:");
        Debug.Log($"   - PlayerProfile: {(characterData.PlayerProfile != null ? "MEVCUT" : "NULL")}");
        Debug.Log($"   - PlayerProfile.Username: {characterData.PlayerProfile?.Username ?? "NULL"}");
        Debug.Log($"   - Ships: {(characterData.Ships != null ? "MEVCUT" : "NULL")}");
        Debug.Log($"   - Ships.Count: {characterData.Ships?.Count ?? 0}");

        // Veriyi PlayerDataManager'a yükle
        PlayerDataManager.Instance.LoadPlayerData(characterData);
        
        Debug.Log($"✅ Hoşgeldin, {characterData.PlayerProfile.Username}! Sahip olunan gemi sayısı: {characterData.Ships.Count}");

        if (characterData.Ships.Count == 0)
        {
            Debug.Log("🚢 Oyuncunun hiç gemisi yok. Gemi oluşturma sahnesine yönlendiriliyor...");
            SceneManager.LoadScene("CreateShip");
        }
        else
        {
            Debug.Log("🚢 Oyuncunun gemileri var. Gemi seçim sahnesine yönlendiriliyor...");
            // ARTIK OTOMATİK SEÇİM YOK - Her zaman gemi seçim ekranına git
            SceneManager.LoadScene("Scenes/SelectShipScene");
        }
    }

    /// <summary>
    ///     Gemi seçim ekranından seçilen gemiyi ayarlar ve oyun dünyasına giriş yapar.
    ///     **DEPRECATED** - ShipSelectionUI artık kendi flow'unu yönetiyor
    /// </summary>
    [System.Obsolete("ShipSelectionUI artık kendi flow'unu yönetiyor")]
    public void SetActiveShipAndEnterGame(ShipSummaryDto selectedShip)
    {
        Debug.Log($"==== GEMİ SEÇİMİ YAPILDI (DEPRECATED METHOD) ====");
        Debug.Log($"Seçilen Gemi: {selectedShip?.Name ?? "NULL"} (ID: {selectedShip?.Id.ToString() ?? "NULL"})");
        
        if (selectedShip == null)
        {
            Debug.LogError("❌ HATA: Seçilen gemi NULL!");
            return;
        }
        
        // PlayerDataManager'a ayarla
        PlayerDataManager.Instance.SetActiveShip(selectedShip);
        Debug.Log($"✅ ActiveShip ayarlandı: {selectedShip.Name}. Oyun sahnesi yükleniyor...");
        SceneManager.LoadScene("FisherSea");
    }

    public void ToScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }
}