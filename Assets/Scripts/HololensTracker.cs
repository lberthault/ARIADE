using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

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
        if (simManager.drawLines)
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
        /*
        DataManager.WriteDataInFile(dataFileName, simManager.GetNavConfig(), "t = " + simTime + " : pos = " + transform.position + " rot = " + transform.rotation.eulerAngles);
        DataManager.WriteDataInFile(dataFileName, simManager.GetNavConfig(), "      inst_v = " + Converter.Round(CurrentSpeed(), 2) + " prev_inst_a = " + Converter.Round(PreviousAcceleration(), 2));
        DataManager.WriteDataInFile(dataFileName, simManager.GetNavConfig(), "      total_d = " + Converter.Round(DistanceTravelled(), 2) + " mean_v = " + Converter.Round(MeanSpeed(), 2));
        */
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

    public void InitAdvice()
    {
        Area nextArea = NextArea(0);
        Area nextNextArea = NextArea(1);
        Area nextNextNextArea = NextArea(2);
        Vector3 position;
        Vector3 rotation;
        if (simManager.GetAdviceName() == SimulationManager.AdviceName.PEANUT)
        {
            position = AdviceBasePosition(nextNextArea) + AdvicePositionOffset(nextArea, nextNextArea, nextNextNextArea, true);
            rotation = AdviceRotation(nextArea, nextNextArea, nextNextNextArea, true);
            InstantiateCompanion(position, Quaternion.Euler(rotation));
            int direction = 1;
            if (GetAction(nextArea, nextNextArea, nextNextNextArea) == Action.TURN_LEFT)
            {
                direction = -1;
            }
            SetCompanionState(direction);
        }
        else if (simManager.GetAdviceName() == SimulationManager.AdviceName.ARROW)
        {
            position = AdviceBasePosition(nextNextArea) + AdvicePositionOffset(nextArea, nextNextArea, nextNextNextArea, true);
            rotation = AdviceRotation(nextArea, nextNextArea, nextNextNextArea, true);
            AddArrow(nextNextArea, position, rotation);
        }
        else
        {
            simManager.DrawLightPath(nextArea, nextNextArea, nextNextNextArea, NextArea(3));
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
        if (i >= simManager.remainingPath.Count())
            return null;
        return simManager.remainingPath.Get(i);
    }

    public int removeLightAdvice = 0;
    private void EnteringArea(Area area)
    {
        if (currentArea == null)
        {
          
            Area lastArea = walkedPath.GetLast();

            // Prevent area double check
            if (lastArea != null && area.Equals(lastArea))
            {
                return;
            }

            // Update landmarks
            if (lastArea != null && !(area.InBigArea() && lastArea.InBigArea()))
            {
                lastArea.GetAreaDetector().RemoveLandmarks(true);
                AreaDetector areaDetector = area.GetAreaDetector();
                areaDetector.DisplayLandmarks(GetDirection(lastArea, area), true);
            }

            // Copy entered area to add it an "in time"
            currentArea = new Area(area, simTime);

            // Check if participant is on the right path and update remaining path
            bool onTheRightPath = false;
            if (NextArea(0).Equals(currentArea))
            {
                onTheRightPath = true;
                simManager.remainingPath.Pop();
            }
            else if (walkedPath.Count() == 0)
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

            // Check if the participant has finished the trial
            if (simManager.remainingPath.Count() == 0)
            {
                simManager.EndTrial();
                return;
            }

            // Store theoretical next and "next next" areas (i.e. disregarding the potential errors)
            Area nextArea = null;
            Area nextNextArea = null;
            if (simManager.remainingPath.Count() > 0)
            {
                nextArea = NextArea(0);
            }

            if (simManager.remainingPath.Count() > 1)
            {
                nextNextArea = NextArea(1);
            }

            if (onTheRightPath)
            {
                // If an error was registered but the participant has corrected it, remove it
                if (currentError != null)
                {
                    currentError.SetCorrect(simTime);
                    currentError = null;
                }

                // Remove previous advices if necessary
                if (lastArea != null && (nextArea == null || nextNextArea != null))
                {
                    if (simManager.GetAdviceName() == SimulationManager.AdviceName.ARROW || simManager.GetAdviceName() == SimulationManager.AdviceName.PEANUT)
                    {
                        RemoveLastAdvice(lastArea);
                    } else if (simManager.GetAdviceName() == SimulationManager.AdviceName.LIGHT)
                    {
                        if (removeLightAdvice == 1)
                        {
                            RemoveLastAdvice(lastArea);
                        } else
                        {
                            removeLightAdvice++;
                        }
                    }
                }

                // Update matching path because participant is on the right way
                matchingPath.Add(new Area(area));

                // Display the next advice

                Vector3 position;
                Vector3 rotation;
                if (simManager.GetAdviceName() == SimulationManager.AdviceName.ARROW)
                {
                    if (walkedPath.Count() != 0)
                    {
                        position = AdviceBasePosition(nextArea);
                        if (nextNextArea != null)
                        {
                            position += AdvicePositionOffset(currentArea, nextArea, nextNextArea, true);
                        }
                        rotation = AdviceRotation(currentArea, nextArea, nextNextArea, true);
                        AddArrow(nextArea, position, rotation);
                    }
                } else if (simManager.GetAdviceName() == SimulationManager.AdviceName.LIGHT)
                {
                      simManager.DrawLightPath(currentArea, nextArea, nextNextArea, NextArea(2));
                } else if (simManager.GetAdviceName() == SimulationManager.AdviceName.PEANUT)
                {        
                    if (walkedPath.Count() != 0)
                    {

                        if (nextArea.InBigArea() && nextNextArea.InBigArea())
                        {
                            SetCompanionState(0);
                            object[] parms = new object[3] { currentArea, nextArea, nextNextArea };
                            StopCoroutine(nameof(MoveCompanion));
                            StartCoroutine(nameof(MoveCompanion2), parms);
                        }
                        else if (!nextArea.InBigArea())
                        {
                            SetCompanionState(0);
                            object[] parms = new object[3] { currentArea, nextArea, nextNextArea };
                            StopCoroutine(nameof(MoveCompanion));
                            StartCoroutine(nameof(MoveCompanion), parms);
                        }
                    }
                    
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
                    if (simManager.GetAdviceName() == SimulationManager.AdviceName.ARROW)
                    {
                        position = AdviceBasePosition(currentArea) + WrongWayAdvicePositionOffset(currentArea, NextArea(0));
                        rotation = WrongWayAdviceRotation(currentArea, NextArea(0));
                        AddWrongWayAdvice(currentArea, position, rotation);
                    }
                    else if (simManager.GetAdviceName() == SimulationManager.AdviceName.LIGHT)
                    {
                        removeLightAdvice = 0;
                        simManager.DrawLightPath(currentArea, NextArea(0), NextArea(1), NextArea(2));
                        // simManager.DrawLightPath(Converter.AreaToVector3(NextArea(0), 0.2f), Converter.AreaToVector3(NextArea(1), 0.2f));
                        position = AdviceBasePosition(currentArea) + WrongWayAdvicePositionOffset(currentArea, NextArea(0));
                        rotation = WrongWayAdviceRotation(currentArea, NextArea(0));
                        AddWrongWayAdvice(currentArea, position, rotation);
                        //simManager.DrawWrongWayLightPath(currentArea, WrongWayAdvicePositionOffset(currentArea, NextArea(0)), WrongWayAdviceRotation(currentArea, NextArea(0)).y);

                    } else
                    {
                        position = AdviceBasePosition(currentArea) + WrongWayAdvicePositionOffset(currentArea, NextArea(0));
                        rotation = WrongWayAdviceRotation(currentArea, NextArea(0));
                        AddWrongWayAdvice(currentArea, position, rotation);
                    }
                }
                else
                {
                    if (simManager.GetAdviceName() == SimulationManager.AdviceName.ARROW)
                    {
                        simManager.RemoveAdviceAtArea(lastArea);
                        simManager.remainingPath.Pop();
                    }
                    else if (simManager.GetAdviceName() == SimulationManager.AdviceName.LIGHT)
                    {
                        simManager.RemoveWrongWayLightAdvice();
                        if (removeLightAdvice == 1)
                        {
                            RemoveLastAdvice(lastArea);
                        }
                        else
                        {
                            removeLightAdvice++;
                        }
                        simManager.DrawLightPath(currentArea, NextArea(0), NextArea(1), NextArea(2));

                    } else
                    {

                        simManager.RemoveAdviceAtArea(lastArea);
                        simManager.remainingPath.Pop();
                    }
                }

                if (simManager.GetAdviceName() == SimulationManager.AdviceName.ARROW)
                {
                    position = AdviceBasePosition(NextArea(0)) + AdvicePositionOffset(currentArea, NextArea(0), NextArea(1), true);
                    rotation = AdviceRotation(currentArea, NextArea(0), NextArea(1), true);
                    AddArrow(NextArea(0), position, rotation);
                } else if (simManager.GetAdviceName() == SimulationManager.AdviceName.PEANUT)
                {
                    SetCompanionState(0);
                    object[] parms = new object[3] { currentArea, NextArea(0), NextArea(1) };
                    StopCoroutine(nameof(MoveCompanion));
                    StartCoroutine(nameof(MoveCompanion), parms);
                }


            }
            walkedPath.Add(currentArea);
        }
        
     
    }

    private IEnumerator MoveCompanion(object[] parms)
    {
        Area currentArea = (Area)parms[0];
        Area nextArea = (Area)parms[1];
        Area nextNextArea = (Area)parms[2];
        Vector3 initPos = simManager.Peanut.transform.position;
        Vector3 initRot = simManager.Peanut.transform.rotation.eulerAngles;
        Vector3 finalPos = AdviceBasePosition(nextArea);
        Vector3 finalRot = AdviceRotation(currentArea, nextArea, nextNextArea, true);
        if (nextNextArea != null)
        {
            finalPos += AdvicePositionOffset(currentArea, nextArea, nextNextArea, true);
        } else
        {
            finalRot.y -= simManager.AdviceConfig.AdviceRotationY[5];
        }

        int vertexCount = 80;
        Vector3 medRot = new Vector3(initRot.x, initRot.y, initRot.z);
        if (nextNextArea != null)
        {
            switch (GetAction(currentArea, nextArea, nextNextArea))
            {
                case Action.TURN_LEFT: medRot.y += -90f; break;
                case Action.TURN_RIGHT: medRot.y += 90f; break;
                case Action.GO_FORWARD: medRot.y += 180f; break;
            }
        } else
        {
            medRot.y += 180f;
        }
        Quaternion r0 = simManager.Peanut.transform.rotation;
        simManager.Peanut.transform.LookAt(Converter.AreaToVector3(nextArea, simManager.Peanut.transform.position.y));
        Quaternion r1 = simManager.Peanut.transform.rotation;
        simManager.Peanut.transform.rotation = r0;
        float turnSpeed = 0f;
        float turnSpeedChange = 0.1f;
        //angle we need to turn
        float angleToTurn;
        while (true)
        {
            angleToTurn = Quaternion.Angle(simManager.Peanut.transform.rotation, r1); 
            if (angleToTurn < 5f)
            {
                break;
            }
            turnSpeed = Mathf.Min(angleToTurn, turnSpeed + turnSpeedChange);
            simManager.Peanut.transform.rotation = Quaternion.Lerp(simManager.Peanut.transform.rotation, r1, Mathf.Clamp01(angleToTurn > 0 ? turnSpeed / angleToTurn : 0f));
  

            yield return new WaitForSeconds(0.01f);
        }
        
       

        for (float ratio = 0; ratio <= 1; ratio += 1f / vertexCount)
        {
            simManager.Peanut.transform.position = initPos + (finalPos - initPos) * ratio;
            yield return new WaitForSeconds(0.001f);
        }

        if (nextNextArea == null)
        {
            SetCompanionState(2);
        } else
        {
            int direction = 1;
            if (GetAction(currentArea, nextArea, nextNextArea) == Action.TURN_LEFT)
            {
                direction = -1;
            }
            SetCompanionState(direction);
        }

        while (true)
        {
            angleToTurn = Quaternion.Angle(simManager.Peanut.transform.rotation, Quaternion.Euler(finalRot));
            if (angleToTurn < 5f)
            {
                break;
            }
            turnSpeed = Mathf.Min(angleToTurn, turnSpeed + turnSpeedChange);
            simManager.Peanut.transform.rotation = Quaternion.Lerp(simManager.Peanut.transform.rotation, Quaternion.Euler(finalRot), Mathf.Clamp01(angleToTurn > 0 ? turnSpeed / angleToTurn : 0f));

            yield return new WaitForSeconds(0.01f);
        }

        yield return null;
    }

    private IEnumerator MoveCompanion2(object[] parms)
    {
        Area currentArea = (Area)parms[0];
        Area nextArea = (Area)parms[1];
        Area nextNextArea = (Area)parms[2];
        Vector3 initPos = simManager.Peanut.transform.position;
        Vector3 initRot = simManager.Peanut.transform.rotation.eulerAngles;
        Vector3 finalPos = AdviceBasePosition(nextArea) + AdvicePositionOffset(currentArea, nextArea, nextNextArea, true);
        Vector3 finalRot = AdviceRotation(currentArea, nextArea, nextNextArea, true);
        int vertexCount = 80;
        Vector3 medRot = new Vector3(initRot.x, initRot.y, initRot.z);
        if (nextNextArea != null)
        {
            switch (GetAction(currentArea, nextArea, nextNextArea))
            {
                case Action.TURN_LEFT: medRot.y -= 90f; break;
                case Action.TURN_RIGHT: medRot.y += 90f; break;
                case Action.GO_FORWARD: medRot.y += 180f; break;
            }
        }

        Quaternion r0 = simManager.Peanut.transform.rotation;
        simManager.Peanut.transform.LookAt(Converter.AreaToVector3(nextArea, simManager.Peanut.transform.position.y));
        Quaternion r1 = simManager.Peanut.transform.rotation;
        simManager.Peanut.transform.rotation = r0;
        float turnSpeed = 0f;
        float turnSpeedChange = 0.1f;
        //angle we need to turn
        float angleToTurn;
        while (true)
        {
            angleToTurn = Quaternion.Angle(simManager.Peanut.transform.rotation, r1);
            if (angleToTurn < 5f)
            {
                break;
            }
            turnSpeed = Mathf.Min(angleToTurn, turnSpeed + turnSpeedChange);
            simManager.Peanut.transform.rotation = Quaternion.Lerp(simManager.Peanut.transform.rotation, r1, Mathf.Clamp01(angleToTurn > 0 ? turnSpeed / angleToTurn : 0f));


            yield return new WaitForSeconds(0.01f);
        }

        for (float ratio = 0; ratio <= 1; ratio += 1f / vertexCount)
        {
            simManager.Peanut.transform.position = initPos + (finalPos - initPos) * ratio;
            yield return new WaitForSeconds(0.001f);
        }

        object[] parms2 = new object[3] { nextArea, nextNextArea, NextArea(3) };
        StartCoroutine(nameof(MoveCompanion), parms2);
        yield return null;
    }

    private void SetCompanionState(int state)
    {
        Animator faceAnimator = simManager.Peanut.transform.Find("Armature").Find("Bone").Find("Bone.001").Find("Face").GetComponent<Animator>();
        if (state == 0)
        {
            faceAnimator.SetInteger("State", 0);
        } else if (state == 2)
        {
            faceAnimator.SetInteger("State", 2);
        } else
        {
            int n = 2; // proba = 1/n
            int r = Random.Range(1, n+1);
            if (r == n)
            {
                faceAnimator.SetInteger("State", 1);
            } else
            {
                faceAnimator.SetInteger("State", 0);
            }
        }

        simManager.Peanut.GetComponent<Animator>().SetInteger("State", state);
    }

    private void RemoveLastAdvice(Area lastArea)
    {
        if (simManager.GetAdviceName() == SimulationManager.AdviceName.ARROW || simManager.GetAdviceName() == SimulationManager.AdviceName.PEANUT)
        {
            simManager.RemoveAdviceAtArea(lastArea);
        } else if (simManager.GetAdviceName() == SimulationManager.AdviceName.LIGHT)
        {
            simManager.RemoveLightAdvice();
        }
    }

    private Vector3 AdviceBasePosition(Area area)
    {
        return Converter.AreaToVector3(area, simManager.AdviceConfig.AdviceBaseHeight);
    }

    private void AddWrongWayAdvice(Area area, Vector3 position, Vector3 rotation)
    {
        GameObject wrongWayAdvicePrefab = simManager.AdviceConfig.WrongWayAdvicePrefab;
        Advice advice = new Advice(area, wrongWayAdvicePrefab, position, rotation);
        simManager.AddAdvice(advice);
    }

    private Vector3 WrongWayAdviceRotation(Area currentArea, Area lastMatchingArea)
    {
        return AdviceRotation(lastMatchingArea, currentArea, null, false);
    }

    private Vector3 WrongWayAdvicePositionOffset(Area currentArea, Area lastMatchingArea)
    {
        return AdvicePositionOffset(currentArea, lastMatchingArea, null, false);
    }

    private void AddArrow(Area area, Vector3 position, Vector3 rotation)
    {
        GameObject advicePrefab = simManager.AdviceConfig.AdvicePrefab;
        Advice advice = new Advice(area, advicePrefab, position, rotation);
        simManager.AddAdvice(advice);
    }

    private void InstantiateCompanion(Vector3 position, Quaternion rotation)
    {
        simManager.Peanut = Instantiate(simManager.AdviceConfig.AdvicePrefab, position, rotation);
    }

    private Direction GetDirection(Area from, Area to)
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

    public Vector3 AdviceRotation(Area from, Area at, Area to, bool onRightPath)
    {
        Direction d1 = GetDirection(from, at);
        Direction d2;
        Action action;
        Vector3 r;
        if (to != null)
        {
            r = simManager.AdviceConfig.AdvicePrefab.transform.rotation.eulerAngles;
            d2 = GetDirection(at, to);
            action = GetAction(d1, d2);
        } else
        {
            if (onRightPath)
            {
                r = simManager.AdviceConfig.AdvicePrefab.transform.rotation.eulerAngles;
                action = Action.GO_FORWARD;
            } else
            {
                r = simManager.AdviceConfig.WrongWayAdvicePrefab.transform.rotation.eulerAngles;
                action = Action.GO_BACKWARD;
            }
        }
        if (d1 == Direction.LEFT)
        {
            r.y += simManager.AdviceConfig.AdviceRotationY[0]; 
        } else if (d1 == Direction.RIGHT)
        {
            r.y += simManager.AdviceConfig.AdviceRotationY[1];
        }
        else if (d1 == Direction.DOWN)
        {
            r.y += simManager.AdviceConfig.AdviceRotationY[2];
        }
        if (action == Action.TURN_LEFT)
        {
            r.y += simManager.AdviceConfig.AdviceRotationY[3];
        }
        else if (action == Action.TURN_RIGHT)
        {
            r.y += simManager.AdviceConfig.AdviceRotationY[4];
        }
        else if (action == Action.GO_FORWARD)
        {
            r.y += simManager.AdviceConfig.AdviceRotationY[5];
        } else
        {
            r.y += simManager.AdviceConfig.AdviceRotationY[6];
        }
        r.x += simManager.AdviceConfig.AdviceRotationX;
        if (!from.InBigArea() && at.InBigArea() && to != null)
        {// ERREUR POUR BIG AREA

            if (Mathf.Abs(at.line - to.line) + Mathf.Abs(at.column - to.column) == 2)
            {
                if (at.Equals(new Area(3, 3)) || at.Equals(new Area(2, 4)))
                    r.y += 45f;
                if (at.Equals(new Area(2, 3)) || at.Equals(new Area(3, 3)))
                    r.y -= 45f;
            }
        }
        return r;
    }

    public Vector3 AdvicePositionOffset(Area from, Area at, Area to, bool onRightPath)
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
            if (onRightPath)
            {
                action = Action.GO_FORWARD;
            } else
            {
                action = Action.GO_BACKWARD;
            }
        }
        float baseOffset = simManager.AdviceBaseOffset;
      
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
