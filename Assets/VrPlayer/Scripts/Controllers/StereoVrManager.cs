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
	private Material srcEqrPanoMat;
	private Material srcFisheyeMat;

	public bool isFisheyeMode = false;

	public void SetMaterials(GameObject sphere, Material srcEqrPanoMat, Material srcFisheyeMat)
	{
		this.sphere = sphere;
		this.srcEqrPanoMat = srcEqrPanoMat;
		this.srcFisheyeMat = srcFisheyeMat;
		sphere.GetComponent<MeshRenderer>().material = srcEqrPanoMat;
		sphereMat = sphere.GetComponent<MeshRenderer>().material;
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
		OU = 2,
		SBS_FISHEYE = 3,
	}

	public enum StereoAngleMode
	{
		EQR_180 = 0,
		EQR_360 = 1,
		FE_190 = 2,
		FE_200 = 3,
	}

	public void SetMaterialByMode(bool isFisheyeEnabled)
	{
		var oldMatTR = sphere.GetComponent<MeshRenderer>().material.mainTexture as RenderTexture;
		isFisheyeMode = isFisheyeEnabled;
		if (!isFisheyeMode)
		{
			sphere.GetComponent<MeshRenderer>().material = srcEqrPanoMat;
			sphereMat = srcEqrPanoMat;
			Debug.Log($"Set EQR mat {sphereMat}");
		}
		else
		{
			sphere.GetComponent<MeshRenderer>().material = srcFisheyeMat;
			sphereMat = srcFisheyeMat;
			Debug.Log($"Set FE mat {sphereMat}");
		}

		SetSphereTexture(oldMatTR);
	}

	public void SetVideoLayout(StereoMode mode)
	{
		isFisheyeMode = (mode == StereoMode.SBS_FISHEYE);
		SetMaterialByMode(isFisheyeMode);
		if (!isFisheyeMode)
		{
			sphereMat.SetFloat("_Layout", (float)mode);
		}
		else
		{

		}
	}

	public void SetImageType(StereoAngleMode angle)
	{
		if (angle == StereoAngleMode.EQR_360)
		{
			sphereMat.SetFloat("_Rotation", 90f);
			sphereMat.SetFloat("_ImageType", 0f);
		}

		if (angle == StereoAngleMode.EQR_180)
		{
			sphereMat.SetFloat("_Rotation", 0f);
			sphereMat.SetFloat("_ImageType", 1f);
		}

		if (angle == StereoAngleMode.FE_200) sphereMat.SetFloat("_FOV", 200f);
		if (angle == StereoAngleMode.FE_190) sphereMat.SetFloat("_FOV", 190f);
	}

	public void SetModeByFileName(string fileName)
	{
		if (fileName.Contains("360")) SetImageType(StereoAngleMode.EQR_360);
		else SetImageType(StereoAngleMode.EQR_180);
		if (fileName.Contains("_ou")) SetVideoLayout(StereoMode.OU);
		else SetVideoLayout(StereoMode.SBS);

		if (fileName.Contains("fisheye190"))
		{
			SetVideoLayout(StereoMode.SBS_FISHEYE);
			SetImageType(StereoAngleMode.FE_190);
		}

		if (fileName.Contains("fisheye200"))
		{
			SetVideoLayout(StereoMode.SBS_FISHEYE);
			SetImageType(StereoAngleMode.FE_200);
		}
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

