using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Bundle : UdonSharpBehaviour
{
    public const int MAX_STACK_SIZE = 25;

    private int[] _values;
    private Transform[] _children;

    private int _cursor = -1;

    public bool HasSpace => _cursor < _values.Length;
    public int Capacity => _values.Length;
    public int Count => _cursor+1;

    public Vector3 cardSize = new Vector3(2, 0.025f, 1f);
    public int totalValue = 0;

    public VRC_Pickup pickup;
    public new Rigidbody rigidbody;
    public BoxCollider boxCollider;

    public Bank bank;

    private void Start()
    {
        var bankGO = GameObject.Find("[ BANK ]");
        if (bankGO) bank = bankGO.GetComponent<Bank>();
    }

    public bool Push(Cash cash)
    {
        if (_values == null)
        {
            _values = new int[MAX_STACK_SIZE];
            _children = new Transform[MAX_STACK_SIZE];
        }

        if (_cursor >= _values.Length)
        {
            Debug.LogError("Cannot stack because we have exceeded the size of " + MAX_STACK_SIZE);
            return false;
        }

        // Push to the stack and update the bundle and values
        _values[++_cursor] = cash.value;
        _children[_cursor] = cash.model;
        totalValue += cash.value;

        //cash.SetBundle(this, true);
        //cash.transform.parent = transform;
        cash.model.parent = transform;
        cash.model.localPosition = new Vector3(0, (Count - 1) * cardSize.y + (cardSize.y / 2f), 0);
        cash.model.localRotation = Quaternion.identity;

        Destroy(cash.gameObject);
        UpdateSize();
        return true;
    }

    public Cash Pop()
    {
        if (_values == null)
            _values = new int[MAX_STACK_SIZE];

        Debug.Log("POP: Creating new Cash");

        // Get the model and create a new cash item
        var value = _values[_cursor];
        var model = _children[_cursor].gameObject;
        
        var cash = bank.SpawnCash(value);
        cash.transform.position = model.transform.position;
        cash.transform.rotation = model.transform.rotation;

        // Destroy our model (not required anymore)
        totalValue -= value;
        Destroy(model.gameObject);
        _cursor--;

        // Destroy ourselves if we have nothing left
        if (_cursor < 0)
            bank.ReturnBundle(this);

        return cash;
        //TODO: Implement POP
        /*
        // Peek the stack, remove, then decrement
        var oldTopCash = Peek();
        if (oldTopCash != null)
        {
            oldTopCash.SetBundle(null, true);
            totalValue -= oldTopCash.value;
            _cursor--;
            UpdateSize();

            // Tell the one below its now top
            var newTopCash = Peek();
            if (newTopCash != null)
                newTopCash.SetBundle(this, true);

            // Clear ourselves out if we have nothing new
            if (_cursor < 0)
                Destroy(gameObject);
        }
        */
    }

    public int Peek()
    {
        if (_values == null || _cursor < 0 || _cursor >= _values.Length)
            return 0;

        return _values[_cursor];
    }


    public override void OnDrop()
    {
        rigidbody.isKinematic = false;

        // If we only have 1 item left, pop it then destroy outselves
        if (Count == 1) Pop();
    }

    public override void OnPickupUseDown()
    {
        var cash = Pop();
        if (cash)
            cash.rigidbody.AddRelativeForce(Vector3.forward * 5f, ForceMode.Impulse);
    }

    public void UpdateSize()
    {
        //canvas.localPosition = new Vector3(0, stack.Count * cardSize.y + 0.01f, 0);
        rigidbody.isKinematic = true;
        boxCollider.center = new Vector3(0, Count * cardSize.y / 2f, 0);
        boxCollider.size = new Vector3(cardSize.x, Count * cardSize.y, cardSize.z);
    }
}
