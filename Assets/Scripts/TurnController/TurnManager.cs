using System;
using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; } // Singleton instance

    // Actions to contoller and pawns to notify turn changes
    public event Action OnPlayerTurnStart;
    public event Action OnPlayerTurnEnd;

    public event Action OnEnemyTurnStart;
    public event Action OnEnemyTurnEnd;

    public bool IsPlayerTurn { get; private set; } = true;

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

    private IEnumerator StartFirstTurn()
    {
        yield return null; // Wait one frame to ensure all components are initialized
        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        IsPlayerTurn = true;
        OnPlayerTurnStart?.Invoke();
        Debug.Log("PLAYER TURN START");
    }

    public void EndPlayerTurn()
    {
        IsPlayerTurn = false;
        OnPlayerTurnEnd?.Invoke();
        Debug.Log("PLAYER TURN END");
        //Enemy Turn
        StartCoroutine(EnemyTurn());
    }

    private IEnumerator EnemyTurn()
    {
        OnEnemyTurnStart?.Invoke();

        Debug.Log("Enemy is thinking");
        yield return new WaitForSeconds(4.0f); // Simulate enemy thinking time

        OnEnemyTurnEnd?.Invoke();
        // Back to Player Turn
        StartPlayerTurn();
    }
}
