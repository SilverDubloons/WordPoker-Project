using UnityEngine;
using Fusion;
using TMPro;

public class Card : NetworkBehaviour
{
    public NetworkTransform networkTransform;
	public TMP_Text valueText;
	public TMP_Text stringText;
	public GameObject cardValueGO;
	public GameObject cardStringGO;
	[Networked, OnChangedRender(nameof(OnCardStringChanged))]
	public string cardString {get;set;}
	[Networked, OnChangedRender(nameof(OnCardValueChanged))]
	public int cardValue {get;set;}
	[Networked, OnChangedRender(nameof(OnOwnerChanged))]
	public PlayerRef owningPlayer{get;set;}
	[Networked] TickTimer moveToPositionTimer {get;set;}
	[Networked] Vector3 originalPosition {get;set;}
	[Networked] Vector3 destinationPosition {get;set;}
	[Networked, OnChangedRender(nameof(OnCardIsFaceUpChanged))]
	public bool cardIsFaceUp {get;set;}
	public SpriteRenderer cardBackdropSpriteRenderer;
	[Networked, OnChangedRender(nameof(OnColorChanged))]
	public Color cardColor {get;set;}
	
	public void MoveToPosition(Vector3 destination)
	{
		originalPosition = transform.position;
		destinationPosition = destination;
		moveToPositionTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, LocalInterface.instance.gameManager.inGameAnimationTime);
	}
	
	public override void FixedUpdateNetwork()
	{
		if(moveToPositionTimer.IsRunning)
		{
			float remainingTime = (float)moveToPositionTimer.RemainingTime(LocalInterface.instance.networkRunner);
			float normalizedValue = (LocalInterface.instance.gameManager.inGameAnimationTime - remainingTime) / LocalInterface.instance.gameManager.inGameAnimationTime;
			transform.position = Vector3.Lerp(originalPosition, destinationPosition, LocalAnimations.instance.animationCurve.Evaluate(normalizedValue));
			if(moveToPositionTimer.Expired(LocalInterface.instance.networkRunner))
			{
				transform.position = destinationPosition;
				moveToPositionTimer = TickTimer.None;
			}
		}
	}
	
	public void OnColorChanged()
	{
		cardBackdropSpriteRenderer.color = cardColor;
	}
	
	public void OnCardIsFaceUpChanged()
	{
		cardValueGO.SetActive(cardIsFaceUp);
		cardStringGO.SetActive(cardIsFaceUp);
	}
	
	public override void Spawned()
	{
		base.Spawned();
		OnColorChanged();
		OnCardValueChanged();
		OnCardStringChanged();
		OnCardIsFaceUpChanged();
		OnOwnerChanged();
		SoundManager.instance.PlayDealSound();
	}
	
	public void OnCardStringChanged()
	{
		stringText.text = cardString;
	}
	
	public void OnCardValueChanged()
	{
		valueText.text = cardValue.ToString();
	}
	
	public void OnOwnerChanged()
	{
		if(owningPlayer == LocalInterface.instance.fusionInterface.localPlayerRef)
		{
			cardValueGO.SetActive(true);
			cardStringGO.SetActive(true);
		}
	}
}
