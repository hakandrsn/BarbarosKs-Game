using Project.Scripts.Interfaces;
using UnityEngine;

namespace BarbarosKs.Testing
{
    public class TestEnemy : MonoBehaviour, IDamageable
    {
        [Header("Test Düşman Ayarları")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = Color.red;
        [SerializeField] private Color hitColor = Color.white;
        [SerializeField] private Color deadColor = Color.gray;
        
        private Renderer objectRenderer;
        private Material originalMaterial;
        
        private void Awake()
        {
            currentHealth = maxHealth;
            objectRenderer = GetComponent<Renderer>();
            
            // Malzeme rengi ayarla
            if (objectRenderer != null)
            {
                originalMaterial = objectRenderer.material;
                objectRenderer.material.color = normalColor;
            }
            
            Debug.Log($"🎯 [TEST-ENEMY] {gameObject.name} oluşturuldu - HP: {currentHealth}/{maxHealth}");
        }
        
        public void TakeDamage(int damage)
        {
            if (currentHealth <= 0) return; // Zaten ölü
            
            currentHealth -= damage;
            Debug.Log($"💥 [TEST-ENEMY] {gameObject.name} hasar aldı! Damage: {damage}, HP: {currentHealth}/{maxHealth}");
            
            // Visual feedback
            if (objectRenderer != null)
            {
                // Hit effect - beyaz yanıp söner
                StartCoroutine(HitFlash());
            }
            
            // Ölüm kontrolü
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        private System.Collections.IEnumerator HitFlash()
        {
            // Beyaz yap
            objectRenderer.material.color = hitColor;
            yield return new WaitForSeconds(0.1f);
            
            // Normal renge döndür (eğer yaşıyorsa)
            if (currentHealth > 0)
                objectRenderer.material.color = normalColor;
        }
        
        private void Die()
        {
            Debug.Log($"💀 [TEST-ENEMY] {gameObject.name} öldü!");
            
            // Rengi gri yap
            if (objectRenderer != null)
                objectRenderer.material.color = deadColor;
            
            // Collider'ı kapat (daha fazla hasar almasın)
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            
            // 3 saniye sonra yok et
            Destroy(gameObject, 3f);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Health bar çiz
            Gizmos.color = Color.green;
            Vector3 healthBarPos = transform.position + Vector3.up * 2f;
            float healthPercent = (float)currentHealth / maxHealth;
            
            // Health bar background
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(healthBarPos, new Vector3(2f, 0.2f, 0.1f));
            
            // Health bar fill
            Gizmos.color = Color.green;
            Gizmos.DrawCube(healthBarPos, new Vector3(2f * healthPercent, 0.2f, 0.1f));
        }
    }
} 