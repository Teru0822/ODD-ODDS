using UnityEngine;
using System.Collections;

/// <summary>
/// 人形（_doll）の動作をテストするための仮スクリプト
/// ※ MovementChargeReceiver に移行したため、このスクリプトは無効化済み。
/// 本番の制御システムが完成したら削除してください。
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class _TempDollTest : MonoBehaviour
{
    private void Awake()
    {
        // MovementChargeReceiver で制御するため、このコンポーネントは無効化する
        this.enabled = false;
        Debug.Log("[_TempDollTest] 無効化しました。WASDキーとガチャクリックで操作できます。");
    }
}
