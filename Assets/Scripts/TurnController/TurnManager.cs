using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; } // Singleton instance
    // TurnManager()
    // {
    //     if (Instance == null) Instance = this;
    //     else
    //     {
    //         Debug.LogError("Constructor met second TurnManager instance");
    //     }
    // }

    // Actions to contoller and pawns to notify turn changes
    public event Action OnPlayerTurnStart;
    public event Action OnPlayerTurnEnd;

    public event Action OnEnemyTurnStart;
    public event Action OnEnemyTurnEnd;

    public bool IsPlayerTurn { get; private set; } = true;
    [SerializeField]
    private NavMeshSurface navMeshSurface;
    private List<UnityEngine.Object> movingPawns = new List<UnityEngine.Object>();
    [SerializeField]
    private Button endTurnButton;
    [SerializeField]
    private Sprite activeEndTurn;
    [SerializeField]
    private Sprite inactiveEndTurn;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // If we need to save current state across scenes, uncomment the next line
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(StartFirstTurn());
    }

    public void RegisterMovingPawn(UnityEngine.Object pawn)
    {
        movingPawns.Add(pawn);
        endTurnButton.image.sprite = inactiveEndTurn;
        endTurnButton.interactable = false;
    }
    public void UnregisterMovingPawn(UnityEngine.Object pawn)
    {
        movingPawns.Remove(pawn);
        if (movingPawns.Count == 0)
        {
            endTurnButton.interactable = true;
            endTurnButton.image.sprite = activeEndTurn;
        }
    }

    private IEnumerator StartFirstTurn()
    {
        yield return null; // Wait one frame to ensure all components are initialized
        EndEnemyTurn();
    }

    public void StartPlayerTurn()
    {
        IsPlayerTurn = true;
        OnPlayerTurnStart?.Invoke();
        Debug.Log("PLAYER TURN START");
    }

    public void EndPlayerTurn()
    {
        if (movingPawns.Count > 0) return;
        IsPlayerTurn = false;
        OnPlayerTurnEnd?.Invoke();
        Debug.Log("PLAYER TURN END");
        StartCoroutine(StartEnemyTurnWithDelay());
    }

    private IEnumerator StartEnemyTurnWithDelay()
    {
        // yield return new WaitForSeconds(0.1f);
        UpdateNavMesh();
        // yield return new WaitForSeconds(0.1f);
        EnemyTurn();
        yield return null;
    }

    private void EnemyTurn()
    {
        OnEnemyTurnStart?.Invoke();
        Debug.Log("Enemy is making its move");
    }

    public void EndEnemyTurn()
    {
        OnEnemyTurnEnd?.Invoke();
        StartCoroutine(StartPlayerTurnWithDelay());
    }

    private IEnumerator StartPlayerTurnWithDelay()
    {
        // yield return new WaitForSeconds(0.1f);
        UpdateNavMesh();
        // yield return new WaitForSeconds(0.1f);
        StartPlayerTurn();
        yield return null;
    }

    public void UpdateNavMesh()
    {
        navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
    }
}
