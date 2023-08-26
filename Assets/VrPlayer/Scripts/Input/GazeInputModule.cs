// Gaze Input Module by Peter Koch <peterept@gmail.com>
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// To use:
// 1. Drag onto your EventSystem game object.
// 2. Disable any other Input Modules (eg: StandaloneInputModule & TouchInputModule) as they will fight over selections.
// 3. Make sure your Canvas is in world space and has a GraphicRaycaster (should by default).
// 4. If you have multiple cameras then make sure to drag your VR (center eye) camera into the canvas.
public class GazeInputModule : PointerInputModule
{
	public enum Mode { Click = 0, Gaze };
	public Mode mode;

	[Header("Gaze Settings")]
	public float GazeTimeInSeconds = 2f;

	public RaycastResult CurrentRaycast;

	private PointerEventData pointerEventData;
	private GameObject currentLookAtHandler;
	private float currentLookAtHandlerClickTime;

	public override void Process()
	{
		HandleLook();
		HandleSelection();
	}

	void HandleLook()
	{
		if (pointerEventData == null)
		{
			pointerEventData = new PointerEventData(eventSystem);
		}
		// fake a pointer always being at the center of the screen


#if UNITY_EDITOR
		pointerEventData.position = new Vector2(Screen.width / 2, Screen.height / 2);
		#else
		pointerEventData.position = new Vector2(UnityEngine.XR.XRSettings.eyeTextureWidth / 2, UnityEngine.XR.XRSettings.eyeTextureHeight / 2);
#endif

		pointerEventData.delta = Vector2.zero;
		List<RaycastResult> raycastResults = new List<RaycastResult>();
		eventSystem.RaycastAll(pointerEventData, raycastResults);
		CurrentRaycast = pointerEventData.pointerCurrentRaycast = FindFirstRaycast(raycastResults);

		//Debug.Log(CurrentRaycast.gameObject);

		ProcessMove(pointerEventData);
	}

	void HandleSelection()
	{
		if (pointerEventData.pointerEnter != null)
		{
			// if the ui receiver has changed, reset the gaze delay timer
			GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointerEventData.pointerEnter);
			if (currentLookAtHandler != handler)
			{
				currentLookAtHandler = handler;
				currentLookAtHandlerClickTime = Time.realtimeSinceStartup + GazeTimeInSeconds;
			}

			// if we have a handler and it's time to click, do it now
			if (currentLookAtHandler != null &&
				(mode == Mode.Gaze && Time.realtimeSinceStartup > currentLookAtHandlerClickTime) ||
				(Gamepad.current != null && Gamepad.current[GamepadButton.A].isPressed) ||
				(mode == Mode.Click && (Input.GetKeyDown(KeyCode.Space) )))

			{
				ExecuteEvents.ExecuteHierarchy(currentLookAtHandler, pointerEventData, ExecuteEvents.pointerClickHandler);
				Debug.Log($"{currentLookAtHandler} : {pointerEventData.pointerCurrentRaycast}");
				currentLookAtHandlerClickTime = float.MaxValue;
			}
		}
		else
		{
			currentLookAtHandler = null;
		}
	}


}