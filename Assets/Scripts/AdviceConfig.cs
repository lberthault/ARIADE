using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdviceConfig
{
    //Advice prefab
    //NOTE : the default advice prefab should be pointing UP
    public GameObject AdvicePrefab { get; }
    //Wrong way advice prefab appearing when the participant takes the wrong path
    public GameObject WrongWayAdvicePrefab { get; }
    //Spawn height
    public float AdviceBaseHeight { get; }
    //Spawn distance from the center of the spawning area normalized by the area size (0=center, 1=border)
    public float AdviceBaseOffsetCoef { get; }
    /*
     * What angle should the advice be turned around Y-axis depending on the direction or user action
        * AdviceRotationY[0] = LEFT (do not change)
        * AdviceRotationY[1] = RIGHT (do not change)
        * AdviceRotationY[2] = DOWN (do not change)
        * NOTE : the default advice prefab should be pointing UP
        * AdviceRotationY[3] = TURN_LEFT
        * AdviceRotationY[4] = TURN_RIGHT
        * AdviceRotationY[5] = GO_FORWARD
        * AdviceRotationY[6] = GO_BACKWARD
    */
    public List<float> AdviceRotationY { get; }
    //What angle should the advice be turned around X-axis
    public float AdviceRotationX { get; }

    public AdviceConfig(GameObject prefab, GameObject wrongWayPrefab, float baseHeight, float baseOffsetCoef, List<float> rotationY, float rotationX)
    {
        AdvicePrefab = prefab;
        WrongWayAdvicePrefab = wrongWayPrefab;
        AdviceBaseHeight = baseHeight;
        AdviceBaseOffsetCoef = baseOffsetCoef;
        AdviceRotationY = rotationY;
        AdviceRotationX = rotationX;
    }
}
