// Filename: TargetingUIManager.cs (Updated Version)

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetingUIManager : MonoBehaviour
{
    [Header("Target Frame Components")] [SerializeField]
    private GameObject _targetFramePanel;

    [SerializeField] private Slider _targetHealthSlider;
    [SerializeField] private TextMeshProUGUI _targetNameText;

    private Health _currentTargetHealth;

    private void Awake()
    {
        PlayerController.OnTargetChanged += HandleTargetChanged;
        _targetFramePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        PlayerController.OnTargetChanged -= HandleTargetChanged;
        // Eski hedefin can olayından aboneliği iptal etmeyi unutma.
        if (_currentTargetHealth != null)
        {
            _currentTargetHealth.CurrentHealth.OnValueChanged -= UpdateTargetHealthUI;
        }
    }

    private void HandleTargetChanged(Targetable newTarget)
    {
        // Önceki hedefin can olayından aboneliği iptal et.
        if (_currentTargetHealth != null)
        {
            _currentTargetHealth.CurrentHealth.OnValueChanged -= UpdateTargetHealthUI;
        }

        if (newTarget)
        {
            _targetFramePanel.SetActive(true);
            var playerInfo = newTarget.GetComponent<PlayerInfo>();
            if (playerInfo)
            {
                // Hem oyuncu adını hem de gemi adını göster.
                // newTarget.gameObject.name, Unity'deki objenin adını verir.
                // playerInfo.Username.Value ise network'ten gelen oyuncu adını verir.
                _targetNameText.text = $"{playerInfo.Username.Value}\n<size=22>{newTarget.gameObject.name}</size>";
            }

            _currentTargetHealth = newTarget.GetComponent<Health>();
            if (_currentTargetHealth)
            {
                // Yeni hedefin can olayına abone ol.
                _currentTargetHealth.CurrentHealth.OnValueChanged += UpdateTargetHealthUI;
                // UI'ı ilk değerlerle güncelle.
                UpdateTargetHealthUI(0, _currentTargetHealth.CurrentHealth.Value);
            }
        }
        else
        {
            _targetFramePanel.SetActive(false);
            _currentTargetHealth = null;
        }
    }

    private void UpdateTargetHealthUI(int previousValue, int newValue)
    {
        if (_targetFramePanel.activeSelf)
        {
            _targetHealthSlider.maxValue = 100; 
            _targetHealthSlider.value = newValue;
            // İsteğe bağlı olarak can metnini de güncelleyebilirsiniz.
        }
    }
}