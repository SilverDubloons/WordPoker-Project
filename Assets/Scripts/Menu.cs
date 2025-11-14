using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Runtime.InteropServices;

public class Menu : MonoBehaviour
{
	[DllImport("__Internal")]
    private static extern void JS_FileSystem_Sync();
	
	public string currentFileManagerVersion;
	
	public bool soundOn;
	public bool musicOn;
	public float soundVolume;
	public float musicVolume;
	public bool muteOnFocusLost;
	public bool fiveSecondWarning;
	public bool myTurnSound;
	public string preferredRegion;
	
	public Toggle soundToggle;
	public Toggle musicToggle;
	public Slider soundVolumeSlider;
	public Slider musicVolumeSlider;
	public Toggle muteOnFocusLostToggle;
	public Toggle fiveSecondWarningToggle;
	public Toggle myTurnSoundToggle;
	public MovingButton disconnectButton;
	public GameObject menuObject;
	public GameObject interactionBlocker;
	public GameObject errorDisplayObject;
	public Label errorLabel;
	
	public static Menu instance;
	
	void Awake()
	{
		instance = this;
	}
	
	public void MusicToggleUpdated()
	{
		musicVolumeSlider.interactable = musicToggle.isOn;
		musicOn = musicToggle.isOn;
		if(musicOn)
		{
			if(Mathf.Abs(musicVolume) <= 0.0001f)
			{
				musicVolume = 0.1f;
				musicVolumeSlider.value = musicVolume;
			}
		}
		MusicManager.instance.MusicOptionsUpdated();
	}
	
	public void SoundToggleUpdated()
	{
		soundVolumeSlider.interactable = soundToggle.isOn;
		soundOn = soundToggle.isOn;
		if(soundOn)
		{
			if(Mathf.Abs(soundVolume) <= 0.0001f)
			{
				soundVolume = 0.1f;
				soundVolumeSlider.value = soundVolume;
			}
		}
	}
	
	public void SoundVolumeSliderUpdated()
	{
		soundVolume = soundVolumeSlider.value;
		if(Mathf.Abs(soundVolume) <= 0.0001f)
		{
			soundToggle.isOn = false;
			soundOn = false;
			soundVolumeSlider.interactable = false;
		}
	}
	
	public void MusicVolumeSliderUpdated()
	{
		musicVolume = musicVolumeSlider.value;
		if(Mathf.Abs(musicVolume) <= 0.0001f)
		{
			musicToggle.isOn = false;
			musicOn = false;
			musicVolumeSlider.interactable = false;
		}
		MusicManager.instance.MusicOptionsUpdated();
	}
	
	public void FiveSecondWarningToggleUpdated()
	{
		fiveSecondWarning = fiveSecondWarningToggle.isOn;
	}
	
	public void MyTurnSoundToggleUpdated()
	{
		myTurnSound = myTurnSoundToggle.isOn;
	}
	
	public void MuteOnFocusLostToggleUpdated()
	{
		muteOnFocusLost = muteOnFocusLostToggle.isOn;
	}
	
	
	
	
	public void UpdateOptionsFile()
	{
		string gameOptionsPath = "";
		#if UNITY_WEBGL && !UNITY_EDITOR
			gameOptionsPath = "/idbfs/WordPokerData/" + "gameOptions" + ".txt";
		#else
			gameOptionsPath = Application.persistentDataPath + "/" + "gameOptions" + ".txt";
		#endif
		#if UNITY_WEBGL && !UNITY_EDITOR
			if(!Directory.Exists("/idbfs/WordPokerData"))
			{
				Directory.CreateDirectory("/idbfs/WordPokerData");
			}
		#endif
		if(File.Exists(gameOptionsPath))
		{
			File.WriteAllText(gameOptionsPath, "");
		}
		StreamWriter writer = new StreamWriter(gameOptionsPath, true);
		writer.WriteLine(currentFileManagerVersion);
		writer.WriteLine($"playerName = {LocalInterface.instance.localPlayerName}");
		writer.WriteLine($"soundOn = {soundOn.ToString()}");
		writer.WriteLine($"soundVolume = {soundVolume.ToString()}");
		writer.WriteLine($"muteOnFocusLost = {muteOnFocusLost.ToString()}");
		writer.WriteLine($"fiveSecondWarning = {fiveSecondWarning.ToString()}");
		writer.WriteLine($"TimeToAct = {LocalInterface.instance.timeToActSliderHelper.slider.value.ToString()}");
		writer.WriteLine($"TimeBetweenBlinds = {LocalInterface.instance.timeBetweenBlindsSliderHelper.slider.value.ToString()}");
		writer.WriteLine($"StartingChips = {LocalInterface.instance.startingChipsSliderHelper.slider.value.ToString()}");
		writer.WriteLine($"WordList = {LocalInterface.instance.wordListDropdown.value.ToString()}");
		writer.WriteLine($"preferredRegion = {preferredRegion}");
		writer.WriteLine($"myTurnSound = {myTurnSound.ToString()}");
		writer.WriteLine($"musicOn = {musicOn.ToString()}");
		writer.Write($"musicVolume = {musicVolume.ToString()}");
		writer.Close();
		FileUpdated();
		MusicManager.instance.MusicOptionsUpdated();
	}
	
	void Start()
	{
		string gameOptionsPath = "";
		bool workingInEditor = false;
		#if UNITY_WEBGL && !UNITY_EDITOR
			gameOptionsPath = "/idbfs/WordPokerData/" + "gameOptions" + ".txt";
		#else
			workingInEditor = true;
			gameOptionsPath = Application.persistentDataPath + "/" + "gameOptions" + ".txt";
		#endif
		if(File.Exists(gameOptionsPath))
		{
			try
			{
				using (StreamReader reader = new StreamReader(gameOptionsPath))
				{
					string optionsData = reader.ReadToEnd();
					string[] lines = optionsData.Split('\n');
					string fileManagerVersion = lines[0].Trim();
					if(fileManagerVersion == currentFileManagerVersion)
					{
						if(!workingInEditor)
						{
							LocalInterface.instance.localPlayerName = lines[1].Replace("playerName = ", string.Empty);
							LocalInterface.instance.playerNameInputField.text = LocalInterface.instance.localPlayerName;
						}
						soundOn = bool.Parse(lines[2].Replace("soundOn = ", string.Empty));
						soundToggle.isOn = soundOn;
						soundVolume = float.Parse(lines[3].Replace("soundVolume = ", string.Empty));
						soundVolumeSlider.value = soundVolume;
						muteOnFocusLost = bool.Parse(lines[4].Replace("muteOnFocusLost = ", string.Empty));
						muteOnFocusLostToggle.isOn = muteOnFocusLost;
						fiveSecondWarning = bool.Parse(lines[5].Replace("fiveSecondWarning = ", string.Empty));
						fiveSecondWarningToggle.isOn = fiveSecondWarning;
						LocalInterface.instance.timeToActSliderHelper.slider.value = Mathf.RoundToInt(float.Parse(lines[6].Replace("TimeToAct = ", string.Empty)));
						LocalInterface.instance.timeBetweenBlindsSliderHelper.slider.value = Mathf.RoundToInt(float.Parse(lines[7].Replace("TimeBetweenBlinds = ", string.Empty)));
						LocalInterface.instance.startingChipsSliderHelper.slider.value = Mathf.RoundToInt(float.Parse(lines[8].Replace("StartingChips = ", string.Empty)));
						LocalInterface.instance.wordListDropdown.value = int.Parse(lines[9].Replace("WordList = ", string.Empty));
						preferredRegion = lines[10].Replace("preferredRegion = ", string.Empty);
						myTurnSound = bool.Parse(lines[11].Replace("myTurnSound = ", string.Empty));
						myTurnSoundToggle.isOn = myTurnSound;
						musicOn = bool.Parse(lines[12].Replace("musicOn = ", string.Empty));
						musicToggle.isOn = musicOn;
						musicVolume = float.Parse(lines[13].Replace("musicVolume = ", string.Empty));
						musicVolumeSlider.value = musicVolume;
						MusicManager.instance.MusicOptionsUpdated();
					}
					else
					{
						DisplayError($"Trying to load a version \"{fileManagerVersion}\" gameOptions. Your version is \"{currentFileManagerVersion}\"");
					}
				}
			}
			catch(Exception exception)
			{
				DisplayError($"An error occurred trying to access {gameOptionsPath}: {exception.Message}");
				UpdateOptionsFile();
			}
		}
		else
		{
			soundOn = true;
			soundToggle.isOn = soundOn;
			soundVolume = 0.25f;
			soundVolumeSlider.value = soundVolume;
			muteOnFocusLost = true;
			muteOnFocusLostToggle.isOn = muteOnFocusLost;
			fiveSecondWarning = true;
			fiveSecondWarningToggle.isOn = fiveSecondWarning;
			myTurnSound = true;
			myTurnSoundToggle.isOn = myTurnSound;
			musicOn = true;
			musicToggle.isOn = musicOn;
			musicVolume = 0.25f;
			musicVolumeSlider.value = musicVolume;
			UpdateOptionsFile();
		}
		SoundManager.instance.PlayDocWelcomeSound();
		// LocalInterface.instance.RefreshRegionDropdown();
	}
	
	public void FileUpdated()	// tested in brave, chrome and firefox, and Will's brave worked as well.
	{
		#if UNITY_WEBGL && !UNITY_EDITOR
			JS_FileSystem_Sync();
		#endif
	}
	
	public void DisplayError(string errorMessage)
	{
		menuObject.SetActive(false);
		interactionBlocker.SetActive(true);
		errorDisplayObject.SetActive(true);
		LocalInterface.instance.loadingSpinnerGO.SetActive(false);
		errorLabel.ChangeText(errorMessage);
	}
}
