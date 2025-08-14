// Filename: RespawnCoordinator.cs

using System;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using Unity.Netcode;
using UnityEngine;

public class RespawnCoordinator : NetworkBehaviour
{
	[ServerRpc(RequireOwnership = false)]
	public void RequestRespawnAtDockyardServerRpc(ulong requesterClientId, string shipId)
	{
		if (!IsServer) return;
		Debug.Log($"[Respawn] İstek alındı. Client={requesterClientId} ShipId={shipId}");
		HandleRespawnRequestAsync(requesterClientId, shipId);
	}

	private async void HandleRespawnRequestAsync(ulong requesterClientId, string shipId)
	{
		ShipRespawnResultDto result = null;
		try
		{
			var playerApi = ServiceLocator.Current.Get<PlayerApiService>();
			result = await playerApi.RespawnShipAsync(Guid.Parse(shipId));
		}
		catch (Exception ex)
		{
			Debug.LogError($"[Respawn] Backend respawn çağrısında hata: {ex.Message}");
		}

		if (result == null)
		{
			Debug.LogError("[Respawn] Backend null döndü veya başarısız.");
			NotifyHidePanelClientRpc(new ClientRpcParams
			{
				Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } }
			});
			return;
		}

		// Backend güncelledi: şimdi yeni gemiyi spawn edelim
		var playerManager = ServiceLocator.Current.Get<PlayerManager>();
		playerManager.SpawnPlayer(requesterClientId, result.ShipId);

		NotifyHidePanelClientRpc(new ClientRpcParams
		{
			Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } }
		});
	}

	[ClientRpc]
	private void NotifyHidePanelClientRpc(ClientRpcParams clientRpcParams = default)
	{
		var deathUi = FindObjectOfType<DeathUIController>();
		if (deathUi != null)
		{
			deathUi.HidePanel();
		}
	}
}


