using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using UnityEngine;

/// <summary>
/// Gülle (Cannonball) verilerini API'dan çeken ve yöneten servis.
/// Caching, filtering ve market operasyonları içerir.
/// </summary>
public class CannonballService : MonoBehaviour
{
    public static CannonballService Instance { get; private set; }

    [Header("Cache Ayarları")]
    [SerializeField] private bool _enableCaching = true;
    [SerializeField] private float _cacheExpirationMinutes = 30f;

    [Header("Durumu")]
    [SerializeField] private bool _isLoading;
    [SerializeField] private bool _hasCache;
    [SerializeField] private DateTime _lastCacheTime;

    // Events
    public static event Action<List<CannonballTypeDto>> OnCannonballsLoaded;
    public static event Action<string> OnLoadError;

    // Cache verileri
    private List<CannonballTypeDto> _cachedCannonballs = new();
    private DateTime _cacheTimestamp;

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

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ CannonballService başlatıldı");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Debug için Inspector'da göstermek
        _hasCache = HasCache;
        _lastCacheTime = _cacheTimestamp;
    }

    #region Public API Methods

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
            Debug.Log("🔄 CannonballService: Gülle verileri API'dan alınıyor...");

            var response = await ApiManager.Instance.GetRequest<ApiResponseDto<List<CannonballTypeDto>>>(
                "/Cannonballs");

            if (response != null && response.Success && response.Data != null)
            {
                Debug.Log($"✅ CannonballService: {response.Data.Count} gülle verisi alındı");
                
                // Cache'i güncelle
                UpdateCache(response.Data);
                
                // GameDataManager'a verileri gönder
                if (GameDataManager.Instance != null)
                {
                    GameDataManager.Instance.LoadCannonballs(response.Data);
                }

                OnCannonballsLoaded?.Invoke(response.Data);
                return response.Data;
            }
            else
            {
                var errorMsg = $"Gülle verileri alınamadı: {response?.Message ?? "Bilinmeyen hata"}";
                Debug.LogError($"❌ CannonballService: {errorMsg}");
                OnLoadError?.Invoke(errorMsg);
                return new List<CannonballTypeDto>();
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"API hatası (LoadCannonballs): {ex.Message}";
            Debug.LogError($"❌ CannonballService: {errorMsg}");
            OnLoadError?.Invoke(errorMsg);
            return new List<CannonballTypeDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Cache'i günceller
    /// </summary>
    private void UpdateCache(List<CannonballTypeDto> cannonballs)
    {
        if (!_enableCaching) return;

        _cachedCannonballs = new List<CannonballTypeDto>(cannonballs);
        _cacheTimestamp = DateTime.Now;
        _hasCache = true;

        Debug.Log($"📦 CannonballService: Cache güncellendi - {cannonballs.Count} gülle");
    }

    /// <summary>
    /// Cache'i temizler
    /// </summary>
    public void ClearCache()
    {
        _cachedCannonballs.Clear();
        _hasCache = false;
        _cacheTimestamp = default;
        Debug.Log("🧹 CannonballService: Cache temizlendi");
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug: Load Cannonballs Now")]
    private async void DebugLoadCannonballs()
    {
        await GetAllCannonballsAsync(forceRefresh: true);
    }

    [ContextMenu("Debug: Clear Cache")]
    private void DebugClearCache()
    {
        ClearCache();
    }

    [ContextMenu("Debug: Log Cache Info")]
    private void DebugLogCacheInfo()
    {
        Debug.Log("=== CANNONBALL CACHE DEBUG ===");
        Debug.Log($"Has Cache: {HasCache}");
        Debug.Log($"Cache Expired: {IsCacheExpired}");
        Debug.Log($"Cache Count: {_cachedCannonballs.Count}");
        Debug.Log($"Cache Time: {_cacheTimestamp}");
    }

    #endregion
} 