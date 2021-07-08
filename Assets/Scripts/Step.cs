using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Step
{
    private Foot _foot;
    public Foot Foot
    {
        get { return _foot; }
    }

    private bool finished;
    public float StartTime { get; }
    public float EndTime { get; set; }
    public Vector3 StartPos { get; }
    public Vector3 EndPos { get; set; }
    public float Duration
    {
        get { return EndTime - StartTime; }
    }

    public float Length 
    {
        get { return Vector3.Distance(Utils.ProjectOnFloor(StartPos), Utils.ProjectOnFloor(EndPos)); }
    }

    public Step(Foot foot, float start, float end, Vector3 startPos)
    {
        this._foot = foot;
        this.StartTime = start;
        this.EndTime = end;
        this.finished = start < end;
        this.StartPos = startPos;
    }
    public Step(Foot foot, float start, float end, Vector3 startPos, bool finished)
    {
        this._foot = foot;
        this.StartTime = start;
        this.EndTime = end;
        this.StartPos = startPos;
        if (GameManager.Instance.drawDebug)
        {
            GameObject marker = GameObject.Instantiate(GameManager.Instance.stepMarkerPrefab, startPos, Quaternion.identity, GameObject.Find("StepMarkers").transform);
            marker.GetComponent<Renderer>().material.color = Color.green;
            marker.name = "Start";
        }

        this.finished = finished;
    }

    public bool IsFinished()
    {
        return finished;
    }

    public void SetFinished(bool finished)
    {
        this.finished = finished;
        if (GameManager.Instance.drawDebug)
        {
            GameObject marker = GameObject.Instantiate(GameManager.Instance.stepMarkerPrefab, EndPos, Quaternion.identity, GameObject.Find("StepMarkers").transform);
            marker.name = "End";
            marker.GetComponent<Renderer>().material.color = Color.red;
        }
    }

}
