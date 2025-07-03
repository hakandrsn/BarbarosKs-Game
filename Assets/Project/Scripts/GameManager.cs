using System;
using BarbarosKs.Shared.DTOs;
using UnityEngine;
using BarbarosKs.Core;

/// <summary>
/// **DEPRECATED** - Bu sınıf kullanım dışı!
/// 
/// Yeni sistemler:
/// - PlayerManager: Player ve ship yönetimi için
/// - SceneController: Sahne yönetimi için
/// - GameStateManager: Oyun durumu yönetimi için
/// 
/// Bu sınıf sadece eski uyumluluk için tutulmaktadır.
/// Yeni kodlarda kullanmayın!
/// </summary>
[System.Obsolete("GameManager deprecated! PlayerManager, SceneController ve GameStateManager kullanın")]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // **DEPRECATED** - PlayerManager kullanın
    [System.Obsolete("PlayerManager.Instance.PlayerProfile kullanın")]
    public CharacterSelectionDto CharacterData => GetCharacterDataFromPlayerManager();

    [System.Obsolete("PlayerManager.Instance.ActiveShip kullanın")]
    public ShipSummaryDto ActiveShip => PlayerManager.Instance?.ActiveShip;

    [System.Obsolete("PlayerManager.Instance.PlayerProfile kullanın")]
    public PlayerProfileDto CurrentPlayerProfile => PlayerManager.Instance?.PlayerProfile;
    
    [System.Obsolete("PlayerManager.Instance.GetPlayerId() kullanın")]
    public Guid? LocalPlayerId => PlayerManager.Instance?.GetPlayerId();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.LogWarning("⚠️ [DEPRECATED] GameManager kullanılıyor! Yeni sistemlere geçin:");
            Debug.LogWarning("   - PlayerManager: Player ve ship yönetimi");
            Debug.LogWarning("   - SceneController: Sahne yönetimi");
            Debug.LogWarning("   - GameStateManager: Oyun durumu yönetimi");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// **DEPRECATED** - PlayerManager.Instance.HandleLoginSuccess() kullanın
    /// </summary>
    [System.Obsolete("PlayerManager.Instance.HandleLoginSuccess() kullanın")]
    public void OnCharacterDataReceived(CharacterSelectionDto characterData)
    {
        Debug.LogWarning("⚠️ [DEPRECATED] GameManager.OnCharacterDataReceived deprecated! PlayerManager.HandleLoginSuccess() kullanın");
        
        // Yeni sisteme yönlendir
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.HandleLoginSuccess(characterData);
        }
        else
        {
            Debug.LogError("❌ PlayerManager bulunamadı! SystemCoordinator çalıştığından emin olun.");
        }
    }

    /// <summary>
    /// **DEPRECATED** - PlayerManager.Instance.HandleShipSelection() kullanın
    /// </summary>
    [System.Obsolete("PlayerManager.Instance.HandleShipSelection() kullanın")]
    public void SetActiveShipAndEnterGame(ShipSummaryDto selectedShip)
    {
        Debug.LogWarning("⚠️ [DEPRECATED] GameManager.SetActiveShipAndEnterGame deprecated! PlayerManager.HandleShipSelection() kullanın");
        
        // Yeni sisteme yönlendir
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.HandleShipSelection(selectedShip);
        }
        else
        {
            Debug.LogError("❌ PlayerManager bulunamadı! SystemCoordinator çalıştığından emin olun.");
        }
    }

    /// <summary>
    /// **DEPRECATED** - SceneController.Instance.LoadScene() kullanın
    /// </summary>
    [System.Obsolete("SceneController.Instance.LoadScene() kullanın")]
    public void ToScene(string scene)
    {
        Debug.LogWarning($"⚠️ [DEPRECATED] GameManager.ToScene deprecated! SceneController.LoadScene() kullanın - Scene: {scene}");
        
        // Yeni sisteme yönlendir
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene(scene);
        }
        else
        {
            Debug.LogError("❌ SceneController bulunamadı! SystemCoordinator çalıştığından emin olun.");
            
            // Fallback - eski method
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }
    }

    /// <summary>
    /// PlayerManager'dan CharacterSelectionDto oluşturur (Compatibility için)
    /// </summary>
    private CharacterSelectionDto GetCharacterDataFromPlayerManager()
    {
        if (PlayerManager.Instance == null) return null;

        var playerProfile = PlayerManager.Instance.PlayerProfile;
        var ships = PlayerManager.Instance.OwnedShips;

        if (playerProfile == null) return null;

        return new CharacterSelectionDto
        {
            PlayerProfile = playerProfile,
            Ships = ships
        };
    }

    /// <summary>
    /// Debug: Yeni sistemlere yönlendirme durumunu gösterir
    /// </summary>
    [ContextMenu("Debug: Show Migration Status")]
    private void DebugShowMigrationStatus()
    {
        Debug.Log("=== GAMEMANAGER MIGRATION STATUS ===");
        Debug.Log($"PlayerManager Available: {PlayerManager.Instance != null}");
        Debug.Log($"SceneController Available: {SceneController.Instance != null}");
        Debug.Log($"GameStateManager Available: {GameStateManager.Instance != null}");
        
        if (PlayerManager.Instance != null)
        {
            Debug.Log($"Player Data: {(PlayerManager.Instance.HasPlayerData ? "✅ Loaded" : "❌ Not Loaded")}");
            Debug.Log($"Active Ship: {(PlayerManager.Instance.HasActiveShip ? "✅ Set" : "❌ Not Set")}");
        }
        
        Debug.Log("=== MİGRATİON RECOMMENDATİON ===");
        Debug.Log("Bu GameManager'ı kullanmayı bırakın ve şu sistemleri kullanın:");
        Debug.Log("1. PlayerManager - Player ve ship yönetimi");
        Debug.Log("2. SceneController - Sahne yönetimi");
        Debug.Log("3. GameStateManager - Oyun durumu yönetimi");
    }

    /// <summary>
    /// Debug: Eski kodları yeni sistemlere yönlendirir
    /// </summary>
    [ContextMenu("Debug: Test New System Integration")]
    private void DebugTestNewSystemIntegration()
    {
        Debug.Log("🔄 Testing new system integration...");
        
        // Test PlayerManager
        if (PlayerManager.Instance != null)
        {
            Debug.Log("✅ PlayerManager available");
            if (PlayerManager.Instance.HasPlayerData)
            {
                Debug.Log($"   Player: {PlayerManager.Instance.PlayerProfile.Username}");
                Debug.Log($"   Ships: {PlayerManager.Instance.ShipCount}");
            }
        }
        
        // Test SceneController
        if (SceneController.Instance != null)
        {
            Debug.Log("✅ SceneController available");
        }
        
        // Test GameStateManager
        if (GameStateManager.Instance != null)
        {
            Debug.Log($"✅ GameStateManager available - State: {GameStateManager.Instance.CurrentState}");
        }
    }
}