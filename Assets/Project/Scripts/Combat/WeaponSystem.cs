using System;
using System.Collections;
using BarbarosKs.Player;
using Project.Scripts.Interfaces;
using UnityEngine;

namespace BarbarosKs.Combat
{
    public class WeaponSystem : MonoBehaviour
    {
        [Header("Silah Ayarları")] [SerializeField]
        private WeaponData[] availableWeapons;

        [SerializeField] private int currentWeaponIndex;

        [Header("Atış Ayarları")] [SerializeField]
        private Transform projectileSpawnPoint;

        [SerializeField] private LayerMask targetLayers;
        private GameObject activeWeaponInstance;
        private AudioSource audioSource;
        private bool isAttacking;
        private float lastAttackTime;

        // Özel değişkenler
        private WeaponData currentWeapon => availableWeapons[currentWeaponIndex];

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && availableWeapons.Length > 0 && availableWeapons[0].attackSound != null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            // İlk silahı kuşan
            if (availableWeapons.Length > 0) EquipWeapon(currentWeaponIndex);
        }

        // Unity Editor için gizmolar
        private void OnDrawGizmosSelected()
        {
            if (availableWeapons == null || availableWeapons.Length == 0 ||
                currentWeaponIndex >= availableWeapons.Length) return;

            // GameSettings'den menzil bilgisini al
            var gameSettings = BarbarosKs.Core.GameSettings.Instance;

            if (!currentWeapon.isRanged)
            {
                // Yakın mesafe silahlarının etki alanı (kırmızı)
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + transform.forward * currentWeapon.range * 0.5f,
                    currentWeapon.range * 0.5f);
            }
            else if (projectileSpawnPoint != null)
            {
                // Uzak mesafe silahları için gelişmiş görselleştirme
                
                // 1. Ateş noktası (mavi küre)
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.5f);
                
                // 2. Ateş yönü (mavi ok)
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(projectileSpawnPoint.position, projectileSpawnPoint.forward * 10f);
                
                // 3. GameSettings'den maksimum menzil (turuncu çember)
                if (gameSettings != null)
                {
                    Gizmos.color = Color.yellow;
                    DrawCircle(transform.position, gameSettings.maxProjectileRange);
                    
                    // Menzil bilgisi label
                    #if UNITY_EDITOR
                    UnityEditor.Handles.color = Color.yellow;
                    UnityEditor.Handles.Label(
                        transform.position + Vector3.up * 5f,
                        $"🎯 Maksimum Menzil: {gameSettings.maxProjectileRange}m\n" +
                        $"⚡ Mermi Hızı: {gameSettings.projectileSpeed} m/s\n" +
                        $"📈 Yörünge: {gameSettings.projectileArcHeight}m"
                    );
                    #endif
                }
                
                // 4. Silah menzili (eğer tanımlıysa - kırmızı)
                if (currentWeapon.range > 0)
                {
                    Gizmos.color = Color.red;
                    DrawCircle(transform.position, currentWeapon.range);
                }
            }
        }

        /// <summary>
        /// Manuel olarak çember çizer (Gizmos.DrawWireCircle Unity'de yok)
        /// </summary>
        private void DrawCircle(Vector3 center, float radius, int segments = 36)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius, 
                    0, 
                    Mathf.Sin(angle) * radius
                );
                
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }

        public void Attack(Transform target = null)
        {
            if (isAttacking || availableWeapons.Length == 0) return;

            // Saldırı hızı kontrolü
            if (Time.time < lastAttackTime + 1f / currentWeapon.attackSpeed) return;

            lastAttackTime = Time.time;

            // Saldırı sesi
            if (audioSource != null && currentWeapon.attackSound != null)
                audioSource.PlayOneShot(currentWeapon.attackSound);

            // Saldırı efekti
            if (currentWeapon.attackEffect != null) currentWeapon.attackEffect.Play();

            // Silah tipine göre farklı saldırı mekanikleri
            if (currentWeapon.isRanged)
                RangedAttack(target);
            else
                MeleeAttack();
        }

        public void SwitchWeapon(int direction)
        {
            if (availableWeapons.Length <= 1) return;

            // Yeni silah indeksi hesapla
            var newIndex = currentWeaponIndex + direction;

            // Sınırları kontrol et ve döngüsel olarak dolaş
            if (newIndex < 0)
                newIndex = availableWeapons.Length - 1;
            else if (newIndex >= availableWeapons.Length) newIndex = 0;

            EquipWeapon(newIndex);
        }

        private void EquipWeapon(int weaponIndex)
        {
            // Aktif silah varsa yok et
            if (activeWeaponInstance != null) Destroy(activeWeaponInstance);

            currentWeaponIndex = weaponIndex;

            // Yeni silahı oluştur
            if (currentWeapon.weaponPrefab != null && currentWeapon.weaponHolder != null)
                activeWeaponInstance = Instantiate(currentWeapon.weaponPrefab,
                    currentWeapon.weaponHolder.position,
                    currentWeapon.weaponHolder.rotation,
                    currentWeapon.weaponHolder);
        }

        private void MeleeAttack()
        {
            // Yakın mesafe saldırısı
            StartCoroutine(MeleeAttackRoutine());
        }

        private IEnumerator MeleeAttackRoutine()
        {
            isAttacking = true;

            // Animasyon senkronizasyonu için bekle
            yield return new WaitForSeconds(0.2f);

            // Etki alanı içindeki düşmanları bul
            var hitColliders = Physics.OverlapSphere(
                transform.position + transform.forward * currentWeapon.range * 0.5f,
                currentWeapon.range * 0.5f, targetLayers);

            foreach (var hitCollider in hitColliders)
                if (hitCollider.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(currentWeapon.damage);

            // Saldırı bekleme süresi
            yield return new WaitForSeconds(0.5f);

            isAttacking = false;
        }

        private void RangedAttack(Transform target)
        {
            if (currentWeapon.projectilePrefab == null || projectileSpawnPoint == null) return;
            if (target == null) return; // Hedef yoksa ateş etme

            // ✅ Sadece local player gülle spawn eder (network senkronizasyon için)
            // PlayerController'dan local player kontrolü yap
            var playerController = GetComponent<PlayerController>();
            bool isLocalPlayer = playerController != null && playerController.GetIsLocalPlayer();
            
            if (!isLocalPlayer)
            {
                Debug.Log("🚫 [WEAPON] Remote player, gülle spawn edilmedi (network'ten gelecek)");
                return;
            }

            // Projektil oluştur (sadece local player için)
            var projectile = Instantiate(currentWeapon.projectilePrefab,
                projectileSpawnPoint.position,
                projectileSpawnPoint.rotation);

            // Projektile hedef ve diğer bilgileri ver (FlightTime artık GameSettings'den hesaplanacak)
            if (projectile.TryGetComponent<Projectile>(out var projectileComponent))
            {
                projectileComponent.Initialize(currentWeapon.damage, target, gameObject);
                Debug.Log("🚀 [WEAPON] Local player gülle spawn edildi");
            }
        }

        // Geliştirici metodları
        public WeaponData GetCurrentWeapon()
        {
            return currentWeapon;
        }

        public int GetCurrentWeaponIndex()
        {
            return currentWeaponIndex;
        }
        
        // Saldırı hızını dinamik olarak değiştir
        public void ChangeAttackSpeed(float newAttackSpeed)
        {
            if (availableWeapons.Length > 0 && currentWeaponIndex < availableWeapons.Length)
            {
                availableWeapons[currentWeaponIndex].attackSpeed = newAttackSpeed;
                Debug.Log($"🔫 [WEAPON] {currentWeapon.weaponName} saldırı hızı değiştirildi: {newAttackSpeed}");
            }
        }
        
        // Tüm silahların saldırı hızını değiştir
        public void ChangeAllWeaponsAttackSpeed(float newAttackSpeed)
        {
            for (int i = 0; i < availableWeapons.Length; i++)
            {
                availableWeapons[i].attackSpeed = newAttackSpeed;
            }
            Debug.Log($"🔫 [WEAPON] Tüm silahların saldırı hızı değiştirildi: {newAttackSpeed}");
        }

        [Serializable]
        public class WeaponData
        {
            public string weaponName;
            public GameObject weaponPrefab;
            public Transform weaponHolder;
            public int damage;
            public float attackSpeed; // Saniyede kaç atak
            public float range;
            public bool isRanged;
            public GameObject projectilePrefab; // Uzak mesafe silahları için
            public float projectileSpeed; // Uzak mesafe silahları için
            public AudioClip attackSound;
            public ParticleSystem attackEffect;
        }
    }
}