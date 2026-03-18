using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 移動力（チャージ）を管理するスクリプト。
/// _doll (PlayerMovement) と同じオブジェクトに付ける。
/// 移動力が残っている間だけ WASD / 矢印キーで移動できる。
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class MovementChargeReceiver : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("残り移動力を表示するUI Text（Legacy Text / TMPどちらでも可）")]
    public Text remainingMovesText;

    [Header("State")]
    [SerializeField] private int _remainingMoves = 0;

    private PlayerMovement _movement;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// _gatya などの外部スクリプトから移動力を追加するために呼ぶ
    /// </summary>
    public void AddMoves(int amount)
    {
        _remainingMoves += amount;
        Debug.Log($"[MovementChargeReceiver] +{amount} チャージ。残り: {_remainingMoves}");
        RefreshUI();
    }

    private void Update()
    {
        if (_remainingMoves <= 0) return;

        DirectionType? dir = GetInputDirection();
        if (dir == null) return;

        _movement.Move(dir.Value, 1);
        _remainingMoves--;
        Debug.Log($"[MovementChargeReceiver] 1マス移動。残り: {_remainingMoves}");
        RefreshUI();
    }

    private DirectionType? GetInputDirection()
    {
        if (Keyboard.current == null) return null;

        if (Keyboard.current.wKey.wasPressedThisFrame    || Keyboard.current.upArrowKey.wasPressedThisFrame)    return DirectionType.Up;
        if (Keyboard.current.sKey.wasPressedThisFrame    || Keyboard.current.downArrowKey.wasPressedThisFrame)  return DirectionType.Down;
        if (Keyboard.current.aKey.wasPressedThisFrame    || Keyboard.current.leftArrowKey.wasPressedThisFrame)  return DirectionType.Left;
        if (Keyboard.current.dKey.wasPressedThisFrame    || Keyboard.current.rightArrowKey.wasPressedThisFrame) return DirectionType.Right;
        return null;
    }

    private void RefreshUI()
    {
        if (remainingMovesText != null)
        {
            remainingMovesText.text = $"残り移動力: {_remainingMoves}";
        }
    }
}
