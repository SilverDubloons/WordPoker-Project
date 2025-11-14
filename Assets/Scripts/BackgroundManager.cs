using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    public RectTransform backgroundRT;
	public float rotationSpeed;
	public bool rotating;
	
	void Update()
	{
		if(rotating)
		{
			backgroundRT.Rotate(0,0, rotationSpeed * Time.deltaTime);
		}
	}
	
	public void FollowMeClicked()
	{
		Application.OpenURL("https://twitter.com/SilverDubloons");
	}
}
