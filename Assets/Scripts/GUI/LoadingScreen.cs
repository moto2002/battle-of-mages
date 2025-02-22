﻿using UnityEngine;
using System.Collections;

public class LoadingScreen : LobbyModule<LoadingScreen> {
	public string loadingText;
	public string downloadingText;
	public string backgroundURL;
	public Color backgroundColor = Color.white;
	public float fadeTime;
	public bool chatEnabled;

	protected WWWResource<Texture2D> background;
	
	[HideInInspector]
	public string statusMessage;

	[HideInInspector]
	public AsyncOperation asyncLoadLevel;

	[HideInInspector]
	public WWW asyncDownload;

	private CallBack func;
	private string level;
	
	// Start
	void Start() {
		background = new WWWResource<Texture2D>("loading screen", backgroundURL);
		this.Disable();
	}
	
	// Securely load a level
	public void SecureLoadLevel(string nLevel, CallBack nFunc = null) {
		LogManager.General.Log("SecureLoadLevel: " + nLevel);
		
		level = nLevel;
		func = nFunc;
		
		// Stop them from destroying because they are parented to destroyable objects
		SaveSingletons();
		
		// There is no reason to send any more data over the network on the default channel,
		// because we are about to load the level, thus all those objects will get deleted anyway
		//uLink.Network.SetSendingEnabled(0, false);
		
		// We need to stop receiving because the level must be loaded first.
		// Once the level is loaded, rpc's and other state update attached to objects in the level are allowed to fire
		//uLink.Network.isMessageQueueRunning = false;
		
		// Remove parties
		GameServerParty.partyList.Clear();
		
		// All network views loaded from a level will get a prefix into their NetworkViewID.
		// This will prevent old updates from clients leaking into a newly created scene.
		//uLink.Network.SetLevelPrefix(++this.levelPrefix);
		
		// Load it
		this.Enable();
		
		// uLink.Network.isMessageQueueRunning is set to true in OnLoadLevel()
	}
	
	// On level load
	void OnLevelWasLoaded(int levelNumber) {
		// To prevent this callback from being called twice
		if(this != LoadingScreen.instance)
			return;

		LogManager.General.Log("Level was loaded: " + level);
		asyncDownload = null;
		asyncLoadLevel = null;
		//statusMessage = loadingText + " 100%";
		
		if(GameManager.isClient) {
			// Allow receiving data again
			uLink.Network.isMessageQueueRunning = true;
			
			if(func != null) {
				LogManager.General.Log("Calling level load callback");
				func();
			} else {
				LogManager.General.LogWarning("Level load callback has not been provided");
			}
			
			// Now the level has been loaded and we can start sending out data to clients
			//uLink.Network.SetSendingEnabled(0, true);
		}
	}
	
	// Asynchronous loading process
	IEnumerator LoadingProcess(string levelName, CallBack func = null) {
		LogManager.General.Log(_("Loading {0}, starting disconnection", levelName));
		uLink.Network.Disconnect();
		
		LogManager.General.Log("Waiting to disconnect from the server...");
		while(uLink.Network.peerType != uLink.NetworkPeerType.Disconnected)
			yield return null;
		
		LogManager.General.Log("Disconnected from the server successfully");
		LogManager.General.Log("Loading level: " + levelName);

		asyncLoadLevel = Application.LoadLevelAsync(levelName);
		yield return asyncLoadLevel;
	}
	
	// Saves singleton destruction by removing their parents
	void SaveSingletons() {
		LogManager.Spam.Log("Saving singletons");

		CharacterPreview.instance.transform.parent = null;

		MusicManager.instance.transform.parent = null;
		MusicManager.instance.transform.position = Cache.vector3Zero;
		MusicManager.instance.transform.rotation = Cache.quaternionIdentity;
		MusicManager.instance.transform.localScale = Cache.vector3Zero;
	}

	// OnEnable
	void OnEnable() {
		backgroundColor = new Color(this.backgroundColor.r, this.backgroundColor.g, this.backgroundColor.b, 0f);
		LogManager.General.Log("Loading screen enabled");
	}
	
	// Enables loading screen
	public void Enable(CallBack fadeEndFunction = null) {
		statusMessage = null;
		asyncLoadLevel = null;
		asyncDownload = null;

		if(fadeEndFunction == null) {
			fadeEndFunction = () => {
				// Start background loading process
				LogManager.Spam.Log("Starting loading coroutine now");
				StartCoroutine(LoadingProcess(level));
			};
		}

		// Enabled already?
		if(enabled) {
			// Custom callback
			fadeEndFunction();
		} else {
			this.Fade(
				fadeTime,
				val => {
					this.backgroundColor = new Color(this.backgroundColor.r, this.backgroundColor.g, this.backgroundColor.b, val);
				},
				() => {
					// Time scale
					Time.timeScale = 0f;
					
					// Custom callback
					fadeEndFunction();
				}
			);

			enabled = true;
		}
	}
	
	// Disables loading screen
	public void Disable() {
		LogManager.General.Log("Loading screen fading out, setting time scale from " + Time.timeScale + " to 1");
		Time.timeScale = 1f;

		this.Fade(
			fadeTime,
			val => {
				this.backgroundColor = new Color(this.backgroundColor.r, this.backgroundColor.g, this.backgroundColor.b, 1f - val);
			},
			() => {
				this.enabled = false;
				statusMessage = null;
				asyncLoadLevel = null;
				asyncDownload = null;
				LogManager.General.Log("Loading screen disabled");
			}
		);
	}
	
	// Draw
	public override void Draw() {
		GUI.depth = -1000;

		if(chatEnabled) {
			using(new GUIArea(5, 5, 500, 300)) {
				LobbyChat.instance.Draw();
			}
			// TODO: Finish this!
		}

		//if(Event.current.type != EventType.Repaint)
		//	return;
		
		GUI.color = Color.white;
		float height = 100;//GUI.skin.label.CalcHeight(new GUIContent(loadingText), Screen.width - 10);
		
		GUI.color = backgroundColor;
		if(background.data != null)
			GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), background.data);

		using(new GUIArea(5, Screen.height - height - 5, Screen.width - 10, height)) {
			using(new GUIVertical()) {
				GUILayout.FlexibleSpace();

				if(statusMessage != null)
					GUILayout.Label(statusMessage);

				if(asyncLoadLevel != null)
					GUILayout.Label(loadingText + " " + (int)(asyncLoadLevel.progress * 100) + "%");
				
				if(asyncDownload != null && !asyncDownload.isDone)
					GUILayout.Label(downloadingText + " " + (int)(asyncDownload.progress * 100) + "%");
			}
		}
	}
	
	// OnGUI
	void OnGUI() {
		Draw();
	}
}
