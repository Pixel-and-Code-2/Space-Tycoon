using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PointMagnetiser))]
[RequireComponent(typeof(BasicMovement))]
public class PawnBrain : MonoBehaviour
{
    private PointMagnetiser pointMagnetiser;
    private BasicMovement basicMovement;

    private List<PathNode> travelPoints = null;
    private int currentTravelPointInd = -1;

    private Action travelNextAction = null;

    void Awake()
    {
        pointMagnetiser = GetComponent<PointMagnetiser>();
        basicMovement = GetComponent<BasicMovement>();
        travelNextAction = () => StartCoroutine(basicMovement.DelayCallback(0.1f, MakeMove));
    }

    void Update()
    {
        if (travelPoints != null && travelPoints.Count > 0 && currentTravelPointInd == -1)
        {
            MakeMove();
        }
    }

    void MakeMove()
    {
        currentTravelPointInd++;
        if (currentTravelPointInd >= travelPoints.Count)
        {
            currentTravelPointInd = -1;
            travelPoints = null;
            return;
        }
        if (travelPoints[currentTravelPointInd].type == PathNodeType.ModuleCenter)
        {
            // basicMovement.LinearMoveToPosition(travelPoints[currentTravelPointInd].position, travelNextAction);
            pointMagnetiser.MagnetiseToPosition(travelPoints[currentTravelPointInd].position, travelNextAction);
        }
        else
        {
            pointMagnetiser.MagnetiseToPosition(travelPoints[currentTravelPointInd].position, travelNextAction);
        }
    }

    public void TravelToModule(ModuleMap moduleMap, ModuleData module)
    {
        travelPoints = VectorPathFinder.FindPath(moduleMap, transform.position, module.GetCenterPosition());
    }
}