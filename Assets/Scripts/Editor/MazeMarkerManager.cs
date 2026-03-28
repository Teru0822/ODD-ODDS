using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 迷路上に配置可能なマーカーの種類を定義する列挙型
/// </summary>
public enum MazeMarkerType
{
    None,       // マーカーなし
    Event,      // イベントマス（宝箱など）
    Trap,       // トラップマス
    Goal,       // ゴール
    Start,      // スタート地点
    Shop        // ショップマス
}

/// <summary>
/// 迷路上の1マスに配置されたマーカー情報を保持するクラス
/// </summary>
[System.Serializable]
public class MazeMarkerData
{
    public Vector2Int gridPosition;
    public MazeMarkerType markerType;

    public MazeMarkerData(Vector2Int pos, MazeMarkerType type)
    {
        gridPosition = pos;
        markerType = type;
    }
}

/// <summary>
/// 迷路のイベントマス・トラップ・ゴール等をSceneビュー上で
/// クリック編集できるマーカー管理スクリプトです。
/// MazeGizmoDisplay と組み合わせて使用します。
/// </summary>
public class MazeMarkerManager : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("同じオブジェクトまたは別オブジェクトにアタッチされた MazeGizmoDisplay")]
    public MazeGizmoDisplay mazeGizmoDisplay;

    [Header("マーカーリスト")]
    [Tooltip("配置済みのマーカー一覧（インスペクターで確認・編集可能）")]
    public List<MazeMarkerData> markers = new List<MazeMarkerData>();

    [Header("エディタ用: 現在選択中のマーカー種別")]
    [Tooltip("Sceneビューでクリック時に配置されるマーカーの種類")]
    public MazeMarkerType currentBrushType = MazeMarkerType.Event;

    [Header("表示設定")]
    [Tooltip("マーカーのギズモサイズ")]
    public float markerSize = 1.2f;

    [Tooltip("マーカーの描画高さ（上に浮かせる）")]
    public float markerHeight = 1.0f;

    /// <summary>
    /// 指定座標にマーカーを追加します（既存なら上書き）
    /// </summary>
    public void SetMarker(Vector2Int pos, MazeMarkerType type)
    {
        // 既存マーカーを検索
        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i].gridPosition == pos)
            {
                if (type == MazeMarkerType.None)
                {
                    // Noneが指定された場合は削除
                    markers.RemoveAt(i);
                    return;
                }
                markers[i].markerType = type;
                return;
            }
        }

        // 新規追加
        if (type != MazeMarkerType.None)
        {
            markers.Add(new MazeMarkerData(pos, type));
        }
    }

    /// <summary>
    /// 指定座標のマーカーを削除します
    /// </summary>
    public void RemoveMarker(Vector2Int pos)
    {
        markers.RemoveAll(m => m.gridPosition == pos);
    }

    /// <summary>
    /// 指定座標のマーカーを取得します（なければnull）
    /// </summary>
    public MazeMarkerData GetMarker(Vector2Int pos)
    {
        return markers.Find(m => m.gridPosition == pos);
    }

    /// <summary>
    /// マーカー種別ごとの色を返します
    /// </summary>
    private Color GetMarkerColor(MazeMarkerType type)
    {
        switch (type)
        {
            case MazeMarkerType.Event: return new Color(1f, 0.85f, 0.1f, 0.8f);   // 金色
            case MazeMarkerType.Trap:  return new Color(1f, 0.15f, 0.15f, 0.8f);   // 赤
            case MazeMarkerType.Goal:  return new Color(0.1f, 1f, 0.3f, 0.8f);     // 緑
            case MazeMarkerType.Start: return new Color(0.3f, 0.7f, 1f, 0.8f);     // 水色
            case MazeMarkerType.Shop:  return new Color(0.8f, 0.4f, 1f, 0.8f);     // 紫
            default: return Color.white;
        }
    }

    /// <summary>
    /// Sceneビューにマーカーをギズモとして描画します
    /// </summary>
    private void OnDrawGizmos()
    {
        if (mazeGizmoDisplay == null) return;

        float cellSize = mazeGizmoDisplay.cellSize;
        Vector3 origin = mazeGizmoDisplay.transform.position;

        foreach (var marker in markers)
        {
            Gizmos.color = GetMarkerColor(marker.markerType);

            Vector3 pos = origin + new Vector3(
                marker.gridPosition.x * cellSize,
                markerHeight,
                marker.gridPosition.y * cellSize
            );

            // ダイヤモンド型（回転した立方体）で表示
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0, 45, 0), Vector3.one);
            Gizmos.DrawCube(Vector3.zero, new Vector3(markerSize, markerSize * 0.5f, markerSize));
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(markerSize, markerSize * 0.5f, markerSize));
            Gizmos.matrix = oldMatrix;
        }
    }
}
