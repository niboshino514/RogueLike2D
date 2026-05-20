using UnityEngine;
using UnityEditor;
using UnityEditor.Tilemaps;

[CreateAssetMenu(menuName = "Custom Brushes/Offset GameObject Brush")]
public class OffsetGameObjectBrush : GameObjectBrush
{
    public Vector3 offset = Vector3.zero;

    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        base.Paint(grid, brushTarget, position);

        // タイルのワールド座標
        Vector3 cellWorldPos = grid.CellToWorld(position);

        // Tilemap の子に生成されたオブジェクトを探す
        foreach (Transform child in brushTarget.transform)
        {
            // 生成直後のオブジェクトはタイル中心にいる
            if (Vector3.Distance(child.position, cellWorldPos) < 0.01f)
            {
                child.position += offset; // オフセットを適用
            }
        }
    }
}
