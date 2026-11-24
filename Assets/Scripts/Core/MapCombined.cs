using UnityEngine;
using System.Collections.Generic;

public class MeshCombiner : MonoBehaviour
{
    void Start()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        List<CombineInstance> combineList = new List<CombineInstance>();

        Material mat = null;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.transform == transform) continue;
            if (mf.sharedMesh == null) continue;

            if (mf.sharedMesh == null)
            {
                Debug.LogWarning("❌ Mesh NULL: " + mf.name);
                continue;
            }

            CombineInstance ci = new CombineInstance();
            ci.mesh = mf.sharedMesh;
            ci.transform = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            combineList.Add(ci);

            if (mat == null)
            {
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr != null)
                    mat = mr.sharedMaterial;
            }

            mf.gameObject.SetActive(false); 
        }

        MeshFilter myMF = gameObject.GetComponent<MeshFilter>();
        if (myMF == null) myMF = gameObject.AddComponent<MeshFilter>();

        MeshRenderer myMR = gameObject.GetComponent<MeshRenderer>();
        if (myMR == null) myMR = gameObject.AddComponent<MeshRenderer>();

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        combinedMesh.CombineMeshes(combineList.ToArray(), true, true);

        myMF.mesh = combinedMesh;
        myMR.material = mat;
    }
}