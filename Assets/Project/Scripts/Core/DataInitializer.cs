using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyun başlatıldığında tüm veri sistemlerini başlatan merkezi sistem.
/// Sıralı veri yükleme ve hata yönetimi sağlar.
/// </summary>
public class DataInitializer : MonoBehaviour
{
    public static DataInitializer Instance { get; private set; }

    [Header("Yükleme Sırası")]
    [SerializeField] private bool _loadCannonballsOnStart = true;
    [SerializeField] private bool _loadItemsOnStart = false; // Gelecekte kullanılacak
    [SerializeField] private bool _autoRetryOnFailure = true;
    [SerializeField] private int _maxRetryAttempts = 3;
    [SerializeField] private float _retryDelaySeconds = 2f;

    [Header("Durum")]
    [SerializeField] private bool _isInitializing;
    [SerializeField] private bool _isInitialized;
    [SerializeField] private string _currentStatus = "Bekleniyor";

    // Events
    public static event Action OnInitializationStarted;
    public static event Action OnInitializationCompleted;
    public static event Action<string> OnInitializationFailed;
    public static event Action<string, float> OnProgressUpdated; // Status, Progress (0-1)

    #region Properties

    /// <summary>Başlatma işlemi devam ediyor mu?</summary>
    public bool IsInitializing => _isInitializing;

    /// <summary>Tüm veriler yüklendi mi?</summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>Mevcut durum açıklaması</summary>
    public string CurrentStatus => _currentStatus;

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ DataInitializer başlatıldı");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Scene'e göre otomatik başlatma
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (currentScene == "CreateShip" || currentScene == "FisherSea")
        {
            Debug.Log($"🎮 DataInitializer: {currentScene} scene'inde otomatik başlatma");
            _ = StartDataInitializationAsync();
        }
    }

    #region Public Methods

    /// <summary>
    /// Veri yükleme işlemini başlatır
    /// </summary>
    public async Task<bool> StartDataInitializationAsync()
    {
        if (_isInitializing)
        {
            Debug.LogWarning("⚠️ DataInitializer: Başlatma zaten devam ediyor!");
            return false;
        }

        if (_isInitialized)
        {
            Debug.Log("✅ DataInitializer: Veriler zaten yüklü");
            return true;
        }

        Debug.Log("🚀 DataInitializer: Veri başlatma işlemi başladı");
        _isInitializing = true;
        _isInitialized = false;
        OnInitializationStarted?.Invoke();

        try
        {
            await InitializeGameDataSystems();
            
            _isInitialized = true;
            _currentStatus = "Tamamlandı";
            OnProgressUpdated?.Invoke(_currentStatus, 1.0f);
            OnInitializationCompleted?.Invoke();
            
            Debug.Log("🎉 DataInitializer: Tüm veriler başarıyla yüklendi!");
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Veri yükleme hatası: {ex.Message}";
            Debug.LogError($"❌ DataInitializer: {errorMsg}");
            
            _currentStatus = $"Hata: {ex.Message}";
            OnProgressUpdated?.Invoke(_currentStatus, 0f);
            OnInitializationFailed?.Invoke(errorMsg);
            
            return false;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    /// <summary>
    /// Verileri temizler ve yeniden başlatır
    /// </summary>
    public async Task<bool> ResetAndReinitializeAsync()
    {
        Debug.Log("🔄 DataInitializer: Yeniden başlatma...");
        
        // Mevcut verileri temizle
        ClearAllData();
        
        // Yeniden başlat
        return await StartDataInitializationAsync();
    }

    /// <summary>
    /// Tüm verileri temizler
    /// </summary>
    public void ClearAllData()
    {
        _isInitialized = false;
        _currentStatus = "Temizlendi";
        
        // Veri sistemlerini temizle
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ClearAllData();
        }
        
        if (CannonballService.Instance != null)
        {
            CannonballService.Instance.ClearCache();
        }
        
        Debug.Log("🧹 DataInitializer: Tüm veriler temizlendi");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Oyun veri sistemlerini sırayla başlatır
    /// </summary>
    private async Task InitializeGameDataSystems()
    {
        float totalSteps = GetTotalSteps();
        float currentStep = 0;

        // 1. Cannonball verilerini yükle
        if (_loadCannonballsOnStart)
        {
            currentStep++;
            _currentStatus = "Gülle verileri yükleniyor...";
            OnProgressUpdated?.Invoke(_currentStatus, currentStep / totalSteps);
            
            await LoadCannonballsWithRetry();
        }

        // 2. Item verilerini yükle (gelecekte)
        if (_loadItemsOnStart)
        {
            currentStep++;
            _currentStatus = "Item verileri yükleniyor...";
            OnProgressUpdated?.Invoke(_currentStatus, currentStep / totalSteps);
            
            // await LoadItemsWithRetry();
            Debug.Log("📦 Item loading henüz implement edilmedi");
        }

        // 3. Player-specific verilerini yükle (eğer login olmuşsa)
        if (ApiManager.Instance.IsLoggedIn)
        {
            currentStep++;
            _currentStatus = "Oyuncu verileri yükleniyor...";
            OnProgressUpdated?.Invoke(_currentStatus, currentStep / totalSteps);
            
            await LoadPlayerSpecificData();
        }

        // 4. GameDataManager'ın tamamen yüklenmesini bekle
        currentStep++;
        _currentStatus = "Sistem kontrolü...";
        OnProgressUpdated?.Invoke(_currentStatus, currentStep / totalSteps);
        
        await WaitForGameDataManagerReady();
    }

    /// <summary>
    /// Gülleleri retry mantığı ile yükler
    /// </summary>
    private async Task LoadCannonballsWithRetry()
    {
        for (int attempt = 1; attempt <= _maxRetryAttempts; attempt++)
        {
            try
            {
                Debug.Log($"🔄 DataInitializer: Gülle verileri yükleniyor (Deneme {attempt}/{_maxRetryAttempts})");
                
                var cannonballs = await CannonballService.Instance.GetAllCannonballsAsync(forceRefresh: true);
                
                if (cannonballs != null && cannonballs.Count > 0)
                {
                    Debug.Log($"✅ DataInitializer: {cannonballs.Count} gülle verisi yüklendi");
                    return;
                }
                else
                {
                    throw new Exception("Gülle verisi alınamadı veya boş");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ DataInitializer: Gülle yükleme hatası (Deneme {attempt}): {ex.Message}");
                
                if (attempt >= _maxRetryAttempts)
                {
                    throw new Exception($"Gülle verileri {_maxRetryAttempts} denemede yüklenemedi: {ex.Message}");
                }
                
                if (_autoRetryOnFailure && attempt < _maxRetryAttempts)
                {
                    Debug.Log($"⏳ DataInitializer: {_retryDelaySeconds}s beklenip tekrar denenecek...");
                    await Task.Delay(Mathf.RoundToInt(_retryDelaySeconds * 1000));
                }
            }
        }
    }

    /// <summary>
    /// Oyuncuya özel verileri yükler
    /// </summary>
    private async Task LoadPlayerSpecificData()
    {
        try
        {
            // Player cannonballs'ı yükle
            var playerCannonballs = await CannonballService.Instance.GetPlayerCannonballsAsync();
            Debug.Log($"✅ DataInitializer: {playerCannonballs.Count} oyuncu güllesi yüklendi");
            
            // Gelecekte: Player items, achievements vb.
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"⚠️ DataInitializer: Oyuncu verileri yüklenemedi: {ex.Message}");
            // Player verileri kritik değil, devam et
        }
    }

    /// <summary>
    /// GameDataManager'ın hazır olmasını bekler
    /// </summary>
    private async Task WaitForGameDataManagerReady()
    {
        int waitCount = 0;
        const int maxWait = 50; // 5 saniye max
        
        while (!GameDataManager.Instance.IsInitialized && waitCount < maxWait)
        {
            await Task.Delay(100); // 100ms bekle
            waitCount++;
        }
        
        if (!GameDataManager.Instance.IsInitialized)
        {
            throw new Exception("GameDataManager 5 saniyede hazır olmadı");
        }
        
        Debug.Log("✅ DataInitializer: GameDataManager hazır");
    }

    /// <summary>
    /// Toplam adım sayısını hesaplar
    /// </summary>
    private float GetTotalSteps()
    {
        float steps = 1; // System kontrolü her zaman
        
        if (_loadCannonballsOnStart) steps++;
        if (_loadItemsOnStart) steps++;
        if (ApiManager.Instance.IsLoggedIn) steps++; // Player data
        
        return steps;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug: Initialize Now")]
    private async void DebugInitializeNow()
    {
        await StartDataInitializationAsync();
    }

    [ContextMenu("Debug: Reset and Reinitialize")]
    private async void DebugResetAndReinitialize()
    {
        await ResetAndReinitializeAsync();
    }

    [ContextMenu("Debug: Clear All Data")]
    private void DebugClearAllData()
    {
        ClearAllData();
    }

    #endregion
} 