
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class Bank : UdonSharpBehaviour
{
    public Material[] cashMaterials;
    
    public VRCObjectPool cashPool;
    public VRCObjectPool bundlePool;

    public Material GetMaterialForValue(int value)
    {
        switch(value)
        {
            default:
            case 1:
                return cashMaterials[0];
            case 5:
                return cashMaterials[1];
            case 10:
                return cashMaterials[2];
            case 20:
                return cashMaterials[3];
            case 50:
                return cashMaterials[4];
            case 100:
                return cashMaterials[5];
            case 500:
                return cashMaterials[6];
        }
    }

    public Bundle SpawnBundle()
    {
        var go = bundlePool.TryToSpawn();
        if (go == null)
        {
            Debug.LogError("Too many bundles out. Cannot create a new one!");
            return null;
        }

        return go.GetComponent<Bundle>();
    }
    public void ReturnBundle(Bundle bundle)
    {
        bundlePool.Return(bundle.gameObject);
    }

    /// <summary>Creates a new cash object from the pool</summary>
    public Cash SpawnCash(int value)
    {
        var go = cashPool.TryToSpawn();
        if (go == null)
        {
            Debug.LogError("Too much cash is out. Cannot create a new one!");
            return null;
        }

        // Set the cash value
        var cash = go.GetComponent<Cash>();
        cash.value = value;
        return cash;
    }
    public void ReturnCash(Cash cash)
    {
        cashPool.Return(cash.gameObject);
    }
}
