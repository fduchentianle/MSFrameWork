using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MSFrame
{

/// <summary>
/// 音乐音效管理器
/// </summary>
public class AudioManager : BaseManager<AudioManager>
{
    /// <summary>
    /// 正在播放的音效信息，用于绑定播放组件和对应的资源路径。
    /// </summary>
    private class SoundInfo
    {
        public AudioSource audioSource;
        public string path;

        public SoundInfo(AudioSource audioSource, string path)
        {
            this.audioSource = audioSource;
            this.path = path;
        }
    }

    //背景音乐的路径
    private string BKName;
    //背景音乐播放组件
    private AudioSource BKMusic = null;
    //背景音乐大小
    private float BKMusicValue = 0.1f;
    //管理正在播放的音效
    private List<SoundInfo> soundList = new List<SoundInfo>();
    //音效音量大小
    private float soundValue = 0.1f;
    //音效是否在播放
    private bool soundIsPlay = true;
    //音效空物体在Resources文件夹下路径
    private readonly string soundPath = "Audio/SoundObj";
    //音频全局设置
    private AudioSetting audioSetting;

    private AudioManager()
    {
        InitAudioSetting();
        MonoManager.Instance.AddUpdateListener(Update);
    }

    /// <summary>
    /// 初始化音频设置，第一次运行时使用默认值并保存到本地
    /// </summary>
    private void InitAudioSetting()
    {
        audioSetting = SaveManager.Instance.LoadSetting<AudioSetting>();
        if (audioSetting == null)
        {
            audioSetting = new AudioSetting();
            SaveManager.Instance.SaveSetting(audioSetting);
        }

        BKMusicValue = audioSetting.bkMusicValue;
        soundValue = audioSetting.soundValue;
    }

    private void Update()
    {
        if (!soundIsPlay)
            return;
        //不停的遍历容器 检测有没有音效播放完毕 播放完了就消除损毁
        //为了避免边遍历边移除的问题 我们采用逆序遍历
        for (int i = soundList.Count-1; i >=0; i--)
        {
            SoundInfo soundInfo = soundList[i];
            AudioSource audioSource = soundInfo.audioSource;
            if (!audioSource.isPlaying)
            {
                //音效播放完毕后清空资源引用，并让资源引用计数减一
                audioSource.clip = null;
                ResManager.Instance.UnloadAsset<AudioClip>(soundInfo.path);
                PoolManager.Instance.PushGameObj(audioSource.gameObject);
                soundList.RemoveAt(i);
            }
        }
    }

    #region 背景音乐
    /// <summary>
    /// 播放背景音乐(Resources加载)
    /// </summary>
    /// <param name="name">资源名</param>
    /// <param name="isSync">是否同步加载</param>
    public void PlayBKMusic(string name, bool isSync = false)
    {
        //如果加载的背景音乐相同，则恢复暂停状态或重新播放停止的音乐
        if (name == BKName)
        {
            if (BKMusic != null && !BKMusic.isPlaying)
            {
                BKMusic.UnPause();
                //Stop后的AudioSource无法通过UnPause恢复，需要重新播放
                if (!BKMusic.isPlaying)
                    BKMusic.Play();
            }
            return;
        }

        //如果已经加载过背景音乐 且 要更换背景音乐
        //需要先卸载之前的资源
        if (name != BKName && BKName != null)
        {
            ResManager.Instance.UnloadAsset<AudioClip>(BKName, true);
            BKName = name;
        }

        //如果第一次加载背景音乐
        if (BKName == null)
        {
            GameObject obj = new GameObject("BKMusic");
            BKMusic = obj.AddComponent<AudioSource>();
            GameObject.DontDestroyOnLoad(obj);
            BKName = name;
        }

        //同步加载
        if (isSync)
        {
            AudioClip clip = ResManager.Instance.Load<AudioClip>(name);
            BKMusic.clip = clip;
            BKMusic.loop = true;
            BKMusic.volume = BKMusicValue;
            BKMusic.Play();
        }
        //异步加载
        else
        {
            ResManager.Instance.LoadAsync<AudioClip>(name, (clip) =>
            {
                BKMusic.clip = clip;
                BKMusic.loop = true;
                BKMusic.volume = BKMusicValue;
                BKMusic.Play();
            });
        }
    }

    /// <summary>
    /// 停止播放背景音乐
    /// </summary>
    public void StopBKMusic()
    {
        if (BKMusic != null)
            BKMusic.Stop();
    }

    /// <summary>
    /// 暂停播放背景音乐
    /// </summary>
    public void PauseBKMusic()
    {
        if (BKMusic != null)
            BKMusic.Pause();
    }

    /// <summary>
    /// 设置背景音乐大小
    /// </summary>
    /// <param name="volume">背景音乐大小</param>
    public void ChangeBKMusicValue(float volume)
    {
        BKMusicValue = volume;
        if (BKMusic != null)
            BKMusic.volume = BKMusicValue;

        audioSetting.bkMusicValue = BKMusicValue;
        SaveManager.Instance.SaveSetting(audioSetting);
    }
    #endregion

    #region 音效
    private AudioSource LoadSound(string path, AudioClip clip, bool isLoop = false)
    {
        SoundObj soundObj = PoolManager.Instance.GetGameObj<SoundObj>(soundPath);
        AudioSource audioSource = soundObj.gameObject.GetComponent<AudioSource>();
        //如果取出来的音效是之前正在使用的 我们先停止
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = isLoop;
        audioSource.volume = soundValue;
        audioSource.Play();
        //全局暂停期间新加载的音效也保持暂停，恢复后再继续播放
        if (!soundIsPlay)
            audioSource.Pause();
        //存储容器 用于记录 方便之后判断是否停止
        //由于从缓存池中去除对象 有可能取出一个之前正在使用的
        //所以我们需要判断容器中没有记录 再去记录
        SoundInfo soundInfo = soundList.Find(info => info.audioSource == audioSource);
        if (soundInfo == null)
            soundList.Add(new SoundInfo(audioSource, path));
        else
            soundInfo.path = path;
        return audioSource;
    }

    private void LoadSoundAsync(string path, AudioClip clip, bool isLoop = false, UnityAction<AudioSource> callBack = null)
    {
        SoundObj soundObj = PoolManager.Instance.GetGameObj<SoundObj>(soundPath);
        AudioSource audioSource = soundObj.gameObject.GetComponent<AudioSource>();
        //如果取出来的音效是之前正在使用的 我们先停止
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = isLoop;
        audioSource.volume = soundValue;
        audioSource.Play();
        //全局暂停期间异步加载完成的音效也保持暂停
        if (!soundIsPlay)
            audioSource.Pause();
        //存储容器 用于记录 方便之后判断是否停止
        //由于从缓存池中去除对象 有可能取出一个之前正在使用的
        //所以我们需要判断容器中没有记录 再去记录
        SoundInfo soundInfo = soundList.Find(info => info.audioSource == audioSource);
        if (soundInfo == null)
            soundList.Add(new SoundInfo(audioSource, path));
        else
            soundInfo.path = path;
        callBack?.Invoke(audioSource);
    }

    /// <summary>
    /// 同步加载音效
    /// </summary>
    /// <param name="name">音效路径</param>
    /// <param name="isLoop">是否重复播放</param>
    /// <returns></returns>
    public AudioSource PlaySound(string name, bool isLoop = false)
    {
        AudioClip clip = ResManager.Instance.Load<AudioClip>(name);
        AudioSource source = LoadSound(name, clip,isLoop);
        return source;
    }

    /// <summary>
    /// 异步加载音效
    /// </summary>
    /// <param name="name">音效路径</param>
    /// <param name="isLoop">是否重复播放</param>
    /// <param name="callBack">回调函数</param>
    public void PlaySoundAsync(string name, bool isLoop = false, UnityAction<AudioSource> callBack = null)
    {
        ResManager.Instance.LoadAsync<AudioClip>(name,(clip)=>
        {
            LoadSoundAsync(name, clip, isLoop, callBack);
        });
    }

    /// <summary>
    /// 停止播放音效
    /// </summary>
    /// <param name="source"></param>
    public void StopSound(AudioSource source)
    {
        for (int i = soundList.Count - 1; i >= 0; i--)
        {
            SoundInfo soundInfo = soundList[i];
            if (soundInfo.audioSource == source)
            {
                //停止播放并清空资源引用
                source.Stop();
                source.clip = null;
                ResManager.Instance.UnloadAsset<AudioClip>(soundInfo.path);
                PoolManager.Instance.PushGameObj(source.gameObject);
                soundList.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// 改变音效大小
    /// </summary>
    /// <param name="v">音效调整的大小</param>
    public void ChangeSoundValue(float v)
    {
        soundValue = v;
        for (int i = 0; i < soundList.Count; i++)
        {
            soundList[i].audioSource.volume = soundValue;
        }

        audioSetting.soundValue = soundValue;
        SaveManager.Instance.SaveSetting(audioSetting);
    }

    /// <summary>
    /// 继续播放或暂停所有音效
    /// </summary>
    /// <param name="isPlay">是否是继续播放 true为播放 false为暂停</param>
    public void PlayOrPauseSound(bool isPlay)
    {
        if (isPlay)
        {
            soundIsPlay = true;
            for (int i = 0; i < soundList.Count; i++)
            {
                soundList[i].audioSource.UnPause();
            }
        }
        else
        {
            soundIsPlay = false;
            for (int i = 0; i < soundList.Count; i++)
            {
                soundList[i].audioSource.Pause();
            }
        }
    }

    /// <summary>
    /// 清空音效相关记录 过场景时在清空缓存池之间去调用
    /// </summary>
    public void ClearSound()
    {
        for (int i = 0; i < soundList.Count; i++)
        {
            SoundInfo soundInfo = soundList[i];
            AudioSource audioSource = soundInfo.audioSource;
            audioSource.Stop();
            audioSource.clip = null;
            ResManager.Instance.UnloadAsset<AudioClip>(soundInfo.path);
            PoolManager.Instance.PushGameObj(audioSource.gameObject);
        }
        soundList.Clear();
    }
    #endregion
}
}
