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
        switch (config)
        {
            case "start":
                videoPlayer.clip = begginingVideo;
                break;
            case "win":
                videoPlayer.clip = endVideoWin;
                break;
            case "lose":
                videoPlayer.clip = endVideoLose;
                break;
        }
        videoPlayer.Play();
        videoPlayer.loopPointReached -= OnVideoEnd;
        videoPlayer.loopPointReached += OnVideoEnd;
        timeOnSlide = 0f;
        AudioController.Instance.Stop(true, true);
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
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.AttentionText, "Победа_persistent_1_GameCongratulationsColor");
            AudioController.Instance.Play(AudioController.Instance.victoryAmbient, true);
        }
        if (configCache == "lose")
        {
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.AttentionText, "Поражение_persistent_2_GameAttentionColor");
            AudioController.Instance.Play(AudioController.Instance.defeatAmbient, true);
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