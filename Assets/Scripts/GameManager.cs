using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTMRealTimeSDK;
using QualisysRealTime.Unity;
using UnityEditor;

public class GameManager : MonoBehaviour
{

    private static GameManager _instance;
    public static GameManager Instance { get { return _instance; } }

    public float SimTime { get; set; }
    private TrialConfig _trialConfig;
    public TrialConfig TrialConfig
    {
        get { return _trialConfig; }
    }

    private int _trialState;
    public int TrialState
    {
        get { return _trialState; }
    }

    public const int TRIAL_NOT_STARTED = 0;
    public const int TRIAL_ONGOING = 1;
    public const int TRIAL_ENDED = 2;

   

    private Path trialPath;
    private Path _remainingPath;
    public Path RemainingPath
    {
        get { return _remainingPath; }
    }
    public enum AdviceConfigName
    {
        ARROW_AIR,
        ARROW_GROUND,
        LIGHT,
        PEANUT
    }


    public AdviceConfig AdviceConfig
    {
        get
        {
            switch (_adviceConfigName)
            {
                case (AdviceConfigName.ARROW_AIR):
                    return ARROW_AIR;
                case (AdviceConfigName.ARROW_GROUND):
                    return ARROW_GROUND;
                case (AdviceConfigName.LIGHT):
                    return LIGHT;
                case (AdviceConfigName.PEANUT):
                    return PEANUT;
                default: return ARROW_AIR;
            };
        }
    }

    private AdviceConfig ARROW_AIR, ARROW_GROUND, LIGHT, PEANUT;

    public float AdviceBaseOffset
    {
        get { return AdviceConfig.AdviceBaseOffsetCoef * AreaDetectorSize; }
    }

    [HideInInspector]
    public float lightPathDelayInSeconds = 0.002f;
    private string dataFileName = "SimulationData";
    private QualisysModule qtm;
    private bool isTracking = false;
    private FeetTracker feetTracker;
    private HololensTracker hololensTracker;
    private HololensCore hololensCore;

    public float AreaDetectorSize { get; set; }



    /* Editor */

    [Header("Trial Configuration")]
    [Space(10)]
    [SerializeField]
    private string participantName;

    [SerializeField]
    private Advice _advice;
    public Advice Advice
    {
        get { return _advice; }
    }

    public Path.PathName pathName;
    [SerializeField]
    private AdviceConfigName _adviceConfigName;

    [Header("Tracking Objects")]
    [Space(10)]
    public GameObject hololens;
    public GameObject feet;

    [Header("Qualisys")]
    [Space(10)]
    public bool enableQualisys;
    [SerializeField]
    [Range(0.1f, 3f)]
    private float moveSpeed = 1f;
    [SerializeField]
    [Range(0.5f, 3f)]
    private float mouseSensitivity = 2f;


    [Header("Invisible Objects")]
    [Space(10)]
    public bool hideInvisibleObjects;
    [SerializeField]
    public Material invisibleMaterial;
    [SerializeField]
    private List<GameObject> invisibleObjects;

    [Header("Occlusion")]
    [Space(10)]
    public bool setOcclusion;
    [SerializeField]
    private List<GameObject> obscureObjects;
    [SerializeField]
    private List<GameObject> obscurableObjects;

    [Header("Light Path")]
    [Space(10)]
    [Range(0.01f, 1f)]
    public float lightPathWidth = 0.5f;

    [Header("Landmarks")]
    [Space(10)]
    [Range(0f, 2f)]
    public float landmarkFadeOutDuration = 0.6f;




    [Header("Debug")]
    [Space(10)]
    public bool drawDebug;
    [SerializeField]
    private Material pathLineMaterial;
    [SerializeField]
    [Range(0f, 2f)]
    private float pathLineHeight = 0.7f;
    [SerializeField]
    [Range(0.01f, 0.1f)]
    private float pathLineWidth = 0.05f;





    [Header("Prefabs")]
    [Space(10)]
    public GameObject arrowPrefab;
    public GameObject peanutPrefab;
    public GameObject wrongWayAdvicePrefab;
    public GameObject stepMarkerPrefab;
    public GameObject areaDetectorPrefab;



    [Header("Materials")]
    [Space(10)]
    public Material lightPathMaterial;
    public Material lightPathWrongWayMaterial;




   

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.Log("Can't have multiple instances of Game Manager class");
        }
        else
        {
            _instance = this;
        }
    }

    void Start()
    {
        InitiateAdviceConfig();
        InitiateVariables();
        InitiateComponents();

        foreach (Area area in Area.BigAreaAreas())
        {
            if (!trialPath.Contains(area))
            {
                DisableAreaDetection(area);
            }
        }
        
        SetTrialState(TRIAL_NOT_STARTED);

        foreach (GameObject o in invisibleObjects)
        {
            o.SetActive(!hideInvisibleObjects);
        }

        if (setOcclusion)
        {
            foreach (GameObject o in obscureObjects)
            {
                Utils.SetObscure(o);
            }
            foreach (GameObject o in obscurableObjects)
            {
                Utils.SetObscurable(o);
            }
        }

        if (drawDebug)
        {
            DrawTrialPath();
        }

        if (enableQualisys)
        {
            qtm.InitServer();
            StartCoroutine(nameof(CheckQTMConnection));
        }
        else
        {
            HololensController hc = hololens.AddComponent<HololensController>();
            hc.movementSpeed = moveSpeed;
            hc.mouseSensitivity = mouseSensitivity;
            hololens.GetComponent<FollowRTObject>().enabled = false;
            feetTracker.LFoot.gameObject.GetComponent<FollowRTObject>().enabled = false;
            feetTracker.RFoot.gameObject.GetComponent<FollowRTObject>().enabled = false;
        }
          
       
        string prefix = "Assets/Landmarks/" + pathName + "/";
        string suffix = ".png";
        bool bigAreaIsDone = false;
        int currentTexture = 1;
        for (int i = 1; i < trialPath.Count - 2; i++)
        {
            Area a = trialPath.Get(i);
            if (!a.InBigArea())
            {
                AreaDetector ad = a.GetAreaDetector();
                Texture t = (Texture)AssetDatabase.LoadAssetAtPath(prefix + "R" + currentTexture + suffix, typeof(Texture));
                ad.Texture = t;
                currentTexture++;
            } else if (!bigAreaIsDone)
            {
                foreach (Area bigAreaArea in Area.BigAreaAreas())
                {
                    AreaDetector ad = bigAreaArea.GetAreaDetector();
                    Texture t = (Texture)AssetDatabase.LoadAssetAtPath(prefix + "R" + currentTexture + suffix, typeof(Texture));
                    ad.Texture = t;
                }
                bigAreaIsDone = true;
                currentTexture++;
            }
        }
    }

    // Checks connection with QTM localhost server
    private IEnumerator CheckQTMConnection()
    {
        while (true)
        {
            qtm.CheckConnection();
            yield return new WaitForSeconds(qtm.qtmPeriod);
        }
    }

    private void DisableAreaDetection(Area area)
    {
        AreaDetector areaDetector = area.GetAreaDetector();
        if (areaDetector != null)
        {
            areaDetector.gameObject.GetComponent<Collider>().enabled = false;
        }
    }

    private void InitiateAdviceConfig()
    {
        /* 
         * h : base height
         * c : base offset coef
         * rY : Y rotation
         * rY[0] = LEFT (do not change)
         * rY[1] = RIGHT (do not change)
         * rY[2] = DOWN (do not change)
         * rY[3] = TURN_LEFT
         * rY[4] = TURN_RIGHT
         * rY[5] = GO_FORWARD
         * rY[6] = GO_BACKWARD
         */
        float h = 0.8f;
        float c = 1.0f;
        List<float> rY = new List<float>() { -90f, +90f, +180f, -100f, +100f, +30f, +180f };
        float rX = 0f;
        ARROW_AIR = new AdviceConfig(arrowPrefab, wrongWayAdvicePrefab, h, c, rY, rX);

        h = 0.4f;
        c = 0.2f;
        rY = new List<float>() { -90f, +90f, +180f, -90f, +90f, +0f, +180f };
        rX = 90f;
        ARROW_GROUND = new AdviceConfig(arrowPrefab, wrongWayAdvicePrefab, h, c, rY, rX);

        h = 0.4f;
        c = 0f;
        rY = new List<float>() { -90f, +90f, +180f, -90f, +90f, +30f, +180f };
        rX = 0f;
        LIGHT = new AdviceConfig(null, wrongWayAdvicePrefab, h, c, rY, rX);

        h = 0f;
        c = 0.3f;
        rY = new List<float>() { -90f, +90f, +180f, 0f, +0f, -50f, +180f };
        rX = 0f;
        PEANUT = new AdviceConfig(peanutPrefab, wrongWayAdvicePrefab, h, c, rY, rX);
    }

    private void InitiateVariables()
    {
        SimTime = 0f;
        switch (pathName)
        {
            case Path.PathName.A: trialPath = new Path(Path.PATH_A); break;
            case Path.PathName.B: trialPath = new Path(Path.PATH_B); break;
            case Path.PathName.C: trialPath = new Path(Path.PATH_C); break;
            case Path.PathName.T: trialPath = new Path(Path.PATH_T); break;
            case Path.PathName.M: trialPath = new Path(Path.PATH_M); break;
        }
        _trialConfig = new TrialConfig(participantName, trialPath, _advice);
        qtm = new QualisysModule();
        _remainingPath = trialPath;

        AreaDetectorSize = areaDetectorPrefab.GetComponent<BoxCollider>().size.x;
    }

    private void InitiateComponents()
    {
        feetTracker = feet.GetComponent<FeetTracker>();
        hololensTracker = hololens.GetComponent<HololensTracker>();
        hololensCore = hololens.GetComponent<HololensCore>();
    }

    private void SetTrialState(int state)
    {
        _trialState = state;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("Data saved");
            WriteData();
        }
        if (!hololensCore.Calibrated && Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("Hololens calibrated");
            GameObject playspace = GameObject.Find("MixedRealityPlayspace");
            if (playspace != null)
            {
                playspace.transform.position = Vector3.zero;
                playspace.transform.rotation = Quaternion.Euler(0f, 89.55f, 0f);
            }
            hololensCore.Calibrated = true;
        }
        if (!isTracking && _trialState == TRIAL_ONGOING) {
            StartCoroutine(nameof(Track));
            isTracking = true;
        }
    }

    // Every time the coroutine starts, the trackers are told to acquire data and update text files
    private IEnumerator Track()
    {
        while (true)
        {
            SimTime += Time.deltaTime;
            feetTracker.Track(SimTime);
            hololensTracker.Track(SimTime);
            yield return null;
        }
    }

    private void OnApplicationQuit()
    {
        WriteData();
    }

    public void StartTrial()
    {
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
        isTracking = false;
    }

    // The data written in the synthetical file at the end of the trial
    public void WriteData()
    {
        string header =
            "Advice;"
            + "Path;"
            + "Path succeeded (0=yes, 1=no);"
            + "Trial time (s);"
            + "Distance travelled (m);"
            + "Walked path;"
            + "Number of travelled areas;"
            + "Mean time in area (s);"
            + "Number of errors;"
            + "Number of areas travelled by error;"
            + "Time spent in error (s);"
            + "Distance travelled in error (m);"
            + "Step count;"
            + "Step length (m);"
            + "Walk speed (m/s);"
            + "Step frequency (/s);"
            + "Proportion of time left foot was in front;"
            + "Step length asymmetry index (%);"
            + "Step length ratio;"
            + "Number of pauses;"
            + "Mean pause duration (s)";
        string data =
                _trialConfig.Advice + ";"
                + _trialConfig.Path.Name + ";"
                + ((TrialState == TRIAL_ENDED) ? 0 : 1) + ";"
                + SimTime + ";"
                + DataAnalyzer.DistanceTravelled(hololensTracker.Data) + ";"
                + hololensCore.WalkedPath + ";"
                + hololensCore.WalkedPath.Count + ";"
                + DataAnalyzer.AverageTimeInArea(SimTime, hololensCore.WalkedPath) + ";"
                + hololensCore.NumberOfErrors() + ";"
                + DataAnalyzer.NumberOfWrongAreas(hololensCore.Errors) + ";"
                + DataAnalyzer.ErrorTime(hololensCore.Errors) + ";"
                + DataAnalyzer.ErrorDistance(hololensTracker.Data, hololensCore.Errors) + ";"
                + feetTracker.StepCount + ";"
                + DataAnalyzer.MeanStepLength(feetTracker.Steps) + ";"
                + DataAnalyzer.MeanSpeed(hololensTracker.Data) + ";"
                + DataAnalyzer.MeanStepFrequency(feetTracker.Steps) + ";"
                + DataAnalyzer.LeftFootInFrontRate(feetTracker.LFoot.Data, feetTracker.RFoot.Data) + ";"
                + DataAnalyzer.StepLengthAsymmetryIndex(feetTracker.LFoot.Steps, feetTracker.RFoot.Steps) + ";"
                + DataAnalyzer.StepLengthRatio(feetTracker.LFoot.Steps, feetTracker.RFoot.Steps) + ";"
                + DataAnalyzer.NumberOfPauses(feetTracker.Steps, 0.5f) + ";"
                + DataAnalyzer.MeanRelativePause(feetTracker.Steps, 0.5f);
        /* Déviation axe de marche(0 = oui, 1 = non)	
         * Nombre d'hésitations	
         * Temps d'hésitations(s) */
        // Hésitation ===== pied en l'air qui change de direction OU pas en arrière
        // Piétinement
        DataWriter.WriteData(dataFileName, header, data, true);
    }

    public Area NextArea(int i)
    {
        if (i >= _remainingPath.Count)
            return null;
        return _remainingPath.Get(i);
    }

    /* Draws the theoretical path the participant has to follow during the xp */
    private void DrawTrialPath()
    {
        GameObject pathLine = new GameObject();
        pathLine.name = "PathLine";
        pathLine.transform.position = Utils.AreaToVector3(trialPath.Get(0), pathLineHeight);
        pathLine.AddComponent<LineRenderer>();
        LineRenderer lr = pathLine.GetComponent<LineRenderer>();
        lr.material = pathLineMaterial;
        lr.startColor = Color.red;
        lr.endColor = Color.red;
        lr.startWidth = pathLineWidth;
        lr.endWidth = pathLineWidth;
        lr.numCornerVertices = 0;
        lr.positionCount = trialPath.Count;
        for (int i = 0; i < trialPath.Count; i++)
        {
            lr.SetPosition(i, Utils.AreaToVector3(trialPath.Get(i), pathLineHeight));
        }
    }

    public Advice GetAdviceName()
    {
        return _advice;
    }

    public class QualisysModule {

        public float qtmPeriod = 0.1f;
        RTClient client;
        DiscoveryResponse? dr;
        private bool connectedToQTM;

        public QualisysModule()
        {
            client = RTClient.GetInstance();
        }

        // Connects client to localhost server
        private bool ConnectToServer()
        {
            return client.Connect(dr.Value, dr.Value.Port, true, true, false, true, false, true);
        }

        // Looks for the localhost discovery response
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
        public void InitServer()
        {
            if (FetchLocalhostServer())
            {
                if (connectedToQTM = ConnectToServer())
                {
                    Debug.Log("Qualisys: Connected to server: " + dr.Value.HostName + " (" + dr.Value.IpAddress + ":" + dr.Value.Port + ")");
                }
                else
                {
                    Debug.LogError("Qualisys: Failed to connect to localhost server");
                }
            }
            else
            {
                Debug.Log("Qualisys: Failed to fetch localhost server");
            }
        }

        public bool IsConnectedToQTM()
        {
            return connectedToQTM;
        }

        public void CheckConnection()
        {
            if (!connectedToQTM)
            {
                if (dr != null)
                {
                    if (connectedToQTM = ConnectToServer())
                    {
                        Debug.Log("Qualisys: Connected to server : " + dr.Value.HostName + " (" + dr.Value.IpAddress + ":" + dr.Value.Port + ")");
                    }
                }
                else
                {
                    InitServer();
                }
            }
        }
    }

}