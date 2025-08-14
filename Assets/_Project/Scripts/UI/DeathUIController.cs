// Filename: DeathUIController.cs

using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class DeathUIController : MonoBehaviour
{
	[Header("Death UI References")]
	[SerializeField] private GameObject _deathPanel;
	[SerializeField] private Button _respawnButton;
	[Tooltip("Panelin görüneceği sağlık eşik değeri. Örn: 0 => sağlık 0 veya altına düşünce göster.")]
	[SerializeField] private int _healthThresholdToShow = 0;

	private Health _localPlayerHealth;

	private void Awake()
	{
		if (_deathPanel != null)
		{
			_deathPanel.SetActive(false);
		}
		if (_respawnButton != null)
		{
			_respawnButton.onClick.AddListener(OnRespawnClicked);
		}

		PlayerController.OnLocalPlayerShipReady += OnLocalPlayerShipReady;
	}

	private void OnDestroy()
	{
		PlayerController.OnLocalPlayerShipReady -= OnLocalPlayerShipReady;
		if (_localPlayerHealth != null)
		{
			_localPlayerHealth.CurrentHealth.OnValueChanged -= OnCurrentHealthChanged;
		}
		if (_respawnButton != null)
		{
			_respawnButton.onClick.RemoveListener(OnRespawnClicked);
		}
	}

	private void OnLocalPlayerShipReady(Transform playerTransform)
	{
		// Önce önceki abonelikten ayrıl
		if (_localPlayerHealth != null)
		{
			_localPlayerHealth.CurrentHealth.OnValueChanged -= OnCurrentHealthChanged;
		}

		_localPlayerHealth = playerTransform.GetComponent<Health>();
		if (_localPlayerHealth == null) return;
		_localPlayerHealth.CurrentHealth.OnValueChanged += OnCurrentHealthChanged;

		// Yeni gemi geldiğinde mevcut sağlık durumuna göre paneli güncelle
		UpdatePanelVisibilityForCurrentHealth(_localPlayerHealth.CurrentHealth.Value);
		// Ağ başlatma sırası nedeniyle bir frame sonra tekrar kontrol et (olasılık düşük ama güvenli)
		StartCoroutine(DeferredInitialCheck());
	}

	private void OnCurrentHealthChanged(int previousValue, int newValue)
	{
		UpdatePanelVisibilityForCurrentHealth(newValue);
	}

	private System.Collections.IEnumerator DeferredInitialCheck()
	{
		yield return null; // bir frame bekle
		if (_localPlayerHealth != null)
		{
			UpdatePanelVisibilityForCurrentHealth(_localPlayerHealth.CurrentHealth.Value);
		}
	}

	private void UpdatePanelVisibilityForCurrentHealth(int current)
	{
		if (current <= _healthThresholdToShow)
		{
			ShowPanel();
		}
		else
		{
			HidePanel();
		}
	}

	private void OnRespawnClicked()
	{
		if (_respawnButton != null) _respawnButton.interactable = false;

		var coordinator = FindObjectOfType<RespawnCoordinator>();
		if (coordinator == null)
		{
			Debug.LogError("RespawnCoordinator sahnede bulunamadı.");
			if (_respawnButton != null) _respawnButton.interactable = true;
			return;
		}

		var session = ServiceLocator.Current.Get<GameSession>();
		var shipId = session.SelectedShipId;
		coordinator.RequestRespawnAtDockyardServerRpc(NetworkManager.Singleton.LocalClientId, shipId.ToString());
	}

	public void ShowPanel()
	{
		if (_deathPanel)
		{
			_deathPanel.SetActive(true);
		}
	}

	public void HidePanel()
	{
		if (_deathPanel != null)
		{
			_deathPanel.SetActive(false);
		}
		if (_respawnButton != null)
		{
			_respawnButton.interactable = true;
		}
	}
}
