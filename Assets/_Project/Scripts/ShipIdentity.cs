using Unity.Netcode;
using Unity.Collections;
using UnityEngine.Serialization; // FixedString için
public class ShipIdentity : NetworkBehaviour
{
    // Network üzerinden senkronize olacak geminin benzersiz veritabanı ID'si.
    // Guid doğrudan senkronize edilemediği için string formatında (FixedString) tutuyoruz.
    [FormerlySerializedAs("ShipId")] public NetworkVariable<FixedString128Bytes> shipId = new NetworkVariable<FixedString128Bytes>();
}