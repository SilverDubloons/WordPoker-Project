using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
	public RectTransform[] suitRTs;
	public float spinSpeed;
	public float radius;
	private float angle;
	
	void Update()
	{
		angle -= Time.deltaTime * spinSpeed;
		if(angle <= 360)
		{
			angle += 360;
		}
		for(int i = 0; i < 4; i++)
		{
			suitRTs[i].anchoredPosition = new Vector2(radius * Mathf.Cos(Mathf.Deg2Rad * (angle + i * 90)), radius * Mathf.Sin(Mathf.Deg2Rad * (angle + i * 90)));
		}
	}
}
