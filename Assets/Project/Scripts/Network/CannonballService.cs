using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Modern MMO Cannonball/Ammo Service
/// GameServer AmmoManager ile real-time sync
/// Client-side ammo tracking, reload management ve combat integration
/// </summary>
public class CannonballService : MonoBehaviour
{
    public static CannonballService Instance { get; private set; }

    [Header("MMO Configuration")]
    [SerializeField] private bool enableRealTimeSync = true;
    [SerializeField] private bool enableLocalCaching = true;
    [SerializeField] private float realTimeSyncInterval = 1f;
    [SerializeField] private float reloadSoundDelay = 0.5f;

    [Header("Cache Ayarları")]
    [SerializeField] private bool _enableCaching = true;
    [SerializeField] private float _cacheExpirationMinutes = 30f;

    [Header("Real-Time Status")]
    [SerializeField] private bool _isLoading;
    [SerializeField] private bool _hasCache;
    [SerializeField] private DateTime _lastCacheTime;
    [SerializeField] private bool _isConnectedToServer;

    // Events - Modern MMO pattern
    public static event Action<List<CannonballTypeDto>> OnCannonballsLoaded;
    public static event Action<AmmoStatus> OnAmmoStatusUpdated; // GameServer sync
    public static event Action<string> OnLoadError;
    public static event Action<int> OnCannonballTypeChanged;
    public static event Action OnReloadStarted;
    public static event Action OnReloadCompleted;
    public static event Action<float> OnReloadProgress; // 0-1 progress

    // Cache verileri
    private List<CannonballTypeDto> _cachedCannonballs = new();
    private DateTime _cacheTimestamp;

    // Real-time MMO data - GameServer sync
    private AmmoStatus _currentAmmoStatus;
    private Dictionary<int, CannonballTypeDto> _cannonballTypes = new();
    private float _lastRealTimeSync;
    private bool _isReloading = false;
    private float _reloadStartTime;
    private float _reloadDuration = 3f;

    #region Properties

    /// <summary>Veriler yükleniyor mu?</summary>
    public bool IsLoading => _isLoading;

    /// <summary>Cache'de veri var mı?</summary>
    public bool HasCache => _hasCache && _cachedCannonballs.Count > 0;

    /// <summary>Cache süresi dolmuş mu?</summary>
    public bool IsCacheExpired
    {
        get
        {
            if (!_hasCache) return true;
            return DateTime.Now.Subtract(_cacheTimestamp).TotalMinutes > _cacheExpirationMinutes;
        }
    }

    /// <summary>GameServer'dan gelen real-time ammo status</summary>
    public AmmoStatus CurrentAmmoStatus => _currentAmmoStatus;

    /// <summary>Reload durumu</summary>
    public bool IsReloading => _isReloading;

    /// <summary>Reload progress (0-1)</summary>
    public float ReloadProgress
    {
        get
        {
            if (!_isReloading) return 1f;
            float elapsed = Time.time - _reloadStartTime;
            return Mathf.Clamp01(elapsed / _reloadDuration);
        }
    }

    /// <summary>Kalan reload süresi (saniye)</summary>
    public float RemainingReloadTime
    {
        get
        {
            if (!_isReloading) return 0f;
            float elapsed = Time.time - _reloadStartTime;
            return Mathf.Max(0f, _reloadDuration - elapsed);
        }
    }

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ Modern MMO CannonballService başlatıldı");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // NetworkManager events'lerine subscribe ol
        SubscribeToNetworkEvents();
    }

    private void Update()
    {
        // Debug için Inspector'da göstermek
        _hasCache = HasCache;
        _lastCacheTime = _cacheTimestamp;
        _isConnectedToServer = Project.Scripts.Network.NetworkManager.Instance?.IsConnected ?? false;

        // Real-time updates
        HandleRealTimeUpdates();

        // Reload progress tracking
        HandleReloadProgress();
    }

    private void SubscribeToNetworkEvents()
    {
        if (Project.Scripts.Network.NetworkManager.Instance != null)
        {
            // NetworkManager'daki ammo events'lerine subscribe ol
            // Modern MMO real-time sync için
        }
    }

    private void HandleRealTimeUpdates()
    {
        if (!enableRealTimeSync || !_isConnectedToServer) return;

        // Real-time sync interval
        if (Time.time - _lastRealTimeSync >= realTimeSyncInterval)
        {
            RequestRealTimeAmmoStatus();
            _lastRealTimeSync = Time.time;
        }
    }

    private void HandleReloadProgress()
    {
        if (_isReloading)
        {
            OnReloadProgress?.Invoke(ReloadProgress);

            // Reload tamamlandı mı?
            if (ReloadProgress >= 1f)
            {
                CompleteReload();
            }
        }
    }

    #region Public MMO API Methods

    /// <summary>
    /// GameServer'dan real-time ammo status iste
    /// </summary>
    public void RequestRealTimeAmmoStatus()
    {
        if (!_isConnectedToServer) return;

        try
        {
            var request = new
            {
                Type = "REQUEST_AMMO_STATUS",
                PlayerId = GetLocalPlayerId(),
                ShipId = GetLocalShipId(),
                Timestamp = DateTime.UtcNow
            };

            // NetworkManager üzerinden GameServer'a gönder
            SendMessageToGameServer(request);
            Debug.Log("🎯 Real-time ammo status istendi");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Real-time ammo request failed: {ex.Message}");
        }
    }

    /// <summary>
    /// GameServer'dan gelen ammo status update'ini işle
    /// </summary>
    public void HandleAmmoStatusUpdate(AmmoStatus ammoStatus)
    {
        _currentAmmoStatus = ammoStatus;

        // Reload durumunu sync et
        if (ammoStatus.IsReloading && !_isReloading)
        {
            StartReload(ammoStatus.ReloadTimeRemaining);
        }
        else if (!ammoStatus.IsReloading && _isReloading)
        {
            CompleteReload();
        }

        OnAmmoStatusUpdated?.Invoke(ammoStatus);
        Debug.Log($"🎯 Ammo status güncellendi: {ammoStatus.CurrentAmmo}/{ammoStatus.TotalAmmo} ({ammoStatus.AmmoPercentage:F1}%)");
    }

    /// <summary>
    /// Attack action gönder (ammo consumption için)
    /// </summary>
    public async Task<bool> TryAttackAsync(Guid targetId, Vector3 position)
    {
        if (!_isConnectedToServer)
        {
            Debug.LogWarning("⚠️ Sunucuya bağlı değil, attack gönderilemedi");
            return false;
        }

        if (_isReloading)
        {
            Debug.LogWarning("⚠️ Reload sırasında attack yapılamaz");
            return false;
        }

        try
        {
            var attackAction = new
            {
                Type = "ATTACK_ACTION",
                AttackType = "PRIMARY",
                AttackerId = GetLocalPlayerId(),
                TargetId = targetId,
                Position = position,
                ShipId = GetLocalShipId(),
                Timestamp = DateTime.UtcNow
            };

            SendMessageToGameServer(attackAction);
            Debug.Log($"⚔️ Attack action gönderildi: {targetId}");

            // Ammo status güncellenmesini bekle
            RequestRealTimeAmmoStatus();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Attack action failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Cannonball type değiştir
    /// </summary>
    public async Task<bool> ChangeCannonballTypeAsync(int newCannonballTypeId)
    {
        if (!_isConnectedToServer) return false;

        try
        {
            var changeRequest = new
            {
                Type = "CHANGE_CANNONBALL_TYPE",
                PlayerId = GetLocalPlayerId(),
                ShipId = GetLocalShipId(),
                NewCannonballTypeId = newCannonballTypeId,
                Timestamp = DateTime.UtcNow
            };

            SendMessageToGameServer(changeRequest);
            OnCannonballTypeChanged?.Invoke(newCannonballTypeId);
            
            // Ammo status güncellenmesini bekle
            await Task.Delay(100);
            RequestRealTimeAmmoStatus();
            
            Debug.Log($"🔄 Cannonball type değiştirildi: {newCannonballTypeId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Cannonball type change failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Manual reload başlat
    /// </summary>
    public async Task<bool> StartManualReloadAsync()
    {
        if (_isReloading)
        {
            Debug.LogWarning("⚠️ Zaten reload sırasında");
            return false;
        }

        try
        {
            var reloadRequest = new
            {
                Type = "MANUAL_RELOAD",
                PlayerId = GetLocalPlayerId(),
                ShipId = GetLocalShipId(),
                Timestamp = DateTime.UtcNow
            };

            SendMessageToGameServer(reloadRequest);
            StartReload(_reloadDuration);
            
            Debug.Log("🔄 Manual reload başlatıldı");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Manual reload failed: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Legacy API Methods (Backward Compatibility)

    /// <summary>
    /// Tüm gülleleri API'dan getirir veya cache'den döner
    /// </summary>
    public async Task<List<CannonballTypeDto>> GetAllCannonballsAsync(bool forceRefresh = false)
    {
        // Cache kontrolü
        if (!forceRefresh && _enableCaching && HasCache && !IsCacheExpired)
        {
            Debug.Log("📦 CannonballService: Cache'den veriler döndürülüyor");
            return new List<CannonballTypeDto>(_cachedCannonballs);
        }

        return await LoadCannonballsFromAPI();
    }

    /// <summary>
    /// Market'te satılan gülleleri getirir
    /// </summary>
    public async Task<List<CannonballTypeDto>> GetMarketCannonballsAsync(bool forceRefresh = false)
    {
        var allCannonballs = await GetAllCannonballsAsync(forceRefresh);
        return allCannonballs.FindAll(c => c.IsActive && c.PurchasePrice > 0);
    }

    /// <summary>
    /// Oyuncunun sahip olduğu gülleleri getirir
    /// </summary>
    public async Task<List<PlayerCannonballDto>> GetPlayerCannonballsAsync()
    {
        if (!ApiManager.Instance.IsLoggedIn)
        {
            Debug.LogError("❌ CannonballService: Kullanıcı giriş yapmamış!");
            return new List<PlayerCannonballDto>();
        }

        try
        {
            _isLoading = true;
            Debug.Log("🔄 CannonballService: Oyuncu gülleleri API'dan alınıyor...");

            var response = await ApiManager.Instance.GetRequest<ApiResponseDto<List<PlayerCannonballDto>>>(
                "/Players/cannonballs");

            if (response != null && response.Success && response.Data != null)
            {
                Debug.Log($"✅ CannonballService: {response.Data.Count} oyuncu güllesi alındı");
                return response.Data;
            }
            else
            {
                var errorMsg = $"Oyuncu gülleleri alınamadı: {response?.Message ?? "Bilinmeyen hata"}";
                Debug.LogError($"❌ CannonballService: {errorMsg}");
                OnLoadError?.Invoke(errorMsg);
                return new List<PlayerCannonballDto>();
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"API hatası (GetPlayerCannonballs): {ex.Message}";
            Debug.LogError($"❌ CannonballService: {errorMsg}");
            OnLoadError?.Invoke(errorMsg);
            return new List<PlayerCannonballDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Gülle satın alma işlemi
    /// </summary>
    public async Task<bool> PurchaseCannonballAsync(Guid cannonballId, int quantity = 1)
    {
        if (!ApiManager.Instance.IsLoggedIn)
        {
            Debug.LogError("❌ CannonballService: Kullanıcı giriş yapmamış!");
            return false;
        }

        try
        {
            _isLoading = true;
            Debug.Log($"🛒 CannonballService: Gülle satın alınıyor... ID: {cannonballId}, Adet: {quantity}");

            var purchaseRequest = new
            {
                CannonballId = cannonballId,
                Quantity = quantity
            };

            var response = await ApiManager.Instance.PostRequest<ApiResponseDto<object>>(
                "/Market/purchase-cannonball", purchaseRequest);

            if (response != null && response.Success)
            {
                Debug.Log($"✅ CannonballService: Gülle başarıyla satın alındı!");
                
                // Real-time ammo status güncelle
                if (_isConnectedToServer)
                {
                    RequestRealTimeAmmoStatus();
                }
                
                return true;
            }
            else
            {
                var errorMsg = $"Gülle satın alınamadı: {response?.Message ?? "Bilinmeyen hata"}";
                Debug.LogError($"❌ CannonballService: {errorMsg}");
                OnLoadError?.Invoke(errorMsg);
                return false;
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"API hatası (PurchaseCannonball): {ex.Message}";
            Debug.LogError($"❌ CannonballService: {errorMsg}");
            OnLoadError?.Invoke(errorMsg);
            return false;
        }
        finally
        {
            _isLoading = false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// API'dan gülle verilerini çeker
    /// </summary>
    private async Task<List<CannonballTypeDto>> LoadCannonballsFromAPI()
    {
        try
        {
            _isLoading = true;
            Debug.Log("🔄 CannonballService: Gülle verileri API'dan yükleniyor...");

            var response = await ApiManager.Instance.GetRequest<ApiResponseDto<List<CannonballTypeDto>>>(
                "/Cannonballs");

            if (response != null && response.Success && response.Data != null)
            {
                UpdateCache(response.Data);
                OnCannonballsLoaded?.Invoke(response.Data);
                Debug.Log($"✅ CannonballService: {response.Data.Count} gülle tipi yüklendi");
                return response.Data;
            }
            else
            {
                var errorMsg = $"Gülle verileri alınamadı: {response?.Message ?? "API yanıt hatası"}";
                Debug.LogError($"❌ CannonballService: {errorMsg}");
                OnLoadError?.Invoke(errorMsg);
                return new List<CannonballTypeDto>();
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"API bağlantı hatası: {ex.Message}";
            Debug.LogError($"❌ CannonballService: {errorMsg}");
            OnLoadError?.Invoke(errorMsg);
            return new List<CannonballTypeDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateCache(List<CannonballTypeDto> cannonballs)
    {
        _cachedCannonballs = new List<CannonballTypeDto>(cannonballs);
        _cacheTimestamp = DateTime.Now;
        _hasCache = true;

        // Dictionary'ye de ekle (hızlı erişim için)
        _cannonballTypes.Clear();
        foreach (var cannonball in cannonballs)
        {
            _cannonballTypes[cannonball.Id] = cannonball;
        }

        Debug.Log($"📦 CannonballService: Cache güncellendi ({cannonballs.Count} item)");
    }

    private void StartReload(double reloadTimeSeconds)
    {
        _isReloading = true;
        _reloadStartTime = Time.time;
        _reloadDuration = (float)reloadTimeSeconds;
        
        OnReloadStarted?.Invoke();
        Debug.Log($"🔄 Reload başladı ({_reloadDuration:F1}s)");
    }

    private void CompleteReload()
    {
        _isReloading = false;
        OnReloadCompleted?.Invoke();
        Debug.Log("✅ Reload tamamlandı");
    }

    private void SendMessageToGameServer(object message)
    {
        try
        {
            // NetworkManager üzerinden mesaj gönder
            string json = JsonConvert.SerializeObject(message);
            // Project.Scripts.Network.NetworkManager.Instance.SendMessage(json);
            // Bu method'u NetworkManager'da implement etmek gerekecek
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ GameServer message send failed: {ex.Message}");
        }
    }

    private Guid GetLocalPlayerId()
    {
        return BarbarosKs.Core.PlayerManager.Instance?.PlayerProfile?.Id ?? Guid.Empty;
    }

    private Guid GetLocalShipId()
    {
        return BarbarosKs.Core.PlayerManager.Instance?.ActiveShip?.Id ?? Guid.Empty;
    }

    #endregion

    #region Public Cache Methods

    public void ClearCache()
    {
        _cachedCannonballs.Clear();
        _cannonballTypes.Clear();
        _hasCache = false;
        _cacheTimestamp = DateTime.MinValue;
        Debug.Log("🗑️ CannonballService: Cache temizlendi");
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug: Load Cannonballs Now")]
    private async void DebugLoadCannonballs()
    {
        await GetAllCannonballsAsync(true);
    }

    [ContextMenu("Debug: Clear Cache")]
    private void DebugClearCache()
    {
        ClearCache();
    }

    [ContextMenu("Debug: Request Real-Time Ammo")]
    private void DebugRequestRealTimeAmmo()
    {
        RequestRealTimeAmmoStatus();
    }

    [ContextMenu("Debug: Log Ammo Info")]
    private void DebugLogAmmoInfo()
    {
        if (_currentAmmoStatus != null)
        {
            Debug.Log($"📊 Current Ammo: {_currentAmmoStatus.CurrentAmmo}/{_currentAmmoStatus.TotalAmmo} " +
                     $"({_currentAmmoStatus.AmmoPercentage:F1}%) - Selected Type: {_currentAmmoStatus.SelectedCannonballType} " +
                     $"- Reloading: {_currentAmmoStatus.IsReloading} ({_currentAmmoStatus.ReloadTimeRemaining:F1}s)");
        }
        else
        {
            Debug.Log("❌ No ammo status available");
        }
    }

    #endregion
} 