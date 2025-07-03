using BarbarosKs.Shared.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BarbarosKs.Core;

namespace BarbarosKs.UI
{
    public class ShipSelectionUI : MonoBehaviour
    {
        [Header("UI Referansları")] 
        [SerializeField] private Transform shipListContainer; // Gemi butonlarının ekleneceği yer (örn: bir Vertical Layout Group)
        [SerializeField] private GameObject shipButtonPrefab; // Tek bir gemi butonunun prefab'ı
        [SerializeField] private TextMeshProUGUI playerNameText; // Oyuncu adını gösterecek text

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        private void Start()
        {
            // PlayerManager'dan veriyi çek ve UI'ı doldur
            if (PlayerManager.Instance == null || !PlayerManager.Instance.HasPlayerData)
            {
                Debug.LogError("❌ ShipSelectionUI: PlayerManager'da oyuncu verisi yok! Login sahnesine dönülüyor...");
                SceneController.Instance?.LoadLogin();
                return;
            }

            var playerProfile = PlayerManager.Instance.PlayerProfile;
            var ownedShips = PlayerManager.Instance.OwnedShips;

            DebugLog($"🚢 ShipSelectionUI: {playerProfile.Username} için gemi seçim ekranı açılıyor");
            DebugLog($"📊 Mevcut gemiler: {ownedShips?.Count ?? 0}");

            // Oyuncu adını UI'a yazdır
            if (playerNameText != null) 
                playerNameText.text = playerProfile.Username;

            // Mevcut tüm butonları temizle (sahne yeniden yüklendiğinde vb. durumlar için)
            ClearExistingButtons();

            // Oyuncunun sahip olduğu her bir gemi için bir buton oluştur
            if (ownedShips != null && ownedShips.Count > 0)
            {
                foreach (var ship in ownedShips)
                {
                    CreateShipButton(ship);
                }
            }
            else
            {
                DebugLog("⚠️ Oyuncunun gemisi yok - CreateShip sahnesine yönlendiriliyor");
                SceneController.Instance?.LoadCreateShip();
            }
        }

        /// <summary>
        /// Mevcut butonları temizler
        /// </summary>
        private void ClearExistingButtons()
        {
            if (shipListContainer == null) return;

            foreach (Transform child in shipListContainer) 
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Gemi butonunu oluşturur
        /// </summary>
        private void CreateShipButton(ShipSummaryDto ship)
        {
            if (shipButtonPrefab == null || shipListContainer == null)
            {
                Debug.LogError("❌ ShipSelectionUI: Button prefab veya container eksik!");
                return;
            }

            var buttonGo = Instantiate(shipButtonPrefab, shipListContainer);

            // Butonun text'ini ayarla
            var buttonText = buttonGo.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) 
            {
                buttonText.text = $"{ship.Name}\nSeviye: {ship.Level}\nCan: {ship.CurrentHull}/{ship.MaxHull}";
            }

            // Butonun tıklama olayını ayarla
            var button = buttonGo.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnShipSelected(ship));
            }

            DebugLog($"✅ Gemi butonu oluşturuldu: {ship.Name}");
        }

        /// <summary>
        /// Bir gemi seçim butonuna tıklandığında çalışır.
        /// </summary>
        private void OnShipSelected(ShipSummaryDto selectedShip)
        {
            DebugLog($"🚢 Gemi seçildi: {selectedShip.Name} (ID: {selectedShip.Id})");
            
            // Kullanıcı arayüzünü kilitle (birden fazla tıklamayı önle)
            SetUIInteractable(false);
            
            // PlayerManager'a gemi seçimini bildir
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.HandleShipSelection(selectedShip);
                DebugLog("✅ PlayerManager'a gemi seçimi bildirimi gönderildi");
            }
            else
            {
                Debug.LogError("❌ PlayerManager bulunamadı!");
                SetUIInteractable(true); // UI'ı tekrar aktif et
            }
        }

        /// <summary>
        /// UI etkileşimini açar/kapatır
        /// </summary>
        private void SetUIInteractable(bool interactable)
        {
            if (shipListContainer == null) return;

            // Tüm butonları devre dışı bırak/etkinleştir
            var buttons = shipListContainer.GetComponentsInChildren<Button>();
            foreach (var button in buttons)
            {
                button.interactable = interactable;
            }

            DebugLog($"🔒 UI interactable: {interactable}");
        }

        /// <summary>
        /// Hata mesajı gösterir
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            Debug.LogError($"🚫 {message}");
            // TODO: Burada bir popup gösterilebilir
        }

        /// <summary>
        /// Debug loglama metodu
        /// </summary>
        private void DebugLog(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[ShipSelectionUI] {message}");
            }
        }

        /// <summary>
        /// Geri dönme butonu için
        /// </summary>
        public void OnBackToLogin()
        {
            DebugLog("🔙 Login ekranına dönülüyor");
            
            // Player verilerini temizle
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.ClearAllData();
            }
            
            // Login sahnesine dön
            SceneController.Instance?.LoadLogin();
        }

        /// <summary>
        /// Gemi oluşturma ekranına gitme butonu için
        /// </summary>
        public void OnCreateNewShip()
        {
            DebugLog("🔨 Gemi oluşturma ekranına gidiliyor");
            SceneController.Instance?.LoadCreateShip();
        }

        #region Debug Methods

        [ContextMenu("Debug: Refresh Ship List")]
        private void DebugRefreshShipList()
        {
            DebugLog("🔄 Gemi listesi yenileniyor...");
            Start(); // Start metodunu tekrar çağır
        }

        [ContextMenu("Debug: Show Player Info")]
        private void DebugShowPlayerInfo()
        {
            if (PlayerManager.Instance == null)
            {
                Debug.Log("❌ PlayerManager yok");
                return;
            }

            Debug.Log("=== SHIP SELECTION DEBUG ===");
            Debug.Log($"Player: {PlayerManager.Instance.PlayerProfile?.Username ?? "NULL"}");
            Debug.Log($"Ship Count: {PlayerManager.Instance.ShipCount}");
            Debug.Log($"Has Active Ship: {PlayerManager.Instance.HasActiveShip}");
        }

        #endregion
    }
}