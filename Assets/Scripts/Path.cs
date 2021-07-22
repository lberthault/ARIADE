using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path
{
    public enum PathName
    {
        A,
        B,
        C,
        T,
        M
    }
    public static string PATH_A = "A16.15.25.24.33.43.42.32.22.21";
    public static string PATH_B = "B01.11.12.13.23.33.32.42.43.44";
    public static string PATH_C = "C55.45.35.34.23.13.12.11.21.31";
    public static string PATH_T = "T53.43.33.34.35"; //Test
    public static string PATH_M = "M46.45.44.43.42.41.42.43.44.45"; //Baseline
    //public static string PATH_A = "A16.15.25.24.33.43.42.32.22.21.20";
    //public static string PATH_B = "B01.11.12.13.23.33.32.42.43.44.54";
    //public static string PATH_C = "C55.45.35.34.23.13.12.11.21.31.30";
    //public static string PATH_T = "T53.43.33.34.35.36"; //Test
    //public static string PATH_M = "M46.45.44.43.42.41.40.41.42.43.44.45.46"; //Baseline

    public PathName Name { get; set; }
    private List<Area> areas;

    public int Count
    {
        get { return areas.Count; }
    }

    public Path()
    {
        areas = new List<Area>();
    }

    public Path(List<Area> areas)
    {
        this.areas = areas;
    }

    public Path(string strPath)
    {
        areas = new List<Area>();
        Name = (PathName)System.Enum.Parse(typeof(PathName), strPath.Substring(0, 1));
        string[] strCoords = strPath.Substring(1, strPath.Length - 1).Split('.');
        int line, column;
        foreach (string strCoord in strCoords)
        {
            line = int.Parse(strCoord.Substring(0, 1));
            column = int.Parse(strCoord.Substring(1, 1));
            areas.Add(new Area(line, column));
        }
    }

    public int CountArea(Area area)
    {
        int res = 0;
        foreach (Area a in areas)
        {
            if (a.Equals(area))
            {
                res++;
            }
        }
        return res;
    }

    public bool Contains(Area area)
    {
        return areas.Contains(area);
    }

    public Area Get(int i)
    {
        return areas[i];
    }

    public Area GetLast()
    {
        if (areas.Count > 0)
        {
            return areas[areas.Count - 1];
        }
        else
        {
            return null;
        }
    }

    public void Add(Area area)
    {
        areas.Add(area);
    }

    public void RemoveLast()
    {
        if (areas.Count > 0)
        {
            areas.RemoveAt(areas.Count - 1);
        } else
        {
            Debug.LogError("Attempting to remove area from trialPath but trialPath is empty");
        }
    }

    public void Remove(Area area)
    {
        areas.Remove(area);
    }

    public void Clear()
    {
        areas.Clear();
    }

    override public string ToString()
    {
        string res = Name.ToString();
        for (int i = 0; i < areas.Count; i++)
        {
            res += areas[i].Line + "" + areas[i].Column;
            if (i != areas.Count - 1)
            {
                res += ".";
            }
        }
        return res;
    }

    public Path Join(Path p)
    {
        List<Area> joinedAreas = new List<Area>();
        joinedAreas.AddRange(areas);
        joinedAreas.AddRange(p.areas);
        return new Path(joinedAreas);
    }

    public Area RemoveFirst()
    {
        Area area = areas[0];
        areas.RemoveAt(0);
        return area;
    }

    public void Push(Area area)
    {
        areas.Insert(0, area);
    }

    public List<Area> AreasInBigArea()
    {
        List<Area> res = new List<Area>();
        foreach (Area area in areas)
        {
            if (area.InBigArea())
            {
                res.Add(area);
            }
        }
        return res;
    }
}
