using UnityEngine;

namespace Project.Scripts.Network
{
    /// <summary>
    ///     Sahnedeki bir ağ nesnesinin kimliğini tutar.
    ///     Bu, tıklama ile hedefleme gibi işlemler için kullanılır.
    /// </summary>
    public class NetworkIdentity : MonoBehaviour
    {
        // Bu ID, NetworkObjectSpawner tarafından nesne oluşturulduğunda atanacak.
        public string EntityId { get; set; }
        public string OwnerPlayerId { get; set; }
    }
}