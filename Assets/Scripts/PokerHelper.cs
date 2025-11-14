using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PokerHelper : MonoBehaviour
{
	public Vector3[] seatLocations;
	public Vector3[] dealerButtonPositions;
	public Vector3[] bettingPositions;
	public Vector3[] communityPotLocations;
	public Vector3[] revealLabelLocations;
	public Vector3Int[] defaultBlindStructure;
	public int[] chipValues;
	public GameObject chipPrefab;
	public GameObject chipPilePrefab;
	public Sprite[] chipSprites;
	public Label totalPotLabel;
	public GameObject inputInterfaceGO;
	public MovingButton foldOrCheckButton;
	public MovingButton callButton;
	public MovingButton betOrRaiseButton;
	public MovingButton increaseBetButton;
	public MovingButton decreaseBetButton;
	public TMP_InputField betInputField;
	public Slider betSlider;
	public Toggle sittingOutToggle;
	public GameObject sittingOutInterface;
	public float timeBetweenCurrentTurnIndicatorBlinks;
	public float timeToDisplayActionTaken;
	public bool updatingBet;
	public GameObject localBestWordDisplayGO;
	public Label localBestWordLabel;
	public Image localBestWordBackdropImage;
	
	public static PokerHelper instance;
	
	void Awake()
	{
		instance = this;
	}
	
	public int PlacePlayer(int total, int num)
	{
		switch(total)
		{
			case 2:
				if(num == 0)
				{
					return 1;
				}
				else
				{
					return 5;
				}
			case 3:
				switch(num)
				{
					case 0:
						return 1;
					case 1:
						return 3;
					case 2:
						return 5;
				}
				break;
			case 4:
				switch(num)
				{
					case 0:
						return 1;
					case 1:
						return 2;
					case 2:
						return 4;
					case 3:
						return 5;
				}
				break;
			case 5:
				return num + 1;
			case 6:
				return num;
		}
		Debug.LogError("PlacePlayer called with unreasonable parameters");
		Menu.instance.DisplayError("PlacePlayer called with unreasonable parameters");
		return 7;
	}
	
	public WordPokerCard[] wordPokerCards;
	
	[System.Serializable]
	public class WordPokerCard
	{
		public char letter;
		public int val;
		public int quantity;
		public WordPokerCard(char letter, int val, int quantity)
		{
			this.letter = letter;
			this.val = val;
			this.quantity = quantity;
		}
	}
	
	public void FoldOrCheckClicked()
	{
		LocalInterface.instance.gameManager.PlayerFoldOrCheckRpc(LocalInterface.instance.fusionInterface.localPlayerRef);
		inputInterfaceGO.SetActive(false);
	}
	
	public void CallClicked()
	{
		LocalInterface.instance.gameManager.PlayerCallRpc(LocalInterface.instance.fusionInterface.localPlayerRef);
		inputInterfaceGO.SetActive(false);
	}
	
	public void BetOrRaiseClicked()
	{
		LocalInterface.instance.gameManager.PlayerBetOrRaiseRpc(LocalInterface.instance.fusionInterface.localPlayerRef, int.Parse(betInputField.text));
		inputInterfaceGO.SetActive(false);
	}
	
	public void BetSliderUpdated()
	{
		if(updatingBet)
		{
			return;
		}
		updatingBet = true;
		int curBet = 0;
		PokerPlayer localPokerPlayer = LocalInterface.instance.gameManager.FindPlayerObjectByPlayerRef(LocalInterface.instance.fusionInterface.localPlayerRef);
		if(betSlider.value == betSlider.maxValue)
		{
			curBet = localPokerPlayer.chips + localPokerPlayer.GetCurrentBet();
			increaseBetButton.ChangeDisabled(true);
		}
		else
		{
			increaseBetButton.ChangeDisabled(false);
			if(betSlider.value == betSlider.minValue)
			{
				curBet = LocalInterface.instance.gameManager.largestBet + LocalInterface.instance.gameManager.minimumRaise;
				if(localPokerPlayer.chips + localPokerPlayer.GetCurrentBet() < LocalInterface.instance.gameManager.largestBet + LocalInterface.instance.gameManager.minimumRaise)
				{
					curBet = localPokerPlayer.chips + localPokerPlayer.GetCurrentBet();
				}
				decreaseBetButton.ChangeDisabled(true);
			}
			else
			{
				decreaseBetButton.ChangeDisabled(false);
			}
		}
		if(betSlider.value != betSlider.maxValue && betSlider.value != betSlider.minValue)
		{
			curBet = Mathf.CeilToInt((LocalInterface.instance.gameManager.largestBet + LocalInterface.instance.gameManager.minimumRaise) / LocalInterface.instance.gameManager.gameBlindStructure[LocalInterface.instance.gameManager.blindLevelThisHand].x) * LocalInterface.instance.gameManager.gameBlindStructure[LocalInterface.instance.gameManager.blindLevelThisHand].x + Mathf.RoundToInt(betSlider.value) * LocalInterface.instance.gameManager.gameBlindStructure[LocalInterface.instance.gameManager.blindLevelThisHand].x;
		}
		betInputField.text = curBet.ToString();
		UpdateBetOrRaiseButton(curBet);
		updatingBet = false;
	}
	
	public void IncrementBetClicked()
	{
		betSlider.value = Mathf.RoundToInt(betSlider.value) + 1;
	}
	
	public void DecrementBetClicked()
	{
		betSlider.value = Mathf.RoundToInt(betSlider.value) - 1;
	}
	
	public void BetInputFieldUpdated()
	{
		if(updatingBet)
		{
			return;
		}
		updatingBet = true;
		PokerPlayer localPokerPlayer = LocalInterface.instance.gameManager.FindPlayerObjectByPlayerRef(LocalInterface.instance.fusionInterface.localPlayerRef);
		int practicalMinimumRaise = LocalInterface.instance.gameManager.minimumRaise;
		if(LocalInterface.instance.gameManager.largestBet + LocalInterface.instance.gameManager.minimumRaise >= localPokerPlayer.GetCurrentBet() + localPokerPlayer.chips)
		{
			practicalMinimumRaise = localPokerPlayer.GetCurrentBet() + localPokerPlayer.chips - LocalInterface.instance.gameManager.largestBet;
		}
		if(betInputField.text == string.Empty)
		{
			betSlider.value = betSlider.minValue;
			UpdateBetOrRaiseButton(LocalInterface.instance.gameManager.largestBet + practicalMinimumRaise);
		}
		else
		{
			int curInput = int.Parse(betInputField.text);
			if(curInput <= LocalInterface.instance.gameManager.largestBet + practicalMinimumRaise)
			{
				betSlider.value = betSlider.minValue;
				UpdateBetOrRaiseButton(LocalInterface.instance.gameManager.largestBet + practicalMinimumRaise);
			}
			else if(curInput < localPokerPlayer.GetCurrentBet() + localPokerPlayer.chips)
			{
				betSlider.value = Mathf.RoundToInt(((float)curInput / (localPokerPlayer.GetCurrentBet() + localPokerPlayer.chips - practicalMinimumRaise)) * (float)betSlider.maxValue);
				UpdateBetOrRaiseButton(curInput);
			}
			else
			{
				betSlider.value = betSlider.maxValue;
				UpdateBetOrRaiseButton(localPokerPlayer.GetCurrentBet() + localPokerPlayer.chips);
			}
		}
		updatingBet = false;
	}
	
	public void UpdateBetOrRaiseButton(int curBet)
	{
		if(LocalInterface.instance.gameManager.FindPlayerObjectByPlayerRef(LocalInterface.instance.fusionInterface.localPlayerRef).betting)
		{
			betOrRaiseButton.ChangeLabel("Bet " + curBet.ToString());
		}
		else
		{
			betOrRaiseButton.ChangeLabel("Raise to " + curBet.ToString());
		}
	}
	
	public string ConvertByteToActionString(byte actionByte)
	{ // 0 = fold, 1 = check, 2 = call, 3 = bet, 4 = raise
		switch(actionByte)
		{
			case 0:
				return "Fold";
			case 1:
				return "Check";
			case 2:
				return "Call";
			case 3:
				return "Bet";
			case 4:
				return "Raise";
		}
		Debug.LogWarning("ConvertByteToActionString was called with a value > 4");
		return "Error";
	}
	
	public void SittingOutToggleUpdated()
	{
		
		if(!sittingOutToggle.isOn)
		{
			if(LocalInterface.instance.networkRunner != null)
			{
				if(LocalInterface.instance.networkRunner.IsRunning)
				{
					LocalInterface.instance.gameManager.SittingOutUpdatedRpc(LocalInterface.instance.fusionInterface.localPlayerRef, LocalInterface.instance.localPlayerName);
				}
			}
			sittingOutInterface.SetActive(false);
		}
		else
		{	
			sittingOutInterface.SetActive(true);
		}
	}
}
