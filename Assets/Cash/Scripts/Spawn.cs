
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Spawn : UdonSharpBehaviour
{
    public GameObject prefab;

    public override void Interact()
    {
        var go = VRCInstantiate(prefab);
        go.transform.position = transform.position + Vector3.up * 10;
        go.SetActive(true);
    }
}
