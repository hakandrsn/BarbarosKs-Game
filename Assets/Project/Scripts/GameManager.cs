using System;
using BarbarosKs.Shared.DTOs;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // API'den gelen tüm veriyi saklamak için
    public CharacterSelectionDto CharacterData { get; private set; }

    // Oyuncunun seçtiği aktif gemiyi saklamak için
    public ShipSummaryDto ActiveShip { get; private set; }

    // Kolay erişim için kısayollar
    public PlayerProfileDto CurrentPlayerProfile => CharacterData?.PlayerProfile;
    public Guid? LocalPlayerId => CharacterData?.PlayerProfile?.Id;

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
            Debug.LogError("Karakter verisi alınamadı veya eksik!");
            return;
        }

        CharacterData = characterData;
        Debug.Log(
            $"Hoşgeldin, {characterData.PlayerProfile.Username}! Sahip olunan gemi sayısı: {characterData.Ships.Count}");

        if (characterData.Ships.Count == 0)
        {
            Debug.Log("Oyuncunun hiç gemisi yok. Doğrudan oyun sahnesine yönlendiriliyor...");
            ActiveShip = null;
            SceneManager.LoadScene("CreateShip"); // TODO: Belki "İlk Gemiyi Alma" sahnesine yönlendirilebilir.
        }
        else
        {
            Debug.Log("Oyuncunun gemileri var. Gemi seçim sahnesine yönlendiriliyor...");
            
            // GEÇİCİ FİX: Eğer tek gemi varsa otomatik seç
            if (characterData.Ships.Count == 1)
            {
                Debug.Log("⚡ GEÇİCİ FİX: Tek gemi var, otomatik olarak seçiliyor...");
                SetActiveShipAndEnterGame(characterData.Ships[0]);
                return;
            }
            
            SceneManager.LoadScene("Scenes/SelectShipScene");
        }
    }

    /// <summary>
    ///     Gemi seçim ekranından seçilen gemiyi ayarlar ve oyun dünyasına giriş yapar.
    /// </summary>
    public void SetActiveShipAndEnterGame(ShipSummaryDto selectedShip)
    {
        Debug.Log($"==== GEMİ SEÇİMİ YAPILDI ====");
        Debug.Log($"Seçilen Gemi: {selectedShip?.Name ?? "NULL"} (ID: {selectedShip?.Id.ToString() ?? "NULL"})");
        
        if (selectedShip == null)
        {
            Debug.LogError("❌ HATA: Seçilen gemi NULL!");
            return;
        }
        
        ActiveShip = selectedShip;
        Debug.Log($"✅ ActiveShip ayarlandı: {selectedShip.Name}. Oyun sahnesi yükleniyor...");
        SceneManager.LoadScene("FisherSea");
    }

    public void ToScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }
}