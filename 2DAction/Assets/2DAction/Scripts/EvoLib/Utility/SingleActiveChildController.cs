using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EvoLib.Utility
{
    /// <summary>
    /// 子オブジェクトのアクティブ状態を「常に 1 つだけ」に制御するエディタ用コンポーネント。<br/>
    /// <br/>
    /// ・インスペクターで有効化すると、複数の子オブジェクトが同時にアクティブになることを防ぎ、<br/>
    ///   1 つの子だけがアクティブになるよう自動で調整する。<br/>
    /// ・Hierarchy の変更を監視し、ユーザーが別の子をアクティブにした場合は、<br/>
    ///   それ以外の子を自動的に非アクティブ化する。<br/>
    /// ・エディタモード専用の動作であり、実行中（Play Mode）には影響しない。<br/>
    /// <br/>
    /// 主な用途：<br/>
    /// ・UI のバリエーションを切り替えるプレハブ管理<br/>
    /// ・複数の子オブジェクトから「どれか 1 つだけ表示したい」ケース<br/>
    /// ・アニメーション、モデル、UI 状態などの切り替え確認<br/>
    /// <br/>
    /// Odin Inspector を使用しており、インスペクター上での操作性が向上している。<br/>
    /// </summary>
    [AddComponentMenu("子オブジェクト単一表示コントローラー")]
    [ExecuteInEditMode]
    public class SingleActiveChildController : MonoBehaviour
    {
        /// <summary>
        /// 単一表示を有効化するかどうか
        /// </summary>
        [SerializeField]
        [LabelWidth(150)]
        [LabelText("単一表示を有効化するかどうか")]
        [Tooltip("✓を付けると、\n子オブジェクトは一つしかアクティブに出来なくなります。")]
        private bool _exclusiveActive = true;

        /// <summary>
        /// 最後にアクティブ状態だったトランスフォーム
        /// </summary>
        private Transform _lastActiveTransform;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                // 子オブジェクト非アクティブ化
                DeactivateAllChildren();

                EditorApplication.hierarchyChanged += OnHierarchyChanged;
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying && _exclusiveActive)
            {
                // bool が ON に切り替わった瞬間に全非表示
                DeactivateAllChildren();
            }
        }

        /// <summary>
        /// 子オブジェクトをすべて非アクティブにする
        /// </summary>
        private void DeactivateAllChildren()
        {
            foreach (Transform child in this.transform)
            {
                child.gameObject.SetActive(false);
            }
        }

        private void OnHierarchyChanged()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!_exclusiveActive)
            {
                return;
            }

            // アクティブ状態の子オブジェクトのトランスフォームを格納
            Transform activeChild = null;
            int activeCount = 0;

            // 現在アクティブ状態の子を探し、
            foreach (Transform child in this.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    activeChild = child;
                    activeCount++;
                }
            }

            // アクティブ数が1以下であればここで処理を終了する
            if (activeCount <= 1)
            {
                _lastActiveTransform = activeChild;
                return;
            }

            // 新たにアクティブ状態になった子オブジェクト以外を非アクティブにする
            foreach (Transform child in this.transform)
            {
                if (child.gameObject.activeSelf &&
                    child.gameObject.transform != _lastActiveTransform)
                {
                    child.gameObject.SetActive(true);
                    _lastActiveTransform.gameObject.SetActive(false);
                    _lastActiveTransform = child.gameObject.transform;
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }
    }
}