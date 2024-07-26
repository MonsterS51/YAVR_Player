using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static StereoVrManager;

public class UtilsPanelScript : MonoBehaviour
{
	public VrPlayerController vpCon;
	public UiController uiCon;

	[Header("VR")]
	public GameObject Btn180;
	public GameObject Btn360;
	public GameObject BtnSBS;
	public GameObject BtnOU;
	public GameObject BtnNone;

	[Header("Zoom")]
	public GameObject zoomPlusBtn;
	public GameObject zoomMinusBtn;
	public GameObject zoomResetBtn;
	public GameObject zoomText;

	[Header("Volume")]
	public GameObject volPlusBtn;
	public GameObject volMinusBtn;
	public GameObject volResetBtn;
	public GameObject volText;

	[Header("Gaze Controll")]
	public GameObject gazeToggle;
	public GazeInputModuleX gazeInpuModule;

	[Header("Gamepad Scheme")]
	public GameObject gamepadInfoPanel;

	void Start()
	{
		BtnSBS.GetComponent<Button>().onClick.AddListener(() => { vpCon.svm.SetVideoLayout(StereoMode.SBS); });
		BtnOU.GetComponent<Button>().onClick.AddListener(() => { vpCon.svm.SetVideoLayout(StereoMode.OU); });
		BtnNone.GetComponent<Button>().onClick.AddListener(() => { vpCon.svm.SetVideoLayout(StereoMode.None); });

		Btn180.GetComponent<Button>().onClick.AddListener(() => { vpCon.svm.SetImageType(false); });
		Btn360.GetComponent<Button>().onClick.AddListener(() => { vpCon.svm.SetImageType(true); });

		zoomMinusBtn.GetComponent<Button>().onClick.AddListener(() => { uiCon.AddZoom(false); UiUpdate(); });
		zoomPlusBtn.GetComponent<Button>().onClick.AddListener(() => { uiCon.AddZoom(true); UiUpdate(); });
		zoomResetBtn.GetComponent<Button>().onClick.AddListener(vpCon.svm.ResetZoom);

		volMinusBtn.GetComponent<Button>().onClick.AddListener(() => { uiCon.AddVolume(false); UiUpdate(); });
		volPlusBtn.GetComponent<Button>().onClick.AddListener(() => { uiCon.AddVolume(true); UiUpdate(); });
		volResetBtn.GetComponent<Button>().onClick.AddListener(() => { vpCon.SetVolume(50); UiUpdate(); });

		gazeInpuModule.gazeEnabled = VrPlayerController.sdm.sd.GazeControlEnabled;
		var gazeTogComp = gazeToggle.GetComponent<Toggle>();
		gazeTogComp.isOn = gazeInpuModule.gazeEnabled;
		gazeTogComp.onValueChanged.AddListener((isGazeEnabled) => { 
			gazeInpuModule.gazeEnabled = isGazeEnabled;
			VrPlayerController.sdm.sd.GazeControlEnabled = isGazeEnabled;
		});

		InvokeRepeating(nameof(UiUpdate), 0f, 0.5f);
	}

	private void UiUpdate()
	{
		if (!uiCon.IsUiEnabled) return;
		zoomText.GetComponentInChildren<TextMeshProUGUI>().text = $"{vpCon.svm.ZoomPercent:0} %";
		volText.GetComponentInChildren<TextMeshProUGUI>().text = $"{vpCon.mediaPlayer?.Volume}";
		gamepadInfoPanel.SetActive(Gamepad.current != null && !Gamepad.current.name.Contains("AndroidGamepad"));
	}
}

