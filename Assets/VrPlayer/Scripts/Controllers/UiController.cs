using System;
using System.Collections;
using System.IO;
using System.Linq;
using Google.XR.Cardboard;
using UnityEngine;
using UnityEngine.UI;

public class UiController : MonoBehaviour
{

	public VrPlayerController vpCon;
	public MainPanelScript mps;

	public MediaItem curFolderMI = null;

	public GameObject uiRoot;
	public GameObject lowToggleUiBtn;
	public GameObject lowRecenterBtn;

	//- Utils Panel
	public GameObject utilsCanvas;

	void Start()
	{
		utilsCanvas.transform.LookAt(Camera.main.transform);
		var q = utilsCanvas.transform.rotation;
		utilsCanvas.transform.rotation = Quaternion.Euler(0, q.eulerAngles.y + 180, 0);

		lowToggleUiBtn.GetComponent<Button>().onClick.AddListener(() => { ToogleUi(); });
		lowRecenterBtn.GetComponent<Button>().onClick.AddListener(() => { StartCoroutine(RecenterTimed()); });
	}


	void OnDestroy()
	{
		curFolderMI?.CancelParseChildMedia();
	}

	//---

	public void ToogleUi()
	{
		uiRoot.SetActive(!uiRoot.activeInHierarchy);

		//- instantly update progress on show main panel
		if (IsUiEnabled) mps.UpdateProgressUI();
	}

	private IEnumerator RecenterTimed()
	{
		yield return new WaitForSeconds(2f);
		try
		{
			Api.Recenter();
			Vibration.Vibrate(20);
		}
		catch (Exception) { }

	}

	public bool IsUiEnabled { get { return uiRoot.activeInHierarchy; } }


	public MediaItem curPlayedMI = null;

	public void OpenMediaFile(MediaItem mi, bool autoPlay = true)
	{
		//- parse for info string
		mi.StartParse();

		//- auto detect VR video type by name
		vpCon.svm.SetModeByFileName(mi.name.ToLower());

		curFolderMI?.CancelParseChildMedia();

		VrPlayerController.sdm.sd.LastFile = mi.media.Mrl;

		vpCon.Stop();
		vpCon.Open(mi.media, autoPlay);
		curPlayedMI = mi;

		mps.UiUpdate();
	}

	///<summary> Play next/prev file from current folder. </summary>
	public void PlayNextFile(bool prev = false)
	{
		if (curFolderMI == null) return;
		var curMrl = vpCon?.mediaPlayer?.Media?.Mrl;
		if (string.IsNullOrWhiteSpace(curMrl)) return;

		var curMi = curFolderMI.listSubMI.FirstOrDefault(x => x.media.Mrl.ToLower() == curMrl.ToLower());
		if (curMi == null) return;

		var curInd = curFolderMI.listSubMI.IndexOf(curMi);
		MediaItem nextMI = null;

		var before = curFolderMI.listSubMI.GetRange(0, curInd);
		var after = curFolderMI.listSubMI.GetRange(curInd + 1, curFolderMI.listSubMI.Count - curInd - 1);
		var total = after.Concat(before).ToList();

		if (prev)
		{
			after.Reverse();
			before.Reverse();
			total = before.Concat(after).ToList();
		}

		for (int i = 0; i < total.Count(); i++)
		{
			var foundMI = total[i];
			if (foundMI.isFolder) continue;
			nextMI = foundMI;
			break;
		}
		if (curMi == nextMI) return;
		if (nextMI != null) OpenMediaFile(nextMI);
	}


	public void SetMessageText(string text)
	{
		mps.SetMessageText(text);
	}

	public void AddVolume(bool positive)
	{
		var step = 2;
		vpCon.AddVolume(positive ? step : -step);
		SetMessageText($"Volume: {vpCon.mediaPlayer.Volume} %");
		mps.UiUpdate();
	}

	public void Seek(bool positive)
	{
		var seekVal = vpCon.Seek(positive);
		mps.SetMessageText($"Seek {(positive ? ">>" : "<<")} {seekVal}ms");
		mps.UiUpdate();
	}

	public void AddZoom(bool positive = true)
	{
		vpCon.svm.AddZoom(positive);
		mps.SetMessageText($"Zoom {vpCon.svm.ZoomPercent} {(positive ? "+" : "-")}");
		mps.UiUpdate();
	}

}
