using MoreMountains.CorgiEngine;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Manager
{
    public class TilemapBoundsManager : MonoBehaviour
    {
        [SerializeField]
        private Tilemap _tilemap;
        [SerializeField] LevelManager _levelManager;

        private Bounds _worldBounds;

        private void Start()
        {
            CalculateBounds();
        }

        /// <summary>
        /// 境界を計算
        /// </summary>
        public void CalculateBounds()
        {
            if (_tilemap == null) return;

            var bounds = _tilemap.cellBounds;
            bool hasTile = false;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            // タイルがあるセルだけ調べる
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (_tilemap.HasTile(pos))
                {
                    hasTile = true;

                    if (pos.x < minX) minX = pos.x;
                    if (pos.x > maxX) maxX = pos.x;
                    if (pos.y < minY) minY = pos.y;
                    if (pos.y > maxY) maxY = pos.y;
                }
            }

            if (!hasTile)
            {
                _worldBounds = new Bounds(Vector3.zero, Vector3.zero);
                return;
            }

            // 左下と右上のセルをワールド座標に変換
            Vector3 minWorld = _tilemap.CellToWorld(new Vector3Int(minX, minY, 0));
            Vector3 maxWorld = _tilemap.CellToWorld(new Vector3Int(maxX + 1, maxY + 1, 0));


            Vector2 cameraWorldSize = GetCameraWorldSize();
            Vector2 min = new Vector2(-(cameraWorldSize.x * 0.5f), -(cameraWorldSize.y * 0.5f));
            Vector2 max = new Vector2((cameraWorldSize.x * 0.5f), (cameraWorldSize.y * 0.5f));

            if (minWorld.x >= min.x)
            {
                minWorld.x = min.x;
                minWorld.x -= 0.01f;
            }
            if (maxWorld.x <= max.x)
            {
                maxWorld.x = max.x;
                maxWorld.x += 0.01f;
            }
            if (minWorld.y >= min.y)
            {
                minWorld.y = min.y;
                minWorld.y -= 0.01f;
            }
            if (maxWorld.y <= max.y)
            {
                maxWorld.y = max.y;
                maxWorld.y += 0.01f;
            }


            // 境界を作成
            _worldBounds = new Bounds();
            _worldBounds.SetMinMax(minWorld, maxWorld);

            _levelManager.LevelBounds = _worldBounds;
        }

        public static Vector2 GetCameraWorldSize()
        {
            Camera cam = Camera.main;

            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;

            return new Vector2(width, height);
        }

#if UNITY_EDITOR

        void OnDrawGizmos()
        {
            // プレイモードか、TileMapがNullの場合処理を行わない
            bool isReturn = EditorApplication.isPlaying || _tilemap == null;

            // プレイモード中なら何もしない
            if (isReturn)
            {
                return;
            }

            // カメラのワールドサイズ
            Vector2 camaraSize = GetCameraWorldSize();
            Vector2 camaraMin = new Vector2(-(camaraSize.x * 0.5f), -(camaraSize.y * 0.5f));
            Vector2 camaraMax = new Vector2((camaraSize.x * 0.5f), (camaraSize.y * 0.5f));
            // カメラの上下左右座標
            Vector2 leftTop = new Vector2(camaraMin.x, camaraMax.y);
            Vector2 leftBottom = new Vector2(camaraMin.x, camaraMin.y);
            Vector2 rightTop = new Vector2(camaraMax.x, camaraMax.y);
            Vector2 rightBottom = new Vector2(camaraMax.x, camaraMin.y);
            // カメラの範囲描画
            Gizmos.color = Color.green;
            Gizmos.DrawLine(leftTop, leftBottom);
            Gizmos.DrawLine(leftBottom, rightBottom);
            Gizmos.DrawLine(rightBottom, rightTop);
            Gizmos.DrawLine(rightTop, leftTop);

            // 境界を計算
            CalculateBounds();
            // ステージの境界範囲描画
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_worldBounds.center, _worldBounds.size);
        }
#endif
    }

}