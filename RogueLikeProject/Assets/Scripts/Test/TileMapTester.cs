using MoreMountains.CorgiEngine;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBoundsDrawer : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] LevelManager _levelManager;

    private Bounds worldBounds;

    void Update()
    {
        CalculateBounds();
    }

    void CalculateBounds()
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
            worldBounds = new Bounds(Vector3.zero, Vector3.zero);
            return;
        }

        // 左下と右上のセルをワールド座標に変換
        Vector3 minWorld = tilemap.CellToWorld(new Vector3Int(minX, minY, 0));
        Vector3 maxWorld = tilemap.CellToWorld(new Vector3Int(maxX + 1, maxY + 1, 0));

        // 境界を作成
        worldBounds = new Bounds();
        worldBounds.SetMinMax(minWorld, maxWorld);

        _levelManager.LevelBounds = worldBounds;
    }

    void OnDrawGizmos()
    {
        if (tilemap == null) return;

        CalculateBounds();

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
    }
}
