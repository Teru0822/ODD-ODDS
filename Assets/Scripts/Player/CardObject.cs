using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("フローでの移動順序を表示するTextMeshPro（プレハブの子オブジェクトにアタッチ）")]
    public TMPro.TextMeshPro orderNumberText;

    private void Update()
    {
        // UI（Canvas）上のクリックだった場合は3D空間側のクリック処理を行わない
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

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

            // 初期化時は順序番号を非表示にする
            ClearOrderNumber();
        }
    }

    /// <summary>
    /// フロー中の順序番号を表示します
    /// </summary>
    public void SetOrderNumber(int number)
    {
        if (orderNumberText != null)
        {
            orderNumberText.text = number.ToString();
            orderNumberText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// フロー中の順序番号を非表示にします
    /// </summary>
    public void ClearOrderNumber()
    {
        if (orderNumberText != null)
        {
            orderNumberText.text = "";
            orderNumberText.gameObject.SetActive(false);
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
        // 破棄選択モード中の場合は、使用ではなく選択の切り替えを行う
        if (DiscardManager.Instance != null && DiscardManager.Instance.IsDiscarding)
        {
            DiscardManager.Instance.ToggleSelection(this);
            return;
        }

        // 新フロー（自動進行中）のクリック処理
        if (CardFlowManager.Instance != null && CardFlowManager.Instance.IsInFlow)
        {
            switch (CardFlowManager.Instance.CurrentPhase)
            {
                case CardFlowPhase.SelectingMoveOrder:
                    CardFlowManager.Instance.OnMoveCardClicked(this);
                    return;
                case CardFlowPhase.OptionalItemUse:
                    CardFlowManager.Instance.OnItemCardClicked(this);
                    return;
                default:
                    // その他のフェーズではクリックを無視
                    return;
            }
        }

        // --- これ以降は通常のアイテム使用（デッキから展開して使う場合） ---

        if (_isUsed) return;
        _isUsed = true;

        if (_cardData == null) return;

        // 手札デッキから削除
        if (PlayerHand.Instance != null)
        {
            PlayerHand.Instance.RemoveCard(_cardData);
        }

        Debug.Log($"[CardObject] {_cardData.CardName} を通常使用しました！");

        if (_cardData is ItemCardData itemCard)
        {
            switch (itemCard.EffectType)
            {
                case ItemEffectType.AddMoveStep:
                    var playerMov = Object.FindFirstObjectByType<PlayerMovement>();
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
                    if (CardFlowManager.Instance != null && PackManager.Instance != null)
                    {
                        Debug.Log("[CardObject] 保持デッキから Redraw を使用！新規パック分を展開します。");
                        List<CardData> newList = PackManager.Instance.GetRandomCards(5);
                        if (newList != null && newList.Count > 0)
                        {
                            CardFlowManager.Instance.StartFlow(newList);
                        }
                    }
                    break;

                default:
                    Debug.Log($"[CardObject] アイテム効果（{itemCard.EffectType}）は未実装です。");
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"[CardObject] 新フローではデッキからの {_cardData.Type} カードの使用は想定されていません。");
        }

        // 使用後は画面から消す
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
