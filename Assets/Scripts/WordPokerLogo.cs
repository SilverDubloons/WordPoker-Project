using UnityEngine;

public class WordPokerLogo : MonoBehaviour
{
    public RectTransform[] tiles;
	public Vector2[] originalPositions;
	private float flourishStage;
	public  float stageTime;
	public float flourishIntensity;
	private bool increasing;
	public float timeBetweenFlourishes; // 0.2
	public float flourishDuration; // 0.5
	public AnimationCurve flourishCurve;
	private float breathStage;
	public float breathTime;
	public float breathIntensity;
	public float timeBetweenBreaths; // 0.1
	public AnimationCurve breathCurve;
	
	void Start()
	{
		flourishStage = 4f;
	}
	
	void Update()
	{
		flourishStage += Time.deltaTime;
		if(flourishStage >= stageTime)
		{
			flourishStage = 0;
		}
		breathStage += Time.deltaTime;
		if(breathStage >= breathTime)
		{
			breathStage = 0;
		}

		for(int i = 0; i < tiles.Length; i++)
		{
			Vector2 newPosition = originalPositions[i];
			float breathState = breathStage + i * timeBetweenBreaths;
			if(breathState > breathTime)
			{
				breathState -= breathTime;
			}
			newPosition += new Vector2(0, breathCurve.Evaluate(breathState / breathTime) * breathIntensity);
			if(flourishStage >= (float)i * timeBetweenFlourishes && flourishStage < i * timeBetweenFlourishes + flourishDuration)
			{
				float flourishState = flourishStage - i * timeBetweenFlourishes;
				newPosition += new Vector2(0, flourishCurve.Evaluate(flourishState / flourishDuration) * flourishIntensity);
			}
			tiles[i].anchoredPosition = newPosition;
		}
	}
}
