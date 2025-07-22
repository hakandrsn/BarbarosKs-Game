// Filename: PlayerHUDController.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUDController : MonoBehaviour
{
    [Header("Player Frame Components")] [SerializeField]
    private GameObject _playerFramePanel;

    [SerializeField] private Slider _playerHealthSlider;
    [SerializeField] private TextMeshProUGUI _playerHealthText;
    [SerializeField] private TextMeshProUGUI _playerAttackRate;
    [SerializeField] private TextMeshProUGUI _playerCurrentVigor;
    [SerializeField] private TextMeshProUGUI _playerRange;

    private Health _localPlayerHealth;
    private ShipStats _localPlayerShipStats;

    private void Awake()
    {
        // Oyuncumuzun gemisi hazır olduğunda bu olayı dinle.
        PlayerController.OnLocalPlayerShipReady += OnLocalPlayerReady;
        _playerFramePanel.SetActive(false); // Başlangıçta paneli gizle.
    }

    private void OnDestroy()
    {
        PlayerController.OnLocalPlayerShipReady -= OnLocalPlayerReady;
        if (_localPlayerHealth != null)
        {
            // Abonelikleri iptal et
            _localPlayerHealth.CurrentHealth.OnValueChanged -= UpdateCurrentHealthUI;
            _localPlayerHealth.MaxHealth.OnValueChanged -= UpdateMaxHealthUI;
        }
    }

    private void OnLocalPlayerReady(Transform playerTransform)
    {
        _playerFramePanel.SetActive(true);
        _localPlayerHealth = playerTransform.GetComponent<Health>();
        _localPlayerShipStats = playerTransform.GetComponent<ShipStats>();


        if (_localPlayerHealth == null) return;

        // Hem anlık can hem de maksimum can değeri değiştiğinde UI'ı güncellemek için abone ol.
        _localPlayerHealth.CurrentHealth.OnValueChanged += UpdateCurrentHealthUI;
        _localPlayerHealth.MaxHealth.OnValueChanged += UpdateMaxHealthUI;

        // UI'ı mevcut verilerle ilk kez doldur.
        UpdateMaxHealthUI(0, _localPlayerHealth.MaxHealth.Value);
        UpdateCurrentHealthUI(0, _localPlayerHealth.CurrentHealth.Value);
        UpdateAttackRate(0, _localPlayerShipStats.AttackRate.Value);
        UpdateRange(0, _localPlayerShipStats.Range.Value);
        UpdateCurrentVigor(0, _localPlayerShipStats.CurrentVigor);
    }

    // Bu metot SADECE slider'ın maksimum değerini ayarlar.
    private void UpdateMaxHealthUI(int previousValue, int newValue)
    {
        _playerHealthSlider.maxValue = newValue;
    }

    // Bu metot SADECE slider'ın anlık değerini ve metni günceller.
    private void UpdateCurrentHealthUI(int previousValue, int newValue)
    {
        // DÜZELTME: NetworkVariable'ın içindeki değere .Value ile erişiyoruz.
        _playerHealthSlider.value = newValue;
        _playerHealthText.text = $"{newValue} / {_localPlayerHealth.MaxHealth.Value}";
    }

    private void UpdateAttackRate(int previousValue, float newValue)
    {
        _playerAttackRate.text = newValue.ToString();
    }

    private void UpdateRange(int previousValue, float newValue)
    {
        _playerRange.text = newValue.ToString();
    }

    private void UpdateCurrentVigor(int previousValue, int newValue)
    {
        _playerCurrentVigor.text = newValue.ToString();
    }
}