using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System;

public class ChipPile : NetworkBehaviour
{
    public Label betLabel;
	public Transform chipParent;
	[Networked, OnChangedRender(nameof(ChipsInPileUpdated))]
	public int chipsInPile {get;set;}
	[Networked, Capacity(6)]
	public NetworkArray<NetworkObject> playersInPot {get;}
	[Networked] TickTimer moveToPositionTimer {get;set;}
	[Networked] TickTimer moveToAnotherPileTimer {get;set;}
	[Networked] TickTimer moveToPositionDelayTimer {get;set;}
	[Networked] public Vector3 originalPosition {get;set;}
	[Networked] public Vector3 destinationPosition {get;set;}
	public NetworkObject networkObject;
	public NetworkTransform networkTransform;
	[Networked] public NetworkObject playerToGiveTo {get;set;}
	[Networked] public NetworkObject destinationPile {get;set;}
	
	public override void Spawned()
	{
		base.Spawned();
		if(chipsInPile > 0)
		{
			ChipsInPileUpdated();
		}
	}
	
	public void DestroyAllChips()
	{
		foreach (Transform child in chipParent)
        {
			Destroy(child.gameObject);
		}
	}
	
	public void HideLabel()
	{
		betLabel.ChangeText("");
	}
	
	public int GetNumberOfPlayersInPot()
	{
		int inPot = 0;
		for(int i = 0; i < 6; i++)
		{
			if(playersInPot[i] != null)
			{
				inPot++;
			}
			else
			{
				break;
			}
		}
		return inPot;
	}
	
	public void MoveToPosition(Vector3 destination, NetworkObject destinationPlayer = null, float delay = 0)
	{
		
		originalPosition = transform.position;
		destinationPosition = destination;
		if(destinationPlayer != null)
		{
			playerToGiveTo = destinationPlayer;
		}
		if(delay > 0.0001f)
		{
			moveToPositionDelayTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, delay);
		}
		else
		{
			moveToPositionTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, LocalInterface.instance.gameManager.inGameAnimationTime);
		}
	}
	
	public void MovePileToAnother(NetworkObject destPile, int quantity)
	{
		Debug.Log($"{this.name} with {chipsInPile.ToString()} chips calls MovePileToAnother towards {destPile.name} which has {destPile.GetComponent<ChipPile>().chipsInPile.ToString()} chips. Quantity to move is {quantity.ToString()}");
		if(quantity == chipsInPile)
		{
			originalPosition = transform.position;
			destinationPile = destPile;
			destinationPosition = destinationPile.transform.position;
			moveToAnotherPileTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, LocalInterface.instance.gameManager.inGameAnimationTime);
		}
		else
		{
			chipsInPile -= quantity;
			NetworkObject newPileNO = LocalInterface.instance.networkRunner.Spawn(PokerHelper.instance.chipPilePrefab, transform.position, Quaternion.identity);
			ChipPile newPile = newPileNO.GetComponent<ChipPile>();
			newPile.chipsInPile = quantity;
			newPile.MovePileToAnother(destPile, quantity);
		}
	}
	
	public override void FixedUpdateNetwork()
	{
		if(moveToPositionTimer.IsRunning || moveToAnotherPileTimer.IsRunning)
		{
			float remainingTime;
			if(moveToPositionTimer.IsRunning)
			{
				remainingTime = (float)moveToPositionTimer.RemainingTime(LocalInterface.instance.networkRunner);
			}
			else
			{
				remainingTime = (float)moveToAnotherPileTimer.RemainingTime(LocalInterface.instance.networkRunner);
			}
			float normalizedValue = (LocalInterface.instance.gameManager.inGameAnimationTime - remainingTime) / LocalInterface.instance.gameManager.inGameAnimationTime;
			transform.position = Vector3.Lerp(originalPosition, destinationPosition, LocalAnimations.instance.animationCurve.Evaluate(normalizedValue));
		}
		if(moveToPositionDelayTimer.Expired(LocalInterface.instance.networkRunner))
		{
			MoveToPosition(destinationPosition, playerToGiveTo);
			moveToPositionDelayTimer = TickTimer.None;
		}
		if(moveToPositionTimer.Expired(LocalInterface.instance.networkRunner))
		{
			transform.position = destinationPosition;
			moveToPositionTimer = TickTimer.None;
			if(playerToGiveTo != null)
			{
				PokerPlayer playerToGiveToPokerPlayer = playerToGiveTo.GetComponent<PokerPlayer>();
				playerToGiveToPokerPlayer.chips += chipsInPile;
				LocalInterface.instance.networkRunner.Despawn(networkObject);
			}
		}
		else if(moveToAnotherPileTimer.Expired(LocalInterface.instance.networkRunner))
		{
			moveToAnotherPileTimer = TickTimer.None;
			destinationPile.GetComponent<ChipPile>().chipsInPile += chipsInPile;
			LocalInterface.instance.networkRunner.Despawn(networkObject);
		}
	}
	
	public void ChipsInPileUpdated()
	{
		if(chipsInPile == 0)
		{
			if(LocalInterface.instance.networkRunner.IsSharedModeMasterClient)
			{
				LocalInterface.instance.networkRunner.Despawn(networkObject);
			}
			return;
		}
		DestroyAllChips();
		betLabel.ChangeText(chipsInPile.ToString());
		int[] chips = new int[PokerHelper.instance.chipValues.Length];
		int numberOfStacks = 0;
		int newValue = chipsInPile;
		for(int i = 0; i < PokerHelper.instance.chipValues.Length; i++)
		{
			bool uniqueStack = true;
			while (newValue >= PokerHelper.instance.chipValues[i])
			{
				newValue -= PokerHelper.instance.chipValues[i];
				chips[i]++;
				if(uniqueStack)
				{
					numberOfStacks++;
					uniqueStack = false;
				}
			}
		}
		int curStack = 0;
		if(numberOfStacks > 4)
		{
			numberOfStacks = 4;
		}
		int chipsInStack = 0;
		for(int i = 0; i < chips.Length; i++)
		{
			if(chips[i] > 0)
			{
				for(int j = 0; j < chips[i]; j++)
				{
					GameObject newChipGO = Instantiate(PokerHelper.instance.chipPrefab, Vector3.zero, Quaternion.identity, chipParent);
					newChipGO.GetComponent<SpriteRenderer>().sprite = PokerHelper.instance.chipSprites[i];
					newChipGO.transform.localPosition = new Vector3(-0.085f * (numberOfStacks - 1) + 0.17f * curStack, chipsInStack * 0.02f, chipsInStack * -1f);
					chipsInStack++;
				}
				if(curStack < 3)
				{
					curStack++;
					chipsInStack = 0;
				}
			}
		}
	}
}
