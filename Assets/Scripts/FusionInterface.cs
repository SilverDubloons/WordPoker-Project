using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using System;
using UnityEngine.UI;
using Unity.Multiplayer.Playmode;
using System.Linq;

public class FusionInterface : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
	public PlayerRef localPlayerRef;
	public bool reconnecting;
	
	private void SetupLobbyInterface()
	{
		if (LocalInterface.instance.networkRunner.IsSharedModeMasterClient)
        {
			Debug.Log("Master Client SetupLobbyInterface");
			LocalInterface.instance.startGameButton.gameObject.SetActive(true);
			if(LocalInterface.instance.networkRunner.ActivePlayers.Count() > 1)
			{
				UpdateLobbyMessage("Game Is Ready To Start", true);
			}
			else
			{
				UpdateLobbyMessage("Need 2 Or More Players To Start");
			}
			LocalInterface.instance.masterClientLobbyInterface.SetActive(true);
			if(LocalAnimations.instance.masterClientLobbyInterfaceRT.anchoredPosition.y < -230f)
			{
				LocalAnimations.instance.RevealLobbyInterface();
				LocalInterface.instance.timeToActSliderHelper.OnSliderChanged();
				LocalInterface.instance.timeBetweenBlindsSliderHelper.OnSliderChanged();
				LocalInterface.instance.startingChipsSliderHelper.OnSliderChanged();
				LocalInterface.instance.WordListDropdownUpdated();
				// LocalAnimations.instance.masterClientLobbyInterfaceRT.anchoredPosition = new Vector2(-70, -125);
			}
			LocalInterface.instance.clientLobbyInterface.SetActive(false);
        }
		else
		{
			Debug.Log("Client SetupLobbyInterface");
			LocalInterface.instance.startGameButton.gameObject.SetActive(false);
			if(LocalInterface.instance.networkRunner.ActivePlayers.Count() > 1)
			{
				UpdateLobbyMessage("Waiting For Host To Start Game");
			}
			else
			{
				UpdateLobbyMessage("Need 2 Or More Players To Start, And You're Not The Host Somehow (Lobby Is Bugged)");
			}
			LocalInterface.instance.masterClientLobbyInterface.SetActive(false);
			LocalInterface.instance.clientLobbyInterface.SetActive(true);
			if(LocalAnimations.instance.clientLobbyInterfaceRT.anchoredPosition.y < -230f)
			{
				// LocalAnimations.instance.clientLobbyInterfaceRT.anchoredPosition = new Vector2(-70, -125);
				LocalAnimations.instance.RevealLobbyInterface();
			}
		}
	}
	
	public void UpdateLobbyMessage(string lobbyMessage, bool canStartGame = false)
	{
		LocalInterface.instance.lobbyMessageLabel.ChangeText(lobbyMessage);
		if(LocalInterface.instance.networkRunner.IsSharedModeMasterClient)
		{
			LocalInterface.instance.startGameButton.ChangeDisabled(!canStartGame);
		}
	}
	
	public bool HasGameStarted()
	{
		if(LocalInterface.instance.networkRunner.SessionInfo.Properties.TryGetValue("GameStarted", out var propertyType))
		{
			bool gameStarted = (bool)propertyType.PropertyValue;
			return gameStarted;
		}
		Debug.LogWarning("Could not determine whether or not game has started");
		return false;
	}
	
	public Vector3 GetLobbyPosition(int joinOrder)
	{
		int totalPlayers = LocalInterface.instance.networkRunner.ActivePlayers.Count();
		float distanceBetweenPlayers = 1.05f;
		float xDestination = (totalPlayers - 1) * (distanceBetweenPlayers / 2f) - (totalPlayers - joinOrder - 1) * distanceBetweenPlayers;
		return new Vector3(xDestination, 0, 0);
	}
	
	public void StartingChipsUpdated(int startingChips)
	{
		List<PokerPlayer> playersInLobby = LocalInterface.instance.gameManager.GetAllPokerPlayers();
		for(int i = 0; i < playersInLobby.Count; i++)
		{
			playersInLobby[i].chips = startingChips;
		}
	}
	
	public void ReorganizeLobbyPlayers()
	{
		List<PokerPlayer> playersInLobby = LocalInterface.instance.gameManager.GetAllPokerPlayers();
		for(int i = 0; i < playersInLobby.Count; i++)
		{
			playersInLobby[i].networkTransform.Teleport(GetLobbyPosition(i));
			playersInLobby[i].playerColor = LocalInterface.instance.playerColors[i];
		}
	}
	
	public async void PlayerJoined(PlayerRef player)
	{
		bool gameStarted = HasGameStarted();
		Debug.Log($"PlayerJoined, gameStarted = {gameStarted.ToString()}");
		if(!gameStarted)
		{
			SetupLobbyInterface();
		}
		if(player == LocalInterface.instance.networkRunner.LocalPlayer)
		{
			localPlayerRef = player;
			if(!LocalInterface.instance.networkRunner.IsSharedModeMasterClient)
			{
				if(!gameStarted)
				{
					// LocalInterface.instance.localAnimations.RevealLobbyInterface();
					LocalInterface.instance.roomCodeLabel.ChangeText(LocalInterface.instance.networkRunner.SessionInfo.Name);
					LocalInterface.instance.lobbyCodeOutputInputField.text = LocalInterface.instance.networkRunner.SessionInfo.Name;
				}
				GameObject gameManagerGO = GameObject.FindWithTag("GameManager");
				while(gameManagerGO == null)
				{
					await Awaitable.NextFrameAsync();
					gameManagerGO = GameObject.FindWithTag("GameManager");
				}
				Debug.Log("gameManager found");
				LocalInterface.instance.gameManager = gameManagerGO.GetComponent<GameManager>();
				if(!gameStarted)
				{
					Debug.Log("Requesting name update");
					LocalInterface.instance.gameManager.SendPlayerNameToMasterRpc(player, LocalInterface.instance.localPlayerName);
				}
			}
		}
		if(LocalInterface.instance.networkRunner.IsSharedModeMasterClient && !gameStarted)
        {
			NetworkObject newPlayerNO = LocalInterface.instance.networkRunner.Spawn(LocalInterface.instance.playerPrefab, Vector3.zero, Quaternion.identity);
			PokerPlayer newPokerPlayer = newPlayerNO.GetComponent<PokerPlayer>();
			newPokerPlayer.owningPlayerRef = player;
			if(localPlayerRef == player)
			{
				newPokerPlayer.playerName = LocalInterface.instance.localPlayerName;
			}
			else
			{
				newPokerPlayer.playerName = "Joining";
			}
			int joinedIndex = LocalInterface.instance.gameManager.AddPlayerToGame(newPlayerNO);
			LocalInterface.instance.gameManager.numberOfPlayersInGame = LocalInterface.instance.networkRunner.ActivePlayers.Count();
			newPokerPlayer.chips = LocalInterface.instance.startingChipsSliderHelper.totalVal;
			// newPokerPlayer.networkTransform.Teleport(GetLobbyPosition(joinedIndex));
			// newPokerPlayer.playerColor = LocalInterface.instance.playerColors[joinedIndex];
			ReorganizeLobbyPlayers();
		}
		if(gameStarted && player == LocalInterface.instance.networkRunner.LocalPlayer)
		{
			List<PokerPlayer> disconnectedPlayers = LocalInterface.instance.gameManager.GetAllDisconnectedPokerPlayers();
			if(disconnectedPlayers.Count > 0)
			{
				// Debug.Log("Click on the player you wish to reconnect as");
				Menu.instance.DisplayError("Click on the player you wish to reconnect as");
				LocalAnimations.instance.RevealGameplayInterface();
				reconnecting = true;
			}
			else
			{
				Menu.instance.DisplayError("Tried to join in progress game with no disconnected players");
				// Debug.Log("Tried to join in progress game with no disconnected players"); 
				LocalInterface.instance.DisconnectClicked();
				LocalAnimations.instance.RevealMainMenu();
			}
		}
	}
	
	public void RemovePlayerFromLobby(PokerPlayer leavingPokerPlayer)
	{
		Debug.Log($"Removing {leavingPokerPlayer.playerName} from lobby");
		LocalInterface.instance.gameManager.RemovePlayerFromGame(leavingPokerPlayer.networkObject);
		LocalInterface.instance.gameManager.numberOfPlayersInGame = LocalInterface.instance.networkRunner.ActivePlayers.Count();
		
		LocalInterface.instance.networkRunner.Despawn(leavingPokerPlayer.networkObject);
		ReorganizeLobbyPlayers();
	}
	
	public void PlayerLeft(PlayerRef player)
	{
		bool gameStarted = HasGameStarted();
		Debug.Log($"PlayerLeft gameStarted = {gameStarted.ToString()} LocalInterface.instance.networkRunner.IsSharedModeMasterClient = {LocalInterface.instance.networkRunner.IsSharedModeMasterClient.ToString()}");
		if(!gameStarted)
		{
			SetupLobbyInterface();
		}
		if(LocalInterface.instance.networkRunner.IsSharedModeMasterClient)
		{
			PokerPlayer leavingPokerPlayer = LocalInterface.instance.gameManager.FindPlayerObjectByPlayerRef(player);
			if(leavingPokerPlayer != null)
			{
				if(!gameStarted)
				{
					Debug.Log($"{leavingPokerPlayer.playerName} left and game has not started and I am the master client ");
					RemovePlayerFromLobby(leavingPokerPlayer);
				}
				else
				{
					Debug.Log($"{leavingPokerPlayer.playerName} left and game started and I am the master client ");
					leavingPokerPlayer.disconnected = true;
					if(LocalInterface.instance.gameManager.currentAction == leavingPokerPlayer.networkObject)
					{
						//LocalInterface.instance.gameManager.FoldPlayer(leavingPokerPlayer);
						LocalInterface.instance.gameManager.PlayerFoldOrCheckRpc(leavingPokerPlayer.owningPlayerRef);
					}
				}
			}
		}
	}
	
/*  	private FusionAppSettings BuildCustomAppSetting(string region, string customAppID = null)
	{
		string appVersion = LocalInterface.instance.version;
        var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();;

        appSettings.UseNameServer = true;
        appSettings.AppVersion = appVersion;

        if (string.IsNullOrEmpty(customAppID) == false)
		{
            appSettings.AppIdFusion = customAppID;
        }

        if (string.IsNullOrEmpty(region) == false)
		{
            appSettings.FixedRegion = region.ToLower();
        }

        // If the Region is set to China (CN),
        // the Name Server will be automatically changed to the right one
        // appSettings.Server = "ns.photonengine.cn";

        return appSettings;
    } */
	
    public async void StartLobby(bool hostGame, string region = "")
    {
		string roomCode = string.Empty;
		
		// var appSettings = BuildCustomAppSetting(region);
		
		if(hostGame)
		{
			roomCode = LocalInterface.instance.CreateLobbyCode(4).ToUpper();
		}
		else
		{
			roomCode = LocalInterface.instance.roomCodeInputField.text.ToUpper();
			if(string.IsNullOrEmpty(roomCode))
			{
				Menu.instance.DisplayError("Invalid Session Input");
				Debug.Log("Invalid Session Input");
				return;
			}
		}
		
		LocalInterface.instance.localAnimations.HideMainMenu();
		
		// LocalInterface.instance.joinGameInterface.SetActive(false);
		
		if(hostGame)
		{
			Dictionary<string, SessionProperty> sessionProperties = new Dictionary<string, SessionProperty>();
			sessionProperties["GameStarted"] = (bool)false;
			StartGameArgs startGameArgs = new StartGameArgs()
			{
				GameMode = GameMode.Shared,
				// CustomPhotonAppSettings = appSettings,
				SessionProperties = sessionProperties,
				SessionName = roomCode,
				EnableClientSessionCreation = hostGame,
				PlayerCount = 6,
			};
			StartGameResult result = await LocalInterface.instance.networkRunner.StartGame(startGameArgs);
		
			if (result.Ok)
			{
				// LocalInterface.instance.lobbyInterface.SetActive(true);
				// LocalInterface.instance.disconnectInterface.SetActive(true);
				// LocalInterface.instance.roomCodeOutputField.text = LocalInterface.instance.networkRunner.SessionInfo.Name;
				LocalInterface.instance.menu.disconnectButton.ChangeDisabled(false);
				// LocalInterface.instance.localAnimations.RevealLobbyInterface();
				LocalInterface.instance.roomCodeLabel.ChangeText(LocalInterface.instance.networkRunner.SessionInfo.Name);
				LocalInterface.instance.lobbyCodeOutputInputField.text = LocalInterface.instance.networkRunner.SessionInfo.Name;
				NetworkObject newGameManagerNO = LocalInterface.instance.networkRunner.Spawn(LocalInterface.instance.gameManagerPrefab, Vector3.zero, Quaternion.identity);
				LocalInterface.instance.gameManager = newGameManagerNO.GetComponent<GameManager>();
				LocalInterface.instance.WordListDropdownUpdated();
			}
			else
			{
				LocalInterface.instance.roomCodeInputField.text = string.Empty;
				// LocalInterface.instance.joinGameInterface.SetActive(true);
				Menu.instance.DisplayError(result.ErrorMessage);
				Debug.LogError(result.ErrorMessage);
				LocalAnimations.instance.RevealMainMenu();
			}
		}
		else
		{
			StartGameArgs startGameArgs = new StartGameArgs()
			{
				GameMode = GameMode.Shared,
				// CustomPhotonAppSettings = appSettings,
				SessionName = roomCode,
				EnableClientSessionCreation = hostGame,
				PlayerCount = 6,
			};
			StartGameResult result = await LocalInterface.instance.networkRunner.StartGame(startGameArgs);
		
			if (result.Ok)
			{
				LocalInterface.instance.menu.disconnectButton.ChangeDisabled(false);
			}
			else
			{
				LocalInterface.instance.roomCodeInputField.text = string.Empty;
				// LocalInterface.instance.joinGameInterface.SetActive(true);
				LocalAnimations.instance.RevealMainMenu();
				Menu.instance.DisplayError(result.ErrorMessage);
				Debug.LogError(result.ErrorMessage);
			}
		}
		LocalInterface.instance.UpdateLoadingSpinner(false);
	}
	
	public void StartGame()
	{	
		Dictionary<string, SessionProperty> sessionProperties = new Dictionary<string, SessionProperty>();
		sessionProperties["GameStarted"] = (bool)true;
		LocalInterface.instance.networkRunner.SessionInfo.UpdateCustomProperties(sessionProperties);
		LocalInterface.instance.gameManager.StartGame();
	}
}