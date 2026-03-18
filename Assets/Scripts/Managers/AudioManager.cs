using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGMとSEを管理するシングルトンクラス
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("AudioManagerがシーンに存在しません。");
            }
            return _instance;
        }
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _seSource;

    // 音声データをInspectorから設定するための構造体
    [System.Serializable]
    public struct SoundData
    {
        public string name;
        public AudioClip clip;
    }

    [Header("Audio Clips")]
    [SerializeField] private SoundData[] _bgmDataList;
    [SerializeField] private SoundData[] _seDataList;

    // 名前からAudioClipを検索するための辞書
    private Dictionary<string, AudioClip> _bgmDictionary;
    private Dictionary<string, AudioClip> _seDictionary;

    private void Awake()
    {
        // シングルトンの初期化
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 辞書の初期化
        _bgmDictionary = new Dictionary<string, AudioClip>();
        _seDictionary = new Dictionary<string, AudioClip>();

        // Inspectorで設定したリストを辞書に登録
        foreach (var data in _bgmDataList)
        {
            if (!_bgmDictionary.ContainsKey(data.name))
            {
                _bgmDictionary.Add(data.name, data.clip);
            }
        }

        foreach (var data in _seDataList)
        {
            if (!_seDictionary.ContainsKey(data.name))
            {
                _seDictionary.Add(data.name, data.clip);
            }
        }

        PlayBgm("tmpBGM");
    }

    /// <summary>
    /// BGMを再生する
    /// </summary>
    /// <param name="bgmName">再生したいBGMの名前</param>
    /// <param name="isLoop">ループ再生するかどうか</param>
    public void PlayBgm(string bgmName, bool isLoop = true)
    {
        if (_bgmDictionary.TryGetValue(bgmName, out AudioClip clip))
        {
            _bgmSource.clip = clip;
            _bgmSource.loop = isLoop;
            _bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"BGMが見つかりません: {bgmName}");
        }
    }

    /// <summary>
    /// BGMを停止する
    /// </summary>
    public void StopBgm()
    {
        _bgmSource.Stop();
    }

    /// <summary>
    /// SEを再生する
    /// </summary>
    /// <param name="seName">再生したいSEの名前</param>
    public void PlaySe(string seName)
    {
        if (_seDictionary.TryGetValue(seName, out AudioClip clip))
        {
            _seSource.PlayOneShot(clip); // 重ね掛け対応のためPlayOneShotを使用
        }
        else
        {
            Debug.LogWarning($"SEが見つかりません: {seName}");
        }
    }
}
