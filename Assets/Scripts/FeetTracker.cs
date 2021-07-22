using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class FeetTracker : MonoBehaviour
{

    public const int NO_CHANGE = 0;

    // Data
    private const string dataFileName = "Feet";
    private List<Step> _steps;
    public List<Step> Steps
    {
        get { return _steps; }
    }

    // Simulation
    private float simTime;

    // Debug
    public Material frontFootMaterial;
    public Material backFootMaterial;

    // Tracking
    private FootTracker _lFoot, _rFoot;
    public FootTracker LFoot {
        get { return _lFoot; }
    }
    public FootTracker RFoot
    {
        get { return _rFoot; }
    }
    private Foot frontFoot;
    private bool isSetUp = false;

    public Step LastStep
    {
        get
        {
            return (StepCount != 0) ? _steps[StepCount - 1] : null;
        }
    }

    public Step LastFinishedStep
    {
        get
        {
            if (LastStep == null)
                return null;
            return LastStep.IsFinished() ? LastStep : _steps[StepCount - 2];
        }
    }

    public int StepCount
    {
        get
        {
            if (_steps.Count != 0)
            {
                if (!_steps[_steps.Count - 1].IsFinished())
                {
                    return _steps.Count - 1;
                }
            }
            return _steps.Count;
        }
    }

    private void Awake()
    {
        _steps = new List<Step>();
        SearchFootTrackers();
    }

    /* Detects and asigns automatically the left and right foot trackers */  
    private void SearchFootTrackers()
    {
        foreach (Transform child in transform.GetComponentsInChildren<Transform>())
        {
            GameObject childGo = child.gameObject;
            if (childGo.TryGetComponent(out FootTracker footTracker))
            {
                if (footTracker.Foot == Foot.Left)
                {
                    if (_lFoot != null)
                    {
                        Debug.LogError("ERROR : more than one object is assigned to left foot");
                    }
                    else
                    {
                        _lFoot = footTracker;
                    }
                }
                else
                {
                    if (_rFoot != null)
                    {
                        Debug.LogError("ERROR : more than one object is assigned to right foot");
                    }
                    else
                    {
                        _rFoot = footTracker;
                    }
                }
            }
        }

        if (_lFoot == null)
        {
            Debug.LogError("ERROR : no object is assigned to left foot");
        }

        if (_rFoot == null)
        {
            Debug.LogError("ERROR : no object is assigned to right foot");
        }

        if (_lFoot != null && _rFoot != null)
        {
            isSetUp = true;
        }
    }

    public void Track(float simTime)
    {
        this.simTime = simTime;
        if (isSetUp)
        {
            frontFoot = DataAnalyzer.FrontFoot(_lFoot.LastDataSnapshot, _rFoot.LastDataSnapshot);
            TrackFoot(_lFoot);
            TrackFoot(_rFoot);
            WriteData();
        }        
    }

    private bool startCountingPause = false;
    public WalkingPause currentPause;
    private float lastStepEndTime = -1f;
    private void TrackFoot(FootTracker foot)
    {
        int res = foot.Track(simTime);
        // Set front foot in different color
        MeshRenderer footRenderer = foot.gameObject.GetComponentInChildren<MeshRenderer>();
        if (footRenderer != null)
        {
            footRenderer.material = (foot.Foot == frontFoot) ? frontFootMaterial : backFootMaterial;
        }

        /* PAUSE DETECTION */
        if (startCountingPause 
            && LastStep != null 
            && LastStep.IsFinished()
            && lastStepEndTime != -1f
            && simTime - lastStepEndTime > 1.5f
            && !currentPause.Relevant)
        {
            currentPause.SetRelevant(true);
            Debug.Log("RELEVANT");
        }

        switch (res)
        {
            case FootTracker.NO_CHANGE: 
                break;
            case FootTracker.STEP_START:
                OnStepStart(foot);
                break;
            case FootTracker.STEP_END:
                OnStepEnd(foot);
                break;
            case FootTracker.STEP_REMOVE:
                OnStepRemove(foot);
                break;

        }
        CheckSteps();
    }

    public void ResetData()
    {
        _lFoot.ResetData();
        _rFoot.ResetData();
        _steps.Clear();
    }

    private void OnStepStart(FootTracker foot)
    {
        _steps.Add(foot.LastStep);
        startCountingPause = false;
        lastStepEndTime = -1f;
        GameManager.Instance.EndWalkingPause(currentPause);
        currentPause = null;
    }

    private void OnStepEnd(FootTracker foot)
    {
        lastStepEndTime = simTime;
        currentPause = new WalkingPause(simTime, GameManager.Instance.hololensCore.WalkedPath.GetLast());
        GameManager.Instance.StartWalkingPause(currentPause);
        startCountingPause = true;
        return;
    }

    public void CleanSteps()
    {
        for (int i = 0; i < StepCount; i++)
        {
            if (!Steps[i].IsFinished())
            {
                Steps.RemoveAt(i);
            }
        }
        CheckSteps();
    }

    private void CheckSteps()
    {
        for (int i = 0; i < Steps.Count; i++)
        {
            if (Steps[i].IsFinished() && (Steps[i].Duration < 0.1f || Steps[i].Duration > 2f || Steps[i].Length < 0.1f || Steps[i].Length > 2f))
            {
                Steps.RemoveAt(i);
            }
        }
    }
    private void OnStepRemove(FootTracker foot)
    {
        _steps.RemoveAt(_steps.Count - 1);
    }

    public void WriteData()
    {
        return;
    }

    public void RegisterData()
    {
        return;
    }

}
