﻿using System;
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
	public float lastScrollPos = 1f;
	public string mediaInfo = string.Empty;

	private uint w = 2;
	private uint h = 1;

	public string MediaName { get { return media.Meta(MetadataType.Title); } }

	public MediaItem(Media media, MediaItem parentMI = null)
	{
		isFolder = media.Type == MediaType.Directory;
		this.media = media;
		name = Path.GetFileNameWithoutExtension(MediaName)?.Replace('_', ' ');
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
		return $"<{(isFolder ? "DIR " : "")}{name}({media.SubItems.Count})>";
	}

	private string[] vFormats = { ".mkv", ".mp4", ".avi", ".mpg", ".mpeg", ".ts", ".webm" };

	private void SetMediaEvents(Media media)
	{

		//- Sub MediaItem create on found
		media.SubItems.ItemAdded += (x, e) =>
		{
			try
			{
				//Debug.Log($"[YAVR]: Add SubItem <{e.Media.Mrl}>");
				var newMI = new MediaItem(e.Media, this);
				newMI.isNetwork = isNetwork;

				//- filter by extension
				var ext = Path.GetExtension(newMI.MediaName);
				if (!newMI.isFolder && !vFormats.Contains(ext)) return;

				listSubMI.Add(newMI);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[YAVR] SubItem Added : " + ex.Message);
			}
		};

		media.SubItems.ItemDeleted += (x, e) =>
		{
			//Debug.Log($"[YAVR]: ItemDeleted <{e.Media.Mrl}>");
			try
			{
				listSubMI.RemoveAll(x => x.media == e.Media);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[YAVR] SubItem Deleted : " + ex.Message);
			}
		};

		media.ParsedChanged += (x, e) =>
		{
			//Debug.Log($"MI {name} : ParsedChanged {e.ParsedStatus}");
			UpdateMediaInfoStr();
		};


	}

	///<summary> Recreate Media object to run parse again. </summary>
	public Task ReparseMedia()
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
			return task;
		}
		return Task.CompletedTask;
	}

	//---

	#region Parse Tasks

	private CancellationTokenSource cts;
	public bool parseInProgress = false;
	public bool parseChildInProgress = false;

	public void StartParseChildMedia()
	{
		if (parseChildInProgress) return;
		parseChildInProgress = true;

		//- parse media one by one for easy cancel

		cts = new CancellationTokenSource();
		var ct = cts.Token;

		var parseTask = Task.Factory.StartNew(() =>
		{
			try
			{
				// последовательно парсим каждый элемент
				foreach (var subMI in listSubMI)
				{
					if (ct.IsCancellationRequested) return;
					var miParseTask = subMI.StartParse();
					miParseTask.Wait();
				}
			}
			catch (Exception) { }
			finally
			{
				cts.Dispose();
				cts = null;
				parseChildInProgress = false;
			}
		});
	}


	public void CancelParseChildMedia()
	{
		cts?.Cancel();
	}


	public Task StartParse()
	{
		if (parseInProgress) return Task.CompletedTask;
		parseInProgress = true;

		var rootTask = Task.Factory.StartNew(() =>
			{
				try
				{
					// в некоторых версиях libVLC Fetch и Parse режимы перепутаны
					var mode = isNetwork ? MediaParseOptions.FetchNetwork : MediaParseOptions.FetchLocal;
					var parseTask = media.ParseAsync(VrPlayerController.libVLC, mode);
					parseTask.Wait();
				}
				catch (Exception e)
				{
					Debug.LogError($"{nameof(StartParse)} : {name} : {e.Message}");
				}
			});

		parseInProgress = false;

		return rootTask;
	}

	private void UpdateMediaInfoStr()
	{
		if (isFolder)
		{
			mediaInfo = $"{listSubMI.Where(x => !x.isFolder).Count()} video";
			return;
		}


		if (media == null) return;
		mediaInfo = string.Empty;

		//- work only right after parse/fetch
		var trackList = media.TrackList(TrackType.Video);
		if (trackList.Count > 0)
		{
			w = trackList[0].Data.Video.Width;
			h = trackList[0].Data.Video.Height;
			var fps = trackList[0].Data.Video.FrameRateNum / trackList[0].Data.Video.FrameRateDen;
			mediaInfo += $"[{w}x{h}:{fps}]";
		}
		trackList.Dispose();

		media.FileStat(FileStat.Size, out var fSize);
		media.FileStat(FileStat.Mtime, out var mTime);
		var refPoint = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		var modDate = refPoint.AddSeconds(mTime);
		var dur = Utils.GetFormatedTimeStr(media.Duration);
		var size = (fSize / 1024) / 1024;
		var sizeStr = size < 1024 ? $"{size} Mb" : $"{String.Format("{0:0.00}", size / 1024f)} Gb";
		mediaInfo += $" <{dur}>  <{sizeStr}>  <{modDate.ToString("dd.MM.yyyy")}>";

	}


	#endregion

}

