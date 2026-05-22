#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;



public class UniqueHierarchyChecker : MonoBehaviour
{
#if UNITY_EDITOR
    public enum ConditionType
    {
        ComponentType,  // コンポーネントの重複チェック
        NameCheck       // 名前の重複チェック
    }

    public enum NameMatchType
    {
        Exact,          // 完全一致
        Contains,       // 部分一致
        IgnoreNumber    // 数字を無視して一致
    }

    [Serializable]
    public class CheckCondition
    {
        [HorizontalGroup("row1")]
        [HideLabel]
        public ConditionType Type;

        // コンポーネントチェック用
        [ShowIf("Type", ConditionType.ComponentType)]
        [HorizontalGroup("row2")]
        [HideLabel]
        public MonoBehaviour Component;

        // 名前チェック用
        [ShowIf("Type", ConditionType.NameCheck)]
        [HorizontalGroup("row3")]
        [LabelText("名前")]
        public string Name;

        [ShowIf("Type", ConditionType.NameCheck)]
        [HorizontalGroup("row3")]
        [LabelText("一致方法")]
        public NameMatchType MatchType;
    }

    [Title("🔍 チェック条件リスト")]
    [ListDrawerSettings(Expanded = true)]
    public List<CheckCondition> Conditions = new List<CheckCondition>();

    [Title("📌 検索範囲設定")]
    [LabelText("同じ階層（同じ親）だけをチェックする")]
    public bool CheckSameHierarchyOnly = false;


    private void OnEnable()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += CheckAll;
        }
    }

    private void OnValidate()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += CheckAll;
        }
    }


    private void CheckAll()
    {
        foreach (var cond in Conditions)
        {
            if (cond.Type == ConditionType.ComponentType)
                CheckComponent(cond);
            else
                CheckName(cond);
        }
    }

    //────────────────────────────────────────────
    // コンポーネントチェック
    //────────────────────────────────────────────
    private void CheckComponent(CheckCondition cond)
    {
        if (cond.Component == null) return;

        var type = cond.Component.GetType();

        var all = UnityEngine.Object.FindObjectsByType(
            type,
            FindObjectsSortMode.None
        );

        var filtered = FilterByHierarchy(all);

        if (filtered.Count > 1)
        {
            Debug.LogWarning(
                $"[UniqueHierarchyChecker] {type.Name} が {filtered.Count} 個あります（階層チェック: {CheckSameHierarchyOnly}）。",
                this
            );
        }
    }

    //────────────────────────────────────────────
    // 名前チェック
    //────────────────────────────────────────────
    private void CheckName(CheckCondition cond)
    {
        if (string.IsNullOrEmpty(cond.Name)) return;

        var all = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsSortMode.None
        );

        var filtered = FilterByHierarchy(all);

        int count = 0;

        foreach (var go in filtered)
        {
            switch (cond.MatchType)
            {
            case NameMatchType.Exact:
                if (go.name == cond.Name) count++;
                break;

            case NameMatchType.Contains:
                if (go.name.Contains(cond.Name)) count++;
                break;

            case NameMatchType.IgnoreNumber:
                string stripped = Regex.Replace(go.name, "[0-9]", "");
                if (stripped == cond.Name) count++;
                break;
            }
        }

        if (count > 1)
        {
            Debug.LogWarning(
                $"[UniqueHierarchyChecker] 名前「{cond.Name}」に一致するオブジェクトが {count} 個あります（階層チェック: {CheckSameHierarchyOnly}）。",
                this
            );
        }
    }

    //────────────────────────────────────────────
    // 階層（親）でフィルタリング（Unity 6 対応）
    //────────────────────────────────────────────
    private List<GameObject> FilterByHierarchy(UnityEngine.Object[] all)
    {
        List<GameObject> result = new List<GameObject>();

        // シーン全体チェック
        if (!CheckSameHierarchyOnly)
        {
            foreach (var obj in all)
            {
                var go = ConvertToGameObject(obj);
                if (go != null && go.scene.IsValid())
                    result.Add(go);
            }
            return result;
        }

        // 同じ階層（同じ親）だけチェック
        Transform parent = this.transform.parent;

        foreach (var obj in all)
        {
            var go = ConvertToGameObject(obj);
            if (go == null) continue;

            // ★ Prefab 内のオブジェクトは除外
            if (!go.scene.IsValid())
                continue;

            // ★ 親が同じかどうか
            if (go.transform.parent == parent)
                result.Add(go);
        }

        return result;
    }

    // Object → GameObject を安全に変換（Unity 6 対応）
    private GameObject ConvertToGameObject(UnityEngine.Object obj)
    {
        switch (obj)
        {
        case GameObject go:
            return go;

        case Component comp:
            return comp.gameObject;

        default:
            return null;
        }
    }
#endif
}