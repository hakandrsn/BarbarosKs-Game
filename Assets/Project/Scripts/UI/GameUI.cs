using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BarbarosKs.Player;
using BarbarosKs.Shared.DTOs.Game; // Yeni ve doğru DTO namespace'imiz
using Project.Scripts.Network;

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

        // Referanslar
        private PlayerHealth _localPlayerHealth;
        private Camera _mainCamera;

        // Hedef izleme için değişkenler
        private readonly List<Transform> _trackedTargets = new List<Transform>();
        private readonly Dictionary<Transform, GameObject> _targetMarkers = new Dictionary<Transform, GameObject>();

        private void Awake()
        {
            _mainCamera = Camera.main;

            // Bu script, sahneye özel olduğu için olay aboneliklerini Awake/OnDestroy yerine
            // OnEnable/OnDisable içinde yapmak daha güvenlidir.
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
                // NetworkManager.Instance.OnActionFailed += HandleActionFailed; // Gelecekte eklenebilir
            }
        }

        private void OnDisable()
        {
            // Bellek sızıntılarını önlemek için tüm olay aboneliklerini iptal et
            PlayerController.OnLocalPlayerSpawned -= InitializeUIForPlayer;

            if (_localPlayerHealth != null)
            {
                _localPlayerHealth.OnHealthChanged.RemoveListener(UpdateHealthUI);
            }

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnEntitySpawned -= HandleEntitySpawned;
                NetworkManager.Instance.OnEntityDespawned -= HandleEntityDespawned;
                NetworkManager.Instance.OnHealthUpdate -= HandleHealthUpdate;
                // NetworkManager.Instance.OnActionFailed -= HandleActionFailed;
            }
        }

        private void Update()
        {
            UpdateTargetMarkers();
        }

        /// <summary>
        /// Yerel oyuncu karakteri sahnede oluşturulduğunda bu metot çağrılır.
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
                float healthPercentage = (float)current / max;
                healthFillImage.color = healthGradient.Evaluate(healthPercentage);
            }
        }

        #endregion

        #region Hedef İşaretçi ve Bildirim Sistemleri

        // Bu metotlar ağdan bağımsız olduğu için aynı kalıyor.
        public void AddTrackedTarget(Transform target)
        {
            /* ... */
        }

        public void RemoveTrackedTarget(Transform target)
        {
            /* ... */
        }

        private void UpdateTargetMarkers()
        {
            /* ... */
        }

        public void ShowNotification(string message, float duration = 0)
        {
            /* ... */
        }

        private IEnumerator HideNotificationAfterDelay(float delay)
        {
            delay = Mathf.Max(0, delay);
            yield return new WaitForSeconds(delay);
            notificationPanel.SetActive(false);
        }

        #endregion

        #region Ağ Olay İşleyicileri (Yenilendi)

        /// <summary>
        /// Sunucudan bir varlığın canının değiştiği bilgisi geldiğinde çalışır.
        /// </summary>
        private void HandleHealthUpdate(S2C_HealthUpdateData data)
        {
            // Eğer canı değişen bizim yerel oyuncumuz ise, UI'ı güncelle.
            // NOT: PlayerHealth script'i bu güncellemeyi zaten kendisi yapmalı ve OnHealthChanged
            // olayını tetiklemeli. Bu yüzden bu metot şimdilik boş kalabilir veya
            // sadece hasar göstergeleri (damage numbers) için kullanılabilir.

            var localPlayerShipId = GameManager.Instance.ActiveShip?.Id.ToString();
            if (data.EntityId == localPlayerShipId)
            {
                // Yerel oyuncunun PlayerHealth script'i bu güncellemeyi zaten alıp
                // OnHealthChanged event'ini tetikleyeceği için burada tekrar UI güncellemeye gerek yok.
            }
            else
            {
                // Başka bir oyuncu hasar aldığında ekranda "100!" gibi bir hasar sayısı göstermek
                // için bu olayı kullanabilirsiniz.
            }
        }

        /// <summary>
        /// Dünyaya yeni bir varlık (oyuncu veya NPC) girdiğinde çalışır.
        /// </summary>
        private void HandleEntitySpawned(S2C_EntitySpawnData data)
        {
            // Gelen varlığın bir oyuncu gemisi olup olmadığını ve kendimize ait olup olmadığını kontrol et
            bool isPlayerShip = data.Entity.PrefabType.StartsWith("PlayerShip");
            bool isOurself = data.Entity.OwnerPlayerId == GameManager.Instance.LocalPlayerId?.ToString();

            if (isPlayerShip && !isOurself)
            {
                // Varlığın özelliklerinden oyuncu adını alalım.
                if (data.Entity.Properties.TryGetValue("playerUsername", out object usernameObj))
                {
                    ShowNotification($"{usernameObj} oyuna katıldı!");
                }
            }
        }

        /// <summary>
        /// Bir varlık dünyadan ayrıldığında (bağlantı koptu, öldü) çalışır.
        /// </summary>
        private void HandleEntityDespawned(S2C_EntityDespawnData data)
        {
            // TODO: Ayrılan oyuncunun ismini bulup göstermek için NetworkObjectSpawner'dan
            // veya başka bir yönetici script'ten destek alınabilir.
            // Şimdilik genel bir mesaj gösteriyoruz.
            ShowNotification($"Bir oyuncu oyundan ayrıldı.");
        }

        #endregion
    }
}