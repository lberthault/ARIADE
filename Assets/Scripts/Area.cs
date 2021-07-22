/* An area represents the square-shaped space between four landmarks */
using System.Collections.Generic;
using UnityEngine;

public class Area
{
   
    /* An area is defined by its line and column, its coordinates on the scene where (0,0) is the top left area */
    private int _line, _column;
    public int Line
    {
        get { return _line; }
    }
    public int Column
    {
        get { return _column; }
    }
    /* The time at which the participant enters and leaves the area */
    private float _inTime, _outTime;
    public float InTime
    {
        get { return _inTime; }
    }
    public float OutTime
    {
        get { return _outTime; }
        set { _outTime = value; }
    }    /* The total time the participant spent in the area */
    public float TimeSpent
    {
        get { return _outTime - _inTime; }
    }

    public Area(int line, int column)
    {
        this._line = line;
        this._column = column;
        _inTime = -1f;
        _outTime = -1f;
    }

    public Area(Area area, float inTime)
    {
        this._line = area._line;
        this._column = area._column;
        this._inTime = inTime;
        this._outTime = -1f;
    }

    public Area(Area area)
    {
        this._line = area._line;
        this._column = area._column;
        this._inTime = -1f;
        this._outTime = -1f;
    }

    override public string ToString()
    {
        return "(" + _line + "," + _column + ")";
    }

    /* Two areas are equal iff they have the same coordinates */
    public override bool Equals(object obj)
    {
        return (obj != null)
            && (obj.GetType() == typeof(Area))
            && (((Area)obj)._line == _line)
            && (((Area)obj)._column == _column);
    }

    public override int GetHashCode() { return 100*_line + _column; }

    public bool InBigArea()
    {
        return (_line == 2 || _line == 3) && (_column == 3 || _column == 4);
    }

    public static List<Area> BigAreaAreas()
    {
        List<Area> res = new List<Area>();
        res.Add(new Area(2, 3));
        res.Add(new Area(3, 3));
        res.Add(new Area(2, 4));
        res.Add(new Area(3, 4));
        return res;
    }

    /* The external border is the set of areas outside the trial area */
    public bool IsOnExternalBorder()
    {
        return _line == 0 || _line == 5 || _column == 0 || _column == 6;
    }

    /* The internal border is the set of the first areas behind the outermost landmarks */
    public bool IsOnInternalBorder()
    {
        return !IsOnExternalBorder() && (_line == 1 || _line == 4 || _column == 1 || _column == 5);
    }

    /* An area is in corner iff it is in a corner of the internal border */
    public bool IsInCorner()
    {
        return (_line == 1 && _column == 1) || (_line == 1 && _column == 5) || (_line == 4 && _column == 1) || (_line == 4 && _column == 5);
    }

    /* The number of decisions the participant can take at the intersection */
    public int NumberOfDecisions()
    {
        bool inBigArea = InBigArea();
        bool onExternalBorder = IsOnExternalBorder();
        bool onInternalBorder = IsOnInternalBorder();
        bool inCorner = IsInCorner();


        if (onExternalBorder)
        {
            return 0;
        }

        if (inCorner)
        {
            return 2;
        }

        if (onInternalBorder && !inCorner)
        {
            return 3;
        }

        if (!inBigArea)
        {
            return 4;
        } else
        {
            return 8;
        }
    }

    public AreaDetector GetAreaDetector()
    {
        GameObject parent = GameObject.Find("AreaDetectors");
        foreach (Transform transform in parent.transform)
        {
            AreaDetector areaDetector = transform.gameObject.GetComponent<AreaDetector>();
            if (areaDetector.Area.Equals(this))
            {
                return areaDetector;
            }
        }
        return null;
    }

}