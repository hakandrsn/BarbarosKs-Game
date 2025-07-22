using System;
using UnityEngine;
#if UNITY_SERVER
using Unity.Netcode;
#endif

public class BuildManager : MonoBehaviour
{
    [Header("Client-Only Objects")]
    [Tooltip("Bu objeler, Dedicated Server build'inde otomatik olarak devre dışı bırakılacak")]
    [SerializeField]
    private GameObject[] _clientOnlyObjects;

    private void Start()
    {
#if UNITY_SERVER
        Debug.Log("--- DEDICATED SERVER BUILD ---");

        foreach (var obj in _clientOnlyObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        Debug.Log("--- DEDICATED SERVER STARTING ---");
        NetworkManager.Singleton.StartServer();
#else
        Debug.Log("--- CLIENT BUILD ---");
        // Client build'i, bağlanmak için UI butonlarını kullanacak.
#endif
    }
}