using UnityEngine;
using BarbarosKs.Combat;

namespace BarbarosKs.Utils
{
    /// <summary>
    /// Shrapnel gülle'sini test etmek için test script'i
    /// </summary>
    public class ShrapnelTester : MonoBehaviour
    {
        [Header("Test Ayarları")]
        [SerializeField] private GameObject shrapnelPrefab;
        [SerializeField] private Transform target;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private int damage = 25;

        [Header("Debug")]
        [SerializeField] private bool autoFindTarget = true;
        [SerializeField] private string targetName = "TestEnemy";

        private void Start()
        {
            // Otomatik hedef bulma
            if (autoFindTarget && target == null)
            {
                GameObject foundTarget = GameObject.Find(targetName);
                if (foundTarget != null)
                {
                    target = foundTarget.transform;
                    Debug.Log($"🎯 [SHRAPNEL TESTER] Hedef bulundu: {target.name}");
                }
            }

            // Spawn point yoksa kendi pozisyonunu kullan
            if (spawnPoint == null)
            {
                spawnPoint = transform;
            }
        }

        [ContextMenu("Test Shrapnel")]
        public void TestShrapnel()
        {
            if (shrapnelPrefab == null)
            {
                Debug.LogError("❌ [SHRAPNEL TESTER] Shrapnel prefab atanmamış!");
                return;
            }

            if (target == null)
            {
                Debug.LogError("❌ [SHRAPNEL TESTER] Hedef atanmamış!");
                return;
            }

            // Shrapnel oluştur
            GameObject shrapnelObj = Instantiate(shrapnelPrefab, spawnPoint.position, Quaternion.identity);
            
            // Projectile component'ını al ve initialize et
            if (shrapnelObj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Initialize(damage, target, gameObject);
                Debug.Log($"🚀 [SHRAPNEL TESTER] Shrapnel fırlatıldı! Damage: {damage}, Target: {target.name}");
            }
            else
            {
                Debug.LogError("❌ [SHRAPNEL TESTER] Projectile component bulunamadı!");
                Destroy(shrapnelObj);
            }
        }

        private void Update()
        {
            // Space tuşu ile test
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TestShrapnel();
            }
        }

        private void OnDrawGizmos()
        {
            // Spawn point'i göster
            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            }

            // Hedefi göster
            if (target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(target.position, 1f);
                
                // Spawn'dan hedefe çizgi çiz
                if (spawnPoint != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(spawnPoint.position, target.position);
                }
            }
        }
    }
} 