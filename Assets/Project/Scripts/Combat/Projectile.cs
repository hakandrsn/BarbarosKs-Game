using Project.Scripts.Interfaces;
using UnityEngine;

namespace BarbarosKs.Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Projektil Ayarları")] [SerializeField]
        private float flightTime = 1f; // Sabit 3 saniye
        
        [Header("Yörünge Ayarları")] [SerializeField]
        private float arcHeight = 1f; // Ne kadar yükseklik

        [SerializeField] private int damage = 10;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private AudioClip hitSound;
        private bool hasHit;

        // Özel değişkenler
        private Transform specificTarget; // Sadece bu hedefe çarpacak
        private GameObject shooter; // Ateş eden nesne
        
        // Hareket için
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float currentTime;
        private bool isFlying;

        private void Awake()
        {
            // Rigidbody artık gerekmiyor - direkt transform hareket
        }

        private void Start()
        {
            if (specificTarget != null)
            {
                StartFlying();
            }
            else
            {
                Debug.LogError("❌ [PROJECTILE] Hedef belirlenmedi!");
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            if (isFlying && !hasHit && specificTarget != null)
            {
                UpdateProjectileMovement();
            }
        }
        
        private void StartFlying()
        {
            startPosition = transform.position;
            targetPosition = specificTarget.position;
            currentTime = 0f;
            isFlying = true;
            
            Debug.Log($"🚀 [PROJECTILE] Uçuş başladı! Hedef: {specificTarget.name}, Süre: {flightTime}s");
            
            // Güvenlik için maksimum süre
            Destroy(gameObject, flightTime + 1f);
        }
        
        private void UpdateProjectileMovement()
        {
            currentTime += Time.deltaTime;
            float progress = currentTime / flightTime;
            
            if (progress >= 1f)
            {
                // Hedefe ulaştı
                transform.position = targetPosition;
                HitTarget();
                return;
            }
            
            // X-Z düzleminde linear hareket
            Vector3 horizontalPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            
            // Y ekseninde parabolik hareket (yüksel-in)
            float heightOffset = arcHeight * 4 * progress * (1 - progress); // Parabolik eğri
            
            // Final pozisyon
            transform.position = new Vector3(horizontalPosition.x, horizontalPosition.y + heightOffset, horizontalPosition.z);
            
            // Hareket yönüne doğru döndür
            Vector3 lookDirection = (transform.position - startPosition).normalized;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        private void HitTarget()
        {
            if (hasHit) return;
            hasHit = true;
            
            Debug.Log($"💥 [PROJECTILE] Hedefe çarptı! Target: {specificTarget.name}");

            // Hedefe hasar ver
            if (specificTarget.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
                Debug.Log($"💥 [PROJECTILE] {damage} hasar verildi!");
            }

            // Çarpma efekti
            if (hitEffectPrefab != null)
            {
                var hitEffect = Instantiate(hitEffectPrefab,
                    transform.position,
                    Quaternion.identity);

                // Efekti bir süre sonra yok et
                Destroy(hitEffect, 2f);
            }

            // Çarpma sesi
            if (hitSound != null) 
                AudioSource.PlayClipAtPoint(hitSound, transform.position);

            // Kendini yok et
            Destroy(gameObject);
        }
        
        // Trigger kullanarak hedef mesafe kontrolü (opsiyonel)
        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;
            
            // Sadece hedef objesi ile çarpışırsa
            if (specificTarget != null && other.gameObject == specificTarget.gameObject)
            {
                HitTarget();
            }
        }

        public void Initialize(int newDamage, Transform target, GameObject shooterObject = null, float flightTimeOverride = 3f)
        {
            damage = newDamage;
            specificTarget = target;
            shooter = shooterObject;
            flightTime = flightTimeOverride;
        }
    }
}