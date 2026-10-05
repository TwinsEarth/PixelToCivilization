#if UNITY_EDITOR && !UNITY_WEBGL
using System;
using UnityEngine;
using UnityEditor;
using PixelToCivilization.Rendering;

namespace PixelToCivilization.EditorTools
{
    public static class LODDiag
    {
        public static void Run()
        {
            try
            {
                var go = new GameObject("DiagRoot");
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(go.transform, false);
                cube.name = "Cube";

                var before = go.GetComponent<LODGroup>();
                Debug.Log("[DIAG] GetComponent result is C# null? " + (before == null)
                          + " | ReferenceEquals null? " + ReferenceEquals(before, null)
                          + " | is null? " + (before is null));

                // Reproduce the ?? pattern
                var lg = go.GetComponent<LODGroup>() ?? go.AddComponent<LODGroup>();
                Debug.Log("[DIAG] after ?? : ReferenceEquals null? " + ReferenceEquals(lg, null)
                          + " | Unity == null? " + (lg == null)
                          + " | type=" + (lg != null ? lg.GetType().Name : "null"));

                try
                {
                    var lods = new LOD[1];
                    lods[0] = new LOD(0.1f, go.GetComponentsInChildren<Renderer>());
                    lg.SetLODs(lods);
                    lg.RecalculateBounds();
                    Debug.Log("[DIAG] SetLODs OK (no throw)");
                }
                catch (Exception e)
                {
                    Debug.Log("[DIAG] SetLODs threw: " + e.GetType().Name + " :: " + e.Message);
                }

                Debug.Log("[DIAG_PASS]");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[DIAG_FAIL] " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
