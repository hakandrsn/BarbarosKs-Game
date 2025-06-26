using UnityEngine;
using UnityEngine.UI;
using TMPro;
// using Project.Scripts.Network; Artık ağ isim alanına ihtiyacımız yok.

namespace BarbarosKs.UI
{
    public class TargetMarker : MonoBehaviour
    {
        [SerializeField] private Image markerImage;
        [SerializeField] private TextMeshProUGUI targetNameText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private Sprite enemySprite;
        [SerializeField] private Sprite objectiveSprite;
        [SerializeField] private Sprite friendlySprite;

        private Transform _target;
        private Transform _playerTransform;
        private string _networkId;

        private void Awake()
        {
            // Oyuncu referansını bir kere bulup saklamak performansı artırır.
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogWarning("TargetMarker: Sahnede 'Player' tag'ine sahip oyuncu bulunamadı!");
            }
        }

        /// <summary>
        /// Bu işaretçiyi belirli bir hedef için başlatır.
        /// </summary>
        /// <param name="targetTransform">İzlenecek nesnenin Transform'u.</param>
        /// <param name="targetNetworkId">Hedefin ağ kimliği (ileride kullanılabilir).</param>
        public void Initialize(Transform targetTransform, string targetNetworkId = "")
        {
            _target = targetTransform;
            _networkId = targetNetworkId;

            // Hedef tipine göre ikon ve renk seçimi
            if (markerImage != null)
            {
                if (_target.CompareTag("Enemy"))
                {
                    markerImage.sprite = enemySprite;
                    markerImage.color = Color.red;
                }
                else if (_target.CompareTag("Objective"))
                {
                    markerImage.sprite = objectiveSprite;
                    markerImage.color = Color.yellow;
                }
                else if (_target.CompareTag("Friendly"))
                {
                    markerImage.sprite = friendlySprite;
                    markerImage.color = Color.green;
                }
            }

            // Hedef adını ayarla
            if (targetNameText != null)
            {
                string targetName = _target.name.Replace("(Clone)", "").Trim();

                // Eğer hedefin bir düşman bileşeni varsa, ismini zenginleştir
                if (_target.TryGetComponent<Enemies.EnemyAI>(out var enemyAI))
                {
                    // Düşman tipini de ekleyebiliriz.
                    targetName = $"{enemyAI.dusmanTipi} ({targetName})";
                }

                targetNameText.text = targetName;
            }
        }

        private void Update()
        {
            // Eğer hedef yok olduysa (örneğin, düşman öldü), bu işaretçiyi de yok et.
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            // Oyuncu ve hedef arasındaki mesafeyi hesapla ve UI'da göster.
            if (distanceText != null && _playerTransform != null)
            {
                float distance = Vector3.Distance(_playerTransform.position, _target.position);
                distanceText.text = $"{distance:F0}m"; // "F0" ondalıksız göstermesini sağlar (örn: 125m)
            }
        }
    }
}