using System.Collections;
using System.Collections.Generic;
using BarbarosKs.Player;
using BarbarosKs.Shared.DTOs;
using BarbarosKs.Core;
using Project.Scripts.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// Yeni ve doğru DTO namespace'imiz

namespace BarbarosKs.UI
{
    public class GameUI : MonoBehaviour
    {
        [Header("Sağlık Göstergesi")] [SerializeField]
        private Slider healthSlider;

        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Gradient healthGradient;

        [Header("Hedef İşaretçileri")] [SerializeField]
        private GameObject targetMarkerPrefab;

        [SerializeField] private Transform targetMarkersContainer;

        [Header("Bildirimler")] [SerializeField]
        private GameObject notificationPanel;

        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private float notificationDuration = 3f;
        private readonly Dictionary<Transform, GameObject> _targetMarkers = new();

        // Hedef izleme için değişkenler
        private readonly List<Transform> _trackedTargets = new();

        // Referanslar
        private PlayerHealth _localPlayerHealth;
        private Camera _mainCamera;

        [Header("Hedef Bilgi UI")]
        [SerializeField] private GameObject targetInfoPanel;
        [SerializeField] private TextMeshProUGUI targetNameText;
        [SerializeField] private TextMeshProUGUI targetDistanceText;
        [SerializeField] private TextMeshProUGUI targetInstructionText;
        
        [Header("Debug UI")]
        [SerializeField] private TextMeshProUGUI debugText;
        
        private PlayerController localPlayer;
        private GameObject currentTarget;

        private void Awake()
        {
            _mainCamera = Camera.main;

            // Bu script, sahneye özel olduğu için olay aboneliklerini Awake/OnDestroy yerine
            // OnEnable/OnDisable içinde yapmak daha güvenlidir.
        }

        private void Start()
        {
            // Local player spawn edildiğinde bağlan
            PlayerController.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
            
            // UI'yi başlangıçta gizle
            if (targetInfoPanel != null)
                targetInfoPanel.SetActive(false);
                
            // Debug bilgilerini göster
            if (debugText != null)
                debugText.text = "🎮 Oyun UI Hazır\n🖱️ Mouse: Hedef seç\n⌨️ Space: Ateş et";
        }

        private void OnDestroy()
        {
            PlayerController.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
        }

        private void OnLocalPlayerSpawned(PlayerController player)
        {
            localPlayer = player;
            Debug.Log("🎮 [UI] Local player UI'ya bağlandı!");
        }

        private void Update()
        {
            if (localPlayer == null) return;
            
            // Seçili hedefi kontrol et
            GameObject selectedTarget = localPlayer.GetSelectedTarget();
            
            if (selectedTarget != currentTarget)
            {
                currentTarget = selectedTarget;
                UpdateTargetUI();
            }
            
            // Hedef varsa mesafeyi güncelle
            if (currentTarget != null)
            {
                UpdateTargetDistance();
            }
        }
        
        private void UpdateTargetUI()
        {
            if (currentTarget == null)
            {
                // Hedef yok - UI'yi gizle
                if (targetInfoPanel != null)
                    targetInfoPanel.SetActive(false);
                    
                Debug.Log("🎯 [UI] Hedef temizlendi - UI gizlendi");
            }
            else
            {
                // Hedef var - UI'yi göster
                if (targetInfoPanel != null)
                    targetInfoPanel.SetActive(true);
                    
                // Hedef ismini güncelle
                if (targetNameText != null)
                    targetNameText.text = $"🎯 Hedef: {currentTarget.name}";
                    
                // Talimat göster
                if (targetInstructionText != null)
                    targetInstructionText.text = "⌨️ Space tuşuna basarak ateş et!";
                    
                Debug.Log($"🎯 [UI] Yeni hedef seçildi: {currentTarget.name}");
            }
        }
        
        private void UpdateTargetDistance()
        {
            if (localPlayer == null || currentTarget == null || targetDistanceText == null) return;
            
            float distance = Vector3.Distance(localPlayer.transform.position, currentTarget.transform.position);
            float timeRemaining = localPlayer.GetTargetTimeRemaining();
            
            targetDistanceText.text = $"📏 Mesafe: {distance:F1}m\n⏰ Kalan süre: {timeRemaining:F0}s";
        }
        
        // Debug bilgilerini güncelle
        public void UpdateDebugInfo(string message)
        {
            if (debugText != null)
            {
                debugText.text = $"🎮 {System.DateTime.Now:HH:mm:ss}\n{message}";
            }
        }
        
        // Genel mesaj gösterme
        public void ShowMessage(string message, float duration = 3f)
        {
            Debug.Log($"📢 [UI MESSAGE] {message}");
            ShowNotification(message, duration);
        }

        private void OnEnable()
        {
            // Olayları dinlemeye başla
            PlayerController.OnLocalPlayerSpawned += InitializeUIForPlayer;

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnEntitySpawned += HandleEntitySpawned;
                NetworkManager.Instance.OnEntityDespawned += HandleEntityDespawned;
                NetworkManager.Instance.OnHealthUpdate += HandleHealthUpdate;
            }
        }

        private void OnDisable()
        {
            // Bellek sızıntılarını önlemek için tüm olay aboneliklerini iptal et
            PlayerController.OnLocalPlayerSpawned -= InitializeUIForPlayer;

            if (_localPlayerHealth != null) _localPlayerHealth.OnHealthChanged.RemoveListener(UpdateHealthUI);

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnEntitySpawned -= HandleEntitySpawned;
                NetworkManager.Instance.OnEntityDespawned -= HandleEntityDespawned;
                NetworkManager.Instance.OnHealthUpdate -= HandleHealthUpdate;
            }
        }

        /// <summary>
        ///     Yerel oyuncu karakteri sahnede oluşturulduğunda bu metot çağrılır.
        /// </summary>
        private void InitializeUIForPlayer(PlayerController localPlayer)
        {
            Debug.Log("GameUI, yerel oyuncuya başarıyla bağlandı.");
            _localPlayerHealth = localPlayer.GetComponent<PlayerHealth>();

            if (_localPlayerHealth != null)
            {
                UpdateHealthUI(_localPlayerHealth.GetCurrentHealth(), _localPlayerHealth.GetMaxHealth());
                _localPlayerHealth.OnHealthChanged.AddListener(UpdateHealthUI);
            }
        }

        #region UI Güncelleme Metotları

        private void UpdateHealthUI(int current, int max)
        {
            if (healthSlider == null) return;

            healthSlider.maxValue = max;
            healthSlider.value = current;

            if (healthText != null)
                healthText.text = $"{current} / {max}";

            if (healthFillImage != null && healthGradient != null)
            {
                var healthPercentage = (float)current / max;
                healthFillImage.color = healthGradient.Evaluate(healthPercentage);
            }
        }

        #endregion

        #region Hedef İşaretçi ve Bildirim Sistemleri

        public void AddTrackedTarget(Transform target)
        {
            if (target == null || _trackedTargets.Contains(target)) return;
            
            _trackedTargets.Add(target);
            Debug.Log($"🎯 Target added to tracking: {target.name}");
        }

        public void RemoveTrackedTarget(Transform target)
        {
            if (target == null || !_trackedTargets.Contains(target)) return;
            
            _trackedTargets.Remove(target);
            
            // Marker'ı kaldır
            if (_targetMarkers.TryGetValue(target, out GameObject marker))
            {
                Destroy(marker);
                _targetMarkers.Remove(target);
            }
            
            Debug.Log($"🎯 Target removed from tracking: {target.name}");
        }

        private void UpdateTargetMarkers()
        {
            foreach (var target in _trackedTargets)
            {
                if (target == null) continue;
                
                // Marker'ın var olup olmadığını kontrol et
                if (!_targetMarkers.TryGetValue(target, out GameObject marker))
                {
                    // Yeni marker oluştur
                    if (targetMarkerPrefab != null && targetMarkersContainer != null)
                    {
                        marker = Instantiate(targetMarkerPrefab, targetMarkersContainer);
                        _targetMarkers[target] = marker;
                    }
                }
                
                // Marker pozisyonunu güncelle
                if (marker != null && _mainCamera != null)
                {
                    Vector3 screenPos = _mainCamera.WorldToScreenPoint(target.position);
                    marker.transform.position = screenPos;
                    marker.SetActive(screenPos.z > 0); // Kameranın arkasındaysa gizle
                }
            }
        }

        public void ShowNotification(string message, float duration = 0)
        {
            if (notificationPanel == null || notificationText == null) return;
            
            notificationPanel.SetActive(true);
            notificationText.text = message;
            
            float displayDuration = duration > 0 ? duration : notificationDuration;
            StartCoroutine(HideNotificationAfterDelay(displayDuration));
            
            Debug.Log($"📢 [NOTIFICATION] {message}");
        }

        private IEnumerator HideNotificationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
        }

        #endregion

        #region Ağ Olay İşleyicileri

        /// <summary>
        ///     Sunucudan bir varlığın canının değiştiği bilgisi geldiğinde çalışır.
        /// </summary>
        private void HandleHealthUpdate(S2C_HealthUpdateData data)
        {
            // PlayerManager üzerinden local player ID'sini al
            if (PlayerManager.Instance?.ActiveShip == null) return;
            
            var localPlayerShipId = PlayerManager.Instance.ActiveShip.Id.ToString();
            if (data.EntityId == localPlayerShipId)
            {
                // Yerel oyuncunun PlayerHealth script'i bu güncellemeyi zaten alıp
                // OnHealthChanged event'ini tetikleyeceği için burada tekrar UI güncellemeye gerek yok.
                Debug.Log($"💚 [UI] Local player health update received: {data.CurrentHealth}");
            }
        }

        /// <summary>
        ///     Dünyaya yeni bir varlık (oyuncu veya NPC) girdiğinde çalışır.
        /// </summary>
        private void HandleEntitySpawned(S2C_EntitySpawnData data)
        {
            // Gelen varlığın bir oyuncu gemisi olup olmadığını ve kendimize ait olup olmadığını kontrol et
            bool isPlayerShip = data.Entity.PrefabType.StartsWith("PlayerShip");
            
            // PlayerManager üzerinden local player ID'sini al
            string localPlayerId = PlayerManager.Instance?.GetPlayerId()?.ToString();
            var isOurself = data.Entity.OwnerPlayerId == localPlayerId;

            if (isPlayerShip && !isOurself)
            {
                // Varlığın özelliklerinden oyuncu adını alalım.
                if (data.Entity.Properties.TryGetValue("playerUsername", out object usernameObj))
                    ShowNotification($"{usernameObj} oyuna katıldı!");
            }
        }

        /// <summary>
        ///     Bir varlık dünyadan ayrıldığında (bağlantı koptu, öldü) çalışır.
        /// </summary>
        private void HandleEntityDespawned(S2C_EntityDespawnData data)
        {
            ShowNotification("Bir oyuncu oyundan ayrıldı.");
        }

        #endregion
    }
}