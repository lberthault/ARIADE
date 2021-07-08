using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Because of the change of reference between Unity and Qualisys, RT objects are always on the other side of the origin (but at the same height).
//This class makes an object the symmetrical of an RT object so they correspond the desired object.
public class FollowRTObject : MonoBehaviour
{
    //The RT object "this" is the symmetrical of
    public GameObject RTObject;

    void Update()
    {
        transform.position = new Vector3(-RTObject.transform.position.x, RTObject.transform.position.y, -RTObject.transform.position.z);
        transform.rotation = Quaternion.Euler(-RTObject.transform.rotation.eulerAngles.x, RTObject.transform.rotation.eulerAngles.y, -RTObject.transform.rotation.eulerAngles.z);
    }

}