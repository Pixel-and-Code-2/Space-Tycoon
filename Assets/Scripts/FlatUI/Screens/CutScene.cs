using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class CutScene : IUILayer
{
    [SerializeField]
    bool skipVideoInEditor = true;
    [SerializeField]
    bool skipVideoInPlayer = true;

    [SerializeField]
    private VideoPlayer videoPlayer;
    [SerializeField]
    private VideoClip begginingVideo;
    [SerializeField]
    private VideoClip endVideoWin;
    [SerializeField]
    private VideoClip endVideoLose;
    [SerializeField]
    private VideoClip titlesVideo;
    [SerializeField]
    private AudioClip titlesAudioOverride;
    [SerializeField, Range(0f, 300f)]
    private float titlesAudioOffset = 0f;

    [SerializeField]
    private GameObject revealingObj;
    [SerializeField, Range(0f, 20f)]
    private float timeBeforeRevealingObj = 3f;

    public GameObject RevealingObj => revealingObj;

    bool ShouldSkipVideo => Application.isEditor ? skipVideoInEditor : skipVideoInPlayer;

    bool advancing;
    bool teardownQueued;
    Coroutine teardownRoutine;

    void OnEnable()
    {
        advancing = false;
        teardownQueued = false;
        if (revealingObj != null)
            revealingObj.SetActive(false);
    }

    void OnDisable()
    {
        advancing = false;
        StopVideoSafe();
    }

    void UnhookVideoEvents()
    {
        if (videoPlayer == null) return;
        videoPlayer.loopPointReached -= OnVideoEnd;
        videoPlayer.prepareCompleted -= OnPreparedPlay;
        videoPlayer.errorReceived -= OnVideoError;
    }

    void StopVideoSafe()
    {
        if (videoPlayer == null) return;
        UnhookVideoEvents();
        try { videoPlayer.Stop(); }
        catch (System.Exception e) { Debug.LogWarning("[CutScene] Stop: " + e.Message); }
        QueueTeardown();
    }

    void QueueTeardown()
    {
        if (videoPlayer == null || teardownQueued) return;
        teardownQueued = true;
        if (teardownRoutine != null)
            StopCoroutine(teardownRoutine);
        if (isActiveAndEnabled)
            teardownRoutine = StartCoroutine(TeardownVideoDeferred());
        else
            TeardownVideoImmediate();
    }

    IEnumerator TeardownVideoDeferred()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        TeardownVideoImmediate();
        teardownRoutine = null;
    }

    void TeardownVideoImmediate()
    {
        teardownQueued = false;
        if (videoPlayer == null) return;
        UnhookVideoEvents();
        try
        {
            if (videoPlayer.targetTexture != null)
                videoPlayer.targetTexture = null;
            videoPlayer.clip = null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CutScene] teardown: " + e.Message);
        }
        videoPlayer.enabled = false;
    }

    private string configCache = "start";
    public override void Initialize(string config)
    {
        configCache = config;
        advancing = false;
        AudioController.Instance.Stop(true, true);
        if (ShouldSkipVideo)
        {
            StopVideoSafe();
            if (configCache == "titles" || configCache == "titles_menu")
            {
                if (titlesAudioOverride != null)
                    AudioController.Instance.Play(titlesAudioOverride, true, titlesAudioOffset);
            }
            OnClickNext();
            return;
        }
        if (videoPlayer == null)
        {
            OnClickNext();
            return;
        }
        UnhookVideoEvents();
        if (teardownRoutine != null)
        {
            StopCoroutine(teardownRoutine);
            teardownRoutine = null;
        }
        teardownQueued = false;
        videoPlayer.enabled = true;
        videoPlayer.playOnAwake = false;
        videoPlayer.clip = begginingVideo;
        switch (config)
        {
            case "start":
                videoPlayer.clip = begginingVideo;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                break;
            case "win":
                videoPlayer.clip = endVideoWin;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                break;
            case "lose":
                videoPlayer.clip = endVideoLose;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                break;
            case "titles":
            case "titles_menu":
                videoPlayer.clip = titlesVideo;
                if (titlesAudioOverride != null)
                {
                    AudioController.Instance.Play(titlesAudioOverride, true, titlesAudioOffset);
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                }
                else
                {
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                }
                break;
        }
        if (videoPlayer.clip == null)
        {
            StopVideoSafe();
            OnClickNext();
            return;
        }
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.prepareCompleted += OnPreparedPlay;
        videoPlayer.Prepare();
        timeOnSlide = 0f;
    }

    void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("[CutScene] VideoPlayer error: " + message);
        UnhookVideoEvents();
        StopVideoSafe();
        OnClickNext();
    }

    void OnPreparedPlay(VideoPlayer source)
    {
        source.prepareCompleted -= OnPreparedPlay;
        if (advancing || source == null || !source.enabled)
            return;
        source.loopPointReached -= OnVideoEnd;
        source.loopPointReached += OnVideoEnd;
        source.Play();
    }

    private void OnVideoEnd(VideoPlayer source)
    {
        if (source != null)
            source.loopPointReached -= OnVideoEnd;
        StopVideoSafe();
        OnClickNext();
    }

    public void OnClickNext()
    {
        if (advancing) return;
        advancing = true;
        timeOnSlide = 0f;
        StopVideoSafe();
        if (configCache == "start")
        {
            if (!HelpSlideService.TryShowSet(HelpSlideService.SlideSet.Start, true, asOverlay: false))
                UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.Help);
            AudioController.Instance.Play(AudioController.Instance.gameAmbient, true);
            return;
        }
        if (configCache == "win")
        {
            if (PlayerPrefs.GetInt("IsFirstWin", 1) == 1)
            {
                PlayerPrefs.SetInt("IsFirstWin", 0);
                UILayersController.Instance.SetLayer(UILayersController.UILayer.CutScene, "titles");
                return;
            }
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.AttentionText, "Победа_persistent_1_GameCongratulationsColor");
            AudioController.Instance.Play(AudioController.Instance.victoryAmbient, true);
            return;
        }
        if (configCache == "lose")
        {
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.AttentionText, "Поражение_persistent_2_GameAttentionColor");
            AudioController.Instance.Play(AudioController.Instance.defeatAmbient, true);
            return;
        }
        if (configCache == "titles")
        {
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.AttentionText, "Победа_persistent_1_GameCongratulationsColor");
            AudioController.Instance.Play(AudioController.Instance.victoryAmbient, true);
            return;
        }
        if (configCache == "titles_menu")
        {
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.MainMenu);
        }
    }

    private float timeOnSlide = 0f;
    private void Update()
    {
        if (ShouldSkipVideo) return;
        timeOnSlide += Time.unscaledDeltaTime;
        if (timeOnSlide >= timeBeforeRevealingObj && revealingObj != null)
            revealingObj.SetActive(true);
    }
}
