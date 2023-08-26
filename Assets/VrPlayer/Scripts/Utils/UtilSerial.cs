using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class UtilSerial
{

	private static JsonSerializerSettings jsonSettings = new()
	{
		TypeNameHandling = TypeNameHandling.Auto,
		NullValueHandling = NullValueHandling.Ignore,
		DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
		MissingMemberHandling = MissingMemberHandling.Ignore,
		Formatting = Formatting.Indented,
		Converters = new List<JsonConverter>() { new FixerStringEnumConverter() }   // модифицированная сериализация Enum
	};

	//? StringEnumConverter - позволяет хранить Enum по именам (а не порядку), FixerStringEnumConverter - обходит exception с измененными enum переводя его в дефолтный
	public class FixerStringEnumConverter : StringEnumConverter
	{
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				return base.ReadJson(reader, objectType, existingValue, serializer);
			}
			catch (JsonSerializationException ex)
			{
				Debug.LogError($"Enum Fix <{ex.Message}> for enum {objectType.FullName} " + ex.Message);
				return Activator.CreateInstance(objectType);
			}
		}
	}


	public static string Serialize_JsonNet(object obj)
	{
		return JsonConvert.SerializeObject(obj, jsonSettings);
	}

	public static T Deserialize_JsonNet<T>(string str)
	{
		if (string.IsNullOrWhiteSpace(str)) return default;

		try
		{
			return JsonConvert.DeserializeObject<T>(str, jsonSettings);
		}
		catch (Exception ex)
		{
			Debug.LogError($"Error deserialize to {typeof(T)} of str: {str} " + ex.Message);
			return default;
		}

	}


	public static object Deserialize_JsonNet(string str, Type t)
	{
		if (string.IsNullOrWhiteSpace(str)) return default;

		try
		{
			return JsonConvert.DeserializeObject(str, t, jsonSettings);
		}
		catch (Exception ex)
		{
			Debug.LogError($"Error deserialize to {t} of str: {str} " + ex.Message);
			return null;
		}
	}

	public static T ReadJson<T>(string filepath)
	{
		if (!File.Exists(filepath)) return default;
		var str = File.ReadAllText(filepath);

		try
		{
			return JsonConvert.DeserializeObject<T>(str, jsonSettings);
		}
		catch (Exception ex)
		{
			Debug.LogError($"Can`t Read Json -> <{typeof(T)}> from file: <{filepath}> " + ex.Message);
			return default;
		}
	}

	///<summary> Запись объекта в JSON файл. </summary>
	public static void WriteJson(object obj, string path)
	{
		var jsonStr = Serialize_JsonNet(obj);
		File.WriteAllText(path, jsonStr);
	}


	public static string savePath = Application.persistentDataPath + "/save_data.json";




}

