using Custom;
using MoreMountains.CorgiEngine;
using UnityEngine;
using UnityEngine.Tilemaps;
using Utility.Core;

namespace Manager
{
    public class TilemapBoundsManager : SingletonMonoBehaviour<TilemapBoundsManager>
    {
        /// <summary>
        /// カスタムレベルマネージャー
        /// </summary>
        [Header("カスタムレベルマネージャー"),SerializeField]
        CustomLevelManager _levelManager;

        
        /// <summary>
        /// 境界を計算
        /// </summary>
        public void CalculateBounds(Tilemap tilemap)
        {
            if (tilemap == null) return;

            var bounds = tilemap.cellBounds;
            bool hasTile = false;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            // タイルがあるセルだけ調べる
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (tilemap.HasTile(pos))
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
                return;
            }

            // 左下と右上のセルをワールド座標に変換
            Vector3 minWorld = tilemap.CellToWorld(new Vector3Int(minX, minY, 0));
            Vector3 maxWorld = tilemap.CellToWorld(new Vector3Int(maxX + 1, maxY + 1, 0));


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
            Bounds worldBounds = new();
            worldBounds.SetMinMax(minWorld, maxWorld);

            _levelManager.SetStageBounds(worldBounds);

            // カメラのワールドサイズ計算
            Vector2 GetCameraWorldSize()
            {
                Camera cam = Camera.main;

                float height = cam.orthographicSize * 2f;
                float width = height * cam.aspect;

                return new Vector2(width, height);
            }
        }
    }
}