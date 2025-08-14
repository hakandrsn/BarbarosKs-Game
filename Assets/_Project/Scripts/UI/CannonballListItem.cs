// Filename: CannonballListItem.cs - Prefab'ın üzerine eklenecek.
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using BarbarosKs.Shared.DTOs;
using UnityEngine.EventSystems;

public class CannonballListItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private Image _selectionFrame; // Seçildiğinde görünecek çerçeve

    public CannonballDto _cannonballData;
    private Action<int> _onSingleClick;
    private Action<int> _onDoubleClick;

    public void Setup(CannonballDto data, int quantity, Action<int> onSingleClick, Action<int> onDoubleClick)
    {
        _cannonballData = data;
        _onSingleClick = onSingleClick;
        _onDoubleClick = onDoubleClick;

        _nameText.text = data.Name;
        _descriptionText.text = data.Description;
        _quantityText.text = $"x{quantity}";
        
        // Resources klasöründen ikonu kodla yüklüyoruz.
        _iconImage.sprite = Resources.Load<Sprite>($"Icons/Cannonballs/{data.IconName}");

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        _selectionFrame.enabled = isSelected;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2)
        {
            _onDoubleClick?.Invoke(_cannonballData.Code);
        }
        else
        {
            _onSingleClick?.Invoke(_cannonballData.Code);
        }
    }
}