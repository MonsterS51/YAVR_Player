using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SaveDataManager
{
	public SaveData sd = new();
	private string savePath = "/save_data.json";

	public void LoadData(string savePath)
	{
		this.savePath = savePath;
		if (File.Exists(savePath))
			sd = UtilSerial.ReadJson<SaveData>(savePath);

		sd ??= new();
	}

	public void SaveData()
	{
		if (sd == null) return;
		UtilSerial.WriteJson(sd, savePath);
	}

}

