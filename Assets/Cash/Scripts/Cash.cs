
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class Cash : UdonSharpBehaviour
{
    /// <summary>How much is this card worth</summary>
    [UdonSynced]
    public int value = 1;

    public new Rigidbody rigidbody;
    public VRCObjectSync objectSync;
    public VRCPickup pickup;
    public BoxCollider stackCollider;
    public BoxCollider boxCollider;

    public MeshRenderer stackRenderer;

    public Transform model;

    public Bank bank;
    public Bundle bundle;
    public bool isTopStack = false;

    /// <summary>Current bundle / cash pile pending for us to join them</summary>
    private Cash _currentPendingBundle;


    public float snapAngle = 22.5f;

    private void Start()
    {
        var bankGO = GameObject.Find("[ BANK ]");
        if (bankGO) bank = bankGO.GetComponent<Bank>();
        SetBundle(null, true);
    }

    public override void OnDeserialization()
    {
        UpdateMaterial();
    }

    /// <summary>Updates the materials based of what the bank has available</summary>
    public void UpdateMaterial()
    {
        // TODO: Remove this
        var renderer = (Renderer)model.GetComponent(typeof(Renderer));
        renderer.material = bank.GetMaterialForValue(value);
    }

    public void SetBundle(Bundle bundle, bool isTopStack)
    {
        Debug.Log("SetBundle Start");
        isTopStack = isTopStack || bundle == null;

        Debug.Log("SetBundle Set Variables");
        this.bundle = bundle;
        this.isTopStack = isTopStack;

        if (this.pickup.IsHeld)
        {
            Debug.Log("SetBundle Drop Pickup");
            this.pickup.Drop();
        }

        if (bundle != null)
        {
            Debug.Log("SetBundle Mark Kinematic");
            rigidbody.isKinematic = true;
            //objectSync.enabled = false;

            Debug.Log("SetBundle Disable Pickup");
            pickup.pickupable = false;
            boxCollider.enabled = false;
        }
        else
        {
            Debug.Log("SetBundle Mark Non-Kinematic");
            rigidbody.isKinematic = false;
            //objectSync.enabled = true;

            Debug.Log("SetBundle Enable Pickup");
            pickup.pickupable = true;
            boxCollider.enabled = true;
        }

        Debug.Log("SetBundle Set Stack Info");
        stackCollider.enabled = isTopStack;
        stackRenderer.enabled = false;


        // Clear what we were doing
        Debug.Log("SetBundle Clear Pending");
        _currentPendingBundle = null;
        Debug.Log("SetBundle End");
    }

    private void Update()
    {
        if (pickup.IsHeld)
        {
            Vector3 targetPosition = transform.position;
            Quaternion targetRotation = transform.rotation;

            if (_currentPendingBundle != null)
            {
                targetPosition = _currentPendingBundle.transform.position + Vector3.up * 0.025f;
                targetRotation = _currentPendingBundle.transform.rotation;
            }

            model.transform.rotation = Quaternion.Slerp(model.transform.rotation, targetRotation, Time.deltaTime * 90f);
            model.transform.position = Vector3.Lerp(model.transform.position, targetPosition, Time.deltaTime * 10f);
        }
     }

    public override void OnPickup()
    {
        // Disable our own trigger
        stackCollider.enabled = false;
    }

    public override void OnDrop()
    {
        //Reload our previous settings
        SetBundle(bundle, isTopStack);
        model.transform.localRotation = Quaternion.identity;
        model.transform.localPosition = Vector3.zero;
    }

    public override void OnPickupUseDown()
    {
        if (_currentPendingBundle != null)
        {
            Debug.Log("OnPickupUseDown Start");
            //TODO: Join the other cards bundle

            Bundle bundle = _currentPendingBundle.bundle;
            if (bundle == null)
            {
                // Create the bundle and add the root element
                Debug.Log("OnPickupUseDown Spawn Start");
                bundle = bank.SpawnBundle();
                bundle.transform.position = _currentPendingBundle.transform.position;
                bundle.transform.rotation = _currentPendingBundle.transform.rotation;
                bundle.Push(_currentPendingBundle);
                Debug.Log("OnPickupUseDown Spawn End");
            }

            // Push ourselves to their bundle
            _currentPendingBundle = null;
            bundle.Push(this);    
            Debug.Log("OnPickupUseDown End");
        }
    }

    /// <summary>Called when this card has been moved into a bundle trigger</summary>
    public bool TrySetPendingStack(Cash other)
    {
        if (other.pickup.IsHeld) 
            return false;
        if (!pickup.IsHeld)
            return false;
        if (!other.isTopStack)
            return false;

        Vector3 selfPlaneForward = transform.forward;
        selfPlaneForward.y = 0;
        selfPlaneForward.Normalize();

        Vector3 otherPlaneForward = other.transform.forward;
        otherPlaneForward.y = 0;
        otherPlaneForward.Normalize();

        float angle = Vector3.Angle(selfPlaneForward, otherPlaneForward);
        if ((angle > snapAngle && angle < (180 - snapAngle)) || (angle > (180 + snapAngle) && angle < (360 - snapAngle)))
            return false;

        _currentPendingBundle = other;
        return true;
    }

    /// <summary>Called when this card has been removed from a bundle trigger</summary>
    public void TryClearPendingStack(Cash other)
    {
        if (_currentPendingBundle == other)
            _currentPendingBundle = null;
    }


    /* === Tells the held cash that they entered / left our air space === */
    private void OnTriggerStay(Collider other)
    {
        if (!this.isTopStack) return;

        var heldCash = other.GetComponent<Cash>();
        if (heldCash == null) return;

        // Skip because we are being held
        if (heldCash.pickup.IsHeld && !heldCash.pickup.currentPlayer.isLocal)
            return;

        // Skip because we are above
        if (transform.position.y > heldCash.transform.position.y)
            return;

        // Skip if our bundle doesn't actually have free space
        if (bundle && !bundle.HasSpace)
            return;

        stackRenderer.enabled = true;
        heldCash.TrySetPendingStack(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var heldCash = other.GetComponent<Cash>();
        if (heldCash == null) return;

        stackRenderer.enabled = false;
        heldCash.TryClearPendingStack(this);
    }

}
