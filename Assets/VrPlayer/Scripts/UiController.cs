using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibVLCSharp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MediaManager;

public class UiController : MonoBehaviour
{

	public VrPlayerController vpCon;

	//- Main Panel
	public GameObject mediaTitle;
	public GameObject playButton;
	public GameObject stopButton;
	public GameObject seekPrevButton;
	public GameObject seekForwardButton;

	//- Files List Panel
	public GameObject currentFolderTitle;
	public GameObject upButton;
	public GameObject refreshButton;
	public GameObject filesListContent;
	public GameObject fileButtonPrefab;

	void Awake()
	{
		playButton.GetComponent<Button>().onClick.AddListener(() => PlayPauseBtnPush());
		stopButton.GetComponent<Button>().onClick.AddListener(() => { vpCon.Stop(); });
		seekPrevButton.GetComponent<Button>().onClick.AddListener(() => { vpCon.Seek(-10000); });
		seekForwardButton.GetComponent<Button>().onClick.AddListener(() => { vpCon.Seek(10000); });

		
		refreshButton.GetComponent<Button>().onClick.AddListener(() => { UpdateRootContent(); });
		upButton.GetComponent<Button>().onClick.AddListener(() => { GoUp(); });

	}


	// Start is called before the first frame update
	void Start()
    {
		currentFolderTitle.GetComponent<TextMeshProUGUI>().text = "Root";
		UpdateTitle();
		UpdateRootContent();
	}

	// Update is called once per frame
	void Update()
    {
		FilesPanelUpdate();
	}

	private void PlayPauseBtnPush()
	{
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
		var tabBtns = filesListContent.GetComponentsInChildren<Button>(true);
		foreach (var btn in tabBtns) Destroy(btn.gameObject);
	}

	private void FillFilesListContainer(List<MediaItem> miList)
	{
		foreach (var mi in miList)
		{
			GameObject entBtn;
			if (mi.isFolder)
			{
				entBtn = CreateFileEntryBtn(mi.name, () => { OpenFolder(mi); });
			}
			else
			{
				entBtn = CreateFileEntryBtn(mi.name, () => { vpCon.Open(mi.media); UpdateTitle(); });
			}
			entBtn.transform.SetParent(filesListContent.transform, false);
		}
	}

	private void UpdateRootContent()
	{
		Debug.Log($"[YAVR]: {nameof(UpdateRootContent)}");

		if (currentMI != null)
		{
			needPanelUpdate = true;
			FilesPanelUpdate();
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
		else {
			currentMI = null;
			UpdateRootContent(); 
		}
	}


	///<summary> Создание кнопки вкладки. </summary>
	private GameObject CreateFileEntryBtn(string name, Action action)
	{
		var newButton = Instantiate(fileButtonPrefab);
		newButton.name = name;

		var btnTextObj = newButton.GetComponentInChildren<TextMeshProUGUI>();
		btnTextObj.text = name;

		var btnComp = newButton.GetComponent<Button>();
		btnComp.onClick.AddListener(() => action?.Invoke());

		return newButton;
	}

	private MediaItem currentMI = null;
	private bool needPanelUpdate = false;
	private Task<MediaParsedStatus> mediaParseTask;
	private void OpenFolder(MediaItem mi)
	{
		Debug.Log($"[YAVR] {nameof(OpenFolder)} : {mi.name}");
		currentMI = mi;
		needPanelUpdate = true;
		ClearFilesContentPanel();
		currentFolderTitle.GetComponent<TextMeshProUGUI>().text = mi.name;

		mediaParseTask = mi.media.ParseAsync(VrPlayerController.libVLC, (mi.isNetwork ? MediaParseOptions.ParseNetwork : MediaParseOptions.ParseLocal));
	}

	private void FilesPanelUpdate()
	{
		if (currentMI == null) return;
		if (!needPanelUpdate) return;
		ClearFilesContentPanel();


		if (mediaParseTask == null) return;

		Debug.Log($"[YAVR] FilesPanelUpdate Parse : {mediaParseTask.Result}");

		//if (mediaParseTask.Result != MediaParsedStatus.Done) return;

		FillFilesListContainer(currentMI.listSubMI);

		needPanelUpdate = false;
	}


}
