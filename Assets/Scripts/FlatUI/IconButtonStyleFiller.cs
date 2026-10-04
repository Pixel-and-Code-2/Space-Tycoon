using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public enum TriggerType
{
    PointerEnter,
    PointerExit,
    PointerDown,
    PointerUp,
    Off,
    On,
    Disabled,
    Enabled
}
public class IconButtonStyleFiller : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Selectable selectable;
    public bool IsButtonOn => isOnCache;
    public bool IsButtonHighlighted => isHighlightedCache;
    public bool IsButtonPressed => isPressedCache;
    public bool IsButtonInteractable => isInteractableCache;
    [System.Serializable]
    private class State
    {
        public TriggerType triggerType;
        public List<GameObject> objToTurnOn;
        public List<GameObject> objToTurnOff;
    }
    [SerializeField]
    private List<State> states = new List<State>();
    [SerializeField]
    private TriggerType defaultState;
    [SerializeField]
    private bool checkInteractableOnHighlight = true;
    [SerializeField]
    private bool checkInteractableOnPress = true;
    [SerializeField]
    private bool defaultOnEnable = true;
    private enum OnToggleInteractableBehaviour
    {
        JustApplyToggle,
        TurnOnInteractableFirst,
        IgnoreCall
    }
    [SerializeField]
    private OnToggleInteractableBehaviour onToggleInteractableBehaviour = OnToggleInteractableBehaviour.TurnOnInteractableFirst;
    void Awake()
    {
        EnsureSelectable();
    }

    void Start()
    {
        if (defaultOnEnable)
            ApplyDefaultOrDisabledVisual();
    }
    void OnValidate()
    {
        ActivateState(defaultState);
    }
    void OnEnable()
    {
        if (defaultOnEnable)
            ApplyDefaultOrDisabledVisual();
    }

    void EnsureSelectable()
    {
        if (selectable == null)
            selectable = GetComponent<Selectable>();
    }

    void ApplyDefaultOrDisabledVisual()
    {
        EnsureSelectable();
        isOnCache = defaultState == TriggerType.On;
        if (selectable != null && !selectable.interactable)
        {
            ActivateState(TriggerType.Disabled);
            isInteractableCache = false;
            return;
        }
        ActivateState(defaultState);
    }
    void OnDisable()
    {
        ActivateState(TriggerType.Off);
    }
    private bool CheckToggle()
    {
        switch (onToggleInteractableBehaviour)
        {
            case OnToggleInteractableBehaviour.JustApplyToggle:
                return true;
            case OnToggleInteractableBehaviour.TurnOnInteractableFirst:
                SetInteractable(true);
                return true;
            case OnToggleInteractableBehaviour.IgnoreCall:
                return false;
        }
        return false;
    }
    public void TurnOnButton()
    {
        if (!CheckToggle()) return;
        ActivateState(TriggerType.On);
        isOnCache = true;
    }
    public void SetInteractable(bool interactable)
    {
        EnsureSelectable();
        if (selectable != null)
            selectable.interactable = interactable;
        isInteractableCache = interactable;
        if (!interactable)
        {
            SyncOnCacheFromVisual();
            SetLayerActive("highlighter", false);
            SetLayerActive("bgActive", false);
            SetLayerActive("mgActive", false);
            SetLayerActive("fgActive", false);
            SetLayerActive("bgPressed", false);
            SetLayerActive("mgPressed", false);
            SetLayerActive("fgPressed", false);
            SetLayerActive("bg", false);
            SetLayerActive("mg", false);
            SetLayerActive("fg", false);
            EnsureDisabledLayerSprites();
            ActivateState(TriggerType.Disabled);
            SetLayerActive("bgDisabled", true);
            SetLayerActive("mgDisabled", true);
            SetLayerActive("fgDisabled", true);
            return;
        }
        ActivateState(TriggerType.Enabled);
        SetLayerActive("bgDisabled", false);
        SetLayerActive("mgDisabled", false);
        SetLayerActive("fgDisabled", false);
        if (isOnCache)
            ActivateState(TriggerType.On);
        else
        {
            ActivateState(TriggerType.Off);
            SetLayerActive("bg", true);
            SetLayerActive("mg", true);
            SetLayerActive("fg", true);
        }
    }

    void SyncOnCacheFromVisual()
    {
        Transform mgActive = transform.Find("mgActive");
        if (mgActive != null && mgActive.gameObject.activeSelf)
        {
            isOnCache = true;
            return;
        }
        Transform mg = transform.Find("mg");
        if (mg != null && mg.gameObject.activeSelf)
        {
            isOnCache = false;
            return;
        }
        isOnCache = defaultState == TriggerType.On;
    }

    void SetLayerActive(string childName, bool active)
    {
        Transform t = transform.Find(childName);
        if (t != null)
            t.gameObject.SetActive(active);
    }

    void EnsureDisabledLayerSprites()
    {
        CopyVisualToDisabled("bg", "bgDisabled");
        CopyVisualToDisabled("mg", "mgDisabled");
        CopyVisualToDisabled("fg", "fgDisabled");
    }

    void CopyVisualToDisabled(string fromName, string toName)
    {
        Transform from = transform.Find(fromName);
        Transform to = transform.Find(toName);
        if (from == null || to == null) return;
        Image src = from.GetComponent<Image>();
        Image dst = to.GetComponent<Image>();
        if (src == null || dst == null) return;
        if (dst.sprite == null && src.sprite != null)
            dst.sprite = src.sprite;
        if (src.sprite != null)
        {
            Color c = src.color;
            dst.color = new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, c.a * 0.55f);
        }
    }
    public void TurnOffButton()
    {
        if (!CheckToggle()) return;
        ActivateState(TriggerType.Off);
        isOnCache = false;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (checkInteractableOnHighlight && !isInteractableCache) return;
        ActivateState(TriggerType.PointerEnter);
        isHighlightedCache = true;
        AudioController.Instance.Play(AudioController.Instance.buttonHover);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (checkInteractableOnHighlight && !isInteractableCache) return;
        ActivateState(TriggerType.PointerExit);
        isHighlightedCache = false;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (checkInteractableOnPress && !isInteractableCache) return;
        ActivateState(TriggerType.PointerDown);
        isPressedCache = true;
        AudioController.Instance.Play(AudioController.Instance.buttonClick);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (checkInteractableOnPress && !isInteractableCache) return;
        ActivateState(TriggerType.PointerUp);
        isPressedCache = false;
    }
    bool isOnCache = false;
    bool isHighlightedCache = false;
    bool isInteractableCache = true;
    bool isPressedCache = false;
    private void ActivateState(TriggerType triggerType)
    {
        foreach (State state in states)
        {
            if (state.triggerType == triggerType)
            {
                ActivateState(state);
                return;
            }
        }
        // Debug.LogWarning($"State {triggerType} not found in {name}");
    }
    private void ActivateState(State state)
    {
        foreach (GameObject obj in state.objToTurnOn)
        {
            obj.SetActive(true);
        }
        foreach (GameObject obj in state.objToTurnOff)
        {
            obj.SetActive(false);
        }
    }
}