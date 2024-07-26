using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class StereoVrManager
{
	private GameObject sphere;
	private Material sphereMat;

	public void SetSphereMaterial(GameObject sphere)
	{
		this.sphere = sphere;
		this.sphereMat = sphere.GetComponent<MeshRenderer>().material;
	}

	public void SetSphereBlack()
	{
		sphereMat.mainTexture = Texture2D.blackTexture;
	}

	public void SetSphereTexture(RenderTexture rt)
	{
		sphereMat.mainTexture = rt;
	}

	//---

	public enum StereoMode
	{
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

	public void SetModeByFileName(string fileName)
	{
		if (fileName.Contains("360")) SetImageType(true);
		else SetImageType(false);
		if (fileName.Contains("_ou")) SetVideoLayout(StereoMode.OU);
		else SetVideoLayout(StereoMode.SBS);
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

	public float ZoomPercent
	{
		get
		{
			var size = sphere.transform.localScale.z;
			var posV = sphere.transform.position;
			return (100 - (posV.z / size) * 100f);
		}
	}

}

