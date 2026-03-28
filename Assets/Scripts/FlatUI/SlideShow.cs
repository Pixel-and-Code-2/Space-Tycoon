using UnityEngine;
using System.Collections.Generic;
using TMPro;

public enum SlideShowType
{
    Start,
    Win,
    Lose
}

public class SlideShow : IUILayer
{
    public static Dictionary<SlideShowType, string> slidesDictionary = new Dictionary<SlideShowType, string>(){
        {SlideShowType.Start, "start"},
        { SlideShowType.Win, "win"},
        { SlideShowType.Lose, "lose"}
    };

    [System.Serializable]
    private class SlidesData
    {
        public List<GameObject> slides;
        public SlideShowType slidesType;
    }
    [SerializeField]
    private List<SlidesData> slidesData;
    private int currentSlidesIndex = 0;
    private SlideShowType currentSlidesType;
    [SerializeField]
    private TextMeshProUGUI revealingText;
    [SerializeField, Range(0f, 20f)]
    private float timeBeforeRevealingText = 3f;
    [SerializeField]
    private AnimationCurve revealCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f));
    [SerializeField, Range(0f, 0.5f)]
    private float textAnimationStep = 0.04f;
    void OnDisable()
    {
        foreach (var slide in slidesData)
        {
            foreach (var slideObject in slide.slides)
            {
                slideObject.SetActive(false);
            }
        }
        currentSlidesIndex = 0;
    }

    public override void Initialize(string config)
    {
        currentSlidesType = SlideShowType.Start;
        switch (config)
        {
            case "start":
                currentSlidesType = SlideShowType.Start;
                currentSlidesIndex = 0;
                break;
            case "win":
                currentSlidesType = SlideShowType.Win;
                currentSlidesIndex = 0;
                break;
            case "lose":
                currentSlidesType = SlideShowType.Lose;
                currentSlidesIndex = 0;
                break;
        }
        timeOnSlide = 0f;
        UpdateSlides();
    }
    public void OnClickNext()
    {
        currentSlidesIndex++;
        timeOnSlide = 0f;
        var currentSlides = slidesData.Find(slides => slides.slidesType == currentSlidesType);
        if (currentSlidesIndex >= currentSlides.slides.Count)
        {
            currentSlidesIndex = 0;
            switch (currentSlidesType)
            {
                case SlideShowType.Start:
                    UILayersController.Instance.SetLayer(UILayersController.UILayer.GameUI);
                    break;
                case SlideShowType.Win:
                    UILayersController.Instance.ShowOverlay(UILayersController.UILayer.AttentionText, "Миссия выполнена!_persistent");
                    break;
                case SlideShowType.Lose:
                    UILayersController.Instance.ShowOverlay(UILayersController.UILayer.AttentionText, "Миссия провалена!Все члены экипажа погибли..._persistent");
                    break;
            }
            return;
        }
        UpdateSlides();
    }
    private void UpdateSlides()
    {
        var currentSlides = slidesData.Find(slides => slides.slidesType == currentSlidesType);
        for (int i = 0; i < currentSlides.slides.Count; i++)
        {
            currentSlides.slides[i].SetActive(i == currentSlidesIndex);
        }
    }
    private float timeOnSlide = 0f;
    private float revealProgress = 0f;
    private void Update()
    {
        timeOnSlide += Time.unscaledDeltaTime;
        if (timeOnSlide >= timeBeforeRevealingText)
        {
            revealingText.gameObject.SetActive(true);
            revealProgress += textAnimationStep;
            if (revealProgress >= 1f)
            {
                revealProgress = 0f;
            }
            revealingText.color = new Color(
                revealingText.color.r,
                revealingText.color.g,
                revealingText.color.b,
                revealCurve.Evaluate(revealProgress)
            );
        }
        else if (revealingText.color.a > 0.01f)
        {
            revealingText.color = new Color(
                revealingText.color.r,
                revealingText.color.g,
                revealingText.color.b,
                0f
            );
        }
    }
}