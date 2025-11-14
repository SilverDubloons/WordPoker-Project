using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class MovingButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
	public RectTransform rt;
	public RectTransform shadowRT;
	public Image buttonImage;
	public RectTransform buttonImageRT;
	private Vector2 buttonImageOrigin;
	//public RectTransform textRT;
	//public Vector2 textOrigin;
	public Color baseColor;
	public Color specialColor;
	public Color hoverColor;
	public Color disabledColor;
	public int pixelsToMoveButton;
	public bool playClickingSound = true;
	
	private bool holdingDown = false;
	private bool mouseOverButton = false;
	private bool specialState;
	public bool disabled;
	private bool setupComplete = false;
	public Label label;
	
	[SerializeField]
    private UnityEvent onClickEvent;
	
	[SerializeField]
    private UnityEvent onDoubleClickEvent;
	
	public void ChangeLabel(string newLabel)
	{
		label.ChangeText(newLabel);
	}
	
	public void OnPointerDown(PointerEventData pointerEventData)
	{
		//print("ccdown= " + pointerEventData.clickCount);
		if(!disabled)
		{
			holdingDown = true;
			buttonImageRT.anchoredPosition = new Vector2(buttonImageRT.anchoredPosition.x, buttonImageRT.anchoredPosition.y - pixelsToMoveButton);
		}
		//print(name + " was pressed down");
	}
	
	void OnDisable()
    {
        ChangeDisabled(disabled);
		if(setupComplete)
		{
			buttonImageRT.anchoredPosition = buttonImageOrigin;
		}
		mouseOverButton = false;
    }
	
	public void OnPointerUp(PointerEventData pointerEventData)
    {
		if(!disabled)
		{
			if(onDoubleClickEvent.GetPersistentEventCount() > 0)
			{
				if(pointerEventData.clickCount >= 2) // button was double clicked
				{
					onDoubleClickEvent.Invoke();
				}
				else
				{
					if(holdingDown && mouseOverButton)
					{
						onClickEvent.Invoke();
					}
				}
			}
			else
			{
				if(holdingDown && mouseOverButton)
				{
					onClickEvent.Invoke();
				}
			}
			if(playClickingSound)
			{
				SoundManager.instance.PlaySound(SoundManager.instance.buttonSound);
			}
			buttonImageRT.anchoredPosition = buttonImageOrigin;
			holdingDown = false;
		}
    }
	
	public void ChangeSpecailState(bool active)
	{
		specialState = active;
		if(specialState)
		{
			buttonImage.color = specialColor;
		}
		else
		{
			buttonImage.color = baseColor;
		}
	}
	
	public void ChangeDisabled(bool disable)
	{
		disabled = disable;
		if(disabled)
		{
			buttonImage.color = disabledColor;
		}
		else
		{
			if(specialState)
			{
				buttonImage.color = specialColor;
			}
			else
			{
				buttonImage.color = baseColor;
			}
		}
	}
	
    public void OnPointerEnter(PointerEventData pointerEventData)
    {
		if(!disabled)
		{
			if(specialState)
			{
				buttonImage.color = hoverColor * specialColor;
			}
			else
			{
				buttonImage.color = hoverColor * baseColor;
			}
		}
		mouseOverButton = true;
    }
	
	public void OnPointerExit(PointerEventData pointerEventData)
    {
		if(!disabled)
		{
			if(specialState)
			{
				buttonImage.color = specialColor;
			}
			else
			{
				buttonImage.color = baseColor;
			}
			buttonImageRT.anchoredPosition = buttonImageOrigin;
		}
		mouseOverButton = false;
    }
	
    void Start()
    {
		//baseColor = buttonImage.color;
		buttonImageOrigin = buttonImageRT.anchoredPosition;
		if(disabled)
		{
			ChangeDisabled(disabled);
		}
		setupComplete = true;
    }
}
