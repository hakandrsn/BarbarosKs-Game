# 🎮 BarbarosKs Unity Veri Yönetimi Sistemi

## 📋 Genel Bakış

Bu sistem, Unity oyununda API verilerini merkezi olarak yöneten modern bir mimari sağlar. **Scalable**, **maintainable** ve **performant** bir yapı ile tasarlanmıştır.

## 🏗️ Sistem Mimarisi

### 🔧 Ana Bileşenler

1. **`GameDataManager`** - Tüm oyun verilerinin merkezi yöneticisi
2. **`CannonballService`** - Gülle verilerini API'dan çeken servis
3. **`MarketManager`** - Market operasyonlarını yöneten sistem
4. **`DataInitializer`** - Oyun başlangıcında verileri yükleyen sistem
5. **`GameSystemInitializer`** - Tüm sistemleri otomatik başlatan koordinatör

### 📁 Dosya Yapısı

```
Assets/Project/Scripts/
├── Core/
│   ├── GameDataManager.cs          # Merkezi veri yönetimi
│   ├── DataInitializer.cs          # Başlangıç veri yükleme
│   ├── GameSystemInitializer.cs    # Sistem koordinatörü
│   └── PlayerDataManager.cs        # Mevcut player veri sistemi
├── Network/
│   ├── CannonballService.cs        # Gülle API servisi
│   └── ApiManager.cs               # Güncellenmiş API manager
└── UI/
    ├── MarketManager.cs            # Market sistem yöneticisi
    └── MarketUI.cs                 # Örnek market UI
```

## 🚀 Kurulum ve Kullanım

### 1. Sistem Başlatma

#### Otomatik Başlatma (Önerilen)
```csharp
// Scene'de GameSystemInitializer component'i ekleyin
// Prefab'ları assign edin veya "Create Systems From Code" aktif edin
// Sistem otomatik olarak tüm manager'ları başlatacak
```

#### Manuel Başlatma
```csharp
// Her sistem için ayrı ayrı
GameDataManager.Instance.Initialize();
CannonballService.Instance.Initialize();
MarketManager.Instance.Initialize();
```

### 2. Veri Yükleme

#### Gülle Verilerini Yüklemek
```csharp
// Otomatik (DataInitializer kullanımı)
await DataInitializer.Instance.StartDataInitializationAsync();

// Manuel
var cannonballs = await CannonballService.Instance.GetAllCannonballsAsync();
```

#### Mevcut Verilere Erişim
```csharp
// Tüm gülleler
var allCannonballs = GameDataManager.Instance.Cannonballs;

// Market gülleler
var marketItems = GameDataManager.Instance.MarketCannonballs;

// ID ile arama
var cannonball = GameDataManager.Instance.GetCannonball(cannonballId);

// Code ile arama (Unity prefab için)
var cannonball = GameDataManager.Instance.GetCannonballByCode(1001);
```

### 3. Market Sistemi

#### Market'i Açmak
```csharp
// Market manager ile
var success = await MarketManager.Instance.OpenMarketAsync();

// UI ile (MarketUI örneğine bakın)
marketUI.OnOpenMarketClicked();
```

#### Satın Alma İşlemi
```csharp
// MarketManager ile
var success = await MarketManager.Instance.PurchaseItemAsync(cannonballId, quantity);

// Event'leri dinlemek
MarketManager.OnPurchaseCompleted += (cannonball, success) => {
    if (success) {
        Debug.Log($"{cannonball.Name} başarıyla satın alındı!");
    }
};
```

#### Filtreleme
```csharp
var filter = new MarketFilterSettings {
    SearchText = "Fire",
    MinPrice = 100,
    MaxPrice = 500,
    SortBy = MarketSortType.Price,
    SortAscending = true
};

MarketManager.Instance.ApplyFilter(filter);
```

## 📊 Event Sistemi

### GameDataManager Events
```csharp
GameDataManager.OnDataLoaded += () => {
    Debug.Log("Tüm veriler yüklendi!");
};

GameDataManager.OnCannonballsLoaded += (cannonballs) => {
    Debug.Log($"{cannonballs.Count} gülle verisi alındı");
};
```

### CannonballService Events
```csharp
CannonballService.OnCannonballsLoaded += (cannonballs) => {
    // API'dan yeni veriler geldi
};

CannonballService.OnLoadError += (error) => {
    Debug.LogError($"Veri yükleme hatası: {error}");
};
```

### MarketManager Events
```csharp
MarketManager.OnMarketItemsUpdated += (items) => {
    // Market UI'sını güncelle
};

MarketManager.OnPurchaseCompleted += (item, success) => {
    // Satın alma sonucu
};

MarketManager.OnMarketError += (error) => {
    // Market hatası
};
```

### DataInitializer Events
```csharp
DataInitializer.OnInitializationCompleted += () => {
    Debug.Log("Sistem hazır!");
};

DataInitializer.OnProgressUpdated += (status, progress) => {
    Debug.Log($"İlerleme: {status} ({progress:P0})");
};
```

## 🔧 Konfigürasyon

### Cache Ayarları
```csharp
// CannonballService Inspector'da
[SerializeField] private bool _enableCaching = true;
[SerializeField] private float _cacheExpirationMinutes = 30f;
```

### Market Ayarları
```csharp
// MarketManager Inspector'da
[SerializeField] private bool _autoRefreshOnShow = true;
```

### Debug Ayarları
```csharp
// Her sistemde mevcut
[SerializeField] private bool _verboseLogging = true;
```

## 🔍 Debug ve Test

### Context Menu Komutları
Her sistemde **Right Click** → **Debug** menüsünden:

#### GameDataManager
- `Debug: Log All Cannonballs`
- `Debug: Log Market Items`

#### CannonballService
- `Debug: Load Cannonballs Now`
- `Debug: Clear Cache`
- `Debug: Log Cache Info`

#### MarketManager
- `Debug: Open Market`
- `Debug: Check System Status`

#### DataInitializer
- `Debug: Initialize Now`
- `Debug: Reset and Reinitialize`

### Inspector Monitoring
Her sistem Inspector'da **gerçek zamanlı** durum bilgisi gösterir:
- Yüklenen veri sayıları
- Cache durumu
- Initialization durumu
- Loading durumu

## 🚨 Hata Yönetimi

### Yaygın Sorunlar ve Çözümleri

#### "GameDataManager Instance null"
```csharp
// Çözüm: GameSystemInitializer'ın sahne başında çalıştığından emin olun
```

#### "API verisi alınamadı"
```csharp
// Çözüm: ApiManager.Instance.IsLoggedIn kontrol edin
if (!ApiManager.Instance.IsLoggedIn) {
    Debug.LogError("Kullanıcı giriş yapmamış!");
}
```

#### "Cache expired"
```csharp
// Çözüm: ForceRefresh ile verileri yenileyin
await CannonballService.Instance.GetAllCannonballsAsync(forceRefresh: true);
```

## 🔮 Gelecek Geliştirmeler

### Planlanan Özellikler
1. **Items System** - Item verilerini yönetmek için
2. **Achievements System** - Başarı sistemi entegrasyonu
3. **Player Inventory** - Oyuncu envanteri yönetimi
4. **Offline Mode** - Çevrimdışı veri desteği
5. **Data Validation** - Veri doğrulama sistemi

### Genişletme Örnekleri

#### Yeni Veri Türü Eklemek
```csharp
// 1. GameDataManager'a yeni koleksiyon ekle
private List<WeaponDto> _weapons = new();

// 2. Yeni Service oluştur
public class WeaponService : MonoBehaviour { ... }

// 3. DataInitializer'a yükleme logic'i ekle
await LoadWeaponsWithRetry();
```

#### Yeni Market Kategorisi
```csharp
// 1. IMarketItem implement eden yeni class
public class WeaponMarketItem : IMarketItem { ... }

// 2. MarketManager'a filtreleme ekle
public List<WeaponDto> GetMarketWeapons() { ... }
```

## 📞 Support

Sistem hakkında sorularınız için:
1. Context Menu debug komutlarını kullanın
2. Inspector'da sistem durumlarını kontrol edin  
3. Console log'larını inceleyin (verboseLogging = true)

---

## 🎯 Özet

Bu sistem sayesinde:
- ✅ **Centralized Data Management** - Tüm veriler tek yerden yönetilir
- ✅ **API Integration** - Otomatik API çağrıları ve cache
- ✅ **Event-Driven Architecture** - UI otomatik güncellenir
- ✅ **Error Handling** - Robust hata yönetimi
- ✅ **Scalable Design** - Kolayca genişletilebilir
- ✅ **Debug Support** - Kapsamlı debug araçları

🚀 **Happy Coding!** 