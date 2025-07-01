using UnityEngine;
using TMPro;
using BarbarosKs.Shared.DTOs;

/// <summary>
/// PlayerDataManager'dan veri çekerek oyuncu bilgilerini gösteren örnek UI component'i
/// </summary>
public class PlayerInfoDisplay : MonoBehaviour
{
    [Header("UI Referansları")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI activeShipNameText;
    [SerializeField] private TextMeshProUGUI shipLevelText;
    [SerializeField] private TextMeshProUGUI shipHealthText;
    [SerializeField] private TextMeshProUGUI shipGoldText;
    [SerializeField] private TextMeshProUGUI shipExperienceText;
    
    [Header("Debug")]
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float updateInterval = 1f;

    private void Start()
    {
        // Event'leri dinle
        PlayerDataManager.OnPlayerDataLoaded += OnPlayerDataLoaded;
        PlayerDataManager.OnActiveShipDataLoaded += OnActiveShipDataLoaded;
        PlayerDataManager.OnPlayerDataCleared += OnPlayerDataCleared;
        
        // Eğer veri zaten yüklüyse, hemen güncelle
        UpdateUI();
        
        // Otomatik güncelleme
        if (autoUpdate)
        {
            InvokeRepeating(nameof(UpdateUI), updateInterval, updateInterval);
        }
    }

    private void OnDestroy()
    {
        // Event'leri temizle
        PlayerDataManager.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        PlayerDataManager.OnActiveShipDataLoaded -= OnActiveShipDataLoaded;
        PlayerDataManager.OnPlayerDataCleared -= OnPlayerDataCleared;
    }

    private void OnPlayerDataLoaded(CharacterSelectionDto characterData)
    {
        Debug.Log("📊 PlayerInfoDisplay: Player data yüklendi, UI güncelleniyor");
        UpdateUI();
    }

    private void OnActiveShipDataLoaded(ShipDetailDto shipDetails)
    {
        Debug.Log("📊 PlayerInfoDisplay: Ship details yüklendi, UI güncelleniyor");
        UpdateUI();
    }

    private void OnPlayerDataCleared()
    {
        Debug.Log("📊 PlayerInfoDisplay: Player data temizlendi, UI sıfırlanıyor");
        ClearUI();
    }

    [ContextMenu("Update UI")]
    private void UpdateUI()
    {
        if (PlayerDataManager.Instance == null) return;

        // Player bilgileri
        if (playerNameText != null)
        {
            playerNameText.text = PlayerDataManager.Instance.HasPlayerData ? 
                PlayerDataManager.Instance.Username : "No Player";
        }

        // Active Ship bilgileri
        if (activeShipNameText != null)
        {
            activeShipNameText.text = PlayerDataManager.Instance.HasActiveShip ? 
                PlayerDataManager.Instance.ActiveShipName : "No Ship Selected";
        }

        if (shipLevelText != null)
        {
            shipLevelText.text = PlayerDataManager.Instance.HasActiveShip ? 
                $"Level {PlayerDataManager.Instance.ActiveShipLevel}" : "Level --";
        }

        // Detaylı ship bilgileri (varsa)
        var health = PlayerDataManager.Instance.ActiveShipHealth;
        if (shipHealthText != null)
        {
            if (health.max > 0)
            {
                float percentage = (float)health.current / health.max * 100f;
                shipHealthText.text = $"HP: {health.current}/{health.max} ({percentage:F1}%)";
            }
            else
            {
                shipHealthText.text = "HP: --/--";
            }
        }

        if (shipGoldText != null)
        {
            shipGoldText.text = PlayerDataManager.Instance.HasDetailedShipData ? 
                $"Gold: {PlayerDataManager.Instance.ActiveShipGold}" : "Gold: --";
        }

        if (shipExperienceText != null)
        {
            shipExperienceText.text = PlayerDataManager.Instance.HasDetailedShipData ? 
                $"XP: {PlayerDataManager.Instance.ActiveShipExperience}" : "XP: --";
        }
    }

    private void ClearUI()
    {
        if (playerNameText != null) playerNameText.text = "No Player";
        if (activeShipNameText != null) activeShipNameText.text = "No Ship Selected";
        if (shipLevelText != null) shipLevelText.text = "Level --";
        if (shipHealthText != null) shipHealthText.text = "HP: --/--";
        if (shipGoldText != null) shipGoldText.text = "Gold: --";
        if (shipExperienceText != null) shipExperienceText.text = "XP: --";
    }
} 