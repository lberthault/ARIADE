using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Makes the link between the area detector gameobject equipped with a collider and the abstract Area class */
public class AreaDetector : MonoBehaviour
{
    public Area Area
    {
        get { return new Area(line, column); }
    }
    public int line, column;
}
