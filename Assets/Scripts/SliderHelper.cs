using UnityEngine;
using UnityEngine.UI;

public class SliderHelper : MonoBehaviour
{
	public Slider slider;
	public Label outputLabel;
	public int multiplier;
	public int baseValue;
	public int totalVal;
	public string sliderType;
	
	public void OnSliderChanged()
	{
		totalVal = baseValue + Mathf.RoundToInt(slider.value) * multiplier;
		outputLabel.ChangeText(totalVal.ToString());
		if(LocalInterface.instance.gameManager != null)
		{
			switch(sliderType)
			{
				case "TimeToAct":
					LocalInterface.instance.gameManager.maximumDecisionTime = totalVal;
					break;
				case "TimeBetweenBlinds":
					LocalInterface.instance.gameManager.timeBetweenBlindLevels = totalVal * 60;
					break;
				case "StartingChips":
					LocalInterface.instance.fusionInterface.StartingChipsUpdated(totalVal);
					LocalInterface.instance.gameManager.chipsAtGameStart = totalVal;
					break;
					
			}
		}
	}
}
