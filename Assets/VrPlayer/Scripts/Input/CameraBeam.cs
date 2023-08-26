using UnityEngine;

public class CameraBeam : MonoBehaviour
{
	private const float _maxDist = 10000;
	private GameObject _targetObject = null;
	public LayerMask worldLayer;


	public float rotationSpeed = 2f;
	float pitch;
	float yaw;

	//- test - block z cam rotation ?

	//private Vector3 startRotation;

	//void Start() { startRotation = transform.rotation.eulerAngles; }
	//void LateUpdate()
	//{
	//	Vector3 newRotation = transform.rotation.eulerAngles;
	//	transform.rotation = Quaternion.Euler(
	//		newRotation.x,
	//		newRotation.y,
	//		startRotation.z
	//	);
	//}

	public void Update()
	{

		try
		{

			//if (Physics.Raycast(transform.position, transform.forward, out var hit, _maxDist, worldLayer))
			//{
			//	Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);

			//	if (_targetObject != hit.transform.gameObject)
			//	{
			//		_targetObject?.SendMessage("OnBeamExit", SendMessageOptions.DontRequireReceiver);
			//		_targetObject = hit.transform.gameObject;
			//		_targetObject?.SendMessage("OnBeamEnter", SendMessageOptions.DontRequireReceiver);
			//	}

			//}
			//else
			//{
			//	_targetObject?.SendMessage("OnBeamExit", SendMessageOptions.DontRequireReceiver);
			//	_targetObject = null;
			//}

			//if (Google.XR.Cardboard.Api.IsTriggerPressed)
			//{
			//	Debug.Log("Pressed XR Trigger");
			//	_targetObject?.SendMessage("OnBeamClick", SendMessageOptions.DontRequireReceiver);
			//	return;
			//}

			//- Gamepad
			//if (Gamepad.current != null)
			//{
			//	if (Gamepad.current[GamepadButton.X].isPressed)
			//	{
			//		Debug.Log("Pressed X");
			//		_targetObject?.SendMessage("OnBeamClick", SendMessageOptions.DontRequireReceiver);
			//	}
			//}

#if UNITY_EDITOR
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
#endif

		}
		catch (System.Exception ex)
		{
			Debug.LogError($"[YAVR] {nameof(CameraBeam)} : Error !");
			Debug.LogException(ex);
		}

	}




}
