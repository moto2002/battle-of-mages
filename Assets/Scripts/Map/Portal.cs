﻿using uLobby;
using UnityEngine;

[SelectionBase]
public class Portal : MonoBehaviour, ActionTarget {
	public ServerType serverType = ServerType.World;
	public Transform[] spawns;

	// Start
	void Start() {
		// Set portal label text
		transform.FindChild("Portal/Label").GetComponent<TextMesh>().text = mapName;
	}

	// Player comes in range
	void OnTriggerEnter(Collider coll) {
		if(!GameManager.isPvE)
			return;
		
		if(Player.main == null)
			return;
		
		if(coll != Player.main.collider)
			return;

		// Set action target
		Player.main.actionTarget = this;
	}

	// Player goes out of range
	void OnTriggerExit(Collider coll) {
		if(!GameManager.isPvE)
			return;
		
		if(Player.main == null)
			return;
		
		if(coll != Player.main.collider)
			return;

		// Reset action target
		Player.main.actionTarget = null;
	}

	// OnAction
	public void OnAction(Entity entity) {
		if(!enabled)
			return;

		// We save it here because the game object might be destroyed in the lambda
		string targetMapName = mapName;

		// Stop saving position data
		entity.networkView.RPC("ActivatePortal", uLink.RPCMode.Server, targetMapName);

		// Send a request to the lobby to change the map
		Lobby.RPC("ActivatePortal", Lobby.lobby, MapManager.currentMapName, targetMapName, serverType);

		// Disable portal
		enabled = false;

		// Activate loading screen
		LoadingScreen.instance.Enable(() => {
			LoadingScreen.instance.statusMessage = "Teleporting to: <color=yellow>" + targetMapName + "</color>";
		});
	}

	// Map name
	public string mapName {
		get {
			return gameObject.name;
		}
	}

	// Camera Y rotation
	public float cameraYRotation {
		get {
			return transform.eulerAngles.y;
		}
	}
	
	// CanAction
	public bool CanAction(Entity entity) {
		return entity.canCast;
	}

	// GetCursorPosition
	public Vector3 GetCursorPosition() {
		return transform.collider.bounds.center;
	}
}