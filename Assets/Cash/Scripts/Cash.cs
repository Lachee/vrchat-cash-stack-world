
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class Cash : UdonSharpBehaviour
{
    /// <summary>The bank this cash belongs too</summary>
    public Bank bank;
    
    /// <summary>How much is this card worth</summary>
    [UdonSynced]
    public int value = 1;

    public Transform model;

    public VRCPickup pickup;
    public new Rigidbody rigidbody;

    public BoxCollider stackCollider;
    public MeshRenderer stackRenderer;

    public float snapAngle = 22.5f;

    private GameObject pendingBundle;
    private Vector3 pendingAnchor;

    private void Start()
    {
        // Find the bank if it doesnt exist
        if (bank == null)
        {
            var bankGO = GameObject.Find("[ BANK ]");
            if (bankGO) bank = bankGO.GetComponent<Bank>();
        }

        // Update our material
        UpdateModelMaterial();

        // Reset ourselves
        OnDrop();
    }

    public override void OnDeserialization()
    {
        UpdateModelMaterial();
    }

    /// <summary>Updates the materials based of what the bank has available</summary>
    public void UpdateModelMaterial()
    {
        model.GetComponent<Renderer>().material = bank.GetCashMaterial(value);
    }

    private void Update()
    {
        // Animate the cash when we are in stack range.
        if (pickup.IsHeld)
        {
            Vector3 targetPosition = transform.position;
            Quaternion targetRotation = transform.rotation;

            if (pendingBundle != null)
            {
                targetPosition = pendingBundle.transform.TransformPoint(pendingAnchor + Vector3.up * 0.025f);
                targetRotation = pendingBundle.transform.rotation;
            }

            model.transform.rotation = Quaternion.Slerp(model.transform.rotation, targetRotation, Time.deltaTime * 90f);
            model.transform.position = Vector3.Lerp(model.transform.position, targetPosition, Time.deltaTime * 10f);
        }
     }

    public override void OnPickup()
    {
        // Enable our collider
        stackCollider.enabled = true;
        stackRenderer.enabled = true;
    }

    public override void OnDrop()
    {
        // Disable our stacker and reset our models
        stackCollider.enabled = false;
        stackRenderer.enabled = false;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localPosition = Vector3.zero;
    }

    public override void OnPickupUseDown()
    {
        if (pendingBundle == null)
            return;

        // Create the bundle if we are the owner
        if (Networking.IsOwner(gameObject))
            bank.CreateBundle(pendingBundle, this);
    }

    /* === Tells the held cash that they entered / left our air space === */
    private void OnTriggerStay(Collider other)
    {
        // We only care if we are being held
        if (!pickup.IsHeld)
            return;

        if (pickup.currentPlayer != Networking.LocalPlayer)
            return;

        // We want to ensure we are above it. 
        if (transform.position.y <= other.transform.position.y)
            return;

        // See if we are correct angle
        if (snapAngle > 0)
        {
            Vector3 selfPlaneForward = transform.forward;
            selfPlaneForward.y = 0;
            selfPlaneForward.Normalize();

            Vector3 otherPlaneForward = other.transform.forward;
            otherPlaneForward.y = 0;
            otherPlaneForward.Normalize();

            float angle = Vector3.Angle(selfPlaneForward, otherPlaneForward);
            if ((angle > snapAngle && angle < (180 - snapAngle)) || (angle > (180 + snapAngle) && angle < (360 - snapAngle)))
                return;
        }

        // Anchor on top of the card
        Cash otherCash = other.GetComponent<Cash>();
        if (otherCash != null)
        {
            pendingBundle = other.gameObject;
            pendingAnchor = new Vector3(0, 0.025f, 0);
            return;
        }

        // Ontop of a bundle
        Bundle otherBundle = other.GetComponent<Bundle>();
        if (otherBundle != null)
        {
            pendingBundle = other.gameObject;
            pendingAnchor = otherBundle.Top;
            return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == pendingBundle)
            pendingBundle = null;
    }

}
