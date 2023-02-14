using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp;
using UnityEngine;

public class MediaItem
{
	public string name = string.Empty;
	public bool isFolder = false;
	public bool isNetwork = false;
	public Media media = null;
	public List<MediaItem> listSubMI = new();
	public MediaItem parentMI = null;
	private Texture2D thumbnail = null;

	public string MediaName { get { return media.Meta(MetadataType.Title); } }

	public MediaItem(Media media, MediaItem parentMI = null)
	{
		isFolder = media.Type == MediaType.Directory;
		this.media = media;
		name = MediaName;
		this.parentMI = parentMI;

		SetMediaEvents(media);

		//- folder already parsed - so we need refill MediaItem
		if (isFolder && media.ParsedStatus == MediaParsedStatus.Done)
		{
			ReparseMedia();
		}

	}



	public override string ToString()
	{
		return $"<{(isFolder ? "DIR " : "")}{MediaName}({media.SubItems.Count})>";
	}

	private void SetMediaEvents(Media media)
	{

		//- Sub MediaItem create on found
		media.SubItems.ItemAdded += (x, e) =>
		{
			try
			{
				Debug.Log($"[YAVR]: Add SubItem <{e.Media.Mrl}>");
				var newMI = new MediaItem(e.Media, this);
				newMI.isNetwork = isNetwork;
				listSubMI.Add(newMI);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[YAVR] SubItem Added : " + ex.Message);
			}
		};

		media.SubItems.ItemDeleted += (x, e) =>
		{
			Debug.Log($"[YAVR]: ItemDeleted <{e.Media.Mrl}>");
			try
			{
				listSubMI.RemoveAll(x => x.media == e.Media);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[YAVR] SubItem Deleted : " + ex.Message);
			}
		};


		if (media.Type == MediaType.File)
		{
			if (!IsThumbnailCached)
			{
				media.ThumbnailGenerated += (x, e) =>
				{
					try
					{
						if (e.Thumbnail == null) return;
						var path = GetThumbnailCachePath(media);
						e.Thumbnail.Save(path);
						e.Thumbnail.Dispose();
						Debug.Log($"[YAVR]: Thumbnail Generated <{name}>");
					}
					catch (Exception ex)
					{
						Debug.LogError($"[YAVR] ThumbnailGenerated : " + ex.Message);
					}
				};


			}
		}
	}

	///<summary> Recreate Media object to run parse again. </summary>
	public void ReparseMedia()
	{

		var uri = new Uri(media.Mrl);
		var newMedia = new Media(uri);
		if (newMedia != null)
		{
			media.Dispose();
			media = newMedia;
			listSubMI.Clear();
			SetMediaEvents(media);
			var task = StartParse();
			task.Wait();
		}
	}

	//---

	#region Parse Tasks

	private CancellationTokenSource cts;
	public bool parseChildInProgerss = false;

	public void StartParseChildMedia()
	{
		if (parseChildInProgerss) return;
		parseChildInProgerss = true;

		//- parse media one by one for easy cancel

		cts = new CancellationTokenSource();
		var ct = cts.Token;

		var parseTask = Task.Factory.StartNew(() =>
		{
			try
			{
				foreach (var subMI in listSubMI)
				{
					if (ct.IsCancellationRequested) return;
					if (!subMI.isFolder)
					{
						var subTask = subMI.StartParse();
						subTask.Wait();
					}
				}
			}
			catch (Exception) { }
			finally
			{
				cts.Dispose();
				cts = null;
				parseChildInProgerss = false;
			}
		});
	}


	public void CancelParseChildMedia()
	{
		cts?.Cancel();
	}

	public Task StartParse()
	{
		var parseTask = Task.Factory.StartNew(() =>
		{
			try
			{
				var mode = isNetwork ? MediaParseOptions.ParseNetwork : MediaParseOptions.ParseLocal;

				var task = media.ParseAsync(VrPlayerController.libVLC, mode, -1);
				task.Wait();

				if (!isFolder)
				{
					if (!IsThumbnailCached)
					{
						var task2 = RunGenerateThumbnail();
						task2.Wait();
					}
				}
			}
			catch (Exception e)
			{
				Debug.Log($"StartParse : {e.Message}");
			}
		});

		return parseTask;
	}

	#endregion

	//---

	#region Thumbnail Gen

	public Texture2D GetThumbnailFromCache()
	{
		if (thumbnail != null) return thumbnail;

		try
		{
			var path = GetThumbnailCachePath(media);
			if (!File.Exists(path)) return null;
			thumbnail ??= LoadPNG(path);
			return thumbnail;
		}
		catch (Exception ex)
		{
			Debug.LogError($"[YAVR] GetThumbnail : Error !");
			Debug.LogException(ex);
			return null;
		}

	}

	private Task RunGenerateThumbnail()
	{
		//- calc aspect for thumbnail
		uint w = 2;
		uint h = 1;
		var firstVideoTrack = media.TrackList(TrackType.Video).FirstOrDefault();
		if (firstVideoTrack != null)
		{
			w = firstVideoTrack.Data.Video.Width;
			h = firstVideoTrack.Data.Video.Height;
		}
		var aspect = w / (float)h;
		w = 250;
		h = (uint)(w / aspect);

		//- media should be parsed at this moment
		var time = (int)(media.Duration * 0.4f);
		return media.GenerateThumbnailAsync(VrPlayerController.libVLC, time, ThumbnailerSeekSpeed.Fast, w, h, false, PictureType.Png);
	}


	public static string thumbsCachePath = VrPlayerController.cachePath + $"/thumbs/";

	private static string GetThumbnailCachePath(Media media)
	{
		Directory.CreateDirectory(thumbsCachePath);
		var hash = media.Mrl.GetHashCode();
		var path = VrPlayerController.cachePath + $"/thumbs/{hash}";
		return path;
	}


	private static Texture2D LoadPNG(string filePath)
	{

		Texture2D tex = null;
		byte[] fileData;

		if (File.Exists(filePath))
		{
			fileData = File.ReadAllBytes(filePath);
			tex = new Texture2D(2, 2);
			tex.LoadImage(fileData);
		}
		return tex;
	}

	private bool IsThumbnailCached
	{
		get
		{
			var path = GetThumbnailCachePath(media);
			return File.Exists(path);
		}
	}

	#endregion

}

