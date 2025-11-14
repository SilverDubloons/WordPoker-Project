using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using Unity.Multiplayer.Playmode;
using Fusion;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;

/* #if UNITY_WEBGL && UNITY_2018_3_OR_NEWER
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]
#endif
 */
public class LocalInterface : MonoBehaviour
{
	public string version;
	
	public string localPlayerName {get;set;}
	public NetworkRunner networkRunnerPrefab;
	public NetworkRunner networkRunner = null;
	public FusionInterface fusionInterface;
	public GameManager gameManager = null;
	public LocalAnimations localAnimations;
	public Menu menu;
	
	public TMP_InputField roomCodeInputField;
	public RectTransform roomCodeInputFieldRT;
	// public TMP_InputField roomCodeOutputField;
	public Label roomCodeLabel;
	public TMP_InputField playerNameInputField;
	public MovingButton hostGameButton;
	public MovingButton joinGameButton;
	public MovingButton copyCodeButton;
	public MovingButton pasteCodeButton;
	public TMP_InputField lobbyCodeOutputInputField;
	
	public GameObject MainMenuCanvasGO;
	public GameObject LobbyCanvasGO;
	public GameObject gameplayCanvasGO;
	public GameObject masterClientLobbyInterface;
	public GameObject clientLobbyInterface;
	public Label clientTimeToActLabel;
	public Label clientTimeBetweenBlindsLabel;
	public Label clientStartingChipsLabel;
	public Label clientDictionaryUsedLabel;
	
	public Label lobbyMessageLabel;
	public MovingButton startGameButton;
	public TMP_Dropdown wordListDropdown;
	public SliderHelper timeToActSliderHelper;
	public SliderHelper timeBetweenBlindsSliderHelper;
	public SliderHelper startingChipsSliderHelper;
	// lobby information for clients, like wordListLabel was in prototype
	
	public GameObject playerPrefab;
	public GameObject gameManagerPrefab;
	public GameObject cardPrefab;
	public GameObject dealerButtonPrefab;
	
	public char[] unambiguousCharacters;
	public Color[] playerColors;
	public Color disconnectedColor;
	public Color eliminatedColor;
	
	public GameObject loadingSpinnerGO;
	public Label loadingSpinnerLabel;
	
	public Label blindStructureInformationLabel;
	public Label currentBlindsLabel;
	public Label timerLabel;
	public RectTransform blindStructureInformationBackdropRT;
	
	public static LocalInterface instance;
	
	public TMP_Dropdown regionDropdown;
	public GameObject startingNetworkRunner;
	private CancellationTokenSource tokenSource;
	
	public Label versionLabel;
	
	void Awake()
	{
		instance = this;
		var mppmTag = CurrentPlayer.ReadOnlyTags();
		if(mppmTag[0] == "p1")
		{
			playerNameInputField.text = "p1";
			localPlayerName = "p1";
		}
		if(mppmTag[0] == "p2")
		{
			playerNameInputField.text = "p2";
			localPlayerName = "p2";
		}
		if(mppmTag[0] == "p3")
		{
			playerNameInputField.text = "p3";
			localPlayerName = "p3";
		}
		if(mppmTag[0] == "p4")
		{
			playerNameInputField.text = "p4";
			localPlayerName = "p4";
		}
		versionLabel.ChangeText($"Version\n{version}");
	}
	
	void Start()
	{
		//#if UNITY_WEBGL && !UNITY_EDITOR
			copyCodeButton.gameObject.SetActive(false);
			pasteCodeButton.gameObject.SetActive(false);
			roomCodeInputFieldRT.offsetMax = new Vector2(-10, roomCodeInputFieldRT.offsetMax.y);
			roomCodeLabel.gameObject.SetActive(false);
			lobbyCodeOutputInputField.gameObject.SetActive(true);
		//#else
			
		//#endif
	}

	public async void RefreshRegionDropdown()
	{
		tokenSource = new CancellationTokenSource();
		var regions = await NetworkRunner.GetAvailableRegions(cancellationToken: tokenSource.Token);
		regionDropdown.options.Clear();
		//regionDropdown.AddOptions(regions.Select(reg => $"{reg.RegionCode} = {reg.RegionPing}").ToList());
		int preferredRegionIndex = -1;
		regions.Sort((x,y) => x.RegionPing - y.RegionPing);
		for(int i = 0; i < regions.Count; i++)
		{
			List<string> nextOption = new List<string> {regions[i].RegionCode.ToUpper()};
			regionDropdown.AddOptions(nextOption);
			if(regions[i].RegionCode.ToUpper() == menu.preferredRegion)
			{
				preferredRegionIndex = i;
			}
		}
		// regionDropdown.AddOptions(regions.Select(reg => $"{reg.RegionCode.ToUpper()}").ToList());
		if(preferredRegionIndex >= 0)
		{
			regionDropdown.value = preferredRegionIndex;
		}
		else
		{
			regionDropdown.value = 0;
		}
		Destroy(startingNetworkRunner);
	}
	
	public void RegionDropdownUpdated()
	{
		menu.preferredRegion = regionDropdown.options[regionDropdown.value].text.ToUpper();
		menu.UpdateOptionsFile();
	}
	
	public string CreateLobbyCode(int numberOfChars)
	{
		string lobbyCode = string.Empty;
		while(lobbyCode.Length < numberOfChars)
		{
			lobbyCode += unambiguousCharacters[UnityEngine.Random.Range(0, unambiguousCharacters.Length)];
		}
		return lobbyCode;
	}
	
	public void RoomCodeOutputInputFieldUpdated()
	{
		lobbyCodeOutputInputField.text = LocalInterface.instance.networkRunner.SessionInfo.Name;
	}
	
	public void StartLobby(bool hostGame)
    {
		UpdateLoadingSpinner(true, hostGame ? "Setting Up Room" : "Joining Room");
		if(networkRunner)
		{
			Destroy(networkRunner.gameObject);
		}
		networkRunner = Instantiate(networkRunnerPrefab, Vector3.zero, Quaternion.identity);
		fusionInterface = networkRunner.GetComponent<FusionInterface>();
		
		//fusionInterface.StartLobby(hostGame, regionDropdown.options[regionDropdown.value].text.ToUpper());
		fusionInterface.StartLobby(hostGame);
	}
	
	public void MainMenuInputUpdated()
	{
		if(playerNameInputField.text.Length > 0)
		{
			hostGameButton.ChangeDisabled(false);
			if(roomCodeInputField.text.Length == 4)
			{
				joinGameButton.ChangeDisabled(false);
			}
			else
			{
				joinGameButton.ChangeDisabled(true);
			}
		}
		else
		{
			hostGameButton.ChangeDisabled(true);
			joinGameButton.ChangeDisabled(true);
		}
	}
	
	public void CopyCodeClicked()
	{
		GUIUtility.systemCopyBuffer = roomCodeLabel.labelShadow.text;
	}
	
	public void DisconnectClicked()
	{
		networkRunner.Shutdown();
		if(LobbyCanvasGO.activeSelf)
		{
			localAnimations.HideLobbyInterface();
		}
		if(gameplayCanvasGO.activeSelf)
		{
			localAnimations.HideGameplayInterface();
		}
		StartCoroutine(RevealMainMenu(localAnimations.animationTime));
		menu.disconnectButton.ChangeDisabled(true);
		menu.interactionBlocker.SetActive(false);
		menu.menuObject.SetActive(false);
	}
	
	public IEnumerator RevealMainMenu(float delay)
	{
		float t = 0;
		while(t < delay)
		{
			t += Time.deltaTime;
			yield return null;
		}
		localAnimations.RevealMainMenu();
	}
	
	public void StartGame()
	{
		menu.UpdateOptionsFile();
		fusionInterface.StartGame();
	}
	
	public void WordListDropdownUpdated()
	{
		if(gameManager != null && networkRunner.IsSharedModeMasterClient)
		{
			gameManager.dictionaryBeingUsed = wordListDropdown.value;
		}
	}
	
	public void UpdateLoadingSpinner(bool active, string message = "")
	{
		loadingSpinnerGO.SetActive(active);
		if(active)
		{
			loadingSpinnerLabel.ChangeText(message);
		}
	}
	
	public void PasteButtonClicked()
	{
		roomCodeInputField.text = GUIUtility.systemCopyBuffer;
	}
	
	public void TwitterClicked()
	{
		Application.OpenURL("https://twitter.com/SilverDubloons");
	}
	
	public void KoFiClicked()
	{
		Application.OpenURL("https://ko-fi.com/silverdubloons");
	}
	
	public void DiscordClicked()
	{
		Application.OpenURL("https://discord.gg/TdJJBgbWTf");
	}
}