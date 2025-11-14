using UnityEngine;

public class MusicManager : MonoBehaviour
{
	public AudioSource musicSource;
	
	public AudioClip mainMenuMusic;
	public AudioClip[] gameplayMusic;
	private int[] songOrder;
	private int curSongIndex;
	
	public static MusicManager instance;
	
	void Awake()
	{
		instance = this;
	}
	
	private void ManageMusic()
	{
		if(!musicSource.isPlaying && LocalInterface.instance.menu.musicOn)
		{
			if(LocalInterface.instance.MainMenuCanvasGO.activeSelf)
			{
				musicSource.clip = mainMenuMusic;
			}
			else
			{
				musicSource.clip = gameplayMusic[songOrder[curSongIndex]];
				curSongIndex++;
				if(curSongIndex >= songOrder.Length)
				{
					curSongIndex = 0;
				}
			}
			musicSource.Play();
		}
	}
	
	void Update()
	{
		ManageMusic();
	}
	
	private void ShuffleSongOrder()
	{
		songOrder = new int[gameplayMusic.Length];
		for(int i = 0; i < gameplayMusic.Length; i++)
		{
			songOrder[i] = i;
		}
		for(int i = 0; i < gameplayMusic.Length; i++)
		{
			int j = Random.Range(0, i + 1);
			int temp = songOrder[i];
			songOrder[i] = songOrder[j];
			songOrder[j] = temp;
		}
	}
	
	void Start()
	{
		ShuffleSongOrder();
	}
	
	public void MusicOptionsUpdated()
	{
		musicSource.volume = LocalInterface.instance.menu.musicVolume;
		if(!LocalInterface.instance.menu.musicOn)
		{
			musicSource.Stop();
		}
	}
}
