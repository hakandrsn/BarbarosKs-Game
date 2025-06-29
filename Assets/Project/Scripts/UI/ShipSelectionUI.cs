using System.Collections.Generic;
using BarbarosKs.core.DTOs;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShipSelectionUI : MonoBehaviour
{
    [Header("UI Referansları")]
    [SerializeField] private Transform shipListContainer; // Gemi butonlarının ekleneceği yer (örn: bir Vertical Layout Group)
    [SerializeField] private GameObject shipButtonPrefab; // Tek bir gemi butonunun prefab'ı
    [SerializeField] private TextMeshProUGUI playerNameText; // Oyuncu adını gösterecek text

    void Start()
    {
        // GameManager'dan veriyi çek ve UI'ı doldur
        var characterData = GameManager.Instance.CharacterData;

        // Eğer bir sebeple veri yoksa (örn: sahne direkt açıldıysa), hata ver ve dur.
        if (characterData == null)
        {
            Debug.LogError("Karakter verisi bulunamadı! Lütfen Login sahnesinden başlayın.");
            // İsteğe bağlı olarak Login sahnesine geri yönlendirilebilir.
            // SceneManager.LoadScene("LoginScene");
            return;
        }
        
        // Oyuncu adını UI'a yazdır
        if(playerNameText != null)
        {
            playerNameText.text = characterData.Player.Username;
        }

        // Mevcut tüm butonları temizle (sahne yeniden yüklendiğinde vb. durumlar için)
        foreach (Transform child in shipListContainer)
        {
            Destroy(child.gameObject);
        }

        // Oyuncunun sahip olduğu her bir gemi için bir buton oluştur
        if (characterData.Player != null)
            foreach (var ship in characterData.Player.Ships)
            {
                var buttonGo = Instantiate(shipButtonPrefab, shipListContainer);

                // Butonun text'ini ayarla
                var buttonText = buttonGo.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"{ship.Name} (Seviye: {ship.Level})";
                }

                // Butonun tıklama olayını ayarla
                var button = buttonGo.GetComponent<Button>();
                if (button != null)
                {
                    // Butona tıklandığında hangi geminin seçildiğini bildirmek için
                    // OnShipSelected metodunu çağırıyoruz.
                    button.onClick.AddListener(() => OnShipSelected(ship));
                }
            }
    }

    /// <summary>
    /// Bir gemi seçim butonuna tıklandığında çalışır.
    /// </summary>
    private void OnShipSelected(ShipSummaryDto selectedShip)
    {
        // Seçimi merkezi GameManager'a bildir ve oyun sahnesini yüklemesini söyle.
        GameManager.Instance.SetActiveShipAndEnterGame(selectedShip);
    }
}