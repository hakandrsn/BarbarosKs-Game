using System;
using System.Collections.Generic;
using System.Linq;
using BarbarosKs.Shared.DTOs;
using UnityEngine;

// Unity'de kullanım kolaylığı için type alias
using CannonballDto = BarbarosKs.Shared.DTOs.CannonballTypeDto;

/// <summary>
/// Oyun içi tüm veri türlerini merkezi olarak yöneten sistem.
/// Cannonballs, Items, Achievements vb. tüm statik veriler burada tutulur.
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [Header("Veri Durumu")]
    [SerializeField] private bool _isInitialized;
    [SerializeField] private int _cannonballCount;
    [SerializeField] private int _itemCount;

    // Events - UI'ların dinlemesi için
    public static event Action OnDataLoaded;
    public static event Action<List<CannonballTypeDto>> OnCannonballsLoaded;
    public static event Action<List<ItemDto>> OnItemsLoaded;

    // Ana veri koleksiyonları
    private List<CannonballTypeDto> _cannonballs = new();
    private List<ItemDto> _items = new();
    private Dictionary<int, CannonballTypeDto> _cannonballLookup = new();
    private Dictionary<int, ItemDto> _itemLookup = new();

    #region Properties - Hızlı Erişim

    /// <summary>Sistem başlatıldı mı?</summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>Tüm gülleler</summary>
    public IReadOnlyList<CannonballTypeDto> Cannonballs => _cannonballs.AsReadOnly();

    /// <summary>Tüm itemlar</summary>
    public IReadOnlyList<ItemDto> Items => _items.AsReadOnly();

    /// <summary>Aktif gülleler (satın alınabilir olanlar)</summary>
    public IReadOnlyList<CannonballTypeDto> ActiveCannonballs => 
        _cannonballs.Where(c => c.IsActive).ToList().AsReadOnly();

    /// <summary>Market'te satılan gülleler</summary>
    public IReadOnlyList<CannonballTypeDto> MarketCannonballs => 
        _cannonballs.Where(c => c.IsActive && c.PurchasePrice > 0).ToList().AsReadOnly();

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ GameDataManager başlatıldı");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Debug için Inspector'da göstermek
        _cannonballCount = _cannonballs.Count;
        _itemCount = _items.Count;
    }

    #region Cannonball Operations

    /// <summary>
    /// API'dan gelen gülle verilerini yükler
    /// </summary>
    public void LoadCannonballs(List<CannonballTypeDto> cannonballs)
    {
        if (cannonballs == null)
        {
            Debug.LogError("❌ GameDataManager: Cannonballs list null!");
            return;
        }

        _cannonballs = new List<CannonballTypeDto>(cannonballs);
        _cannonballLookup.Clear();

        // Lookup dictionary'yi oluştur (O(1) erişim için)
        foreach (var cannonball in _cannonballs)
        {
            _cannonballLookup[cannonball.Id] = cannonball;
        }

        Debug.Log($"✅ GameDataManager: {_cannonballs.Count} gülle verisi yüklendi");
        Debug.Log($"📊 Market gülleler: {MarketCannonballs.Count}");
        Debug.Log($"📊 Aktif gülleler: {ActiveCannonballs.Count}");

        OnCannonballsLoaded?.Invoke(_cannonballs);
        CheckInitializationComplete();
    }

    /// <summary>
    /// ID ile gülle arama (O(1) performans)
    /// </summary>
    public CannonballTypeDto GetCannonball(int cannonballId)
    {
        return _cannonballLookup.TryGetValue(cannonballId, out var cannonball) ? cannonball : null;
    }

    /// <summary>
    /// Code ile gülle arama (Unity prefab eşleştirme için)
    /// </summary>
    public CannonballTypeDto GetCannonballByCode(int code)
    {
        return _cannonballs.FirstOrDefault(c => c.Code == code);
    }

    /// <summary>
    /// İsim ile gülle arama
    /// </summary>
    public CannonballTypeDto GetCannonballByName(string name)
    {
        return _cannonballs.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Kategori/tür ile gülle filtreleme
    /// </summary>
    public List<CannonballTypeDto> GetCannonballsByCategory(string category)
    {
        return _cannonballs.Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Fiyat aralığına göre gülle filtreleme
    /// </summary>
    public List<CannonballTypeDto> GetCannonballsByPriceRange(int minPrice, int maxPrice)
    {
        return _cannonballs.Where(c => c.PurchasePrice >= minPrice && c.PurchasePrice <= maxPrice).ToList();
    }

    #endregion

    #region Item Operations

    /// <summary>
    /// API'dan gelen item verilerini yükler
    /// </summary>
    public void LoadItems(List<ItemDto> items)
    {
        if (items == null)
        {
            Debug.LogError("❌ GameDataManager: Items list null!");
            return;
        }

        _items = new List<ItemDto>(items);
        _itemLookup.Clear();

        // Lookup dictionary'yi oluştur
        foreach (var item in _items)
        {
            _itemLookup[item.Id] = item;
        }

        Debug.Log($"✅ GameDataManager: {_items.Count} item verisi yüklendi");

        OnItemsLoaded?.Invoke(_items);
        CheckInitializationComplete();
    }

    /// <summary>
    /// ID ile item arama (O(1) performans)
    /// </summary>
    public ItemDto GetItem(int itemId)
    {
        return _itemLookup.TryGetValue(itemId, out var item) ? item : null;
    }

    #endregion

    #region Market Operations

    /// <summary>
    /// Market'te satılan tüm ürünleri getirir (hem gülleler hem itemlar)
    /// </summary>
    public List<IMarketItem> GetMarketItems()
    {
        var marketItems = new List<IMarketItem>();
        
        // Market güllelerini ekle
        marketItems.AddRange(MarketCannonballs.Select(c => c.ToMarketItem()));
        
        // Market itemlarını ekle (gelecekte)
        // marketItems.AddRange(MarketItems.Cast<IMarketItem>());
        
        return marketItems;
    }

    /// <summary>
    /// Fiyata göre sıralı market ürünleri
    /// </summary>
    public List<IMarketItem> GetMarketItemsSortedByPrice(bool ascending = true)
    {
        var items = GetMarketItems();
        return ascending 
            ? items.OrderBy(item => item.Price).ToList()
            : items.OrderByDescending(item => item.Price).ToList();
    }

    #endregion

    #region System Operations

    /// <summary>
    /// Tüm verileri temizler
    /// </summary>
    public void ClearAllData()
    {
        _cannonballs.Clear();
        _items.Clear();
        _cannonballLookup.Clear();
        _itemLookup.Clear();
        _isInitialized = false;

        Debug.Log("🧹 GameDataManager: Tüm veriler temizlendi");
    }

    /// <summary>
    /// Tüm temel veriler yüklendiğinde çağrılır
    /// </summary>
    private void CheckInitializationComplete()
    {
        // Şu an sadece cannonball verisi yeterli, gelecekte daha fazla kriter eklenebilir
        if (_cannonballs.Count > 0 && !_isInitialized)
        {
            _isInitialized = true;
            Debug.Log("🎉 GameDataManager: Başlangıç verileri tamamen yüklendi!");
            OnDataLoaded?.Invoke();
        }
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug: Log All Cannonballs")]
    private void DebugLogCannonballs()
    {
        Debug.Log("=== CANNONBALLS DEBUG ===");
        foreach (var cannonball in _cannonballs)
        {
            Debug.Log($"• {cannonball.Name} - {cannonball.PurchasePrice} Gold - DMG:{cannonball.BaseDamage} - Active:{cannonball.IsActive}");
        }
    }

    [ContextMenu("Debug: Log Market Items")]
    private void DebugLogMarketItems()
    {
        Debug.Log("=== MARKET ITEMS DEBUG ===");
        var marketItems = GetMarketItems();
        foreach (var item in marketItems)
        {
            Debug.Log($"• {item.Name} - {item.Price} Gold");
        }
    }

    #endregion
}

/// <summary>
/// Market'te satılan ürünler için ortak interface
/// </summary>
public interface IMarketItem
{
    Guid Id { get; }
    string Name { get; }
    int Price { get; }
    string Description { get; }
    bool IsActive { get; }
}

/// <summary>
/// CannonballDto için IMarketItem adapter
/// </summary>
public class CannonballMarketItem : IMarketItem
{
    private readonly CannonballTypeDto _cannonball;

    public CannonballMarketItem(CannonballTypeDto cannonball)
    {
        _cannonball = cannonball;
    }

    public Guid Id 
    { 
        get 
        {
            // int Id'yi deterministik olarak Guid'e çevir
            var bytes = new byte[16];
            var idBytes = BitConverter.GetBytes(_cannonball.Id);
            Array.Copy(idBytes, 0, bytes, 0, Math.Min(idBytes.Length, bytes.Length));
            return new Guid(bytes);
        }
    }

    public string Name => _cannonball.Name;
    public int Price => _cannonball.PurchasePrice;
    public string Description => _cannonball.Description;
    public bool IsActive => _cannonball.IsActive;
    
    // Cannonball'a özgü özellikler
    public int Code => _cannonball.Code;
    public int BaseDamage => _cannonball.BaseDamage;
    public CannonballTypeDto OriginalCannonball => _cannonball;
}

// CannonballDto için IMarketItem extension
public static class CannonballDtoExtensions
{
    public static bool IsMarketItem(this CannonballTypeDto cannonball)
    {
        return cannonball.IsActive && cannonball.PurchasePrice > 0;
    }
    
    public static IMarketItem ToMarketItem(this CannonballTypeDto cannonball)
    {
        return new CannonballMarketItem(cannonball);
    }
} 