using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{

    [SerializeField]
    private GameObject obj;

    void Start()
    {
        MatchPositionAndRotation();
    }
    void Update()
    {
        MatchPositionAndRotation();
    }

    private void MatchPositionAndRotation()
    {
        // transform.position = obj.transform.position;
        transform.position = new Vector3(-obj.transform.position.x, obj.transform.position.y, -obj.transform.position.z);
        transform.rotation = Quaternion.Euler(-obj.transform.rotation.eulerAngles.x, obj.transform.rotation.eulerAngles.y, -obj.transform.rotation.eulerAngles.z);
        // transform.rotation = obj.transform.rotation;
        // transform.rotation = Quaternion.Euler(-obj.transform.rotation.eulerAngles.z, obj.transform.rotation.eulerAngles.y, obj.transform.rotation.eulerAngles.x);
        // transform.rotation = Quaternion.Euler(obj.transform.rotation.eulerAngles.y, obj.transform.rotation.eulerAngles.z, -obj.transform.rotation.eulerAngles.x);
    }
}
