using UnityEngine;

/// <summary>
/// 迷路CSVデータを読み込み、Sceneビュー上にGizmo（ホログラム）として
/// 壁と通路を色分け表示するスクリプトです。
/// 実体のGameObjectは一切生成しないため、Hierarchyを汚さず軽量に動作します。
/// </summary>
public class MazeGizmoDisplay : MonoBehaviour
{
    [Header("迷路データ")]
    [Tooltip("Resources/MazeData フォルダ内のCSVファイル（拡張子なしで指定）")]
    public string mazeDataResourcePath = "MazeData/maze_block_01";

    [Header("描画設定")]
    [Tooltip("1マスのワールド内サイズ（メートル）")]
    public float cellSize = 1.0f;

    [Tooltip("ギズモの描画高さ（Y座標）")]
    public float drawHeight = 0.5f;

    [Tooltip("壁マスの色")]
    public Color wallColor = new Color(0.9f, 0.2f, 0.2f, 0.35f);

    [Tooltip("通路マスの色")]
    public Color pathColor = new Color(0.2f, 0.6f, 1.0f, 0.25f);

    [Tooltip("ゲート（出入口）マスの色")]
    public Color gateColor = new Color(0.2f, 1.0f, 0.4f, 0.5f);

    [Header("表示オプション")]
    [Tooltip("壁を表示するか")]
    public bool showWalls = true;

    [Tooltip("通路を表示するか")]
    public bool showPaths = true;

    [Tooltip("ワイヤーフレーム表示にするか（falseの場合は塗りつぶし）")]
    public bool wireframeMode = false;

    // 内部データ
    private int _width;
    private int _height;
    private int[,] _mazeData;
    private bool _dataLoaded = false;

    // ゲート判定用の定数（Pythonスクリプトと一致させる）
    private const int GATE_WIDTH = 5;

    /// <summary>
    /// インスペクターの値が変更された時にデータを再読み込みします
    /// </summary>
    private void OnValidate()
    {
        LoadMazeData();
    }

    private void Awake()
    {
        LoadMazeData();
    }

    /// <summary>
    /// Resources フォルダからCSVデータを読み込んで2D配列に格納します
    /// </summary>
    public void LoadMazeData()
    {
        if (string.IsNullOrEmpty(mazeDataResourcePath))
        {
            _dataLoaded = false;
            return;
        }

        TextAsset textAsset = Resources.Load<TextAsset>(mazeDataResourcePath);
        if (textAsset == null)
        {
            Debug.LogWarning($"[MazeGizmoDisplay] リソースが見つかりません: {mazeDataResourcePath}");
            _dataLoaded = false;
            return;
        }

        string[] lines = textAsset.text.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogWarning("[MazeGizmoDisplay] CSVデータが不正です。");
            _dataLoaded = false;
            return;
        }

        // ヘッダ行を読み込み: 幅,高さ
        string[] header = lines[0].Trim().Split(',');
        if (header.Length < 2 ||
            !int.TryParse(header[0], out _width) ||
            !int.TryParse(header[1], out _height))
        {
            Debug.LogWarning("[MazeGizmoDisplay] ヘッダ行の解析に失敗しました。");
            _dataLoaded = false;
            return;
        }

        _mazeData = new int[_height, _width];

        for (int y = 0; y < _height && y + 1 < lines.Length; y++)
        {
            string line = lines[y + 1].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cells = line.Split(',');
            for (int x = 0; x < _width && x < cells.Length; x++)
            {
                if (int.TryParse(cells[x], out int value))
                {
                    _mazeData[y, x] = value;
                }
            }
        }

        _dataLoaded = true;
        Debug.Log($"[MazeGizmoDisplay] 迷路データを読み込みました: {_width}x{_height}");
    }

    /// <summary>
    /// 指定座標がゲート（出入口）かどうかを判定します
    /// </summary>
    private bool IsGateCell(int x, int y)
    {
        int halfGate = GATE_WIDTH / 2;
        int gateCenterX = _width / 2;
        int gateCenterY = _height / 2;

        // 上辺ゲート
        if (y == 0 && Mathf.Abs(x - gateCenterX) <= halfGate) return true;
        // 下辺ゲート
        if (y == _height - 1 && Mathf.Abs(x - gateCenterX) <= halfGate) return true;
        // 左辺ゲート
        if (x == 0 && Mathf.Abs(y - gateCenterY) <= halfGate) return true;
        // 右辺ゲート
        if (x == _width - 1 && Mathf.Abs(y - gateCenterY) <= halfGate) return true;

        return false;
    }

    /// <summary>
    /// Sceneビューにギズモ（ホログラム）を描画します
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!_dataLoaded || _mazeData == null) return;

        Vector3 origin = transform.position;
        Vector3 cubeSize = new Vector3(cellSize, 0.1f, cellSize);

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                bool isPath = _mazeData[y, x] == 1;
                bool isGate = isPath && IsGateCell(x, y);

                // 表示フィルタリング
                if (isPath && !showPaths && !isGate) continue;
                if (!isPath && !showWalls) continue;

                // 色の決定
                if (isGate)
                {
                    Gizmos.color = gateColor;
                }
                else if (isPath)
                {
                    Gizmos.color = pathColor;
                }
                else
                {
                    Gizmos.color = wallColor;
                }

                // ワールド座標の計算（Yは「上」、X-Z平面に迷路を展開）
                // 迷路のY軸 → UnityのZ軸に対応させる
                Vector3 pos = origin + new Vector3(x * cellSize, drawHeight, y * cellSize);

                if (wireframeMode)
                {
                    Gizmos.DrawWireCube(pos, cubeSize);
                }
                else
                {
                    Gizmos.DrawCube(pos, cubeSize);
                }
            }
        }

        // 外枠のワイヤー表示（全体サイズの把握用）
        Gizmos.color = Color.yellow;
        Vector3 center = origin + new Vector3(
            _width * cellSize * 0.5f,
            drawHeight,
            _height * cellSize * 0.5f
        );
        Vector3 totalSize = new Vector3(_width * cellSize, 0.2f, _height * cellSize);
        Gizmos.DrawWireCube(center, totalSize);
    }
}
