using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HololensTracker : MonoBehaviour
{

    private GameManager gm;
    private float simTime;

    //Data
    private string dataFileName;
    private List<DataSnapshot> _data;
    public List<DataSnapshot> Data
    {
        get { return _data; }
    }

    private Trail trail;

    private Vector3 lastPosition;

    private void Awake()
    {
        trail = GetComponent<Trail>();
        _data = new List<DataSnapshot>();
        dataFileName = "HololensData";
    }

    private void Start()
    {
        gm = GameManager.Instance;
        if (gm.drawDebug)
        {
            trail.Initiate();
        }
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, lastPosition) > 5f)
        {
            if (trail.Initiated) trail.Clear();
        }
        lastPosition = transform.position;

    }

    public void Track(float simTime)
    {
        this.simTime = simTime;
        if (trail.Initiated)
        {
            trail.CheckModifications();
        }

        RegisterData();
        WriteData();
    }

    public void RegisterData()
    {
        _data.Add(new DataSnapshot(simTime, transform.position, transform.rotation.eulerAngles));
    }

    public void WriteData()
    {
        string header = "Simulation time (s);" +
            "Position vector;" +
            ";" +
            ";" +
            "Euler rotation vector;" +
            ";" +
            ";" +
            "Instant speed (m/s);" +
            "Mean speed (m/s);" +
            "Distance travelled (m)";
        string data = simTime + ";"
                + transform.position.x + ";"
                + transform.position.y + ";"
                + transform.position.z + ";"
                + transform.rotation.eulerAngles.x + ";"
                + transform.rotation.eulerAngles.y + ";"
                + transform.rotation.eulerAngles.z + ";"
                + DataAnalyzer.CurrentSpeed(_data) + ";"
                + DataAnalyzer.MeanSpeed(_data) + ";"
                + DataAnalyzer.DistanceTravelled(_data);
        DataWriter.WriteDataSingleLine(dataFileName, header, data, false);
    }
   
    public void ResetData()
    {
        _data.Clear();
    }

}
