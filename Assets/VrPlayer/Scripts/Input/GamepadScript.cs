using Google.XR.Cardboard;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

internal class GamepadScript : MonoBehaviour
{
	public UiController uiCon;
	public VrPlayerController vpCon;

	private bool volumeLongPress = false;
	private float nextUpdate = 0.15f;

	// Update is called once per frame
	void Update()
	{

		//- Gamepad media commands
		var gp = Gamepad.current;
		if (gp == null) return;


		if (gp[GamepadButton.Select].wasPressedThisFrame)
			Application.Quit();

		if (gp[GamepadButton.Y].wasPressedThisFrame)
			uiCon.ToogleUi();

		if (gp[GamepadButton.X].wasPressedThisFrame)
			vpCon.PlayPause();

		if (gp[GamepadButton.B].wasPressedThisFrame)
			Api.Recenter();

		if (gp[GamepadButton.DpadUp].wasPressedThisFrame)
			uiCon.AddZoom(true);
		else
		if (gp[GamepadButton.DpadDown].wasPressedThisFrame)
			uiCon.AddZoom(false);

		if (gp[GamepadButton.DpadLeft].wasPressedThisFrame)
			uiCon.PlayNextFile(true);
		else
		if (gp[GamepadButton.DpadRight].wasPressedThisFrame)
			uiCon.PlayNextFile();

		if (gp[GamepadButton.RightTrigger].wasPressedThisFrame)
			uiCon.Seek(true);
		else
		if (gp[GamepadButton.LeftTrigger].wasPressedThisFrame)
			uiCon.Seek(false);


		// vlume with long pree support
		volumeLongPress = (gp[GamepadButton.RightShoulder].isPressed | gp[GamepadButton.LeftShoulder].isPressed);

		if (volumeLongPress && Time.time >= nextUpdate)
		{
			if (gp[GamepadButton.RightShoulder].isPressed)
				uiCon.AddVolume(true);
			else
			if (gp[GamepadButton.LeftShoulder].isPressed)
				uiCon.AddVolume(false);

			nextUpdate = Time.time + 0.15f;
		}

	}
}

