using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts
{
    public class NetworkManagerUI : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;

        private void Awake()
        {
            _hostButton.onClick.AddListener(() =>
            {
                Debug.Log("Host Button Clicked");
                NetworkManager.Singleton.StartHost();
                HideButtons();
            });
            
            _clientButton.onClick.AddListener(() =>
            {
                Debug.Log("Client olarak bağlanılıyor");
                NetworkManager.Singleton.StartClient();
                HideButtons();
            });
        }

        private void HideButtons()
        {
            _hostButton.gameObject.SetActive(false);
            _clientButton.gameObject.SetActive(false);
        }
    }
}