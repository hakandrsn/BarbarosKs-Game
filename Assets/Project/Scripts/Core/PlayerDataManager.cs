using System;
using BarbarosKs.Shared.DTOs;
using UnityEngine;

/// <summary>
/// Oyuncunun tüm verilerini merkezi olarak yöneten sistem.
/// UI'lar bu sınıftan verilere erişir.
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [Header("Oyuncu Verileri")]
    [SerializeField] private bool _hasPlayerData;
    [SerializeField] private bool _hasActiveShip;
    [SerializeField] private bool _hasDetailedShipData;

    // Events - UI'ların dinlemesi için
    public static event Action<CharacterSelectionDto> OnPlayerDataLoaded;
    public static event Action<ShipDetailDto> OnActiveShipDataLoaded;
    public static event Action OnPlayerDataCleared;

    // Ana veriler
    private CharacterSelectionDto _characterData;
    private ShipSummaryDto _activeShip;
    private ShipDetailDto _activeShipDetails;

    #region Properties - UI'ların erişeceği veriler

    /// <summary>Oyuncu profil verileri (username, id vb.)</summary>
    public PlayerProfileDto PlayerProfile => _characterData?.PlayerProfile;

    /// <summary>Oyuncunun sahip olduğu tüm gemiler</summary>
    public System.Collections.Generic.List<ShipSummaryDto> OwnedShips => _characterData?.Ships;

    /// <summary>Seçili olan gemi (özet bilgiler)</summary>
    public ShipSummaryDto ActiveShip => _activeShip;

    /// <summary>Seçili geminin detaylı verileri</summary>
    public ShipDetailDto ActiveShipDetails => _activeShipDetails;

    /// <summary>Oyuncu verileri yüklenmiş mi?</summary>
    public bool HasPlayerData => _characterData != null;

    /// <summary>Aktif gemi seçilmiş mi?</summary>
    public bool HasActiveShip => _activeShip != null;

    /// <summary>Aktif geminin detaylı verileri yüklenmiş mi?</summary>
    public bool HasDetailedShipData => _activeShipDetails != null;

    /// <summary>Oyuncunun kullanıcı adı (hızlı erişim)</summary>
    public string Username => PlayerProfile?.Username ?? "Unknown";

    /// <summary>Aktif geminin adı (hızlı erişim)</summary>
    public string ActiveShipName => ActiveShip?.Name ?? "No Ship";

    /// <summary>Aktif geminin seviyesi (hızlı erişim)</summary>
    public int ActiveShipLevel => ActiveShip?.Level ?? 0;

    /// <summary>Aktif geminin altını (hızlı erişim)</summary>
    public int ActiveShipGold => ActiveShipDetails?.Gold ?? 0;

    /// <summary>Aktif geminin deneyimi (hızlı erişim)</summary>
    public int ActiveShipExperience => ActiveShipDetails?.Experience ?? 0;

    /// <summary>Aktif geminin can durumu (hızlı erişim)</summary>
    public (int current, int max) ActiveShipHealth => 
        ActiveShip != null ? (ActiveShip.CurrentHull, ActiveShip.MaxHull) : (0, 0);

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ PlayerDataManager başlatıldı");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Debug için Inspector'da göstermek
        _hasPlayerData = HasPlayerData;
        _hasActiveShip = HasActiveShip;
        _hasDetailedShipData = HasDetailedShipData;
    }

    #region Public Methods

    /// <summary>
    /// Login sonrası oyuncu verilerini yükler
    /// </summary>
    public void LoadPlayerData(CharacterSelectionDto characterData)
    {
        if (characterData == null)
        {
            Debug.LogError("❌ PlayerDataManager: CharacterData null!");
            return;
        }

        _characterData = characterData;
        Debug.Log($"✅ PlayerDataManager: Oyuncu verileri yüklendi - {characterData.PlayerProfile.Username}");
        Debug.Log($"📊 Sahip olunan gemiler: {characterData.Ships?.Count ?? 0}");

        OnPlayerDataLoaded?.Invoke(characterData);
    }

    /// <summary>
    /// Aktif gemi seçildikten sonra çağrılır
    /// </summary>
    public void SetActiveShip(ShipSummaryDto ship)
    {
        if (ship == null)
        {
            Debug.LogError("❌ PlayerDataManager: Ship null!");
            return;
        }

        _activeShip = ship;
        _activeShipDetails = null; // Detaylı veriler henüz yüklenmedi
        
        Debug.Log($"✅ PlayerDataManager: Aktif gemi ayarlandı - {ship.Name} (Level {ship.Level})");
    }

    /// <summary>
    /// API'dan gelen detaylı gemi verilerini yükler
    /// </summary>
    public void LoadActiveShipDetails(ShipDetailDto shipDetails)
    {
        if (shipDetails == null)
        {
            Debug.LogError("❌ PlayerDataManager: ShipDetails null!");
            return;
        }

        _activeShipDetails = shipDetails;
        
        // ActiveShip verilerini de güncelle (çünkü DetailDto, SummaryDto'dan türüyor)
        if (_activeShip != null && _activeShip.Id == shipDetails.Id)
        {
            _activeShip.Name = shipDetails.Name;
            _activeShip.Level = shipDetails.Level;
            _activeShip.CurrentHull = shipDetails.CurrentHull;
            _activeShip.MaxHull = shipDetails.MaxHull;
            _activeShip.IsActive = shipDetails.IsActive;
        }

        Debug.Log($"✅ PlayerDataManager: Detaylı gemi verileri yüklendi");
        Debug.Log($"📊 Altın: {shipDetails.Gold}, Deneyim: {shipDetails.Experience}/{shipDetails.RequiredExperienceForNextLevel}");
        Debug.Log($"📊 Can: {shipDetails.CurrentHull}/{shipDetails.MaxHull}");

        OnActiveShipDataLoaded?.Invoke(shipDetails);
    }

    /// <summary>
    /// Tüm verileri temizler (logout işlemi için)
    /// </summary>
    public void ClearAllData()
    {
        _characterData = null;
        _activeShip = null;
        _activeShipDetails = null;
        
        Debug.Log("🧹 PlayerDataManager: Tüm veriler temizlendi");
        OnPlayerDataCleared?.Invoke();
    }

    /// <summary>
    /// Belirli bir gemi ID'si ile geminin sahibi olup olmadığını kontrol eder
    /// </summary>
    public bool OwnsShip(Guid shipId)
    {
        return OwnedShips?.Exists(ship => ship.Id == shipId) ?? false;
    }

    /// <summary>
    /// Oyuncunun sahip olduğu gemilerden ID ile arama yapar
    /// </summary>
    public ShipSummaryDto GetOwnedShip(Guid shipId)
    {
        return OwnedShips?.Find(ship => ship.Id == shipId);
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug: Log All Data")]
    private void DebugLogAllData()
    {
        Debug.Log("=== PLAYER DATA MANAGER DEBUG ===");
        Debug.Log($"Player: {(HasPlayerData ? Username : "NO DATA")}");
        Debug.Log($"Ships Count: {OwnedShips?.Count ?? 0}");
        Debug.Log($"Active Ship: {(HasActiveShip ? $"{ActiveShipName} (Lv.{ActiveShipLevel})" : "NONE")}");
        Debug.Log($"Detailed Data: {(HasDetailedShipData ? "LOADED" : "NOT LOADED")}");
        
        if (HasDetailedShipData)
        {
            Debug.Log($"Gold: {ActiveShipGold}, XP: {ActiveShipExperience}");
            Debug.Log($"Health: {ActiveShipHealth.current}/{ActiveShipHealth.max}");
        }
    }

    #endregion
} 