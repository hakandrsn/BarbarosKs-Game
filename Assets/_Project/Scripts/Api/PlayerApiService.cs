// Filename: PlayerApiService.cs

using System;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using UnityEngine; // Hata verirse bu satırı silin

public class PlayerApiService : BaseApiService, IGameService
{
    public CharacterSelectionDto PlayerData { get; private set; }
    public ShipDetailDto ShipData { get; private set; }
    public event Action OnPlayerDataReceived;
    public event Action OnPlayerDataReceivedFailed;

    public async Task GetMyCharacterDataAsync()
    {
        var characterData = await GetAsync<CharacterSelectionDto>("/api/Players/me");

        if (characterData != null)
        {
            PlayerData = characterData;
            OnPlayerDataReceived?.Invoke();
        }
        else
        {
            OnPlayerDataReceivedFailed?.Invoke();
        }
    }

    public async Task<AttackResult> ProcessAttackAsync(Guid attackerId, Guid targetId)
    {
        var payload = new AttackRequestPayload
        {
            AttackerShipId = attackerId.ToString(),
            TargetShipId = targetId.ToString()
        };
        return await PostAsync<AttackRequestPayload, AttackResult>("/api/gateway/attack", payload);
    }


    public async Task<ShipDetailResponse> GetShipDetailAsync(Guid shipId)
    {
        // Yeni ve güvenli sunucu endpoint'ini çağırıyoruz.
        var endpoint = $"/api/Players/server/ships/{shipId}";

        // Artık JWT Token kontrolü yapmayan GetAsync'i çağırabiliriz.
        // BaseApiService'teki GetAsync'ten 'requireAuth' kontrolünü kaldırabilir veya false geçebilirsiniz.
        return await GetAsync<ShipDetailResponse>(endpoint, false);
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
        var response = await PutAsync<SetActiveCannonballRequestDto, ApiResponseDto>(endpoint, requestPayload);

        // 4. Return true if the response is not null and the API reported success.
        return response is { Success: true };
    }
}