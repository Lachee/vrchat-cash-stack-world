
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ModelScriptSpawn : UdonSharpBehaviour
{
    public GameObject model;

    public override void Interact()
    {
        var clone = VRCInstantiate(model);
        clone.transform.position = transform.position;
    }
}
