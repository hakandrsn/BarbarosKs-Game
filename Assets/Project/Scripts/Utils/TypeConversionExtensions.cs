using UnityEngine;
// System.Numerics'i kullanabilmek için using ifadesi ekliyoruz.
using System.Numerics;

/// <summary>
/// Unity'nin tipleri (Vector3, Quaternion) ve .NET'in standart tipleri arasında
/// kolayca dönüşüm yapmak için "extension" metotları sağlar.
/// </summary>
public static class TypeConversionExtensions
{
    // --- Vector3 Dönüşümleri ---

    /// <summary>
    /// UnityEngine.Vector3 tipini System.Numerics.Vector3 tipine dönüştürür.
    /// </summary>
    public static System.Numerics.Vector3 ToNumeric(this UnityEngine.Vector3 v)
    {
        return new System.Numerics.Vector3(v.x, v.y, v.z);
    }

    /// <summary>
    /// System.Numerics.Vector3 tipini UnityEngine.Vector3 tipine dönüştürür.
    /// DİKKAT: System.Numerics.Vector3'ün property'leri büyük harfle başlar (X, Y, Z).
    /// </summary>
    public static UnityEngine.Vector3 ToUnity(this System.Numerics.Vector3 v)
    {
        return new UnityEngine.Vector3(v.X, v.Y, v.Z);
    }

    // --- Quaternion Dönüşümleri ---

    /// <summary>
    /// UnityEngine.Quaternion tipini System.Numerics.Quaternion tipine dönüştürür.
    /// </summary>
    public static System.Numerics.Quaternion ToNumeric(this UnityEngine.Quaternion q)
    {
        return new System.Numerics.Quaternion(q.x, q.y, q.z, q.w);
    }

    /// <summary>
    /// System.Numerics.Quaternion tipini UnityEngine.Quaternion tipine dönüştürür.
    /// DİKKAT: System.Numerics.Quaternion'un property'leri büyük harfle başlar (X, Y, Z, W).
    /// </summary>
    public static UnityEngine.Quaternion ToUnity(this System.Numerics.Quaternion q)
    {
        return new UnityEngine.Quaternion(q.X, q.Y, q.Z, q.W);
    }
}