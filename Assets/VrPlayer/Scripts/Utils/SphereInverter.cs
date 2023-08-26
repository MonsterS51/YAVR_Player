
using System.Linq;
using UnityEngine;

public class SphereInverter : MonoBehaviour
{

	private void Awake()
	{
		var mf = GetComponent<MeshFilter>();

		var mesh = mf.mesh;

		// Reverse the triangles
		mesh.triangles = mesh.triangles.Reverse().ToArray();

		// also invert the normals
		mesh.normals = mesh.normals.Select(n => -n).ToArray();
	}
}

