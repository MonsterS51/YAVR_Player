using System;
using System.Collections;
using System.Collections.Generic;
using Google.XR.Cardboard;
using LibVLCSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Management;

public class VrPlayerController : MonoBehaviour
{
	public static LibVLC libVLC; //The LibVLC class is mainly used for making MediaPlayer and Media objects. You should only have one LibVLC instance.
	public MediaPlayer mediaPlayer; //MediaPlayer is the main class we use to interact with VLC

	//Screens
	public Renderer screen; //Assign a mesh to render on a 3d object
	public RawImage canvasScreen; //Assign a Canvas RawImage to render on a GUI object

	public Texture2D _vlcTexture = null; //This is the texture libVLC writes to directly. It's private.
	//public RenderTexture rt = null; //We copy it into this texture which we actually use in unity.


	public string path = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"; //Can be a web path or a local path

	public bool flipTextureX = false; //No particular reason you'd need this but it is sometimes useful
	public bool flipTextureY = true; //Set to false on Android, to true on Windows

	public bool automaticallyFlipOnAndroid = true; //Automatically invert Y on Android

	public bool playOnAwake = true; //Open path and Play during Awake

	public bool logToConsole = false; //Log function calls and LibVLC logs to Unity console


	public MediaManager mm = new();

	public static string cachePath;


	//Unity Awake, OnDestroy, and Update functions
	#region unity
	void Awake()
	{
		RenderSettings.skybox.mainTexture = Texture2D.blackTexture;

		cachePath = Application.temporaryCachePath;

		//- lock fps to 60 for heat and energy saving
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 60;

		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		if (!Api.HasDeviceParams())	Api.ScanDeviceParams();	

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

		//Play On Start
		if (playOnAwake)
			Open();
	}

	void OnDestroy()
	{
		mm.Dispose();

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
					//var flip = new Vector2(flipTextureX ? -1 : 1, flipTextureY ? -1 : 1);
					//Graphics.Blit(_vlcTexture, rt, flip, Vector2.zero); //If you wanted to do post processing outside of VLC you could use a shader here.
				}
			}
		}

	}

	void OnApplicationFocus(bool hasFocus)
	{
		if (!hasFocus) mediaPlayer?.SetPause(true);
	}

	void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus) mediaPlayer?.SetPause(true);
	}

	#endregion

	/// <summary>
	/// Gets a value indicating whether the VR mode is enabled.
	/// </summary>
	private bool _isVrModeEnabled
	{
		get
		{
			return XRGeneralSettings.Instance.Manager.isInitializationComplete;
		}
	}


	//---

	//Public functions that expose VLC MediaPlayer functions in a Unity-friendly way. You may want to add more of these.
	#region vlc
	public void Open(string path)
	{
		Log("VLCPlayerExample Open " + path);
		this.path = path;
		Open();
	}

	public void Open()
	{
		try
		{
			Log("VLCPlayerExample Open");
			if (mediaPlayer.Media != null)
				mediaPlayer.Media.Dispose();

			var trimmedPath = path.Trim(new char[] { '"' });//Windows likes to copy paths with quotes but Uri does not like to open them
			mediaPlayer.Media = new Media(new Uri(trimmedPath));

			Log($"VLCPlayerExample Media {mediaPlayer.Media.Mrl}");

			Play();
		}
		catch (Exception ex)
		{
			Debug.LogError($"[YAVR]: Cant open <{mediaPlayer.Media.Mrl}> : " + ex.Message);
		}
	}

	public void Open(Media media)
	{
		Log($"VLCPlayerExample Open <{media.Mrl}>");
		CreateLibVLC();
		if (mediaPlayer.Media != null)
			mediaPlayer.Media.Dispose();
		mediaPlayer.Media = media;
		Play();
	}

	public void Play()
	{
		Log("VLCPlayerExample Play");

		mediaPlayer.Play();
	}

	public void Pause()
	{
		Log("VLCPlayerExample Pause");
		mediaPlayer.Pause();
	}

	public void Stop()
	{
		Log("VLCPlayerExample Stop");
		mediaPlayer?.Stop();
		_vlcTexture = null;
		//rt = null;
		RenderSettings.skybox.mainTexture = Texture2D.blackTexture;


	}

	public void Seek(long timeDelta)
	{
		Log("VLCPlayerExample Seek " + timeDelta);
		mediaPlayer.SetTime(mediaPlayer.Time + timeDelta);
	}

	public void SetTime(long time)
	{
		Log("VLCPlayerExample SetTime " + time);
		mediaPlayer.SetTime(time);
	}

	public void SetVolume(int volume = 100)
	{
		Log("VLCPlayerExample SetVolume " + volume);
		mediaPlayer.SetVolume(volume);
	}

	public int Volume
	{
		get
		{
			if (mediaPlayer == null)
				return 0;
			return mediaPlayer.Volume;
		}
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

	public List<MediaTrack> Tracks(TrackType type)
	{
		Log("VLCPlayerExample Tracks " + type);
		return ConvertMediaTrackList(mediaPlayer?.Tracks(type));
	}

	public MediaTrack SelectedTrack(TrackType type)
	{
		Log("VLCPlayerExample SelectedTrack " + type);
		return mediaPlayer?.SelectedTrack(type);
	}

	public void Select(MediaTrack track)
	{
		Log("VLCPlayerExample Select " + track.Name);
		mediaPlayer?.Select(track);
	}

	public void Unselect(TrackType type)
	{
		Log("VLCPlayerExample Unselect " + type);
		mediaPlayer?.Unselect(type);
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

		var orientation = tracks[0]?.Data.Video.Orientation; //At the moment we're assuming the track we're playing is the first track

		return orientation;
	}

	#endregion

	//Private functions create and destroy VLC objects and textures
	#region internal
	//Create a new static LibVLC instance and dispose of the old one. You should only ever have one LibVLC instance.
	void CreateLibVLC()
	{
		Log("VLCPlayerExample CreateLibVLC");
		//Dispose of the old libVLC if necessary
		if (libVLC != null)
		{
			libVLC.Dispose();
			libVLC = null;
		}

		Core.Initialize(Application.dataPath); //Load VLC dlls
		libVLC = new LibVLC(enableDebugLogs: true, "--smb-user=Android", "--smb-pwd=", "--input-repeat=9999"); 
		//You can customize LibVLC with advanced CLI options here https://wiki.videolan.org/VLC_command-line_help/

		//Setup Error Logging
		Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
		libVLC.Log += (s, e) =>
		{
			//Always use try/catch in LibVLC events.
			//LibVLC can freeze Unity if an exception goes unhandled inside an event handler.
			try
			{
				if (logToConsole)
				{
					Log(e.FormattedLog);
				}
			}
			catch (Exception ex)
			{
				Log("Exception caught in libVLC.Log: \n" + ex.ToString());
			}

		};
	}

	public bool isBuffering = false;

	//Create a new MediaPlayer object and dispose of the old one. 
	void CreateMediaPlayer()
	{
		Log("VLCPlayerExample CreateMediaPlayer");
		if (mediaPlayer != null)
		{
			DestroyMediaPlayer();
		}
		mediaPlayer = new MediaPlayer(libVLC);
		mediaPlayer.EnableHardwareDecoding = true;

		//- for buffering indication
		mediaPlayer.Buffering += (s, e) => { isBuffering = true; };

		mediaPlayer.EncounteredError += (s, e) => {
			Debug.LogError("" + e.ToString());
		};


		mediaPlayer.FileCaching = 500;
		mediaPlayer.NetworkCaching = 500;

		Resources.UnloadUnusedAssets();
	}

	//Dispose of the MediaPlayer object. 
	void DestroyMediaPlayer()
	{
		Log("VLCPlayerExample DestroyMediaPlayer");
		mediaPlayer?.Stop();
		mediaPlayer?.Dispose();
		mediaPlayer = null;
	}

	private Dictionary<string, Texture2D> texCache = new();
	//private Dictionary<string, RenderTexture> rtCache = new();

	//Resize the output textures to the size of the video
	void ResizeOutputTextures(uint px, uint py)
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
			//	RenderSettings.skybox.mainTexture = null;
			//	rt.Release();
			//	rt.DiscardContents();
			//	RenderTexture.ReleaseTemporary(rt);
			//	DestroyImmediate(rt);
			//}

			//if (rtCache.ContainsKey(rtID))
			//{
			//	rt = rtCache[rtID];
			//}
			//else
			//{
			//	rt = new RenderTexture(_vlcTexture.width, _vlcTexture.height, 0, RenderTextureFormat.ARGB32); //Make a renderTexture the same size as vlctex
			//	rt.name = rtID;
			//	rtCache.TryAdd(rtID, rt);
			//	Debug.Log($"Cache RT {rtID}");
			//}

			//- force destroy old RenderTexture, or it stay in memory for long time
			Resources.UnloadUnusedAssets();

			if (screen != null)
				screen.material.mainTexture = _vlcTexture;
			if (canvasScreen != null)
				canvasScreen.texture = _vlcTexture;

			RenderSettings.skybox.mainTexture = _vlcTexture;
		}
	}

	//Converts MediaTrackList objects to Unity-friendly generic lists. Might not be worth the trouble.
	List<MediaTrack> ConvertMediaTrackList(MediaTrackList tracklist)
	{
		if (tracklist == null)
			return new List<MediaTrack>(); //Return an empty list

		var tracks = new List<MediaTrack>((int)tracklist.Count);
		for (uint i = 0; i < tracklist.Count; i++)
		{
			tracks.Add(tracklist[i]);
		}
		return tracks;
	}

	void Log(string message)
	{
		if (logToConsole)
			Debug.Log($"[YAVR]: {message}");
	}
	#endregion

	public void SetVideoLayout(bool isSBS)
	{
		if (isSBS) RenderSettings.skybox.SetFloat("_Layout", 1f);
		else RenderSettings.skybox.SetFloat("_Layout", 2f);
	}

	public void SetImageType(bool is360)
	{
		if (is360) {
			RenderSettings.skybox.SetFloat("_Rotation", 90f);
			RenderSettings.skybox.SetFloat("_ImageType", 0f); 
		}
		else {
			RenderSettings.skybox.SetFloat("_Rotation", 0f);
			RenderSettings.skybox.SetFloat("_ImageType", 1f);
		}
	}

}
