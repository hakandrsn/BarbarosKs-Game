// Filename: CannonballApiService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;

public class CannonballApiService : BaseApiService, IGameService
{
    public async Task<List<CannonballTypeDto>> GetAllCannonballTypesAsync()
    {
        // Bu genel bir oyun verisi olduğu için kimlik doğrulaması gerektirmez.
        return await GetAsync<List<CannonballTypeDto>>("/api/cannonballs/all", false);
    }
    
}