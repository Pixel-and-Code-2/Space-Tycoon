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
        selectable = GetComponent<Selectable>();
    }

    void Start()
    {
        OnValidate();
    }
    void OnValidate()
    {
        ActivateState(defaultState);
    }
    void OnEnable()
    {
        ActivateState(defaultState);
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
        selectable.interactable = interactable;
        ActivateState(interactable ? TriggerType.Enabled : TriggerType.Disabled);
        isInteractableCache = interactable;
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
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (checkInteractableOnPress && !isInteractableCache) return;
        ActivateState(TriggerType.PointerUp);
        isPressedCache = false;
    }
    bool isOnCache = true;
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