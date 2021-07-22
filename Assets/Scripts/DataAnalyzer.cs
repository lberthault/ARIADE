using System.Collections.Generic;
using UnityEngine;

public class DataAnalyzer
{

    public static float DistanceTravelled(List<DataSnapshot> data)
    {
        float res = 0f;
        int N = data.Count - 1;
        float d;
        for (int i = 0; i < N; i++)
        {
            d = Vector3.Distance(Utils.ProjectOnFloor(data[i].Position), Utils.ProjectOnFloor(data[i + 1].Position));
            if (d <= 1f)
            {
                res += d;
            }
        }
        return res;
    }

    public static float MeanHeight(List<DataSnapshot> data)
    {
        float res = 0f;
        foreach (DataSnapshot snap in data)
        {
            res += snap.Position.y;
        }
        return res / data.Count;
    }

    public static float CurrentSpeed(List<DataSnapshot> data)
    {
        return Speed(data, data.Count - 1);
    }

    public static float MeanSpeed(List<DataSnapshot> data)
    {
        float v = 0f;
        int N = data.Count - 1;
        if (N > 0)
        {
            v = DistanceTravelled(data) / (data[N].Time - data[0].Time);
        }
        return v;
    }

    public static float Speed(List<DataSnapshot> data, float t)
    {
        return Speed(data, Utils.TimeToIndex(data, t));
    }

    public static float Speed(List<DataSnapshot> data, int i)
    {
        if (i <= 0 || i >= data.Count)
        {
            return -1;
        }
        float d = Vector3.Distance(Utils.ProjectOnFloor(data[i - 1].Position), Utils.ProjectOnFloor(data[i].Position));
        float t = data[i].Time - data[i - 1].Time;
        float v = d / t;
        if (v > 10f)
        {
            v = -1;
        }
        return v;
    }

    public static float GetDistance(List<DataSnapshot> data, int i, int j)
    {
        return Vector3.Distance(Utils.ProjectOnFloor(data[i].Position), Utils.ProjectOnFloor(data[j].Position));
    }

    public static float GetDistance(List<DataSnapshot> data, float t0, float tf)
    {
        return GetDistance(data, Utils.TimeToIndex(data, t0), Utils.TimeToIndex(data, tf));
    }

    public static float MeanStepLength(List<Step> steps)
    {
        float res = 0;
        int N = steps.Count;
        if (N == 0)
        {
            return -1;
        }
        for (int i = 0; i < N; i++)
        {
            res += steps[i].Length;
        }
        return res / N;
    }

    public static float StepLengthAsymmetryIndex(List<Step> lFootSteps, List<Step> rFootSteps)
    {
        int N = Mathf.Min(lFootSteps.Count, rFootSteps.Count);
        if (N > 0)
        {
            float leftFootMeanStepLength = MeanStepLength(lFootSteps.GetRange(0, N));
            float rightFootMeanStepLength = MeanStepLength(rFootSteps.GetRange(0, N));
            float AI = Mathf.Abs((leftFootMeanStepLength - rightFootMeanStepLength) / (0.5f * (leftFootMeanStepLength + rightFootMeanStepLength)) * 100f);
            return AI;
        } else
        {
            return -1;
        }
    }

    public static float StepLengthRatio(List<Step> lFootSteps, List<Step> rFootSteps)
    {
        int N = Mathf.Min(lFootSteps.Count, rFootSteps.Count);
        if (N > 0)
        {
            float leftFootMeanStepLength = MeanStepLength(lFootSteps.GetRange(0, N));
            float rightFootMeanStepLength = MeanStepLength(rFootSteps.GetRange(0, N));
            float R;
            if (leftFootMeanStepLength < rightFootMeanStepLength)
            {
                R = leftFootMeanStepLength / rightFootMeanStepLength;
            } else
            {
                R = rightFootMeanStepLength / leftFootMeanStepLength;
            }
            return R;
        }
        else
        {
            return -1;
        }
    }

    public static float MeanStepTime(List<Step> steps)
    { 
        float res = 0;
        int N = steps.Count;
        if (N == 0)
        {
            return -1;
        }
        for (int i = 0; i < N; i++)
        {
            res += steps[i].Duration;
        }
        return res / N;
    }
    /*
    public static float NetPauseAfterStep(List<Step> steps, int stepIndex)
    {
        if (steps.Count < stepIndex + 2)
            return -1;
        return steps[stepIndex + 1].StartTime - steps[stepIndex].EndTime;
    }

    public static float RelativePauseAfterStep(List<Step> steps, int stepIndex, float threshold)
    {
        if (steps.Count < stepIndex + 2)
            return -1;
        float netPause = steps[stepIndex + 1].StartTime - steps[stepIndex].EndTime;
        if (netPause < threshold)
            return -1;
        return netPause - threshold;
    }

    public static int NumberOfPauses(List<Step> steps, float threshold)
    {
        int res = 0;
        int N = steps.Count - 1;
        if (N == 0)
        {
            return -1;
        }
        for (int i = 0; i < N - 1; i++)
        {
            if (RelativePauseAfterStep(steps, i, threshold) >= 0)
            {
                res++;
            }
        }
        return res;
    }

    public static float MeanNetPause(List<Step> steps)
    {
        float res = 0;
        int N = steps.Count - 1;
        if (N == 0)
        {
            return -1;
        }
        for (int i = 0; i < N - 1; i++)
        {
            res += NetPauseAfterStep(steps, i);
        }
        return res / N;
    }

    public static float MeanRelativePause(List<Step> steps, float threshold)
    {
        float res = 0;
        int N = steps.Count - 1;
        if (N == 0)
        {
            return -1;
        }
        for (int i = 0; i < N - 1; i++)
        {
            if (RelativePauseAfterStep(steps, i, threshold) >= 0)
            {
                res += RelativePauseAfterStep(steps, i, threshold);
            }
        }
        return res / N;
    }
    */

    public static float MeanPauseDuration(List<WalkingPause> pauses)
    {
        if (pauses.Count == 0) 
            return 0;
        float res = 0;
        foreach (WalkingPause pause in pauses)
        {
            res += pause.Duration;
        }
        return res / pauses.Count;
    }

    public static float TotalPauseDuration(List<WalkingPause> pauses)
    {
        float res = 0;
        foreach (WalkingPause pause in pauses)
        {
            res += pause.Duration;
        }
        return res;
    }

    public static float MeanStepFrequency(List<Step> steps)
    {
        if (steps.Count == 0)
        {
            return 0f;
        }
        float duration;
        int count;
        if (steps[steps.Count - 1].IsFinished())
        {
            duration = steps[steps.Count - 1].EndTime - steps[0].StartTime;
            count = steps.Count;
        } else
        {
            duration = steps[steps.Count - 2].EndTime - steps[0].StartTime;
            count = steps.Count - 1;
        }
        if (duration <= 0f)
        {
            return 0f;
        }
        return count / duration;
    }

    /* Returns the current front foot */
    public static Foot FrontFoot(DataSnapshot lFootData, DataSnapshot rFootData)
    {
        if (lFootData == null || rFootData == null)
        {
            return Foot.Left;
        }
        float thetaL = lFootData.Rotation.y * Mathf.PI / 180f;
        thetaL %= 2 * Mathf.PI;

        float thetaR = rFootData.Rotation.y * Mathf.PI / 180f;
        thetaR %= 2 * Mathf.PI;
        // Compute the medium angle between the feet */
        float theta = (thetaL + thetaR) / 2.0f;
        if (thetaL > thetaR && thetaL - thetaR > Mathf.PI)
        {
            theta += Mathf.PI;
        }

        theta %= 2 * Mathf.PI;

        /* Compute a director vector of the line passing between the feet */
        Vector3 v = new Vector3(Mathf.Tan(theta), 0, 1);
        if (theta < Mathf.PI / 2f || theta > 3f * Mathf.PI / 2f)
        {
            v = -v;
        }

        if (lFootData.Rotation.y > 0.0f &&
            lFootData.Rotation.y < 180f &&
            rFootData.Rotation.y > 180f &&
            rFootData.Rotation.y < 360f)
        {
            v = -v;
        }
        Vector3 posL = Utils.ProjectOnFloor(lFootData.Position);
        Vector3 posR = Utils.ProjectOnFloor(rFootData.Position);
        Vector3 OC = (posL + posR) / 2.0f;
        
        // Compute distance between both feet and the center of the feet to determine which one is in front
        float dL = Utils.ScalarProduct(posL - OC, v) / Utils.ScalarProduct(v, v);
        float dR = Utils.ScalarProduct(posR - OC, v) / Utils.ScalarProduct(v, v);

        return dL < dR ? Foot.Right : Foot.Left;
    }

    /* The rate at which the left foot is in front during the xp */
    public static float LeftFootInFrontRate(List<DataSnapshot> leftFootData, List<DataSnapshot> rightFootData)
    {
        int N = Mathf.Min(leftFootData.Count, rightFootData.Count);
        int n = 0;
        for (int i = 0; i < N; i++)
        {
            if (FrontFoot(leftFootData[i], rightFootData[i]) == Foot.Left)
            {
                n++;
            }
        }
        return 100f * n / N;
    }

    public static int NumberOfWrongAreas(List<PathError> errors)
    {
        int res = 0;
        foreach (PathError error in errors)
        {
            res += error.NumberOfWrongAreas();
        }
        return res;
    }

    public static float TotalDistanceWalkedDuringError(List<DataSnapshot> data, List<PathError> errors)
    {
        float res = 0;
        foreach (PathError error in errors)
        {
            res += DistanceWalkedDuringError(data, error);
        }
        return res;
    }

    public static float DistanceWalkedDuringError(List<DataSnapshot> data, PathError error)
    {
        return GetDistance(data, error.WalkedPath.Get(1).InTime, error.WalkedPath.GetLast().OutTime);
    }

    public static float ErrorTime(List<PathError> errors)
    {
        float res = 0;
        foreach (PathError error in errors)
        {
            res += error.Duration();
        }
        return res;
    }

    public static float AverageTimeInArea(float simTime, Path walkedPath)
    {
        if (walkedPath.Count == 0)
            return -1;
        return (simTime - walkedPath.Get(0).InTime) / walkedPath.Count;
    }

    public static float TotalTimeSpentLookingAtLandmarks(Dictionary<Area, float> d)
    {
        float res = 0f;
        foreach (Area area in d.Keys)
        {
            res += d[area];
        }
        return res;
    }

    public static float MeanTimeSpentLookingAtLandmarks(Dictionary<Area, float> d)
    {
        if (d.Count == 0)
        {
            return 0f;
        }
        return TotalTimeSpentLookingAtLandmarks(d) / d.Count;
    }

    public static float TotalTimeSpentLookingAtLandmarksDuringPauses(List<WalkingPause> pauses)
    {
        float res = 0f;
        foreach (WalkingPause pause in pauses)
        {
            res += pause.TimeSpentLookingAtLandmarks;
        }
        return res;
    }
}
