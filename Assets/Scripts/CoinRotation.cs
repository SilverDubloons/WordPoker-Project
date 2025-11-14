using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CoinRotation : MonoBehaviour, IPointerClickHandler
{
    public RectTransform rt;
	public Transform baseCanvas;
	public Vector2 relativePosition;
	private bool spinning;
	
	public void SetRelativePosition()
	{
		Transform oldParent = transform.parent;
		transform.SetParent(baseCanvas);
		relativePosition = rt.anchoredPosition + new Vector2(0, 0);
		transform.SetParent(oldParent);
	}

    public void OnPointerClick(PointerEventData eventData)
    {
		if(!spinning)
		{
			SetRelativePosition();
			Vector2 mousePos = new Vector2((Input.mousePosition.x/Screen.width)*640 - 320,((Input.mousePosition.y/Screen.height))*360 -180);
			Vector2 clickOffset = mousePos - relativePosition;
			// Debug.Log($"relativePosition = {relativePosition.ToString()} mousePos = {mousePos.ToString()} clickOffset = {clickOffset.ToString()}");
			StartCoroutine(RotateCoin(clickOffset));
		}
    }
	
	public IEnumerator RotateCoin(Vector2 clickOffset)
	{
		spinning = true;
		float t = 0;
		//float rotationSpeed = 1f;
		//Vector3 rotationToApply = new Vector3(Mathf.Round(clickOffset.x) * 360f, Mathf.Round(clickOffset.y) * 360f, 0);
		Vector3 startRotation = Vector3.zero;
		Vector3 endRotation = new Vector3(Mathf.Round(clickOffset.y/5) * 360f, Mathf.Round(clickOffset.x/5) * 360f, 0);
		while(t < 2f)
		{
			t += Time.deltaTime;
			//rt.Rotate(rotationToApply * rotationSpeed * Time.deltaTime);
			rt.eulerAngles = Vector3.Lerp(startRotation, endRotation, t / 2f);
			yield return null;
		}
		spinning = false;
	}
}