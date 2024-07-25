using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibVLCSharp;
using UnityEngine;

public class MediaManager
{



	public LibVLC _libVLC;
	private List<MediaDiscoverer> _mediaDiscoverers = new();

	public void InitializeMediaDiscoverers()
	{
		Dispose();
		foreach (var md in _libVLC.MediaDiscoverers(MediaDiscovererCategory.Lan))
		{
			var discoverer = new MediaDiscoverer(_libVLC, md.Name);
			discoverer.MediaList.ItemAdded += (x, e) => { Debug.Log($"[YAVR]: Found {md.Name} : {e.Media.Meta(MetadataType.Title)}"); };
			_mediaDiscoverers.Add(discoverer);
			discoverer.Start();
			Debug.Log($"[YAVR]: Discoverer found {md.Name}");
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
			var newMI = new MediaItem(diskMedia);
			newMI.name = $"(DR) {drive.Name}";
			newMI.isFolder = true;
			newMI.isNetwork = false;
			rootItems.Add(newMI);
		}
		return rootItems;
	}

	private List<MediaItem> LoadAndroidStorages()
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
			var newMI = new MediaItem(diskMedia);
			newMI.name = $"(SD) {dir}";
			newMI.isFolder = true;
			newMI.isNetwork = false;
			rootItems.Add(newMI);
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
			rootItems.AddRange(LoadAndroidStorages());
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
