// Filename: PlayerApiService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using BarbarosKs.Shared; // ApiResponse için gerekli
using UnityEngine;

public class PlayerApiService : BaseApiService, IGameService
{
    // Önbellek verileri (UI için)
    public List<PlayerShipListDto> CachedShips { get; private set; }
    public ShipDetailDto SelectedShipDetails { get; private set; }

    // Olaylar (UI'ın dinlemesi için)
    public event Action OnPlayerShipsReceived;
    public event Action<string> OnPlayerShipsFailed;
    
    public event Action<ShipDetailDto> OnShipDetailReceived;
    public event Action<string> OnShipDetailFailed;

    public event Action<ShipDetailDto> OnShipCreated; // Yeni gemi oluşunca
    public event Action<string> OnShipCreateFailed;

    #region Ship Selection & Management (PlayersController)

    /// <summary>
    /// Oyuncunun sahip olduğu tüm gemileri listeler.
    /// Endpoint: GET api/players/me/ships
    /// </summary>
    public async Task GetMyShipsDataAsync()
    {
        // API artık ApiResponse<List<...>> dönüyor.
        var response = await GetAsync<ApiResponse<List<PlayerShipListDto>>>("api/players/me/ships");

        if (response != null && response.Success)
        {
            CachedShips = response.Data;
            Debug.Log($"[PlayerApiService] {CachedShips.Count} gemi alındı.");
            OnPlayerShipsReceived?.Invoke();
        }
        else
        {
            string msg = response?.Message ?? "Gemi listesi alınamadı.";
            Debug.LogError($"[PlayerApiService] Hata: {msg}");
            OnPlayerShipsFailed?.Invoke(msg);
        }
    }

    /// <summary>
    /// Seçilen geminin detaylarını getirir.
    /// Endpoint: GET api/players/ships/{id}/details
    /// </summary>
    public async Task GetShipDetailAsync(Guid shipId)
    {
        string endpoint = $"api/players/ships/{shipId}/details";
        var response = await GetAsync<ApiResponse<ShipDetailDto>>(endpoint);

        if (response != null && response.Success)
        {
            SelectedShipDetails = response.Data;
            OnShipDetailReceived?.Invoke(response.Data);
        }
        else
        {
            string msg = response?.Message ?? "Gemi detayı bulunamadı.";
            OnShipDetailFailed?.Invoke(msg);
        }
    }

    /// <summary>
    /// Yeni bir gemi oluşturur.
    /// Endpoint: POST api/players/ships/create
    /// </summary>
    public async Task CreateShipAsync(CreateShipRequestDto createShipDto)
    {
        string endpoint = "api/players/ships/create";
        var response = await PostAsync<CreateShipRequestDto, ApiResponse<ShipDetailDto>>(endpoint, createShipDto);

        if (response != null && response.Success)
        {
            // Başarılı olursa listeyi yenilemek iyi bir fikirdir
            CachedShips?.Add(new PlayerShipListDto 
            { 
                ShipId = response.Data.ShipId, 
                ShipName = response.Data.ShipName,
                Level = response.Data.Level,
                // Diğer özet alanlar...
            });
            
            OnShipCreated?.Invoke(response.Data);
        }
        else
        {
            OnShipCreateFailed?.Invoke(response?.Message ?? "Gemi oluşturulamadı.");
        }
    }

    /// <summary>
    /// Gemiyi respawn eder (Canını doldurur, pozisyonunu sıfırlar).
    /// Endpoint: POST api/players/ships/{id}/respawn
    /// </summary>
    public async Task<bool> RespawnShipAsync(Guid shipId)
    {
        string endpoint = $"api/players/ships/{shipId}/respawn";
        
        // POST isteği boş body ile gidiyor
        var response = await PostAsync<object, ApiResponse<ShipRespawnResultDto>>(endpoint, new { });

        if (response != null && response.Success)
        {
            Debug.Log($"[PlayerApiService] Gemi respawn edildi. Yeni HP: {response.Data.CurrentHull}");
            return true;
        }
        
        Debug.LogError($"[PlayerApiService] Respawn hatası: {response?.Message}");
        return false;
    }

    #endregion

    #region Inventory & Testing (InventoryService)

    /// <summary>
    /// TEST: Gemiye eşya ekler.
    /// Endpoint: POST api/players/inventory/take-item
    /// </summary>
    public async Task<bool> TestAddItemToShip(Guid shipId, int itemCode, int quantity)
    {
        string endpoint = "api/players/inventory/take-item";
        
        var payload = new AddItemProcess
        {
            ShipId = shipId,
            ItemCode = itemCode,
            Quantity = quantity
        };

        // API basitçe bool dönen bir yapıya sahipti (ApiResponse<bool>)
        var response = await PostAsync<AddItemProcess, ApiResponse<bool>>(endpoint, payload);

        return response != null && response.Success;
    }
    
    // NOT: Equip, UseItem gibi metotlar WebAPI'de InventoryService tam yazılınca buraya eklenecek.

    #endregion

    #region Connection (Gelecek Planı)

    // Eski 'ConnectRequestToServerForData' yerine, Dedicated Server IP'sini isteyen bir metot olacak.
    // Şimdilik burası boş, çünkü ConnectionController henüz yazılmadı.
    /*
    public async Task<ServerConnectionInfoDto> RequestGameServerAccess(Guid shipId)
    {
        // Gelecekte: /api/matchmaking/request veya /api/connection/token
        return null; 
    }
    */

    #endregion
}