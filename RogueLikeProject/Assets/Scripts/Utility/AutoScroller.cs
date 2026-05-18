using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

namespace Utility
{
    public class AutoScroller : MonoBehaviour
    {
        /// <summary>
        /// ScrollRectコンポーネント
        /// </summary>
        [Header("ScrollRectコンポーネント")]
        [SerializeField]
        private ScrollRect _scrollRect;

        /// <summary>
        /// スクロールエリアのRectTransform
        /// </summary>
        [Header("スクロールエリアのRectTransform")]
        [SerializeField]
        private RectTransform _viewportRectransform;

        /// <summary>
        /// 縦スクロール情報
        /// </summary>
        [Serializable]
        struct VerticalScrollInfo
        {
            /// <summary>
            /// NodeのRectTransform
            /// </summary>
            [Header("要素のプレハブオブジェクト")]
            public RectTransform nodePrefab;

            /// <summary>
            /// VerticalLayoutGroup(Spacing取得用)
            /// </summary>
            [Header("Spacingが設定されているVerticalLayoutGroup")]
            public VerticalLayoutGroup verticalLayoutGroup;
        }

        /// <summary>
        /// 余白のサイズ
        /// </summary>
        [Header("余白のサイズ（px）")]
        [SerializeField]
        private float margin = 20f;

        /// <summary>
        /// スクロールに掛かる時間
        /// </summary>
        [Header("スクロールに掛かる時間")]
        [SerializeField]
        private float _scrollTime = 0.1f;

        /// <summary>
        /// 縦スクロール情報
        /// </summary>
        [Header("縦スクロール情報 ※縦スクロールを行う際設定")]
        [Tooltip("縦スクロールを行う場合、[グリッドレイアウト]の設定は不要です")]
        [SerializeField]
        private VerticalScrollInfo _verticalScrollInfo;

        /// <summary>
        /// グリッドレイアウト
        /// </summary>
        [Header("グリッドレイアウト ※グリッドを使った縦スクロールを行う際設定")]
        [Tooltip("グリッドを使った縦スクロールを行う場合、[縦スクロール情報]の設定は不要です")]
        [SerializeField]
        private GridLayoutGroup _gridLayout;

        /// <summary>
        /// 列を返す
        /// </summary>
        /// <returns></returns>
        public int GetColumns() => _gridLayout.constraintCount;

        public void VerticalScroll(int nodeIndex, int maxIndex, bool isScrollAnim = true)
        {
            var spacing = _verticalScrollInfo.verticalLayoutGroup.spacing;
            var paddingTop = _verticalScrollInfo.verticalLayoutGroup.padding.top;
            var paddingBottom = _verticalScrollInfo.verticalLayoutGroup.padding.bottom;

            var nodeCount = maxIndex;
            var nodeSize = _verticalScrollInfo.nodePrefab.sizeDelta.y;

            // コンテンツ全体の高さ（padding を含める）
            var contentHeight = paddingTop + paddingBottom
                              + (nodeSize * nodeCount)
                              + (spacing * (nodeCount - 1));

            var p = 1.0f - _scrollRect.verticalNormalizedPosition;

            var viewportSize = _viewportRectransform.rect.height;
            var halfViewport = viewportSize * 0.5f;

            var centerPosition = (contentHeight - viewportSize) * p + halfViewport;
            var topPosition = centerPosition - halfViewport;
            var bottomPosition = centerPosition + halfViewport;

            // ノードの上下端座標（paddingTop を考慮）
            var nodeTop = paddingTop + (nodeSize * nodeIndex) + (spacing * nodeIndex);
            var nodeBottom = nodeTop + nodeSize;

            float scrollTimer = isScrollAnim ? _scrollTime : 0.0f;
            if (!isScrollAnim)
            {
                _scrollRect.DOKill();
            }


            // --- 例外処理: 端の場合は強制的に 0 or 1 ---
            if (nodeIndex == 0)
            {
                _scrollRect.DOVerticalNormalizedPos(1.0f, scrollTimer); // 最上端へ指定秒で移動
                return;
            }
            if (nodeIndex == nodeCount - 1)
            {
                _scrollRect.DOVerticalNormalizedPos(0.0f, scrollTimer); // 最下端へ指定秒で移動
                return;
            }

            // 選択した要素が上側にはみ出ている（上端が viewport より上 + margin）
            if (nodeTop < topPosition + margin)
            {
                var newP = (nodeTop - margin) / (contentHeight - viewportSize);
                float target = 1.0f - Mathf.Clamp01(newP);

                // DOTweenでスムーズに移動（Ease付き）
                _scrollRect.DOVerticalNormalizedPos(target, scrollTimer)
                           .SetEase(Ease.OutCubic);
                return;
            }

            // 選択した要素が下側にはみ出ている（下端が viewport より下 - margin）
            if (nodeBottom > bottomPosition - margin)
            {
                var newP = (nodeBottom + margin - viewportSize) / (contentHeight - viewportSize);
                float target = 1.0f - Mathf.Clamp01(newP);

                _scrollRect.DOVerticalNormalizedPos(target, scrollTimer)
                           .SetEase(Ease.OutCubic);
            }
        }

        public void GridScroll(int nodeIndex, int nodeCount, bool isScrollAnim = true)
        {
            int columns = _gridLayout.constraintCount; // 列数
            int rowCount = Mathf.CeilToInt((float)nodeCount / columns);

            float contentHeight = _gridLayout.padding.top + _gridLayout.padding.bottom
                                + (_gridLayout.cellSize.y * rowCount)
                                + (_gridLayout.spacing.y * (rowCount - 1));

            float p = 1.0f - _scrollRect.verticalNormalizedPosition;
            float viewportSize = _viewportRectransform.rect.height;
            float halfViewport = viewportSize * 0.5f;

            float centerPosition = (contentHeight - viewportSize) * p + halfViewport;
            float topPosition = centerPosition - halfViewport;
            float bottomPosition = centerPosition + halfViewport;

            // ノードの上下端
            int rowIndex = nodeIndex / columns;
            float nodeTop = _gridLayout.padding.top
                          + (_gridLayout.cellSize.y + _gridLayout.spacing.y) * rowIndex;
            float nodeBottom = nodeTop + _gridLayout.cellSize.y;

            float scrollTimer = isScrollAnim ? _scrollTime : 0.0f;

            // --- 例外処理: 端の場合は強制的に 0 or 1 ---
            if (nodeIndex == 0)
            {
                _scrollRect.DOVerticalNormalizedPos(1.0f, scrollTimer).SetEase(Ease.OutCubic); // 最上端へ指定秒で移動
                return;
            }
            if (nodeIndex == nodeCount - 1)
            {
                _scrollRect.DOVerticalNormalizedPos(0.0f, scrollTimer).SetEase(Ease.OutCubic); // 最下端へ指定秒で移動
                return;
            }

            // 上端判定
            if (nodeTop < topPosition + margin)
            {
                float newP = (nodeTop - margin) / (contentHeight - viewportSize);
                float target = 1.0f - Mathf.Clamp01(newP);

                _scrollRect.DOVerticalNormalizedPos(target, scrollTimer).SetEase(Ease.OutCubic); // 指定秒でアニメーション
                return;
            }

            // 下端判定
            if (nodeBottom > bottomPosition - margin)
            {
                float newP = (nodeBottom + margin - viewportSize) / (contentHeight - viewportSize);
                float target = 1.0f - Mathf.Clamp01(newP);

                _scrollRect.DOVerticalNormalizedPos(target, scrollTimer).SetEase(Ease.OutCubic);
            }
        }
    }
}