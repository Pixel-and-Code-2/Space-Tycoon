using UnityEngine;

public enum ControlType
{
    walk,
    shoot
}

[RequireComponent(typeof(ISelectorBrain))]
[RequireComponent(typeof(FormulaDataMonoBase))]
public class PawnController : MonoBehaviour
{
    private ISelectorBrain selector;
    private ISelectable selectedPawn = null;
    private IControlableSelectable selectedWalkablePawn = null;
    private Vector3 lastHitPoint = Vector3.zero;
    private bool hitPointValid = false;
    [SerializeField]
    private PathDrawerWithText pathDrawerWithText;
    private ControlType controlType = ControlType.shoot;
    private bool pathFrozen = false;
    // [SerializeReference]
    [SerializeField]
    private FormulaField calculateShootAccuracy = new FormulaField();
    public const string SHOOTING_DISTANCE_LABEL = "shootingDistance";
    private FormulaDataMonoBase shootingFormulaData;

    void Awake()
    {
        selector = GetComponent<ISelectorBrain>();
        if (pathDrawerWithText == null)
        {
            Debug.LogError("PawnMoveUIController not found");
            return;
        }
        shootingFormulaData = GetComponent<FormulaDataMonoBase>();
    }

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnEnd += OnTurnEnd;
        }
        selector.OnControlTypeChange += ChangeControlType;
        pathDrawerWithText.SetVisible(false);
    }

    private void OnTurnEnd()
    {
        if (selectedPawn != null)
        {
            selectedPawn.OnDeselect();
            selectedPawn = null;
            selectedWalkablePawn = null;
        }
        pathDrawerWithText.SetVisible(false);
        pathFrozen = false;
    }

    private void OnParamsUpdated()
    {
        OnValidate();
    }

    void OnDestroy()
    {
        HandleInittingGlobalVars.onParamsUpdated -= OnParamsUpdated;
    }

    void OnValidate()
    {
        if (calculateShootAccuracy == null)
        {
            calculateShootAccuracy = new FormulaField();
        }
        // shootingFormulaData = null;
        if (shootingFormulaData == null)
        {
            Debug.LogWarning("formulaData is null, reinitting");
            shootingFormulaData = GetComponent<FormulaDataMonoBase>();
        }
        if (shootingFormulaData.parametersDict.Count < 2)
        {
            shootingFormulaData.FullFillWithParameters(new string[] { SHOOTING_DISTANCE_LABEL });
        }

        if (
            calculateShootAccuracy.names.Count < 3 ||
            calculateShootAccuracy.dataAssets[0] != shootingFormulaData as Object ||
            calculateShootAccuracy.dataAssets[1] != HandleInittingGlobalVars.pawnMustHaveParams as Object ||
            calculateShootAccuracy.dataAssets[2] != HandleInittingGlobalVars.pawnMustHaveParams as Object
            )
        {
            calculateShootAccuracy.dataAssets.Clear();
            calculateShootAccuracy.names.Clear();
            calculateShootAccuracy.names.Add("Calculated");
            calculateShootAccuracy.dataAssets.Add(shootingFormulaData as Object);
            calculateShootAccuracy.names.Add("Initiator");
            calculateShootAccuracy.dataAssets.Add(HandleInittingGlobalVars.pawnMustHaveParams as Object);
            calculateShootAccuracy.names.Add("Target");
            calculateShootAccuracy.dataAssets.Add(HandleInittingGlobalVars.pawnMustHaveParams as Object);
        }
        HandleInittingGlobalVars.onParamsUpdated -= OnParamsUpdated;
        HandleInittingGlobalVars.onParamsUpdated += OnParamsUpdated;
    }
    void Update()
    {


        if (TurnManager.Instance != null && !TurnManager.Instance.IsPlayerTurn)
        {
            return;
        }

        if (selectedPawn == null)
        {
            TryToTakeControlOfPawn();
        }
        else
        {
            ClickWithPawnHandle();
        }

        DeselectionCheck();

        if (pathFrozen && selectedWalkablePawn != null && !selectedWalkablePawn.IsMoving())
        {
            pathFrozen = false;
        }
    }

    private void TryToTakeControlOfPawn()
    {
        ISelectable selectionClick = selector.GetSelectionClickValue();
        if (selectionClick is IControlableSelectable walkablePawn)
        {
            pathDrawerWithText.SetVisible(true);
            selectedWalkablePawn = walkablePawn;
            pathFrozen = walkablePawn.IsMoving();
        }

        if (selectionClick != null)
        {
            selectedPawn = selectionClick;
            selectedPawn.OnSelect();
        }
    }

    private void ClickWithPawnHandle()
    {
        bool clicked = selector.GetSelectionClickState();
        if (clicked && hitPointValid && selectedWalkablePawn != null && !pathFrozen)
        {
            if (controlType == ControlType.walk)
            {
                selectedWalkablePawn.OnMove(lastHitPoint);
                pathFrozen = true;
                pathDrawerWithText.SetVisible(false);
            }
            else if (controlType == ControlType.shoot)
            {
                float randomValue = Random.value;
                if (randomValue < GetShootAccuracy(lastHitPoint))
                {
                    selectedWalkablePawn.OnShoot(lastHitPoint);
                }
                else
                {
                    selectedWalkablePawn.OnShoot(lastHitPoint);
                }
                selectedWalkablePawn.OnShoot(lastHitPoint);
            }
        }
    }

    private void DeselectionCheck()
    {
        bool deselectionClick = selector.GetDeselectionClickState();
        if (deselectionClick && selectedPawn != null)
        {
            pathDrawerWithText.SetVisible(false);
            selectedPawn.OnDeselect();
            selectedPawn = null;
            selectedWalkablePawn = null;
        }
    }

    private void FixedUpdate()
    {
        if (TurnManager.Instance != null && !TurnManager.Instance.IsPlayerTurn)
        {
            return;
        }

        if (selectedWalkablePawn != null)
        {
            SetAimToPoint();
        }
        else
        {
            UnSetAim();
        }
    }

    private void SetAimToPoint()
    {

        if (pathFrozen)
        {

            return;
        }
        if (controlType == ControlType.walk)
        {
            (Vector3 worldPoint, Vector2 screenPoint, FloorHitResult hit) = selector.GetMoveSelectionValue();
            if (hit != FloorHitResult.NoHit)
            {
                (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) = selectedWalkablePawn.GetPathPointsTo(worldPoint);

                if (pointsAvailable != null || pointsOutOfRange != null)
                {
                    pathDrawerWithText.SetText((PawnDataController.CalculateLineStringDistance(pointsAvailable) + PawnDataController.CalculateLineStringDistance(pointsOutOfRange)).ToString("F1") + "m", screenPoint);
                    pathDrawerWithText.SetPathPoints(pointsAvailable, pointsOutOfRange);
                    if (pointsOutOfRange != null)
                    {
                        pathDrawerWithText.SetTextColor(Color.red);
                    }
                    else
                    {
                        pathDrawerWithText.SetTextColor(Color.green);
                    }
                    if (!pathDrawerWithText.GetVisible())
                    {
                        pathDrawerWithText.SetVisible(true);
                    }
                }
                else
                {
                    pathDrawerWithText.SetVisible(false);
                }

                lastHitPoint = worldPoint;
                hitPointValid = true;
            }
        }
        else if (controlType == ControlType.shoot)
        {
            (Vector3 worldPoint, Vector2 screenPoint, PawnHitResult hit) = selector.GetShootSelectionValue();
            if (hit != PawnHitResult.NoHit)
            {
                Vector3 originPoint = selectedWalkablePawn.GetTransform().position;
                if (hit == PawnHitResult.PawnHit)
                {
                    float accuracy = GetShootAccuracy(worldPoint);
                    if (accuracy < 0.1f)
                    {
                        pathDrawerWithText.SetTextColor(Color.red);
                    }
                    else if (accuracy > 0.9f)
                    {
                        pathDrawerWithText.SetTextColor(Color.green);
                    }
                    else
                    {
                        float h = (accuracy - 0.1f) / 0.8f * 0.33f;
                        pathDrawerWithText.SetTextColor(Color.HSVToRGB(h, 1f, 1f));
                    }
                    pathDrawerWithText.SetText(
                        Vector3.Distance(originPoint, worldPoint).ToString("F1") + "m" + (accuracy * 100f).ToString("F0") + "%",
                        screenPoint
                    );
                }
                else if (hit == PawnHitResult.FloorHit)
                {
                    pathDrawerWithText.SetTextColor(Color.red);
                    pathDrawerWithText.SetText(
                        Vector3.Distance(originPoint, worldPoint).ToString("F1") + "m",
                        screenPoint
                    );
                }
                pathDrawerWithText.SetPathPoints(new Vector3[] { selectedWalkablePawn.GetTransform().position, worldPoint }, null);
                pathDrawerWithText.SetVisible(true);

                lastHitPoint = worldPoint;
                hitPointValid = true;
            }
        }
    }

    private float GetShootAccuracy(Vector3 targetPoint)
    {
        shootingFormulaData.parametersDict[SHOOTING_DISTANCE_LABEL] = Vector3.Distance(selectedWalkablePawn.GetTransform().position, targetPoint);
        float res = calculateShootAccuracy.EvaluateFormula(
            new System.Collections.Generic.Dictionary<string, float>[] {
                shootingFormulaData.parametersDict
            }
        );
        return res;
    }

    private float GetDefenceAccuracy()
    {
        return 0f;
    }
    private void UnSetAim()
    {
        hitPointValid = false;
        lastHitPoint = Vector3.zero;
    }
    public void ChangeControlType(ControlType newControlType)
    {
        controlType = newControlType;
    }
}