using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingPause
{
    private bool _relevant;

    public bool Relevant
    {
        get { return _relevant; }
    }
    private float _startTime;
    public float StartTime
    {
        get { return _startTime; }
    }
    private Area _pauseArea;
    public Area PauseArea
    {
        get { return _pauseArea; }
    }
    private float _timeSpentLookingAtLandmarks;
    public float TimeSpentLookingAtLandmarks 
    {
        get { return _timeSpentLookingAtLandmarks; }
    }
    private float _endTime;
    public float EndTime
    {
        get { return _endTime; }
    }
    private bool _finished;
    public bool Finished
    {
        get { return _finished; }
    }

    public float Duration
    {
        get { return EndTime - StartTime; }
    }
    public WalkingPause(float startTime, Area pauseArea)
    {
        _finished = false;
        _startTime = startTime;
        _pauseArea = pauseArea;
    }
    public void SetFinished(float endTime)
    {
        _finished = true;
        _endTime = endTime;
    }

    public void AddTimeSpentLookingAtLandmarks(float dt)
    {
        _timeSpentLookingAtLandmarks += dt;
    }

    public void SetRelevant(bool relevant)
    {
        _relevant = relevant;
    }
}
