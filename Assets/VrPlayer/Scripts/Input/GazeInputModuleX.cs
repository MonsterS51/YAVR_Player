using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

//Build on top of https://github.com/RenanDresch/GazeInputSystem/blob/WIP/Assets/Package/Examples/Scripts/GazeInputModule.cs

[AddComponentMenu("Event/Gaze Input Module X")]
public class GazeInputModuleX : PointerInputModule
{
	public GameObject reticle;
	public GameObject targetObject;
	public bool targetIsClickable = false;

	private float gazeInteval = 1.5f;
	public float gazeTimer = 0;
	public bool gazeEnabled = true;
	public bool gazeInProgress = false;
	public GameObject gazeObject = null;


	private readonly Type[] clickableType = {
		typeof(Button),
		typeof(ScrollRect),
		typeof(Slider),
		typeof(Toggle),
		typeof(ToggleGroup)
	};

	//---

	void Update()
	{
		if (gazeTimer > 0) gazeTimer -= Time.deltaTime;
		if (gazeInProgress)
		{
			//scale reticle from x3 to x1
			var percent = ((gazeTimer / gazeInteval) * 2f) + 1f;
			reticle.transform.localScale = new Vector3(percent, percent, 1);
		}
	}

	//---

	public bool IsTriggerPushed()
	{
		return Input.GetKeyDown(KeyCode.Space) ||
			Google.XR.Cardboard.Api.IsTriggerPressed ||
			(Gamepad.current != null && Gamepad.current[GamepadButton.A].wasPressedThisFrame);
	}

	public override void Process()
	{
		ProcessMouseEvent();
	}

	///<summary> Clicking by gaze. </summary>
	private void GazeProcess(PointerEventData ped)
	{
		if (!gazeEnabled) return;

		// gaze object changed
		if (gazeObject != targetObject)
		{
			gazeObject = targetObject;

			if (gazeObject != null && targetIsClickable)
			{
				// gaze start
				gazeTimer = gazeInteval;
				gazeInProgress = true;
			}

			if (gazeObject == null | !targetIsClickable)
			{
				// abort gaze
				gazeTimer = 0;
				gazeInProgress = false;
				reticle.transform.localScale = Vector3.one;
			}

		}
		else
		{
			// gaze send click and done
			if (gazeInProgress && gazeTimer <= 0)
			{
				ProcessMousePress(ped);
				gazeInProgress = false;
				reticle.transform.localScale = Vector3.one;
			}
		}
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

		//process 'OnPointerEnter'
		if (pointerEventData.pointerEnter != targetObject)
		{
			// deselect previous element
			HandlePointerExitAndEnter(pointerEventData, null);
			HandlePointerExitAndEnter(pointerEventData, targetObject);
			pointerEventData.pointerEnter = targetObject;
		}

		var eventGo = pointerEventData.pointerCurrentRaycast.gameObject;

		// update reticle color on change focus
		if (targetObject != eventGo)
		{
			targetIsClickable = IsClickable(eventGo);
			SetReticleColor(targetIsClickable ? Color.cyan : Color.white);
		}

		targetObject = eventGo;

		GazeProcess(pointerEventData);

		// Process the first mouse button fully
		if (IsTriggerPushed() && IsPushTimeoutOver()) 
			ProcessMousePress(pointerEventData);

		ProcessMove(pointerEventData);
		//ProcessDrag(pointerEventData);

		//- hide reticle on nothing
		reticle.SetActive(targetObject != null);
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
		if (newPressed == null) newPressed = newClick;

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
			var clicked = ExecuteEvents.Execute(pointerEvent.pointerClick, pointerEvent, ExecuteEvents.pointerClickHandler);

			//make some noise on click
			if (clicked) Utils.Vibrate();
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

	//---

	private void SetReticleColor(Color color)
	{
		var image = reticle.GetComponent<Image>();
		if (image != null) image.color = color;
	}

	private bool IsClickable(GameObject go)
	{
		if (go == null) return false;

		if (clickableType.Contains(go.GetType())) return true;

		foreach (var clickType in clickableType)
		{
			var parentComps = go.GetComponentsInParent(clickType);
			if (parentComps.Length > 0) return true;
		}

		return false;
	}


	// Some timeout system for pushes

	private readonly Stopwatch pushTimeoutSW = Stopwatch.StartNew();
	private readonly int timeotTimeMs = 100;
	private bool IsPushTimeoutOver()
	{
		if (pushTimeoutSW.ElapsedMilliseconds > timeotTimeMs) {
			pushTimeoutSW.Restart();
			return true; 
		}
		if (!pushTimeoutSW.IsRunning) pushTimeoutSW.Restart();
		return false;
	}

}
