using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FootTracker : MonoBehaviour
{

    public const int NO_CHANGE = 0;
    public const int STEP_START = 1;
    public const int STEP_END = 2;
    public const int STEP_REMOVE = 3;

    public Foot Foot;

    private GameManager gm;

    public string Name
    {
        get
        {
            return Foot + " Foot";
        }
    }

    //Data
    private string dataFileName;
    private List<DataSnapshot> _data;
    public List<DataSnapshot> Data
    {
        get { return _data; }
    }
    public DataSnapshot LastDataSnapshot
    {
        get
        {
            if (DataCount > 0)
            {
                return _data[DataCount - 1];
            }
            else
            {
                return null;
            };
        }
    }
    private List<Step> _steps;
    public List<Step> Steps
    {
        get { return _steps; }
        set { _steps = value; }
    }
    public int DataCount
    {
        get { return _data.Count; }
    }
    private Accelerometer accelerometer;
    private float magnitude;

    //Trail
    private Trail trail;

    private Vector3 lastPosition;

    //Step
    private bool stepping = false;
    private float stepDelay = 0f;
    private float endStepDetectionThreshold = 0.2f; //0.2f
    private float highMagnitudeThreshold = 0.2f; //1f
    private float lowMagnitudeThreshold = 0.1f; //0.3f

    public Step LastStep
    {
        get
        {
            return (StepCount != 0) ? Steps[StepCount - 1] : null;
        }
    }

    public Step LastFinishedStep
    {
        get
        {
            if (LastStep == null)
                return null;
            return LastStep.IsFinished() ? LastStep : Steps[StepCount - 2];
        }
    }

    public int StepCount
    {
        get
        {
            return _steps.Count;
        }
    }

    private void Awake()
    {
        trail = GetComponent<Trail>();
        accelerometer = GetComponent<Accelerometer>();
        _steps = new List<Step>();
        dataFileName = Foot + "FootData";
        _data = new List<DataSnapshot>();
    }

    private void Start()
    {
        gm = GameManager.Instance;
        if (gm.drawDebug)
        {
            trail.Initiate();
        }
    }
  
    public int Track(float simTime)
    {
        if (trail.Initiated)
        {
            trail.CheckModifications();
        }

        Vector3 acceleration = accelerometer.LinearAcceleration(transform.position, Accelerometer.DEFAULT_SAMPLES);
        magnitude = acceleration.magnitude;

        RegisterData(simTime);
        WriteData(simTime);
        int res = UpdateSteps(simTime);

        //Clear trail when trial ended
        if (Vector3.Distance(transform.position, lastPosition) > 5f)
        {
            if (trail.Initiated) trail.Clear();
        }
        lastPosition = transform.position;

        return res;
    }

    private void WriteData(float simTime)
    {
        string header = "Simulation time (s);" +
            "Position vector;" +
            ";" +
            ";" +
            "Euler rotation vector;" +
            ";" +
            ";" +
            "Magnitude (m/s²);" +
            "Mean speed (m/s);" +
            "Nb Steps;" +
            "Mean step length (m);" +
            "Mean step time (s)";
        string data = simTime + ";"
                + transform.position.x + ";"
                + transform.position.y + ";"
                + transform.position.z + ";"
                + transform.rotation.eulerAngles.x + ";"
                + transform.rotation.eulerAngles.y + ";"
                + transform.rotation.eulerAngles.z + ";"
                + magnitude + ";"
                + DataAnalyzer.MeanSpeed(this._data) + ";"
                + StepCount + ";"
                + DataAnalyzer.MeanStepLength(_steps) + ";"
                + DataAnalyzer.MeanStepTime(_steps);
        DataWriter.WriteDataSingleLine(dataFileName, header, data, false);
    }

    float hesitationDistanceThreshold = 0.03f;
    float hesitationDelayThreshold = 1f;
    float hesitationDistance = 0f;
    float hesitationDelay = 0f;
    bool hesitating = false;

    Vector3 lastEndPosition;
    /* Analyze data to determine current step state */
    private int UpdateSteps(float simTime)
    {
        /* HESITATION DETECTION */
        if (Vector3.Angle(-transform.forward, transform.position - lastPosition) > 100f)
        {
            if (!hesitating)
            {
                hesitating = true;
            }
            hesitationDelay = 0f;
            hesitationDistance += Vector3.Distance(transform.position, lastPosition);
            if (hesitationDistance >= hesitationDistanceThreshold)
            {
                hesitationDistance = 0f;
            }
        }
        else
        {
            if (hesitating)
            {
                hesitationDelay += Time.deltaTime;
            }
            if (hesitationDelay > hesitationDelayThreshold)
            {
                hesitating = false;
                hesitationDelay = 0f;
                hesitationDistance = 0f;
            }
        }

        /* STEP DETECTION */
        if (magnitude < lowMagnitudeThreshold)
        {
            stepDelay += Time.deltaTime;
            if (stepDelay > endStepDetectionThreshold && stepping)
            {
                Step step = LastStep;
                step.EndTime = simTime - stepDelay;
                step.EndPos = transform.position;
                lastEndPosition = transform.position;
                step.SetFinished(true);
                //Debug.Log(Foot + " : " + step.Length);
                stepDelay = 0f;
                stepping = false;
                if (step.Duration < 0.1f || step.Duration > 5f || step.Length < 0.01f || step.Length > 3f)
                {
                    Steps.Remove(step);
                }
                return STEP_END;
            } else
            {

            }
        } else
        {
            stepDelay = 0f;
            if (magnitude > highMagnitudeThreshold && !stepping)
            {
                //Step step = new Step(Foot, simTime, -1, transform.position, false);
                Vector3 pos;
                if (lastEndPosition == Vector3.zero)
                {
                    pos = lastPosition;
                } else
                {
                    pos = lastEndPosition;
                }
                Step step = new Step(Foot, simTime, -1, pos, false);
                _steps.Add(step);
                stepping = true;
                return STEP_START;
            }
        }
        return NO_CHANGE;
    }

    public void RegisterData(float time)
    {
        _data.Add(new DataSnapshot(time, transform.position, transform.rotation.eulerAngles));
    }

    public void ResetData()
    {
        _data.Clear();
        _steps.Clear();
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
}
