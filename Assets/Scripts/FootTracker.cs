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
    private float highMagnitudeThreshold = 0.8f; //1f
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
            "Mean step time (s);" +
            "Mean step pause (s)";
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
                + DataAnalyzer.MeanStepTime(_steps) + ";"
                + DataAnalyzer.MeanNetPause(_steps);
        DataWriter.WriteData(dataFileName, header, data, false);
    }

    float d = 0f;
    float dt = 0f;
    bool hesitating = false;
    /* Analyze data to determine current step state */
    private int UpdateSteps(float simTime)
    {
        if (magnitude < lowMagnitudeThreshold)
        {
            stepDelay += Time.deltaTime;
            if (stepDelay > endStepDetectionThreshold && stepping)
            {
                Step step = LastStep;
                step.EndTime = simTime - stepDelay;
                step.EndPos = transform.position;
                step.SetFinished(true);
                //Debug.Log(Foot + " : " + step.Length);
                stepDelay = 0f;
                stepping = false;
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
                Step step = new Step(Foot, simTime, -1, lastPosition, false);
                _steps.Add(step);
                stepping = true;
                return STEP_START;
            }
        }
        if (Vector3.Angle(-transform.forward, transform.position - lastPosition) > 100f)
        {
            if (!hesitating)
            {
                hesitating = true;
            }
            dt = 0f;
            d += Vector3.Distance(transform.position, lastPosition);
            if (d >= 0.03f)
            {
                Debug.Log("BACK");
                d = 0f;
            }
        } else
        {
            if (hesitating)
            {
                dt += Time.deltaTime;
            }
            if (dt > 1f)
            {
                hesitating = false;
                dt = 0f;
                d = 0f;
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

}
