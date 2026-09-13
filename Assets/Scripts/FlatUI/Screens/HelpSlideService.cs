using UnityEngine;
using System.Collections.Generic;

public static class HelpSlideService
{
    public enum SlideSet
    {
        Start,
        Combat
    }

    static readonly HashSet<SlideSet> shownThisSession = new HashSet<SlideSet>();
    static bool hookedLoad;
    static bool hasPendingUnlock;
    static SlideSet pendingUnlock;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void HookSaveLoad()
    {
        if (hookedLoad) return;
        hookedLoad = true;
        if (SaveHub.Instance != null)
            SaveHub.Instance.OnLoad += _ => ResetSession();
    }

    public static void ResetSession()
    {
        shownThisSession.Clear();
        hasPendingUnlock = false;
    }

    public static bool IsUnlocked(SlideSet set)
    {
        HookSaveLoad();
        return shownThisSession.Contains(set);
    }

    public static void Unlock(SlideSet set)
    {
        shownThisSession.Add(set);
    }

    public static List<Sprite> GetSprites(SlideSet set)
    {
        var settings = HandleInittingGlobalVars.globalSettingsAssets;
        if (settings == null) return new List<Sprite>();
        if (set == SlideSet.Combat)
            return settings.helpSlidesFirstCombat != null ? new List<Sprite>(settings.helpSlidesFirstCombat) : new List<Sprite>();
        return settings.helpSlidesStart != null ? new List<Sprite>(settings.helpSlidesStart) : new List<Sprite>();
    }

    public static List<Sprite> GetUnlockedUnion()
    {
        List<Sprite> list = new List<Sprite>();
        if (IsUnlocked(SlideSet.Start))
            list.AddRange(GetSprites(SlideSet.Start));
        if (IsUnlocked(SlideSet.Combat))
            list.AddRange(GetSprites(SlideSet.Combat));
        list.RemoveAll(s => s == null);
        return list;
    }

    public static void SetPendingUnlock(SlideSet set)
    {
        hasPendingUnlock = true;
        pendingUnlock = set;
    }

    public static void ConsumePendingUnlock()
    {
        if (!hasPendingUnlock) return;
        Unlock(pendingUnlock);
        hasPendingUnlock = false;
    }

    public static bool TryShowSet(SlideSet set, bool unlock, bool asOverlay = true)
    {
        HookSaveLoad();
        List<Sprite> sprites = GetSprites(set);
        sprites.RemoveAll(s => s == null);
        if (sprites.Count == 0) return false;
        if (unlock) Unlock(set);
        else SetPendingUnlock(set);
        OpenHelp(sprites, asOverlay);
        return true;
    }

    public static bool TryShowUnlockedUnion(bool asOverlay = true)
    {
        List<Sprite> sprites = GetUnlockedUnion();
        if (sprites.Count == 0)
        {
            if (GetSprites(SlideSet.Start).Count > 0)
                return TryShowSet(SlideSet.Start, true, asOverlay);
            return false;
        }
        OpenHelp(sprites, asOverlay);
        return true;
    }

    static void OpenHelp(List<Sprite> sprites, bool asOverlay)
    {
        HelpUI help = Object.FindFirstObjectByType<HelpUI>(FindObjectsInactive.Include);
        if (help != null)
            help.PlayForcedSlides(sprites);

        if (asOverlay)
            UILayersController.Instance.ShowOverlay(UILayersController.UILayer.Help);
        else
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.Help);

        if (help != null)
            help.PlayForcedSlides(sprites);
    }
}
