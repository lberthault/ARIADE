using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Accelerometer : MonoBehaviour
{
    public static int DEFAULT_SAMPLES = 20;
    private Vector3 _acceleration;
    public Vector3 Acceleration
    {
        get { return _acceleration; }
    }
    private Vector3[] positionVector;
    private float[] positionTimeVector;
    private int positionSamplesTaken = 0;

    //This function calculates the acceleration vector in meter/second^2.
    //A low number of samples can give a jittery result due to rounding errors.
    //If more samples are used, the output is more smooth but has a higher latency.
    public Vector3 LinearAcceleration(Vector3 position, int samples)
    {

        Vector3 averageSpeedChange = Vector3.zero;
        _acceleration = Vector3.zero;
        Vector3 deltaDistance;
        float deltaTime;
        Vector3 speedA;
        Vector3 speedB;

        //Clamp sample amount. In order to calculate acceleration we need at least 2 changes
        //in speed, so we need at least 3 position samples.
        if (samples < 3)
        {
            samples = 3;
        }

        //Initialize
        if (positionVector == null)
        {
            positionVector = new Vector3[samples];
            positionTimeVector = new float[samples];
        }

        //Fill the position and time sample array and shift the location in the array to the left
        //each time a new sample is taken. This way index 0 will always hold the oldest sample and the
        //highest index will always hold the newest sample. 
        for (int i = 0; i < positionVector.Length - 1; i++)
        {
            positionVector[i] = positionVector[i + 1];
            positionTimeVector[i] = positionTimeVector[i + 1];
        }
        positionVector[positionVector.Length - 1] = position;
        positionTimeVector[positionTimeVector.Length - 1] = Time.time;

        positionSamplesTaken++;

        //The output acceleration can only be calculated if enough samples are taken.
        if (positionSamplesTaken >= samples)
        {
            //Calculate average speed change.
            for (int i = 0; i < positionVector.Length - 2; i++)
            {

                deltaDistance = positionVector[i + 1] - positionVector[i];
                deltaTime = positionTimeVector[i + 1] - positionTimeVector[i];

                //If deltaTime is 0, the output is invalid.
                if (deltaTime == 0)
                {
                    return _acceleration;
                }

                speedA = deltaDistance / deltaTime;
                deltaDistance = positionVector[i + 2] - positionVector[i + 1];
                deltaTime = positionTimeVector[i + 2] - positionTimeVector[i + 1];

                if (deltaTime == 0)
                {
                    return _acceleration;
                }

                speedB = deltaDistance / deltaTime;

                //This is the accumulated speed change at this stage, not the average yet.
                averageSpeedChange += speedB - speedA;
            }

            //Now this is the average speed change.
            averageSpeedChange /= positionVector.Length - 2;

            //Get the total time difference.
            float deltaTimeTotal = positionTimeVector[positionTimeVector.Length - 1] - positionTimeVector[0];

            //Now calculate the acceleration, which is an average over the amount of samples taken.
            _acceleration = averageSpeedChange / deltaTimeTotal;

            return _acceleration;
        }
        else
        {
            return _acceleration;
        }
    }

}
