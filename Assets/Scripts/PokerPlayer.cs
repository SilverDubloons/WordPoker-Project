using UnityEngine;
using Fusion;
using TMPro;
using System;

public class PokerPlayer : NetworkBehaviour
{
	[Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public string playerName{get;set;}
	public Label playerNameLabel;
	[Networked, OnChangedRender(nameof(OnPlayerChipsChanged))]
	public int chips{get;set;}
	public Label playerChipsLabel;
	[Networked, OnChangedRender(nameof(OnPlayerConnectionStatusChanged))]
	public bool disconnected{get;set;}
	[Networked, OnChangedRender(nameof(OnColorChanged))]
	public Color playerColor {get;set;}
	public SpriteRenderer backdropSpriteRenderer;
	[Networked] public int originalConnectionOrder {get;set;}
	[Networked] public int inGameOrder {get;set;}
	public NetworkObject networkObject;
	[Networked] public bool sittingOut{get;set;}
	[Networked] public NetworkObject currentCard1{get;set;}
	[Networked] public NetworkObject currentCard2{get;set;}
	[Networked, OnChangedRender(nameof(OnHasCardsChanged))]
	public bool hasCards{get;set;}
	[Networked] public PlayerRef owningPlayerRef{get;set;}
	public NetworkTransform networkTransform;
	[Networked] TickTimer moveToSeatTimer {get;set;}
	[Networked] public TickTimer currentTurnIndicatorTimer {get;set;}
	[Networked] public TickTimer actionLabelVisibleTimer {get;set;}
	[Networked] public TickTimer moveBetDelayTimer {get;set;}
	[Networked] public TickTimer returnBetDelayTimer {get;set;}
	[Networked] Vector3 originalLobbyPosition {get;set;}
	[Networked] Vector3 seatPositionV3 {get;set;}
	[Networked, OnChangedRender(nameof(OnSeatPositionChanged))]
	public int seatPosition {get;set;}
	[Networked] public int handsSinceBeingBigBlind {get;set;}
	[Networked] public bool actionOpen {get;set;}
	[Networked] public bool raiseOpen {get;set;}
	[Networked] public bool hadChipsAtHandStart {get;set;}
	[Networked] public bool myTurn {get;set;}
	[Networked] public NetworkObject currentBetChipPile {get;set;}
	[Networked] public bool betting {get;set;}
	[Networked, OnChangedRender(nameof(OnCurrentTurnIndicatorVisibleChanged))]
	public bool currentTurnIndicatorVisible {get;set;}
	public GameObject currentTurnIndicator;
	[Networked, OnChangedRender(nameof(OnActionLabelVisibleChanged))]
	public bool actionLabelVisible {get;set;}
	public GameObject playerDisplay;
	public Label wordRevealLabel;
	public Label actionLabel;
	[Networked] public byte actionTakenByte {get;set;} // 0 = fold, 1 = check, 2 = call, 3 = bet, 4 = raise
	[Networked] public NetworkObject movingToPot {get;set;}
	[Networked] public int movingQuantity {get;set;}
	[Networked, OnChangedRender(nameof(OnCardsRevealedChanged))]
	public bool cardsRevealed {get;set;}
	[Networked, OnChangedRender (nameof(OnCurrentBestWordChanged))]
	public string currentBestWord {get;set;}
	[Networked, OnChangedRender (nameof(OnCurrentBestWordPointsChanged))]
	public int currentBestWordPoints {get;set;}
	[Networked] public int earnedFromSplitPot {get;set;}
	[Networked] public int positionsLeftOfDealer {get;set;}
	[Networked, OnChangedRender(nameof(OnHasBeenEliminatedChanged))]
	public bool hasBeenEliminated {get;set;}
	[Networked] public int chipsReservedForBetCollection {get;set;}
	public Transform playerTimerTransform;
	public SpriteRenderer timerSpriteRenderer;
	[Networked, OnChangedRender(nameof(OnTimerDisplayColorChanged))]
	public Color timerDisplayColor {get;set;}
    [Networked, OnChangedRender(nameof(OnTimeRemainingChanged))]
	public float timeRemaining {get;set;}
	// [Networked] public bool foldedThisTurn {get;set;}
	
	public void UpdateBet(int newBet)
	{
		if(currentBetChipPile == null)
		{
			if(newBet == 0)
			{
				return;
			}
			NetworkObject newPileNO = LocalInterface.instance.networkRunner.Spawn(PokerHelper.instance.chipPilePrefab, PokerHelper.instance.bettingPositions[seatPosition], Quaternion.identity);
			newPileNO.name = $"{playerName} bet ChipPile";
			currentBetChipPile = newPileNO;
			
			// ChipPile newChipPile = newPileNO.GetComponent<ChipPile>();
			
		}
		currentBetChipPile.GetComponent<ChipPile>().chipsInPile = newBet;
	}
	
	public string GetLetters()
	{
		string holeCardLetters = $"{currentCard1.GetComponent<Card>().cardString}{currentCard2.GetComponent<Card>().cardString}";
		return holeCardLetters;
	}
	
	public int GetCurrentBet()
	{
		if(currentBetChipPile == null)
		{
			return 0;
		}
		else
		{
			return currentBetChipPile.GetComponent<ChipPile>().chipsInPile - chipsReservedForBetCollection;
		}
	}
	
	public void MoveToSeat(Vector3 destination, int position)
	{
		originalLobbyPosition = transform.position;
		seatPositionV3 = destination;
		seatPosition = position;
		moveToSeatTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, LocalInterface.instance.gameManager.inGameAnimationTime);
	}
	
	public void MoveBet(ChipPile destPile, int quantity, float delay = 0)
	{
		// Debug.Log($"<color=green>[Silver]</color> {this.name} MoveBet destPile.name = {destPile.name}, quantity = {quantity}, delay = {delay.ToString()} chipsReservedForBetCollection = {chipsReservedForBetCollection} GetCurrentBet() = {GetCurrentBet()} movingQuantity = {movingQuantity}");
		if(delay > 0.0001f)
		{
			chipsReservedForBetCollection += quantity;
			movingToPot = destPile.networkObject;
			moveBetDelayTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, delay);
			movingQuantity = quantity;
		}
		else
		{
			bool nullCurrentBet = false;
			if(quantity == GetCurrentBet())
			{
				nullCurrentBet = true;
			}
			currentBetChipPile.GetComponent<ChipPile>().MovePileToAnother(destPile.networkObject, quantity);
			if(nullCurrentBet)
			{
				currentBetChipPile = null;
			}
		}
	}
	
	public void ReturnBet(int amountToReturn = 0, float delay = 0)
	{
		if(delay > 0.0001f)
		{
			returnBetDelayTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, delay);
		}
		else
		{
			currentBetChipPile.GetComponent<ChipPile>().MoveToPosition(transform.position, networkObject);
		}
	}
	
	public override void FixedUpdateNetwork()
	{
		if(moveToSeatTimer.IsRunning)
		{
			float remainingTime = (float)moveToSeatTimer.RemainingTime(LocalInterface.instance.networkRunner);
			float normalizedValue = (LocalInterface.instance.gameManager.inGameAnimationTime - remainingTime) / LocalInterface.instance.gameManager.inGameAnimationTime;
			transform.position = Vector3.Lerp(originalLobbyPosition, seatPositionV3, LocalAnimations.instance.animationCurve.Evaluate(normalizedValue));
			if(moveToSeatTimer.Expired(LocalInterface.instance.networkRunner))
			{
				transform.position = seatPositionV3;
				moveToSeatTimer = TickTimer.None;
			}
		}
		if(myTurn)
		{
			if(currentTurnIndicatorTimer.Expired(LocalInterface.instance.networkRunner))
			{
				currentTurnIndicatorVisible = !currentTurnIndicatorVisible;
				currentTurnIndicatorTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, PokerHelper.instance.timeBetweenCurrentTurnIndicatorBlinks);
			}
		}
		if(actionLabelVisibleTimer.Expired(LocalInterface.instance.networkRunner))
		{
			actionLabelVisible = false;
			actionLabelVisibleTimer = TickTimer.None;
		}
		if(moveBetDelayTimer.Expired(LocalInterface.instance.networkRunner))
		{
			MoveBet(movingToPot.GetComponent<ChipPile>(), movingQuantity);
			moveBetDelayTimer = TickTimer.None;
		}
		if(returnBetDelayTimer.Expired(LocalInterface.instance.networkRunner))
		{
			ReturnBet();
			returnBetDelayTimer = TickTimer.None;
		}
	}
	
	public override void Spawned()
	{
		base.Spawned();
		OnPlayerNameChanged();
		OnColorChanged();
		OnPlayerChipsChanged();
		OnCurrentTurnIndicatorVisibleChanged();
		OnActionLabelVisibleChanged();
		OnSeatPositionChanged();
		OnCardsRevealedChanged();
		OnHasBeenEliminatedChanged();
		OnTimerDisplayColorChanged();
		OnTimeRemainingChanged();
		// Debug.Log($"{playerName} Spawned");
	}
	
	public void DisplayActionTaken(byte actionByte)
	{
		actionLabelVisibleTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, PokerHelper.instance.timeToDisplayActionTaken);
		actionTakenByte = actionByte;
		actionLabelVisible = true;
	}
	
	public void OnCurrentBestWordChanged()
	{
		UpdateWordRevealLabel();
	}
	
	public void OnCurrentBestWordPointsChanged()
	{
		UpdateWordRevealLabel();
	}
	
	public void OnHasBeenEliminatedChanged()
	{
		if(hasBeenEliminated)
		{
			playerChipsLabel.ChangeText("Eliminated");
			backdropSpriteRenderer.color = LocalInterface.instance.eliminatedColor;
		}
		else
		{
			OnColorChanged();
		}
	}
	
	public void OnSeatPositionChanged()
	{
		wordRevealLabel.transform.localPosition = PokerHelper.instance.revealLabelLocations[seatPosition];
	}
	
	public void OnCardsRevealedChanged()
	{
		wordRevealLabel.gameObject.SetActive(cardsRevealed);
		if(currentCard1 != null)
		{
			Card card1 = currentCard1.GetComponent<Card>();
			card1.cardValueGO.SetActive(cardsRevealed);
			card1.cardStringGO.SetActive(cardsRevealed);
		}
		if(currentCard2 != null)
		{
			Card card2 = currentCard2.GetComponent<Card>();
			card2.cardValueGO.SetActive(cardsRevealed);
			card2.cardStringGO.SetActive(cardsRevealed);
		}
		if(cardsRevealed)
		{
			UpdateWordRevealLabel();
		}
	}
	
	public void UpdateWordRevealLabel()
	{
		if(currentBestWord == string.Empty)
		{
			wordRevealLabel.ChangeText("No Word\n0");
			if(owningPlayerRef == LocalInterface.instance.fusionInterface.localPlayerRef)
			{
				// if(currentBestWord == "zzzzzzz")
				if(!hasCards)
				{
					PokerHelper.instance.localBestWordDisplayGO.SetActive(false);
				}
				else
				{
					PokerHelper.instance.localBestWordDisplayGO.SetActive(true);
					PokerHelper.instance.localBestWordLabel.ChangeText("No Word\n0");
				}
			}
		}
		else
		{
			wordRevealLabel.ChangeText($"{currentBestWord.ToUpper()}\n{currentBestWordPoints}");
			if(owningPlayerRef == LocalInterface.instance.fusionInterface.localPlayerRef)
			{
				PokerHelper.instance.localBestWordDisplayGO.SetActive(true);
				string localBestWordText = $"Best Word\n{currentBestWord.ToUpper()}\n";
				int wordLengthBonus = EvaluateHand.instance.wordLengthBonuses[currentBestWord.Length - 1];
				if(wordLengthBonus > 0)
				{
				localBestWordText += $"{currentBestWordPoints - wordLengthBonus} Points\n+{wordLengthBonus} ({currentBestWord.Length})\n{currentBestWordPoints} Total";
				}
				else
				{
					localBestWordText += $"{currentBestWordPoints} Points";
				}
				PokerHelper.instance.localBestWordLabel.ChangeText(localBestWordText);
				// if(currentBestWord == "zzzzzzz")
				if(!hasCards && !currentCard1.gameObject.activeSelf && !currentCard2.gameObject.activeSelf)
				{
					PokerHelper.instance.localBestWordDisplayGO.SetActive(false);
				}
			}
		}
	}
	
	public void OnActionLabelVisibleChanged()
	{
		if(actionLabelVisible)
		{
			playerDisplay.SetActive(false);
			actionLabel.ChangeText(PokerHelper.instance.ConvertByteToActionString(actionTakenByte));
		}
		else
		{
			actionLabel.ChangeText(string.Empty);
			playerDisplay.SetActive(true);
		}
	}
	
	public void OnCurrentTurnIndicatorVisibleChanged()
	{
		currentTurnIndicator.SetActive(currentTurnIndicatorVisible);
	}

	public void OnPlayerNameChanged()
	{
		gameObject.name = playerName;
		playerNameLabel.ChangeText(playerName);
	}
	
	public void OnPlayerChipsChanged()
	{
		if(chips > 0)
		{
			playerChipsLabel.ChangeText(chips.ToString());
		}
		else
		{
			playerChipsLabel.ChangeText("All In");
		}
	}
	
	public void OnPlayerConnectionStatusChanged()
	{
		if(disconnected)
		{
			playerColor = LocalInterface.instance.disconnectedColor;
		}
		else
		{
			playerColor = LocalInterface.instance.playerColors[originalConnectionOrder];
		}
	}
	
	public void OnColorChanged()
	{
		backdropSpriteRenderer.color = playerColor;
	}
	
	void OnMouseEnter()
	{
		if(!hasCards && LocalInterface.instance.fusionInterface.HasGameStarted() && owningPlayerRef == LocalInterface.instance.fusionInterface.localPlayerRef)
		{
			if(currentCard1 != null)
			{
				currentCard1.gameObject.SetActive(true);
			}
			if(currentCard2 != null)
			{
				currentCard2.gameObject.SetActive(true);
			}
			PokerHelper.instance.localBestWordDisplayGO.SetActive(true);
		}
	}
	
	void OnMouseExit()
	{
		if(!hasCards && LocalInterface.instance.fusionInterface.HasGameStarted() && owningPlayerRef == LocalInterface.instance.fusionInterface.localPlayerRef)
		{
			if(currentCard1 != null)
			{
				currentCard1.gameObject.SetActive(false);
			}
			if(currentCard2 != null)
			{
				currentCard2.gameObject.SetActive(false);
			}
			PokerHelper.instance.localBestWordDisplayGO.SetActive(false);
		}
	}
	
	public void OnMouseDown()
	{
		Debug.Log($"{playerName} was clicked. disconnected = {disconnected.ToString()} LocalInterface.instance.fusionInterface.reconnecting = {LocalInterface.instance.fusionInterface.reconnecting.ToString()}");
		if(disconnected && LocalInterface.instance.fusionInterface.reconnecting)
		{
			LocalInterface.instance.fusionInterface.reconnecting = false;
			PokerHelper.instance.sittingOutToggle.isOn = false;
			LocalInterface.instance.gameManager.OnBlindLevelChange();
			ReconnectRequestRpc(LocalInterface.instance.fusionInterface.localPlayerRef, LocalInterface.instance.localPlayerName);
		}
	}
	
	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void ReconnectRequestRpc(PlayerRef requestingPlayer, string reconnectionName)
	{
		disconnected = false;
		sittingOut = false;
		playerName = reconnectionName;
		owningPlayerRef = requestingPlayer;
		if(hasCards)
		{
			currentCard1.GetComponent<Card>().owningPlayer = requestingPlayer;
			currentCard2.GetComponent<Card>().owningPlayer = requestingPlayer;
		}
	}
	
	public void DespawnCards()
	{
		if(currentCard1 != null)
		{
			LocalInterface.instance.networkRunner.Despawn(currentCard1);
		}
		if(currentCard2 != null)
		{
			LocalInterface.instance.networkRunner.Despawn(currentCard2);
		}
	}
	
	public void PlayerFolds()
	{
		// LocalInterface.instance.networkRunner.Despawn(currentCard1);
		// LocalInterface.instance.networkRunner.Despawn(currentCard2);
		currentCard1.GetComponent<Card>().cardColor = new Color(1f,1f,1f,2f/3f);
		currentCard2.GetComponent<Card>().cardColor = new Color(1f,1f,1f,2f/3f);
		cardsRevealed = false;
		hasCards = false;
		// foldedThisTurn = true;
		// currentBestWord = "zzzzzzz";
		// SpawnLocalGhostCardsRpc(owningPlayerRef);
	}
	
	public void OnHasCardsChanged()
	{
		if(!hasCards)
		{
			if(currentCard1 != null)
			{
				currentCard1.gameObject.SetActive(false);
			}
			if(currentCard2 != null)
			{
				currentCard2.gameObject.SetActive(false);
			}
			if(owningPlayerRef == LocalInterface.instance.fusionInterface.localPlayerRef)
			{
				PokerHelper.instance.localBestWordBackdropImage.color = new Color(1f,0f,0f,2f/3f);
				PokerHelper.instance.localBestWordDisplayGO.SetActive(false);
			}
		}
		else
		{
			if(owningPlayerRef == LocalInterface.instance.fusionInterface.localPlayerRef)
			{
				PokerHelper.instance.localBestWordBackdropImage.color = new Color(1f,0f,0f,1f);
			}
		}
	}
	
	
	
	
	
	
	public void OnTimerDisplayColorChanged()
	{
		timerSpriteRenderer.color = timerDisplayColor;
	}
	
	public void OnTimeRemainingChanged()
	{
		if(LocalInterface.instance.gameManager != null)
		{
			float normalizedTimeRemaining = Mathf.Round((timeRemaining / LocalInterface.instance.gameManager.maximumDecisionTime) * 50f) / 50f;
			//Debug.Log($"normalizedTimeRemaining = {normalizedTimeRemaining} Mathf.Abs(normalizedTimeRemaining - playerTimerTransform.localScale.y = {Mathf.Abs(normalizedTimeRemaining - playerTimerTransform.localScale.y)}");
			if(Mathf.Abs(normalizedTimeRemaining - playerTimerTransform.localScale.y) > 0.0001f)
			{
				playerTimerTransform.localScale = new Vector3(normalizedTimeRemaining, 0.02f, 1);
			}
		}
		else
		{
			playerTimerTransform.localScale = new Vector3(0, 0.02f, 1);
		}
	}
}
