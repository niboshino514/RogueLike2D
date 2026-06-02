using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public static class HierarchySoloTool
{
    private static bool soloMode = false;
    private static GameObject soloTarget = null;
    private static Dictionary<GameObject, bool> originalStates = new Dictionary<GameObject, bool>();

    static HierarchySoloTool()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    [MenuItem("Tools/Visibility/Toggle Solo Mode")]
    public static void ToggleSoloMode()
    {
        soloMode = !soloMode;

        if (!soloMode)
        {
            RestoreStates();
            soloTarget = null;
        }

        Debug.Log("Solo Mode: " + (soloMode ? "ON" : "OFF"));
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        if (!soloMode)
            return;

        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null)
            return;

        // Solo ボタンの位置
        Rect buttonRect = new Rect(selectionRect.xMax - 40, selectionRect.y, 40, selectionRect.height);

        if (GUI.Button(buttonRect, "Solo"))
        {
            ApplySolo(obj);
        }

        // 目アイコン（Active）をクリックした場合の検出
        Event e = Event.current;
        if (e.type == EventType.MouseDown && selectionRect.Contains(e.mousePosition))
        {
            // Active の切り替えが起きたとき
            if (obj.activeSelf != (soloTarget == obj))
            {
                ApplySolo(obj);
            }
        }
    }

    private static void ApplySolo(GameObject target)
    {
        if (!soloMode)
            return;

        soloTarget = target;

        originalStates.Clear();

        foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
        {
            if (obj.transform.parent == target.transform.parent)
            {
                originalStates[obj] = obj.activeSelf;
                obj.SetActive(obj == target);
            }
        }

        Debug.Log("Solo: " + target.name);
    }

    private static void RestoreStates()
    {
        foreach (var kv in originalStates)
        {
            if (kv.Key != null)
                kv.Key.SetActive(kv.Value);
        }

        originalStates.Clear();
    }
}
