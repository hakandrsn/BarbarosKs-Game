// Filename: ShipSelectionButton.cs

using System;
using BarbarosKs.Shared.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipSelectionButton : MonoBehaviour
{
    [SerializeField] private TMP_Text _shipInfoText;
    [SerializeField] private Button _button;

    private Guid _shipId;
    private Action<Guid> _onClickCallback;
    private PlayerApiService _playerApiService;
    // Bu metot, ana UI yöneticimiz tarafından çağrılacak.

    private void Start()
    {
        _playerApiService = ServiceLocator.Current.Get<PlayerApiService>();
    }

    public void Setup(ShipDetailDto shipData, Action<Guid> onClickCallback)
    {
        _shipId = shipData.ShipId;
        _onClickCallback = onClickCallback;
        _shipInfoText.text = $"{shipData.ShipName} <size=20>(Lv. {shipData.Level} {shipData.ShipType})</size>";
        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        // Butona tıklandığında, ana yöneticiye hangi geminin ID'sinin
        // seçildiğini bildiren callback'i çağır.
        _onClickCallback?.Invoke(_shipId);
    }
}