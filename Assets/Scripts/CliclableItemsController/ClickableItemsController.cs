using UnityEngine;
using System.Collections.Generic;

public class ClickableItemsController : MonoBehaviour
{
    [System.Serializable]
    public class TaskItem
    {
        public enum TaskItemStatus
        {
            Unavailable = 0,
            ReadyToStart = 1,
            InProgress = 2,
            Done = 3,
        }
        public enum TextShowTime
        {
            BeforeContextMenu,
            BeforeStart,
            AfterComplete
        }
        [System.Serializable]
        public class TextToShow
        {
            [System.Serializable]
            public class TimedSfx
            {
                public AudioClip clip;
                public float atSeconds;
            }
            [System.Serializable]
            public class CompleteSound
            {
                public AudioClip clip;
                public int authorIndex;
                public List<TimedSfx> timedSfx = new List<TimedSfx>();
            }
            public TextShowTime showTime;
            public string text;
            [HideInInspector]
            public bool shown = false;
            public bool showOnce = true;
            public bool showOnlyOnStepByStep = false;
            public int author = -1;
            public List<CompleteSound> completeSounds = new List<CompleteSound>();
        }
        [System.Serializable]
        public class TaskCondition
        {
            public int itemIndex = -1;
            public TaskItemStatus requiredStatus = TaskItemStatus.Done;
            public bool isMain = true;
            public bool atLeast;
        }
        public ISelectable selectable;
        public TaskItemStatus status = TaskItemStatus.Unavailable;
        public List<TaskCondition> readyWhen = new List<TaskCondition>();
        public List<TaskCondition> doneWhen = new List<TaskCondition>();
        public List<TextToShow> textToShow = new List<TextToShow>();
        public string shortLevelName = string.Empty;
        public string completeText = string.Empty;
        public Color completeTextColor = Color.yellow;
        public bool onlyShowText = false;
        [Tooltip("Show ? marker, but one click runs StartWork as if menu button was pressed")]
        public bool startImmediately = false;
    }
    public static ClickableItemsController Instance { get; private set; }
    public System.Action OnTaskUpdated;
    public ISelectable currentSelectedItem { get; private set; }
    [SerializeField]
    public List<TaskItem> mainTaskScenario;
    [SerializeField]
    public List<TaskItem> sideTaskScenario;
    private int currentTaskScenarioIndex = 0;
    private const string UNIQUE_ID = "ClickableItemsController";
    [SerializeField]
    private GameObject[] authorsObjects;

    struct NarrativeVoiceEntry
    {
        public int authorIndex;
        public AudioClip clip;
        public string textPreview;
        public List<TaskItem.TextToShow.TimedSfx> timedSfx;
    }

    readonly Queue<NarrativeVoiceEntry> narrativeVoiceQueue = new Queue<NarrativeVoiceEntry>();
    int lastQuestionAuthorIndex = -1;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogError("ClickableItemsController instance already exists");
    }

    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
            TurnManager.Instance.OnPlayerTurnEnd += OnPlayerTurnEnd;
        }
        SaveHub.Instance.OnLoad += OnLoadData;
        SaveHub.Instance.OnSave += OnSaveData;
        UILayersController.Instance.OnGameResumed += OnGameResumed;
    }

    void OnEnable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
            TurnManager.Instance.OnPlayerTurnEnd += OnPlayerTurnEnd;
        }
    }
    void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
        }
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
        }
        if (UILayersController.Instance != null)
            UILayersController.Instance.OnGameResumed -= OnGameResumed;
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoadData;
            SaveHub.Instance.OnSave -= OnSaveData;
        }
    }
    void Update()
    {
        bool updated = false;
        updated |= TryEnterInProgressFromWork(mainTaskScenario);
        updated |= TryEnterInProgressFromWork(sideTaskScenario);
        if (updated) OnTaskUpdated?.Invoke();
    }

    private bool TryEnterInProgressFromWork(List<TaskItem> scenario)
    {
        bool updated = false;
        foreach (TaskItem item in scenario)
        {
            if (item.selectable.IsWorking()
                && item.status == TaskItem.TaskItemStatus.ReadyToStart)
            {
                item.status = TaskItem.TaskItemStatus.InProgress;
                item.selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.InProgress);
                UnregisterSelectable(item.selectable);
                CheckActionBox();
                updated = true;
            }
        }
        return updated;
    }
    private void OnSaveData(System.Action<SaveRecord[], string> addSaveData)
    {
        List<SaveRecord> records = new List<SaveRecord>();
        for (int i = 0; i < mainTaskScenario.Count; i++)
        {
            records.Add(new SaveRecord()
            {
                recordName = "MainTaskScenario_" + i,
                recordType = SaveRecordType.integerNumber,
                intValue = (int)mainTaskScenario[i].status
            });
            for (int j = 0; j < mainTaskScenario[i].textToShow.Count; j++)
            {
                records.Add(new SaveRecord()
                {
                    recordName = "MainTaskScenarioTextShown_" + i + "_" + j,
                    recordType = SaveRecordType.boolean,
                    boolValue = mainTaskScenario[i].textToShow[j].shown
                });
            }
        }


        for (int i = 0; i < sideTaskScenario.Count; i++)
        {
            records.Add(new SaveRecord()
            {
                recordName = "SideTaskScenario_" + i,
                recordType = SaveRecordType.integerNumber,
                intValue = (int)sideTaskScenario[i].status
            });
            for (int j = 0; j < sideTaskScenario[i].textToShow.Count; j++)
            {
                records.Add(new SaveRecord()
                {
                    recordName = "SideTaskScenarioTextShown_" + i + "_" + j,
                    recordType = SaveRecordType.boolean,
                    boolValue = sideTaskScenario[i].textToShow[j].shown
                });
            }
        }
        addSaveData(records.ToArray(), UNIQUE_ID);
    }
    private void OnLoadData(LoadedData data)
    {
        for (int i = 0; i < mainTaskScenario.Count; i++)
        {
            mainTaskScenario[i].status = (TaskItem.TaskItemStatus)data.GetData("MainTaskScenario_" + i, UNIQUE_ID, (int)TaskItem.TaskItemStatus.Unavailable);
            for (int j = 0; j < mainTaskScenario[i].textToShow.Count; j++)
            {
                mainTaskScenario[i].textToShow[j].shown = data.GetData("MainTaskScenarioTextShown_" + i + "_" + j, UNIQUE_ID, false);
            }
        }
        for (int i = 0; i < sideTaskScenario.Count; i++)
        {
            sideTaskScenario[i].status = (TaskItem.TaskItemStatus)data.GetData("SideTaskScenario_" + i, UNIQUE_ID, (int)TaskItem.TaskItemStatus.Unavailable);
            for (int j = 0; j < sideTaskScenario[i].textToShow.Count; j++)
            {
                sideTaskScenario[i].textToShow[j].shown = data.GetData("SideTaskScenarioTextShown_" + i + "_" + j, UNIQUE_ID, false);
            }
        }
        CheckActionBox();
        OnTaskUpdated?.Invoke();
    }
    private bool CheckActionBoxInternal(List<TaskItem> scenario, string label)
    {
        bool updated = false;
        for (int i = 0; i < scenario.Count; i++)
        {
            if (scenario[i].status == TaskItem.TaskItemStatus.Unavailable)
            {
                if (CheckReadyWhen(scenario[i]))
                {
                    scenario[i].status = TaskItem.TaskItemStatus.ReadyToStart;
                    scenario[i].selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.ReadyToStart);
                    updated = true;
                }
            }
            if (scenario[i].status != TaskItem.TaskItemStatus.Done
                && scenario[i].status != TaskItem.TaskItemStatus.Unavailable
                && scenario[i].doneWhen.Count > 0
                && CheckDoneWhen(scenario[i]))
            {
                scenario[i].status = TaskItem.TaskItemStatus.Done;
                scenario[i].selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.Done);
                UnregisterSelectable(scenario[i].selectable);
                updated = true;
                if (i > 1) PlayerPrefs.SetInt("EducationCompleted", 1);
            }
            if (scenario[i].selectable.IsWorking()
                && scenario[i].status == TaskItem.TaskItemStatus.ReadyToStart)
            {
                scenario[i].status = TaskItem.TaskItemStatus.InProgress;
                updated = true;
            }
            if (scenario[i].status != TaskItem.TaskItemStatus.ReadyToStart)
            {
                if (scenario[i].selectable.OccupiedBy == scenario[i]){
                    UnregisterSelectable(scenario[i].selectable);
                }
            }
            else
            {
                if (scenario[i].selectable.OccupiedBy == null)
                {
                    scenario[i].selectable.OccupiedBy = scenario[i];
                    UI3DManager.Instance.RegisterSelectable(scenario[i].selectable, label);
                }
            }
        }
        return updated;
    }
    private void UnregisterSelectable(ISelectable selectable)
    {
        if (selectable == null) return;
        if (selectable.OccupiedBy != null)
        {
            selectable.OccupiedBy = null;
            if (UI3DManager.Instance != null)
                UI3DManager.Instance.UnregisterSelectable(selectable);
        }
    }
    private void CheckActionBox()
    {
        bool anyUpdated = false;
        bool updated;
        int guard = 0;
        do
        {
            updated = false;
            updated |= CheckActionBoxInternal(mainTaskScenario, "!");
            updated |= CheckActionBoxInternal(sideTaskScenario, "?");
            anyUpdated |= updated;
            if (++guard > 256)
            {
                Debug.LogError("ClickableItemsController: CheckActionBox loop exceeded guard");
                break;
            }
        } while (updated);
        if (anyUpdated) OnTaskUpdated?.Invoke();
    }

    private static bool StatusMatchesCondition(TaskItem.TaskItemStatus actual, TaskItem.TaskCondition cond)
    {
        int a = (int)actual;
        int r = (int)cond.requiredStatus;
        return cond.atLeast ? a >= r : a == r;
    }

    private bool CheckReadyWhen(TaskItem item)
    {
        if (item.readyWhen.Count == 0) return true;
        foreach (var cond in item.readyWhen)
        {
            var scenario = cond.isMain ? mainTaskScenario : sideTaskScenario;
            if (!StatusMatchesCondition(scenario[cond.itemIndex].status, cond))
                return false;
        }
        return true;
    }
    private bool CheckDoneWhen(TaskItem item)
    {
        foreach (var cond in item.doneWhen)
        {
            var scenario = cond.isMain ? mainTaskScenario : sideTaskScenario;
            if (StatusMatchesCondition(scenario[cond.itemIndex].status, cond))
                return true;
        }
        return false;
    }

    public bool IsOnlyShowTextTask(ISelectable selectable)
    {
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.onlyShowText
                && item.selectable == selectable
                && (item.status == TaskItem.TaskItemStatus.ReadyToStart || item.status == TaskItem.TaskItemStatus.InProgress))
                return true;
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.onlyShowText
                && item.selectable == selectable
                && (item.status == TaskItem.TaskItemStatus.ReadyToStart || item.status == TaskItem.TaskItemStatus.InProgress))
                return true;
        }
        return false;
    }

    public ClickableTaskInfo GetTaskInfo(ISelectable selectable)
    {
        if (selectable.OccupiedBy != null)
        {
            for (int i = 0; i < mainTaskScenario.Count; i++)
            {
                if (mainTaskScenario[i] == selectable.OccupiedBy)
                    return new ClickableTaskInfo { isTask = true, isSide = false, taskId = i };
            }
            for (int i = 0; i < sideTaskScenario.Count; i++)
            {
                if (sideTaskScenario[i] == selectable.OccupiedBy)
                    return new ClickableTaskInfo { isTask = true, isSide = true, taskId = i };
            }
        }
        for (int i = 0; i < mainTaskScenario.Count; i++)
        {
            TaskItem item = mainTaskScenario[i];
            if (item.selectable != selectable) continue;
            if (item.status == TaskItem.TaskItemStatus.InProgress || item.status == TaskItem.TaskItemStatus.ReadyToStart)
                return new ClickableTaskInfo { isTask = true, isSide = false, taskId = i };
        }
        for (int i = 0; i < sideTaskScenario.Count; i++)
        {
            TaskItem item = sideTaskScenario[i];
            if (item.selectable != selectable) continue;
            if (item.status == TaskItem.TaskItemStatus.InProgress || item.status == TaskItem.TaskItemStatus.ReadyToStart)
                return new ClickableTaskInfo { isTask = true, isSide = true, taskId = i };
        }
        return ClickableTaskInfo.None;
    }
    public bool HasReadyOrProgressTask(ISelectable selectable)
    {
        if (selectable == null) return false;
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable != selectable) continue;
            if (item.status == TaskItem.TaskItemStatus.ReadyToStart || item.status == TaskItem.TaskItemStatus.InProgress)
                return true;
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable != selectable) continue;
            if (item.status == TaskItem.TaskItemStatus.ReadyToStart || item.status == TaskItem.TaskItemStatus.InProgress)
                return true;
        }
        return false;
    }

    public bool OnSelect(ISelectable selectable)
    {
        bool selecting = false;
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable == selectable && item.status == TaskItem.TaskItemStatus.ReadyToStart)
            {
                selecting = true;
                break;
            }
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable == selectable && item.status == TaskItem.TaskItemStatus.ReadyToStart)
            {
                selecting = true;
                break;
            }
        }

        ClickableItem clickableItem = selectable.GetClickableItem();
        if (clickableItem != null && clickableItem.gameObject.layer != LayerMask.NameToLayer("ClickableItem"))
        {
            selecting = true;
        }
        if (selecting)
        {
            UnregisterSelectable(selectable);
            if (currentSelectedItem == null)
            {
                currentSelectedItem = selectable;
                currentSelectedItem.OnSelect();
            }
            else
            {
                if (currentSelectedItem != selectable)
                {
                    OnDeselect();
                    currentSelectedItem = selectable;
                    currentSelectedItem.OnSelect();
                }
            }
        }
        return selecting;
    }
    public void OnDeselect()
    {
        if (currentSelectedItem == null) return;
        currentSelectedItem.OnDeselect();
        CheckActionBox();
        currentSelectedItem = null;
    }
    public void OnContextMenu()
    {
        if (currentSelectedItem == null)
        {
            return;
        }
        bool blocked = CheckScenarioForText(mainTaskScenario, TaskItem.TextShowTime.BeforeContextMenu);
        if (blocked) return;
        blocked = CheckScenarioForText(sideTaskScenario, TaskItem.TextShowTime.BeforeContextMenu);
        if (blocked) return;
        TaskItem occupied = FindReadyTask(currentSelectedItem);
        if (occupied != null && occupied.startImmediately)
        {
            ISelectable selected = currentSelectedItem;
            ClickableItem clickable = selected != null ? selected.GetClickableItem() : null;
            if (clickable != null)
                clickable.TryStartWorkImmediately();
            CheckActionBox();
            return;
        }
        List<ContextMenuItem> items = currentSelectedItem.OnContextMenu();
        if (items != null)
        {
            UI3DManager.Instance.ShowContextMenu(currentSelectedItem.GetTransform().position, items);
        }
        UnregisterSelectable(currentSelectedItem);
    }

    TaskItem FindReadyTask(ISelectable selectable)
    {
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable == selectable
                && (item.status == TaskItem.TaskItemStatus.ReadyToStart || item.status == TaskItem.TaskItemStatus.InProgress))
                return item;
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable == selectable
                && (item.status == TaskItem.TaskItemStatus.ReadyToStart || item.status == TaskItem.TaskItemStatus.InProgress))
                return item;
        }
        return null;
    }
    bool CheckScenarioForText(List<TaskItem> taskScenario, TaskItem.TextShowTime showTime, ISelectable target = null)
    {
        bool res = false;
        if (target == null) target = currentSelectedItem;
        foreach (TaskItem item in taskScenario)
        {
            if (item.selectable == target)
                res |= ShowTaskTexts(item, showTime);
        }
        return res;
    }

    private bool ShowTaskTexts(TaskItem item, TaskItem.TextShowTime? onlyShowTime = null)
    {
        bool res = false;
        ISelectable target = item.selectable;
        int askerForThisBatch = -1;
        foreach (TaskItem.TextToShow text in item.textToShow)
        {
            if (text.showOnlyOnStepByStep && HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] < 0.5f) continue;
            if (onlyShowTime.HasValue && text.showTime != onlyShowTime.Value) continue;
            if (text.shown) continue;
            text.shown = true;

            int authorIndex = ResolveAuthorIndex(text, target, ref askerForThisBatch);
            if (askerForThisBatch >= 0)
                lastQuestionAuthorIndex = askerForThisBatch;

            TaskItem.TextToShow.CompleteSound sound = FindSoundForAuthor(text, authorIndex);
            AudioClip clip = sound != null ? sound.clip : null;
            string preview = text.text != null && text.text.Length > 40 ? text.text.Substring(0, 40) + "..." : text.text;
            narrativeVoiceQueue.Enqueue(new NarrativeVoiceEntry
            {
                authorIndex = authorIndex,
                clip = clip,
                textPreview = preview,
                timedSfx = sound != null ? sound.timedSfx : null
            });
            Debug.Log("[NarrativeQueue] enqueue author=" + authorIndex
                + " (" + AuthorName(authorIndex) + ") clip=" + (clip != null ? clip.name : "NULL")
                + " text='" + preview + "'");

            UILayersController.Instance.ShowOverlay(UILayersController.UILayer.NarrativeText, text.text + "_" + authorIndex);
            res = true;
        }
        return res;
    }

    int ResolveAuthorIndex(TaskItem.TextToShow text, ISelectable target, ref int askerInBatch)
    {
        // author == -1 → кто активировал (вопрос)
        if (text.author == -1)
        {
            int asker = ResolveActivatorAuthor(target);
            askerInBatch = asker;
            return asker;
        }

        // author == 0 → фиксированно Заяц (механические реплики и т.п.)
        if (text.author == 0)
            return 0;

        // author == 1 → фиксированный «первый» слот (часто Гусев в старых диалогах);
        // если это первая реплика батча — считаем спрашивающим.
        if (text.author == 1)
        {
            if (askerInBatch < 0)
                askerInBatch = 1;
            return 1;
        }

        // author == 2 → шаблон ответа: другой человек (не тот, кто спросил)
        int askerId = askerInBatch >= 0 ? askerInBatch : lastQuestionAuthorIndex;
        if (askerId < 0)
            return 2;
        if (askerId == 0)
        {
            int pick = Random.Range(1, 3);
            Debug.Log("[Narrative] Zaya asked → random answerer=" + pick + " (" + AuthorName(pick) + ")");
            return pick;
        }
        int other = askerId == 1 ? 2 : 1;
        Debug.Log("[Narrative] asker=" + askerId + " (" + AuthorName(askerId) + ") → answerer=" + other + " (" + AuthorName(other) + ")");
        return other;
    }

    int ResolveActivatorAuthor(ISelectable target)
    {
        GameObject checker = null;
        if (target != null && target.GetClickableItem() != null)
        {
            var clickable = target.GetClickableItem();
            checker = clickable.taskExecutor == null
                ? (PawnController.Instance != null && PawnController.Instance.currentSelectedPawn != null
                    ? PawnController.Instance.currentSelectedPawn.gameObject
                    : null)
                : clickable.taskExecutor.gameObject;
        }
        if (checker == null && PawnController.Instance != null && PawnController.Instance.currentSelectedPawn != null)
            checker = PawnController.Instance.currentSelectedPawn.gameObject;
        if (checker == null || authorsObjects == null) return -1;
        for (int i = 0; i < authorsObjects.Length; i++)
        {
            if (authorsObjects[i] == checker)
                return i;
        }
        return -1;
    }

    static TaskItem.TextToShow.CompleteSound FindSoundForAuthor(TaskItem.TextToShow text, int authorIndex)
    {
        if (text.completeSounds == null) return null;
        foreach (var sound in text.completeSounds)
        {
            if (sound.authorIndex == authorIndex)
                return sound;
        }
        return null;
    }

    string AuthorName(int index)
    {
        if (authorsObjects == null || index < 0 || index >= authorsObjects.Length || authorsObjects[index] == null)
            return "?";
        return authorsObjects[index].name;
    }

    public void PlayNextNarrativeVoice()
    {
        if (narrativeVoiceQueue.Count == 0)
        {
            Debug.Log("[NarrativeVoice] queue empty");
            return;
        }
        NarrativeVoiceEntry entry = narrativeVoiceQueue.Dequeue();
        string who = AuthorName(entry.authorIndex);
        Debug.Log("[NarrativeVoice] PLAY author=" + entry.authorIndex + " (" + who + ") clip="
            + (entry.clip != null ? entry.clip.name : "NULL") + " text='" + entry.textPreview + "'");
        if (AudioController.Instance != null)
        {
            AudioController.Instance.ClearVoiceCues();
            if (entry.clip != null)
                AudioController.Instance.Play(entry.clip);
            if (entry.timedSfx != null)
            {
                for (int i = 0; i < entry.timedSfx.Count; i++)
                {
                    TaskItem.TextToShow.TimedSfx cue = entry.timedSfx[i];
                    if (cue == null || cue.clip == null) continue;
                    AudioController.Instance.ScheduleVoiceCue(cue.clip, cue.atSeconds);
                }
            }
        }
        GameObject speaker = null;
        if (authorsObjects != null && entry.authorIndex >= 0 && entry.authorIndex < authorsObjects.Length)
            speaker = authorsObjects[entry.authorIndex];
        SliderToPawnConnector.ShowVoiceFor(speaker, entry.clip);
    }

    public void ClearNarrativeVoiceQueue()
    {
        narrativeVoiceQueue.Clear();
        if (AudioController.Instance != null)
            AudioController.Instance.ClearVoiceCues();
        SliderToPawnConnector.ClearVoiceIcons();
    }

    private void OnGameResumed()
    {
        OnDeselect();
    }
    public void OnPlayerTurnStart()
    {
        // if (taskScenario.Count > currentTaskScenarioIndex && !taskScenario[currentTaskScenarioIndex].IsWorking())
        //     UI3DManager.Instance.ShowMessage("Task " + (currentTaskScenarioIndex + 1), taskScenario[currentTaskScenarioIndex].GetTransform().position, Color.purple);
        CheckActionBox();
    }

    private void OnPlayerTurnEnd()
    {
        if (currentTaskScenarioIndex < mainTaskScenario.Count)
        {
            foreach (TaskItem item in mainTaskScenario)
            {
                if (item.status == TaskItem.TaskItemStatus.ReadyToStart)
                {
                    UnregisterSelectable(item.selectable);
                }
            }
            foreach (TaskItem item in sideTaskScenario)
            {
                if (item.status == TaskItem.TaskItemStatus.ReadyToStart)
                {
                    UnregisterSelectable(item.selectable);
                }
            }
        }
    }

    public void OnCompleteTask(ISelectable selectable)
    {
        TaskItem completedItem = FinishTaskStatus(selectable);
        if (completedItem != null)
            PresentTaskComplete(selectable, completedItem);
    }

    public TaskItem FinishTaskStatus(ISelectable selectable)
    {
        bool updated = false;
        TaskItem completedItem = CompleteInProgressTask(mainTaskScenario, selectable, ref updated);
        if (completedItem == null)
            completedItem = CompleteInProgressTask(sideTaskScenario, selectable, ref updated);
        if (completedItem != null && !string.IsNullOrEmpty(completedItem.completeText))
        {
            UI3DManager.Instance.ShowMessage(completedItem.completeText, selectable.GetTransform().position, completedItem.completeTextColor, true);
        }
        if (updated) OnTaskUpdated?.Invoke();
        if (completedItem != null)
        {
            ClickableItem clickable = selectable.GetClickableItem();
            IControlableSelectable executor = clickable != null ? clickable.taskExecutor : null;
            if (executor == null && PawnController.Instance != null)
                executor = PawnController.Instance.currentSelectedPawn;
            StatBoostService.TryGrantAfterTask(executor, completedItem);
        }
        return completedItem;
    }

    public void PresentTaskComplete(ISelectable selectable, TaskItem completedItem)
    {
        if (completedItem == null) return;
        ShowTaskTexts(completedItem, TaskItem.TextShowTime.AfterComplete);
    }

    private TaskItem CompleteInProgressTask(List<TaskItem> scenario, ISelectable selectable, ref bool updated)
    {
        foreach (TaskItem item in scenario)
        {
            if (item.selectable != selectable) continue;
            if (item.status != TaskItem.TaskItemStatus.InProgress) continue;
            item.status = TaskItem.TaskItemStatus.Done;
            item.selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.Done);
            UnregisterSelectable(item.selectable);
            CheckActionBox();
            updated = true;
            return item;
        }
        return null;
    }

    public void OnStartTask(ISelectable selectable)
    {
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable == selectable
                && (item.status == TaskItem.TaskItemStatus.ReadyToStart || item.status == TaskItem.TaskItemStatus.InProgress))
            {
                ShowTaskTexts(item, TaskItem.TextShowTime.BeforeStart);
                return;
            }
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable == selectable
                && (item.status == TaskItem.TaskItemStatus.ReadyToStart || item.status == TaskItem.TaskItemStatus.InProgress))
            {
                ShowTaskTexts(item, TaskItem.TextShowTime.BeforeStart);
                return;
            }
        }
    }
    public void OnCancelTask(ClickableItem clickableItem)
    {
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable == clickableItem && item.status == TaskItem.TaskItemStatus.InProgress)
            {
                item.status = TaskItem.TaskItemStatus.ReadyToStart;
                item.selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.ReadyToStart);
                CheckActionBox();
                return;
            }
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable == clickableItem && item.status == TaskItem.TaskItemStatus.InProgress)
            {
                item.status = TaskItem.TaskItemStatus.ReadyToStart;
                item.selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.ReadyToStart);
                CheckActionBox();
                return;
            }
        }
    }

}