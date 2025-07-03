using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BarbarosKs.Shared.DTOs;
using System.Collections;
using BarbarosKs.Core;

// Unity'de kullanım kolaylığı için type alias
using CannonballDto = BarbarosKs.Shared.DTOs.CannonballTypeDto;

namespace BarbarosKs.UI
{
    /// <summary>
    /// Market UI'sı için örnek implementation.
    /// Yeni veri sistemi (GameDataManager, CannonballService, MarketManager) kullanır.
    /// </summary>
    public class MarketUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject _marketPanel;
        [SerializeField] private Transform _itemListContainer;
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private Button _openMarketButton;
        [SerializeField] private Button _closeMarketButton;
        [SerializeField] private Button _refreshButton;

        [Header("Filter UI")]
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private TMP_Dropdown _categoryDropdown;
        [SerializeField] private TMP_Dropdown _sortDropdown;
        [SerializeField] private Slider _minPriceSlider;
        [SerializeField] private Slider _maxPriceSlider;
        [SerializeField] private TextMeshProUGUI _minPriceText;
        [SerializeField] private TextMeshProUGUI _maxPriceText;

        [Header("Status UI")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _playerGoldText;

        [Header("Debug")]
        [SerializeField] private bool _verboseLogging = true;

        // Market item UI'ları
        private List<MarketItemUI> _activeItemUIs = new();
        private MarketManager.MarketFilter _currentFilter = new();
        private bool _isPurchasing = false;

        private void Awake()
        {
            SetupUI();
        }

        private void Start()
        {
            SetupEventListeners();
            UpdatePlayerGoldDisplay();
        }

        private void OnDestroy()
        {
            CleanupEventListeners();
        }

        private void SetupUI()
        {
            // UI başlangıç durumu
            if (_marketPanel) _marketPanel.SetActive(false);
            if (_loadingPanel) _loadingPanel.SetActive(false);

            // Button events
            if (_openMarketButton) _openMarketButton.onClick.AddListener(OnOpenMarketClicked);
            if (_closeMarketButton) _closeMarketButton.onClick.AddListener(OnCloseMarketClicked);
            if (_refreshButton) _refreshButton.onClick.AddListener(OnRefreshClicked);

            // Filter events
            if (_searchInput) _searchInput.onValueChanged.AddListener(OnSearchChanged);
            if (_categoryDropdown) _categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
            if (_sortDropdown) _sortDropdown.onValueChanged.AddListener(OnSortChanged);
            if (_minPriceSlider) _minPriceSlider.onValueChanged.AddListener(OnMinPriceChanged);
            if (_maxPriceSlider) _maxPriceSlider.onValueChanged.AddListener(OnMaxPriceChanged);

            DebugLog("MarketUI başlatıldı");
        }

        private void SetupEventListeners()
        {
            // Market sistem event'leri
            if (MarketManager.Instance != null)
            {
                MarketManager.OnMarketItemsUpdated += OnMarketItemsUpdated;
                MarketManager.OnPurchaseCompleted += OnPurchaseCompleted;
                MarketManager.OnMarketError += OnMarketError;
            }

            // Player data event'leri - PlayerManager kullan (mevcut event'ler)
            if (PlayerManager.Instance != null)
            {
                PlayerManager.OnActiveShipChanged += OnPlayerShipUpdated;
                PlayerManager.OnPlayerDataLoaded += OnPlayerProfileUpdated;
            }

            // Veri yükleme event'leri
            if (DataInitializer.Instance != null)
            {
                DataInitializer.OnInitializationCompleted += OnDataInitializationCompleted;
                DataInitializer.OnProgressUpdated += OnDataLoadingProgress;
            }
        }

        private void CleanupEventListeners()
        {
            // Event cleanup
            if (MarketManager.Instance != null)
            {
                MarketManager.OnMarketItemsUpdated -= OnMarketItemsUpdated;
                MarketManager.OnPurchaseCompleted -= OnPurchaseCompleted;
                MarketManager.OnMarketError -= OnMarketError;
            }

            // PlayerManager event cleanup
            if (PlayerManager.Instance != null)
            {
                PlayerManager.OnActiveShipChanged -= OnPlayerShipUpdated;
                PlayerManager.OnPlayerDataLoaded -= OnPlayerProfileUpdated;
            }

            if (DataInitializer.Instance != null)
            {
                DataInitializer.OnInitializationCompleted -= OnDataInitializationCompleted;
                DataInitializer.OnProgressUpdated -= OnDataLoadingProgress;
            }
        }

        #region Button Handlers

        private async void OnOpenMarketClicked()
        {
            DebugLog("Market açılıyor...");
            
            if (MarketManager.Instance == null)
            {
                ShowStatus("MarketManager bulunamadı!", true);
                return;
            }

            if (_marketPanel) _marketPanel.SetActive(true);
            ShowLoading(true);

            try
            {
                await MarketManager.Instance.OpenMarketAsync();
                DebugLog("Market başarıyla açıldı");
            }
            catch (Exception ex)
            {
                ShowStatus($"Market açılamadı: {ex.Message}", true);
                DebugLog($"Market açılma hatası: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void OnCloseMarketClicked()
        {
            DebugLog("Market kapatılıyor...");
            
            if (MarketManager.Instance != null)
            {
                MarketManager.Instance.CloseMarket();
            }
            
            if (_marketPanel) _marketPanel.SetActive(false);
        }

        private async void OnRefreshClicked()
        {
            DebugLog("Market yenileniyor...");
            
            if (MarketManager.Instance == null) return;

            ShowLoading(true);
            MarketManager.Instance.RefreshMarketItems();
            ShowLoading(false);
        }

        #endregion

        #region Filter Handlers

        private void OnSearchChanged(string searchText)
        {
            _currentFilter.SearchText = searchText;
            ApplyCurrentFilter();
        }

        private void OnCategoryChanged(int categoryIndex)
        {
            // Kategori filtreleme şu an desteklenmiyor (MarketFilter'da Category field yok)
            // Gelecekte eklenebilir
            DebugLog($"Kategori seçildi: {categoryIndex}");
        }

        private void OnSortChanged(int sortIndex)
        {
            _currentFilter.SortType = (MarketManager.MarketSortType)sortIndex;
            ApplyCurrentFilter();
        }

        private void OnMinPriceChanged(float value)
        {
            _currentFilter.MinPrice = Mathf.RoundToInt(value);
            if (_minPriceText) _minPriceText.text = $"{_currentFilter.MinPrice} Gold";
            ApplyCurrentFilter();
        }

        private void OnMaxPriceChanged(float value)
        {
            _currentFilter.MaxPrice = Mathf.RoundToInt(value);
            if (_maxPriceText) _maxPriceText.text = $"{_currentFilter.MaxPrice} Gold";
            ApplyCurrentFilter();
        }

        private void ApplyCurrentFilter()
        {
            if (MarketManager.Instance != null)
            {
                // MarketManager'daki mevcut filtreleme methodlarını kullan
                MarketManager.Instance.SetPriceFilter(_currentFilter.MinPrice, _currentFilter.MaxPrice);
                MarketManager.Instance.SetSearchFilter(_currentFilter.SearchText);
                MarketManager.Instance.SetSorting(_currentFilter.SortType, _currentFilter.SortAscending);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Market ürünleri güncellendiğinde çağrılır
        /// </summary>
        private void OnMarketItemsUpdated(List<IMarketItem> items)
        {
            Debug.Log($"🔄 MarketUI: {items.Count} ürün güncellendi");
            RefreshItemDisplay(items);
        }

        /// <summary>
        /// Satın alma işlemi tamamlandığında çağrılır
        /// </summary>
        private void OnPurchaseCompleted(IMarketItem item, bool success)
        {
            _isPurchasing = false;

            if (success)
            {
                _statusText.text = $"✅ {item.Name} başarıyla satın alındı!";
                _statusText.color = Color.green;
                
                // Altın miktarını güncelle
                UpdatePlayerGoldDisplay();
                
                Debug.Log($"✅ MarketUI: Satın alma başarılı - {item.Name}");
            }
            else
            {
                _statusText.text = $"❌ {item.Name} satın alınamadı!";
                _statusText.color = Color.red;
                
                Debug.Log($"❌ MarketUI: Satın alma başarısız - {item.Name}");
            }

            // 3 saniye sonra mesajı temizle
            StartCoroutine(ClearStatusAfterDelay(3f));
        }

        private void OnMarketError(string errorMessage)
        {
            ShowStatus($"Market Hatası: {errorMessage}", true);
        }

        private void OnPlayerShipUpdated(ShipSummaryDto shipData)
        {
            UpdatePlayerGoldDisplay();
            DebugLog($"Player ship updated: {shipData?.Name}");
        }

        private void OnPlayerProfileUpdated(PlayerProfileDto playerProfile)
        {
            UpdatePlayerGoldDisplay();
            DebugLog($"Player profile updated: {playerProfile?.Username}");
        }

        private void OnDataInitializationCompleted()
        {
            DebugLog("Veri başlatma tamamlandı, market hazır");
            ShowStatus("Market hazır!", false);
        }

        private void OnDataLoadingProgress(string status, float progress)
        {
            ShowStatus($"Yükleniyor: {status}", false);
        }

        #endregion

        #region UI Updates

        private void RefreshItemDisplay(List<IMarketItem> items)
        {
            // Mevcut UI'ları temizle
            ClearActiveItemUIs();

            if (_itemListContainer == null || _itemPrefab == null)
            {
                Debug.LogError("Market UI components eksik!");
                return;
            }

            // Yeni UI'ları oluştur
            foreach (var item in items)
            {
                var itemUI = CreateMarketItemUI(item);
                if (itemUI != null)
                {
                    _activeItemUIs.Add(itemUI);
                }
            }

            Debug.Log($"🔄 MarketUI: {_activeItemUIs.Count} ürün UI'sı oluşturuldu");
        }

        private MarketItemUI CreateMarketItemUI(IMarketItem item)
        {
            var itemObj = Instantiate(_itemPrefab, _itemListContainer);
            var itemUI = itemObj.GetComponent<MarketItemUI>();
            
            if (itemUI != null)
            {
                itemUI.Setup(item, OnItemPurchaseClicked);
                return itemUI;
            }
            else
            {
                Debug.LogError("MarketItemUI component bulunamadı!");
                Destroy(itemObj);
                return null;
            }
        }

        private void ClearActiveItemUIs()
        {
            foreach (var itemUI in _activeItemUIs)
            {
                if (itemUI != null && itemUI.gameObject != null)
                {
                    Destroy(itemUI.gameObject);
                }
            }
            _activeItemUIs.Clear();
        }

        private async void OnItemPurchaseClicked(IMarketItem item)
        {
            if (_isPurchasing)
            {
                Debug.LogWarning("⚠️ Zaten bir satın alma işlemi devam ediyor!");
                return;
            }

            Debug.Log($"🛒 MarketUI: Satın alma tıklandı - {item.Name}");
            
            if (MarketManager.Instance != null)
            {
                _isPurchasing = true;
                _statusText.text = $"🔄 {item.Name} satın alınıyor...";
                _statusText.color = Color.yellow;
                
                var success = await MarketManager.Instance.PurchaseItemAsync(item, 1);
                // OnPurchaseCompleted event'i sonucu işleyecek
            }
            else
            {
                Debug.LogError("❌ MarketManager bulunamadı!");
                _statusText.text = "❌ Market servisi bulunamadı!";
                _statusText.color = Color.red;
            }
        }

        private void UpdatePlayerGoldDisplay()
        {
            if (_playerGoldText != null)
            {
                // PlayerManager kullan - mevcut property'ler
                if (PlayerManager.Instance != null && PlayerManager.Instance.ActiveShip != null)
                {
                    var level = PlayerManager.Instance.ActiveShip.Level;
                    _playerGoldText.text = $"💰 {level:N0} Silver";
                    DebugLog($"Player silver updated: {level}");
                }
                else
                {
                    _playerGoldText.text = "💰 --- Silver";
                    DebugLog("Player silver unavailable - PlayerManager or ActiveShip is null");
                }
            }
        }

        private IEnumerator ClearStatusAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (_statusText != null)
            {
                _statusText.text = "";
                _statusText.color = Color.white;
            }
        }

        private void ShowLoading(bool show)
        {
            if (_loadingPanel) _loadingPanel.SetActive(show);
        }

        private void ShowStatus(string message, bool isError)
        {
            if (_statusText)
            {
                _statusText.text = message;
                _statusText.color = isError ? Color.red : Color.white;
            }
            
            DebugLog($"Status: {message}");
        }

        #endregion

        #region Debug

        private void DebugLog(string message)
        {
            if (_verboseLogging)
            {
                Debug.Log($"[MarketUI] {message}");
            }
        }

        [ContextMenu("Debug: Open Market")]
        private void DebugOpenMarket()
        {
            OnOpenMarketClicked();
        }

        [ContextMenu("Debug: Check System Status")]
        private void DebugCheckSystemStatus()
        {
            Debug.Log("=== MARKET UI SYSTEM STATUS ===");
            Debug.Log($"GameDataManager: {(GameDataManager.Instance != null ? "✅" : "❌")}");
            Debug.Log($"CannonballService: {(CannonballService.Instance != null ? "✅" : "❌")}");
            Debug.Log($"MarketManager: {(MarketManager.Instance != null ? "✅" : "❌")}");
            Debug.Log($"PlayerManager: {(PlayerManager.Instance != null ? "✅" : "❌")}");
            Debug.Log($"DataInitializer: {(DataInitializer.Instance != null ? "✅" : "❌")}");
            
            if (PlayerManager.Instance != null)
            {
                Debug.Log($"Current Ship: {(PlayerManager.Instance.ActiveShip != null ? PlayerManager.Instance.ActiveShip.Name : "NULL")}");
                Debug.Log($"Player Profile: {(PlayerManager.Instance.PlayerProfile != null ? PlayerManager.Instance.PlayerProfile.Username : "NULL")}");
            }
        }

        [ContextMenu("Debug: Update Gold Display")]
        private void DebugUpdateGoldDisplay()
        {
            UpdatePlayerGoldDisplay();
        }

        #endregion
    }

    /// <summary>
    /// Market'teki tek bir ürün için UI component
    /// </summary>
    public class MarketItemUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _damageText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private Image _iconImage;

        private IMarketItem _item;
        private System.Action<IMarketItem> _onPurchaseClicked;

        public void Setup(IMarketItem item, System.Action<IMarketItem> onPurchaseClicked)
        {
            _item = item;
            _onPurchaseClicked = onPurchaseClicked;

            UpdateUI();
            
            if (_purchaseButton)
            {
                _purchaseButton.onClick.RemoveAllListeners();
                _purchaseButton.onClick.AddListener(OnPurchaseClicked);
            }
        }

        private void UpdateUI()
        {
            if (_nameText) _nameText.text = _item.Name;
            if (_priceText) _priceText.text = $"{_item.Price} Silver";
            if (_descriptionText) _descriptionText.text = _item.Description;
            
            // Damage bilgisi sadece CannonballMarketItem için mevcut
            if (_damageText)
            {
                if (_item is CannonballMarketItem cannonballItem)
                {
                    _damageText.text = $"DMG: {cannonballItem.BaseDamage}";
                }
                else
                {
                    _damageText.text = ""; // Diğer item türleri için damage bilgisi yok
                }
            }
            
            // Icon yükleme (gelecekte)
            // if (_iconImage) LoadIcon(_item.IconPath);
        }

        private void OnPurchaseClicked()
        {
            _onPurchaseClicked?.Invoke(_item);
        }
    }
} 