using Google.XR.Cardboard;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

internal class GamepadScript : MonoBehaviour
{
	public UiController uiCon;
	public VrPlayerController vpCon;

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

		if (gp[GamepadButton.DpadDown].wasPressedThisFrame)
			uiCon.AddZoom(false);

		if (gp[GamepadButton.DpadLeft].wasPressedThisFrame)
			uiCon.PlayNextFile(true);

		if (gp[GamepadButton.DpadRight].wasPressedThisFrame)
			uiCon.PlayNextFile();

		if (gp[GamepadButton.RightShoulder].wasPressedThisFrame)
			uiCon.AddVolume(true);

		if (gp[GamepadButton.LeftShoulder].wasPressedThisFrame)
			uiCon.AddVolume(false);

		if (gp[GamepadButton.RightTrigger].wasPressedThisFrame)
			uiCon.Seek(true);

		if (gp[GamepadButton.LeftTrigger].wasPressedThisFrame)
			uiCon.Seek(false);


	}
}

