using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// テスト用カードアセットを一括生成するエディタースクリプト
/// メニュー Tools → Create Test Card Assets から実行できます
/// </summary>
public static class TestCardAssetCreator
{
    private const string OutputPath = "Assets/Data/Cards";

    [MenuItem("Tools/テスト用カードアセットを生成")]
    public static void CreateTestCards()
    {
        // 出力先フォルダの作成
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(OutputPath))
            AssetDatabase.CreateFolder("Assets/Data", "Cards");

        // ===========================
        // お金カード（MoneyCard）
        // ===========================
        CreateMoneyCard("MoneyCard_100",  "お金カード（＋100）",  100);
        CreateMoneyCard("MoneyCard_200",  "お金カード（＋200）",  200);
        CreateMoneyCard("MoneyCard_500",  "お金カード（＋500）",  500);

        // ===========================
        // 移動カード（MoveCard）
        // ===========================
        CreateMoveCard("MoveCard_Right1", "移動カード（右 1歩）", DirectionType.Right, 1);
        CreateMoveCard("MoveCard_Right2", "移動カード（右 2歩）", DirectionType.Right, 2);
        CreateMoveCard("MoveCard_Right3", "移動カード（右 3歩）", DirectionType.Right, 3);
        CreateMoveCard("MoveCard_Up1",    "移動カード（上 1歩）", DirectionType.Up,    1);
        CreateMoveCard("MoveCard_Down1",  "移動カード（下 1歩）", DirectionType.Down,  1);
        CreateMoveCard("MoveCard_Left1",  "移動カード（左 1歩）", DirectionType.Left,  1);

        // ===========================
        // アイテムカード（ItemCard）
        // ===========================
        CreateItemCard("ItemCard_AddMove2", "アイテムカード（移動量＋2）", ItemEffectType.AddMoveStep, 2);
        CreateItemCard("ItemCard_Redraw",   "アイテムカード（引き直し）",   ItemEffectType.Redraw,       0);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TestCardAssetCreator] テスト用カードアセットの生成が完了しました。場所: {OutputPath}");
        EditorUtility.DisplayDialog(
            "生成完了",
            $"テスト用カードアセットを {OutputPath} に生成しました！\n" +
            "PackManager の AllAvailableCards リストに追加してください。",
            "OK"
        );
    }

    private static void CreateMoneyCard(string fileName, string cardName, int amount)
    {
        string path = $"{OutputPath}/{fileName}.asset";
        if (File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), path)))
        {
            Debug.Log($"[TestCardAssetCreator] すでに存在するためスキップ: {path}");
            return;
        }

        var asset = ScriptableObject.CreateInstance<MoneyCardData>();
        asset.CardName = cardName;
        asset.Amount = amount;
        asset.Description = $"所持金が {amount} 増加する。";
        AssetDatabase.CreateAsset(asset, path);
    }

    private static void CreateMoveCard(string fileName, string cardName, DirectionType direction, int steps)
    {
        string path = $"{OutputPath}/{fileName}.asset";
        if (File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), path)))
        {
            Debug.Log($"[TestCardAssetCreator] すでに存在するためスキップ: {path}");
            return;
        }

        var asset = ScriptableObject.CreateInstance<MoveCardData>();
        asset.CardName = cardName;
        asset.Direction = direction;
        asset.Steps = steps;
        asset.Description = $"{direction} 方向に {steps} マス移動する。";
        AssetDatabase.CreateAsset(asset, path);
    }

    private static void CreateItemCard(string fileName, string cardName, ItemEffectType effectType, int effectValue)
    {
        string path = $"{OutputPath}/{fileName}.asset";
        if (File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), path)))
        {
            Debug.Log($"[TestCardAssetCreator] すでに存在するためスキップ: {path}");
            return;
        }

        var asset = ScriptableObject.CreateInstance<ItemCardData>();
        asset.CardName = cardName;
        asset.EffectType = effectType;
        asset.EffectValue = effectValue;
        asset.Description = $"アイテム効果「{effectType}」を発動する。";
        AssetDatabase.CreateAsset(asset, path);
    }
}
