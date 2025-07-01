using Project.Scripts.Interfaces;
using UnityEngine;
using BarbarosKs.Core;

namespace BarbarosKs.Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Projektil Ayarları (GameSettings'den Override)")]
        [SerializeField] private int damage = 10;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private AudioClip hitSound;
        private bool hasHit;

        // GameSettings referansı
        private GameSettings gameSettings;
        
        // Hareket için değişkenler
        private Transform specificTarget;
        private GameObject shooter;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Vector3 previousPosition; // Önceki pozisyonu kaydetmek için
        private float totalDistance;
        private float flightTime; // Artık hesaplanacak
        private float currentTime;
        private bool isFlying;

        private void Awake()
        {
            // GameSettings'i al
            gameSettings = GameSettings.Instance;
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
            previousPosition = startPosition; // Başlangıç pozisyonunu kaydet
            
            // Mesafeyi hesapla
            totalDistance = Vector3.Distance(startPosition, targetPosition);
            
            // GameSettings'den hıza göre uçma süresini hesapla
            flightTime = gameSettings.CalculateFlightTime(totalDistance);
            
            // Menzil kontrolü
            if (!gameSettings.IsWithinRange(totalDistance))
            {
                Debug.LogWarning($"⚠️ [PROJECTILE] Hedef menzil dışında! Mesafe: {totalDistance:F1}m, Max: {gameSettings.maxProjectileRange}m");
            }
            
            currentTime = 0f;
            isFlying = true;
            
            Debug.Log($"🚀 [PROJECTILE] Hızlı mermi fırlatıldı!");
            Debug.Log($"   Hedef: {specificTarget.name}");
            Debug.Log($"   Mesafe: {totalDistance:F1}m");
            Debug.Log($"   Hız: {gameSettings.projectileSpeed} m/s");
            Debug.Log($"   Uçma Süresi: {flightTime:F2}s");
            
            // Güvenlik için maksimum yaşam süresi
            Destroy(gameObject, gameSettings.projectileMaxLifetime);
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
            
            // Önceki pozisyonu kaydet (rotasyon için)
            previousPosition = transform.position;
            
            // ✅ YENİ: Hız tabanlı hareket
            // X-Z düzleminde linear hareket
            Vector3 horizontalPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            
            // Y ekseninde hafif parabolik yörünge (GameSettings'den yükseklik)
            float arcHeight = gameSettings.projectileArcHeight;
            float heightOffset = arcHeight * 4 * progress * (1 - progress); // Parabolik eğri
            
            // Final pozisyon
            Vector3 currentPosition = new Vector3(horizontalPosition.x, horizontalPosition.y + heightOffset, horizontalPosition.z);
            transform.position = currentPosition;
            
            // ✅ YENİ: Hareket yönüne doğru döndür (GameSettings'den hız)
            Vector3 moveDirection = (currentPosition - previousPosition).normalized;
            if (moveDirection != Vector3.zero)
            {
                float rotationSpeed = gameSettings.projectileRotationSpeed * Time.deltaTime;
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed);
            }
        }

        private void HitTarget()
        {
            if (hasHit) return;
            hasHit = true;
            
            Debug.Log($"💥 [PROJECTILE] Hızlı mermi hedefe çarptı! Target: {specificTarget.name}");
            Debug.Log($"   Uçma Süresi: {currentTime:F2}s (Planlanan: {flightTime:F2}s)");

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

                // GameSettings'den efekt kalma süresi
                Destroy(hitEffect, gameSettings.hitEffectDuration);
            }

            // Çarpma sesi (GameSettings'den ses seviyesi)
            if (hitSound != null) 
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position, gameSettings.combatSoundVolume);
            }

            // Kendini yok et
            Destroy(gameObject);
        }
        
        // Trigger kullanarak erken çarpışma kontrolü
        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;
            
            // Sadece hedef objesi ile çarpışırsa
            if (specificTarget != null && other.gameObject == specificTarget.gameObject)
            {
                Debug.Log($"🎯 [PROJECTILE] Trigger ile erken hedefe ulaşıldı: {other.name}");
                HitTarget();
            }
        }

        /// <summary>
        /// Projektili initialize eder. FlightTime artık hesaplanacak.
        /// </summary>
        public void Initialize(int newDamage, Transform target, GameObject shooterObject = null)
        {
            damage = newDamage;
            specificTarget = target;
            shooter = shooterObject;
            
            Debug.Log($"🔧 [PROJECTILE] Initialize edildi - Damage: {damage}, Target: {target?.name}");
        }

        /// <summary>
        /// Network'ten gelen verilerle projektili initialize eder
        /// </summary>
        public void InitializeFromNetwork(int newDamage, Transform target, float networkFlightTime)
        {
            damage = newDamage;
            specificTarget = target;
            flightTime = networkFlightTime; // Network'ten gelen süreyi kullan
            
            Debug.Log($"🔧 [PROJECTILE] Network initialize edildi - Damage: {damage}, Target: {target?.name}, FlightTime: {flightTime:F2}s");
        }

        /// <summary>
        /// Debug için mevcut projektil bilgilerini gösterir
        /// </summary>
        [ContextMenu("Show Projectile Info")]
        public void ShowProjectileInfo()
        {
            if (gameSettings == null) gameSettings = GameSettings.Instance;
            
            Debug.Log($"🔍 [PROJECTILE INFO]:");
            Debug.Log($"   Speed: {gameSettings.projectileSpeed} m/s");
            Debug.Log($"   Arc Height: {gameSettings.projectileArcHeight} m");
            Debug.Log($"   Max Range: {gameSettings.maxProjectileRange} m");
            Debug.Log($"   Is Flying: {isFlying}");
            Debug.Log($"   Distance: {totalDistance:F1} m");
            Debug.Log($"   Flight Time: {flightTime:F2} s");
        }

        #region Unity Editor Gizmos

        /// <summary>
        /// Unity Editor'da projektil yolunu ve bilgilerini gösterir
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (gameSettings == null) gameSettings = GameSettings.Instance;
            if (gameSettings == null) return;

            // Başlangıç pozisyonu (yeşil küre)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPosition, 1f);

            // Hedef pozisyonu (kırmızı küre)
            if (specificTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(targetPosition, 1f);

                // Direkt çizgi (gri)
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(startPosition, targetPosition);

                // Projektil yolu (parabolik - sarı noktalı çizgi)
                DrawProjectileTrajectory();

                // Bilgi label'ı
                #if UNITY_EDITOR
                UnityEditor.Handles.color = isFlying ? Color.green : Color.white;
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2f,
                    $"🚀 Projektil Bilgileri:\n" +
                    $"   Mesafe: {totalDistance:F1}m\n" +
                    $"   Hız: {gameSettings.projectileSpeed} m/s\n" +
                    $"   Uçma Süresi: {flightTime:F2}s\n" +
                    $"   Mevcut İlerleme: {(currentTime / flightTime * 100):F0}%\n" +
                    $"   Durum: {(isFlying ? "✈️ Uçuyor" : "🎯 Hedef")}"
                );
                #endif
            }
        }

        /// <summary>
        /// Parabolik projektil yolunu çizer
        /// </summary>
        private void DrawProjectileTrajectory()
        {
            if (specificTarget == null || gameSettings == null) return;

            Gizmos.color = Color.yellow;
            
            // Yolu 20 parçaya böl ve noktalı çizgi çiz
            int segments = 20;
            Vector3 previousPoint = startPosition;
            
            for (int i = 1; i <= segments; i++)
            {
                float progress = (float)i / segments;
                
                // X-Z düzleminde linear hareket
                Vector3 horizontalPosition = Vector3.Lerp(startPosition, targetPosition, progress);
                
                // Y ekseninde parabolik yörünge
                float arcHeight = gameSettings.projectileArcHeight;
                float heightOffset = arcHeight * 4 * progress * (1 - progress);
                
                Vector3 currentPoint = new Vector3(horizontalPosition.x, horizontalPosition.y + heightOffset, horizontalPosition.z);
                
                // Çizgi çiz
                Gizmos.DrawLine(previousPoint, currentPoint);
                
                // Her 5. noktada küçük küre çiz
                if (i % 5 == 0)
                {
                    Gizmos.DrawWireSphere(currentPoint, 0.3f);
                }
                
                previousPoint = currentPoint;
            }
        }

        /// <summary>
        /// Oyun sırasında da görünür minimal gizmos
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!isFlying) return; // Sadece uçarken göster
            if (!Application.isPlaying) return; // Sadece oyun oynarken

            // Mevcut pozisyon (küçük mavi küre)
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Hedefe olan yön (mavi ok)
            if (specificTarget != null)
            {
                Vector3 direction = (specificTarget.position - transform.position).normalized;
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, direction * 5f);
            }
        }

        #endregion
    }
}