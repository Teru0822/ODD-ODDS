using UnityEngine;
using System.Collections;

/// <summary>
/// 人形（_doll）の動作をテストするための仮スクリプト
/// 動作確認後、本番の制御システムが完成したら削除されます
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class _TempDollTest : MonoBehaviour
{
    private PlayerMovement _playerMovement;

    private void Start()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        
        // 起動から1秒後に指定方向への移動をテストする
        StartCoroutine(TestMovementRoutine());
    }

    private IEnumerator TestMovementRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        
        Debug.Log("[_TempDollTest] テスト開始: 上に2マス移動を試みます...");
        _playerMovement.Move(DirectionType.Up, 2);

        yield return new WaitForSeconds(1.0f);

        Debug.Log("[_TempDollTest] テスト継続: 右に1マス移動を試みます...");
        _playerMovement.Move(DirectionType.Right, 1);
        
        yield return new WaitForSeconds(1.0f);

        Debug.Log("[_TempDollTest] テスト継続: 下に5マス移動を試みます... (盤面外に到達するかのテスト)");
        _playerMovement.Move(DirectionType.Down, 5);
    }
}
