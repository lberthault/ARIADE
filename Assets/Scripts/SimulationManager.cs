using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using QTMRealTimeSDK;
using QualisysRealTime.Unity;

public class SimulationManager : MonoBehaviour
{


    // Simulation
    public float SimTime { get; set; }
    private NavConfig navConfig;
    [SerializeField]
    private string participantName;
    private int trialState;
    public int TrialState
    {
        get { return trialState; }
    }
    public const int TRIAL_NOT_STARTED = 0;
    public const int TRIAL_ONGOING = 1;
    public const int TRIAL_ENDED = 2;
    // Debug
    public const int DEBUG_MODE = 0;
    public const int XP_MODE = 1;
    [SerializeField]
    private bool debugMode;
    public int Mode
    {
        get { return debugMode ? DEBUG_MODE : XP_MODE; }
    }
    [SerializeField]
    private List<GameObject> debugObjects;
    [SerializeField]
    private Material pathLineMaterial;
    [SerializeField]
    private float pathLineHeight = 0.7f;
    [SerializeField]
    private float pathLineWidth = 0.05f;
    [SerializeField]
    private float debugMovementSpeed;
    [SerializeField]
    private float debugMouseSensitivity;
    // Path
    public enum PathName
    {
        A,
        B,
        C,
        T
    }
    public static Path pathA = new Path("A16.15.25.99.43.42.32.22.21.20");
    public static Path pathB = new Path("B01.11.12.13.99.32.42.43.44.54");
    public static Path pathC = new Path("C55.45.35.99.13.12.11.21.31.30");
    public static Path pathT = new Path("T51.41.42.52");

    [SerializeField]
    private PathName pathName;
    public Path trialPath;
    public Path remainingPath;
    // Advice
    public enum AdviceName
    {
        ARROW,
        LIGHT,
        PEANUT
    };

    public enum AdviceConfigName
    {
        ARROW_AIR,
        ARROW_GROUND
    }

    [SerializeField]
    private AdviceName advice;

    [SerializeField]
    private AdviceConfigName adviceConfigName;
    public AdviceConfig AdviceConfig
    {
        get
        {
            switch (adviceConfigName)
            {
                case (AdviceConfigName.ARROW_AIR):
                    return ARROW_AIR;
                case (AdviceConfigName.ARROW_GROUND):
                    return ARROW_GROUND;
                default: return ARROW_AIR;
            };
        }
    }

    public AdviceConfig ARROW_AIR, ARROW_GROUND;

    [SerializeField]
    public GameObject arrowPrefab, lightPrefab, peanutPrefab;
    [SerializeField]
    public GameObject arrowWrongWayPrefab, lightWrongWayPrefab, peanutWrongWayPrefab;

    List<Advice> visibleAdvice;

    public float AdviceBaseOffset
    {
        get { return AdviceConfig.AdviceBaseOffsetCoef * AreaDetectorSize; }
    }
    // Data
    private string dataFileName = "SimulationData";

    // GUI
    [SerializeField]
    private GameObject canvasGUI;
    [SerializeField]
    private GameObject participantGUI, pathGUI, adviceGUI, timeGUI, dataGUI, qualisysGUI, modeGUI, trialStateGUI;
    private TextMeshProUGUI participantText, pathText, adviceText, timeText, dataText, qualisysText, modeText, trialStateText;

    // QTM
    public float qtmPeriod = 0.1f;
    RTClient client;
    DiscoveryResponse? dr;
    private bool connectedToQTM;

    // Tracking
    public float trackingPeriod = 0.01f;
    private bool isTracking = false;

    [SerializeField]
    private GameObject feet;
    private FeetTracker feetTracker;
    [SerializeField]
    private GameObject hololens;
    private HololensTracker hololensTracker;

    // Area detector
    [SerializeField]
    private GameObject areaDetectorPrefab;
    public float AreaDetectorSize { get; set; }

    void Start()
    {
        InitiateAdviceConfig();
        InitiateVariables();
        InitiateComponents();
        InitiateGUI();
        SetTrialState(TRIAL_NOT_STARTED);

        foreach (GameObject o in debugObjects)
        {
            o.SetActive(debugMode);
        }

        if (debugMode)
        {
            DrawTrialPath();
            HololensController hc = hololens.AddComponent<HololensController>();
            hc.movementSpeed = debugMovementSpeed;
            hc.mouseSensitivity = debugMouseSensitivity;
        } else
        {
            InitQTMServer();
            StartCoroutine(nameof(CheckQTMConnection));
        }
    }

    private void InitiateAdviceConfig()
    {
        /* 
         * h : base height
         * c : base offset coef
         * rY : Y rotation
         * 0 = LEFT (should not change)
         * 1 = RIGHT (should not change)
         * 2 = DOWN (should not change)
         * 3 = TURN_LEFT
         * 4 = TURN_RIGHT
         * 5 = GO_FORWARD
         * 6 = GO_BACKWARD
         */
        float h = 0.8f;
        float c = 1.0f;
        List<float> rY = new List<float>() { -90f, +90f, +180f, -100f, +100f, +30f, +180f };
        float rX = 0f;
        ARROW_AIR = new AdviceConfig(arrowPrefab, arrowWrongWayPrefab, h, c, rY, rX);

        h = 0.3f;
        c = 0.5f;
        rY = new List<float>() { -90f, +90f, +180f, -90f, +90f, +30f, +180f };
        rX = 90f;
        ARROW_GROUND = new AdviceConfig(arrowPrefab, arrowWrongWayPrefab, h, c, rY, rX);
    }

    private void InitiateVariables()
    {
        SimTime = 0f;
        switch (pathName)
        {
            case PathName.A: trialPath = pathA; break;
            case PathName.B: trialPath = pathB; break;
            case PathName.C: trialPath = pathC; break;
            case PathName.T: trialPath = pathT; break;
        }
        navConfig = new NavConfig(participantName, trialPath, advice);
        client = RTClient.GetInstance();
        remainingPath = trialPath;
        visibleAdvice = new List<Advice>();

        AreaDetectorSize = areaDetectorPrefab.GetComponent<BoxCollider>().size.x;
        

    }

    private void InitiateGUI()
    {
        qualisysText.text = "Qualisys: Disconnected";
        pathText.text = "Path " + trialPath.Name;
        adviceText.text = "Advice: " + advice.ToString();
        participantText.text = "Participant: " + participantName;
        modeText.text = debugMode ? "Debug Mode" : "XP Mode";
        canvasGUI.SetActive(debugMode);
    }

    private void InitiateComponents()
    {
        feetTracker = feet.GetComponent<FeetTracker>();
        hololensTracker = hololens.GetComponent<HololensTracker>();
        qualisysText = qualisysGUI.GetComponent<TextMeshProUGUI>();
        dataText = dataGUI.GetComponent<TextMeshProUGUI>();
        timeText = timeGUI.GetComponent<TextMeshProUGUI>();
        pathText = pathGUI.GetComponent<TextMeshProUGUI>();
        adviceText = adviceGUI.GetComponent<TextMeshProUGUI>();
        participantText = participantGUI.GetComponent<TextMeshProUGUI>();
        modeText = modeGUI.GetComponent<TextMeshProUGUI>();
        trialStateText = trialStateGUI.GetComponent<TextMeshProUGUI>();
    }

    private void SetTrialState(int state)
    {
        trialState = state;
        switch (state)
        {
            case TRIAL_NOT_STARTED:
                trialStateText.text = "Trial not started"; break;
            case TRIAL_ONGOING:
                trialStateText.text = "Trial ongoing"; break;
            case TRIAL_ENDED:
                trialStateText.text = "Trial ended"; break;
        }
    }

    void Update()
    {
        if (isTracking)
        {
            SimTime += Time.deltaTime;
        } else if (trialState == TRIAL_ONGOING) {
            StartCoroutine(nameof(Track));
            isTracking = true;
        }
    }

    // Every time the coroutine starts, the trackers are told to acquire data and update text files
    private IEnumerator Track()
    {
        while (true)
        {
            feetTracker.Track(SimTime);
            hololensTracker.Track(SimTime);
            UpdateDataText();
            yield return new WaitForSeconds(trackingPeriod);
        }
    }

    private void OnApplicationQuit()
    {
        WriteData();
    }

    // Updates debug mode GUI
    private void UpdateDataText()
    {
        float prevAcc = Converter.Round(hololensTracker.PreviousAcceleration(), 2);
        dataText.text = "Steps= " + feetTracker.StepCount + 
            "\nDistance travelled= " + Converter.Round(hololensTracker.DistanceTravelled(), 2) + 
            "\nSpeed= " + Converter.Round(hololensTracker.CurrentSpeed(), 2) + 
            "\nPrev_Acceleration= " + prevAcc + " (" + (prevAcc >= 0 ? "+" : "-")  + ")";

        TimeSpan timeSpan = TimeSpan.FromSeconds(SimTime);
        string time = string.Format("Elapsed time: {0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        timeText.text = time;
    }

    // Checks connection with QTM localhost server
    private IEnumerator CheckQTMConnection()
    {
        while (true)
        {
            if (!connectedToQTM)
            {
                if (dr != null)
                {
                    if (connectedToQTM = ConnectToServer())
                    {
                        qualisysText.text = "Qualisys: Connected";
                        Debug.Log("Qualisys: Connected to server : " + dr.Value.HostName + " (" + dr.Value.IpAddress + ":" + dr.Value.Port + ")");
                    }
                } else
                {
                    InitQTMServer();
                }
            }
            yield return new WaitForSeconds(qtmPeriod);
        }
    }

    // Connects client to localhost server
    private bool ConnectToServer()
    {
        return client.Connect(dr.Value, dr.Value.Port, true, true, false, true, false, true);
    }

    // Looks fot the localhost discovery response
    private bool FetchLocalhostServer()
    {
        List<DiscoveryResponse> servers = client.GetServers();
        foreach (DiscoveryResponse res in servers)
        {
            if (res.HostName == "Localhost")
            {
                dr = res;
                break;
            }
        }

        return dr != null;
    }

    // Initiate QTM Server by connecting to localhost
    private void InitQTMServer()
    {
        if (FetchLocalhostServer())
        {
            if (connectedToQTM = ConnectToServer())
            {
                qualisysText.text = "Qualisys : Connected";
                Debug.Log("Qualisys: Connected to server: " + dr.Value.HostName + " (" + dr.Value.IpAddress + ":" + dr.Value.Port + ")");
            }
            else
            {
                qualisysText.text = "Qualisys : Disconnected";
                Debug.LogError("Qualisys: Failed to connect to localhost server");
            }
        }
        else
        {
            qualisysText.text = "Qualisys : Disconnected";
            Debug.Log("Qualisys: Failed to fetch localhost server");
        }
    }

    public bool IsConnectedToQTM()
    {
        return connectedToQTM;
    }

    public NavConfig GetNavConfig()
    {
        return navConfig;
    }

    public void StartTrial(Area startingArea)
    {
        while (!trialPath.Get(0).Equals(startingArea))
        {
            trialPath.Pop();
        }
        SetTrialState(TRIAL_ONGOING);
        ResetData();
    }

    private void ResetData()
    {
        feetTracker.ResetData();
        hololensTracker.ResetData();
        SimTime = 0f;
    }

    public void EndTrial()
    {
        SetTrialState(TRIAL_ENDED);
        StopCoroutine(nameof(Track));
        RemoveAllAdvice();
        WriteData();
        isTracking = false;
    }

    // The data written in the synthetical file at the end of the trial
    public void WriteData()
    {
        DataManager.WriteDataInFile(dataFileName, navConfig, "Simulation time : " + Converter.Round(SimTime, 2) + "s");
        DataManager.WriteDataInFile(dataFileName, navConfig, "Distance travelled : " + Converter.Round(hololensTracker.DistanceTravelled(), 2) + " m");
        DataManager.WriteDataInFile(dataFileName, navConfig, "Mean speed : " + Converter.Round(hololensTracker.MeanSpeed(), 2) + " m/s");
        DataManager.WriteDataInFile(dataFileName, navConfig, "Number of steps : " + feetTracker.StepCount);
        DataManager.WriteDataInFile(dataFileName, navConfig, "Mean step frequency : " + Converter.Round(feetTracker.MeanStepFrequency(), 2) + " step/s");
        DataManager.WriteDataInFile(dataFileName, navConfig, "Front foot : " + Converter.Round(feetTracker.LeftFootInFrontRate() * 100f, 0) + "% left");
        DataManager.WriteDataInFile(dataFileName, navConfig, "Walked path : " + hololensTracker.walkedPath);
        DataManager.WriteDataInFile(dataFileName, navConfig, "Number of areas covered : " + hololensTracker.walkedPath.Count());
        DataManager.WriteDataInFile(dataFileName, navConfig, "Average time in area : " + Converter.Round(hololensTracker.AverageTimeInArea(), 2) + " s");
        DataManager.WriteDataInFile(dataFileName, navConfig, "Number of errors : " + hololensTracker.NumberOfErrors());
        DataManager.WriteDataInFile(dataFileName, navConfig, "Total wrong areas : " + hololensTracker.TotalWrongAreas());
        DataManager.WriteDataInFile(dataFileName, navConfig, "Total error time : " + Converter.Round(hololensTracker.TotalErrorTime(), 2) + " s");
        DataManager.WriteDataInFile(dataFileName, navConfig, "Total error distance : " + Converter.Round(hololensTracker.TotalErrorDistance(), 2) + " m");
    }

    public void AddAdvice(Advice advice)
    {
        visibleAdvice.Add(advice);
    }

    public void RemoveAdviceAtArea(Area area)
    {
        foreach (Advice advice in visibleAdvice)
        {
            if (advice.Area.Equals(area))
            {
                advice.Remove();
            }
        }
    }

    public void RemoveAllAdvice()
    {
        foreach (Advice advice in visibleAdvice)
        {
            advice.Remove();
        }
    }

    /* Draws the theoretical path the participant has to follow during the xp */
    private void DrawTrialPath()
    {
        GameObject pathLine = new GameObject();
        pathLine.name = "PathLine";
        pathLine.transform.position = Converter.AreaToVector3(trialPath.Get(0), pathLineHeight);
        pathLine.AddComponent<LineRenderer>();
        LineRenderer lr = pathLine.GetComponent<LineRenderer>();
        lr.material = pathLineMaterial;
        lr.startColor = Color.red;
        lr.endColor = Color.red;
        lr.startWidth = pathLineWidth;
        lr.endWidth = pathLineWidth;
        lr.numCornerVertices = 1;
        lr.positionCount = trialPath.Count();
        for (int i = 0; i < trialPath.Count(); i++)
        {
            lr.SetPosition(i, Converter.AreaToVector3(trialPath.Get(i), pathLineHeight));
        }
    }

    public static int DistanceBetweenAreas(Area a1, Area a2)
    {
        return Mathf.Abs(a2.column - a1.column) + Mathf.Abs(a2.line - a1.line);
    }

}