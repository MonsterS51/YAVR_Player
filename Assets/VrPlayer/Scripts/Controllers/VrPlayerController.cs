using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.XR.Cardboard;
using LibVLCSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Management;

public class VrPlayerController : MonoBehaviour
{
	public static LibVLC libVLC;    //The LibVLC class is mainly used for making MediaPlayer and Media objects. You should only have one LibVLC instance.
	public MediaPlayer mediaPlayer; //MediaPlayer is the main class we use to interact with VLC

	public GameObject sphere;
	private Material sphereMat;

	//Screens
	public Renderer screen;                 //Assign a mesh to render on a 3d object
	public RawImage canvasScreen;           //Assign a Canvas RawImage to render on a GUI object

	public Texture2D _vlcTexture = null;    //This is the texture libVLC writes to directly. It's private.
	public RenderTexture rt = null;         //We copy it into this texture which we actually use in unity.

	public string path = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"; //Can be a web path or a local path

	public bool flipTextureX = false;   //No particular reason you'd need this but it is sometimes useful
	public bool flipTextureY = true;    //Set to false on Android, to true on Windows

	public bool automaticallyFlipOnAndroid = true;  //Automatically invert Y on Android

	public MediaManager mm = new();

	public static string cachePath;

	#region Unity

	void Awake()
	{
		LoadData();

		sphereMat = sphere.GetComponent<MeshRenderer>().material;
		sphereMat.mainTexture = Texture2D.blackTexture;

		cachePath = Application.temporaryCachePath;

		//- set targetFrameRate to max for smooth camera and less android vsync stutters
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 999;

		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		if (!Api.HasDeviceParams()) Api.ScanDeviceParams();

		Debug.Log($"VR Device: {UnityEngine.XR.XRSettings.loadedDeviceName}");

		//Setup LibVLC
		if (libVLC == null)
			CreateLibVLC();

		//Setup Screen
		if (screen == null)
			screen = GetComponent<Renderer>();
		if (canvasScreen == null)
			canvasScreen = GetComponent<RawImage>();

		//Automatically flip on android
		if (automaticallyFlipOnAndroid && Application.platform == RuntimePlatform.Android)
			flipTextureY = !flipTextureY;

		//Setup Media Player
		CreateMediaPlayer();

		mm._libVLC = libVLC;
		mm.InitializeMediaDiscoverers();
	}

	void OnDestroy()
	{
		SaveData();

		mm.Dispose();

		rtCache.Clear();
		texCache.Clear();

		//- Dispose of mediaPlayer, or it will stay in nemory and keep playing audio
		DestroyMediaPlayer();

		Resources.UnloadUnusedAssets();
	}

	void Update()
	{

		if (_isVrModeEnabled)
		{
			if (Api.IsCloseButtonPressed)
			{
				//ExitVR();
			}

			if (Api.IsGearButtonPressed) Api.ScanDeviceParams();
			Api.UpdateScreenParams();
		}
		else
		{
			// TODO(b/171727815): Add a button to switch to VR mode.
			//if (_isScreenTouched)
			//{
			//	EnterVR();
			//}
		}

		if (mediaPlayer != null)
		{
			//Get size every frame
			uint height = 0;
			uint width = 0;
			mediaPlayer.Size(0, ref width, ref height);

			//Automatically resize output textures if size changes
			if (_vlcTexture == null || _vlcTexture.width != width || _vlcTexture.height != height)
			{
				ResizeOutputTextures(width, height);
			}

			if (_vlcTexture != null)
			{
				//Update the vlc texture(tex)
				var texptr = mediaPlayer.GetTexture(width, height, out bool updated);
				if (updated)
				{
					_vlcTexture.UpdateExternalTexture(texptr);

					//Copy the vlc texture into the output texture, flipped over
					var flip = new Vector2(flipTextureX ? -1 : 1, flipTextureY ? -1 : 1);
					Graphics.Blit(_vlcTexture, rt, flip, Vector2.zero); //If you wanted to do post processing outside of VLC you could use a shader here.
				}
			}
		}

	}

	void OnApplicationFocus(bool hasFocus)
	{
		if (!hasFocus)
		{
			mediaPlayer?.SetPause(true);
			SaveData();
		}
	}

	void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
		{
			mediaPlayer?.SetPause(true);
			SaveData();
		}
	}

	#endregion

	/// <summary>  Gets a value indicating whether the VR mode is enabled. /// </summary>
	private bool _isVrModeEnabled
	{
		get
		{
			return XRGeneralSettings.Instance.Manager.isInitializationComplete;
		}
	}


	//---

	#region Player Actions
	public void Open(string path)
	{
		Debug.Log($"Open {path}");
		this.path = path;

		try
		{
			mediaPlayer?.Media?.Dispose();
			var trimmedPath = path.Trim(new char[] { '"' });    //Windows likes to copy paths with quotes but Uri does not like to open them
			mediaPlayer.Media = new Media(new Uri(trimmedPath));
			Play();
		}
		catch (Exception ex)
		{
			Debug.LogError($"Cant open <{mediaPlayer.Media.Mrl}> : {ex.Message}");
		}

	}

	public void Open(Media media, bool autoPlay = true)
	{
		Debug.Log($"Open Media <{media.Mrl}>");
		if (mediaPlayer.Media != null)
			mediaPlayer.Media.Dispose();
		mediaPlayer.Media = media;
		if (autoPlay) Play();
	}

	public void Play()
	{
		mediaPlayer.Play();
	}

	public void Pause()
	{
		mediaPlayer.Pause();
	}

	public void PlayPause()
	{
		if (mediaPlayer.IsPlaying) mediaPlayer.Pause();
		else Play();
	}

	public void Stop()
	{
		mediaPlayer?.Stop();
		_vlcTexture = null;
		rt = null;
		sphereMat.mainTexture = Texture2D.blackTexture;
	}

	public void Seek(long timeDelta)
	{
		mediaPlayer?.SetTime(mediaPlayer.Time + timeDelta);
	}

	public void SetTime(long time)
	{
		mediaPlayer?.SetTime(time, true);
	}

	public void AddVolume(int volume)
	{
		if (mediaPlayer == null) return;
		var newVol = mediaPlayer.Volume + volume;
		SetVolume(newVol);
	}

	public void SetVolume(int volume)
	{
		if (mediaPlayer == null) return;
		var newVol = volume;
		newVol = Mathf.Clamp(newVol, 0, 100);
		mediaPlayer.SetVolume(newVol);
		if (sd != null) sd.Volume = newVol;
	}

	public bool IsPlaying
	{
		get
		{
			if (mediaPlayer == null)
				return false;
			return mediaPlayer.IsPlaying;
		}
	}

	public long Duration
	{
		get
		{
			if (mediaPlayer == null || mediaPlayer.Media == null)
				return 0;
			return mediaPlayer.Media.Duration;
		}
	}

	public long Time
	{
		get
		{
			if (mediaPlayer == null)
				return 0;
			return mediaPlayer.Time;
		}
	}

	public string GetCurrentPlayedTitle()
	{
		if (mediaPlayer.Media == null) return string.Empty;
		return mediaPlayer.Media.Meta(MetadataType.Title);
	}


	//This returns the video orientation for the currently playing video, if there is one
	public VideoOrientation? GetVideoOrientation()
	{
		var tracks = mediaPlayer?.Tracks(TrackType.Video);

		if (tracks == null || tracks.Count == 0)
			return null;

		var orientation = tracks[0]?.Data.Video.Orientation;    //At the moment we're assuming the track we're playing is the first track

		tracks.Dispose();

		return orientation;
	}

	#endregion

	//---

	#region LibVlc internal

	///<summary> Create a new static LibVLC instance and dispose of the old one. You should only ever have one LibVLC instance.</summary>
	private void CreateLibVLC()
	{
		//Dispose of the old libVLC if necessary
		if (libVLC != null)
		{
			libVLC.Dispose();
			libVLC = null;
			DestroyMediaPlayer();
		}

		Core.Initialize(Application.dataPath);  //Load VLC dlls

		// опции вращения через фильтры тормозят - "--video-filter=rotate", "--rotate-angle=180"

		var options = new string[] { $"--smb-user={sd.NetLogin}", $"--smb-pwd={sd.NetPass}", "--input-repeat=9999" };

		var debugLogs = false;

		libVLC = new LibVLC(enableDebugLogs: debugLogs, options);
		//You can customize LibVLC with advanced CLI options here https://wiki.videolan.org/VLC_command-line_help/


		//Setup Error Logging
		Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
		libVLC.Log += (s, e) =>
		{
			if (!debugLogs) return;

			try
			{
				Debug.Log(e.FormattedLog);
			}
			catch (Exception ex)
			{
				Debug.Log("Exception caught in libVLC.Log: \n" + ex.ToString());
			}
		};
	}

	public static bool isBuffering = false;


	///<summary> Create a new MediaPlayer object and dispose of the old one.  </summary>
	private void CreateMediaPlayer()
	{
		if (mediaPlayer != null) DestroyMediaPlayer();

		mediaPlayer = new MediaPlayer(libVLC);
		mediaPlayer.EnableHardwareDecoding = true;

		//- for buffering indication
		mediaPlayer.Buffering += (s, e) => { isBuffering = true; };

		mediaPlayer.EncounteredError += (s, e) =>
		{
			Debug.LogError("" + e.ToString());
		};


		mediaPlayer.FileCaching = 10000;
		mediaPlayer.NetworkCaching = 10000;

		mediaPlayer.SetVolume(100);
		if (sd != null) mediaPlayer.SetVolume(sd.Volume);

		Resources.UnloadUnusedAssets();
	}

	private void DestroyMediaPlayer()
	{
		mediaPlayer?.Stop();
		mediaPlayer?.Dispose();
		mediaPlayer = null;
	}

	private readonly Dictionary<string, Texture2D> texCache = new();
	private readonly Dictionary<string, RenderTexture> rtCache = new();

	///<summary> Resize the output textures to the size of the video </summary>
	private void ResizeOutputTextures(uint px, uint py)
	{
		var texptr = mediaPlayer.GetTexture(px, py, out bool updated);
		if (px != 0 && py != 0 && updated && texptr != IntPtr.Zero)
		{

			//If the currently playing video uses the Bottom Right orientation, we have to do this to avoid stretching it.
			if (GetVideoOrientation() == VideoOrientation.BottomRight)
			{
				uint swap = px;
				px = py;
				py = swap;
			}

			var rtID = $"{px}x{py}";

			if (texCache.ContainsKey(rtID))
			{
				_vlcTexture = texCache[rtID];
				_vlcTexture.UpdateExternalTexture(IntPtr.Zero);
			}
			else
			{
				_vlcTexture = Texture2D.CreateExternalTexture((int)px, (int)py, TextureFormat.RGBA32, false, true, texptr);
				_vlcTexture.name = rtID;

				//- cache - because better, when recreate for every file (GC and UnloadUnusedAssets() work strange on Android)
				texCache.TryAdd(rtID, _vlcTexture);
				Debug.Log($"Cache External Tex {rtID}");
			}


			//Make a texture of the proper size for the video to output to

			//- destroy old RenderTexture
			//if (rt != null)
			//{
			//	sphereMat.mainTexture.mainTexture = null;
			//	rt.Release();
			//	rt.DiscardContents();
			//	RenderTexture.ReleaseTemporary(rt);
			//	DestroyImmediate(rt);
			//}

			if (rtCache.ContainsKey(rtID))
			{
				rt = rtCache[rtID];
				ClearOutRenderTexture(rt);
			}
			else
			{
				rt = new RenderTexture(_vlcTexture.width, _vlcTexture.height, 0, RenderTextureFormat.ARGB32);   //Make a renderTexture the same size as vlctex
				rt.name = rtID;

				rtCache.TryAdd(rtID, rt);
				Debug.Log($"Cache RT {rtID}");
			}

			//- force destroy old RenderTexture, or it stay in memory for long time
			Resources.UnloadUnusedAssets();

			if (screen != null)
				screen.material.mainTexture = rt;
			if (canvasScreen != null)
				canvasScreen.texture = rt;

			sphereMat.mainTexture = rt;
		}
	}

	public void ClearOutRenderTexture(RenderTexture renderTexture)
	{
		RenderTexture rt = RenderTexture.active;
		RenderTexture.active = renderTexture;
		GL.Clear(true, true, Color.black);
		RenderTexture.active = rt;
	}

	#endregion

	//---

	#region Player Util


	public enum StereoMode {
		None = 0,
		SBS = 1,
		OU = 2
}

	public void SetVideoLayout(StereoMode mode)
	{
		sphereMat.SetFloat("_Layout", (float)mode);
	}

	public void SetImageType(bool is360)
	{
		if (is360)
		{
			sphereMat.SetFloat("_Rotation", 90f);
			sphereMat.SetFloat("_ImageType", 0f);
		}
		else
		{
			sphereMat.SetFloat("_Rotation", 0f);
			sphereMat.SetFloat("_ImageType", 1f);
		}
	}

	public static string GetFormatedTimeStr(long timeMs)
	{
		var timespan = TimeSpan.FromMilliseconds(timeMs);
		string totalStr;
		if (timespan.TotalHours >= 1)
			totalStr = string.Format("{0:D2}:{1:D2}:{2:D2}", timespan.Hours, timespan.Minutes, timespan.Seconds);
		else
			totalStr = string.Format("{0}:{1:00}", (int)timespan.TotalMinutes, timespan.Seconds);
		return totalStr;
	}

	public void AddZoom(bool positive = true)
	{
		var size = sphere.transform.localScale.z;
		var posV = sphere.transform.position;
		var step = size * 0.01f;
		posV.z += positive ? -step : step;
		posV.z = Mathf.Clamp(posV.z, -size * 0.5f, size * 0.5f);
		sphere.transform.position = posV;
	}

	public void ResetZoom()
	{
		var posV = sphere.transform.position;
		posV.z = 0;
		sphere.transform.position = posV;
	}

	public static void Vibrate()
	{
		Vibration.Vibrate(20);
		Debug.Log("Vibrate");
	}

	#endregion

	//---

	#region Save/Load Data

	public SaveData sd;
	private void LoadData()
	{
		if (!File.Exists(UtilSerial.savePath))
		{
			sd = new();
			return;
		}

		sd = UtilSerial.ReadJson<SaveData>(UtilSerial.savePath);
		sd ??= new();
	}

	private void SaveData()
	{
		if (sd == null) return;
		UtilSerial.WriteJson(sd, UtilSerial.savePath);
	}

	#endregion

}
