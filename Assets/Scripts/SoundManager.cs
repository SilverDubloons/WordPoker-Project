using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour
{
	public AudioClip buttonSound;
	public AudioClip fiveSecondWarningSound;
	public AudioClip myTurnSound;
	public AudioClip myTurnSoundDoc;
	public AudioClip welcomeSoundDoc;
	public AudioClip[] tickSounds;
	public AudioClip[] dealSounds;
	public AudioClip[] foldSounds;
	public AudioClip[] betSounds;
	public AudioClip[] callSounds;
	
	public AudioSource soundSource;
	public AudioSource tickSource;
	
	public static SoundManager instance;
	void Awake()
	{
		instance = this;
	}
	
	public void PlaySound(AudioClip sound, float volumeFactor = 1f)
	{
		if(LocalInterface.instance.menu.soundOn && (Application.isFocused || (!Application.isFocused && !LocalInterface.instance.menu.muteOnFocusLost)))
		{
			soundSource.PlayOneShot(sound, LocalInterface.instance.menu.soundVolume * volumeFactor);
		}
	}
	
	public void PlayBetSound()
	{
		PlaySound(betSounds[Random.Range(0,betSounds.Length)]);
	}
	
	public void PlayFoldSound()
	{
		PlaySound(foldSounds[Random.Range(0,foldSounds.Length)]);
	}
	
	public void PlayDealSound()
	{
		PlaySound(dealSounds[Random.Range(0,dealSounds.Length)]);
	}
	
	public void PlayCallSound()
	{
		PlaySound(callSounds[Random.Range(0,callSounds.Length)]);
	}
	
	public void PlayCheckSound()
	{
		StartCoroutine(CheckSounds());
	}
	
	public IEnumerator CheckSounds()
	{
		PlayTickSound();
		yield return new WaitForSeconds(0.07f);
		PlayTickSound();
	}
	
	private float lastTickSoundTime = 0;
	private int tickSoundIndex = 0;
	
	public void PlayTickSound()
	{
		if(LocalInterface.instance.menu.soundOn && (Application.isFocused || (!Application.isFocused && !LocalInterface.instance.menu.muteOnFocusLost)))
		{
			if(Time.time - lastTickSoundTime > 0.2f)
			{
				tickSoundIndex = 0;
			}
			lastTickSoundTime = Time.time;
			tickSource.pitch = 1f + 0.05f * tickSoundIndex;
			tickSource.PlayOneShot(tickSounds[Random.Range(0,tickSounds.Length)], LocalInterface.instance.menu.soundVolume * 0.5f);
			tickSoundIndex++;
		}
	}
	
	public void PlayFiveSecondWarningSound()
	{
		if(LocalInterface.instance.menu.fiveSecondWarning)
		{
			soundSource.PlayOneShot(fiveSecondWarningSound, LocalInterface.instance.menu.soundVolume);
		}
	}
	
	public void PlayMyTurnSound()
	{
		if(LocalInterface.instance.menu.myTurnSound)
		{
			int r = UnityEngine.Random.Range(0,100000);
			if(r == 0)
			{
				soundSource.PlayOneShot(myTurnSoundDoc, LocalInterface.instance.menu.soundVolume);
			}
			else
			{
				soundSource.PlayOneShot(myTurnSound, LocalInterface.instance.menu.soundVolume);
			}
		}
	}
	
	public void PlayDocWelcomeSound()
	{
		if(LocalInterface.instance.menu.soundOn)
		{
			int r = UnityEngine.Random.Range(0,1000);
			if(r == 0)
			{
				soundSource.PlayOneShot(welcomeSoundDoc, LocalInterface.instance.menu.soundVolume);
			}
		}
	}
}
