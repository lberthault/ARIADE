using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdviceManager : MonoBehaviour
{

    private static AdviceManager _instance;
    public static AdviceManager Instance { get { return _instance; } }

    private GameManager gm;
    private HololensCore core;

    //ARROWS
    private List<Arrow> visibleArrows;

    //LIGHT PATH
    private LineRenderer lightPathLineRenderer;
    private LineRenderer lightPathWrongWayLineRenderer;
    private int removeLightAdvice = -1;
    private int lightPathFragmentVertexCount = 50;
    private Vector3 nextLightPathFragmentStartingPoint;
    private List<int> flags;

    public GameObject Peanut { get; set; }



    public void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.Log("Can't have multiple instances of Game Manager class");
        }
        else
        {
            _instance = this;
        }
    }

    public void Start()
    {
        flags = new List<int>();
        visibleArrows = new List<Arrow>();
        core = GameObject.Find("Hololens").GetComponent<HololensCore>();
        gm = GameManager.Instance;
    }

    public IEnumerator MoveCompanionSimpleArea(object[] parms)
    {
        Area a1 = (Area)parms[0];
        Area a2 = (Area)parms[1];
        Area a3 = (Area)parms[2];
        Vector3 initPos = Peanut.transform.position;
        Vector3 initRot = Peanut.transform.rotation.eulerAngles;
        Vector3 finalPos = AdviceBasePosition(a2);
        Vector3 finalRot = AdviceRotation(a1, a2, a3, true);
        if (a3 != null)
        {
            finalPos += AdvicePositionOffset(a1, a2, a3, true);
        } else
        {
            finalRot.y -= gm.AdviceConfig.AdviceRotationY[5];
        }

        int vertexCount = 80;
        Vector3 medRot = new Vector3(initRot.x, initRot.y, initRot.z);
        if (a3 != null)
        {
            switch (Utils.GetAction(a1, a2, a3))
            {
                case UserAction.TURN_LEFT: medRot.y += -90f; break;
                case UserAction.TURN_RIGHT: medRot.y += 90f; break;
                case UserAction.GO_FORWARD: medRot.y += 180f; break;
            }
        } else
        {
            medRot.y += 180f;
        }

        //ROTATE
        Quaternion r0 = Peanut.transform.rotation;
        Peanut.transform.LookAt(Utils.AreaToVector3(a2, Peanut.transform.position.y));
        Quaternion r1 = Peanut.transform.rotation;
        Peanut.transform.rotation = r0;
        float turnSpeed = 0f;
        float turnSpeedChange = 0.1f;
        //angle we need to turn
        float angleToTurn;
        while (true)
        {
            angleToTurn = Quaternion.Angle(Peanut.transform.rotation, r1); 
            if (angleToTurn < 5f)
            {
                break;
            }
            turnSpeed = Mathf.Min(angleToTurn, turnSpeed + turnSpeedChange);
            Peanut.transform.rotation = Quaternion.Lerp(Peanut.transform.rotation, r1, Mathf.Clamp01(angleToTurn > 0 ? turnSpeed / angleToTurn : 0f));

            yield return new WaitForSeconds(0.01f);
        }
        
        //MOVE
        for (float ratio = 0; ratio <= 1; ratio += 1f / vertexCount)
        {
            Peanut.transform.position = initPos + (finalPos - initPos) * ratio;
            yield return new WaitForSeconds(0.001f);
        }

        //ANIMATE
        if (a3 == null)
        {
            AnimateCompanion(2);
        } else
        {
            int direction = 1;
            if (Utils.GetAction(a1, a2, a3) == UserAction.TURN_LEFT)
            {
                direction = -1;
            }
            AnimateCompanion(direction);
        }

        //ROTATE
        while (true)
        {
            angleToTurn = Quaternion.Angle(Peanut.transform.rotation, Quaternion.Euler(finalRot));
            if (angleToTurn < 5f)
            {
                break;
            }
            turnSpeed = Mathf.Min(angleToTurn, turnSpeed + turnSpeedChange);
            Peanut.transform.rotation = Quaternion.Lerp(Peanut.transform.rotation, Quaternion.Euler(finalRot), Mathf.Clamp01(angleToTurn > 0 ? turnSpeed / angleToTurn : 0f));

            yield return new WaitForSeconds(0.01f);
        }

        yield return null;
    }

    public IEnumerator MoveCompanionBigArea(object[] parms)
    {
        Area a1 = (Area)parms[0];
        Area a2 = (Area)parms[1];
        Area a3 = (Area)parms[2];
        Area a4 = (Area)parms[3];
        Vector3 initPos = Peanut.transform.position;
        Vector3 initRot = Peanut.transform.rotation.eulerAngles;
        Vector3 finalPos = AdviceBasePosition(a2) + AdvicePositionOffset(a1, a2, a3, true);
        Vector3 finalRot = AdviceRotation(a1, a2, a3, true);
        int vertexCount = 80;
        Vector3 medRot = new Vector3(initRot.x, initRot.y, initRot.z);
        if (a3 != null)
        {
            switch (Utils.GetAction(a1, a2, a3))
            {
                case UserAction.TURN_LEFT: medRot.y -= 90f; break;
                case UserAction.TURN_RIGHT: medRot.y += 90f; break;
                case UserAction.GO_FORWARD: medRot.y += 180f; break;
            }
        }

        //ROTATE
        Quaternion r0 = Peanut.transform.rotation;
        Peanut.transform.LookAt(Utils.AreaToVector3(a2, Peanut.transform.position.y));
        Quaternion r1 = Peanut.transform.rotation;
        Peanut.transform.rotation = r0;
        float turnSpeed = 0f;
        float turnSpeedChange = 0.1f;
        //angle we need to turn
        float angleToTurn;
        while (true)
        {
            angleToTurn = Quaternion.Angle(Peanut.transform.rotation, r1);
            if (angleToTurn < 5f)
            {
                break;
            }
            turnSpeed = Mathf.Min(angleToTurn, turnSpeed + turnSpeedChange);
            Peanut.transform.rotation = Quaternion.Lerp(Peanut.transform.rotation, r1, Mathf.Clamp01(angleToTurn > 0 ? turnSpeed / angleToTurn : 0f));


            yield return new WaitForSeconds(0.01f);
        }

        //MOVE
        for (float ratio = 0; ratio <= 1; ratio += 1f / vertexCount)
        {
            Peanut.transform.position = initPos + (finalPos - initPos) * ratio;
            yield return new WaitForSeconds(0.001f);
        }

        object[] parms2 = new object[3] { a2, a3, a4 };
        StartCoroutine(nameof(MoveCompanionSimpleArea), parms2);
        yield return null;
    }

    public void InstantiateCompanion(Vector3 position, Quaternion rotation)
    {
        Peanut = Instantiate(gm.AdviceConfig.AdvicePrefab, position, rotation);
        //GameManager.SetObscurable(_gm.Peanut);
    }
    public void AnimateCompanion(int state)
    {
        Animator faceAnimator = Peanut.transform.Find("Armature").Find("Bone").Find("Bone.001").Find("Face").GetComponent<Animator>();
        if (state == 0)
        {
            faceAnimator.SetInteger("State", 0);
        }
        else if (state == 2)
        {
            faceAnimator.SetInteger("State", 2);
        }
        else
        {
            int n = 2; // proba = 1/n
            int r = Random.Range(1, n + 1);
            if (r == n)
            {
                faceAnimator.SetInteger("State", 1);
            }
            else
            {
                faceAnimator.SetInteger("State", 0);
            }
        }

        Peanut.GetComponent<Animator>().SetInteger("State", state);
    }

    public void RemovePreviousAdviceIfNecessary()
    {
        if (core.LastArea != null && (gm.NextArea(0) == null || gm.NextArea(1) != null))
        {
            if (gm.GetAdviceName() == Advice.ARROW || gm.GetAdviceName() == Advice.COMPANION)
            {
                RemoveLastAdvice(core.LastArea);
            }
            else if (gm.GetAdviceName() == Advice.LIGHT)
            {
                RemoveWrongWayLightAdvice();
                if (removeLightAdvice == 1)
                {
                    RemoveLightAdvice();
                }
                else
                {
                    removeLightAdvice++;
                }
            }
        }
    }

    public void UpdateAdviceInError()
    {
        if (gm.GetAdviceName() == Advice.ARROW)
        {
            Vector3 position = AdviceBasePosition(gm.NextArea(0)) + AdvicePositionOffset(core.CurrentArea, gm.NextArea(0), gm.NextArea(1), true);
            Vector3 rotation = AdviceRotation(core.CurrentArea, gm.NextArea(0), gm.NextArea(1), true);
            AddArrow(gm.NextArea(0), position, rotation);
        }
        else if (gm.GetAdviceName() == Advice.COMPANION)
        {
            AnimateCompanion(0);
            object[] parms = new object[3] { core.CurrentArea, gm.NextArea(0), gm.NextArea(1) };
            StopCoroutine(nameof(MoveCompanionSimpleArea));
            StartCoroutine(nameof(MoveCompanionSimpleArea), parms);
        }
    }

    public void DisplayNextAdvice(bool errorCorrected)
    {
        Area nextArea = gm.NextArea(0);
        Area nextNextArea = gm.NextArea(1);
        Vector3 position;
        Vector3 rotation;
        if (gm.GetAdviceName() == Advice.ARROW)
        {
            //if (walkedPath.Count() != 0)
            //{
            position = AdviceBasePosition(nextArea);
            if (nextNextArea != null)
            {
                position += AdvicePositionOffset(core.CurrentArea, nextArea, nextNextArea, true);
            }
            if (nextArea != null && nextNextArea != null)
            {

                rotation = AdviceRotation(core.CurrentArea, nextArea, nextNextArea, true);
                AddArrow(nextArea, position, rotation);
            }
            //}
        }
        else if (gm.GetAdviceName() == Advice.LIGHT)
        {
            DrawLightPath(core.CurrentArea, nextArea, nextNextArea, gm.NextArea(2));
        }
        else if (gm.GetAdviceName() == Advice.COMPANION)
        {
            if (Peanut == null)
            {
                position = AdviceBasePosition(nextArea) + AdvicePositionOffset(core.CurrentArea, nextArea, nextNextArea, true);
                rotation = AdviceRotation(core.CurrentArea, nextArea, nextNextArea, true);

                InstantiateCompanion(position, Quaternion.Euler(rotation));

                int direction = 1;
                if (Utils.GetAction(core.CurrentArea, nextArea, nextNextArea) == UserAction.TURN_LEFT)
                {
                    direction = -1;
                }
                AnimateCompanion(direction);
            }
            else
            {
                if (nextArea == null || nextNextArea == null)
                {
                    return;
                }
                if (nextArea.InBigArea() && nextNextArea.InBigArea())
                {
                    AnimateCompanion(0);
                    object[] parms = new object[4] { core.CurrentArea, nextArea, nextNextArea, gm.NextArea(2)};
                    StopCoroutine(nameof(MoveCompanionSimpleArea));
                    StartCoroutine(nameof(MoveCompanionBigArea), parms);
                }
                else if (errorCorrected || !nextArea.InBigArea())
                {
                    errorCorrected = false;
                    AnimateCompanion(0);
                    object[] parms = new object[3] { core.CurrentArea, nextArea, nextNextArea };
                    StopCoroutine(nameof(MoveCompanionSimpleArea));
                    StartCoroutine(nameof(MoveCompanionSimpleArea), parms);
                }
            }

        }
    }

    public void NotWorsening()
    {
        if (gm.GetAdviceName() == Advice.ARROW)
        {
            RemoveAdviceAtArea(core.LastArea);
            gm.RemainingPath.RemoveFirst();
        }
        else if (gm.GetAdviceName() == Advice.LIGHT)
        {
            RemoveWrongWayLightAdvice();
            if (removeLightAdvice == 1)
            {
                RemoveLightAdvice();
            }
            else
            {
                removeLightAdvice++;
            }
            DrawLightPath(core.CurrentArea, gm.NextArea(0), gm.NextArea(1), gm.NextArea(2));

        }
        else
        {
            RemoveAdviceAtArea(core.LastArea);
            gm.RemainingPath.RemoveFirst();
        }
    }

    public void Worsening()
    {
        Vector3 position, rotation;
        RemoveAllAdvice();
        if (gm.GetAdviceName() == Advice.ARROW)
        {
            position = AdviceBasePosition(core.CurrentArea) + WrongWayAdvicePositionOffset(core.CurrentArea, gm.NextArea(0));
            rotation = WrongWayAdviceRotation(core.CurrentArea, gm.NextArea(0));
            AddWrongWayAdvice(core.CurrentArea, position, rotation);
        }
        else if (gm.GetAdviceName() == Advice.LIGHT)
        {
            removeLightAdvice = -1;
            DrawLightPath(core.CurrentArea, gm.NextArea(0), gm.NextArea(1), gm.NextArea(2));
            // _gm.DrawLightPath(Converter.AreaToVector3(NextArea(0), 0.2f), Converter.AreaToVector3(NextArea(1), 0.2f));
            position = AdviceBasePosition(core.CurrentArea) + WrongWayAdvicePositionOffset(core.CurrentArea, gm.NextArea(0));
            rotation = WrongWayAdviceRotation(core.CurrentArea, gm.NextArea(0));
            AddWrongWayAdvice(core.CurrentArea, position, rotation);
            //_gm.DrawWrongWayLightPath(_core.CurrentArea, WrongWayAdvicePositionOffset(_core.CurrentArea, NextArea(0)), WrongWayAdviceRotation(_core.CurrentArea, NextArea(0)).y);

        }
        else
        {
            position = AdviceBasePosition(core.CurrentArea) + WrongWayAdvicePositionOffset(core.CurrentArea, gm.NextArea(0));
            rotation = WrongWayAdviceRotation(core.CurrentArea, gm.NextArea(0));
            AddWrongWayAdvice(core.CurrentArea, position, rotation);
        }
    }

    public void RemoveLastAdvice(Area lastArea)
    {
        if (gm.GetAdviceName() == Advice.ARROW || gm.GetAdviceName() == Advice.COMPANION)
        {
            RemoveAdviceAtArea(lastArea);
        }
    }

    public Vector3 AdviceBasePosition(Area area)
    {
        return Utils.AreaToVector3(area, gm.AdviceConfig.AdviceBaseHeight);
    }

    public void AddWrongWayAdvice(Area area, Vector3 position, Vector3 rotation)
    {
        GameObject wrongWayAdvicePrefab = gm.AdviceConfig.WrongWayAdvicePrefab;
        Arrow advice = new Arrow(area, wrongWayAdvicePrefab, position, rotation);
        AddArrow(advice);
    }

    public Vector3 WrongWayAdviceRotation(Area currentArea, Area lastMatchingArea)
    {
        return AdviceRotation(lastMatchingArea, currentArea, null, false);
    }

    public Vector3 WrongWayAdvicePositionOffset(Area currentArea, Area lastMatchingArea)
    {
        return AdvicePositionOffset(currentArea, lastMatchingArea, null, false);
    }

    public void AddArrow(Area area, Vector3 position, Vector3 rotation)
    {
        GameObject advicePrefab = gm.AdviceConfig.AdvicePrefab;
        Arrow advice = new Arrow(area, advicePrefab, position, rotation);
        AddArrow(advice);
    }


    public Vector3 AdviceRotation(Area from, Area at, Area to, bool onRightPath)
    {
        Direction d1 = Utils.GetDirection(from, at);
        Direction d2;
        UserAction action;
        Vector3 r;
        if (to != null)
        {
            r = gm.AdviceConfig.AdvicePrefab.transform.rotation.eulerAngles;
            d2 = Utils.GetDirection(at, to);
            action = Utils.GetAction(d1, d2);
        } else
        {
            if (onRightPath)
            {
                r = gm.AdviceConfig.AdvicePrefab.transform.rotation.eulerAngles;
                action = UserAction.GO_FORWARD;
            } else
            {
                r = gm.AdviceConfig.WrongWayAdvicePrefab.transform.rotation.eulerAngles;
                action = UserAction.GO_BACKWARD;
            }
        }
        if (d1 == Direction.LEFT)
        {
            r.y += gm.AdviceConfig.AdviceRotationY[0]; 
        } else if (d1 == Direction.RIGHT)
        {
            r.y += gm.AdviceConfig.AdviceRotationY[1];
        }
        else if (d1 == Direction.DOWN)
        {
            r.y += gm.AdviceConfig.AdviceRotationY[2];
        }
        if (action == UserAction.TURN_LEFT)
        {
            r.y += gm.AdviceConfig.AdviceRotationY[3];
        }
        else if (action == UserAction.TURN_RIGHT)
        {
            r.y += gm.AdviceConfig.AdviceRotationY[4];
        }
        else if (action == UserAction.GO_FORWARD)
        {
            r.y += gm.AdviceConfig.AdviceRotationY[5];
        } else
        {
            r.y += gm.AdviceConfig.AdviceRotationY[6];
        }
        r.x += gm.AdviceConfig.AdviceRotationX;
        if (!from.InBigArea() && at.InBigArea() && to != null)
        {

            if (Mathf.Abs(at.Line - to.Line) + Mathf.Abs(at.Column - to.Column) == 2)
            {
                if (at.Equals(new Area(3, 3)) || at.Equals(new Area(2, 4)))
                {
                    r.y += 45f;
                }
                if (at.Equals(new Area(2, 3)) || at.Equals(new Area(3, 4)))
                {
                    r.y -= 45f;
                }
            }
        }
        return r;
    }

    public Vector3 AdvicePositionOffset(Area from, Area at, Area to, bool onRightPath)
    {
        Direction d1 = Utils.GetDirection(from, at);
        Direction d2;
        UserAction action;
        if (to != null)
        {
            d2 = Utils.GetDirection(at, to);
            action = Utils.GetAction(d1, d2);
        }
        else
        {
            if (onRightPath)
            {
                action = UserAction.GO_FORWARD;
            } else
            {
                action = UserAction.GO_BACKWARD;
            }
        }
        float baseOffset = gm.AdviceBaseOffset;
      
        Vector3 offset = Vector3.zero;

        int eps = (gm.GetAdviceName() == Advice.ARROW) ? 0 : 1;

        if (action == UserAction.GO_FORWARD)
        {
            switch (d1)
            {
                case Direction.UP: offset += new Vector3(1, 0, eps) * baseOffset; break;
                case Direction.DOWN: offset += new Vector3(-1, 0, -eps) * baseOffset; break;
                case Direction.LEFT: offset += new Vector3(-eps, 0, 1) * baseOffset; break;
                case Direction.RIGHT: offset += new Vector3(eps, 0, -1) * baseOffset; break;
            }
        }
        else if (action == UserAction.GO_BACKWARD)
        {
            switch (d1)
            {
                case Direction.UP: offset += new Vector3(-1, 0, 0) * baseOffset; break;
                case Direction.DOWN: offset += new Vector3(1, 0, 0) * baseOffset; break;
                case Direction.LEFT: offset += new Vector3(0, 0, -1) * baseOffset; break;
                case Direction.RIGHT: offset += new Vector3(0, 0, 1) * baseOffset; break;
            }
        }
        else if (action == UserAction.TURN_LEFT)
        {
            switch (d1)
            {
                case Direction.UP: offset += new Vector3(eps, 0, 1) * baseOffset; break;
                case Direction.DOWN: offset += new Vector3(-eps, 0, -1) * baseOffset; break;
                case Direction.LEFT: offset += new Vector3(-1, 0, eps) * baseOffset; break;
                case Direction.RIGHT: offset += new Vector3(1, 0, -eps) * baseOffset; break;
            }
        }
        else if (action == UserAction.TURN_RIGHT)
        {
            switch (d1)
            {
                case Direction.UP: offset += new Vector3(eps, 0, -1) * baseOffset; break;
                case Direction.DOWN: offset += new Vector3(-eps, 0, 1) * baseOffset; break;
                case Direction.LEFT: offset += new Vector3(1, 0, eps) * baseOffset; break;
                case Direction.RIGHT: offset += new Vector3(-1, 0, -eps) * baseOffset; break;
            }
        }
        return offset;
    }
    public void AddArrow(Arrow arrow)
    {
        visibleArrows.Add(arrow);
    }

    IEnumerator DrawLightPathTwoSegments(object[] parms)
    {
        List<Vector3> positionList = new List<Vector3>();
        Vector3 at = (Vector3)parms[0];
        Vector3 to = (Vector3)parms[1];
        Vector3 toto = (Vector3)parms[2];
        lightPathLineRenderer.positionCount = 0;
        for (float ratio = 0; ratio <= 1; ratio += 1f / lightPathFragmentVertexCount)
        {
            Vector3 tangent1 = Vector3.Lerp(nextLightPathFragmentStartingPoint, at, ratio);
            Vector3 tangent2 = Vector3.Lerp(at, (at + to) / 2f, ratio);
            Vector3 curve = Vector3.Lerp(tangent1, tangent2, ratio);
            positionList.Add(curve);
            lightPathLineRenderer.positionCount++;
            lightPathLineRenderer.SetPosition(lightPathLineRenderer.positionCount - 1, curve);
            yield return new WaitForSeconds(GameManager.Instance.lightPathDelayInSeconds);
        }
        flags.Add(positionList.Count);
        if (toto != null)
        {
            for (float ratio = 0; ratio <= 1; ratio += 1f / lightPathFragmentVertexCount)
            {
                Vector3 tangent1 = Vector3.Lerp((at + to) / 2f, to, ratio);
                Vector3 tangent2 = Vector3.Lerp(to, (to + toto) / 2f, ratio);
                Vector3 curve = Vector3.Lerp(tangent1, tangent2, ratio);
                positionList.Add(curve);
                lightPathLineRenderer.positionCount++;
                lightPathLineRenderer.SetPosition(lightPathLineRenderer.positionCount - 1, curve);
                yield return new WaitForSeconds(GameManager.Instance.lightPathDelayInSeconds);
            }

            nextLightPathFragmentStartingPoint = (to + toto) / 2f;
        }
        else
        {
            nextLightPathFragmentStartingPoint = (at + to) / 2f;
        }
        //flags.Add(positionList.Count);
        flags.Add(positionList.Count - flags[flags.Count - 1]);
        yield return null;
    }

    IEnumerator DrawLightPathOneSegment(object[] parms)
    {
        List<Vector3> positionList = new List<Vector3>();
        Vector3 at = (Vector3)parms[0];
        Vector3 to = (Vector3)parms[1];
        Vector3 toto = (Vector3)parms[2];
        for (float ratio = 0; ratio <= 1; ratio += 1f / lightPathFragmentVertexCount)
        {
            Vector3 tangent1 = Vector3.Lerp((at + to) / 2f, to, ratio);
            Vector3 tangent2 = Vector3.Lerp(to, (to + toto) / 2f, ratio);
            Vector3 curve = Vector3.Lerp(tangent1, tangent2, ratio);
            positionList.Add(curve);
            lightPathLineRenderer.positionCount++;
            lightPathLineRenderer.SetPosition(lightPathLineRenderer.positionCount - 1, curve);
            yield return new WaitForSeconds(GameManager.Instance.lightPathDelayInSeconds);
        }

        nextLightPathFragmentStartingPoint = (to + toto) / 2f;
        flags.Add(positionList.Count);
        yield return null;
    }

    public void DrawLightPath(Area fromArea, Area atArea, Area toArea, Area totoArea)
    {
        float h = GameManager.Instance.AdviceConfig.AdviceBaseHeight;
        Vector3 from = Utils.AreaToVector3(fromArea, h);
        Vector3 at = Utils.AreaToVector3(atArea, h);
        Vector3 to;
        Vector3 toto;
        if (toArea == null)
        {
            to = at;
        }
        else
        {
            to = Utils.AreaToVector3(toArea, h);
        }

        if (totoArea == null)
        {
            toto = to;
        }
        else
        {
            toto = Utils.AreaToVector3(totoArea, h);
        }

        if (lightPathLineRenderer == null)
        {
            GameObject pathLine = new GameObject();
            pathLine.name = "LightPath";
            pathLine.transform.position = from;
            lightPathLineRenderer = pathLine.AddComponent<LineRenderer>();
            lightPathLineRenderer.material = GameManager.Instance.lightPathMaterial;
            Utils.SetObscurable(lightPathLineRenderer.gameObject);
            lightPathLineRenderer.startColor = Color.white;
            lightPathLineRenderer.endColor = Color.white;
            lightPathLineRenderer.startWidth = GameManager.Instance.lightPathWidth;
            lightPathLineRenderer.endWidth = GameManager.Instance.lightPathWidth;
            pathLine.transform.rotation = Quaternion.Euler(pathLine.transform.rotation.eulerAngles + new Vector3(90f, 0, 0));
            lightPathLineRenderer.alignment = LineAlignment.TransformZ;
            lightPathLineRenderer.numCornerVertices = 0;
            nextLightPathFragmentStartingPoint = from;
            object[] parms = new object[3] { at, to, toto };
            StopCoroutine(nameof(DrawLightPathTwoSegments));
            StopCoroutine(nameof(DrawLightPathOneSegment));
            StartCoroutine(nameof(DrawLightPathTwoSegments), parms);
            return;
        }
        else
        {

            List<Vector3> positionList = new List<Vector3>();
            if (lightPathLineRenderer.positionCount == 0)
            {
                nextLightPathFragmentStartingPoint = from;
                object[] parms = new object[3] { at, to, toto };
                StopCoroutine(nameof(DrawLightPathTwoSegments));
                StopCoroutine(nameof(DrawLightPathOneSegment));
                StartCoroutine(nameof(DrawLightPathTwoSegments), parms);

            }
            else
            {
                object[] parms = new object[3] { at, to, toto };
                StopCoroutine(nameof(DrawLightPathTwoSegments));
                StopCoroutine(nameof(DrawLightPathOneSegment));
                StartCoroutine(nameof(DrawLightPathOneSegment), parms);
            }


        }

    }
    public void RemoveAdviceAtArea(Area area)
    {
        foreach (Arrow advice in visibleArrows)
        {
            if (advice.Area.Equals(area))
            {
                advice.Remove();
            }
        }

    }

    public void RemoveAllAdvice()
    {
        if (GameManager.Instance.Advice == Advice.ARROW || GameManager.Instance.Advice == Advice.COMPANION)
        {
            foreach (Arrow advice in visibleArrows)
            {
                advice.Remove();
            }
        }
        else if (GameManager.Instance.Advice == Advice.LIGHT)
        {
            if (lightPathLineRenderer != null)
            {
                Vector3[] newPos = new Vector3[0];
                lightPathLineRenderer.positionCount = 0;
                lightPathLineRenderer.SetPositions(newPos);
            }
            if (lightPathWrongWayLineRenderer != null)
            {
                Vector3[] newPos = new Vector3[0];
                lightPathWrongWayLineRenderer.positionCount = 0;
                lightPathWrongWayLineRenderer.SetPositions(newPos);
            }
            foreach (Arrow advice in visibleArrows)
            {
                advice.Remove();
            }
            flags.Clear();
        }
    }

    public void RemoveWrongWayLightAdvice()
    {
        foreach (Arrow advice in visibleArrows)
        {
            advice.Remove();
        }
    }

    public void RemoveLightAdvice()
    {
        if (lightPathLineRenderer != null)
        {
            int n = flags[0];
            Vector3[] newPos = new Vector3[lightPathLineRenderer.positionCount - n];
            for (int i = n; i < lightPathLineRenderer.positionCount; i++)
            {
                newPos[i - n] = lightPathLineRenderer.GetPosition(i);
            }
            lightPathLineRenderer.positionCount -= n;
            lightPathLineRenderer.SetPositions(newPos);
            flags.RemoveAt(0);
        }
        if (lightPathWrongWayLineRenderer != null)
        {
            Vector3[] newPos = new Vector3[0];
            lightPathWrongWayLineRenderer.positionCount = 0;
            lightPathWrongWayLineRenderer.SetPositions(newPos);
        }
        foreach (Arrow advice in visibleArrows)
        {
            advice.Remove();
        }
    }

}
