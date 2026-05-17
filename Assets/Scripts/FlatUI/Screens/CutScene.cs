using UnityEngine;
using UnityEngine.Video;

public class CutScene : IUILayer
{
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
    void OnEnable()
    {
        revealingObj.gameObject.SetActive(false);
    }
    void OnDisable()
    {
        videoPlayer.clip = null;
    }

    private string configCache = "start";
    public override void Initialize(string config)
    {
        videoPlayer.clip = begginingVideo;
        configCache = config;
        AudioController.Instance.Stop(true, true);
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
        videoPlayer.Play();
        videoPlayer.loopPointReached -= OnVideoEnd;
        videoPlayer.loopPointReached += OnVideoEnd;
        timeOnSlide = 0f;
    }
    private void OnVideoEnd(VideoPlayer videoPlayer)
    {
        videoPlayer.loopPointReached -= OnVideoEnd;
        OnClickNext();
    }
    public void OnClickNext()
    {
        timeOnSlide = 0f;
        if (configCache == "start")
        {
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.Help);
            AudioController.Instance.Play(AudioController.Instance.gameAmbient, true);
        }
        if (configCache == "win")
        {
            if (PlayerPrefs.GetInt("IsFirstWin", 1) == 1)
            {
                PlayerPrefs.SetInt("IsFirstWin", 0);
                UILayersController.Instance.SetLayer(UILayersController.UILayer.CutScene, "titles");
                return;
            }
            else
            {
                UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.AttentionText, "Победа_persistent_1_GameCongratulationsColor");
                AudioController.Instance.Play(AudioController.Instance.victoryAmbient, true);
            }
        }
        if (configCache == "lose")
        {
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.AttentionText, "Поражение_persistent_2_GameAttentionColor");
            AudioController.Instance.Play(AudioController.Instance.defeatAmbient, true);
        }
        if (configCache == "titles")
        {
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.AttentionText, "Победа_persistent_1_GameCongratulationsColor");
            AudioController.Instance.Play(AudioController.Instance.victoryAmbient, true);
        }
        if (configCache == "titles_menu")
        {
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.MainMenu);
        }
    }
    private float timeOnSlide = 0f;
    private void Update()
    {
        timeOnSlide += Time.unscaledDeltaTime;
        if (timeOnSlide >= timeBeforeRevealingObj)
        {
            revealingObj.gameObject.SetActive(true);
        }
    }
}