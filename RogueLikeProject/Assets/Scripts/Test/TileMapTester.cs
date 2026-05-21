using MoreMountains.CorgiEngine;
using NUnit.Framework.Internal;
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


        Vector2 cameraWorldSize = GetCameraWorldSize();
        Vector2 min = new Vector2(-(cameraWorldSize.x * 0.5f), -(cameraWorldSize.y * 0.5f));
        Vector2 max = new Vector2((cameraWorldSize.x * 0.5f), (cameraWorldSize.y * 0.5f));
        Debug.Log(minWorld);
        Debug.Log(maxWorld);
        Debug.Log(min);
        Debug.Log(max);

        if (minWorld.x >= min.x)
        {
            minWorld.x = min.x;
        }
        if (maxWorld.x <= max.x)
        {
            maxWorld.x = max.x;
        }
        if(minWorld.y >= min.y)
        {
            minWorld.y = min.y; 
        }
        if (maxWorld.y <= max.y)
        {
            maxWorld.y = max.y;
        }


            // 境界を作成
            worldBounds = new Bounds();
        worldBounds.SetMinMax(minWorld, maxWorld);

        _levelManager.LevelBounds = worldBounds;
    }

    public static Vector2 GetCameraWorldSize()
    {
        Camera cam = Camera.main;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        return new Vector2(width, height);
    }

    void OnDrawGizmos()
    {
        if (tilemap == null) return;
        Vector2 test = GetCameraWorldSize();
        Vector2 min = new Vector2(-(test.x * 0.5f), -(test.y * 0.5f));
        Vector2 max = new Vector2((test.x * 0.5f), (test.y * 0.5f));

        Vector2 leftTop = new Vector2(min.x, max.y);
        Vector2 leftBottom = new Vector2(min.x, min.y);
        Vector2 rightTop = new Vector2(max.x, max.y);
        Vector2 rightBottom = new Vector2(max.x, min.y);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(leftTop, leftBottom);
        Gizmos.DrawLine(leftBottom, rightBottom);
        Gizmos.DrawLine(rightBottom, rightTop);
        Gizmos.DrawLine(rightTop, leftTop);

        CalculateBounds();

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
    }
}
