// Filename: PlayerApiService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using UnityEngine; // Hata verirse bu satırı silin

public class PlayerApiService : BaseApiService, IGameService
{
    public List<PlayerShipListDto> Ships { get; private set; }
    public PlayerSessionDto PlayerSession { get; private set; }
    public ShipStatsDto ShipStats { get; private set; }
    public ShipData ShipData { get; private set; }

    public event Action OnPlayerShipsReceived;
    public event Action OnPlayerShipsReceivedFailed;

    public event Action OnPlayerConnected;
    public event Action OnPlayerConnectFailed;


	public async Task ConnectRequestToServerForData(Guid shipId, string preferredRegion = null, string overrideToken = null)
	{
		var request = new ConnectionRequestDto
		{
			ShipId = shipId,
			PreferredRegion = preferredRegion
		};

		ApiResponse<ConnectionResponseDto> response;
		if (!string.IsNullOrEmpty(overrideToken))
		{
			// İstek başına token ile POST
			response = await PostAsync<ConnectionRequestDto, ApiResponse<ConnectionResponseDto>>(
				"/api/connection/request", request, true, overrideToken);
		}
		else
		{
			// Global token ile POST (Login sonrası SetToken çağrıldıysa)
			response = await PostAsync<ConnectionRequestDto, ApiResponse<ConnectionResponseDto>>(
				"/api/connection/request", request, true);
		}

		if (response is { Success: true })
		{
			var connectionResponse = response.Data;
			PlayerSession = connectionResponse.PlayerSession;
			ShipStats = connectionResponse.ShipStats;
			ShipData = connectionResponse.ShipData;
			OnPlayerConnected?.Invoke();
		}
		else
		{
			OnPlayerConnectFailed?.Invoke();
		}
	}

    // get all ships list for select
    public async Task GetMyShipsDataAsync()
    {
        var response = await GetAsync<ApiResponse<List<PlayerShipListDto>>>("/api/players/me/ships");
        var shipList = response?.Data;
        if (shipList != null)
        {
            Debug.Log($"GetMyShipsDataAsync {shipList.Count}");
            Ships = shipList;
            OnPlayerShipsReceived?.Invoke();
        }
        else
        {
            OnPlayerShipsReceivedFailed?.Invoke();
        }
    }

    public async Task<AttackResponseDto> ProcessAttackAsync(Guid attackerId, Guid targetId)
    {
        var payload = new AttackRequestDto
        {
            AttackerShipId = attackerId,
            TargetShipId = targetId
        };
        return await PostAsync<AttackRequestDto, AttackResponseDto>("/api/gateway/attack", payload);
    }

    public async Task<ShipRespawnResultDto> RespawnShipAsync(Guid shipId)
    {
        var endpoint = $"/api/players/ships/{shipId}/respawn";
        // JWT gerekli varsayımıyla default requireAuth=true kullanılacak
        return await PostAsync<object, ShipRespawnResultDto>(endpoint, new { });
    }


    // seçilen geminin özelliklerini getirir ama server için değil clientte görmesi için token gerekli
    public async Task<ShipDetailResponse> GetShipDetailAsync(Guid shipId)
    {
        // Yeni ve güvenli sunucu endpoint'ini çağırıyoruz.
        var endpoint = $"/api/players/ships/{shipId}/details";

        // Artık JWT Token kontrolü yapmayan GetAsync'i çağırabiliriz.
        // BaseApiService'teki GetAsync'ten 'requireAuth' kontrolünü kaldırabilir veya false geçebilirsiniz.
        return await GetAsync<ShipDetailResponse>(endpoint);
    }

    public async Task<ShipDetailDto> CreateShipAsync(CreateShipRequestDto createShipDto)
    {
        var endpoint = $"/api/players/ships/create";

        return await PostAsync<CreateShipRequestDto, ShipDetailDto>(endpoint, createShipDto);
    }

    /// <summary>
    /// Sets the currently active cannonball for a ship by making a PUT request to the API.
    /// </summary>
    /// <param name="shipId">The unique ID of the ship.</param>
    /// <param name="cannonballCode">The unique 'Code' of the cannonball to activate.</param>
    /// <returns>True if the operation was successful, false otherwise.</returns>
    public async Task<bool> SetActiveCannonball(Guid shipId, int cannonballCode)
    {
        // 1. Construct the correct endpoint URL.
        string endpoint = $"/api/Cannonball/ship/{shipId}/active";

        // 2. Create the request payload object.
        var requestPayload = new SetActiveCannonballRequestDto
        {
            CannonballCode = cannonballCode
        };

        // 3. Call the generic PutAsync method from BaseApiService.
        // It sends the payload and expects a generic ApiResponseDto back.
        var response = await PutAsync<SetActiveCannonballRequestDto, ApiResponse>(endpoint, requestPayload);

        // 4. Return true if the response is not null and the API reported success.
        return response is { Success: true };
    }

    #region Connection methods

    // first connection and include stat
    public async Task<ConnectionResponseDto> ConnectToServer(ConnectionRequestDto request)
    {
        var endpoint = $"/api/connection/request";
        return await PostAsync<ConnectionRequestDto, ConnectionResponseDto>(endpoint, request, false);
    }

    public async Task<object> DisconnectFromServer(DisconnectRequestDto request)
    {
        var endpoint = $"/api/connection/disconnect";
        return await PostAsync<DisconnectRequestDto, object>(endpoint, request, false);
    }

    public async Task<List<ServerStatusDto>> GetServers()
    {
        var endpoint = $"/api/connection/servers";
        return await GetAsync<List<ServerStatusDto>>(endpoint, false);
    }

    #endregion
}