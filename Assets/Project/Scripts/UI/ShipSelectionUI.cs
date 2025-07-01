using BarbarosKs.Shared.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using BarbarosKs.Core;

public class ShipSelectionUI : MonoBehaviour
{
    [Header("UI Referansları")] [SerializeField]
    private Transform shipListContainer; // Gemi butonlarının ekleneceği yer (örn: bir Vertical Layout Group)

    [SerializeField] private GameObject shipButtonPrefab; // Tek bir gemi butonunun prefab'ı
    [SerializeField] private TextMeshProUGUI playerNameText; // Oyuncu adını gösterecek text

    private void Start()
    {
        // PlayerDataManager'dan veriyi çek ve UI'ı doldur
        if (!PlayerDataManager.Instance.HasPlayerData)
        {
            Debug.LogError("❌ ShipSelectionUI: PlayerDataManager'da oyuncu verisi yok! Login sahnesine dönülüyor...");
            SceneManager.LoadScene("Login");
            return;
        }

        var playerProfile = PlayerDataManager.Instance.PlayerProfile;
        var ownedShips = PlayerDataManager.Instance.OwnedShips;

        Debug.Log($"🚢 ShipSelectionUI: {playerProfile.Username} için gemi seçim ekranı açılıyor");
        Debug.Log($"📊 Mevcut gemiler: {ownedShips?.Count ?? 0}");

        // Oyuncu adını UI'a yazdır
        if (playerNameText != null) 
            playerNameText.text = playerProfile.Username;

        // Mevcut tüm butonları temizle (sahne yeniden yüklendiğinde vb. durumlar için)
        foreach (Transform child in shipListContainer) 
            Destroy(child.gameObject);

        // Oyuncunun sahip olduğu her bir gemi için bir buton oluştur
        if (ownedShips != null)
        {
            foreach (var ship in ownedShips)
            {
                CreateShipButton(ship);
            }
        }
    }

    /// <summary>
    /// Gemi butonunu oluşturur
    /// </summary>
    private void CreateShipButton(ShipSummaryDto ship)
    {
        var buttonGo = Instantiate(shipButtonPrefab, shipListContainer);

        // Butonun text'ini ayarla
        var buttonText = buttonGo.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null) 
            buttonText.text = $"{ship.Name} (Seviye: {ship.Level})";

        // Butonun tıklama olayını ayarla
        var button = buttonGo.GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(() => OnShipSelected(ship));
    }

    /// <summary>
    ///     Bir gemi seçim butonuna tıklandığında çalışır.
    ///     Loading süreci başlatır ve LoadingManager'a devreder.
    /// </summary>
    private void OnShipSelected(ShipSummaryDto selectedShip)
    {
        Debug.Log($"🚢 Gemi seçildi: {selectedShip.Name} (ID: {selectedShip.Id})");
        
        // Kullanıcı arayüzünü kilitle (birden fazla tıklamayı önle)
        SetUIInteractable(false);
        
        // Seçilen gemiyi PlayerDataManager'a geçici olarak set et
        PlayerDataManager.Instance.SetActiveShip(selectedShip);
        
        // LoadingManager yoksa oluştur
        if (LoadingManager.Instance == null)
        {
            var loadingManagerGO = new GameObject("LoadingManager");
            loadingManagerGO.AddComponent<LoadingManager>();
        }
        
        // LoadingManager'a tüm süreci devret
        LoadingManager.Instance.StartShipLoadingProcess(selectedShip.Id);
    }

    /// <summary>
    /// UI etkileşimini açar/kapatır
    /// </summary>
    private void SetUIInteractable(bool interactable)
    {
        // Tüm butonları devre dışı bırak/etkinleştir
        var buttons = shipListContainer.GetComponentsInChildren<Button>();
        foreach (var button in buttons)
        {
            button.interactable = interactable;
        }
    }

    /// <summary>
    /// Hata mesajı gösterir (şimdilik Debug.LogError, ileride UI popup olabilir)
    /// </summary>
    private void ShowErrorMessage(string message)
    {
        Debug.LogError($"🚫 {message}");
        // TODO: Burada bir popup gösterilebilir
    }
}