using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Not sure how I feel about requiring an object extending this in order to be teleported
// Can we make this generic to any gameobject?
public class PortalTraveler : MonoBehaviour {

    public GameObject graphicsObject;
    public GameObject graphicsClone { get; set; }
    public Vector3 previousOffsetFromPortal { get; set; }

    public Material[] originalMaterials { get; set; }
    public Material[] cloneMaterials { get; set; }

    public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot) {
        transform.position = pos;
        transform.rotation = rot;
    }

    // Called when first touches portal
    public virtual void EnterPortalThreshold() {
        if (graphicsClone == null) {
            graphicsClone = Instantiate(graphicsObject);
            graphicsClone.transform.parent = graphicsObject.transform.parent;
            graphicsClone.transform.localScale = graphicsObject.transform.localScale;
            originalMaterials = GetMaterials(graphicsObject);
            cloneMaterials = GetMaterials(graphicsClone);
        } else {
            graphicsClone.SetActive(true);
        }
    }

    // Called once no longer touching portal (excluding when teleporting)
    public virtual void ExitPortalThreshold() {
        graphicsClone.SetActive(false);
        // Disable slicing
        for (int i = 0; i < originalMaterials.Length; i++) {
            originalMaterials[i].SetVector("sliceNormal", Vector3.zero);
        }
    }

    Material[] GetMaterials(GameObject g) {
        var renderers = g.GetComponentsInChildren<MeshRenderer>();
        var matList = new List<Material>();

        foreach (var renderer in renderers) {
            foreach (var mat in renderer.materials) {
                matList.Add(mat);
            }
        }

        return matList.ToArray();
    }
}
