using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlideOut : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform rt;
	public float moveSpeed;
	public Vector2 startPosition;
	public Vector2 endPosition;
	private bool mouseOver = false;
	private float moveState = 0;
	
    void Update()
	{
		if(mouseOver && moveState < 1f)
		{
			moveState += moveSpeed * Time.deltaTime;
			if(moveState > 1f)
			{
				moveState = 1f;
			}
			rt.anchoredPosition = Vector2.Lerp(startPosition, endPosition, moveState);
		}
		if(!mouseOver && moveState > 0)
		{
			moveState -= moveSpeed * Time.deltaTime;
			if(moveState < 0)
			{
				moveState = 0;
			}
			rt.anchoredPosition = Vector2.Lerp(startPosition, endPosition, moveState);
		}
		
	}
	
	public void OnPointerEnter(PointerEventData pointerEventData)
    {
		// SoundManager.instance.PlaySlideOutSound();
		transform.SetSiblingIndex(transform.parent.childCount - 1);
		mouseOver = true;
	}
	
	public void OnPointerExit(PointerEventData pointerEventData)
    {
		// SoundManager.instance.PlaySlideOutSound(true);
		mouseOver = false;
	}
}
