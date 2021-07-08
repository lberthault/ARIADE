using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Represents an advice : arrow, light path or PEANUT */
public class Arrow
{
    //Arrow spawn area
    private Area _area;
    public Area Area { 
        get { return _area; }
    }
    //Arrow instance game object
    private GameObject arrowObj;

    public Arrow(Area area, GameObject arrowPrefab, Vector3 position, Vector3 rotation)
    {
        _area = new Area(area);
        arrowObj = GameObject.Instantiate(arrowPrefab, position, Quaternion.Euler(rotation));
    }

    // Removes the advice from scene and memory
    public void Remove()
    {
        GameObject.Destroy(arrowObj);
    }
}
