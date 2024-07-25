using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FilesPanelScript : MonoBehaviour
{
	public VrPlayerController vpCon;
	public UiController uiCon;

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

	//- fps
	[SerializeField] private float _fpsRefreshRate = 0.2f;
	private float _fpsTimer;

	void Start()
	{
		refreshButton.GetComponent<Button>().onClick.AddListener(() =>
		{
			RefreshContent();
		});

		contentScrollBar.GetComponent<Scrollbar>().onValueChanged.AddListener((v) =>
		{
			RememberScrollPos();
		});

		upButton.GetComponent<Button>().onClick.AddListener(() => { GoUp(); });

		fileCanvas.transform.LookAt(Camera.main.transform);
		Quaternion q = fileCanvas.transform.rotation;
		fileCanvas.transform.rotation = Quaternion.Euler(0, q.eulerAngles.y + 180, 0);

		TrySetLastState();

		InvokeRepeating(nameof(UiUpdate), 0f, 1f);
	}

	void Update()
	{
		if (!uiCon.IsUiEnabled) return;
		if (Time.unscaledTime <= _fpsTimer) return;
		
		int fps = (int)(1f / Time.unscaledDeltaTime);
		_fpsTimer = Time.unscaledTime + _fpsRefreshRate;
		var tmpFps = textFPS.GetComponentInChildren<TextMeshProUGUI>();
		tmpFps.text = $"FPS: {fps}";
	}

	private void UiUpdate()
	{
		if (!uiCon.IsUiEnabled) return;

		//- parse indication
		if (uiCon.curFolderMI != null && uiCon.curFolderMI.parseChildInProgress)
			textParse.SetActive(true);
		else
			textParse.SetActive(false);

		//- buffering indication
		if (VrPlayerController.isBuffering)
		{
			textBuffering.SetActive(true);
			VrPlayerController.isBuffering = false;
		}
		else
			textBuffering.SetActive(false);

		//- update buttons info
		if (uiCon.curFolderMI != null && !uiCon.curFolderMI.parseInProgress)
		{
			try
			{
				var fileBtns = filesListContent.GetComponentsInChildren<Button>(true);
				foreach (var subMi in uiCon.curFolderMI.listSubMI)
				{
					var fileBtn = fileBtns.FirstOrDefault(x => x.name == subMi.name);
					if (fileBtn == null) continue;
					UpdateFileButtonInfoLine(fileBtn.gameObject, subMi.mediaInfo);

					if (subMi.isFolder) continue;

					if (vpCon.mediaPlayer != null && vpCon.mediaPlayer.Media != null)
					{
						var firstLine = fileBtn.transform.Find("FirstLineText")?.GetComponentInChildren<TextMeshProUGUI>();
						if (firstLine != null)
						{
							if (uiCon.curPlayedMI?.media.Mrl.ToLower() == subMi.media.Mrl.ToLower()) firstLine.color = Color.cyan;
							else firstLine.color = Color.white;
						}
					}
				}
			}
			catch (Exception ex) {
				Debug.LogException(ex);
			}
		}
	}

	//---

	private void RefillFilesPanel()
	{
		if (uiCon.curFolderMI == null) return;
		ClearFilesContentPanel();
		FillFilesListContainer(uiCon.curFolderMI.listSubMI);
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
				entBtn = CreateFileEntryBtn(mi, () => { uiCon.OpenMediaFile(mi); });
			}
			entBtn.transform.SetParent(filesListContent.transform, false);
		}

		if (uiCon.curFolderMI != null) StartCoroutine(SetLastScrollPosCoroutine());
	}

	///<summary> Update info line, if needed. </summary>
	private void UpdateFileButtonInfoLine(GameObject go, string text)
	{
		var secondLine = go.transform.Find("SecondLineText")?.GetComponentInChildren<TextMeshProUGUI>();
		if (secondLine != null) secondLine.text = text;
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

		var firstLine = newButton.transform.Find("FirstLineText")?.GetComponentInChildren<TextMeshProUGUI>();
		if (firstLine != null) firstLine.text = mi.name;

		UpdateFileButtonInfoLine(newButton, mi.mediaInfo);

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

		return newButton;
	}

	private void GoUp()
	{
		if (uiCon.curFolderMI != null && uiCon.curFolderMI.parentMI != null)
		{
			OpenFolder(uiCon.curFolderMI.parentMI);
			return;
		}
		else
		{
			uiCon.curFolderMI = null;
			RefreshContent();
		}
	}

	///<summary> Remember last scroll pos. </summary>
	private void RememberScrollPos()
	{
		if (uiCon.curFolderMI == null) return;
		uiCon.curFolderMI.lastScrollPos = contentScrollBar.GetComponent<Scrollbar>().value;

		if (vpCon.sd == null) return;
		vpCon.sd.FilesListScroll = uiCon.curFolderMI.lastScrollPos;
	}


	//---

	#region Open/Refresh Folder

	private void OpenFolder(MediaItem mi)
	{
		Debug.Log($"[YAVR] {nameof(OpenFolder)} : {mi.name}");

		uiCon.curFolderMI?.CancelParseChildMedia();

		uiCon.curFolderMI = mi;

		if (mi != null) StartCoroutine(OpenFolderCoroutine(mi));
	}

	///<summary> Magic coroutine to set scroll position. </summary>
	IEnumerator SetLastScrollPosCoroutine()
	{
		yield return new WaitForEndOfFrame();
		if (uiCon.curFolderMI == null) scrollRectGo.verticalNormalizedPosition = 1f;
		scrollRectGo.verticalNormalizedPosition = uiCon.curFolderMI.lastScrollPos;
	}

	private IEnumerator OpenFolderCoroutine(MediaItem mi)
	{
		uiCon.SetMessageText($"Open Folder: {mi.name}");
		ClearFilesContentPanel();

		var header = $"{mi.name} ({mi.listSubMI.Count} ent)";
		currentFolderTitle.GetComponent<TextMeshProUGUI>().text = header;

		var parseTask = mi.StartParse();
		while (parseTask.Status == TaskStatus.Running ||
			parseTask.Status == TaskStatus.Created ||
			parseTask.Status == TaskStatus.WaitingToRun ||
			parseTask.Status == TaskStatus.WaitingForActivation
			)
			yield return new WaitForSeconds(.1f);

		//- start parse all sub items
		mi.StartParseChildMedia();

		RefillFilesPanel();
		UiUpdate();
	}



	private void RefreshContent()
	{
		Debug.Log($"[YAVR]: {nameof(RefreshContent)}");

		if (uiCon.curFolderMI != null)
		{
			StartCoroutine(RefreshMiCoroutine(uiCon.curFolderMI));
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

		var parseTask = uiCon.curFolderMI.ReparseMedia();
		while (parseTask.Status == TaskStatus.Running ||
			parseTask.Status == TaskStatus.Created ||
			parseTask.Status == TaskStatus.WaitingToRun ||
			parseTask.Status == TaskStatus.WaitingForActivation
			)
			yield return new WaitForSeconds(.1f);

		//- start parse all sub items - only for thumbnail
		mi.StartParseChildMedia();

		RefillFilesPanel();
		UiUpdate();
	}

	#endregion

	//---

	#region Last State

	public void TrySetLastState()
	{

		var decodedUrl = HttpUtility.UrlDecode(vpCon.sd.LastFile);
		Debug.Log("Try Set LastFile: " + decodedUrl);

		if (string.IsNullOrWhiteSpace(vpCon.sd?.LastFile))
		{
			currentFolderTitle.GetComponent<TextMeshProUGUI>().text = "Root";
			RefreshContent();
		}
		else
		{
			StartCoroutine(SetLastFolderCoroutine());
		}
	}


	private IEnumerator SetLastFolderCoroutine()
	{
		fileCanvas.SetActive(false);


		var folderUri = new Uri(vpCon.sd.LastFile);
		//Debug.Log($"LastFolder Host: {folderUri.Host}");

		var uriSegments = new List<string>();
		if (!string.IsNullOrWhiteSpace(folderUri.Host)) uriSegments.Add(folderUri.Host);
		uriSegments.AddRange(folderUri.Segments);

		//Debug.Log($"LastFolder Segments: {string.Join(",\n", uriSegments)}");

		bool isRoot = true;
		foreach (var uriSeg in uriSegments)
		{
			var curFolder = uriSeg.Trim('\\', '/');
			if (string.IsNullOrWhiteSpace(curFolder)) continue;

			var decodedUrl = HttpUtility.UrlDecode(curFolder);
			Debug.Log($" > Try Open: {decodedUrl}");
			uiCon.SetMessageText($"Try Open: {decodedUrl}");

			if (uiCon.curFolderMI != null)
			{
				var task = uiCon.curFolderMI.StartParse();
				while (task.Status == TaskStatus.Running ||
					task.Status == TaskStatus.Created ||
					task.Status == TaskStatus.WaitingToRun ||
					task.Status == TaskStatus.WaitingForActivation
					)
				{
					yield return new WaitForSeconds(.1f);
				}
			}

			List<MediaItem> miList;
			if (isRoot)
			{
				miList = vpCon.mm.GetRootMediaItems();

				//- try wait lan folders
				if (!folderUri.AbsolutePath.StartsWith("file") && !miList.Any(x => x.media.Mrl.ToLower().EndsWith(curFolder.ToLower())))
				{
					yield return new WaitForSeconds(1f);
					miList = vpCon.mm.GetRootMediaItems();
				}

				isRoot = false;
			}
			else
			{
				miList = uiCon.curFolderMI.listSubMI;
			}

			if (curFolder.EndsWith(':')) curFolder = curFolder + "/";

			//Debug.Log($"   >>> Search <{curFolder.ToLower()}> in list:\n   {string.Join("\n   ", miList.Select(x => x.media.Mrl))}");

			var nextMI = miList.FirstOrDefault(mi => mi.media.Mrl.ToLower().EndsWith("/" + curFolder.ToLower()));
			if (nextMI == null) break;

			//Debug.Log($"   >>> nextMI: {nextMI}");

			if (nextMI.isFolder) uiCon.curFolderMI = nextMI;
			else uiCon.OpenMediaFile(nextMI, false);
		}

		if (uiCon.curFolderMI != null)
		{
			if (vpCon.sd != null) uiCon.curFolderMI.lastScrollPos = vpCon.sd.FilesListScroll;	// recall scroll position
			StartCoroutine(OpenFolderCoroutine(uiCon.curFolderMI));
		}
		else
		{
			currentFolderTitle.GetComponent<TextMeshProUGUI>().text = "Root";
			RefreshContent();
		}

		uiCon.SetMessageText("Load done");

		fileCanvas.SetActive(true);
	}

	#endregion



}

