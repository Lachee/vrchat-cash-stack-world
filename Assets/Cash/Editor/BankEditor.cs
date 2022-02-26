using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

[CustomEditor(typeof(Bank))]
public class BankEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draws the default convert to UdonBehaviour button, program asset field, sync settings, etc.
        if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
        base.OnInspectorGUI();

        Bank bank = (Bank)target;

        // Adds all the cash to the pool
        if (GUILayout.Button("Populate Cash Pool"))
        {
            var sceneCash = FindUdonSharpObjectsOfTypeEnumerable<Cash>();
            var childCash = bank.GetUdonSharpComponentsInChildren<Cash>(true);
            bank.cashPool.Pool = sceneCash.Union(childCash).Select(c => c.gameObject).ToArray();
        }

        // Adds all the bundles to the pool
        if (GUILayout.Button("Populate Bundle Pool"))
        {
            var sceneBundles = FindUdonSharpObjectsOfTypeEnumerable<Bundle>();
            var childBundles = bank.GetUdonSharpComponentsInChildren<Bundle>(true);
            bank.bundlePool.Pool = sceneBundles.Union(childBundles).Select(c => c.gameObject).ToArray();
        }
    }

    private static IEnumerable<T> FindUdonSharpObjectsOfTypeEnumerable<T>() where T : UdonSharpBehaviour
        => FindObjectsOfType<UdonBehaviour>().Select(ub => ub.gameObject.GetUdonSharpComponent<T>()).Where(component => component != null).ToArray();
    private static T[] FindUdonSharpObjectsOfType<T>() where T : UdonSharpBehaviour
        => FindUdonSharpObjectsOfTypeEnumerable<T>().ToArray();
}
