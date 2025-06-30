using System.Collections;
using Project.Scripts.Interfaces;
using UnityEngine;
using UnityEngine.Events;

// IDamageable için

namespace BarbarosKs.Player
{
    // NOT: Bu script artık sunucudan gelen veriyi görselleştirdiği için,
    // IDamageable arayüzünü burada kullanmak kafa karıştırıcı olabilir.
    // Hasar alınacak asıl varlık sunucudaki Player nesnesidir.
    // Ancak diğer scriptlerle uyumluluk için şimdilik kalabilir.
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int Die1 = Animator.StringToHash("Die");

        [Header("Sağlık Ayarları")] [SerializeField]
        private int maxHealth = 100;

        [SerializeField] private int currentHealth;

        [Header("Efekt ve Ses Ayarları")] [SerializeField]
        private float invincibilityTime = 0.5f; // Hasar aldıktan sonra kısa süreli dokunulmazlık (efekt tekrarı için)

        [SerializeField] private GameObject damageEffectPrefab;
        [SerializeField] private AudioClip damageSound;
        [SerializeField] private AudioClip deathSound;

        // Olaylar (UI gibi diğer scriptlerin dinlemesi için)
        public UnityEvent<int, int> OnHealthChanged = new();
        public UnityEvent OnDeath = new();
        private Animator _animator;
        private AudioSource _audioSource;
        private bool _isDead;

        // Özel değişkenler
        private bool _isInvincible;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            _animator = GetComponent<Animator>();
            currentHealth = maxHealth;
        }

        private void Start()
        {
            // Başlangıçta UI'ın doğru değerle başlaması için olayı tetikle
            OnHealthChanged.Invoke(currentHealth, maxHealth);
        }

        // IDamageable arayüzü için gerekli metot.
        // Bu metot, istemci tarafında anlık bir efekt yaratmak için kullanılabilir (örn: bir tuzağa basma)
        // ama canı kalıcı olarak DEĞİŞTİRMEZ. Kalıcı değişiklik sadece sunucudan gelen veriyle olur.
        public void TakeDamage(int damage)
        {
            if (_isInvincible || _isDead) return;

            PlayDamageEffects();
            StartCoroutine(InvincibilityRoutine());
        }

        public int GetCurrentHealth()
        {
            return currentHealth;
        }

        public int GetMaxHealth()
        {
            return maxHealth;
        }

        /// <summary>
        ///     Sunucudan gelen can verisiyle bu karakterin durumunu günceller.
        ///     Bu metot, NetworkObjectSpawner veya benzeri bir yönetici tarafından çağrılmalıdır.
        /// </summary>
        public void UpdateHealthFromServer(int newCurrentHealth)
        {
            if (_isDead) return;

            // Eğer canımız azaldıysa hasar efektlerini, arttıysa iyileşme efektlerini oynatabiliriz.
            if (newCurrentHealth < currentHealth) PlayDamageEffects();

            currentHealth = Mathf.Clamp(newCurrentHealth, 0, maxHealth);

            // UI ve diğer dinleyicilere canın değiştiğini bildir.
            OnHealthChanged.Invoke(currentHealth, maxHealth);

            // Sunucudan gelen veriye göre ölüm kontrolü
            if (currentHealth <= 0) Die();
        }

        /// <summary>
        ///     Hasar aldığında çalışacak olan ses ve görsel efektleri oynatır.
        /// </summary>
        private void PlayDamageEffects()
        {
            if (_isInvincible) return;

            if (damageSound != null && _audioSource != null) _audioSource.PlayOneShot(damageSound);

            if (damageEffectPrefab)
            {
                var effect = Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            if (_animator) _animator.SetTrigger(Hit);
        }

        /// <summary>
        ///     Karakterin canı sıfıra ulaştığında çalışır.
        /// </summary>
        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            if (deathSound != null && _audioSource != null) _audioSource.PlayOneShot(deathSound);

            if (_animator != null) _animator.SetTrigger(Die1);

            OnDeath.Invoke();

            // Bu script artık ölüm mesajı GÖNDERMEZ.
            // Sadece sunucudan gelen "canın sıfır" bilgisine göre görsel olarak ölür.
        }

        /// <summary>
        ///     Kısa süreli hasar almazlık sağlar (görsel/ses efektlerinin üst üste binmemesi için).
        /// </summary>
        private IEnumerator InvincibilityRoutine()
        {
            _isInvincible = true;
            yield return new WaitForSeconds(invincibilityTime);
            _isInvincible = false;
        }
    }
}