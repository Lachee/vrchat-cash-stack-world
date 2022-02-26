using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Bundle : UdonSharpBehaviour
{
    public const int MAX_STACK_SIZE = 25;

    [UdonSynced]
    public int[] values = new int[MAX_STACK_SIZE];
    private Renderer[] _children = new Renderer[MAX_STACK_SIZE];

    private int _cursor = -1;

    public bool HasSpace => _cursor < values.Length;
    public int Capacity => values.Length;
    public int Count => _cursor+1;

    public Vector3 cardSize = new Vector3(2, 0.025f, 1f);
    public int totalValue = 0;

    public VRC_Pickup pickup;
    public new Rigidbody rigidbody;
    public BoxCollider boxCollider;

    public Bank bank;

    private int _delayPushFrame = 0;
    private int _pendingPushA = 0;
    private int _pendingPushB = 0;

    /// <summary>The top position relative to the bundle</summary>
    public Vector3 Top 
        => new Vector3(0, boxCollider.bounds.max.y, 0);

    private void Start()
    {
        if (bank == null)
        {
            var bankGO = GameObject.Find("[ BANK ]");
            if (bankGO) bank = bankGO.GetComponent<Bank>();
        }
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        => true;

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        Debug.Log("Pushing AB: " + _pendingPushA + ", " + _pendingPushB);
        Push2(_pendingPushA, _pendingPushB);
        _pendingPushA = 0;
        _pendingPushB = 0;
    }

    public override void OnDeserialization()
    {
        SynchroniseVisuals();
        UpdateSize();
        Debug.Log("Deserialized. Cursor at " + _cursor);
    }

    public override void OnPreSerialization()
    {
    }

    /// <summary>Delays pushing by a frame</summary>
    public void DelayPush(int value, int secondaryValue)
    {
        _pendingPushA = value;
        _pendingPushB = secondaryValue;
        _delayPushFrame = Time.frameCount;
    }

    private void Update()
    {
        if (_delayPushFrame > 0 && _delayPushFrame < Time.frameCount)
            Push2(_pendingPushA, _pendingPushB);
    }

    /// <summary>Pushes two values</summary>
    public void Push2(int value, int secondaryValue)
    {
        _delayPushFrame = 0;

        Debug.Log("Push2 Value: " + value + ", " + secondaryValue);
        if (!Networking.IsOwner(gameObject))
        {
            _pendingPushA = value;
            _pendingPushB = secondaryValue;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            return;
        }

        if (value > 0)
            Push(value);
        if (secondaryValue > 0)
            Push(secondaryValue);
    }

    /// <summary>Pushes a single value</summary>
    public void Push(int value)
    {
        Debug.Log("Push Value: " + value);

        // We are not owner, so lets set ourselves as owner first.
        // SetOwner will trigger OnOwnershipTransferred
        if (!Networking.IsOwner(gameObject))
        {
            _pendingPushA = value;
            _pendingPushB = 0;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            return;
        }

        // If the value is 0, abort.
        // There is no 0 cash.
        if (value <= 0)
            return;

        if (_cursor >= values.Length)
        {
            Debug.LogError("Cannot stack because we have exceeded the size of " + MAX_STACK_SIZE);
            return;
        }


        // Update the arrays
        _cursor++;
        values[_cursor] = value;
        _children[_cursor] = null;
        totalValue += value;

        // Spawn the model and affix its location to our own. 
        // Note: For code-duplication reasons, this can also be a SynchroniseVisuals() call.
        _children[_cursor] = bank.SpawnLocalCashModel(values[_cursor]).GetComponent<Renderer>();
        AttachModel(_cursor);
        
        // Update our visuals
        UpdateSize();
        RequestSerialization();
    }


    /// <summary>Pops cash off the stack</summary>
    public Cash Pop()
    {
        return null;

        if (values == null)
            Clear();


        // Get the model and create a new cash item
        var value = values[_cursor];
        var model = _children[_cursor];

        Debug.Log("Popping " + _cursor + ": " + value);

        // Spawn the cash
        var cash = bank.SpawnCash(value);
        cash.transform.position = Top;
        cash.transform.rotation = transform.rotation;

        // Destroy our model (not required anymore)
        if (model != null)
        {
            cash.transform.position = model.transform.position;
            cash.transform.rotation = model.transform.rotation;
            Debug.Log("Destroying Unneeded Model");
            Destroy(model.gameObject);
        }

        // Clear the values
        values[_cursor] = 0;
        _children[_cursor] = null;
        totalValue -= value;

        // Destroy ourselves if we have nothing left
        _cursor--;
        if (_cursor < 0)
            bank.ReturnBundle(this);

        UpdateSize();
        //RequestSerialization();
        return cash;
    }

    /// <summary>Peeks the top most value from the stack. Returns 0 if there is nothing available</summary>
    public int Peek()
    {
        if (values == null || _cursor < 0 || _cursor >= values.Length)
            return 0;

        return values[_cursor];
    }

    /// <summary>Clears all models and resets the stacks</summary>
    public void Clear()
    {
        if (_children != null)
        {
            foreach (var model in _children)
            {
                if (model && model.gameObject)
                    Destroy(model.gameObject);
            }
        }

        values = new int[MAX_STACK_SIZE];
        _children = new Renderer[MAX_STACK_SIZE];
        UpdateSize();
    }


    public override void OnDrop()
    {
        rigidbody.isKinematic = false;

        // If we only have 1 item left, pop it then destroy outselves
        if (Count == 1) 
            Pop();
    }

    public override void OnPickupUseDown()
    {
        // Pop the cash and add some force to it
        var cash = Pop();
        if (cash)
            cash.rigidbody.AddRelativeForce(Vector3.forward * 5f, ForceMode.Impulse);
    }

    /// <summary>Updates the size of the budnle collider</summary>
    public void UpdateSize()
    {
        rigidbody.isKinematic = true;

        boxCollider.center = new Vector3(0, Count * cardSize.y / 2f, 0);
        boxCollider.size = new Vector3(cardSize.x, Count * cardSize.y, cardSize.z);
    }

    /// <summary>Updates the visualisation of the bundle</summary>
    public void SynchroniseVisuals()
    {
        bool foundEndOfValues = false;
        for (int i = 0; i < values.Length; i++)
        {
            if (!foundEndOfValues && values[i] == 0)
            {
                _cursor = i - 1;
                foundEndOfValues = true;
            }

            // We have reached the end, null out all children
            if (foundEndOfValues)
            {
                if (_children[i] != null)
                {
                    Debug.Log("Destroying Leftover Child: " + i);
                    Destroy(_children[i].gameObject);
                    _children[i] = null;
                }
                else
                {
                   // Debug.Log("Abort Early, reached null child");
                   // return;
                }
            } 
            else if (!foundEndOfValues)
            {
                // otherwise update all the children to match
                if (_children[i] == null)
                {
                    Debug.Log("Needing new child, spawning");
                    _children[i] = bank.SpawnLocalCashModel(values[i]).GetComponent<Renderer>();
                }

                _children[i].material = bank.GetCashMaterial(values[i]);
                AttachModel(i);
            }
        }
    }

    /// <summary>Fixes the model so it is in the correct spot for its given index</summary>
    private void AttachModel(int index)
    {
        _children[index].transform.parent = transform;
        _children[index].transform.localPosition = new Vector3(0, index * cardSize.y, 0);
        _children[index].transform.localRotation = Quaternion.identity;
    }
}
