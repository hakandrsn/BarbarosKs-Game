using System.Collections.Generic;
using BarbarosKs.core.DTOs;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // API'den gelen tüm veriyi saklamak için yeni bir property
    public AccountDto CharacterData { get; private set; }

    // Kolay erişim için property'ler
    public AccountDto CurrentAccount;
    public PlayerDto CurrentPlayerProfile => CharacterData?.Player;
    public List<ShipSummaryDto> PlayerShips => CurrentPlayerProfile?.Ships;

    // Oyuncunun seçtiği aktif gemiyi saklamak için
    public ShipSummaryDto ActiveShip { get; private set; }

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

    public void OnCharacterDataReceived(AccountDto characterData)
    {
        if (characterData == null)
        {
            Debug.LogError("Karakter verisi alınamadı veya eksik!");
            return;
        }

        this.CharacterData = characterData;
        if (characterData.Player != null)
            Debug.Log($"Hoşgeldin, {characterData.Player.Username}! Sahip olunan gemi sayısı: {PlayerShips.Count}");

        // --- AKILLI YÖNLENDİRME MANTIĞI ---
        // Oyuncunun hiç gemisi yok mu? (Yani yeni kayıt olmuş)
        if (PlayerShips == null || PlayerShips.Count == 0)
        {
            Debug.Log("Oyuncunun hiç gemisi yok. Doğrudan oyun sahnesine yönlendiriliyor...");
            // Yeni oyuncunun aktif bir gemisi olmayacağı için ActiveShip'i null yapıyoruz.
            this.ActiveShip = null;
            SceneManager.LoadScene("FisherSea");
        }
        else // Oyuncunun gemileri var mı?
        {
            Debug.Log("Oyuncunun gemileri var. Gemi seçim sahnesine yönlendiriliyor...");
            SceneManager.LoadScene("Scenes/SelectShipScene");
        }
    }

    /// <summary>
    /// ShipSelectionUI tarafından, oyuncu bir gemi seçtiğinde çağrılır.
    /// </summary>
    public void SetActiveShipAndEnterGame(ShipSummaryDto selectedShip)
    {
        this.ActiveShip = selectedShip;
        Debug.Log($"Aktif gemi seçildi: {selectedShip.Name} (ID: {selectedShip.Id}). Oyun sahnesi yükleniyor...");
        SceneManager.LoadScene("FisherSea");
    }

    public void OnAccountReceived(AccountDto accountData)
    {
        if (accountData == null)
        {
            Debug.LogError("Karakter verisi alınamadı veya eksik!");
            return;
        }

        this.CurrentAccount = accountData;
    }
}