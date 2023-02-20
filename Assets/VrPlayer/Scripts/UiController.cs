using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

	//- Files List Panel
	public GameObject fileCanvas;
	public GameObject currentFolderTitle;
	public GameObject upButton;
	public GameObject refreshButton;
	public GameObject filesListContent;
	public GameObject fileButtonPrefab;
	public GameObject contentScrollBar;
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

	void Awake() { }

	private bool _isDraggingSeekBar;

	// Start is called before the first frame update
	void Start()
	{

		playButton.GetComponent<Button>().onClick.AddListener(() => PlayPauseBtnPush());
		stopButton.GetComponent<Button>().onClick.AddListener(() => { vpCon.Stop(); progressBar.value = 0; });
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
		currentMI?.CancelParseChildMedia();
	}

	//---

	private void UiUpdate()
	{
		if (uiRoot.activeInHierarchy)
		{
			FilesPanelUpdate();

			if (!_isDraggingSeekBar && vpCon?.mediaPlayer != null && vpCon.mediaPlayer.IsPlaying)
			{
				progressBar.value = (float)vpCon.mediaPlayer.Position;
			}

			//- parse indication
			if (currentMI != null && currentMI.parseChildInProgerss)
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
			if (currentMI != null)
			{
				var fileBtns = filesListContent.GetComponentsInChildren<Button>(true);
				foreach (var subMi in currentMI.listSubMI)
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


	private void UpdateTitle()
	{
		var title = vpCon.GetCurrentPlayedTitle();
		mediaTitle.GetComponent<TextMeshProUGUI>().SetText(title);
	}

	private void ClearFilesContentPanel()
	{
		var fileBtns = filesListContent.GetComponentsInChildren<Button>(true);
		foreach (var btn in fileBtns) Destroy(btn.gameObject);
	}

	private void FillFilesListContainer(List<MediaItem> miList)
	{
		foreach (var mi in miList)
		{
			GameObject entBtn;
			if (mi.isFolder)
			{
				entBtn = CreateFileEntryBtn(mi.name, true, mi.GetThumbnailFromCache(), () => { OpenFolder(mi); });
			}
			else
			{
				entBtn = CreateFileEntryBtn(mi.name, false, mi.GetThumbnailFromCache(), () => { OpenMediaFile(mi); });
			}
			entBtn.transform.SetParent(filesListContent.transform, false);
		}
	}

	private void RefreshContent()
	{
		Debug.Log($"[YAVR]: {nameof(RefreshContent)}");

		if (currentMI != null)
		{
			currentMI.ReparseMedia();
			currentMI.StartParseChildMedia();
			needPanelUpdate = true;
			UiUpdate();
			return;
		}


		currentMI = null;
		needPanelUpdate = false;

		currentFolderTitle.GetComponent<TextMeshProUGUI>().text = "Root";

		ClearFilesContentPanel();

		var miList = vpCon.mm.GetRootMediaItems();
		FillFilesListContainer(miList);


	}

	private void GoUp()
	{
		if (currentMI != null && currentMI.parentMI != null)
		{
			OpenFolder(currentMI.parentMI);
			return;
		}
		else
		{
			currentMI = null;
			RefreshContent();
		}
	}


	///<summary> Создание кнопки вкладки. </summary>
	private GameObject CreateFileEntryBtn(string name, bool isDirectory, Texture2D icon, Action action)
	{
		var newButton = Instantiate(fileButtonPrefab);
		newButton.name = name;

		var btnTextObj = newButton.GetComponentInChildren<TextMeshProUGUI>();
		btnTextObj.text = name;

		var btnComp = newButton.GetComponent<Button>();
		btnComp.onClick.AddListener(() => action?.Invoke());

		if (isDirectory)
		{
			var iconimage = newButton.transform.Find("IconFolder");
			iconimage.gameObject.SetActive(true);
			iconimage = newButton.transform.Find("IconFile");
			iconimage.gameObject.SetActive(false);
		} else
		{
			var iconimage = newButton.transform.Find("IconFolder");
			iconimage.gameObject.SetActive(false);
			iconimage = newButton.transform.Find("IconFile");
			iconimage.gameObject.SetActive(true);
		}

		UpdateFileButtonThumbnail(newButton, icon);

		return newButton;
	}

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


	private MediaItem currentMI = null;
	private bool needPanelUpdate = false;

	private void OpenFolder(MediaItem mi)
	{
		Debug.Log($"[YAVR] {nameof(OpenFolder)} : {mi.name}");

		currentMI?.CancelParseChildMedia();

		currentMI = mi;
		needPanelUpdate = true;
		ClearFilesContentPanel();
		currentFolderTitle.GetComponent<TextMeshProUGUI>().text = mi.name;

		contentScrollBar.GetComponent<Scrollbar>().value = 1;

		//- parse just to be sure
		var parseTask = mi.StartParse();
		parseTask.Wait();

		//- start parse all sub items
		mi.StartParseChildMedia();

		UiUpdate();
	}

	private void FilesPanelUpdate()
	{
		if (currentMI == null) return;
		if (!needPanelUpdate) return;
		ClearFilesContentPanel();
		FillFilesListContainer(currentMI.listSubMI);
		needPanelUpdate = false;
	}

	private void OpenMediaFile(MediaItem mi)
	{
		//- auto detect VR video type by name
		if (mi.name.Contains("360")) vpCon.SetImageType(true);
		else vpCon.SetImageType(false);
		if (mi.name.Contains("_ou") | mi.name.Contains("_OU")) vpCon.SetVideoLayout(false);
		else vpCon.SetVideoLayout(true);

		currentMI?.CancelParseChildMedia();
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
			Debug.LogError("Failed Clear Thumbnails Cache : " + ex.Message);
		}

	}

	public void PlayNextFile(bool prev = false)
	{
		if (currentMI == null) return;
		var curMrl = vpCon?.mediaPlayer?.Media?.Mrl;
		if (string.IsNullOrWhiteSpace(curMrl)) return;

		var curMi = currentMI.listSubMI.FirstOrDefault(x => x.media.Mrl == curMrl);
		if (curMi == null) return;

		var curInd = currentMI.listSubMI.IndexOf(curMi);
		MediaItem nextMI = null;

		var before = currentMI.listSubMI.GetRange(0, curInd);
		var after = currentMI.listSubMI.GetRange(curInd + 1, currentMI.listSubMI.Count - curInd - 1);
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

}
