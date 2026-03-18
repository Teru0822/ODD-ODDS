using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 3D空間上のCubeなどをガチャのボタンに見立て、クリックで購入とアニメーションを行うクラス
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractGachaButton : MonoBehaviour, IClickInteractable
{
    [Header("Gacha Settings")]
    [Tooltip("購入するパックの番号")]
    [SerializeField] private int _packId;
    [Tooltip("パックの売価")]
    [SerializeField] private int _price;

    [Header("Animation Settings")]
    [Tooltip("クリック時に押し込まれるZ軸方向の距離")]
    [SerializeField] private float _pushZOffset = 0.05f;
    [Tooltip("押し込まれる、または元に戻る際のアニメーション時間(秒)")]
    [SerializeField] private float _animationDuration = 0.1f;

    private bool _isAnimating = false;
    private Vector3 _originalLocalPosition;

    private void Start()
    {
        _originalLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        // アニメーション中は連続クリックを受け付けないようにする
        if (_isAnimating) return;

        // Mouseによる左クリックを検知
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // マウスポインターがこのオブジェクトに合っていれば OnInteract を呼ぶ
                    if (hit.transform == transform)
                    {
                        OnInteract();
                    }
                }
            }
        }
    }

    /// <summary>
    /// クリックされた際に呼ばれる処理（IClickInteractableの実装）
    /// </summary>
    public void OnInteract()
    {
        // 購入処理を走らせる
        if (PackManager.Instance != null)
        {
            bool isSuccess = PackManager.Instance.BuyPack(_packId, _price);
            Debug.Log(isSuccess);
            if (isSuccess)
            {
                // 購入に成功した場合、Z軸方向に移動するアニメーションを再生する
                StartCoroutine(PushAnimationCoroutine());
            }
        }
        else
        {
            Debug.LogError("[GachaButton] PackManagerがシーンに存在しません。");
        }
    }

    /// <summary>
    /// Z軸方向に多少移動させ、元に戻すアニメーションコルーチン
    /// </summary>
    private IEnumerator PushAnimationCoroutine()
    {
        _isAnimating = true;

        Vector3 targetPosition = _originalLocalPosition + new Vector3(0, 0, _pushZOffset);
        float elapsedTime = 0f;

        // Z軸方向へ押し込む
        while (elapsedTime < _animationDuration)
        {
            transform.localPosition = Vector3.Lerp(_originalLocalPosition, targetPosition, (elapsedTime / _animationDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = targetPosition;

        // 少し待機
        yield return new WaitForSeconds(0.05f);

        // 元の位置に戻る
        elapsedTime = 0f;
        while (elapsedTime < _animationDuration)
        {
            transform.localPosition = Vector3.Lerp(targetPosition, _originalLocalPosition, (elapsedTime / _animationDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = _originalLocalPosition;

        _isAnimating = false;
    }
}
