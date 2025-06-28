using BarbarosKs.core.DTOs;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public AccountDto CurrentUser { get; private set; }
    public PlayerDto CurrentPlayer { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ApiManager'dan gelen başarılı giriş yanıtı ile çağrılır.
    /// </summary>
    public void OnLoginSuccess(AccountDto userData)
    {
        CurrentUser = userData;
        CurrentPlayer = userData.Player;
        if (CurrentPlayer != null) Debug.Log($"Hoşgeldin, {CurrentPlayer.Username}! Oyuncu ID: {CurrentPlayer.Id}");

        // Login başarılı olduğuna göre oyun sahnesine geçebiliriz.
        // Scene'in Build Settings'de ekli olduğundan emin olun.
        SceneManager.LoadScene("FisherSea"); // Sahnenizin adını buraya yazın
    }
}