using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// マネー（お金）オブジェクトをクリックした際の挙動を制御します
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractMoney : MonoBehaviour, IClickInteractable
{
    [Header("UI Controls")]
    [Tooltip("クリック時に表示・非表示を切り替えるCanvasオブジェクト（ローグライク様子 など）")]
    public GameObject moneyCanvas;

    private void Update()
    {
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

    public void OnInteract()
    {
        if (moneyCanvas != null)
        {
            // マネーをクリックしたときは表示する
            moneyCanvas.SetActive(true);
            Debug.Log("[InteractMoney] Money UI Visibility set to TRUE.");
        }
        else
        {
            Debug.LogWarning("[InteractMoney] Money Canvas がアタッチされていません。");
        }
    }

    /// <summary>
    /// 他のオブジェクトをクリックした際やESCキーで呼び出される
    /// </summary>
    public void HideUI()
    {
        if (moneyCanvas != null && moneyCanvas.activeSelf)
        {
            moneyCanvas.SetActive(false);
            Debug.Log("[InteractMoney] Money UI Hidden.");
        }
    }
}
