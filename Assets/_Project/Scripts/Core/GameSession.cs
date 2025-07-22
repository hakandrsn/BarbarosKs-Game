// Filename: GameSession.cs
using System;
using BarbarosKs.Shared.DTOs;

public class GameSession : IGameService
{
    public Guid SelectedShipId { get; set; }
    public string PlayerName { get; set; }
    public ShipDetailDto SelectedShip { get; set; }
}