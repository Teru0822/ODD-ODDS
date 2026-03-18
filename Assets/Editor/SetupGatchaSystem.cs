using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

/// <summary>
/// ワンクリックで _gatya クリック移動システムをシーンに組み込むエディタツール
/// </summary>
public class SetupGatchaSystem
{
    [MenuItem("Tools/Setup Gatya Click System")]
    public static void Setup()
    {
        // ---- 1. _doll に MovementChargeReceiver を追加 ----
        GameObject doll = GameObject.Find("_doll");
        if (doll == null)
        {
            Debug.LogError("[Setup] _doll がシーンに見つかりません。");
            return;
        }

        MovementChargeReceiver receiver = doll.GetComponent<MovementChargeReceiver>();
        if (receiver == null)
            receiver = doll.AddComponent<MovementChargeReceiver>();

        // ---- 2. _gatya に Collider と GatchaClickHandler を追加 ----
        GameObject gatya = GameObject.Find("_gatya");
        if (gatya == null)
        {
            Debug.LogError("[Setup] _gatya がシーンに見つかりません。");
            return;
        }

        // Collider がなければ BoxCollider を追加
        if (gatya.GetComponent<Collider>() == null)
            gatya.AddComponent<BoxCollider>();

        GatchaClickHandler handler = gatya.GetComponent<GatchaClickHandler>();
        if (handler == null)
            handler = gatya.AddComponent<GatchaClickHandler>();

        handler.chargeReceiver = receiver;

        // ---- 3. UI Canvas / Text を作成 ----
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
        }

        // EventSystem がなければ追加（新Input Systemに対応したモジュールを使う）
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // 新Input System を使っているプロジェクトでは InputSystemUIInputModule を使う
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // 残り移動力テキスト
        GameObject textObj = GameObject.Find("RemainingMovesText");
        Text textComp;
        if (textObj == null)
        {
            textObj = new GameObject("RemainingMovesText");
            textObj.transform.SetParent(canvasObj.transform, false);
            textComp = textObj.AddComponent<Text>();
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.text = "残り移動力: 0";
            textComp.fontSize = 28;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.UpperLeft;

            // 左上に固定
            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot     = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -20);
            rt.sizeDelta = new Vector2(300, 50);
        }
        else
        {
            textComp = textObj.GetComponent<Text>();
        }

        // _doll の receiver に Text を接続
        receiver.remainingMovesText = textComp;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Setup] ガチャクリックシステムのセットアップが完了しました！");
    }
}
