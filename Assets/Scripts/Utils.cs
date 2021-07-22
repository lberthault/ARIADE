using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class contains useful methods to manage data in this project */
public class Utils
{

    /* Using the dichotomy search, returns the index of the datasnapshot closest to a specific simulation time */
    public static int TimeToIndex(List<DataSnapshot> data, float time)
    {
        int dataCount = data.Count;
        if (dataCount == 0)
        {
            return -1;
        }
        int iMin = 0;
        int iMax = dataCount - 1;
        float tMin = data[iMin].Time;
        float tMax = data[iMax].Time;
        if (time <= tMin)
        {
            return iMin;
        }
        if (time >= tMax)
        {
            return iMax;
        }
        return BinarySearch(data, time, iMin, iMax);
    }

    private static int BinarySearch(List<DataSnapshot> data, float time, int iMin, int iMax)
    {
        if (iMax - iMin == 1)
        {
            float d1 = time - data[iMin].Time;
            float d2 = data[iMax].Time - time;
            if (d1 < d2)
            {
                return iMin;
            }
            else
            {
                return iMax;
            }
        }
        int iMed = (iMin + iMax) / 2;
        if (data[iMed].Time < time)
        {
            return BinarySearch(data, time, iMed, iMax);
        }
        else
        {
            return BinarySearch(data, time, iMin, iMed);
        }
    }

    /* Rounds a float by keeping a certain number of decimal digits */
    public static float Round(float x, int digits)
    {
        return (float) System.Math.Round((double) x, digits);
    }

    /* Returns the Vector3 corresponding to the center of an Area at a specific height */
    public static Vector3 AreaToVector3(Area area, float height)
    {
        GameObject areaDetectors = GameObject.Find("AreaDetectors");
        foreach (Transform transform in areaDetectors.transform)
        {
            GameObject go = transform.gameObject;
            AreaDetector areaDetector = go.GetComponent<AreaDetector>();
            if (areaDetector.Area.Equals(area))
            {
                return new Vector3(go.transform.position.x, height, go.transform.position.z);
            }
        }
        return Vector3.zero;
    }

    public static Vector3 ProjectOnFloor(Vector3 v)
    {
        return new Vector3(v.x, 0, v.z);
    }

    public static Direction GetDirection(Area from, Area to)
    {
        int dLine = to.Line - from.Line;
        int dColumn = to.Column - from.Column;
        if (dLine >= 1)
        {
            return Direction.DOWN;
        }
        if (dLine <= -1)
        {
            return Direction.UP;
        }
        if (dColumn >= 1)
        {
            return Direction.RIGHT;
        }
        return Direction.LEFT;
    }

    public static UserAction GetAction(Area from, Area at, Area to)
    {
        Direction d1 = GetDirection(from, at);
        Direction d2 = GetDirection(at, to);
        return GetAction(d1, d2);
    }

    public static UserAction GetAction(Direction d1, Direction d2)
    {
        if (d1 == d2)
        {
            return UserAction.GO_FORWARD;
        }
        if (AreOppositeDirections(d1, d2))
        {
            return UserAction.GO_BACKWARD;
        }
        if ((d1 == Direction.RIGHT && d2 == Direction.UP)
            || (d1 == Direction.UP && d2 == Direction.LEFT)
            || (d1 == Direction.LEFT && d2 == Direction.DOWN)
            || (d1 == Direction.DOWN && d2 == Direction.RIGHT))
        {
            return UserAction.TURN_LEFT;
        }
        return UserAction.TURN_RIGHT;
    }

    public static bool AreOppositeDirections(Direction d1, Direction d2)
    {
        return (int)d1 + (int)d2 == 0;
    }

    public static float ScalarProduct(Vector3 v1, Vector3 v2)
    {
        return (v1.x * v2.x) + (v1.y * v2.y) + (v1.z * v2.z);
    }
    public static void SetObscurable(GameObject o)
    {
        Component[] renderers = o.GetComponentsInChildren(typeof(Renderer));
        foreach (Component renderer in renderers)
        {
            ((Renderer)renderer).material.renderQueue = 3002;
        }
    }

    public static void SetObscure(GameObject o)
    {
        Component[] renderers = o.GetComponentsInChildren(typeof(Renderer));
        foreach (Component renderer in renderers)
        {
            ((Renderer)renderer).material = GameManager.Instance.invisibleMaterial;
        }
    }

    public static string PathNameToString(Path.PathName pathName)
    {
        return (pathName == Path.PathName.M) ? "BASELINE" : pathName.ToString();
    }

}
