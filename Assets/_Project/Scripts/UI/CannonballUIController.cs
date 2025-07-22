// Filename: CannonballUIController.cs (Final, Corrected Version)

using System.Collections.Generic;
using BarbarosKs.Shared.DTOs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro için

public class CannonballUIController : MonoBehaviour
{
    [Header("UI Referansları")] [SerializeField]
    private GameObject _panel;

    [SerializeField] private RectTransform _listContentArea;
    [SerializeField] private GameObject _listItemPrefab;
    [SerializeField] private Button _equipButton;
    [SerializeField] private Button _openInventoryButton; // Envanteri açacak ana buton
    [SerializeField] private Image _selectedCannonballIcon;

    // Servisler ve Referanslar
    private PlayerInventory _playerInventory;
    private GameDataService _gameDataService;
    private ShipCombat _localPlayerShipCombat;

    private int _selectedCannonballCode = 0;
    private List<CannonballListItem> _instantiatedItems = new List<CannonballListItem>();

    void Start()
    {
        // GameManager'ın var olmasını bekle
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager bulunamadı! Sahnenin doğru kurulduğundan emin olun.");
            this.enabled = false; // Script'i devre dışı bırak.
            return;
        }

        _playerInventory = ServiceLocator.Current.Get<PlayerInventory>();
        _gameDataService = ServiceLocator.Current.Get<GameDataService>();

        PlayerController.OnLocalPlayerShipReady += OnLocalPlayerReady;
        _equipButton.onClick.AddListener(OnEquipButtonClicked);
        _openInventoryButton.onClick.AddListener(TogglePanel);
        _panel.SetActive(false);
    }

    private void OnLocalPlayerReady(Transform playerTransform)
    {
        _localPlayerShipCombat = playerTransform.GetComponent<ShipCombat>();
        if (_localPlayerShipCombat)
        {
            _localPlayerShipCombat.EquippedCannonballCode.OnValueChanged += OnEquippedCannonballChanged;
            UpdateSelectedIcon(_localPlayerShipCombat.EquippedCannonballCode.Value);
        }
    }

    public void TogglePanel()
    {
        bool isActive = !_panel.activeSelf;
        _panel.SetActive(isActive);

        if (isActive)
        {
            RefreshList();
        }
    }

    private void RefreshList()
    {
        foreach (Transform child in _listContentArea)
        {
            Destroy(child.gameObject);
        }

        _instantiatedItems.Clear();

        if (_playerInventory == null || _gameDataService == null) return;

        foreach (var inventoryItem in _playerInventory.CannonballQuantities)
        {
            int code = inventoryItem.Key;
            int quantity = inventoryItem.Value;

            if (quantity <= 0) continue;

            CannonballTypeDto cannonballStats = _gameDataService.GetCannonballStatsByCode(code);
            if (cannonballStats == null) continue;

            GameObject itemGO = Instantiate(_listItemPrefab, _listContentArea);
            var itemScript = itemGO.GetComponent<CannonballListItem>();
            // DTO'daki doğru alan adlarını kullandığımızdan emin olalım
            itemScript.Setup(cannonballStats, quantity, HandleSingleClick, HandleDoubleClick);
            _instantiatedItems.Add(itemScript);
        }

        if (_localPlayerShipCombat != null)
        {
            UpdateSelectionVisuals(_localPlayerShipCombat.EquippedCannonballCode.Value);
        }
    }

    private void HandleSingleClick(int code)
    {
        _selectedCannonballCode = code;
        _equipButton.interactable = true;
        UpdateSelectionVisuals(code);
    }

    private void HandleDoubleClick(int code)
    {
        _selectedCannonballCode = code;
        EquipSelectedCannonball();
    }

    private void OnEquipButtonClicked()
    {
        EquipSelectedCannonball();
    }

    private void EquipSelectedCannonball()
    {
        if (_selectedCannonballCode == 0 || _localPlayerShipCombat == null) return;

        _localPlayerShipCombat.EquipCannonballServerRpc(_selectedCannonballCode);
        TogglePanel(); // Paneli kapat
    }

    private void OnEquippedCannonballChanged(int previousCode, int newCode)
    {
        UpdateSelectedIcon(newCode);
        if (_panel.activeSelf)
        {
            UpdateSelectionVisuals(newCode);
        }
    }
    
    private void UpdateSelectedIcon(int cannonballCode)
    {
        var cannonballStats = _gameDataService?.GetCannonballStatsByCode(cannonballCode);
        if (cannonballStats != null)
        {
            _selectedCannonballIcon.sprite = Resources.Load<Sprite>($"Icons/Cannonballs/{cannonballStats.IconName}");
        }
    }

    private void UpdateSelectionVisuals(int selectedCode)
    {
        foreach (var item in _instantiatedItems)
        {
            // cannonballCode'un CannonballListItem'de public olduğunu varsayıyoruz.
            item.SetSelected(item._cannonballData.Code == selectedCode);
        }
    }

    private void OnDestroy()
    {
        // --- DÜZELTME BURADA ---
        // Artık null kontrolü yapmıyoruz, doğrudan abonelikten çıkıyoruz.
        PlayerController.OnLocalPlayerShipReady -= OnLocalPlayerReady;

        if (_localPlayerShipCombat != null)
        {
            _localPlayerShipCombat.EquippedCannonballCode.OnValueChanged -= OnEquippedCannonballChanged;
        }
    }
}