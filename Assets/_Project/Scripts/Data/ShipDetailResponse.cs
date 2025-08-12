// Filename: AuthResponse.cs

using BarbarosKs.Shared.DTOs;
using BarbarosKs.Shared.DTOs.player;

[System.Serializable]
public class ShipDetailResponse
{
    public bool success;
    public string message;
    public ShipDetailDto Data;
    // playerData gibi diğer alanları şimdilik eklememize gerek yok.
}