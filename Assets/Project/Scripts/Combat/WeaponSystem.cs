using System.Collections;
using System.Collections.Generic;
using Project.Scripts.Interfaces;
using UnityEngine;

namespace BarbarosKs.Combat
{
    public class WeaponSystem : MonoBehaviour
    {
        [System.Serializable]
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

        [Header("Silah Ayarları")]
        [SerializeField] private WeaponData[] availableWeapons;
        [SerializeField] private int currentWeaponIndex = 0;

        [Header("Atış Ayarları")]
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private LayerMask targetLayers;

        // Özel değişkenler
        private WeaponData currentWeapon => availableWeapons[currentWeaponIndex];
        private GameObject activeWeaponInstance;
        private float lastAttackTime;
        private AudioSource audioSource;
        private bool isAttacking;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && availableWeapons.Length > 0 && availableWeapons[0].attackSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            // İlk silahı kuşan
            if (availableWeapons.Length > 0)
            {
                EquipWeapon(currentWeaponIndex);
            }
        }

        public void Attack()
        {
            if (isAttacking || availableWeapons.Length == 0) return;

            // Saldırı hızı kontrolü
            if (Time.time < lastAttackTime + (1f / currentWeapon.attackSpeed))
            {
                return;
            }

            lastAttackTime = Time.time;

            // Saldırı sesi
            if (audioSource != null && currentWeapon.attackSound != null)
            {
                audioSource.PlayOneShot(currentWeapon.attackSound);
            }

            // Saldırı efekti
            if (currentWeapon.attackEffect != null)
            {
                currentWeapon.attackEffect.Play();
            }

            // Silah tipine göre farklı saldırı mekanikleri
            if (currentWeapon.isRanged)
            {
                RangedAttack();
            }
            else
            {
                MeleeAttack();
            }
        }

        public void SwitchWeapon(int direction)
        {
            if (availableWeapons.Length <= 1) return;

            // Yeni silah indeksi hesapla
            int newIndex = currentWeaponIndex + direction;

            // Sınırları kontrol et ve döngüsel olarak dolaş
            if (newIndex < 0)
            {
                newIndex = availableWeapons.Length - 1;
            }
            else if (newIndex >= availableWeapons.Length)
            {
                newIndex = 0;
            }

            EquipWeapon(newIndex);
        }

        private void EquipWeapon(int weaponIndex)
        {
            // Aktif silah varsa yok et
            if (activeWeaponInstance != null)
            {
                Destroy(activeWeaponInstance);
            }

            currentWeaponIndex = weaponIndex;

            // Yeni silahı oluştur
            if (currentWeapon.weaponPrefab != null && currentWeapon.weaponHolder != null)
            {
                activeWeaponInstance = Instantiate(currentWeapon.weaponPrefab, 
                    currentWeapon.weaponHolder.position, 
                    currentWeapon.weaponHolder.rotation, 
                    currentWeapon.weaponHolder);
            }
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
            Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * currentWeapon.range * 0.5f, 
                currentWeapon.range * 0.5f, targetLayers);

            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(currentWeapon.damage);
                }
            }

            // Saldırı bekleme süresi
            yield return new WaitForSeconds(0.5f);

            isAttacking = false;
        }

        private void RangedAttack()
        {
            if (currentWeapon.projectilePrefab == null || projectileSpawnPoint == null) return;

            // Projektil oluştur
            GameObject projectile = Instantiate(currentWeapon.projectilePrefab, 
                projectileSpawnPoint.position, 
                projectileSpawnPoint.rotation);

            // Projektile hasar değerini ve hedef katmanını bildir
            if (projectile.TryGetComponent<Projectile>(out var projectileComponent))
            {
                projectileComponent.Initialize(currentWeapon.damage, targetLayers);
            }

            // Projektile hız ekle
            if (projectile.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = projectileSpawnPoint.forward * currentWeapon.projectileSpeed;
            }
        }

        // Unity Editor için gizmolar
        private void OnDrawGizmosSelected()
        {
            if (availableWeapons == null || availableWeapons.Length == 0 || currentWeaponIndex >= availableWeapons.Length) return;

            Gizmos.color = Color.red;

            if (!currentWeapon.isRanged)
            {
                // Yakın mesafe silahlarının etki alanı
                Gizmos.DrawWireSphere(transform.position + transform.forward * currentWeapon.range * 0.5f, 
                    currentWeapon.range * 0.5f);
            }
            else if (projectileSpawnPoint != null)
            {
                // Uzak mesafe silahlarının atış doğrultusu
                Gizmos.DrawRay(projectileSpawnPoint.position, projectileSpawnPoint.forward * 5f);
            }
        }

        // Geliştirici metodları
        public WeaponData GetCurrentWeapon() => currentWeapon;
        public int GetCurrentWeaponIndex() => currentWeaponIndex;
    }
}
