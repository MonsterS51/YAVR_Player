using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UtilsPanelScript : MonoBehaviour
{
	public VrPlayerController vpCon;
	public UiController uiCon;

	public GameObject zoomPlusBtn;
	public GameObject zoomMinusBtn;
	public GameObject zoomResetBtn;
	public GameObject zoomText;

	public GameObject fovPlusBtn;
	public GameObject fovMinusBtn;
	public GameObject fovResetBtn;
	public GameObject fovText;

	void Start()
	{
		zoomMinusBtn.GetComponent<Button>().onClick.AddListener(() => { uiCon.AddZoom(false); UiUpdate(); });
		zoomPlusBtn.GetComponent<Button>().onClick.AddListener(() => { uiCon.AddZoom(true); UiUpdate(); });
		zoomResetBtn.GetComponent<Button>().onClick.AddListener(vpCon.ResetZoom);

		fovMinusBtn.GetComponent<Button>().onClick.AddListener(() => { UnityEngine.XR.XRDevice.fovZoomFactor -= 0.025f; });
		fovPlusBtn.GetComponent<Button>().onClick.AddListener(() => { UnityEngine.XR.XRDevice.fovZoomFactor += 0.025f; });
		fovResetBtn.GetComponent<Button>().onClick.AddListener(() => { UnityEngine.XR.XRDevice.fovZoomFactor = 1f; });

		InvokeRepeating(nameof(UiUpdate), 0f, 0.5f);
	}

	private void UiUpdate()
	{
		if (!uiCon.IsUiEnabled) return;
		var size = vpCon.sphere.transform.localScale.z;
		var posV = vpCon.sphere.transform.position;
		var zoomPerc = 100 - (posV.z / size) * 100f;
		zoomText.GetComponentInChildren<TextMeshProUGUI>().text = $"{zoomPerc:0} %";

		fovText.GetComponentInChildren<TextMeshProUGUI>().text = $"{UnityEngine.XR.XRDevice.fovZoomFactor}";
	}
}

