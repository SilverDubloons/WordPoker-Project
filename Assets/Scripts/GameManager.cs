using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using System;
using UnityEngine.UI;
using Unity.Multiplayer.Playmode;
using System.Linq;
using System.Threading.Tasks;

public class GameManager : NetworkBehaviour
{
    [Networked, Capacity(6)]
	public NetworkArray<NetworkObject> playersInGame {get;}
	[Networked] public int numberOfPlayersInGame {get;set;}
	[Networked] public NetworkObject firstAction {get;set;}
	[Networked] public NetworkObject currentAction {get;set;}
	// [Networked] public float currentActionDecisionTime {get;set;} // tick timer ?
	[Networked, OnChangedRender(nameof(OnMaxTimeToActChanged))]
	public float maximumDecisionTime {get;set;}
	[Networked, OnChangedRender(nameof(OnTimeBetweenBlindLevelsChanged))]
	public float timeBetweenBlindLevels {get;set;}
	[Networked, OnChangedRender(nameof(OnDictionaryChanged))] 
	public int dictionaryBeingUsed{get;set;}
	[Networked] TickTimer revealGameplayTimer {get;set;}
	[Networked] TickTimer assignSeatsTimer {get;set;}
	[Networked] TickTimer startNextHandTimer {get;set;}
	[Networked] TickTimer blindLevelTimer {get;set;}
	[Networked] TickTimer dealNextCardTimer {get;set;}
	[Networked] TickTimer endHandTimer {get;set;}
	[Networked] TickTimer givePotToPlayerTimer {get;set;}
	[Networked] TickTimer dealNextStreetTimer {get;set;}
	[Networked] TickTimer startShowdownTimer {get;set;}
	[Networked] TickTimer showdownTimer {get;set;}
	[Networked] TickTimer startNextStreetTimer {get;set;}
	[Networked] TickTimer collectBetsTimer {get;set;}
	[Networked] TickTimer currentActionTimer {get;set;}
	[Networked] public NetworkObject pokerPlayerToGivePotTo {get;set;}
	[Networked, Capacity(17)]	// max capacity needed should be (max players * cards each players get) + max community cards. In word poker, this will be (6 * 2) + 5 = 17
	public NetworkArray<int> currentHandRandomOrder {get;}
	[Networked] bool playerMakingDecision {get;set;}
	[Networked] public NetworkObject dealerButton {get;set;}
	[Networked] public NetworkObject currentDealer {get;set;}
	[Networked] public NetworkObject lastPlayerDealtTo {get;set;}
	[Networked, Capacity(15)]
	public NetworkArray<Vector3Int> gameBlindStructure {get;}
	[Networked, OnChangedRender(nameof(OnBlindLevelChange))] 
	public int currentBlindLevel {get;set;}
	[Networked] public int blindLevelThisHand {get;set;}
	[Networked] public int handsPlayed {get;set;}
	[Networked, OnChangedRender(nameof(OnTimeToNextBlindChanged))] 
	public float timeToNextBlind {get;set;}
	[Networked] public int cardsDealtThisHand {get;set;}
	[Networked, Capacity(128)]
	public NetworkArray<byte> gameDeck {get;}
	[Networked] public int numberOfCardsInDeck {get;set;}
	[Networked, Capacity(26)]
	public NetworkArray<int> letterValues {get;}
	[Networked] public float inGameAnimationTime {get;set;}
	[Networked, OnChangedRender(nameof(OnTotalPotChanged))]
	public int totalCurrentPot {get;set;}
	[Networked] public int largestBet {get;set;}
	[Networked] public int minimumRaise {get;set;}
	[Networked] public int currentStreet {get;set;}
	[Networked] public int dealNextStreetDelayIterations {get;set;}
	[Networked] public bool currentActionCanCheck {get;set;}
	[Networked] public bool currentActionCanBet {get;set;}
	[Networked] public bool handsFaceUp {get;set;}
	[Networked, Capacity(5)]
	public NetworkArray<NetworkObject> communityPots {get;}
	[Networked] public byte numberOfPotsThisHand {get;set;}
	[Networked, Capacity(5)]
	public NetworkArray<NetworkObject> communityCards {get;}
	[Networked, Capacity(5)]
	public NetworkArray<byte> communityLetters {get;}
	[Networked] public bool playedFiveSecondWarningAlready {get;set;}
	[Networked] public int chipsAtGameStart {get;set;}
	
	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void SendPlayerNameToMasterRpc(PlayerRef player, string playerName)
    {
		Debug.Log("SendPlayerNameToMasterRpc");
		FindPlayerObjectByPlayerRef(player).playerName = playerName;
	}
	
	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void SittingOutUpdatedRpc(PlayerRef player, string playerName)
    {
		PokerPlayer sittingOutUpdatedPokerPlayer = FindPlayerObjectByPlayerRef(player);
		sittingOutUpdatedPokerPlayer.playerName = playerName;
		sittingOutUpdatedPokerPlayer.sittingOut = false;
	}
	
	public PokerPlayer FindPlayerObjectByPlayerRef(PlayerRef player)
	{
		for(int i = 0; i < 6; i++)
		{
			if(playersInGame[i] != null)
			{
				PokerPlayer PokerPlayer = playersInGame[i].GetComponent<PokerPlayer>();
				if(PokerPlayer.owningPlayerRef == player)
				{
					// Debug.Log($"FindPlayerObjectByPlayerRef success, found {PokerPlayer.playerName}");
					return PokerPlayer;
				}
			}
		}
		Debug.LogWarning("Could not find player object by player ref. This warning triggers when a player tried to join after the game started, and there were no disconnected players. If you see this in any other context, there's probably a problem.");
		return null;
	}
	
	public void OnTotalPotChanged()
	{
		if(totalCurrentPot == 0)
		{
			PokerHelper.instance.totalPotLabel.ChangeText(string.Empty);
		}
		else
		{
			PokerHelper.instance.totalPotLabel.ChangeText($"Total Pot: {totalCurrentPot}");
		}
	}
	
	public void OnTimeToNextBlindChanged()
	{
		int minutes = (int)timeToNextBlind / 60;
		int seconds = (int)timeToNextBlind % 60;
		string nextLevelTimer = minutes.ToString() + ":";
		if(seconds < 10)
		{
			nextLevelTimer += "0" + seconds.ToString();
		}
		else
		{
			nextLevelTimer += seconds.ToString();
		}
		LocalInterface.instance.timerLabel.ChangeText(nextLevelTimer);
	}
	
	public void OnMaxTimeToActChanged()
	{
		// Debug.Log($"<color=green>[Silver]</color> OnMaxTimeToActChanged, master = {LocalInterface.instance.networkRunner.IsSharedModeMasterClient.ToString()} maximumDecisionTime = {maximumDecisionTime.ToString()}");
		if(!LocalInterface.instance.networkRunner.IsSharedModeMasterClient)
		{
			LocalInterface.instance.timeToActSliderHelper.slider.value = Mathf.RoundToInt((maximumDecisionTime - LocalInterface.instance.timeToActSliderHelper.baseValue) / LocalInterface.instance.timeToActSliderHelper.multiplier);
		}
		LocalInterface.instance.clientTimeToActLabel.ChangeText($"{Mathf.RoundToInt(maximumDecisionTime).ToString()} Seconds");
	}
	
	public void OnTimeBetweenBlindLevelsChanged()
	{
		if(!LocalInterface.instance.networkRunner.IsSharedModeMasterClient)
		{
			LocalInterface.instance.timeBetweenBlindsSliderHelper.slider.value = Mathf.RoundToInt((timeBetweenBlindLevels - LocalInterface.instance.timeBetweenBlindsSliderHelper.baseValue) / LocalInterface.instance.timeBetweenBlindsSliderHelper.multiplier);
		}
		LocalInterface.instance.clientTimeBetweenBlindsLabel.ChangeText($"{Mathf.RoundToInt(timeBetweenBlindLevels / 60).ToString()} Minutes");
	}
	
	public void OnDictionaryChanged()
	{
		switch(dictionaryBeingUsed)
		{
			case 0:
				LocalInterface.instance.clientDictionaryUsedLabel.ChangeText("Common (~25k)");
				break;
			case 1:
				LocalInterface.instance.clientDictionaryUsedLabel.ChangeText("Complete (~280k)");
				break;
		}
		if(!LocalInterface.instance.networkRunner.IsSharedModeMasterClient)
		{
			LocalInterface.instance.wordListDropdown.value = dictionaryBeingUsed;
		}
	}
	
	public override void Spawned()
	{
		base.Spawned();
		OnDictionaryChanged();
		OnTimeBetweenBlindLevelsChanged();
		OnMaxTimeToActChanged();
		OnBlindLevelChange();
		OnTotalPotChanged();
		// LocalInterface.instance.gameManager = this;
	}
	
	public int AddPlayerToGame(NetworkObject joiningPlayer)
	{
		int i = 0;
		while(playersInGame[i] != null)
		{
			i++;
		}
		playersInGame.Set(i, joiningPlayer);
		return i;
	}
	
	public void RemovePlayerFromGame(NetworkObject leavingPlayer)
	{
		int i = 0;
		while(playersInGame[i] != leavingPlayer)
		{
			i++;
		}
		for(int j = i + 1; j < 6; j++)
		{
			playersInGame.Set(j - 1, playersInGame[j]);
		}
		playersInGame.Set(5, null);
	}
	
	public int[] GetRandomizedIntArray(int l)
	{
		int[] randomOrder = new int[l];
		for(int i = 0; i <  l; i++)
		{
			randomOrder[i] = i;
		}
		for(int i = 0; i < l; i++)
		{
			int r = UnityEngine.Random.Range(0, i + 1);
			int temp = randomOrder[i];
			randomOrder[i] = randomOrder[r];
			randomOrder[r] = temp;
		}
		return randomOrder;
	}
	
	public void ReorganizePlayersInGame()
	{
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			if(playersInGame[i] == null)
			{
				int nextIndex = i + 1;
				while(nextIndex < 6)
				{
					if(playersInGame[nextIndex] != null)
					{
						playersInGame.Set(i, playersInGame[nextIndex]);
						playersInGame.Set(nextIndex, null);
					}
				}
			}
		}
	}
		
	public List<PokerPlayer> GetAllDisconnectedPokerPlayers()
	{
		List<PokerPlayer> disconnectedPlayers = new List<PokerPlayer>();
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			PokerPlayer pokerPlayer = playersInGame[i].GetComponent<PokerPlayer>();
			if(pokerPlayer.disconnected)
			{
				disconnectedPlayers.Add(pokerPlayer);
			}
		}
		return disconnectedPlayers;
	}
	
	public List<PokerPlayer> GetAllConnectedPokerPlayers()
	{
		List<PokerPlayer> connectedPlayers = new List<PokerPlayer>();
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			PokerPlayer pokerPlayer = playersInGame[i].GetComponent<PokerPlayer>();
			if(!pokerPlayer.disconnected)
			{
				connectedPlayers.Add(pokerPlayer);
			}
		}
		return connectedPlayers;
	}
	
	public List<PokerPlayer> GetAllPokerPlayers()
	{
		if(!LocalInterface.instance.fusionInterface.HasGameStarted())
		{
			ReorganizePlayersInGame();
		}
		List<PokerPlayer> pokerPlayers = new List<PokerPlayer>();
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			PokerPlayer pokerPlayer = playersInGame[i].GetComponent<PokerPlayer>();
			pokerPlayers.Add(pokerPlayer);
		}
		return pokerPlayers;
	}
	
	public List<PokerPlayer> GetAllPokerPlayersWithChips()
	{
		List<PokerPlayer> pokerPlayers = new List<PokerPlayer>();
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			PokerPlayer pokerPlayer = playersInGame[i].GetComponent<PokerPlayer>();
			if(pokerPlayer.chips > 0)
			{
				pokerPlayers.Add(pokerPlayer);
			}
		}
		return pokerPlayers;
	}
	
	public List<PokerPlayer> GetAllPokerPlayersWithCards()
	{
		List<PokerPlayer> pokerPlayers = new List<PokerPlayer>();
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			PokerPlayer pokerPlayer = playersInGame[i].GetComponent<PokerPlayer>();
			if(pokerPlayer.hasCards)
			{
				pokerPlayers.Add(pokerPlayer);
			}
		}
		return pokerPlayers;
	}
	
	public List<PokerPlayer> GetAllPokerPlayersWithBets()
	{
		List<PokerPlayer> pokerPlayers = new List<PokerPlayer>();
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			PokerPlayer pokerPlayer = playersInGame[i].GetComponent<PokerPlayer>();
			if(pokerPlayer.GetCurrentBet() > 0)
			{
				pokerPlayers.Add(pokerPlayer);
			}
		}
		return pokerPlayers;
	}
	
	public List<PokerPlayer> GetAllPokerPlayersWhoHadChipsAtHandStart()
	{
		List<PokerPlayer> pokerPlayers = new List<PokerPlayer>();
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			PokerPlayer pokerPlayer = playersInGame[i].GetComponent<PokerPlayer>();
			if(pokerPlayer.hadChipsAtHandStart)
			{
				pokerPlayers.Add(pokerPlayer);
			}
		}
		return pokerPlayers;
	}
	
	public PokerPlayer GetPlayerToTheLeft(PokerPlayer pokerPlayer, bool needsChips, bool needsCards, bool needsActionOpen, bool needsToHaveHadChips)
	{
		int nextPlayerIndex = pokerPlayer.inGameOrder + 1;
		if(nextPlayerIndex >= numberOfPlayersInGame)
		{
			nextPlayerIndex = 0;
		}
		while(nextPlayerIndex != pokerPlayer.inGameOrder)
		{
			PokerPlayer nextPlayer = playersInGame[nextPlayerIndex].GetComponent<PokerPlayer>();
			if( ((needsChips && nextPlayer.chips > 0) || !needsChips) && ((needsCards && nextPlayer.hasCards) || !needsCards) && ((needsActionOpen && nextPlayer.actionOpen) || !needsActionOpen) && ((needsToHaveHadChips && nextPlayer.hadChipsAtHandStart) || !needsToHaveHadChips) )
			{
				return nextPlayer;
			}
			nextPlayerIndex++;
			if(nextPlayerIndex >= numberOfPlayersInGame)
			{
				nextPlayerIndex = 0;
			}
		}
		// Debug.LogWarning("GetPlayerToTheLeft found no suitable player");
		return null;
	}
	
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void HideLobbyRpc()
	{
		LocalAnimations.instance.HideLobbyInterface();
	}
	
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RevealGameplayRpc()
	{
		LocalAnimations.instance.RevealGameplayInterface();
		currentBlindLevel = 0;
	}
	
	public void StartGame()
	{
		HideLobbyRpc();
		for(int i = 0; i < PokerHelper.instance.defaultBlindStructure.Length; i++)
		{
			gameBlindStructure.Set(i, PokerHelper.instance.defaultBlindStructure[i]);
		}
		int cardsInDeck = 0;
		for(int i = 0; i < PokerHelper.instance.wordPokerCards.Length; i++)
		{
			letterValues.Set(i, PokerHelper.instance.wordPokerCards[i].val);
			for(int j = 0; j < PokerHelper.instance.wordPokerCards[i].quantity; j++)
			{
				gameDeck.Set(cardsInDeck, Convert.ToByte(i));
				cardsInDeck++;
			}
		}
		blindLevelThisHand = 0;
		inGameAnimationTime = LocalAnimations.instance.animationTime;
		numberOfCardsInDeck = cardsInDeck;
 		currentBlindLevel = -1;
		revealGameplayTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime);
	}
	
	public void AssignSeats()
	{
		ReorganizePlayersInGame();
		int[] randomOrder = GetRandomizedIntArray(numberOfPlayersInGame);
		List<PokerPlayer> allPokerPlayers = new List<PokerPlayer>();
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			PokerPlayer pokerPlayer = playersInGame[i].GetComponent<PokerPlayer>();
			allPokerPlayers.Add(pokerPlayer);
			pokerPlayer.originalConnectionOrder = i;
			pokerPlayer.inGameOrder = randomOrder[i];
			int tableSeat = PokerHelper.instance.PlacePlayer(numberOfPlayersInGame, pokerPlayer.inGameOrder);
			pokerPlayer.MoveToSeat(PokerHelper.instance.seatLocations[tableSeat], tableSeat);
			//pokerPlayer.wordRevealLabel.transform.position = PokerHelper.instance.revealLabelLocations[tableSeat];
		}
		allPokerPlayers.Sort((x,y) => x.inGameOrder - y.inGameOrder);
		for(int i = 0; i < allPokerPlayers.Count; i++)
		{
			playersInGame.Set(i, allPokerPlayers[i].networkObject);
		}
		NetworkObject newDealerButtonNO = LocalInterface.instance.networkRunner.Spawn(LocalInterface.instance.dealerButtonPrefab, Vector3.zero, Quaternion.identity);
		DealerButton newDealerButton = newDealerButtonNO.GetComponent<DealerButton>();
		dealerButton = newDealerButton.networkObject;
		newDealerButton.networkTransform.Teleport(PokerHelper.instance.dealerButtonPositions[allPokerPlayers[0].seatPosition]);
		currentDealer = allPokerPlayers[0].networkObject;
		
		if(allPokerPlayers.Count > 2) // setup hands since being big blind
		{
			allPokerPlayers[0].handsSinceBeingBigBlind = 2;
			allPokerPlayers[1].handsSinceBeingBigBlind = 1;
			allPokerPlayers[2].handsSinceBeingBigBlind = 0;
			for(int i = 3; i < allPokerPlayers.Count; i++)
			{
				allPokerPlayers[i].handsSinceBeingBigBlind = allPokerPlayers.Count - i + 2;
			}
		}
	}
	
	public void OnBlindLevelChange()
	{
		if(currentBlindLevel < 0)
		{
			return;
		}
		Debug.Log($"OnBlindLevelChange, currentBlindLevel = {currentBlindLevel.ToString()} gameBlindStructure.Length = {gameBlindStructure.Length.ToString()}");
		string blindInfo = "";
		for(int i = 0; i < gameBlindStructure.Length; i++)
		{
			if(gameBlindStructure[i].x == 0)
			{
				break;
			}
			if(i == currentBlindLevel)
			{
				blindInfo += "<color=red>";
			}
			blindInfo += gameBlindStructure[i].x.ToString() + "/" + gameBlindStructure[i].y.ToString();
			if(gameBlindStructure[i].z > 0)
			{
				blindInfo += "/" + gameBlindStructure[i].z.ToString();
			}
			if(i == currentBlindLevel)
			{
				blindInfo += "</color>";
			}
			if(i < gameBlindStructure.Length - 1)
			{
				blindInfo += "\n";
			}
		}
		
		string currentBlindInfo = gameBlindStructure[currentBlindLevel].x.ToString() + "/" + gameBlindStructure[currentBlindLevel].y.ToString();
		if(gameBlindStructure[currentBlindLevel].z > 0)
		{
			currentBlindInfo += "\n" + gameBlindStructure[currentBlindLevel].z.ToString() + " ante";
		}
		LocalInterface.instance.currentBlindsLabel.ChangeText(currentBlindInfo);
		
		LocalInterface.instance.blindStructureInformationLabel.ChangeText(blindInfo, true);
		LocalInterface.instance.blindStructureInformationLabel.ForceMeshUpdate();
		LocalInterface.instance.blindStructureInformationBackdropRT.sizeDelta = new Vector2(LocalInterface.instance.blindStructureInformationBackdropRT.sizeDelta.x, LocalInterface.instance.blindStructureInformationLabel.label.textBounds.size.y + 12);
		if(currentBlindLevel >= gameBlindStructure.Length - 1)
		{
			LocalInterface.instance.timerLabel.ChangeText("Final");
		}
		else
		{
			if(gameBlindStructure[currentBlindLevel + 1].x <= 0 && gameBlindStructure[0].x != 0)
			{
				LocalInterface.instance.timerLabel.ChangeText("Final");
			}
		}
	}
	
	public void DealCardTo(PokerPlayer pokerPlayer)
	{
		NetworkObject newCardNO = LocalInterface.instance.networkRunner.Spawn(LocalInterface.instance.cardPrefab, new Vector3(0,0,-5), Quaternion.identity);
		Card newCard = newCardNO.GetComponent<Card>();
		newCard.owningPlayer = pokerPlayer.owningPlayerRef;
		newCard.cardString = ConvertByteToLetter(gameDeck[currentHandRandomOrder[cardsDealtThisHand]]);
		newCard.cardValue = letterValues[ConvertLetterToNumber(newCard.cardString)];
		if(pokerPlayer.currentCard1 == null)
		{
			pokerPlayer.currentCard1 = newCardNO;
			newCard.MoveToPosition(pokerPlayer.transform.position + new Vector3(-0.24f, 0.22f, -5f));
		}
		else
		{
			pokerPlayer.currentCard2 = newCardNO;
			newCard.MoveToPosition(pokerPlayer.transform.position + new Vector3(0.24f, 0.22f, -5f));
			UpdatePlayerBestWord(pokerPlayer);
		}
		cardsDealtThisHand++;
		lastPlayerDealtTo = pokerPlayer.networkObject;
	}
	
	public string ConvertByteToLetter(byte b)
	{
		char letter = (char)('A' + b);
		return letter.ToString();
	}
	
	public int ConvertLetterToNumber(string letter)
	{
		char character = char.Parse(letter);
		int number = character - 'A';
		return number;
	}
	
	[Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
	public void PlayerWonRpc(PlayerRef playerWhoWon, string playerWhoWonName)
	{
		if(playerWhoWon == LocalInterface.instance.fusionInterface.localPlayerRef)
		{
			Menu.instance.DisplayError("You Win!");
		}
		else
		{
			Menu.instance.DisplayError($"{playerWhoWonName} Won!");
		}
	}
	
	public void ResetLobby()
	{
		Dictionary<string, SessionProperty> sessionProperties = new Dictionary<string, SessionProperty>();
		sessionProperties["GameStarted"] = (bool)false;
		LocalInterface.instance.networkRunner.SessionInfo.UpdateCustomProperties(sessionProperties);
		List<PokerPlayer> disconnectedPlayers = GetAllDisconnectedPokerPlayers();
		for(int i = disconnectedPlayers.Count - 1; i >= 0; i--)
		{
			LocalInterface.instance.networkRunner.Despawn(disconnectedPlayers[i].networkObject);
		}
		LocalInterface.instance.networkRunner.Despawn(dealerButton);
		List<PokerPlayer> connectedPlayers = GetAllConnectedPokerPlayers();
		handsPlayed = 0;
		blindLevelTimer = TickTimer.None;
		connectedPlayers.Sort((x,y) => x.originalConnectionOrder - y.originalConnectionOrder);
		for(int i = 0; i < connectedPlayers.Count; i ++)
		{
			connectedPlayers[i].hasBeenEliminated = false;
			connectedPlayers[i].sittingOut = false;
			connectedPlayers[i].originalConnectionOrder = i;
			LocalInterface.instance.fusionInterface.StartingChipsUpdated(chipsAtGameStart);
		}
		ReorganizePlayersInGame();
		LocalInterface.instance.fusionInterface.ReorganizeLobbyPlayers();
		ResetLobbyRpc();
	}
	
	[Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
	public void ResetLobbyRpc()
	{
		LocalAnimations.instance.HideGameplayInterface();
		LocalAnimations.instance.RevealLobbyInterface();
	}
	
	public void StartNextHand()
	{
		// do stuff involving hands played, like the game history
		cardsDealtThisHand = 0;
		numberOfPotsThisHand = 0;
		handsFaceUp = false;
		blindLevelThisHand = currentBlindLevel;
		List<PokerPlayer> allPokerPlayers = GetAllPokerPlayers();
		for(int i = 0; i < allPokerPlayers.Count; i++)
		{
			if(allPokerPlayers[i].chips <= 0)
			{
				// allPokerPlayers[i].foldedThisTurn = false;
				allPokerPlayers[i].hasBeenEliminated = true;
			}
		}
		List<PokerPlayer> playersWithChips = GetAllPokerPlayersWithChips();
		if(playersWithChips.Count == 1)
		{
			// Debug.Log($"Game Over, {playersWithChips[0].playerName} wins");
			PlayerWonRpc(playersWithChips[0].owningPlayerRef, playersWithChips[0].playerName);
			ResetLobby();
			return;
		}
		for(int i = 0; i < playersWithChips.Count; i++)
		{
			playersWithChips[i].hadChipsAtHandStart = true;
			playersWithChips[i].hasCards = true;
			// playersWithChips[i].foldedThisTurn = false;
		}
		
		// determine button and blinds
		playersWithChips.Sort((x,y) => x.handsSinceBeingBigBlind - y.handsSinceBeingBigBlind);
		if(playersWithChips.Count == 2)
		{
			currentDealer = playersWithChips[0].networkObject;
			dealerButton.GetComponent<DealerButton>().MoveToSeat(playersWithChips[0].seatPosition);
			PostBlind(playersWithChips[0], gameBlindStructure[blindLevelThisHand].x, false);
			PostBlind(playersWithChips[1], gameBlindStructure[blindLevelThisHand].y, true);
			playersWithChips[0].handsSinceBeingBigBlind++;
			playersWithChips[1].handsSinceBeingBigBlind = 0;
			currentAction = playersWithChips[0].networkObject;
		}
		else
		{
			PokerPlayer bigBlind = playersWithChips[playersWithChips.Count - 1];
			PostBlind(bigBlind, gameBlindStructure[blindLevelThisHand].y, true);
			PokerPlayer rightOfBB = GetPlayerToTheRight(bigBlind);
			PokerPlayer nextDealer;
			if(rightOfBB.handsSinceBeingBigBlind == 0)
			{
				nextDealer = GetPlayerToTheRight(rightOfBB);
				PostBlind(rightOfBB, gameBlindStructure[blindLevelThisHand].x, false);
			}
			else
			{
				nextDealer = rightOfBB;
				// dead small blind this hand
			}
			currentDealer = nextDealer.networkObject;
			dealerButton.GetComponent<DealerButton>().MoveToSeat(currentDealer.GetComponent<PokerPlayer>().seatPosition);
			currentAction = GetPlayerToTheLeft(bigBlind, true, false, false, false).networkObject;
			for(int i = 0; i < playersWithChips.Count; i++)
			{
				playersWithChips[i].handsSinceBeingBigBlind++;
			}
			bigBlind.handsSinceBeingBigBlind = 0;
		}
		
		largestBet = gameBlindStructure[blindLevelThisHand].y;
		minimumRaise = gameBlindStructure[blindLevelThisHand].y;
		currentStreet = 1;
		ResetPlayerActionOpen(true);
		
		int[] randomOrder = GetRandomizedIntArray(numberOfCardsInDeck);
		for(int i = 0; i < (playersWithChips.Count * 2 + 5); i++)
		{
			currentHandRandomOrder.Set(i, randomOrder[i]);
		}
		PokerPlayer firstToDealTo = GetPlayerToTheLeft(currentDealer.GetComponent<PokerPlayer>(), false, false, false, true);
		DealCardTo(firstToDealTo);
		lastPlayerDealtTo = firstToDealTo.networkObject;
		dealNextCardTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime / 10f);
	}
	
	public void ResetPlayerActionOpen(bool openRaising)
	{
		List<PokerPlayer> playersWithChipsAndCards = GetAllPlayersWithChipsAndCards();
		for(int i = 0; i < playersWithChipsAndCards.Count; i++)
		{
			playersWithChipsAndCards[i].actionOpen = true;
			playersWithChipsAndCards[i].raiseOpen = openRaising;
		}
	}
	
	public List<PokerPlayer> GetAllPlayersWithChipsAndCards()
	{
		List<PokerPlayer> playersWithChipsAndCards = new List<PokerPlayer>();
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			PokerPlayer pokerPlayer = playersInGame[i].GetComponent<PokerPlayer>();
			if(pokerPlayer.chips > 0 && pokerPlayer.hasCards)
			{
				playersWithChipsAndCards.Add(pokerPlayer);
			}
		}
		return playersWithChipsAndCards;
	}
	
	public PokerPlayer GetPlayerToTheRight(PokerPlayer pokerPlayer)
	{
		int nextPlayerIndex = pokerPlayer.inGameOrder - 1;
		if(nextPlayerIndex < 0)
		{
			nextPlayerIndex = numberOfPlayersInGame - 1;
		}
		while(nextPlayerIndex != pokerPlayer.inGameOrder)
		{
			PokerPlayer nextRight = playersInGame[nextPlayerIndex].GetComponent<PokerPlayer>();
			if(nextRight.hadChipsAtHandStart)
			{
				return nextRight;
			}
			nextPlayerIndex--;
			if(nextPlayerIndex < 0)
			{
				nextPlayerIndex = numberOfPlayersInGame - 1;
			}
		}
		Debug.LogWarning("GetPlayerToTheRight found no eligibile player");
		return null;
	}
	
	public void PostBlind(PokerPlayer pokerPlayer, int amount, bool isBigBlind)
	{
		int addedToPot = amount;
		if(pokerPlayer.chips < amount)
		{
			addedToPot = pokerPlayer.chips;
		}
		pokerPlayer.chips -= addedToPot;
		pokerPlayer.UpdateBet(addedToPot);
		totalCurrentPot += addedToPot;
	}
	
	public override void FixedUpdateNetwork()
	{
		if(revealGameplayTimer.Expired(LocalInterface.instance.networkRunner))
		{
			RevealGameplayRpc();
			assignSeatsTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime);
			revealGameplayTimer = TickTimer.None;
		}
		if(assignSeatsTimer.Expired(LocalInterface.instance.networkRunner))
		{
			AssignSeats();
			startNextHandTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime * 1.2f);
			assignSeatsTimer = TickTimer.None;
		}
		if(startNextHandTimer.Expired(LocalInterface.instance.networkRunner))
		{
			startNextHandTimer = TickTimer.None;
			if(handsPlayed == 0)
			{
				blindLevelTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, timeBetweenBlindLevels);
			}
			StartNextHand();
		}
		if(blindLevelTimer.Expired(LocalInterface.instance.networkRunner))
		{
			if(currentBlindLevel < gameBlindStructure.Length - 1)
			{
				if(gameBlindStructure[currentBlindLevel + 1].x > 0)
				{
					currentBlindLevel++;
					if(currentBlindLevel < gameBlindStructure.Length - 1)
					{
						if(gameBlindStructure[currentBlindLevel + 1].x > 0)
						{
							blindLevelTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, timeBetweenBlindLevels);
						}
						else
						{
							blindLevelTimer = TickTimer.None;
						}
					}
					else
					{
						blindLevelTimer = TickTimer.None;
					}
				}
			}
		}
		if(blindLevelTimer.IsRunning)
		{
			timeToNextBlind = (float)blindLevelTimer.RemainingTime(LocalInterface.instance.networkRunner);
		}
		if(dealNextCardTimer.Expired(LocalInterface.instance.networkRunner))
		{
			PokerPlayer playerToTheLeftOfLastToBeDealtTo = GetPlayerToTheLeft(lastPlayerDealtTo.GetComponent<PokerPlayer>(), false, false, false, true);
			if(playerToTheLeftOfLastToBeDealtTo.currentCard2 == null)
			{
				DealCardTo(playerToTheLeftOfLastToBeDealtTo);
				dealNextCardTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime / 10f);
			}
			else
			{
				CurrentActionUpdated();
				dealNextCardTimer = TickTimer.None;
			}
		}
		if(givePotToPlayerTimer.Expired(LocalInterface.instance.networkRunner))
		{
			GivePotToPlayer(pokerPlayerToGivePotTo);
			givePotToPlayerTimer = TickTimer.None;
		}
		if(endHandTimer.Expired(LocalInterface.instance.networkRunner))
		{
			EndHand();
			endHandTimer = TickTimer.None;
		}
		if(dealNextStreetTimer.Expired(LocalInterface.instance.networkRunner))
		{
			DealNextStreet(dealNextStreetDelayIterations);
			dealNextStreetTimer = TickTimer.None;
		}
		if(startShowdownTimer.Expired(LocalInterface.instance.networkRunner))
		{
			StartShowdown();
			startShowdownTimer = TickTimer.None;
		}
		if(showdownTimer.Expired(LocalInterface.instance.networkRunner))
		{
			Showdown();
			showdownTimer = TickTimer.None;
		}
		if(startNextStreetTimer.Expired(LocalInterface.instance.networkRunner))
		{
			StartNextStreet();
			startNextStreetTimer = TickTimer.None;
		}
		if(collectBetsTimer.IsRunning)
		{
			Debug.Log($"collectBetsTimer is running, time left = {(float)collectBetsTimer.RemainingTime(LocalInterface.instance.networkRunner)}");
		}
		if(collectBetsTimer.Expired(LocalInterface.instance.networkRunner))
		{
			collectBetsTimer = TickTimer.None;
			CollectBets();
		}
		if(currentActionTimer.IsRunning)
		{
			PokerPlayer currentActionPokerPlayer = currentAction.GetComponent<PokerPlayer>();
			currentActionPokerPlayer.timeRemaining = (float)currentActionTimer.RemainingTime(LocalInterface.instance.networkRunner);
			if((float)currentActionTimer.RemainingTime(LocalInterface.instance.networkRunner) < 5f && !playedFiveSecondWarningAlready)
			{
				playedFiveSecondWarningAlready = true;
				currentActionPokerPlayer.timerDisplayColor = Color.red;
				PlayFiveSecondWarningSoundRpc(currentAction.GetComponent<PokerPlayer>().owningPlayerRef);
			}
		}
		if(currentActionTimer.Expired(LocalInterface.instance.networkRunner))
		{
			currentActionTimer = TickTimer.None;
			PokerPlayer currentActionPokerPlayer = currentAction.GetComponent<PokerPlayer>();
			currentActionPokerPlayer.sittingOut = true;
			currentActionPokerPlayer.playerName = "AFK";
			SitPlayerOutRpc(currentActionPokerPlayer.owningPlayerRef);
			PlayerFoldOrCheckRpc(currentActionPokerPlayer.owningPlayerRef);
		}
	}
	
	[Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
	public void SitPlayerOutRpc([RpcTarget] PlayerRef player)
	{
		PokerHelper.instance.sittingOutInterface.SetActive(true);
		PokerHelper.instance.sittingOutToggle.isOn = true;
		PokerHelper.instance.inputInterfaceGO.SetActive(false);
	}
	
 	[Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
	public void PlayFiveSecondWarningSoundRpc([RpcTarget] PlayerRef player)
	{
		SoundManager.instance.PlayFiveSecondWarningSound();
	}
	
	[Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
	public void PlayMyTurnSoundRpc([RpcTarget] PlayerRef player)
	{
		SoundManager.instance.PlayMyTurnSound();
	}
	
	public void CurrentActionUpdated()
	{
		PokerPlayer currentActionPokerPlayer = currentAction.GetComponent<PokerPlayer>();
		if(currentActionPokerPlayer.chips <= 0)
		{
			MoveAction();
			return;
		}
		else
		{
			List<PokerPlayer> playersWhoHadChips = GetAllPokerPlayersWhoHadChipsAtHandStart();
			bool playerNeedsToAct = false;
			for(int i = 0; i < playersWhoHadChips.Count; i++)
			{
				if(playersWhoHadChips[i].networkObject != currentAction)
				{
					if(playersWhoHadChips[i].chips > 0 || playersWhoHadChips[i].GetCurrentBet() > currentActionPokerPlayer.GetCurrentBet())
					{
						playerNeedsToAct = true;
						break;
					}
				}
			}
			if(!playerNeedsToAct)
			{
				MoveAction();
				return;
			}
		}
		PlayMyTurnSoundRpc(currentActionPokerPlayer.owningPlayerRef);
		playedFiveSecondWarningAlready = false;
		currentActionTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, maximumDecisionTime);
		currentActionPokerPlayer.timeRemaining = maximumDecisionTime;
		currentActionPokerPlayer.timerDisplayColor = Color.yellow;
		currentActionPokerPlayer.myTurn = true;
		currentActionPokerPlayer.currentTurnIndicatorTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, PokerHelper.instance.timeBetweenCurrentTurnIndicatorBlinks / 20f);
		bool canCheck = false;
		bool canCall = false;
		bool canBet = false;
		bool canRaise = false;
		bool canFold = false;
		
		if(currentActionPokerPlayer.GetCurrentBet() == largestBet)
		{
			canCheck = true;
			if(largestBet == 0)
			{
				currentActionPokerPlayer.betting = true;
				canBet = true;
			}
			else
			{
				currentActionPokerPlayer.betting = false;
				canRaise = true;
			}
		}
		else
		{
			canCall = true;
			canFold = true;
			// Debug.Log($"<color=green>[Silver]</color> chips = {currentActionPokerPlayer.chips} curBet = {currentActionPokerPlayer.GetCurrentBet()} largestBet = {largestBet} raiseOpen = {currentActionPokerPlayer.raiseOpen.ToString()}");
			if(currentActionPokerPlayer.chips + currentActionPokerPlayer.GetCurrentBet() > largestBet && currentActionPokerPlayer.raiseOpen)
			{
				List<PokerPlayer> playersWithChipsAndCards = GetAllPlayersWithChipsAndCards();
				// Debug.Log($"<color=green>[Silver]</color> playersWithChipsAndCards.Count = {playersWithChipsAndCards.Count}");
				for(int i = 0; i < playersWithChipsAndCards.Count; i++)
				{
					if(playersWithChipsAndCards[i] != currentActionPokerPlayer)
					{
						canRaise = true;
						break;
					}
				}
			}
		}
		if(canCheck)
		{
			currentActionCanCheck = true;
		}
		else
		{
			currentActionCanCheck = false;
		}
		
		bool canChooseHowMuchToRaise = true;
		int sliderMaxValue = 0;
		int practicalMinimumRaise = minimumRaise;
		
		if(canBet || canRaise)
		{
			if(largestBet + minimumRaise >= currentActionPokerPlayer.GetCurrentBet() + currentActionPokerPlayer.chips)
			{
				practicalMinimumRaise = currentActionPokerPlayer.GetCurrentBet() + currentActionPokerPlayer.chips - largestBet;
				canChooseHowMuchToRaise = false;
			}
			else
			{
				sliderMaxValue = Mathf.CeilToInt((currentActionPokerPlayer.GetCurrentBet() + currentActionPokerPlayer.chips - practicalMinimumRaise - largestBet) / gameBlindStructure[blindLevelThisHand].x) + 1;
			}
			currentActionCanBet = canBet;
		}
		
		if(currentActionPokerPlayer.disconnected || currentActionPokerPlayer.sittingOut)
		{
			PlayerFoldOrCheckRpc(currentActionPokerPlayer.owningPlayerRef);
		}
		else
		{
			EnableInputUIRpc(currentActionPokerPlayer.owningPlayerRef, canCheck, canCall, canFold, canBet, canRaise, canChooseHowMuchToRaise, sliderMaxValue, practicalMinimumRaise);
		}
	}
	
	[Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
	public void EnableInputUIRpc([RpcTarget] PlayerRef player, bool canCheck, bool canCall, bool canFold, bool canBet, bool canRaise, bool canChooseHowMuchToRaise, int sliderMaxValue, int practicalMinimumRaise)
	{
		PokerPlayer currentActionPokerPlayer = currentAction.GetComponent<PokerPlayer>();
		PokerHelper.instance.inputInterfaceGO.SetActive(true);
		if(canCheck)
		{
			PokerHelper.instance.foldOrCheckButton.ChangeLabel("Check");
		}
		if(canCall)
		{
			PokerHelper.instance.callButton.gameObject.SetActive(true);
		}
		else
		{
			PokerHelper.instance.callButton.gameObject.SetActive(false);
		}
		if(canBet || canRaise)
		{
			PokerHelper.instance.betOrRaiseButton.gameObject.SetActive(true);
			if(canChooseHowMuchToRaise)
			{
				PokerHelper.instance.betInputField.gameObject.SetActive(true);
				PokerHelper.instance.betSlider.gameObject.SetActive(true);
				PokerHelper.instance.increaseBetButton.gameObject.SetActive(true);
				PokerHelper.instance.decreaseBetButton.gameObject.SetActive(true);
				PokerHelper.instance.decreaseBetButton.ChangeDisabled(true);
				PokerHelper.instance.betSlider.maxValue = sliderMaxValue;
				PokerHelper.instance.betSlider.value = 0;
				PokerHelper.instance.betInputField.text = (largestBet + practicalMinimumRaise).ToString();
				if(canBet)
				{
					PokerHelper.instance.betOrRaiseButton.ChangeLabel($"Bet {(largestBet + practicalMinimumRaise).ToString()}");
				}
				else
				{
					PokerHelper.instance.betOrRaiseButton.ChangeLabel($"Raise to {(largestBet + practicalMinimumRaise).ToString()}");
				}
			}
			else
			{
				PokerHelper.instance.betInputField.gameObject.SetActive(false);
				PokerHelper.instance.betSlider.gameObject.SetActive(false);
				PokerHelper.instance.increaseBetButton.gameObject.SetActive(false);
				PokerHelper.instance.decreaseBetButton.gameObject.SetActive(false);
				PokerHelper.instance.betInputField.text = (largestBet + practicalMinimumRaise).ToString();
				if(canBet)
				{
					PokerHelper.instance.betOrRaiseButton.ChangeLabel($"Bet {(largestBet + practicalMinimumRaise).ToString()}");
				}
				else
				{
					PokerHelper.instance.betOrRaiseButton.ChangeLabel($"Raise to {(largestBet + practicalMinimumRaise).ToString()}");
				}
			}
		}
		else
		{
			PokerHelper.instance.betInputField.gameObject.SetActive(false);
			PokerHelper.instance.betSlider.gameObject.SetActive(false);
			PokerHelper.instance.betOrRaiseButton.gameObject.SetActive(false);
			PokerHelper.instance.increaseBetButton.gameObject.SetActive(false);
			PokerHelper.instance.decreaseBetButton.gameObject.SetActive(false);
		}
		if(canFold)
		{
			PokerHelper.instance.foldOrCheckButton.ChangeLabel("Fold");
		}
	}
	
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void PlaySoundRpc(byte soundToPlay)
	{
		switch(soundToPlay)
		{
			case 0:
				SoundManager.instance.PlayFoldSound();
				break;
			case 1:
				SoundManager.instance.PlayCheckSound();
				break;
			case 2:
				SoundManager.instance.PlayCallSound();
				break;
			case 3:
				SoundManager.instance.PlayBetSound();
				break;
		}
	}
	
	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void PlayerFoldOrCheckRpc(PlayerRef actingPlayer)
	{
		PokerPlayer currentActionPokerPlayer = currentAction.GetComponent<PokerPlayer>();
		if(actingPlayer == currentActionPokerPlayer.owningPlayerRef)
		{
			PlayerHasActed(currentActionPokerPlayer);
			if(currentActionCanCheck)
			{
				PlaySoundRpc((byte)1);
				currentActionPokerPlayer.DisplayActionTaken(1);
			}
			else
			{
				PlaySoundRpc((byte)0);
				currentActionPokerPlayer.DisplayActionTaken(0);
				currentActionPokerPlayer.PlayerFolds();
			}
			MoveAction();
		}
		else
		{
			Debug.LogWarning("Received fold/check input from player who does not match the playerRef of the currentAction player");
		}
	}
	
	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void PlayerCallRpc(PlayerRef actingPlayer)
	{
		PokerPlayer currentActionPokerPlayer = currentAction.GetComponent<PokerPlayer>();
		if(actingPlayer == currentActionPokerPlayer.owningPlayerRef)
		{
			PlaySoundRpc((byte)2);
			PlayerHasActed(currentActionPokerPlayer);
			int amountNeededToCall = largestBet - currentActionPokerPlayer.GetCurrentBet();
			int amountCalled = 0;
			if(currentActionPokerPlayer.chips > amountNeededToCall)
			{
				amountCalled = amountNeededToCall;
			}
			else
			{
				amountCalled = currentActionPokerPlayer.chips;
			}
			currentActionPokerPlayer.chips -= amountCalled;
			totalCurrentPot += amountCalled;
			currentActionPokerPlayer.UpdateBet(currentActionPokerPlayer.GetCurrentBet() + amountCalled);
			currentActionPokerPlayer.DisplayActionTaken(2);
			MoveAction();
		}
		else
		{
			Debug.LogWarning("Received call input from player who does not match the playerRef of the currentAction player");
		}
	}
	
	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void PlayerBetOrRaiseRpc(PlayerRef actingPlayer, int newBet)
	{
		PokerPlayer currentActionPokerPlayer = currentAction.GetComponent<PokerPlayer>();
		if(actingPlayer == currentActionPokerPlayer.owningPlayerRef)
		{
			PlaySoundRpc((byte)3);
			PlayerHasActed(currentActionPokerPlayer);
			totalCurrentPot += newBet - currentActionPokerPlayer.GetCurrentBet();
			currentActionPokerPlayer.chips -= newBet - currentActionPokerPlayer.GetCurrentBet();
			currentActionPokerPlayer.UpdateBet(newBet);
			bool atLeastMinRaise = false;
			if(newBet - largestBet >= minimumRaise)
			{
				atLeastMinRaise = true;
				minimumRaise = newBet - largestBet;
			}
			largestBet = newBet;
			ReopenActionForOtherPlayers(currentActionPokerPlayer, atLeastMinRaise);
			currentActionPokerPlayer.DisplayActionTaken(currentActionCanBet ? (byte)3 : (byte)4);
			MoveAction();
		}
		else
		{
			Debug.LogWarning("Received bet/raise input from player who does not match the playerRef of the currentAction player");
		}
	}
	
	public void ReopenActionForOtherPlayers(PokerPlayer ignorePlayer, bool reopenRaising)
	{
		ResetPlayerActionOpen(reopenRaising);
		ignorePlayer.actionOpen = false;
		ignorePlayer.raiseOpen = false;
	}

	public void PlayerHasActed(PokerPlayer playerWhoActed)
	{
		currentActionTimer = TickTimer.None;
		playerWhoActed.myTurn = false;
		playerWhoActed.currentTurnIndicatorTimer = TickTimer.None;
		playerWhoActed.currentTurnIndicatorVisible = false;
		playerWhoActed.actionOpen = false;
		playerWhoActed.timeRemaining = 0;
	}
	
	public void MoveAction()
	{
		List<PokerPlayer> playersWithCards = GetAllPokerPlayersWithCards();
		if(playersWithCards.Count == 1)
		{
			int delayIterations = CollectBets();
			pokerPlayerToGivePotTo = playersWithCards[0].networkObject;
			givePotToPlayerTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, (delayIterations + 1) * inGameAnimationTime);
			return;
		}
		PokerPlayer currentActionPokerPlayer = GetPlayerToTheLeft(currentAction.GetComponent<PokerPlayer>(), true, true, true, true);
		if(currentActionPokerPlayer == null)
		{
			AdvanceStreet();
		}
		else
		{
			currentAction = currentActionPokerPlayer.networkObject;
			for(int i = 0; i < playersWithCards.Count; i++)
			{
				if((playersWithCards[i].chips > 0 && playersWithCards[i].networkObject != currentAction) || playersWithCards[i].GetCurrentBet() > currentAction.GetComponent<PokerPlayer>().GetCurrentBet())
				{
					CurrentActionUpdated();
					return;
				}
			}
			AdvanceStreet();
		}
	}
	
	public void AdvanceStreet()
	{
		int delayIterations = CollectBets();
		List<PokerPlayer> playersWithChipsAndCards = GetAllPlayersWithChipsAndCards();
		if(playersWithChipsAndCards.Count <= 1)
		{
			if(!handsFaceUp)
			{
				RevealPlayerCards();
			}
			else
			{
				// UpdateWordRevealLabels();
			}
		}
		dealNextStreetDelayIterations = delayIterations;
		dealNextStreetTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime * delayIterations);
	}
	
	public void DealNextStreet(int delayIterations)
	{
		switch(currentStreet)
		{
			case 1:
				DealFlop();
				break;
			case 2:
				DealSingleCommunityCard(true);
				break;
			case 3:
				DealSingleCommunityCard(false);
				break;
			case 4:
				int delayIterationsShowdown = CollectBets();
				startShowdownTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime * delayIterationsShowdown);
				return;
		}
		currentStreet++;
		/* List<PokerPlayer> playersWithCards = GetAllPokerPlayersWithCards();
		for(int i = 0; i < playersWithCards.Count; i++)
		{
			UpdatePlayerBestWord(playersWithCards[i]);
		} */
		List<PokerPlayer> playersWhoHadChips = GetAllPokerPlayersWhoHadChipsAtHandStart();
		for(int i = 0; i < playersWhoHadChips.Count; i++)
		{
			UpdatePlayerBestWord(playersWhoHadChips[i]);
		}
		startNextStreetTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime * ((float)delayIterations + 1.5f));
	}
	
	public void DealFlop()
	{
		for(int i = 0; i < 3; i++)
		{
			NetworkObject newCardNO = LocalInterface.instance.networkRunner.Spawn(LocalInterface.instance.cardPrefab, new Vector3(-0.96f, 0.22f, 0), Quaternion.identity);
			Card newCard = newCardNO.GetComponent<Card>();
			newCard.cardString = ConvertByteToLetter(gameDeck[currentHandRandomOrder[cardsDealtThisHand]]);
			newCard.cardValue = letterValues[ConvertLetterToNumber(newCard.cardString)];
			newCard.cardIsFaceUp = true;
			cardsDealtThisHand++;
			communityCards.Set(i, newCardNO);
			communityLetters.Set(i, Convert.ToByte(ConvertLetterToNumber(newCard.cardString)));
			if(i > 0)
			{
				newCard.MoveToPosition(new Vector3(-0.96f + 0.48f * i, 0.22f, 0));
			}
		}
	}
	
	public void DealSingleCommunityCard(bool turn)
	{
		NetworkObject newCardNO = LocalInterface.instance.networkRunner.Spawn(LocalInterface.instance.cardPrefab, new Vector3(-0.96f, 0.22f, 0), Quaternion.identity);
		Card newCard = newCardNO.GetComponent<Card>();
		newCard.cardString = ConvertByteToLetter(gameDeck[currentHandRandomOrder[cardsDealtThisHand]]);
		newCard.cardValue = letterValues[ConvertLetterToNumber(newCard.cardString)];
		newCard.cardIsFaceUp = true;
		cardsDealtThisHand++;
		communityCards.Set(turn ? 3 : 4, newCardNO);
		newCard.MoveToPosition(new Vector3(0.96f - (turn ? 0.48f : 0), 0.22f, 0));
		// communityLetters = communityLetters + newCard.cardString;
		communityLetters.Set(turn ? 3 : 4, Convert.ToByte(ConvertLetterToNumber(newCard.cardString)));
	}
	
	public string GetCommunityLettersAsString()
	{
		string communityLettersString = string.Empty;
		if(currentStreet <= 1)
		{
			return communityLettersString;
		}
		else
		{
			communityLettersString = $"{ConvertByteToLetter(communityLetters[0])}{ConvertByteToLetter(communityLetters[1])}{ConvertByteToLetter(communityLetters[2])}";
			if(currentStreet > 2)
			{
				communityLettersString += $"{ConvertByteToLetter(communityLetters[3])}";
				if(currentStreet > 3)
				{
					communityLettersString += $"{ConvertByteToLetter(communityLetters[4])}";
				}
			}
		}
		return communityLettersString;
	}
	
	public void UpdatePlayerBestWord(PokerPlayer pokerPlayer)
	{
		Debug.Log($"Updating best word for {pokerPlayer.playerName}");
		string allLetters = pokerPlayer.GetLetters() + GetCommunityLettersAsString();
		allLetters = allLetters.Trim();
		string bestWord = EvaluateHand.instance.FindBestWord(allLetters, dictionaryBeingUsed);
		pokerPlayer.currentBestWord = "abcdefg";
		pokerPlayer.currentBestWord = bestWord;
		int bestWordPoints = EvaluateHand.instance.CalculateWordScore(bestWord);
		pokerPlayer.currentBestWordPoints = bestWordPoints;
	}
	
	public void StartShowdown()
	{
		if(!handsFaceUp)
		{
			RevealPlayerCards();
		}
/* 		else
		{
			Showdown();
		} */
		showdownTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime * 2f);
	}
	
	public void Showdown()
	{
		int delayIterations = 0;
		for(int i = 0; i < numberOfPotsThisHand; i++)
		{
			ChipPile communityPot = communityPots[i].GetComponent<ChipPile>();
			RemovePlayersWithoutCardsFromPot(communityPot);
			List<PokerPlayer> playersInCommunityPot = new List<PokerPlayer>();
			for(int j = 0; j < 6; j++)
			{
				if(communityPot.playersInPot[j] != null)
				{
					playersInCommunityPot.Add(communityPot.playersInPot[j].GetComponent<PokerPlayer>());
				}
			}
			playersInCommunityPot.Sort((x,y) => x.currentBestWordPoints - y.currentBestWordPoints);
			playersInCommunityPot.Reverse();
			List<PokerPlayer> playersWhoWonThisPot = new List<PokerPlayer>();
			int bestScore = playersInCommunityPot[0].currentBestWordPoints;
			for(int j = 0; j < playersInCommunityPot.Count; j++)
			{
				if(playersInCommunityPot[j].currentBestWordPoints == bestScore)
				{
					playersWhoWonThisPot.Add(playersInCommunityPot[j]);
				}
				else
				{
					break;
				}
			}
			if(playersWhoWonThisPot.Count > 1)
			{
				int totalPot = communityPot.chipsInPile;
				int equalSplit = communityPot.chipsInPile / playersWhoWonThisPot.Count;
				for(int j = 0; j < playersWhoWonThisPot.Count; j++)
				{
					playersWhoWonThisPot[j].earnedFromSplitPot = equalSplit;
				}
				int leftOver = totalPot - equalSplit * playersWhoWonThisPot.Count;
				if(leftOver > 0)
				{
					for(int j = 0; j < playersWhoWonThisPot.Count; j++)
					{
						playersWhoWonThisPot[j].positionsLeftOfDealer = playersWhoWonThisPot[j].seatPosition - currentDealer.GetComponent<PokerPlayer>().seatPosition;
						if(playersWhoWonThisPot[j].positionsLeftOfDealer <= 0)
						{
							playersWhoWonThisPot[j].positionsLeftOfDealer += 6;
						}
					}
					playersWhoWonThisPot.Sort((x,y) => x.positionsLeftOfDealer - y.positionsLeftOfDealer);
					int index = 0;
					while(leftOver > 0)
					{
						playersWhoWonThisPot[index].earnedFromSplitPot++;
						leftOver--;
						if(index >= playersWhoWonThisPot.Count)
						{
							index = 0;
						}
					}
				}
				for(int j = 0; j < playersWhoWonThisPot.Count; j++)
				{
					NetworkObject newChipPileNO = LocalInterface.instance.networkRunner.Spawn(PokerHelper.instance.chipPilePrefab, communityPot.transform.position, Quaternion.identity);
					ChipPile newChipPile = newChipPileNO.GetComponent<ChipPile>();
					newChipPile.chipsInPile = playersWhoWonThisPot[j].earnedFromSplitPot;
					newChipPile.MoveToPosition(playersWhoWonThisPot[j].transform.position, playersWhoWonThisPot[j].networkObject, delayIterations * inGameAnimationTime);
					communityPot.chipsInPile -= playersWhoWonThisPot[j].earnedFromSplitPot;
				}
			}
			else
			{
				communityPot.MoveToPosition(playersWhoWonThisPot[0].transform.position, playersWhoWonThisPot[0].networkObject, delayIterations * inGameAnimationTime);
			}
			delayIterations++;
		}
		endHandTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, ((float)delayIterations + 1.5f) * inGameAnimationTime);
	}
	
/* 	
	public void ReorganizePlayersInGame()
	{
		for(int i = 0; i < numberOfPlayersInGame; i++)
		{
			if(playersInGame[i] == null)
			{
				int nextIndex = i + 1;
				while(nextIndex < 6)
				{
					if(playersInGame[nextIndex] != null)
					{
						playersInGame.Set(i, playersInGame[nextIndex]);
						playersInGame.Set(nextIndex, null);
					}
				}
			}
		}
	} */
	
	public void RemovePlayersWithoutCardsFromPot(ChipPile communityPot)
	{
		bool needToReorganize = false;
		List<NetworkObject> cleanedPlayersInPot = new List<NetworkObject>();
		for(int i = 0; i < 6; i++)
		{
			if(communityPot.playersInPot[i] != null)
			{
				if(!communityPot.playersInPot[i].GetComponent<PokerPlayer>().hasCards)
				{
					communityPot.playersInPot.Set(i, null);
					needToReorganize = true;
				}
				else
				{
					cleanedPlayersInPot.Add(communityPot.playersInPot[i]);
				}
			}
		}
		if(needToReorganize)
		{
			for(int i = 0; i < 6; i++)
			{
				communityPot.playersInPot.Set(i, null);
			}
			for(int i = 0; i < cleanedPlayersInPot.Count; i++)
			{
				communityPot.playersInPot.Set(i, cleanedPlayersInPot[i]);
			}
		}
	}
	
	public void StartNextStreet()
	{
		minimumRaise = gameBlindStructure[blindLevelThisHand].y;
		largestBet = 0;
		List<PokerPlayer> playersWithChipsAndCards = GetAllPlayersWithChipsAndCards();
		if(playersWithChipsAndCards.Count > 1)
		{
			ResetPlayerActionOpen(true);
			currentAction = GetPlayerToTheLeft(currentDealer.GetComponent<PokerPlayer>(), true, true, true, true).networkObject;
			CurrentActionUpdated();
		}
		else
		{
			AdvanceStreet();
		}
	}
	
	public void RevealPlayerCards()
	{
		handsFaceUp = true;
		List<PokerPlayer> playersWithCards = GetAllPokerPlayersWithCards();
		for(int i = 0; i < playersWithCards.Count; i++)
		{
			playersWithCards[i].cardsRevealed = true;
		}
	}
	
/* 	public void UpdateWordRevealLabels()
	{
		List<PokerPlayer> playersWithCards = GetAllPokerPlayersWithCards();
		for(int i = 0; i < playersWithCards.Count; i++)
		{
			playersWithCards[i].UpdateWordRevealLabel();
		}
	} */
	
	public void GivePotToPlayer(NetworkObject pokerPlayerNO)
	{
		PokerPlayer winningPlayer = pokerPlayerNO.GetComponent<PokerPlayer>();
		communityPots[0].GetComponent<ChipPile>().MoveToPosition(winningPlayer.transform.position, pokerPlayerNO);
		endHandTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime);
	}
	
	public void EndHand()
	{
		totalCurrentPot = 0;
		List<PokerPlayer> pokerPlayers = GetAllPokerPlayers();
		for(int i = 0; i < pokerPlayers.Count; i++)
		{
			if(pokerPlayers[i].hasCards)
			{
				pokerPlayers[i].PlayerFolds();
			}
			pokerPlayers[i].DespawnCards();
			pokerPlayers[i].hadChipsAtHandStart = false;
			pokerPlayers[i].actionOpen = false;
			pokerPlayers[i].raiseOpen = false;

		}
		for(int i = 0; i < 5; i++)
		{
			if(communityCards[i] != null)
			{
				LocalInterface.instance.networkRunner.Despawn(communityCards[i]);
			}
			else
			{
				break;
			}
		}
		handsPlayed++;
		startNextHandTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime / 2);
	}
	
	public int CollectBets()
	{
		List<PokerPlayer> playersWithBets = GetAllPokerPlayersWithBets();
		playersWithBets.Sort((x,y) => x.GetCurrentBet() - y.GetCurrentBet());
		if(numberOfPotsThisHand == 0)
		{
			NetworkObject newPotNO = LocalInterface.instance.networkRunner.Spawn(PokerHelper.instance.chipPilePrefab, PokerHelper.instance.communityPotLocations[0], Quaternion.identity);
			communityPots.Set(0, newPotNO);
			newPotNO.name = $"pot {numberOfPotsThisHand.ToString()}";
			ChipPile newPot = newPotNO.GetComponent<ChipPile>();
			for(int i = 0; i < playersWithBets.Count; i++)
			{
				newPot.playersInPot.Set(newPot.GetNumberOfPlayersInPot(), playersWithBets[i].networkObject);
			}
			numberOfPotsThisHand++;
		}
		ChipPile currentPot = communityPots[numberOfPotsThisHand - 1].GetComponent<ChipPile>();
		// int delayIterations = 0;
		// Debug.Log($"<color=green>[Silver]</color> Starting CollectBets, playersWithBets.Count = {playersWithBets.Count}, numberOfPotsThisHand = {numberOfPotsThisHand}");
		for(int i = 0; i < playersWithBets.Count; i++)
		{
			int smallestBetFromSomeoneWithCards = int.MaxValue;
			for(int j = 0; j < playersWithBets.Count; j++)
			{
				if(playersWithBets[j].GetCurrentBet() > 0 && playersWithBets[j].hasCards)
				{
					if(playersWithBets[j].GetCurrentBet() < smallestBetFromSomeoneWithCards)
					{
						smallestBetFromSomeoneWithCards = playersWithBets[j].GetCurrentBet();
					}
				}
			}
			for(int j = 0; j < playersWithBets.Count; j++)
			{
				if(playersWithBets[j].GetCurrentBet() > 0)
				{
					// Debug.Log($"<color=green>[Silver]</color> i = {i} j = {j} playersWithBets[j].GetCurrentBet() = {playersWithBets[j].GetCurrentBet()}, delayIterations = {delayIterations}");
					playersWithBets[j].MoveBet(currentPot, Mathf.Min(playersWithBets[j].GetCurrentBet(), smallestBetFromSomeoneWithCards));
				}
			}
			List<PokerPlayer> playersWithCardsButNoBet = new List<PokerPlayer>();
			List<PokerPlayer> playersWithCardsAndABet = new List<PokerPlayer>();
			for(int j = 0; j < playersWithBets.Count; j++)
			{
				if(playersWithBets[j].hasCards)
				{
					if(playersWithBets[j].GetCurrentBet() == 0)
					{
						playersWithCardsButNoBet.Add(playersWithBets[j]);
					}
					else
					{
						playersWithCardsAndABet.Add(playersWithBets[j]);
					}
				}
			}
			if(playersWithCardsAndABet.Count > 0)
			{
				if(playersWithCardsAndABet.Count == 1)
				{
					// playersWithCardsAndABet[0].ReturnBet(delayIterations * inGameAnimationTime);
					totalCurrentPot -= playersWithCardsAndABet[0].GetCurrentBet();
					playersWithCardsAndABet[0].ReturnBet();
					break;
				}
				else
				{
					if(playersWithCardsButNoBet.Count > 0)
					{
						NetworkObject newPotNO = LocalInterface.instance.networkRunner.Spawn(PokerHelper.instance.chipPilePrefab, PokerHelper.instance.communityPotLocations[numberOfPotsThisHand], Quaternion.identity);
						communityPots.Set(numberOfPotsThisHand, newPotNO);
						newPotNO.name = $"pot {numberOfPotsThisHand.ToString()}";
						ChipPile newPot = newPotNO.GetComponent<ChipPile>();
						currentPot = newPot;
						for(int k = 0; k < playersWithCardsAndABet.Count; k++)
						{
							newPot.playersInPot.Set(newPot.GetNumberOfPlayersInPot(), playersWithCardsAndABet[k].networkObject);
							// newPot.playersInPot.Set(playersWithCardsAndABet[k].networkObject);
						}
						numberOfPotsThisHand++;
					}
				}
			}
			else
			{
				if(playersWithCardsButNoBet.Count > 2 && currentStreet < 4)
				{
					List<PokerPlayer> playersWithCardsAndChips = new List<PokerPlayer>();
					List<PokerPlayer> playersWhoAreAllIn = new List<PokerPlayer>();
					for(int j = 0; j < playersWithCardsButNoBet.Count; j++)
					{
						if(playersWithCardsButNoBet[j].chips > 0)
						{
							playersWithCardsAndChips.Add(playersWithCardsButNoBet[j]);
						}
						else
						{
							playersWhoAreAllIn.Add(playersWithCardsButNoBet[j]);
						}
					}
					if(playersWithCardsAndChips.Count >= 2 && playersWhoAreAllIn.Count >= 1)
					{
						if(communityPots[numberOfPotsThisHand - 1].GetComponent<ChipPile>().GetNumberOfPlayersInPot() > playersWithCardsAndChips.Count)
						{
							NetworkObject newPotNO = LocalInterface.instance.networkRunner.Spawn(PokerHelper.instance.chipPilePrefab, PokerHelper.instance.communityPotLocations[numberOfPotsThisHand], Quaternion.identity);
							communityPots.Set(numberOfPotsThisHand, newPotNO);
							newPotNO.name = $"pot {numberOfPotsThisHand.ToString()}";
							ChipPile newPot = newPotNO.GetComponent<ChipPile>();
							currentPot = newPot;
							for(int k = 0; k < playersWithCardsAndChips.Count; k++)
							{
								newPot.playersInPot.Set(newPot.GetNumberOfPlayersInPot(), playersWithCardsAndChips[k].networkObject);
								// newPot.playersInPot.Add(playersWithCardsAndChips[k].networkObject);
							}
							numberOfPotsThisHand++;
						}
					}
				}
				break;
			}
			List<PokerPlayer> playersWithBets2 = GetAllPokerPlayersWithBets();
			if(playersWithBets2.Count > 0)
			{
				collectBetsTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime);
				List<int> uniqueBets = new List<int>();
				for(int j = 0; j < playersWithBets2.Count; j++)
				{
					if(!uniqueBets.Contains(playersWithBets2[j].GetCurrentBet()))
					{
						uniqueBets.Add(playersWithBets2[j].GetCurrentBet());
					}
				}
				// Debug.Log($"returning {uniqueBets.Count} uniqueBets");
				return uniqueBets.Count + 0;
			}
		}
		// resetPlayerCollectionDataTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, inGameAnimationTime * delayIterations);
		return 0;
	}
}
