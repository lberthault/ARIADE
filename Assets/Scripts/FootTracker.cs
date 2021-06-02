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

    public Foot foot;

    private SimulationManager simManager;

    //Data
    private string dataFileName;
    private List<DataSnapshot> data;
    private List<Step> steps;
    public List<Step> Steps
    {
        get { return steps; }
        set { steps = value; }
    }

    //Trail
    private TrailRenderer trailRenderer;
    public Material footTrailMaterial;
    public float trailTime = 9999f;
    public float trailSize = 0.05f;
    public float trailSensibility = 0.01f;

    Vector3 lastPosition;
    public float bigJumpThreshold = 5f;

    public Step LastStep
    {
        get
        {
            if (StepCount != 0)
            {
                return steps[StepCount - 1];
            }
            else
            {
                return null;
            };
        }
    }

    public int StepCount
    {
        get
        {
            return steps.Count;
        }
    }

    public string Name
    {
        get
        {
            return foot + " Foot";
        }
    }

    private void Awake()
    {
        steps = new List<Step>();
        simManager = GameObject.Find("SimulationManager").GetComponent<SimulationManager>();
        dataFileName = foot + "FootData";
        data = new List<DataSnapshot>();
        if (simManager.Mode == SimulationManager.DEBUG_MODE)
        {
            SetupTrailRenderer();
        }
    }

    /* Adds a trail renderer to the foot to visually follow its trajectory */
    private void SetupTrailRenderer()
    {
        trailRenderer = (TrailRenderer)gameObject.AddComponent(typeof(TrailRenderer));
        trailRenderer.startWidth = trailSize;
        trailRenderer.endWidth = trailSize;
        trailRenderer.time = trailTime;
        trailRenderer.minVertexDistance = trailSensibility;
        trailRenderer.material = footTrailMaterial;
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, lastPosition) > bigJumpThreshold)
        {
            if (trailRenderer != null) trailRenderer.Clear();
        }
        lastPosition = transform.position;
    }

    public int Track(float simTime)
    {
        if (trailRenderer != null)
        {
            CheckTrailRendererModifications();
        }
        RegisterData(simTime);
        WriteData(simTime);
        int res = UpdateSteps(simTime);
        return res;
    }

    private void WriteData(float simTime)
    {
        DataManager.WriteDataInFile(dataFileName, simManager.GetNavConfig(), simTime + "=" + VerticalAcceleration(DataCount()-2));
        //DataManager.WriteDataInFile(dataFileName, simManager.GetNavConfig(), "t = " + simTime + " : pos = " + transform.position + " rot = " + transform.rotation.eulerAngles + " mSpeed = " + Converter.Round(MeanSpeed(), 2));
        //DataManager.WriteDataInFile(dataFileName, simManager.GetNavConfig(), "   steps = " + StepCount + " : mLength = " + Converter.Round(TotalMeanStepLength(), 2) + " mTime = " + Converter.Round(TotalMeanStepTime(), 2) + " mPause = " + Converter.Round(TotalMeanPause(), 2));
    }

    //bool startStep = false;
    //List<float> tt = new List<float>();

    /* Analyses data to determine the step state */
    private int UpdateSteps(float simTime)
    {
        if (LastStep == null || (LastStep != null && LastStep.isFinished()))
        {
            if (GroundedCriterion())
            {
                // Stays grounded
            } else
            {
                // Has just left floor
                Step step = new Step(foot, simTime, -1, false);
                steps.Add(step);
                return STEP_START;
            }
        } else
        {
            if (GroundedCriterion())
            {
                // Has just landed
                Step step = LastStep;
                step.End = simTime;
                step.setFinished(true);
                return STEP_END;
            } else
            {
                // Stays in the air
            }
        }
        return NO_CHANGE;
        /*
        float v1 = VerticalAcceleration(DataCount() - 4);
        float v2 = VerticalAcceleration(DataCount() - 3);
        float v3 = VerticalAcceleration(DataCount() - 2);
        float threshold = 0.05f;
        if (!startStep)
        {
            if (v3 > v2 && v2 > v1)
            {
                if (v1 > threshold)
                {
                    Debug.Log("Start Peak");
                    startStep = true;
                    tt.Add(DataCount() - 4);
                    Step step = new Step(foot, simTime, -1, false);
                    steps.Add(step);
                    return STEP_START;
                }
            }
        } else
        {
            if (v3 > threshold)
            {
                tt.Add(DataCount() - 2);
            } else
            {
                if (v3 > v2)
                {
                    Debug.Log("End Peak");
                    startStep = false;
                    Step step = LastStep;
                    step.End = simTime;
                    step.setFinished(true);
                    return STEP_END;
                }
            }
        }
        return NO_CHANGE;*/
    }


    /* Updates trail renderer parameters if they are edited during the simulation */
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
        if (trailRenderer.material != footTrailMaterial)
        {
            trailRenderer.material = footTrailMaterial;
        }
    }
   
    public void RegisterData(DataSnapshot footData)
    {
        data.Add(footData);
    }

    public void RegisterData(float time, Vector3 position, Vector3 rotation)
    {
        data.Add(new DataSnapshot(time, position, rotation));
    }

    public void RegisterData(float time)
    {
        data.Add(new DataSnapshot(time, transform.position, transform.rotation.eulerAngles));
    }

    public DataSnapshot LastDataSnapshot()
    {
        if (DataCount() > 0)
        {
            return data[DataCount() - 1];
        } else
        {
            return null;
        }
    }

    public bool IsInAir()
    {
        if (LastStep == null)
        {
            return false;
        }
        else
        {
            return !LastStep.isFinished();
        }
    }

    public int DataCount()
    {
        return data.Count;
    }
 
    public DataSnapshot GetDataAtIndex(int i)
    {
        return data[i];
    }

    public DataSnapshot GetDataAtTime(float t)
    {
        return data[Converter.TimeToIndex(data, t)];
    }

    public Vector3 ProjectOnFloor(Vector3 v)
    {
        return new Vector3(v.x, 0, v.z);
    }

    public float DistanceTravelled()
    {
        if (DataCount() == 0)
        {
            return 0;
        }
        return GetDistance(0, DataCount()-1);
    }

    public float GetDistance(int i, int j)
    {
        return Vector3.Distance(ProjectOnFloor(data[i].Position), ProjectOnFloor(data[j].Position));
    }

    public float GetDistance(float t0, float tf)
    {
        return GetDistance(Converter.TimeToIndex(data, t0), Converter.TimeToIndex(data, tf));
    }

    public void ResetData()
    {
        data.Clear();
        steps.Clear();
    }

    public float GetTime(int i, int j)
    {
        return data[j].Time - data[i].Time;
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

    public float VerticalSpeed(float t)
    {
        return VerticalSpeed(Converter.TimeToIndex(data, t));
    }

    public float VerticalSpeed(int i)
    {
        if (i <= 0 || i >= data.Count)
        {
            return -1;
        }
        float d = data[i].Position.y - data[i - 1].Position.y;
        float t = data[i].Time - data[i - 1].Time;
        float v = d / t;
        if (v > 100f)
        {
            v = -1;
        }
        return Mathf.Abs(v);
    }

    public float VerticalAcceleration(float t)
    {
        return VerticalAcceleration(Converter.TimeToIndex(data, t));
    }
    public float VerticalAcceleration(int i)
    {
        if (i <= 0 || i >= data.Count - 1)
        {
            return -1;
        }
        if (VerticalSpeed(i) == -1 || VerticalSpeed(i + 1) == -1)
        {
            return -1;
        }
        else
        {

            float v = VerticalSpeed(i + 1) - VerticalSpeed(i);
            float t = data[i + 1].Time - data[i - 1].Time;
            return Mathf.Abs(v / t);
        }
    }

    private bool GroundedCriterion()
    {
        return transform.position.y < 0.01f;
    }

    public float StepTime(int i)
    {
        Step step = steps[i];
        return step.Duration;
    }

    public float StepLength(int i)
    {
        Step step = steps[i];
        return GetDistance(step.Start, step.End);
    }

    public float MeanStepLength(int i, int j)
    {
        float res = 0;
        int N = j - i;
        if (N == 0)
        {
            return -1;
        }
        for (int k = i; k < j; k++)
        {
            res += StepLength(k);
        }
        return res / N;
    }

    public float TotalMeanStepLength()
    {
        return MeanStepLength(0, steps.Count);

    }

    public float MeanStepTime(int i, int j)
    {
        float res = 0;
        int N = j-i;
        if (N == 0)
        {
            return -1;
        }
        for (int k = i; k < j; k++)
        {
            res += StepTime(k);
        }
        return res / N;
    }

    public float TotalMeanStepTime()
    {
        return MeanStepTime(0, steps.Count);

    }

    public float PauseAfterStep(int i)
    {
        return data[Converter.TimeToIndex(data, steps[i + 1].Start)].Time - data[Converter.TimeToIndex(data, steps[i].End)].Time;
    }

    public float MeanPause(int i, int j)
    {
        float res = 0;
        int N = j - i;
        if (N == 0)
        {
            return -1;
        }
        for (int k = i; k < j; k++)
        {
            res += PauseAfterStep(k);
        }
        return res / N;
    }

    public float TotalMeanPause()
    {
        return MeanPause(0, steps.Count - 1);

    }

}
