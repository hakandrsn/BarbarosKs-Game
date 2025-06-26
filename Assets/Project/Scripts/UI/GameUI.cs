using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BarbarosKs.Player;
using Project.Scripts.Network;
using Project.Scripts.Network.Models; // Yeni ve standart modellerimizi kullanıyoruz

namespace BarbarosKs.UI
{
    public class GameUI : MonoBehaviour
    {
        [Header("Sağlık Göstergesi")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Gradient healthGradient;

        [Header("Hedef İşaretçileri")]
        [SerializeField] private GameObject targetMarkerPrefab;
        [SerializeField] private Transform targetMarkersContainer;

        [Header("Bildirimler")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private float notificationDuration = 3f;

        // Oyuncu referansları (Sadece yerel oyuncu için)
        private PlayerHealth _localPlayerHealth;
        
        // Hedef izleme için değişkenler
        private readonly List<Transform> _trackedTargets = new List<Transform>();
        private readonly Dictionary<Transform, GameObject> _targetMarkers = new Dictionary<Transform, GameObject>();
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;

            // Bu UI, sadece yerel oyuncunun bilgilerini takip etmeli.
            // Sahnedeki "Player" tag'ine sahip nesneyi bulup onun bileşenlerini alıyoruz.
            GameObject localPlayerObject = GameObject.FindGameObjectWithTag("Player");
            if (localPlayerObject != null)
            {
                _localPlayerHealth = localPlayerObject.GetComponent<PlayerHealth>();
            }
            else
            {
                Debug.LogWarning("Sahnede 'Player' tag'ine sahip yerel oyuncu bulunamadı. UI düzgün çalışmayabilir.");
            }
        }

        private void Start()
        {
            // Yerel oyuncunun can olaylarını dinlemeye başla
            if (_localPlayerHealth != null)
            {
                UpdateHealthUI(_localPlayerHealth.GetCurrentHealth(), _localPlayerHealth.GetMaxHealth());
                _localPlayerHealth.OnHealthChanged.AddListener(UpdateHealthUI);
            }

            // Ağ yöneticisinden gelen genel oyun olaylarını dinlemeye başla
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPlayerJoined += HandlePlayerJoined;
                // NetworkManager.Instance.OnPlayerLeft += HandlePlayerLeft; // Bu olay eklendiğinde aktif edilecek
            }
        }

        private void OnDestroy()
        {
            // Bellek sızıntılarını önlemek için tüm olay aboneliklerini iptal et
            if (_localPlayerHealth != null)
            {
                _localPlayerHealth.OnHealthChanged.RemoveListener(UpdateHealthUI);
            }
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPlayerJoined -= HandlePlayerJoined;
                // NetworkManager.Instance.OnPlayerLeft -= HandlePlayerLeft;
            }
        }
        
        private void Update()
        {
            UpdateTargetMarkers();
        }

        #region UI Güncelleme Metotları

        private void UpdateHealthUI(int current, int max)
        {
            if (healthSlider == null) return;
            
            healthSlider.maxValue = max;
            healthSlider.value = current;

            if (healthText != null)
            {
                healthText.text = $"{current} / {max}";
            }

            if (healthFillImage != null && healthGradient != null)
            {
                float healthPercentage = (float)current / max;
                healthFillImage.color = healthGradient.Evaluate(healthPercentage);
            }
        }

        #endregion

        #region Hedef İşaretçi Sistemi

        // Bu metotlar artık ağa mesaj göndermiyor, sadece yerel UI'ı yönetiyor.
        public void AddTrackedTarget(Transform target)
        {
            if (target == null || _trackedTargets.Contains(target)) return;

            _trackedTargets.Add(target);

            if (targetMarkerPrefab != null && targetMarkersContainer != null)
            {
                GameObject marker = Instantiate(targetMarkerPrefab, targetMarkersContainer);
                _targetMarkers.Add(target, marker);

                if (marker.TryGetComponent<TargetMarker>(out var markerComponent))
                {
                    // TODO: Hedefin Network ID'si buraya bir şekilde iletilmeli.
                    markerComponent.Initialize(target, ""); 
                }
            }
        }

        public void RemoveTrackedTarget(Transform target)
        {
            if (target == null || !_trackedTargets.Contains(target)) return;

            _trackedTargets.Remove(target);

            if (_targetMarkers.TryGetValue(target, out var marker))
            {
                Destroy(marker);
                _targetMarkers.Remove(target);
            }
        }

        private void UpdateTargetMarkers()
        {
            // ... Bu metodun içeriği doğru ve ağdan bağımsız olduğu için aynı kalıyor ...
            // ... Sadece _trackedTargets ve _targetMarkers referanslarını kullanacak şekilde güncellendi ...
            if (_mainCamera == null) return;

            // Aktif olmayan hedefleri temizle
            for (int i = _trackedTargets.Count - 1; i >= 0; i--)
            {
                if (_trackedTargets[i] == null)
                {
                    RemoveTrackedTarget(_trackedTargets[i]);
                }
            }
            
            foreach (var target in _trackedTargets)
            {
                if (target == null || !_targetMarkers.TryGetValue(target, out var marker)) continue;

                Vector3 screenPos = _mainCamera.WorldToScreenPoint(target.position);
                if (screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width && screenPos.y > 0 && screenPos.y < Screen.height)
                {
                    marker.transform.position = screenPos;
                    marker.transform.rotation = Quaternion.identity;
                }
                else
                {
                    // Ekran dışı mantığı... (Mevcut kodunuzdaki gibi)
                }
            }
        }
        
        #endregion

        #region Bildirim Sistemi

        public void ShowNotification(string message, float duration = 0)
        {
            if (notificationPanel == null || notificationText == null) return;

            notificationText.text = message;
            notificationPanel.SetActive(true);
            StopAllCoroutines();
            float actualDuration = duration > 0 ? duration : notificationDuration;
            StartCoroutine(HideNotificationAfterDelay(actualDuration));
        }

        private IEnumerator HideNotificationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            notificationPanel.SetActive(false);
        }

        #endregion

        #region Network Olay İşleyicileri

        /// <summary>
        /// NetworkManager'dan yeni bir oyuncunun katıldığı bilgisi geldiğinde çalışır.
        /// </summary>
        private void HandlePlayerJoined(Project.Scripts.Network.Models.Player joinedPlayer)
        {
            // Gelen veri artık standart 'Player' modelimizden geliyor.
            ShowNotification($"{joinedPlayer.Name} oyuna katıldı!");
        }

        /// <summary>
        /// Bir oyuncu oyundan ayrıldığında çalışır.
        /// </summary>
        private void HandlePlayerLeft(string playerId) // TODO: Bu olay NetworkManager'da (string playerId, string playerName) olarak güncellenebilir.
        {
            // Şimdilik sadece ID ile bildirim gösteriyoruz.
            ShowNotification($"Bir oyuncu oyundan ayrıldı.");
        }

        #endregion
    }
}