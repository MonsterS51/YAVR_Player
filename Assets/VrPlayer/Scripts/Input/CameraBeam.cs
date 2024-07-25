using UnityEngine;

public class CameraBeam : MonoBehaviour
{
	private const float _maxDist = 10000;
	public LayerMask worldLayer;

	public float rotationSpeed = 2f;
	float pitch;
	float yaw;

	public void Update()
	{

		try
		{
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
