using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 3D空間上のCubeなどをガチャのボタンに見立て、クリックで購入・パック開封・DeckView移動を行うクラス
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractGachaButton : MonoBehaviour, IClickInteractable
{
    [Header("Gacha Settings")]
    [Tooltip("購入するパックの番号")]
    [SerializeField] private int _packId;
    [Tooltip("パックの売価")]
    [SerializeField] private int _price;
    [Tooltip("このボタンをクリック可能にする自販機視点のTransform（対象の視点でないとクリック不可になります）")]
    public Transform gachaViewTarget;

    [Header("Card Drawing")]
    [Tooltip("パック開封後に移動する手元ビューのTransform")]
    public Transform deckViewTarget;
    [Tooltip("カードを展開するHandManagerの参照")]
    public HandManager handManager;

    [Header("Animation Settings")]
    [Tooltip("クリック時に押し込まれるZ軸方向の距離")]
    [SerializeField] private float _pushZOffset = 0.05f;
    [Tooltip("押し込まれる、または元に戻る際のアニメーション時間(秒)")]
    [SerializeField] private float _animationDuration = 0.1f;

    private bool _isAnimating = false;
    private Vector3 _originalLocalPosition;
    private CameraFollow _mainCamera;

    private void Start()
    {
        _originalLocalPosition = transform.localPosition;
        if (Camera.main != null)
            _mainCamera = Camera.main.GetComponent<CameraFollow>();

        // インスペクターで未設定の場合、自動的にシーンから探す
        if (handManager == null)
        {
            handManager = FindObjectOfType<HandManager>();
        }
    }

    private void Update()
    {
        // アニメーション中は連続クリックを受け付けないようにする
        if (_isAnimating) return;

        // 【追加修正】自販機視点の時だけクリックを許可する
        // gachaViewTarget が未設定の場合は制限なしとする（フォールバック）
        if (gachaViewTarget != null && CameraFollow.Instance != null)
        {
            if (!CameraFollow.Instance.IsAtView(gachaViewTarget)) return;
        }

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
    /// クリックされた際に呼ばれる処理（IClickInteractableの実装）
    /// </summary>
    public void OnInteract()
    {
        if (PackManager.Instance == null)
        {
            Debug.LogError("[GachaButton] PackManagerがシーンに存在しません。");
            return;
        }

        // 購入処理
        bool isSuccess = PackManager.Instance.BuyPack(_packId, _price);
        if (!isSuccess) return;

        // ボタンを押し込むアニメーション開始
        StartCoroutine(PushAnimationCoroutine());

        // パックを即座に開封してカードデータを取得
        List<CardData> drawnCards = PackManager.Instance.OpenPack(_packId);
        if (drawnCards == null || drawnCards.Count == 0)
        {
            Debug.LogWarning("[GachaButton] パック開封に失敗しました。");
            return;
        }

        // カメラをDeckViewへ移動してからカードを展開
        if (_mainCamera != null && deckViewTarget != null)
        {
            _mainCamera.MoveToView(deckViewTarget);
        }

        if (handManager != null)
        {
            StartCoroutine(DrawAfterCameraMove(drawnCards));
        }
        else
        {
            Debug.LogWarning("[GachaButton] HandManagerが設定されていないためカード展開をスキップしました。");
        }
    }

    private IEnumerator DrawAfterCameraMove(List<CardData> drawnCards)
    {
        // カメラ移動の滑らかさを待つ（HandManagerのboardViewWaitTimeと同様の仕組み）
        yield return new WaitForSeconds(0.4f);
        handManager.DrawCards(drawnCards);
    }

    /// <summary>
    /// Z軸方向に多少移動させ、元に戻すアニメーションコルーチン
    /// </summary>
    private IEnumerator PushAnimationCoroutine()
    {
        _isAnimating = true;

        Vector3 targetPosition = _originalLocalPosition + new Vector3(0, 0, _pushZOffset);
        float elapsedTime = 0f;

        while (elapsedTime < _animationDuration)
        {
            transform.localPosition = Vector3.Lerp(_originalLocalPosition, targetPosition, elapsedTime / _animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = targetPosition;

        yield return new WaitForSeconds(0.05f);

        elapsedTime = 0f;
        while (elapsedTime < _animationDuration)
        {
            transform.localPosition = Vector3.Lerp(targetPosition, _originalLocalPosition, elapsedTime / _animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = _originalLocalPosition;

        _isAnimating = false;
    }
}
