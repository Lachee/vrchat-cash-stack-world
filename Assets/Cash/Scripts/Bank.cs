using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class Bank : UdonSharpBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Cash Prefab. For it to spawn, it has to be in the scene.")]
    public GameObject cashModelPrefab;

    [Header("Pools")]
    public VRCObjectPool cashPool;
    public VRCObjectPool bundlePool;

    [Header("Costmetics")]
    public Material[] cashMaterials;

    private GameObject pendingRoot;
    private Cash pendingCash;
    
    /// <summary>
    /// Creates a new cash bundle
    /// </summary>
    /// <param name="root">The root Bundle or Cash to create the bundle with</param>
    /// <param name="cash">The cash to add to the bundle</param>
    public void CreateBundle(GameObject root, Cash cash)
    {
        if (root == null)
            return;

        // Become the owner of the bank then create the bundle
        pendingRoot = root;
        pendingCash = cash;
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        else
        {
            InternalBundle();
        }
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
    {
        Debug.Log("Player " + requestingPlayer + " is requesting ownership from " + requestedOwner + " for the bank");
        return true;
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (player == Networking.LocalPlayer)
        {
            Debug.Log("Ownership transfered so going to proceed with bundling");
            if (pendingRoot != null && pendingCash != null)
                InternalBundle();
        }
    }

    /// <summary>performs the bundlign on the cash</summary>
    private void InternalBundle()
    {
        if (pendingRoot == null && pendingCash == null)
            return;

        Debug.Log("Bundling");

        // Get the bundle, if it doesn't exist create it (assuming its also a cash then)
        Bundle bundle = null;
        if (bundle == null)
        {
            Cash cash = pendingRoot.GetComponent<Cash>();
            if (cash == null)
            {
                Debug.LogWarning("Attempted to connect to a non-bundle and non-cash object!");
                return;
            }

            // Create a new bundle and set it to the pending position
            // Then add the pending's value
            Debug.Log("Spawning Bundle");
            bundle = this.SpawnBundle();
            bundle.transform.position = pendingRoot.transform.position;
            bundle.transform.rotation = pendingRoot.transform.rotation;

            // Add base value
            Debug.Log("Adding cash.value to new bundle: " + cash.value);
            bundle.DelayPush(cash.value, pendingCash.value);
            ReturnCash(pendingCash);
            ReturnCash(cash);
        }
        else
        {
            // Add ourselves to the bundle
            Debug.Log("Adding pending cash to value: " + pendingCash.value);
            bundle.DelayPush(pendingCash.value, 0);
            ReturnCash(pendingCash);
        }

        pendingRoot = null;
        pendingCash = null;
    }

    /// <summary>Gets the material for the given cash value</summary>
    public Material GetCashMaterial(int value)
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

    /// <summary>Creates a empty bundle</summary>
    public Bundle SpawnBundle()
    {
        var go = bundlePool.TryToSpawn();
        if (go == null)
        {
            Debug.LogError("Too many bundles out. Cannot create a new one!");
            return null;
        }

        var bundle = go.GetComponent<Bundle>();
        bundle.Clear();
        bundle.bank = this;
        return bundle;
    }
    public void ReturnBundle(Bundle bundle)
    {
        if (bundle.pickup.IsHeld)
            bundle.pickup.Drop();

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
        cash.bank = this;
        cash.value = value;
        return cash;
    }
    public void ReturnCash(Cash cash)
    {
        if (cash.pickup.IsHeld)
            cash.pickup.Drop();

        cashPool.Return(cash.gameObject);
    }

    /// <summary>Spawns new visualisation for the cash.
    /// <para>This only spawns it locally</para>
    /// </summary>
    public GameObject SpawnLocalCashModel(int value)
    {
        var clone = VRCInstantiate(cashModelPrefab);
        clone.SetActive(true);
        clone.GetComponent<Renderer>().material = GetCashMaterial(value);
        clone.transform.position = transform.position;
        return clone;
    }
}
