using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

//Build on top of https://github.com/RenanDresch/GazeInputSystem/blob/WIP/Assets/Package/Examples/Scripts/GazeInputModule.cs

[AddComponentMenu("Event/Gaze Input Module X")]
public class GazeInputModuleX : PointerInputModule
{

	public GameObject m_CurrentFocusedGameObject;

	public override void Process()
	{
		ProcessMouseEvent();
	}

	public bool IsTriggerPushed()
	{
		return Input.GetKeyDown(KeyCode.Space) ||
			Google.XR.Cardboard.Api.IsTriggerPressed ||
			(Gamepad.current != null && Gamepad.current[GamepadButton.A].isPressed);
	}


	protected void ProcessMouseEvent()
	{
		var pointerEventData = new PointerEventData(eventSystem);

#if UNITY_EDITOR
		pointerEventData.position = new Vector2(Screen.width / 2, Screen.height / 2);
#else
		pointerEventData.position = new Vector2(UnityEngine.XR.XRSettings.eyeTextureWidth / 2, UnityEngine.XR.XRSettings.eyeTextureHeight / 2);
#endif

		pointerEventData.delta = Vector2.zero;
		List<RaycastResult> raycastResults = new List<RaycastResult>();
		eventSystem.RaycastAll(pointerEventData, raycastResults);
		pointerEventData.pointerCurrentRaycast = FindFirstRaycast(raycastResults);

		if (pointerEventData.pointerEnter != m_CurrentFocusedGameObject)
		{
			// deselect previous element
			HandlePointerExitAndEnter(pointerEventData, null);
			HandlePointerExitAndEnter(pointerEventData, m_CurrentFocusedGameObject);
			pointerEventData.pointerEnter = m_CurrentFocusedGameObject;
		}

		m_CurrentFocusedGameObject = pointerEventData.pointerCurrentRaycast.gameObject;

		// Process the first mouse button fully
		if (IsTriggerPushed()) ProcessMousePress(pointerEventData);
		ProcessMove(pointerEventData);
		//ProcessDrag(pointerEventData);
	}


	/// <summary>
	/// Calculate and process any mouse button state changes.
	/// </summary>
	protected void ProcessMousePress(PointerEventData pointerEvent)
	{
		var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

		pointerEvent.eligibleForClick = true;
		pointerEvent.delta = Vector2.zero;
		pointerEvent.dragging = false;
		pointerEvent.useDragThreshold = true;
		pointerEvent.pressPosition = pointerEvent.position;
		pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

		DeselectIfSelectionChanged(currentOverGo, pointerEvent);

		// search for the control that will receive the press
		// if we can't find a press handler set the press
		// handler to be what would receive a click.
		var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);
		var newClick = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

		// didnt find a press handler... search for a click handler
		if (newPressed == null)	newPressed = newClick;

		pointerEvent.clickCount = 1;
		pointerEvent.pointerPress = newPressed;
		pointerEvent.rawPointerPress = currentOverGo;
		pointerEvent.pointerClick = newClick;
		pointerEvent.clickTime = Time.unscaledTime;

		// Save the drag handler as well
		pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

		if (pointerEvent.pointerDrag != null)
			ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

		ReleaseMouse(pointerEvent, currentOverGo);
	}

	private void ReleaseMouse(PointerEventData pointerEvent, GameObject currentOverGo)
	{
		ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

		var pointerClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

		// PointerClick and Drop events
		if (pointerEvent.pointerClick == pointerClickHandler && pointerEvent.eligibleForClick)
		{
			ExecuteEvents.Execute(pointerEvent.pointerClick, pointerEvent, ExecuteEvents.pointerClickHandler);
		}
		if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
		{
			ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
		}

		pointerEvent.eligibleForClick = false;
		pointerEvent.pointerPress = null;
		pointerEvent.rawPointerPress = null;
		pointerEvent.pointerClick = null;

		if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
			ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

		pointerEvent.dragging = false;
		pointerEvent.pointerDrag = null;

		// redo pointer enter / exit to refresh state
		// so that if we moused over something that ignored it before
		// due to having pressed on something else
		// it now gets it.
		if (currentOverGo != pointerEvent.pointerEnter)
		{
			HandlePointerExitAndEnter(pointerEvent, null);
			HandlePointerExitAndEnter(pointerEvent, currentOverGo);
		}

	}


}
