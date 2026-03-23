#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class SetupStatusUI
{
    [MenuItem("ODD-ODDS/Setup/Setup Status UI")]
    public static void CreateStatusUI()
    {
        // ==========================================
        // 既存のものをクリーンアップ
        // ==========================================
        GameObject existingCanvas = GameObject.Find("PlayerStatusUI");
        if (existingCanvas != null) GameObject.DestroyImmediate(existingCanvas);
        
        GameObject existingMenu = GameObject.Find("LevelUpMenuRoot");
        if (existingMenu != null) GameObject.DestroyImmediate(existingMenu);

        // ==========================================
        // 1. Managerの生成
        // ==========================================
        PlayerStatusManager manager = GameObject.FindObjectOfType<PlayerStatusManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("PlayerStatusManager");
            managerObj.AddComponent<PlayerStatusManager>();
        }

        // ==========================================
        // 2. CanvasとEventSystemの生成
        // ==========================================
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            if (GameObject.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
        }

        // ==========================================
        // 3. 左下ステータスパネル (Lv文字 + HP/MPバー) の生成
        // ==========================================
        GameObject statusPanelObj = new GameObject("PlayerStatusUI");
        statusPanelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = statusPanelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0); 
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(50, 50); 
        panelRect.sizeDelta = new Vector2(400, 150);
        
        VerticalLayoutGroup vLayout = statusPanelObj.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 15;
        vLayout.childAlignment = TextAnchor.LowerLeft; // 下揃えで並べる
        vLayout.childControlHeight = false;
        vLayout.childForceExpandHeight = false;

        PlayerStatusUI statusUIComponent = statusPanelObj.AddComponent<PlayerStatusUI>();

        // 3-1. Level表記テキストの生成 (一番上に置くため最初に生成)
        GameObject levelTextObj = new GameObject("LevelText");
        levelTextObj.transform.SetParent(statusPanelObj.transform, false);
        TextMeshProUGUI levelText = levelTextObj.AddComponent<TextMeshProUGUI>();
        levelText.text = "Lv 1";
        levelText.fontSize = 28;
        levelText.fontStyle = FontStyles.Bold;
        levelText.color = Color.white;
        levelText.alignment = TextAlignmentOptions.BottomLeft;
        
        RectTransform levelRect = levelText.GetComponent<RectTransform>();
        levelRect.sizeDelta = new Vector2(350, 40);

        // 3-2. HPゲージの生成
        GameObject hpSliderObj = DefaultControls.CreateSlider(new DefaultControls.Resources());
        hpSliderObj.name = "HP_Bar";
        hpSliderObj.transform.SetParent(statusPanelObj.transform, false);
        
        Slider hpSlider = hpSliderObj.GetComponent<Slider>();
        hpSlider.interactable = false;
        hpSlider.transition = Selectable.Transition.None;
        hpSlider.maxValue = 100;
        hpSlider.value = 100;
        
        hpSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 30);

        Transform hpBackground = hpSliderObj.transform.Find("Background");
        if (hpBackground != null)
        {
            Image bgImg = hpBackground.GetComponent<Image>();
            bgImg.sprite = null;
            bgImg.color = Color.white;
        }

        Transform hpHandle = hpSliderObj.transform.Find("Handle Slide Area");
        if (hpHandle != null) GameObject.DestroyImmediate(hpHandle.gameObject);

        Transform hpFillArea = hpSliderObj.transform.Find("Fill Area");
        if (hpFillArea != null)
        {
            RectTransform fillAreaRect = hpFillArea.GetComponent<RectTransform>();
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            Transform hpFill = hpFillArea.Find("Fill");
            if (hpFill != null)
            {
                Image fillImg = hpFill.GetComponent<Image>();
                fillImg.sprite = null;
                fillImg.color = new Color(0.2f, 0.8f, 0.2f, 1f); 
            }
        }

        // 3-3. MPゲージの生成
        GameObject mpSliderObj = DefaultControls.CreateSlider(new DefaultControls.Resources());
        mpSliderObj.name = "MP_Bar";
        mpSliderObj.transform.SetParent(statusPanelObj.transform, false);
        
        Slider mpSlider = mpSliderObj.GetComponent<Slider>();
        mpSlider.interactable = false;
        mpSlider.transition = Selectable.Transition.None;
        mpSlider.maxValue = 100;
        mpSlider.value = 100;

        mpSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 20);

        Transform mpBackground = mpSliderObj.transform.Find("Background");
        if (mpBackground != null)
        {
            Image bgImg = mpBackground.GetComponent<Image>();
            bgImg.sprite = null;
            bgImg.color = Color.white;
        }

        Transform mpHandle = mpSliderObj.transform.Find("Handle Slide Area");
        if (mpHandle != null) GameObject.DestroyImmediate(mpHandle.gameObject);

        Transform mpFillArea = mpSliderObj.transform.Find("Fill Area");
        if (mpFillArea != null)
        {
            RectTransform fillAreaRect = mpFillArea.GetComponent<RectTransform>();
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            Transform mpFill = mpFillArea.Find("Fill");
            if (mpFill != null)
            {
                Image fillImg = mpFill.GetComponent<Image>();
                fillImg.sprite = null;
                fillImg.color = new Color(0.1f, 0.6f, 0.9f, 1f); 
            }
        }

        // スクリプトへ参照をアサイン
        SerializedObject soStatus = new SerializedObject(statusUIComponent);
        soStatus.FindProperty("_levelText").objectReferenceValue = levelText;
        soStatus.FindProperty("_hpSlider").objectReferenceValue = hpSlider;
        soStatus.FindProperty("_mpSlider").objectReferenceValue = mpSlider;
        soStatus.ApplyModifiedProperties();

        // ==========================================
        // 4. Tabキーで開くメニュー(LevelUpMenuRoot)の生成
        // ==========================================
        GameObject menuRootObj = new GameObject("LevelUpMenuRoot");
        menuRootObj.transform.SetParent(canvas.transform, false);
        LevelUpMenuUI menuUIComponent = menuRootObj.AddComponent<LevelUpMenuUI>();

        // パネル背景（中央）
        GameObject menuPanelObj = new GameObject("MenuPanel");
        menuPanelObj.transform.SetParent(menuRootObj.transform, false);
        RectTransform menuRect = menuPanelObj.AddComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.5f); // 画面中央
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.anchoredPosition = Vector2.zero;
        menuRect.sizeDelta = new Vector2(600, 300);

        Image menuBg = menuPanelObj.AddComponent<Image>();
        menuBg.color = new Color(0f, 0f, 0f, 0.85f); // 半透明の黒

        VerticalLayoutGroup menuVLayout = menuPanelObj.AddComponent<VerticalLayoutGroup>();
        menuVLayout.spacing = 50;
        menuVLayout.childAlignment = TextAnchor.MiddleCenter;
        menuVLayout.childControlHeight = false;
        menuVLayout.childControlWidth = false;

        // "Current Level" テキスト
        GameObject statusTextObj = new GameObject("CurrentLevelText");
        statusTextObj.transform.SetParent(menuPanelObj.transform, false);
        TextMeshProUGUI menuStatusText = statusTextObj.AddComponent<TextMeshProUGUI>();
        menuStatusText.text = "Current Level: 1";
        menuStatusText.fontSize = 40;
        menuStatusText.color = Color.white;
        menuStatusText.alignment = TextAlignmentOptions.Center;
        menuStatusText.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 60);

        // ボタンの生成 (Unityの標準Buttonを使わず自前で作る)
        GameObject buttonObj = new GameObject("LevelUpButton");
        buttonObj.transform.SetParent(menuPanelObj.transform, false);
        RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(400, 80);
        Image btnImg = buttonObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.2f); // 緑色のボタン
        Button lvlBtn = buttonObj.AddComponent<Button>();

        // ボタンのテキスト
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = btnTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Pay 500 Money to Level Up";
        buttonText.fontSize = 28;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        RectTransform btnTextRect = buttonText.GetComponent<RectTransform>();
        // ボタンにぴったり沿うようにアンカー設定
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        // スクリプトへ参照をアサイン
        SerializedObject soMenu = new SerializedObject(menuUIComponent);
        soMenu.FindProperty("_menuPanel").objectReferenceValue = menuPanelObj;
        soMenu.FindProperty("_statusText").objectReferenceValue = menuStatusText;
        soMenu.FindProperty("_levelUpButton").objectReferenceValue = lvlBtn;
        soMenu.FindProperty("_buttonText").objectReferenceValue = buttonText;
        soMenu.ApplyModifiedProperties();

        // メニューの初期状態を非表示にする
        menuPanelObj.SetActive(false);

        // ==========================================
        // 5. 仮テスト用スクリプトの添付確認
        // ==========================================
        GameObject testObj = GameObject.Find("_TempStatusTest") ?? new GameObject("_TempStatusTest");
        if (testObj.GetComponent<_TempStatusTest>() == null) testObj.AddComponent<_TempStatusTest>();

        EditorUtility.SetDirty(statusPanelObj);
        EditorUtility.SetDirty(menuRootObj);
        Debug.Log("[SetupStatusUI] 左下のレベル・HP・MP表示と、TabキーメニューのUI自動生成が完了しました！");
    }
}
#endif
