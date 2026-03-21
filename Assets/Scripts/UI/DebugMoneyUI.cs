using UnityEngine;
using TMPro;

/// <summary>
/// マネー（所持金）を常にデバッグ表示するためのUIスクリプトです。
/// Canvas内のText(TextMeshPro)にアタッチして使用します。
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class DebugMoneyUI : MonoBehaviour
{
    private TextMeshProUGUI _textMesh;

    private void Start()
    {
        _textMesh = GetComponent<TextMeshProUGUI>();
        
        // 初期値を表示
        if (MoneyManager.Instance != null)
        {
            UpdateMoneyText(MoneyManager.Instance.GetCurrentMoney());
            // 金額変更イベントを購読
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;
        }
    }

    private void OnDestroy()
    {
        // イベント購読を解除
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
        }
    }

    private void OnMoneyChanged(int currentMoney, int amount)
    {
        UpdateMoneyText(currentMoney);
    }

    private void UpdateMoneyText(int currentMoney)
    {
        if (_textMesh != null)
        {
            _textMesh.text = $"Money: {currentMoney} G";
        }
    }
}
