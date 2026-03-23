using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Tabキーで開閉する「レベルアップメニュー」のUI制御クラス
/// </summary>
public class LevelUpMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("メニュー全体のパネル（背景）")]
    [SerializeField] private GameObject _menuPanel;
    
    [Tooltip("現在のレベルを表示するテキスト")]
    [SerializeField] private TextMeshProUGUI _statusText;
    
    [Tooltip("お金を払ってレベルアップするボタン")]
    [SerializeField] private Button _levelUpButton;
    
    [Tooltip("ボタン内に表示するコスト等のテキスト")]
    [SerializeField] private TextMeshProUGUI _buttonText;

    private void Start()
    {
        // 初期状態はメニューを隠す
        if (_menuPanel != null) 
        {
            _menuPanel.SetActive(false);
        }
        
        // ボタンクリック時のイベントを登録
        if (_levelUpButton != null)
        {
            _levelUpButton.onClick.AddListener(OnLevelUpButtonClicked);
        }

        UpdateMenuDisplay(); // 起動時に一度テキストを作っておく
    }

    private void Update()
    {
        // 新しいInput SystemでTabキーの押下を判定
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    /// <summary>
    /// メニューの開閉を切り替える
    /// </summary>
    private void ToggleMenu()
    {
        if (_menuPanel == null) return;
        
        bool isActive = !_menuPanel.activeSelf;
        _menuPanel.SetActive(isActive);

        // 開いた瞬間に、最新のレベル・金額を再取得して表示を更新する
        if (isActive)
        {
            UpdateMenuDisplay();
        }
    }

    /// <summary>
    /// ボタンが押されたときの処理
    /// </summary>
    private void OnLevelUpButtonClicked()
    {
        if (PlayerStatusManager.Instance != null)
        {
            // PlayerStatusManagerのレベルアップ処理を呼ぶ（内部でMoney確認と消費をしてくれる）
            bool success = PlayerStatusManager.Instance.TryLevelUp();

            if (success)
            {
                // レベルアップに成功した場合、表示Lvと次のコストが変わるので表示を更新する
                UpdateMenuDisplay(); 
            }
            else
            {
                // お金が足りない場合のアラートなどの処理（今回はコンソールのみ）
                Debug.Log("[LevelUpMenuUI] レベルアップのボタンを押しましたが、お金が足りません！");
            }
        }
    }

    /// <summary>
    /// メニュー内の「現在のレベル」と「必要コスト」のテキスト表示を更新する
    /// </summary>
    private void UpdateMenuDisplay()
    {
        if (PlayerStatusManager.Instance == null) return;

        int currentLevel = PlayerStatusManager.Instance.GetCurrentLevel();
        int requiredCost = PlayerStatusManager.Instance.GetLevelUpCost();

        if (_statusText != null)
        {
            _statusText.text = $"Current Level: {currentLevel}";
        }

        if (_buttonText != null)
        {
            _buttonText.text = $"Pay {requiredCost} Money to Level Up";
        }
    }
}
