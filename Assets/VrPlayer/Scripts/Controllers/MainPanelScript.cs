using System.Timers;
using LibVLCSharp;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainPanelScript : MonoBehaviour
{
	public VrPlayerController vpCon;
	public UiController uiCon;

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
	public GameObject msgText;

	private bool _isDraggingSeekBar;

	void Start()
	{
		playButton.GetComponent<Button>().onClick.AddListener(() => PlayPauseBtnPush());
		stopButton.GetComponent<Button>().onClick.AddListener(() => Stop() );
		seekPrevButton.GetComponent<Button>().onClick.AddListener(() => uiCon.Seek(false) );
		seekForwardButton.GetComponent<Button>().onClick.AddListener(() => uiCon.Seek(true));
		nextFileButton.GetComponent<Button>().onClick.AddListener(() => uiCon.PlayNextFile());
		prevFileButton.GetComponent<Button>().onClick.AddListener(() => uiCon.PlayNextFile(true));

		//- progressBar events config
		var seekBarEvents = progressBar.GetComponent<EventTrigger>();
		EventTrigger.Entry seekBarPointerDown = new();
		seekBarPointerDown.eventID = EventTriggerType.PointerDown;
		seekBarPointerDown.callback.AddListener((data) =>
		{
			_isDraggingSeekBar = true;
		});
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


	private void UiUpdate()
	{
		if (msgTimer > 0) msgTimer -= 1;
		else msgText.SetActive(false);

		if (!uiCon.IsUiEnabled) return;

		UpdateTitle(uiCon.curPlayedMI);

		UpdateProgressUI();
	}

	public void UpdateProgressUI()
	{
		if (_isDraggingSeekBar || vpCon?.mediaPlayer == null || !vpCon.mediaPlayer.IsPlaying) return;
		progressBar.value = (float)vpCon.mediaPlayer.Position;
		UpdateMediaTime();
	}

	public void UpdateTitle(MediaItem mi)
	{
		if (mi?.media == null) return;
		var title = mi.media.Meta(MetadataType.Title);
		mediaTitle.GetComponent<TextMeshProUGUI>().SetText(title);
		SetMediaInfoText(mi.mediaInfo);
	}

	public void SetMediaInfoText(string text = "")
	{
		mediaInfo.GetComponent<TextMeshProUGUI>().SetText(text);
	}

	private void PlayPauseBtnPush()
	{
		if (vpCon.mediaPlayer == null) return;
		if (vpCon.mediaPlayer.IsPlaying)
		{
			vpCon.mediaPlayer.Pause();
		}
		else vpCon.Play();
	}

	public void Stop()
	{
		vpCon.Stop();
		progressBar.value = 0;
		ClearMediaTime();
		SetMediaInfoText();
	}



	public void UpdateMediaTime()
	{
		if (vpCon.mediaPlayer?.Media == null) return;
		var totTime = vpCon.mediaPlayer.Media.Duration;
		totalTime.GetComponent<TextMeshProUGUI>().SetText(VrPlayerController.GetFormatedTimeStr(totTime));

		var curTime = vpCon.mediaPlayer.Time;
		currentTime.GetComponent<TextMeshProUGUI>().SetText(VrPlayerController.GetFormatedTimeStr(curTime));
	}

	public void ClearMediaTime()
	{
		totalTime.GetComponent<TextMeshProUGUI>().SetText("00:00");
		currentTime.GetComponent<TextMeshProUGUI>().SetText("00:00");
	}

	private float msgTimer = 2;
	public void SetMessageText(string text)
	{
		if (string.IsNullOrWhiteSpace(text)) msgText.SetActive(false);
		else {
			var tmp = msgText.GetComponentInChildren<TextMeshProUGUI>();
			if (tmp != null) tmp.text = text;
			msgText.SetActive(true);
			msgTimer = 2;
		}
	}


}

