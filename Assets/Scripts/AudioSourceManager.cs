using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 控制音效与背景音乐的播放与切换，在GameManager之后
public class AudioSourceManager : MonoBehaviour
{
    // 单例模式
    public static AudioSourceManager Instance { get; private set; }

    public AudioSource audioSource;

    public AudioClip[] audioClips;//特效音

    public AudioClip[] audioBackgroundClips;//背景音

    public bool muteCheckMate;
    // Start is called before the first frame update
    void Start()
    {
        muteCheckMate = false;
        Instance = this;
    }

    /// <summary>
    /// 特效音
    /// </summary>
    /// <param name="soundIndex"></param>
    public void PlaySound(int soundIndex)
    {
        if (soundIndex == 4 && muteCheckMate)
            return;
        audioSource.PlayOneShot(audioClips[soundIndex]);
    }
    /// <summary>
    /// BGM播放
    /// </summary>
    /// <param name="soundIndex"></param>
    public void PlayBGM(int soundIndex)
    {
        audioSource.Stop();
        audioSource.clip = audioBackgroundClips[soundIndex];
        audioSource.Play();
    }

    public void MuteCheckMate()
    {
        muteCheckMate = true;
    }
    public void UnMuteCheckMate()
    {
        muteCheckMate = false;
    }
}
