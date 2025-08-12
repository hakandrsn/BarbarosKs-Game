using Unity.Netcode;


//Burası gemi oluşturken clientte gödnerilecek tüm datayı içerir
public struct ShipStatsData : INetworkSerializable
{
    public float Speed;
    public float Maneuverability;
    public float HitRate;
    public float Range;
    public float Armor;
    public int CurrentVigor;
    public float Cooldown;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Speed);
        serializer.SerializeValue(ref Maneuverability);
        serializer.SerializeValue(ref HitRate);
        serializer.SerializeValue(ref Range);
        serializer.SerializeValue(ref Armor);
        serializer.SerializeValue(ref CurrentVigor);
        serializer.SerializeValue(ref Cooldown);
    }
}