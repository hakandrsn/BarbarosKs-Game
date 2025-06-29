using UnityEngine;
using UnityEngine.SceneManagement;

public class Initializer : MonoBehaviour
{
    void Start()
    {
        // Bu script, oyun başlar başlamaz bir sonraki sahneyi yükler.
        // Yöneticileri barındıran bu nesne DontDestroyOnLoad olduğu için
        // bir sonraki sahneye taşınacaktır.
        SceneManager.LoadScene("Login"); // Buraya kendi login sahnenizin adını yazın.
    }
}