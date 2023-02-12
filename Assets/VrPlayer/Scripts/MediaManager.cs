using System;
using System.Collections.Generic;
using System.IO;
using LibVLCSharp;
using UnityEngine;

public class MediaManager
{



	public LibVLC _libVLC;
	List<MediaDiscoverer> _mediaDiscoverers = new();

	public void InitializeMediaDiscoverers()
	{
		Dispose();
		foreach (var md in _libVLC.MediaDiscoverers(MediaDiscovererCategory.Lan))
		{
			var discoverer = new MediaDiscoverer(_libVLC, md.Name);
			discoverer.MediaList.ItemAdded += (x, e) => { Debug.Log($"[YAVR]: Found {md.Name} : {e.Media.Meta(MetadataType.Title)}"); };
			//discoverer.MediaList.ItemDeleted += MediaList_ItemDeleted;
			_mediaDiscoverers.Add(discoverer);
			discoverer.Start();
			Debug.Log($"[YAVR]: Discover found {md.Name}");
		}

	}



	public class MediaItem
	{

		public MediaItem(Media media, MediaItem parentMI = null)
		{
			isFolder = media.Type == MediaType.Directory;
			this.media = media;
			name = MediaName;
			this.parentMI = parentMI;

			media.SubItemAdded += (x, e) =>
			{
				try
				{
					Debug.Log($"[YAVR]: Add SubItem <{e.SubItem.Mrl}>");
					var newMI = new MediaItem(e.SubItem, this);
					newMI.isNetwork = isNetwork;
					listSubMI.Add(newMI);
				}
				catch (Exception ex)
				{
					Debug.LogError($"[YAVR] SubItemAdded : Error !");
					Debug.LogException(ex);
				}
			};
		}
		public string name;
		public string MediaName { get { return media.Meta(MetadataType.Title); } }
		public bool isFolder;
		public bool isNetwork = false;
		public Media media;
		public List<MediaItem> listSubMI = new();
		public MediaItem parentMI = null;

		public override string ToString()
		{

			return $"<{(isFolder ? "DIR " : "")}{MediaName}({media.SubItems.Count})>";
		}
	}

	private List<MediaItem> LoadLocalDrives()
	{
		List<MediaItem> rootItems = new();
		DriveInfo[] allDrives = DriveInfo.GetDrives();
		foreach (var drive in allDrives)
		{
			if (!drive.IsReady) continue;

			var diskMedia = new Media(new Uri($"{drive.Name}"));
			var newMId = new MediaItem(diskMedia);
			newMId.name = drive.Name;
			newMId.isFolder = true;
			newMId.isNetwork = false;

			var task = diskMedia.ParseAsync(_libVLC);
			task.Wait();
			rootItems.Add(newMId);
		}
		return rootItems;
	}

	private List<MediaItem> LoadAndroidStoraged()
	{
		List<MediaItem> rootItems = new();

		string[] potentialDirectories = new string[]
		{
				"/storage",
				"/sdcard",
				"/storage/emulated/0",
				"/mnt/sdcard",
				"/storage/sdcard0",
				"/storage/sdcard1"
		};

		foreach (var dir in potentialDirectories)
		{
			if (!Directory.Exists(dir)) continue;

			var diskMedia = new Media(new Uri($"{dir}"));
			var newMId = new MediaItem(diskMedia);
			newMId.name = dir;
			newMId.isFolder = true;
			newMId.isNetwork = false;

			var task = diskMedia.ParseAsync(_libVLC);
			task.Wait();
			rootItems.Add(newMId);
		}
		return rootItems;
	}

	private List<MediaItem> LoadLanSources()
	{
		List<MediaItem> rootItems = new();

		foreach (var md in _mediaDiscoverers)
		{
			foreach (var media in md.MediaList)
			{
				var newMI = new MediaItem(media);
				newMI.name = $"(LAN) {newMI.MediaName}";
				newMI.isNetwork = true;
				rootItems.Add(newMI);
			}
		}

		return rootItems;
	}

	public List<MediaItem> GetRootMediaItems()
	{
		List<MediaItem> rootItems = new();

		if (Application.platform == RuntimePlatform.Android)
			rootItems.AddRange(LoadAndroidStoraged());
		else
			rootItems.AddRange(LoadLocalDrives());

		rootItems.AddRange(LoadLanSources());

		Debug.Log($"[YAVR]: RootMedias ({rootItems.Count}): {string.Join("\n", rootItems)}");
		return rootItems;
	}


	public void Dispose()
	{
		foreach (var md in _mediaDiscoverers)
		{
			md.Dispose();
		}
		_mediaDiscoverers.Clear();
	}

}
