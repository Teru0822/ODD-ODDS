using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// プレイヤーのレベル表記、HP・MPバーUIを自動で更新するクラス
/// </summary>
public class PlayerStatusUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("レベルを表示するテキスト (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI _levelText;

    [Tooltip("HPを表示するスライダー")]
    [SerializeField] private Slider _hpSlider;
    
    [Tooltip("MPを表示するスライダー")]
    [SerializeField] private Slider _mpSlider;

    private void Start()
    {
        if (PlayerStatusManager.Instance != null)
        {
            PlayerStatusManager.Instance.OnLevelChanged += HandleLevelChanged;
            PlayerStatusManager.Instance.OnHPChanged += HandleHPChanged;
            PlayerStatusManager.Instance.OnMPChanged += HandleMPChanged;

            // 初期化時に現在の値を即座に反映させる
            HandleLevelChanged(PlayerStatusManager.Instance.GetCurrentLevel());
            HandleHPChanged(PlayerStatusManager.Instance.GetCurrentHP(), PlayerStatusManager.Instance.GetMaxHP());
            HandleMPChanged(PlayerStatusManager.Instance.GetCurrentMP(), PlayerStatusManager.Instance.GetMaxMP());
        }
        else
        {
            Debug.LogWarning("[PlayerStatusUI] PlayerStatusManager is not found in the scene.");
        }
    }

    private void OnDestroy()
    {
        if (PlayerStatusManager.Instance != null)
        {
            PlayerStatusManager.Instance.OnLevelChanged -= HandleLevelChanged;
            PlayerStatusManager.Instance.OnHPChanged -= HandleHPChanged;
            PlayerStatusManager.Instance.OnMPChanged -= HandleMPChanged;
        }
    }

    /// <summary>
    /// レベルが変化した際にイベントから情報を受け取ってテキストを更新
    /// </summary>
    private void HandleLevelChanged(int currentLevel)
    {
        if (_levelText != null)
        {
            _levelText.text = $"Lv {currentLevel}";
        }
    }

    /// <summary>
    /// HPが変化した際にイベントから情報を受け取ってバーの長さを更新
    /// </summary>
    private void HandleHPChanged(int currentHP, int maxHP)
    {
        if (_hpSlider != null)
        {
            _hpSlider.maxValue = maxHP;
            _hpSlider.value = currentHP;

            if (_hpSlider.fillRect != null)
            {
                _hpSlider.fillRect.gameObject.SetActive(currentHP > 0);
            }
        }
    }

    /// <summary>
    /// MPが変化した際にイベントから情報を受け取ってバーの長さを更新
    /// </summary>
    private void HandleMPChanged(int currentMP, int maxMP)
    {
        if (_mpSlider != null)
        {
            _mpSlider.maxValue = maxMP;
            _mpSlider.value = currentMP;

            if (_mpSlider.fillRect != null)
            {
                _mpSlider.fillRect.gameObject.SetActive(currentMP > 0);
            }
        }
    }
}
