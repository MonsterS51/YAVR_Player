using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public static class Utils
{
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

	public static void Vibrate()
	{
		Vibration.Vibrate(20);
		Debug.Log("Vibrate");
	}

}

