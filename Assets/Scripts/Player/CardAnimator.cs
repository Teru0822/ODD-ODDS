using System.Collections;
using UnityEngine;

/// <summary>
/// カードが指定した位置へ滑らかに移動・回転するアニメーションを処理します
/// </summary>
public class CardAnimator : MonoBehaviour
{
    private Coroutine _animationCoroutine;

    /// <summary>
    /// 現在の位置から目標の座標・回転へ指定時間かけて移動アニメーションを行います
    /// </summary>
    public void AnimateTo(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }
        _animationCoroutine = StartCoroutine(MoveAndRotateRoutine(targetPosition, targetRotation, duration));
    }

    private IEnumerator MoveAndRotateRoutine(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            // 0 -> 1 に向かう進行度（少し滑らかに減速するイージング SmoothStep）
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        // 最後に確実に目標位置へ合わせる
        transform.position = targetPos;
        transform.rotation = targetRot;
    }
}
