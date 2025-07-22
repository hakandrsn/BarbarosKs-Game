// Filename: PlayerInfo.cs

using TMPro;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class PlayerInfo : NetworkBehaviour
{
    // Oyuncunun adını tüm client'lara senkronize et.
    public NetworkVariable<FixedString64Bytes> Username = new NetworkVariable<FixedString64Bytes>();
    
    // Gelecekte oyuncu seviyesi, loncası gibi bilgiler de buraya eklenebilir.
    // public NetworkVariable<int> PlayerLevel = new NetworkVariable<int>();

    [SerializeField] private TextMeshProUGUI  _userNameTextGUI;
    /// <summary>
    /// Bu metot, gemi spawn olurken sunucudaki PlayerManager tarafından çağrılır.
    /// </summary>
    public void Initialize(string username)
    {
        // NetworkVariable'ı sadece sunucu değiştirebilir.
        if (IsServer)
        {
            Username.Value = username;
            _userNameTextGUI.text = username;
        }
    }
}