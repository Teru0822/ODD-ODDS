using UnityEngine;

/// <summary>
/// マウスクリックなどのインタラクトイベントを受け取るための共通インターフェース
/// </summary>
public interface IClickInteractable
{
    /// <summary>
    /// オブジェクトがクリックや選択された際に呼び出されます
    /// </summary>
    void OnInteract();
}
