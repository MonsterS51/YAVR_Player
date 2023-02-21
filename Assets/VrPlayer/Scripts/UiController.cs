using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.XR.Cardboard;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class UiController : MonoBehaviour
{

	public VrPlayerController vpCon;

	public GameObject reticle;

	public GameObject uiRoot;


	//- Main Panel
	public GameObject mediaTitle;
	public GameObject playButton;
	public GameObject stopButton;
	public GameObject seekPrevButton;
	public GameObject seekForwardButton;
	public GameObject nextFileButton;
	public GameObject prevFileButton;
	public Slider progressBar;
	public GameObject currentTime;
	public GameObject totalTime;
	public GameObject mediaInfo;


	//- Files List Panel
	public GameObject fileCanvas;
	public GameObject currentFolderTitle;
	public GameObject upButton;
	public GameObject refreshButton;
	public GameObject filesListContent;
	public GameObject fileButtonPrefab;
	public GameObject contentScrollBar;
	public ScrollRect scrollRectGo;
	public GameObject textParse;
	public GameObject textBuffering;
	public GameObject textFPS;

	//- Utils Panel
	public GameObject utilsCanvas;
	public GameObject clearThumbBtn;
	public GameObject Btn180;
	public GameObject Btn360;
	public GameObject BtnSBS;
	public GameObject BtnOU;

	//- fps
	[SerializeField] private float _fpsRefreshRate = 0.5f;
	private float _fpsTimer;

	private string mediaInfoStr = string.Empty;

	void Awake() { }

	private bool _isDraggingSeekBar;

	// Start is called before the first frame update
	void Start()
	{

		playButton.GetComponent<Button>().onClick.AddListener(() => PlayPauseBtnPush());
		stopButton.GetComponent<Button>().onClick.AddListener(() => { Stop(); });
		seekPrevButton.GetComponent<Button>().onClick.AddListener(() => { vpCon.Seek(-10000); });
		seekForwardButton.GetComponent<Button>().onClick.AddListener(() => { vpCon.Seek(10000); });

		nextFileButton.GetComponent<Button>().onClick.AddListener(() => { PlayNextFile(); });
		prevFileButton.GetComponent<Button>().onClick.AddListener(() => { PlayNextFile(true); });



		refreshButton.GetComponent<Button>().onClick.AddListener(() => { RefreshContent(); });
		upButton.GetComponent<Button>().onClick.AddListener(() => { GoUp(); });

		fileCanvas.transform.LookAt(Camera.main.transform);
		Quaternion q = fileCanvas.transform.rotation;
		fileCanvas.transform.rotation = Quaternion.Euler(0, q.eulerAngles.y + 180, 0);

		utilsCanvas.transform.LookAt(Camera.main.transform);
		q = utilsCanvas.transform.rotation;
		utilsCanvas.transform.rotation = Quaternion.Euler(0, q.eulerAngles.y + 180, 0);

		clearThumbBtn.GetComponent<Button>().onClick.AddListener(() => { ClearThumbnailsCache(); });

		BtnSBS.GetComponent<Button>().onClick.AddListener(() => { vpCon.SetVideoLayout(true); });
		BtnOU.GetComponent<Button>().onClick.AddListener(() => { vpCon.SetVideoLayout(false); });
		Btn180.GetComponent<Button>().onClick.AddListener(() => { vpCon.SetImageType(false); });
		Btn360.GetComponent<Button>().onClick.AddListener(() => { vpCon.SetImageType(true); });


		currentFolderTitle.GetComponent<TextMeshProUGUI>().text = "Root";
		UpdateTitle();
		RefreshContent();

		//- progressBar events config
		var seekBarEvents = progressBar.GetComponent<EventTrigger>();
		EventTrigger.Entry seekBarPointerDown = new();
		seekBarPointerDown.eventID = EventTriggerType.PointerDown;
		seekBarPointerDown.callback.AddListener((data) => { _isDraggingSeekBar = true; });
		seekBarEvents.triggers.Add(seekBarPointerDown);

		EventTrigger.Entry seekBarPointerUp = new();
		seekBarPointerUp.eventID = EventTriggerType.PointerUp;
		seekBarPointerUp.callback.AddListener((data) =>
		{
			if (vpCon.mediaPlayer.IsPlaying) vpCon?.mediaPlayer?.SetPosition(progressBar.value);
			_isDraggingSeekBar = false;
		});
		seekBarEvents.triggers.Add(seekBarPointerUp);

		InvokeRepeating(nameof(UiUpdate), 0f, 1f);
	}

	private void UpdateMediaInfo()
	{
		mediaInfoStr = string.Empty;
		var vt = vpCon.mediaPlayer.SelectedTrack(LibVLCSharp.TrackType.Video);
		if (vt != null)
		{
			mediaInfoStr += $"{vt.Data.Video.Width}x{vt.Data.Video.Height}";
			if (vt.Data.Video.FrameRateNum > 0 && vt.Data.Video.FrameRateNum < 500) mediaInfoStr += $" {vt.Data.Video.FrameRateNum}fps";
		}
	}

	// Update is called once per frame
	void Update()
	{
		//- Gamepad media commands
		if (Gamepad.current != null)
		{
			if (Gamepad.current[GamepadButton.Y].wasPressedThisFrame) uiRoot.SetActive(!uiRoot.activeInHierarchy);
			if (Gamepad.current[GamepadButton.X].wasPressedThisFrame)
			{
				if (vpCon.mediaPlayer.IsPlaying) vpCon.mediaPlayer.Pause();
				else vpCon.Play();
			}
			if (Gamepad.current[GamepadButton.B].wasPressedThisFrame) Api.Recenter();
		}

		if (uiRoot.activeInHierarchy && Time.unscaledTime > _fpsTimer)
		{
			int fps = (int)(1f / Time.unscaledDeltaTime);
			_fpsTimer = Time.unscaledTime + _fpsRefreshRate;
			var tmpFps = textFPS.GetComponentInChildren<TextMeshProUGUI>();
			tmpFps.text = $"FPS: {fps}";
		}

	}

	void OnDestroy()
	{
		curFolderMI?.CancelParseChildMedia();
	}

	//---

	private void UiUpdate()
	{
		if (uiRoot.activeInHierarchy)
		{

			if (!_isDraggingSeekBar && vpCon?.mediaPlayer != null && vpCon.mediaPlayer.IsPlaying)
			{
				progressBar.value = (float)vpCon.mediaPlayer.Position;
				UpdateMediaInfo();
				UpdateMediaTime();
			}

			//- parse indication
			if (curFolderMI != null && curFolderMI.parseChildInProgerss)
				textParse.SetActive(true);
			else
				textParse.SetActive(false);

			//- buffering indication
			if (vpCon.isBuffering)
			{
				textBuffering.SetActive(true);
				vpCon.isBuffering = false;
			}
			else
				textBuffering.SetActive(false);


			//- update thumbnails in buttons
			if (curFolderMI != null)
			{
				var fileBtns = filesListContent.GetComponentsInChildren<Button>(true);
				foreach (var subMi in curFolderMI.listSubMI)
				{
					if (subMi.isFolder) continue;
					var fileBtn = fileBtns.FirstOrDefault(x => x.name == subMi.name);
					if (fileBtn != null)
					{
						UpdateFileButtonThumbnail(fileBtn.gameObject, subMi.GetThumbnailFromCache());
					}
				}
			}




		}
	}


	private void PlayPauseBtnPush()
	{
		if (vpCon.mediaPlayer == null) return;
		if (vpCon.mediaPlayer.IsPlaying)
		{
			vpCon.mediaPlayer.Pause();
		}
		else vpCon.Play();
		UpdateTitle();
	}

	private void Stop()
	{
		vpCon.Stop();
		progressBar.value = 0;
		ClearMediaTime();
	}

	private void UpdateTitle()
	{
		var title = vpCon.GetCurrentPlayedTitle();
		mediaTitle.GetComponent<TextMeshProUGUI>().SetText(title);
	}

	private void GoUp()
	{
		if (curFolderMI != null && curFolderMI.parentMI != null)
		{
			OpenFolder(curFolderMI.parentMI);
			return;
		}
		else
		{
			curFolderMI = null;
			RefreshContent();
		}
	}


	private void OpenMediaFile(MediaItem mi)
	{
		//- auto detect VR video type by name
		if (mi.name.Contains("360")) vpCon.SetImageType(true);
		else vpCon.SetImageType(false);
		if (mi.name.Contains("_ou") | mi.name.Contains("_OU")) vpCon.SetVideoLayout(false);
		else vpCon.SetVideoLayout(true);

		curFolderMI?.CancelParseChildMedia();
		vpCon.Stop();
		vpCon.Open(mi.media);
		UpdateTitle();
	}

	private void ClearThumbnailsCache()
	{
		try
		{
			var di = new DirectoryInfo(MediaItem.thumbsCachePath);
			if (!di.Exists) return;
			foreach (var file in di.GetFiles())
			{
				file.Delete();
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[YAVR] Failed Clear Thumbnails Cache : " + ex.Message);
		}

	}

	public void PlayNextFile(bool prev = false)
	{
		if (curFolderMI == null) return;
		var curMrl = vpCon?.mediaPlayer?.Media?.Mrl;
		if (string.IsNullOrWhiteSpace(curMrl)) return;

		var curMi = curFolderMI.listSubMI.FirstOrDefault(x => x.media.Mrl == curMrl);
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


	public void UpdateMediaTime()
	{
		if (vpCon.mediaPlayer?.Media == null) return;
		var totTime = vpCon.mediaPlayer.Media.Duration;
		totalTime.GetComponent<TextMeshProUGUI>().SetText(GetFormatedTimeStr(totTime));

		var curTime = vpCon.mediaPlayer.Time;
		currentTime.GetComponent<TextMeshProUGUI>().SetText(GetFormatedTimeStr(curTime));

		mediaInfo.GetComponent<TextMeshProUGUI>().SetText(mediaInfoStr);


	}

	public void ClearMediaTime()
	{
		totalTime.GetComponent<TextMeshProUGUI>().SetText("00:00");
		currentTime.GetComponent<TextMeshProUGUI>().SetText("00:00");
		mediaInfo.GetComponent<TextMeshProUGUI>().SetText(string.Empty);
	}

	public string GetFormatedTimeStr(long timeMs)
	{
		var timespan = TimeSpan.FromMilliseconds(timeMs);
		var totalStr = string.Empty;
		if (timespan.TotalHours >= 1)
			totalStr = string.Format("{0:D2}:{1:D2}:{2:D2}", timespan.Hours, timespan.Minutes, timespan.Seconds);
		else
			totalStr = string.Format("{0}:{1:00}", (int)timespan.TotalMinutes, timespan.Seconds);
		return totalStr;
	}

	//---

	#region Files Panel Content

	private MediaItem curFolderMI = null;

	private void RefillFilesPanel()
	{
		if (curFolderMI == null) return;
		ClearFilesContentPanel();
		FillFilesListContainer(curFolderMI.listSubMI);
	}

	private void FillFilesListContainer(List<MediaItem> miList)
	{
		foreach (var mi in miList)
		{
			GameObject entBtn;
			if (mi.isFolder)
			{
				entBtn = CreateFileEntryBtn(mi, () => { OpenFolder(mi); });
			}
			else
			{
				entBtn = CreateFileEntryBtn(mi, () => { OpenMediaFile(mi); });
			}
			entBtn.transform.SetParent(filesListContent.transform, false);
		}

		if (curFolderMI != null) StartCoroutine(SetLastScrollPosCoroutine());
	}

	///<summary> Update the preview texture on the file button. </summary>
	private void UpdateFileButtonThumbnail(GameObject go, Texture2D thumbTex)
	{
		if (thumbTex == null) return;

		//- hide icon
		var iconimage = go.transform.Find("IconFolder");
		iconimage.gameObject.SetActive(false);
		iconimage = go.transform.Find("IconFile");
		iconimage.gameObject.SetActive(false);

		//- show thumbnail
		var thumbImage = go.transform.Find("Thumbnail");
		thumbImage.gameObject.SetActive(true);
		var rawImage = thumbImage?.GetComponent<RawImage>();
		if (rawImage != null) rawImage.texture = thumbTex;

	}

	private void ClearFilesContentPanel()
	{
		var fileBtns = filesListContent.GetComponentsInChildren<Button>(true);
		foreach (var btn in fileBtns) Destroy(btn.gameObject);
	}


	///<summary> Create file button. </summary>
	private GameObject CreateFileEntryBtn(MediaItem mi, Action action)
	{
		var newButton = Instantiate(fileButtonPrefab);
		newButton.name = mi.name;

		var btnTextObj = newButton.GetComponentInChildren<TextMeshProUGUI>();
		btnTextObj.text = mi.name;

		var btnComp = newButton.GetComponent<Button>();
		btnComp.onClick.AddListener(() => action?.Invoke());

		if (mi.isFolder)
		{
			var iconimage = newButton.transform.Find("IconFolder");
			iconimage.gameObject.SetActive(true);
			iconimage = newButton.transform.Find("IconFile");
			iconimage.gameObject.SetActive(false);
		}
		else
		{
			var iconimage = newButton.transform.Find("IconFolder");
			iconimage.gameObject.SetActive(false);
			iconimage = newButton.transform.Find("IconFile");
			iconimage.gameObject.SetActive(true);
		}

		UpdateFileButtonThumbnail(newButton, mi.GetThumbnailFromCache());

		return newButton;
	}

	#endregion

	//---

	#region Open/Refresh Folder

	private void OpenFolder(MediaItem mi)
	{
		Debug.Log($"[YAVR] {nameof(OpenFolder)} : {mi.name}");

		curFolderMI?.CancelParseChildMedia();

		//- remember last scroll pos
		if (curFolderMI != null)
			curFolderMI.lastScrollPos = contentScrollBar.GetComponent<Scrollbar>().value;

		curFolderMI = mi;

		StartCoroutine(OpenFolderCoroutine(mi));
	}

	///<summary> Magic coroutine to set scroll position. </summary>
	IEnumerator SetLastScrollPosCoroutine()
	{
		yield return new WaitForEndOfFrame();
		if (curFolderMI == null) scrollRectGo.verticalNormalizedPosition = 1f;
		scrollRectGo.verticalNormalizedPosition = curFolderMI.lastScrollPos; 
	}

	private IEnumerator OpenFolderCoroutine(MediaItem mi)
	{
		ClearFilesContentPanel();
		currentFolderTitle.GetComponent<TextMeshProUGUI>().text = mi.name;

		var parseTask = mi.StartParse();
		while (parseTask.Status == TaskStatus.Running ||
			parseTask.Status == TaskStatus.Created ||
			parseTask.Status == TaskStatus.WaitingToRun ||
			parseTask.Status == TaskStatus.WaitingForActivation
			)
			yield return new WaitForSeconds(.1f);

		//- start parse all sub items - but for what? 
		//mi.StartParseChildMedia();

		RefillFilesPanel();
		UiUpdate();
	}



private void RefreshContent()
	{
		Debug.Log($"[YAVR]: {nameof(RefreshContent)}");

		if (curFolderMI != null)
		{
			StartCoroutine(RefreshMiCoroutine(curFolderMI));
			return;
		}

		currentFolderTitle.GetComponent<TextMeshProUGUI>().text = "Root";

		ClearFilesContentPanel();

		var miList = vpCon.mm.GetRootMediaItems();
		FillFilesListContainer(miList);
	}

	private IEnumerator RefreshMiCoroutine(MediaItem mi)
	{
		mi.CancelParseChildMedia();
		ClearFilesContentPanel();

		var parseTask = curFolderMI.ReparseMedia();
		while (parseTask.Status == TaskStatus.Running ||
			parseTask.Status == TaskStatus.Created ||
			parseTask.Status == TaskStatus.WaitingToRun ||
			parseTask.Status == TaskStatus.WaitingForActivation
			)
			yield return new WaitForSeconds(.1f);

		//- start parse all sub items - only for thumbnail
		mi.StartParseChildMedia(true);

		RefillFilesPanel();
		UiUpdate();
	}

	#endregion

}
