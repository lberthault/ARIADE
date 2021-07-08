/* An error represents a bad decision at a specific starting intersection */
public class PathError
{
    /* The path the participant has to follow to correct the error */
    private Path _path;
    public Path Path
    {
        get { return _path; }
    }
    private bool _corrected;
    public bool Corrected
    {
        get { return _corrected; }
    }
    /* Start and end time */
    private float startTime, endTime;
    /* The number of times the participant took bad decisions in this error i.e. not turning back or not following the right arrows */
    private int numberOfWrongAreas;
    
    public PathError(Area from, Area to, float simTime)
    {
        startTime = simTime;
        _path = new Path();
        _path.Add(from);
        _path.Add(to);
        numberOfWrongAreas++;
        _corrected = false;
    }

    /* If the error is not corrected, this is called each time the participant passes through an area and updates the variables */
    public bool Update(Area area)
    {
        if (!_path.Contains(area))
        {
            _path.Add(area);
            numberOfWrongAreas++;
            return true;
        } else
        {
            _path.RemoveLast();
            return false;
        }

    }

    public void SetCorrect(float simTime)
    {
        endTime = simTime;
        _corrected = true;
    }

    public int NumberOfWrongAreas()
    {
        // Doesn't count the start/end area
        return numberOfWrongAreas;
    }

    public float Time()
    {
        return endTime - startTime;
    }
}