using UnityEngine;
using System.Collections;
using System.Linq;

public class LocalAnimations : MonoBehaviour
{
	public float animationTime;
	public AnimationCurve animationCurve;
	
	public RectTransform mainMenuRT;
	public RectTransform logoRT;
	public RectTransform selfPromotionRT;
	public RectTransform lobbyMessageRT;
	public RectTransform lobbyCodeRT;
	// public RectTransform startGameButtonRT;
	public RectTransform masterClientLobbyInterfaceRT;
	public RectTransform clientLobbyInterfaceRT;
	public RectTransform pokerTableRT;
	
	public delegate void CallbackFunction();
	
	public static LocalAnimations instance;
	
	void Awake()
	{
		instance = this;
	}
	
	void Start()
	{
		RevealMainMenu();
	}
	
/* 	void Update()
	{
		Debug.Log($"masterClientLobbyInterfaceRT.anchoredPosition.y = {masterClientLobbyInterfaceRT.anchoredPosition.y.ToString()}");
	} */
	
	public void RevealGameplayInterface()
	{
		LocalInterface.instance.gameplayCanvasGO.SetActive(true);
		PokerHelper.instance.inputInterfaceGO.SetActive(false);
		StartCoroutine(MoveRectTransform(pokerTableRT, new Vector2(0, 360) , new Vector2(0, 0)));
	}
	
	public void HideGameplayInterface()
	{
		PokerHelper.instance.inputInterfaceGO.SetActive(false);
		LocalInterface.instance.gameplayCanvasGO.SetActive(true);
		LocalInterface.instance.blindStructureInformationLabel.ChangeText(string.Empty);
		LocalInterface.instance.currentBlindsLabel.ChangeText(string.Empty);
		LocalInterface.instance.timerLabel.ChangeText(string.Empty);
		PokerHelper.instance.sittingOutToggle.isOn = false;
		PokerHelper.instance.sittingOutInterface.SetActive(false);
		StartCoroutine(MoveRectTransform(pokerTableRT, new Vector2(0, 0) , new Vector2(0, 360), DisableGameplayCanvas));
	}
	
    public void HideMainMenu()
	{
		LocalInterface.instance.MainMenuCanvasGO.SetActive(true);
		LocalInterface.instance.roomCodeInputField.interactable = false;
		LocalInterface.instance.playerNameInputField.interactable = false;
		LocalInterface.instance.hostGameButton.ChangeDisabled(true);
		LocalInterface.instance.joinGameButton.ChangeDisabled(true);
		StartCoroutine(MoveRectTransform(mainMenuRT, new Vector2(-230, 0) , new Vector2(-430, 0), DisableMainMenuCanvas));
		StartCoroutine(MoveRectTransform(logoRT, new Vector2(160, 110) , new Vector2(470, 110)));
		StartCoroutine(MoveRectTransform(selfPromotionRT, new Vector2(160, -110) , new Vector2(160, -230)));
	}
	
	public void RevealMainMenu()
	{
		LocalInterface.instance.MainMenuCanvasGO.SetActive(true);
		LocalInterface.instance.roomCodeInputField.interactable = false;
		LocalInterface.instance.playerNameInputField.interactable = false;
		LocalInterface.instance.hostGameButton.ChangeDisabled(true);
		LocalInterface.instance.joinGameButton.ChangeDisabled(true);
		StartCoroutine(MoveRectTransform(mainMenuRT, new Vector2(-430, 0) , new Vector2(-230, 0), EnableMainMenuInteraction));
		StartCoroutine(MoveRectTransform(logoRT, new Vector2(470, 110) , new Vector2(160, 110)));
		StartCoroutine(MoveRectTransform(selfPromotionRT, new Vector2(160, -230) , new Vector2(160, -110)));
	}
	
	public void HideLobbyInterface()
	{
		LocalInterface.instance.LobbyCanvasGO.SetActive(true);
		LocalInterface.instance.copyCodeButton.ChangeDisabled(true);
		StartCoroutine(MoveRectTransform(lobbyMessageRT, new Vector2(-40, 145) , new Vector2(-40, 225)));
		StartCoroutine(MoveRectTransform(lobbyCodeRT, new Vector2(230, 145) , new Vector2(230, 225), DisableLobbyCanvas));
		if(LocalInterface.instance.masterClientLobbyInterface.activeSelf)
		{
			LocalInterface.instance.wordListDropdown.interactable = false;
			LocalInterface.instance.timeToActSliderHelper.slider.interactable = false;
			LocalInterface.instance.timeBetweenBlindsSliderHelper.slider.interactable = false;
			LocalInterface.instance.startingChipsSliderHelper.slider.interactable = false;
			StartCoroutine(MoveRectTransform(masterClientLobbyInterfaceRT, new Vector2(-70, -125) , new Vector2(-70, -240), DisableMasterClientLobbyInterface));
		}
		if(LocalInterface.instance.clientLobbyInterface.activeSelf)
		{
			StartCoroutine(MoveRectTransform(clientLobbyInterfaceRT, new Vector2(-70, -125) , new Vector2(-70, -240), DisableClientLobbyInterface));
		}
	}
	
	public void RevealLobbyInterface()
	{
		LocalInterface.instance.LobbyCanvasGO.SetActive(true);
		LocalInterface.instance.copyCodeButton.ChangeDisabled(true);
		StartCoroutine(MoveRectTransform(lobbyMessageRT, new Vector2(-40, 225) , new Vector2(-40, 145)));
		StartCoroutine(MoveRectTransform(lobbyCodeRT, new Vector2(230, 225) , new Vector2(230, 145), EnableCopyButton));
		if(LocalInterface.instance.networkRunner.IsSharedModeMasterClient)
		{
			LocalInterface.instance.masterClientLobbyInterface.SetActive(true);
			LocalInterface.instance.clientLobbyInterface.SetActive(false);
			LocalInterface.instance.startGameButton.ChangeDisabled(true);
			LocalInterface.instance.wordListDropdown.interactable = false;
			LocalInterface.instance.timeToActSliderHelper.slider.interactable = false;
			LocalInterface.instance.timeBetweenBlindsSliderHelper.slider.interactable = false;
			LocalInterface.instance.startingChipsSliderHelper.slider.interactable = false;
			StartCoroutine(MoveRectTransform(masterClientLobbyInterfaceRT, new Vector2(-70, -240) , new Vector2(-70, -125), EnableMasterClientLobbyOptions));
		}
		else
		{
			LocalInterface.instance.masterClientLobbyInterface.SetActive(false);
			LocalInterface.instance.clientLobbyInterface.SetActive(true);
			StartCoroutine(MoveRectTransform(clientLobbyInterfaceRT, new Vector2(-70, -240) , new Vector2(-70, -125)));
		}
	}
	
	public void DisableMasterClientLobbyInterface()
	{
		LocalInterface.instance.masterClientLobbyInterface.SetActive(false);
		LocalInterface.instance.LobbyCanvasGO.SetActive(false);
	}
	
	public void DisableClientLobbyInterface()
	{
		LocalInterface.instance.clientLobbyInterface.SetActive(false);
		LocalInterface.instance.LobbyCanvasGO.SetActive(false);
	}
	
	public void EnableMasterClientLobbyOptions()
	{
		LocalInterface.instance.wordListDropdown.interactable = true;
		LocalInterface.instance.timeToActSliderHelper.slider.interactable = true;
		LocalInterface.instance.timeBetweenBlindsSliderHelper.slider.interactable = true;
		LocalInterface.instance.startingChipsSliderHelper.slider.interactable = true;
		EnableStartGameButton();
	}
	
	public void EnableStartGameButton()
	{
		if(LocalInterface.instance.networkRunner.ActivePlayers.Count() > 1 && LocalInterface.instance.networkRunner.IsSharedModeMasterClient)
		{
			LocalInterface.instance.startGameButton.ChangeDisabled(false);
		}
	}
	
	public void DisableLobbyCanvas()
	{
		LocalInterface.instance.LobbyCanvasGO.SetActive(false);
	}
	
	public void DisableGameplayCanvas()
	{
		LocalInterface.instance.gameplayCanvasGO.SetActive(false);
	}
	
	public void EnableCopyButton()
	{
		LocalInterface.instance.copyCodeButton.ChangeDisabled(false);
	}
	
	public void DisableMainMenuCanvas()
	{
		LocalInterface.instance.MainMenuCanvasGO.SetActive(false);
	}
	
	public void EnableMainMenuInteraction()
	{
		LocalInterface.instance.roomCodeInputField.interactable = true;
		LocalInterface.instance.playerNameInputField.interactable = true;
		LocalInterface.instance.MainMenuInputUpdated();
	}
	
	public IEnumerator MoveRectTransform(RectTransform rt, Vector2 startPosition, Vector3 endPosition, CallbackFunction endFunction = null)
	{
		float t = 0;
		while(t < animationTime)
		{
			t += Time.deltaTime;
			rt.anchoredPosition = Vector2.Lerp(startPosition, endPosition, animationCurve.Evaluate(t / animationTime));
			yield return null;
		}
		rt.anchoredPosition = endPosition;
		if(endFunction != null)
		{
			endFunction();
		}
	}
}