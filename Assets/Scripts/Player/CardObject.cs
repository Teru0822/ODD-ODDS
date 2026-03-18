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
    private HandManager _handManager;

    [Header("Visual References")]
    [Tooltip("カードの絵柄を表示するSpriteRenderer（あれば割り当て）")]
    public SpriteRenderer iconRenderer;
    [Tooltip("カードの名前を表示する3Dテキスト等があれば（オプション）")]
    public TMPro.TextMeshPro nameText;

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
    public void Initialize(CardData data, HandManager handManager)
    {
        _cardData = data;
        _handManager = handManager;

        // 見た目の反映
        if (_cardData != null)
        {
            if (iconRenderer != null && _cardData.Icon != null)
            {
                iconRenderer.sprite = _cardData.Icon;
            }
            if (nameText != null)
            {
                nameText.text = _cardData.CardName;
            }
            // TODO: SpriteRenderer等がない（Cubeそのままなど）場合、色変えなどで仮表現することも可能
        }
    }

    /// <summary>
    /// 並んでいるカードがプレイヤーにクリック（使用）された時の処理
    /// </summary>
    public void OnInteract()
    {
        if (_cardData == null) return;

        Debug.Log($"[CardObject] {_cardData.CardName} を使用しました！");

        // カードのタイプによる効果の振り分け
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
            // 盤面の主人公を移動させる
            PlayerMovement playerMov = FindObjectOfType<PlayerMovement>();
            if (playerMov != null)
            {
                playerMov.Move(moveCard.Direction, moveCard.Steps);
            }

            // オブジェクトの動きが見えるように盤面監視モードへ強制フォーカスを促す
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
            {
                // 人形(PlayerMovement)の位置を見下ろす視点へ戻る、もしくは全体視点へ
                // 現在すでにズームしている場合はそこにとどまり、手札のみクリアでヨシとするかの設計次第。
                // とりあえず手元UIなどを片付けて盤面を見るため、視点をリセットさせる。
                cam.ResetToFollow();
            }
        }
        else if (_cardData is ItemCardData itemCard)
        {
            // TODO: アイテム効果の実装
            Debug.Log($"[CardObject] アイテム効果（{itemCard.EffectType}）は未実装です。");
        }

        // 使用後は自分自身を手札から取り除き、破棄する
        if (_handManager != null)
        {
            _handManager.RemoveCardFromHand(gameObject);
        }
        Destroy(gameObject);
    }
}
