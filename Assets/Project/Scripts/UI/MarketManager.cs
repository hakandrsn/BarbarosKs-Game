using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using BarbarosKs.Shared.Enums;
using UnityEngine;

// Unity'de kullanım kolaylığı için type alias
using CannonballDto = BarbarosKs.Shared.DTOs.CannonballTypeDto;

/// <summary>
/// Market operasyonlarını yöneten sistem.
/// Filtreleme, sıralama, satın alma işlemleri.
/// </summary>
public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    [Header("Market Durumu")]
    [SerializeField] private bool _isMarketOpen;
    [SerializeField] private bool _isLoading;
    [SerializeField] private int _totalItems;
    [SerializeField] private int _filteredItems;

    [Header("Filtre Ayarları")]
    [SerializeField] private int _minPrice;
    [SerializeField] private int _maxPrice = 1000;
    [SerializeField] private string _searchText = "";
    [SerializeField] private MarketSortType _sortType = MarketSortType.Name;
    [SerializeField] private bool _sortAscending = true;

    // Events
    public static event Action OnMarketOpened;
    public static event Action OnMarketClosed;
    public static event Action<List<IMarketItem>> OnMarketItemsUpdated;
    public static event Action<IMarketItem, bool> OnPurchaseCompleted;
    public static event Action<string> OnMarketError;

    // Cache data
    private List<IMarketItem> _allMarketItems = new();
    private List<IMarketItem> _filteredMarketItems = new();

    #region Properties

    /// <summary>Market açık mı?</summary>
    public bool IsMarketOpen => _isMarketOpen;

    /// <summary>Veriler yükleniyor mu?</summary>
    public bool IsLoading => _isLoading;

    /// <summary>Tüm market ürünleri</summary>
    public IReadOnlyList<IMarketItem> AllItems => _allMarketItems.AsReadOnly();

    /// <summary>Filtrelenmiş market ürünleri</summary>
    public IReadOnlyList<IMarketItem> FilteredItems => _filteredMarketItems.AsReadOnly();

    /// <summary>Aktif filtre ayarları</summary>
    public MarketFilter CurrentFilter => new()
    {
        MinPrice = _minPrice,
        MaxPrice = _maxPrice,
        SearchText = _searchText,
        SortType = _sortType,
        SortAscending = _sortAscending
    };

    #endregion

    #region Market Sort Types

    public enum MarketSortType
    {
        Name,
        Price,
        Damage // Gülleler için
    }

    [Serializable]
    public struct MarketFilter
    {
        public int MinPrice;
        public int MaxPrice;
        public string SearchText;
        public MarketSortType SortType;
        public bool SortAscending;
    }

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ MarketManager başlatıldı");
            
            // Event subscription
            SubscribeToEvents();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void Update()
    {
        // Debug için Inspector'da göstermek
        _totalItems = _allMarketItems.Count;
        _filteredItems = _filteredMarketItems.Count;
    }

    #region Event Management

    private void SubscribeToEvents()
    {
        // GameDataManager'dan market verilerini al
        GameDataManager.OnCannonballsLoaded += OnCannonballsLoaded;
    }

    private void UnsubscribeFromEvents()
    {
        GameDataManager.OnCannonballsLoaded -= OnCannonballsLoaded;
    }

    private void OnCannonballsLoaded(List<CannonballTypeDto> cannonballs)
    {
        RefreshMarketItems();
    }

    #endregion

    #region Market Operations

    /// <summary>
    /// Market'i açar ve verileri yükler
    /// </summary>
    public async Task OpenMarketAsync()
    {
        _isLoading = true;
        Debug.Log("🏪 MarketManager: Market açılıyor...");

        try
        {
            // Cannonball verilerini güncelle
            if (CannonballService.Instance != null)
            {
                await CannonballService.Instance.GetMarketCannonballsAsync(forceRefresh: false);
            }

            // Market items'ları güncelle
            RefreshMarketItems();

            _isMarketOpen = true;
            OnMarketOpened?.Invoke();
            Debug.Log($"✅ MarketManager: Market açıldı - {_allMarketItems.Count} ürün mevcut");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Market açılamadı: {ex.Message}";
            Debug.LogError($"❌ MarketManager: {errorMsg}");
            OnMarketError?.Invoke(errorMsg);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Market'i kapatır
    /// </summary>
    public void CloseMarket()
    {
        _isMarketOpen = false;
        OnMarketClosed?.Invoke();
        Debug.Log("🚪 MarketManager: Market kapatıldı");
    }

    /// <summary>
    /// Market ürünlerini yeniler
    /// </summary>
    public void RefreshMarketItems()
    {
        _allMarketItems.Clear();

        if (GameDataManager.Instance == null || !GameDataManager.Instance.IsInitialized)
        {
            Debug.LogWarning("⚠️ MarketManager: GameDataManager henüz başlatılmadı");
            return;
        }

        // Güllelerden market items oluştur
        var marketCannonballs = GameDataManager.Instance.MarketCannonballs;
        _allMarketItems.AddRange(marketCannonballs.Select(c => c.ToMarketItem()));

        // TODO: Gelecekte diğer item türleri de eklenebilir
        // _allMarketItems.AddRange(GameDataManager.Instance.MarketItems);

        Debug.Log($"🔄 MarketManager: {_allMarketItems.Count} market ürünü yüklendi");

        // Filtreleme uygula
        ApplyFilters();
    }

    #endregion

    #region Filtering & Sorting

    /// <summary>
    /// Fiyat filtresini uygular
    /// </summary>
    public void SetPriceFilter(int minPrice, int maxPrice)
    {
        _minPrice = Mathf.Max(0, minPrice);
        _maxPrice = Mathf.Max(minPrice, maxPrice);
        
        Debug.Log($"💰 MarketManager: Fiyat filtresi - {_minPrice}-{_maxPrice} Gold");
        ApplyFilters();
    }

    /// <summary>
    /// Metin arama filtresini uygular
    /// </summary>
    public void SetSearchFilter(string searchText)
    {
        _searchText = searchText ?? "";
        Debug.Log($"🔍 MarketManager: Arama filtresi - '{_searchText}'");
        ApplyFilters();
    }

    /// <summary>
    /// Sıralama ayarlarını değiştirir
    /// </summary>
    public void SetSorting(MarketSortType sortType, bool ascending = true)
    {
        _sortType = sortType;
        _sortAscending = ascending;
        
        Debug.Log($"📊 MarketManager: Sıralama - {_sortType} ({(ascending ? "Artan" : "Azalan")})");
        ApplyFilters();
    }

    /// <summary>
    /// Tüm filtreleri temizler
    /// </summary>
    public void ClearFilters()
    {
        _minPrice = 0;
        _maxPrice = 1000;
        _searchText = "";
        _sortType = MarketSortType.Name;
        _sortAscending = true;
        
        Debug.Log("🧹 MarketManager: Filtreler temizlendi");
        ApplyFilters();
    }

    /// <summary>
    /// Tüm aktif filtreleri uygular
    /// </summary>
    private void ApplyFilters()
    {
        var items = new List<IMarketItem>(_allMarketItems);

        // Fiyat filtresi
        items = items.Where(item => item.Price >= _minPrice && item.Price <= _maxPrice).ToList();

        // Metin arama filtresi
        if (!string.IsNullOrEmpty(_searchText))
        {
            var searchLower = _searchText.ToLower();
            items = items.Where(item => 
                item.Name.ToLower().Contains(searchLower) ||
                item.Description.ToLower().Contains(searchLower)
            ).ToList();
        }

        // Sıralama
        items = _sortType switch
        {
            MarketSortType.Name => _sortAscending 
                ? items.OrderBy(item => item.Name).ToList()
                : items.OrderByDescending(item => item.Name).ToList(),
            
            MarketSortType.Price => _sortAscending 
                ? items.OrderBy(item => item.Price).ToList()
                : items.OrderByDescending(item => item.Price).ToList(),
            
            MarketSortType.Damage => _sortAscending 
                ? items.OrderBy(item => GetItemDamage(item)).ToList()
                : items.OrderByDescending(item => GetItemDamage(item)).ToList(),
            
            _ => items
        };

        _filteredMarketItems = items;
        OnMarketItemsUpdated?.Invoke(_filteredMarketItems);
        
        Debug.Log($"🎯 MarketManager: {_filteredMarketItems.Count}/{_allMarketItems.Count} ürün gösteriliyor");
    }

    /// <summary>
    /// Item'ın damage değerini alır (gülleler için)
    /// </summary>
    private int GetItemDamage(IMarketItem item)
    {
        if (item is CannonballMarketItem cannonballItem)
        {
            return cannonballItem.BaseDamage;
        }
        return 0; // Diğer item türleri için default
    }

    #endregion

    #region Purchase Operations

    /// <summary>
    /// Ürün satın alma işlemi
    /// </summary>
    public async Task<bool> PurchaseItemAsync(IMarketItem item, int quantity = 1)
    {
        if (item == null)
        {
            Debug.LogError("❌ MarketManager: Satın alınacak ürün null!");
            return false;
        }

        if (!_isMarketOpen)
        {
            Debug.LogError("❌ MarketManager: Market kapalı!");
            return false;
        }

        try
        {
            _isLoading = true;
            Debug.Log($"🛒 MarketManager: Satın alınıyor - {item.Name} x{quantity}");

            bool success = false;

            // Ürün türüne göre satın alma işlemi
            if (item is CannonballMarketItem cannonballItem)
            {
                // Gülle satın alma
                if (CannonballService.Instance != null)
                {
                    success = await CannonballService.Instance.PurchaseCannonballAsync(
                        item.Id, quantity);
                }
            }
            else
            {
                // Diğer item türleri için gelecek implementasyon
                Debug.LogWarning($"⚠️ MarketManager: {item.GetType().Name} satın alma henüz desteklenmiyor");
            }

            OnPurchaseCompleted?.Invoke(item, success);

            if (success)
            {
                Debug.Log($"✅ MarketManager: Satın alma başarılı - {item.Name} x{quantity}");
                
                // Market'i güncelle (stok değişimi için)
                RefreshMarketItems();
            }
            else
            {
                Debug.LogError($"❌ MarketManager: Satın alma başarısız - {item.Name}");
            }

            return success;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Satın alma hatası ({item.Name}): {ex.Message}";
            Debug.LogError($"❌ MarketManager: {errorMsg}");
            OnMarketError?.Invoke(errorMsg);
            OnPurchaseCompleted?.Invoke(item, false);
            return false;
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// ID ile ürün satın alma (kullanım kolaylığı için)
    /// </summary>
    public async Task<bool> PurchaseItemByIdAsync(Guid itemId, int quantity = 1)
    {
        var item = _allMarketItems.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            Debug.LogError($"❌ MarketManager: ID ile ürün bulunamadı - {itemId}");
            return false;
        }

        return await PurchaseItemAsync(item, quantity);
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug: Open Market")]
    private async void DebugOpenMarket()
    {
        await OpenMarketAsync();
    }

    [ContextMenu("Debug: Refresh Items")]
    private void DebugRefreshItems()
    {
        RefreshMarketItems();
    }

    [ContextMenu("Debug: Log Market Items")]
    private void DebugLogMarketItems()
    {
        Debug.Log("=== MARKET ITEMS DEBUG ===");
        foreach (var item in _filteredMarketItems)
        {
            Debug.Log($"• {item.Name} - {item.Price} Gold - Active:{item.IsActive}");
        }
    }

    [ContextMenu("Debug: Test Purchase")]
    private async void DebugTestPurchase()
    {
        if (_filteredMarketItems.Count > 0)
        {
            var firstItem = _filteredMarketItems[0];
            await PurchaseItemAsync(firstItem, 1);
        }
        else
        {
            Debug.LogWarning("Market'te satın alınacak ürün yok!");
        }
    }

    #endregion
} 