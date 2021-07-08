using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HololensCore : MonoBehaviour
{
    GameManager gm;
    AdviceManager am;
    float simTime;
    private bool _participantReady = false;
    private Area _lastArea;
    public Area LastArea
    {
        get { return _lastArea; }
    }
    private Area _currentArea;
    public Area CurrentArea
    {
        get { return _currentArea; }
    }
    private Path _walkedPath = new Path(); 
    public Path WalkedPath
    {
        get { return _walkedPath; }
    }
    private Path matchingPath = new Path();

    private List<PathError> _errors;
    public List<PathError> Errors
    {
        get { return _errors; }
    }

    private PathError currentError;

    private bool errorCorrected = false;
    public bool Calibrated { get; set; }

    private void Awake()
    {
        _errors = new List<PathError>();
    }

    private void Start()
    {
        gm = GameManager.Instance;
        am = AdviceManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_participantReady && Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("Participant ready");
            _participantReady = true;
            if (gm.pathName != Path.PathName.M)
            {
                gm.StartTrial();
                EnteringArea(gm.NextArea(0));
            }
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.TryGetComponent(out AreaDetector detector))
        {

            if (gm.TrialState == GameManager.TRIAL_ONGOING)
            {
                EnteringArea(detector.Area);
            }

        }
    }
    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.TryGetComponent<AreaDetector>(out AreaDetector detector))
        {
            if (gm.TrialState == GameManager.TRIAL_ONGOING)
            {
                ExitingArea();
            }
        }
    }

    public int NumberOfErrors()
    {
        return _errors.Count;
    }

    private void EnteringArea(Area area)
    {
        if (_currentArea == null && _participantReady)
        {
            _lastArea = _walkedPath.GetLast();

            // Prevent area double check
            if (_lastArea != null && area.Equals(_lastArea))
                return;

            // Update landmarks
            if (_lastArea != null && !(area.InBigArea() && _lastArea.InBigArea()))
            {
                //Debug.Log("Removed landmarks at " + lastArea);
                _lastArea.GetAreaDetector().RemoveLandmarks(true);
                AreaDetector areaDetector = area.GetAreaDetector();
                if (gm.pathName != Path.PathName.M)
                    areaDetector.DisplayLandmarks(Utils.GetDirection(_lastArea, area), true);
            }

            // Copy entered area to add it an "in time"
            _currentArea = new Area(area, simTime);

            // Check if participant is on the right path and update remaining path
            bool onTheRightPath = false;
            if (gm.NextArea(0).Equals(_currentArea))
            {
                onTheRightPath = true;
                gm.RemainingPath.RemoveFirst();
            }
            else if (_walkedPath.Count == 0)
            {
                for (int i = 0; i < gm.RemainingPath.Count; i++)
                {
                    if (gm.RemainingPath.Get(i).Equals(_currentArea))
                    {
                        onTheRightPath = true;
                        for (int j = 0; j < i + 1; j++)
                        {
                            gm.RemainingPath.RemoveFirst();
                        }
                    }
                }
            }

            // Check if the participant has finished the trial
            if (gm.RemainingPath.Count == 0)
            {
                gm.EndTrial();
                return;
            }

            if (onTheRightPath)
            {
                // If an error was registered but the participant has corrected it, remove it
                if (currentError != null)
                {
                    errorCorrected = true;
                    currentError.SetCorrect(simTime);
                    currentError = null;
                }

                // Remove previous advice if necessary
                am.RemovePreviousAdviceIfNecessary();
                

                // Update matching path because participant is on the right way
                matchingPath.Add(new Area(area));

                // Display the next advice
                if (gm.pathName != Path.PathName.M)
                {
                    am.DisplayNextAdvice(errorCorrected);
                }



            }
            else
            {
                if (gm.pathName != Path.PathName.M)
                {
                    bool worsening;
                    if (currentError == null)
                    {
                        gm.RemainingPath.Push(_lastArea);
                        am.RemoveAllAdvice();
                        currentError = new PathError(_walkedPath.GetLast(), _currentArea, simTime);
                        _errors.Add(currentError);
                        worsening = true;
                    }
                    else
                    {
                        if (worsening = currentError.Update(_currentArea))
                        {
                            // Error worsening
                            gm.RemainingPath.Push(currentError.Path.Get(currentError.Path.Count - 2));
                        }
                    }
                    if (worsening)
                    {
                        am.Worsening();
                    }
                    else
                    {
                        am.NotWorsening();
                    }

                    am.UpdateAdviceInError();
                }



            }
            _walkedPath.Add(_currentArea);
        }


    }

    private void ExitingArea()
    {
        if (_currentArea != null)
        {
            _currentArea.OutTime = simTime;
            _currentArea = null;
        }
    }

}
