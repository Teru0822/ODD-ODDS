using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// HPとMPの増減をテストするための仮スクリプト（検証用）
/// ※本格的な戦闘や魔法ダメージシステムが構築されるまでの間、キーボードの数字キーで代用します
/// </summary>
public class _TempStatusTest : MonoBehaviour
{
    private void Update()
    {
        if (PlayerStatusManager.Instance == null) return;

        // New Input Systemのキーボード入力を取得
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 【1】キーで10ダメージ受ける
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            Debug.Log("[_TempStatusTest] 1キー押下: 10ダメージ");
            PlayerStatusManager.Instance.TakeDamage(10);
        }

        // 【2】キーで20回復する
        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            Debug.Log("[_TempStatusTest] 2キー押下: 20回復");
            PlayerStatusManager.Instance.Heal(20);
        }

        // 【3】キーで10MP消費する
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            Debug.Log("[_TempStatusTest] 3キー押下: 10MP消費");
            PlayerStatusManager.Instance.TryConsumeMP(10);
        }

        // 【4】キーで30MP回復する
        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            Debug.Log("[_TempStatusTest] 4キー押下: 30MP回復");
            PlayerStatusManager.Instance.RestoreMP(30);
        }
    }
}
