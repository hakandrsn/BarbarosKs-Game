using System;
using UnityEngine;

namespace BarbarosKs.Ship
{
    /// <summary>
    /// Gemi kontrol sınıfı - Oyuncu tarafından seçilen gemiyi kontrol eder
    /// </summary>
    public class ShipController : MonoBehaviour
    {
        [Header("Gemi Özellikleri")]
        [SerializeField] private string shipId;
        [SerializeField] private string shipName;
        [SerializeField] private int currentHullDurability = 1000;
        [SerializeField] private int maxHullDurability = 1000;
        [SerializeField] private bool isLocalShip = false;
        [SerializeField] private GameObject shipModel;
        [SerializeField] private GameObject shipDamageVFX;
        [SerializeField] private GameObject shipDestroyVFX;

        [Header("Hareket Ayarları")]
        [SerializeField] private float maxSpeed = 15f;
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private float sailDeployment = 0f;

        [Header("Ses Efektleri")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip destroySound;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Gemi verilerini ayarlar
        /// </summary>
        /// <param name="id">Gemi ID'si</param>
        /// <param name="name">Gemi adı</param>
        /// <param name="currentHull">Mevcut gövde dayanıklılığı</param>
        /// <param name="maxHull">Maksimum gövde dayanıklılığı</param>
        public void SetShipData(string id, string name, int currentHull, int maxHull)
        {
            shipId = id;
            shipName = name;
            currentHullDurability = currentHull;
            maxHullDurability = maxHull;

            // Gemi adını güncelle
            gameObject.name = $"Ship_{name}_{id}";

            // Diğer ayarlamaları yap
            UpdateVisuals();
        }

        /// <summary>
        /// Gemi görsel öğelerini günceller
        /// </summary>
        private void UpdateVisuals()
        {
            // Gemi durumuna göre görsel değişiklikleri yap
            if (shipModel != null)
            {
                // Hasar durumuna göre görsel ayarlamaları yapabilirsiniz
                float healthPercentage = (float)currentHullDurability / maxHullDurability;

                // Hasara göre efektleri aktifleştir/deaktifleştir
                if (shipDamageVFX != null)
                {
                    shipDamageVFX.SetActive(healthPercentage < 0.5f);
                }
            }
        }

        /// <summary>
        /// Gemiye hasar verir
        /// </summary>
        /// <param name="damageAmount">Hasar miktarı</param>
        public void TakeDamage(int damageAmount)
        {
            if (currentHullDurability <= 0) return; // Gemi zaten yok edilmiş

            currentHullDurability -= damageAmount;

            // Ses efekti çal
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }

            // Geminin durumunu kontrol et
            if (currentHullDurability <= 0)
            {
                currentHullDurability = 0;
                OnShipDestroyed();
            }
            else
            {
                UpdateVisuals();
            }
        }

        /// <summary>
        /// Gemi yok edildiğinde çağrılır
        /// </summary>
        private void OnShipDestroyed()
        {
            // Yok edilme efektlerini oynat
            if (shipDestroyVFX != null)
            {
                shipDestroyVFX.SetActive(true);
            }

            // Yok edilme sesini çal
            if (audioSource != null && destroySound != null)
            {
                audioSource.PlayOneShot(destroySound);
            }

            // Görsel modeli devre dışı bırak
            if (shipModel != null)
            {
                shipModel.SetActive(false);
            }

            // Yerel gemi ise oyun mantığını buna göre ayarla
            if (isLocalShip)
            {
                // Oyuncuya ölüm/yenilgi bildirimi gönder
                // GameManager veya UI Manager ile etkileşime geçebilirsiniz
            }

            // Yok edilme sonrası temizlik işlemleri
            Invoke(nameof(CleanupDestroyedShip), 3.0f);
        }

        /// <summary>
        /// Yok edilen gemiyi temizler
        /// </summary>
        private void CleanupDestroyedShip()
        {
            // Yerel gemi değilse yok et
            if (!isLocalShip)
            {
                Destroy(gameObject);
            }
            else
            {
                // Yerel gemi ise yeniden doğma veya diğer mantığı uygula
            }
        }

        /// <summary>
        /// Gemiyi tamir eder
        /// </summary>
        /// <param name="repairAmount">Tamir miktarı</param>
        public void RepairShip(int repairAmount)
        {
            if (currentHullDurability >= maxHullDurability) return; // Zaten tam sağlıklı

            currentHullDurability += repairAmount;
            if (currentHullDurability > maxHullDurability)
            {
                currentHullDurability = maxHullDurability;
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Gemi sağlık durumunu yüzde olarak döndürür
        /// </summary>
        public float GetHealthPercentage()
        {
            return (float)currentHullDurability / maxHullDurability;
        }

        /// <summary>
        /// Gemi ID'sini döndürür
        /// </summary>
        public string GetShipId()
        {
            return shipId;
        }

        /// <summary>
        /// Gemi adını döndürür
        /// </summary>
        public string GetShipName()
        {
            return shipName;
        }

        /// <summary>
        /// Yelken açıklık oranını döndürür (0-1 arası)
        /// </summary>
        public float GetSailDeployment()
        {
            return sailDeployment;
        }

        /// <summary>
        /// Geminin mevcut hızını döndürür
        /// </summary>
        public float GetCurrentSpeed()
        {
            return currentSpeed;
        }

        /// <summary>
        /// Geminin maksimum hızını döndürür
        /// </summary>
        public float GetMaxSpeed()
        {
            return maxSpeed;
        }
    }
}
