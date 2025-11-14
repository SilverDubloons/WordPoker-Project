using UnityEngine;
using Fusion;

public class DealerButton : NetworkBehaviour
{
    [Networked] TickTimer moveToSeatTimer {get;set;}
	[Networked] Vector3 originalPosition {get;set;}
	[Networked] Vector3 destinationPosition {get;set;}
	public NetworkObject networkObject;
	public NetworkTransform networkTransform;
	
	public void MoveToSeat(int seatPosition)
	{
		originalPosition = transform.position;
		destinationPosition = PokerHelper.instance.dealerButtonPositions[seatPosition];
		moveToSeatTimer = TickTimer.CreateFromSeconds(LocalInterface.instance.networkRunner, LocalAnimations.instance.animationTime);
	}
	
	public override void FixedUpdateNetwork()
	{
		if(moveToSeatTimer.IsRunning)
		{
			float remainingTime = (float)moveToSeatTimer.RemainingTime(LocalInterface.instance.networkRunner);
			float normalizedValue = (LocalAnimations.instance.animationTime - remainingTime) / LocalAnimations.instance.animationTime;
			transform.position = Vector3.Lerp(originalPosition, destinationPosition, LocalAnimations.instance.animationCurve.Evaluate(normalizedValue));
			if(moveToSeatTimer.Expired(LocalInterface.instance.networkRunner))
			{
				transform.position = destinationPosition;
				moveToSeatTimer = TickTimer.None;
			}
		}
	}
}
