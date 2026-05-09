using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using System;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }
    [SerializeField]
    private AudioSource musicSource;
    [SerializeField]
    private AudioSource soundSource;
    [SerializeField, Range(0.0001f, 10f)]
    private float fadeDuration = 0.7f;
    [SerializeField]
    private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField]
    public AudioClip gameAmbient;
    [SerializeField]
    public AudioClip combatAmbient;
    [SerializeField]
    public AudioClip victoryAmbient;
    [SerializeField]
    public AudioClip defeatAmbient;
    [SerializeField]
    public AudioClip mainMenuAmbient;
    [SerializeField]
    private AudioMixer audioMixer;
    [SerializeField]
    public AudioClip buttonHover;
    [SerializeField]
    public AudioClip buttonClick;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogError("AudioController instance already exists");
    }
    void Start()
    {
        musicSource.loop = true;
        soundSource.loop = false;
        TurnManager.Instance.OnTriggerZoneExit += OnTriggerZoneExit;
        TurnManager.Instance.OnTriggerZoneEnter += OnTriggerZoneEnter;
        SaveHub.Instance.OnLoad += OnLoad;
        SaveHub.Instance.OnSave += OnSave;
        audioMixer.SetFloat("musicVol", Mathf.Log10(PlayerPrefs.GetFloat("MusicVolume", 1f)) * 20f);
        audioMixer.SetFloat("sfxVol", Mathf.Log10(PlayerPrefs.GetFloat("SoundVolume", 1f)) * 20f);
    }
    private void OnSave(Action<SaveRecord[], string> addSaveData)
    {
        addSaveData(new SaveRecord[] {
            new SaveRecord() {
                recordName = "CurrentMusicClip",
                recordType = SaveRecordType.stringValue,
                stringValue = musicSource.clip != null ? musicSource.clip.name : ""
            }
        }, "AudioController");
    }
    private void OnLoad(LoadedData data)
    {
        string currentMusicClip = data.GetData("CurrentMusicClip", "AudioController", "");
        if (currentMusicClip != "")
        {
            if (currentMusicClip == gameAmbient.name)
            {
                Play(gameAmbient, true);
            }
            else if (currentMusicClip == combatAmbient.name)
            {
                Play(combatAmbient, true);
            }
            else if (currentMusicClip == victoryAmbient.name)
            {
                Play(victoryAmbient, true);
            }
            else if (currentMusicClip == defeatAmbient.name)
            {
                Play(defeatAmbient, true);
            }
            else if (currentMusicClip == mainMenuAmbient.name)
            {
                Play(mainMenuAmbient, true);
            }
        }
    }
    void OnDestroy()
    {
        TurnManager.Instance.OnTriggerZoneExit -= OnTriggerZoneExit;
        TurnManager.Instance.OnTriggerZoneEnter -= OnTriggerZoneEnter;
        SaveHub.Instance.OnLoad -= OnLoad;
        SaveHub.Instance.OnSave -= OnSave;
    }
    private void OnTriggerZoneExit()
    {
        Play(gameAmbient, true);
    }
    private void OnTriggerZoneEnter()
    {
        Play(combatAmbient, true);
    }
    public void Play(AudioClip clip, bool isMusic = false)
    {
        // Debug.Log("Playing " + clip.name + " as " + (isMusic ? "music" : "sound"));
        if (isMusic)
        {
            StartCoroutine(ChangeMusic(clip));
        }
        else
        {
            if (clip != null)
            {
                soundSource.Stop();
                soundSource.clip = clip;
                soundSource.Play();
            }
        }
    }
    private float timeSpent = 0f;
    private bool isChanging = false;
    private int pendingId = 0;
    private IEnumerator ChangeMusic(AudioClip music)
    {
        int id = ++pendingId;
        while (isChanging && id == pendingId)
        {
            yield return null;
        }
        if (id == pendingId)
        {
            isChanging = true;
            if (musicSource.isPlaying)
            {
                timeSpent = (1f - musicSource.volume) * fadeDuration;
                while (timeSpent < fadeDuration)
                {
                    timeSpent += Time.unscaledDeltaTime;
                    musicSource.volume = Mathf.Lerp(1f, 0f, fadeCurve.Evaluate(timeSpent / fadeDuration));
                    yield return null;
                }
                musicSource.Stop();
            }
            if (music != null)
            {
                timeSpent = 0f;
                musicSource.clip = music;
                musicSource.Play();
                while (timeSpent < fadeDuration)
                {
                    timeSpent += Time.unscaledDeltaTime;
                    musicSource.volume = Mathf.Lerp(0f, 1f, fadeCurve.Evaluate(timeSpent / fadeDuration));
                    yield return null;
                }
            }
            isChanging = false;
        }
    }
    public void Stop(bool stopMusic = true, bool stopSound = true)
    {
        if (stopMusic) StartCoroutine(ChangeMusic(null));
        if (stopSound) soundSource.Stop();
    }
    public void SetVolume(float volume, bool isMusic)
    {
        audioMixer.SetFloat(isMusic ? "musicVol" : "sfxVol", Mathf.Log10(volume) * 20f);
    }
}
