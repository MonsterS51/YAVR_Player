using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class CameraBeam : MonoBehaviour
{
	private const float _maxDist = 10000;
	private GameObject _targetObject = null;
	public LayerMask worldLayer;


	public float rotationSpeed = 2f;
	float pitch;
	float yaw;

	public void Update()
	{

		try
		{
			if (_targetObject.IsDestroyed()) _targetObject = null;


			if (Physics.Raycast(transform.position, transform.forward, out var hit, _maxDist, worldLayer))
			{
				Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);

				if (_targetObject != hit.transform.gameObject)
				{
					_targetObject?.SendMessage("OnBeamExit");
					_targetObject = hit.transform.gameObject;
					_targetObject?.SendMessage("OnBeamEnter");
				}

			}
			else
			{
				_targetObject?.SendMessage("OnBeamExit");
				_targetObject = null;
			}

			if (Google.XR.Cardboard.Api.IsTriggerPressed)
			{
				Debug.Log("Pressed XR Trigger");
				_targetObject?.SendMessage("OnBeamClick");
				return;
			}

			//- Gamepad
			if (Gamepad.current != null)
			{
				if (Gamepad.current[GamepadButton.X].isPressed)
				{
					Debug.Log("Pressed X");
					_targetObject?.SendMessage("OnBeamClick");
				}
			}

			//- mouse for editor
			if (Input.mousePresent && Input.GetMouseButton(1))
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;

				pitch += rotationSpeed * Input.GetAxis("Mouse Y");
				yaw += rotationSpeed * Input.GetAxis("Mouse X");
				pitch = Mathf.Clamp(pitch, -90f, 90f);
				while (yaw < 0f) yaw += 360f;
				while (yaw >= 360f) yaw -= 360f;
				transform.eulerAngles = new Vector3(-pitch, yaw, 0f);
			}
			else
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"[YAVR] {nameof(CameraBeam)} : Error !");
			Debug.LogException(ex);
		}

	}




}
