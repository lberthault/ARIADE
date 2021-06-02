using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HololensTracker : MonoBehaviour
{

    public enum Direction
    {
        LEFT = -1,
        RIGHT = 1,
        UP = 2,
        DOWN = -2
    }

    public enum Action
    {
        TURN_LEFT,
        TURN_RIGHT,
        GO_FORWARD,
        GO_BACKWARD
    }

    SimulationManager simManager;
    float simTime;

    //Data
    private string dataFileName;
    private List<DataSnapshot> data;
    public Path matchingPath = new Path();
    public Path walkedPath = new Path();
    public Area currentArea;
    public Area lastMatchingArea;
    public List<Error> errors;

    //Trail
    private TrailRenderer trailRenderer;
    public Material hololensTrailMaterial;
    public float trailTime = 9999f;
    public float trailSize = 0.05f;
    public float trailSensibility = 0.01f;

    Vector3 lastPosition;
    public float bigJumpThreshold = 5f;

    private Error currentError;

    private void Awake()
    {
        data = new List<DataSnapshot>();
        errors = new List<Error>();
        simManager = GameObject.Find("SimulationManager").GetComponent<SimulationManager>();
        dataFileName = "HololensData";
        if (simManager.Mode == SimulationManager.DEBUG_MODE)
        {
            trailRenderer = (TrailRenderer)gameObject.AddComponent(typeof(TrailRenderer));
            trailRenderer.startWidth = trailSize;
            trailRenderer.endWidth = trailSize;
            trailRenderer.time = trailTime;
            trailRenderer.minVertexDistance = trailSensibility;
            trailRenderer.material = hololensTrailMaterial;
        }
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, lastPosition) > bigJumpThreshold)
        {
            if (trailRenderer != null) trailRenderer.Clear();
        }
        lastPosition = transform.position;
    }

    private void CheckTrailRendererModifications()
    {
        if (trailRenderer.time != trailTime)
        {
            trailRenderer.time = trailTime;
        }
        if (trailRenderer.startWidth != trailSize)
        {
            trailRenderer.startWidth = trailSize;
            trailRenderer.endWidth = trailSize;
        }
        if (trailRenderer.minVertexDistance != trailSensibility)
        {
            trailRenderer.minVertexDistance = trailSensibility;
        }
        if (trailRenderer.material != hololensTrailMaterial)
        {
            trailRenderer.material = hololensTrailMaterial;
        }
    }

    public int Track(float simTime)
    {
        this.simTime = simTime;
        if (trailRenderer != null)
        {
            CheckTrailRendererModifications();
        }
        RegisterData();
        WriteData();
        return 0;
    }

    public void RegisterData()
    {
        data.Add(new DataSnapshot(simTime, transform.position, transform.rotation.eulerAngles));
    }

    public void WriteData()
    {
        DataManager.WriteDataInFile(dataFileName, simManager.GetNavConfig(), "t = " + simTime + " : pos = " + transform.position + " rot = " + transform.rotation.eulerAngles);
        DataManager.WriteDataInFile(dataFileName, simManager.GetNavConfig(), "      inst_v = " + Converter.Round(CurrentSpeed(), 2) + " prev_inst_a = " + Converter.Round(PreviousAcceleration(), 2));
        DataManager.WriteDataInFile(dataFileName, simManager.GetNavConfig(), "      total_d = " + Converter.Round(DistanceTravelled(), 2) + " mean_v = " + Converter.Round(MeanSpeed(), 2));
    }

    public Area AreaAtIndex(int i)
    {
        return walkedPath.Get(i);
    }

    public int NumberOfEntries(Area area)
    {
        int n = 0;
        for (int i = 0; i < walkedPath.Count(); i++)
        {
            if (walkedPath.Get(i).Equals(area))
            {
                n++;
            }
        }
        return n;
    }

    public float TimeInArea(int i)
    {
        return AreaAtIndex(i).Time;
    }

    public float AverageTimeInArea()
    {
        return (simTime - walkedPath.Get(0).inTime) / walkedPath.Count();
    }

    public float CurrentSpeed()
    {
        return Speed(data.Count - 1);
    }
    public float PreviousAcceleration()
    {
        return Acceleration(data.Count - 2);
    }

    public float DistanceTravelled()
    {
        float res = 0f;
        int N = data.Count - 1;
        float d;
        for (int i = 0; i < N; i++)
        {
            d = Vector3.Distance(ProjectOnFloor(data[i].Position), ProjectOnFloor(data[i + 1].Position));
            if (d <= 1f)
            {
                res += d;
            }
        }
        return res;
    }

    public float MeanSpeed()
    {
        float v = 0f;
        int N = data.Count - 1;
        if (N > 0)
        {
            v = DistanceTravelled() / (data[N].Time - data[0].Time);
        }
        return v;
    }

    public float Speed(float t)
    {
        return Speed(Converter.TimeToIndex(data, t));
    }

    public float Speed(int i)
    {
        if (i <= 0 || i >= data.Count)
        {
            return -1;
        }
        float d = Vector3.Distance(ProjectOnFloor(data[i - 1].Position), ProjectOnFloor(data[i].Position));
        float t = data[i].Time - data[i - 1].Time;
        float v = d / t;
        if (v > 10f)
        {
            v = -1;
        }
        return v;
    }

    public float Acceleration(float t)
    {
        return Acceleration(Converter.TimeToIndex(data, t));
    }
    public float Acceleration(int i)
    {
        if (i <= 0 || i >= data.Count - 1)
        {
            return -1;
        }
        if (Speed(i) == -1 || Speed(i + 1) == -1)
        {
            return -1;
        } else
        {

            float v = Speed(i + 1) - Speed(i);
            float t = data[i + 1].Time - data[i - 1].Time;
            return v / t;
        }
    }

    private void OnTriggerEnter(Collider collision)
    {

        if (collision.gameObject.TryGetComponent<AreaDetector>(out AreaDetector detector))
        {

            if (simManager.TrialState == SimulationManager.TRIAL_ONGOING)
            {
                EnteringArea(detector.Area);
            } else
            {
                if (simManager.TrialState == SimulationManager.TRIAL_NOT_STARTED)
                {
                    if ((detector.Area.IsOnExternalBorder() || detector.Area.IsOnInternalBorder()) && simManager.trialPath.Contains(detector.Area))
                    {
                        simManager.StartTrial(detector.Area);
                        EnteringArea(detector.Area);
                    }
                }
            }
            
        }

    }

    public int NumberOfErrors()
    {
        return errors.Count;
    }

    public int TotalWrongAreas()
    {
        int res = 0;
        foreach (Error error in errors)
        {
            res += error.NumberOfWrongAreas();
        }
        return res;
    }

    public float TotalErrorDistance()
    {
        float res = 0;
        foreach (Error error in errors)
        {
            res += GetDistance(error.path.Get(1).inTime, error.path.GetLast().inTime);
        }
        return res;
    }

    public Vector3 ProjectOnFloor(Vector3 v)
    {
        return new Vector3(v.x, 0, v.z);
    }

    public float GetDistance(int i, int j)
    {
        return Vector3.Distance(ProjectOnFloor(data[i].Position), ProjectOnFloor(data[j].Position));
    }

    public float GetDistance(float t0, float tf)
    {
        return GetDistance(Converter.TimeToIndex(data, t0), Converter.TimeToIndex(data, tf));
    }

    public float TotalErrorTime()
    {
        float res = 0;
        foreach (Error error in errors)
        {
            res += error.Time();
        }
        return res;
    }

    private Area NextArea(int i)
    {
        return simManager.remainingPath.Get(i);
    }

    private void EnteringArea(Area area)
    {
        if (currentArea == null)
        {
            Area lastArea = walkedPath.GetLast();
            currentArea = new Area(area, simTime);
            bool onTheRightPath = false;
            if (NextArea(0).Equals(currentArea))
            {
                onTheRightPath = true;
                simManager.remainingPath.Pop();
            } else if (walkedPath.Count() == 0)
            {
                for (int i = 0; i < simManager.remainingPath.Count(); i++)
                {
                    if (simManager.remainingPath.Get(i).Equals(currentArea))
                    {
                        onTheRightPath = true;
                        for (int j = 0; j < i + 1; j++)
                        {
                            simManager.remainingPath.Pop();
                        }
                    }
                }
            }
            if (simManager.remainingPath.Count() == 0)
            {
                simManager.EndTrial();
                return;
            }
            Area nextArea = null;
            Area nextNextArea = null;
            if (simManager.remainingPath.Count() > 1)
            {
                nextArea = NextArea(0);
                nextNextArea = NextArea(1);
            }
               
            if (onTheRightPath)
            {
                if (currentError != null)
                {
                    currentError.SetCorrect(simTime);
                    currentError = null;
                }
                if (lastArea != null)
                {
                    simManager.RemoveAdviceAtArea(lastArea);
                }
                lastMatchingArea = new Area(area);
                matchingPath.Add(lastMatchingArea);
                if (nextArea != null && nextNextArea != null)
                {
                    Vector3 position = AdviceBasePosition(nextArea) + AdvicePositionOffset(currentArea, nextArea, nextNextArea);
                    Vector3 rotation = AdviceRotation(currentArea, nextArea, nextNextArea);
                    AddAdvice(nextArea, position, rotation);
                }

            }
            else
            {
                bool worsening;
                if (currentError == null)
                {
                    simManager.remainingPath.Push(lastArea);
                    simManager.RemoveAllAdvice();
                    currentError = new Error(walkedPath.GetLast(), currentArea, simTime);
                    errors.Add(currentError);
                    worsening = true;
                }
                else
                {
                    if (worsening = currentError.Update(currentArea))
                    {
                        // Error worsening
                        simManager.remainingPath.Push(currentError.path.Get(currentError.path.Count() - 2));
                    }
                }
                Vector3 position;
                Vector3 rotation;
                if (worsening)
                {
                    simManager.RemoveAllAdvice();
                    position = AdviceBasePosition(currentArea) + WrongWayAdvicePositionOffset(currentArea, NextArea(0));
                    rotation = WrongWayAdviceRotation(currentArea, NextArea(0));
                    AddWrongWayAdvice(currentArea, position, rotation);
                    
                } else
                {
                    simManager.RemoveAdviceAtArea(lastArea);
                    simManager.remainingPath.Pop();
                }
                position = AdviceBasePosition(NextArea(0)) + AdvicePositionOffset(currentArea, NextArea(0), NextArea(1));
                rotation = AdviceRotation(currentArea, NextArea(0), NextArea(1));
                AddAdvice(NextArea(0), position, rotation);
            }
            walkedPath.Add(currentArea);
        }
    }

    private Vector3 AdviceBasePosition(Area area)
    {
        return Converter.AreaToVector3(area, simManager.AdviceBaseHeight);
    }

    private void AddWrongWayAdvice(Area area, Vector3 position, Vector3 rotation)
    {
        GameObject wrongWayAdvicePrefab = simManager.GetWrongWayAdvicePrefab();
        Advice advice = new Advice(area, wrongWayAdvicePrefab, position, rotation);
        simManager.AddAdvice(advice);
    }

    private Vector3 WrongWayAdviceRotation(Area currentArea, Area lastMatchingArea)
    {
        return AdviceRotation(lastMatchingArea, currentArea, null);
    }

    private Vector3 WrongWayAdvicePositionOffset(Area currentArea, Area lastMatchingArea)
    {
        return AdvicePositionOffset(currentArea, lastMatchingArea, null);
    }

    private void AddAdvice(Area area, Vector3 position, Vector3 rotation)
    {
        GameObject advicePrefab = simManager.GetAdvicePrefab();
        Advice advice = new Advice(area, advicePrefab, position, rotation);
        simManager.AddAdvice(advice);
    }
    
    private Direction GetDirection(Area from, Area to)
    {
        if (from.IsBigArea())
        {
            if (to.column == 2)
            {
                return Direction.LEFT;
            }
            if (to.column == 5)
            {
                return Direction.RIGHT;
            }
            if (to.line == 1)
            {
                return Direction.UP;
            }
            return Direction.DOWN;
            
        }
        else if (to.IsBigArea())
        {
            if (from.column == 2)
            {
                return Direction.RIGHT;
            }
            if (from.column == 5)
            {
                return Direction.LEFT;
            }
            if (from.line == 1)
            {
                return Direction.DOWN;
            }
            return Direction.UP;
            
        } else
        {
            int dLine = to.line - from.line;
            int dColumn = to.column - from.column;
            if (dLine >= 1)
            {
                return Direction.DOWN;
            }
            if (dLine <= -1)
            {
                return Direction.UP;
            }
            if (dColumn >= 1)
            {
                return Direction.RIGHT;
            }
            return Direction.LEFT;
        }
    }

    public Action GetAction(Area from, Area at, Area to)
    {
        Direction d1 = GetDirection(from, at);
        Direction d2 = GetDirection(at, to);
        return GetAction(d1, d2);
    }

    public Action GetAction(Direction d1, Direction d2)
    {
        if (d1 == d2)
        {
            return Action.GO_FORWARD;
        }
        if (AreOppositeDirections(d1, d2))
        {
            return Action.GO_BACKWARD;
        }
        if ((d1 == Direction.RIGHT && d2 == Direction.UP)
            || (d1 == Direction.UP && d2 == Direction.LEFT)
            || (d1 == Direction.LEFT && d2 == Direction.DOWN)
            || (d1 == Direction.DOWN && d2 == Direction.RIGHT))
        {
            return Action.TURN_LEFT;
        }
        return Action.TURN_RIGHT;
    }

    public bool AreOppositeDirections(Direction d1, Direction d2)
    {
        return (int)d1 + (int)d2 == 0;
    }

    public Vector3 AdviceRotation(Area from, Area at, Area to)
    {
        Direction d1 = GetDirection(from, at);
        Direction d2;
        Action action;
        Vector3 r;
        if (to != null)
        {
            r = simManager.GetAdvicePrefab().transform.rotation.eulerAngles;
            d2 = GetDirection(at, to);
             action = GetAction(d1, d2);
        } else
        {
            r = simManager.GetWrongWayAdvicePrefab().transform.rotation.eulerAngles;
            action = Action.GO_BACKWARD;
        }
        if (d1 == Direction.LEFT)
        {
            r.y -= 90; 
        } else if (d1 == Direction.RIGHT)
        {
            r.y += 90;
        }
        else if (d1 == Direction.DOWN)
        {
            r.y += 180;
        }
        if (action == Action.TURN_LEFT)
        {
            r.y -= 100;
        }
        else if (action == Action.TURN_RIGHT)
        {
            r.y += 100;
        }
        else if (action == Action.GO_FORWARD)
        {
            r.y += 30;
        } else
        {
            r.y += 180;
        }
        return r;
    }

    public Vector3 AdvicePositionOffset(Area from, Area at, Area to)
    {
        Direction d1 = GetDirection(from, at);
        Direction d2;
        Action action;
        if (to != null)
        {
            d2 = GetDirection(at, to);
            action = GetAction(d1, d2);
        }
        else
        {
            action = Action.GO_BACKWARD;
        }
        float baseOffset = simManager.AdviceBaseOffset;
        if (at.IsBigArea())
        {
            baseOffset *= 2;
        }
        Vector3 offset = Vector3.zero;
        if (action == Action.GO_FORWARD)
        {
            switch (d1)
            {
                case Direction.UP: offset += new Vector3(1, 0, 1) * baseOffset; break;
                case Direction.DOWN: offset += new Vector3(-1, 0, -1) * baseOffset; break;
                case Direction.LEFT: offset += new Vector3(-1, 0, 1) * baseOffset; break;
                case Direction.RIGHT: offset += new Vector3(1, 0, -1) * baseOffset; break;
            }
        } else if (action == Action.GO_BACKWARD)
        {
            switch (d1)
            {
                case Direction.UP: offset += new Vector3(-1, 0, 0) * baseOffset; break;
                case Direction.DOWN: offset += new Vector3(1, 0, 0) * baseOffset; break;
                case Direction.LEFT: offset += new Vector3(0, 0, -1) * baseOffset; break;
                case Direction.RIGHT: offset += new Vector3(0, 0, 1) * baseOffset; break;
            }
        } else if (action == Action.TURN_LEFT)
        {
            switch (d1)
            {
                case Direction.UP: offset += new Vector3(1, 0, 1) * baseOffset; break;
                case Direction.DOWN: offset += new Vector3(-1, 0, -1) * baseOffset; break;
                case Direction.LEFT: offset += new Vector3(-1, 0, 1) * baseOffset; break;
                case Direction.RIGHT: offset += new Vector3(1, 0, -1) * baseOffset; break;
            }
        } else if (action == Action.TURN_RIGHT)
        {
            switch (d1)
            {
                case Direction.UP: offset += new Vector3(1, 0, -1) * baseOffset; break;
                case Direction.DOWN: offset += new Vector3(-1, 0, 1) * baseOffset; break;
                case Direction.LEFT: offset += new Vector3(1, 0, 1) * baseOffset; break;
                case Direction.RIGHT: offset += new Vector3(-1, 0, -1) * baseOffset; break;
            }
        }
        return offset;
    }
    
    private void ExitingArea()
    {
        if (currentArea != null)
        {
            currentArea.outTime = simTime;
            currentArea = null;
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.TryGetComponent<AreaDetector>(out AreaDetector detector))
        {
            if (simManager.TrialState == SimulationManager.TRIAL_ONGOING)
            {
                ExitingArea();
            }
        }
    }

    public void ResetData()
    {
        data.Clear();
    }

    public float MeanHeight()
    {
        float res = 0f;
        foreach (DataSnapshot snap in data)
        {
            res += snap.Position.y;
        }
        return res / data.Count;
    }

}
