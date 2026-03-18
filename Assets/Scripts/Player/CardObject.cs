using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 展開された個々のカードにアタッチされ、自身が何のカードデータか（お金・移動など）を保持し、
/// クリックされた際に効果を発動する役割を持つクラス
/// </summary>
[RequireComponent(typeof(Collider))]
public class CardObject : MonoBehaviour, IClickInteractable
{
    private CardData _cardData;
    public CardData CardData => _cardData; // 手札保存時にデータを取得できるように公開
    private HandManager _handManager;
    private Transform _boardViewTarget; // HandManagerから渡される盤面ビュー参照
    private float _cameraWaitTime = 0.8f; // カメラ移動完了を待つ御時間（秒）
    private bool _isUsed = false; // カードが既に使用されたかどうかのフラグ

    [HideInInspector] public bool IsFromPack { get; set; } = false; // パックから出たばかりの新規カードかどうか

    [Header("Visual References")]
    [Tooltip("カードの絵柄を表示するSpriteRenderer（あれば割り当て）")]
    public SpriteRenderer iconRenderer;
    [Tooltip("カードの名前を表示するTextMeshPro（プレハブの子オブジェクトにアタッチされている場合割り当て）")]
    public TMPro.TextMeshPro nameText;
    [Tooltip("カードの効果説明を表示するTextMeshPro（プレハブの子オブジェクトにアタッチ）")]
    public TMPro.TextMeshPro effectText;

    private void Update()
    {
        // 自身のクリック判定（New Input System）
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform == transform)
                    {
                        OnInteract();
                    }
                }
            }
        }
    }

    /// <summary>
    /// カードがドローされた際に HandManager から呼ばれる初期化関数
    /// </summary>
    public void Initialize(CardData data, HandManager handManager, Transform boardViewTarget)
    {
        _cardData = data;
        _handManager = handManager;
        _boardViewTarget = boardViewTarget;
        // HandManagerから待機時間を取得する
        if (handManager != null) _cameraWaitTime = handManager.boardViewWaitTime;

        // 見た目の反映
        if (_cardData != null)
        {
            if (iconRenderer != null && _cardData.Icon != null)
            {
                iconRenderer.sprite = _cardData.Icon;
            }
            if (nameText != null)
            {
                // インスペクター上の名前に日本語が含まれる可能性があるため
                // 確実にアルファベットのみ（種類名）を表示するようにする
                nameText.text = _cardData.Type.ToString().ToUpper();
            }
            // 効果テキストの生成
            if (effectText != null)
            {
                effectText.text = BuildEffectText(_cardData);
            }

            // カードの種類によって色を変える（視覚的な区別）
            ApplyCardColor();
        }
    }

    /// <summary>
    /// カードデータから効果説明テキストを生成する
    /// </summary>
    private string BuildEffectText(CardData data)
    {
        if (data is MoneyCardData money)
            return $"MONEY +{money.Amount}G";
        if (data is MoveCardData move)
            return $"{DirectionLabel(move.Direction)} {move.Steps} STEPS";
        if (data is ItemCardData item)
            return $"ITEM: {item.EffectType}";
        return data.Description;
    }

    private string DirectionLabel(DirectionType dir)
    {
        switch (dir)
        {
            case DirectionType.Up:    return "UP";
            case DirectionType.Down:  return "DOWN";
            case DirectionType.Left:  return "LEFT";
            case DirectionType.Right: return "RIGHT";
            default: return dir.ToString().ToUpper();
        }
    }

    /// <summary>
    /// カードの種類に応じてMeshRendererの色を変える
    /// </summary>
    private void ApplyCardColor()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend == null || _cardData == null) return;

        Color cardColor;
        switch (_cardData.Type)
        {
            case CardType.Money:
                // お金カード：ゴールドっぽい黄色
                cardColor = new Color(1f, 0.85f, 0.1f);
                break;
            case CardType.Move:
                // 移動カード：水色
                cardColor = new Color(0.2f, 0.7f, 1f);
                break;
            case CardType.Item:
                // アイテムカード：緑
                cardColor = new Color(0.2f, 0.9f, 0.3f);
                break;
            default:
                cardColor = Color.white;
                break;
        }

        // Materialをインスタンス化して色を適用（他のカードに影響しないように）
        rend.material = new Material(rend.material);
        rend.material.color = cardColor;
    }

    /// <summary>
    /// 並んでいるカードがプレイヤーにクリック（使用）された時の処理
    /// </summary>
    public void OnInteract()
    {
        // 【新機能】破棄選択モード中の場合は、使用ではなく選択の切り替えを行う
        if (DiscardManager.Instance != null && DiscardManager.Instance.IsDiscarding)
        {
            DiscardManager.Instance.ToggleSelection(this);
            return;
        }

        if (_isUsed) return;
        _isUsed = true;

        if (_cardData == null) return;

        // 【修正】使用時にプレイヤーの保持デッキ（PlayerHand）内からも削除する
        // ※保持デッキから展開中の場合も、これでデータ上の重複を防げる
        if (PlayerHand.Instance != null)
        {
            PlayerHand.Instance.RemoveCard(_cardData);
        }

        Debug.Log($"[CardObject] {_cardData.CardName} を使用しました！");

        // カードのタイプによる効果の振り分け
        bool destroyImmediately = true; // 移動カードはコルーチン内で破棄するため、フラグで制御

        if (_cardData is MoneyCardData moneyCard)
        {
            // 即座にお金を増やす
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AddMoney(moneyCard.Amount);
            }
        }
        else if (_cardData is MoveCardData moveCard)
        {
            // Coroutine終了後に破棄するため、即時破棄はしない
            destroyImmediately = false;

            // ClearHand（カメラ移動でHideAllUIsが呼ばれる）の影響を受けないよう
            // 先に手札リストから自分を外しておく
            if (_handManager != null)
            {
                _handManager.RemoveCardFromHand(gameObject);
            }

            // 保持デッキに残りの展開カードを移す（使用者が展開中に選んでいないカード）
            if (PlayerHand.Instance != null && _handManager != null)
            {
                _handManager.SaveRemainingCardsToPlayerHand();
            }

            // 【重要】上限を超えて破棄選択モードに入った場合は、移動処理（カメラ移動・人形移動）を中断して選択を優先する
            if (DiscardManager.Instance != null && DiscardManager.Instance.IsDiscarding)
            {
                Destroy(gameObject);
                return;
            }

            // カメラをBoxViewへ移動させ、到達後に人形を動かすCoroutineを開始
            CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (cam != null)
            {
                if (_boardViewTarget != null)
                {
                    cam.MoveToView(_boardViewTarget);
                    // カメラ待機後に人形移動・カード破棄
                    StartCoroutine(WaitCameraAndMove(cam, moveCard.Direction, moveCard.Steps));
                }
                else
                {
                    // BoardViewTargetが未設定の場合は即時移動（フォールバック）
                    PlayerMovement playerMov = FindObjectOfType<PlayerMovement>();
                    if (playerMov != null) playerMov.Move(moveCard.Direction, moveCard.Steps);
                    destroyImmediately = true; // フォールバック時は即時破棄
                    Debug.LogWarning("[CardObject] boardViewTarget が未設定のため即時移動します。HandManagerのInspectorで設定してください。");
                }
            }
            else
            {
                // カメラが取得できない場合も即時移動
                PlayerMovement playerMov = FindObjectOfType<PlayerMovement>();
                if (playerMov != null) playerMov.Move(moveCard.Direction, moveCard.Steps);
                destroyImmediately = true;
            }
        }
        else if (_cardData is ItemCardData itemCard)
        {
            switch (itemCard.EffectType)
            {
                case ItemEffectType.AddMoveStep:
                    var playerMov = FindObjectOfType<PlayerMovement>();
                    if (playerMov != null)
                    {
                        if (itemCard.EffectValue == 0)
                        {
                            Debug.LogWarning($"[CardObject] {itemCard.CardName} の EffectValue が 0 です。効果が反映されない可能性があります。");
                        }
                        playerMov.AddMoveBonus(itemCard.EffectValue);
                    }
                    else
                    {
                        Debug.LogWarning("[CardObject] PlayerMovementが見つからないため、移動ボーナスを適用できませんでした。");
                    }
                    break;

                case ItemEffectType.Redraw:
                    if (_handManager != null)
                    {
                        _handManager.RedrawCards();
                    }
                    break;

                default:
                    Debug.Log($"[CardObject] アイテム効果（{itemCard.EffectType}）は未実装です。");
                    break;
            }
        }

        // 移動カード以外は使用後即座に自身を手札から取り除き、破棄する
        if (destroyImmediately)
        {
            if (_handManager != null)
            {
                _handManager.RemoveCardFromHand(gameObject);
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// カメラがboardViewTargetへ到達するのを待ち、到達後に人形を移動させるコルーチン
    /// </summary>
    private IEnumerator WaitCameraAndMove(CameraFollow cam, DirectionType direction, int steps)
    {
        // 設定時間だけ待機してカメラが向くのを待つ
        yield return new WaitForSeconds(_cameraWaitTime);

        // カメラの待機中にオブジェクトが消えた場合のガード
        if (this == null || gameObject == null) yield break;

        // 人形を移動させる
        PlayerMovement playerMov = FindObjectOfType<PlayerMovement>();
        if (playerMov != null)
        {
            playerMov.Move(direction, steps);
        }

        // コルーチン終了後にカードを手札リストから除去・破棄する
        if (_handManager != null)
        {
            _handManager.RemoveCardFromHand(gameObject);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// 選択状態に応じたハイライト表示を切り替えます
    /// </summary>
    public void SetHighlight(bool highlighted, Color color)
    {
        // 子オブジェクトを含めてMeshRendererを探す（カードの見た目が子にある場合を想定）
        var renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            if (highlighted)
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", color * 0.5f);
                r.material.color = color;
            }
            else
            {
                r.material.DisableKeyword("_EMISSION");
                // ハイライト解除時は元のカード色に戻す
                ApplyCardColor();
            }
        }
    }
}
