using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Makes the link between the area detector gameobject equipped with a collider and the abstract Area class */
public class AreaDetector : MonoBehaviour
{
    Renderer r;
    bool fadeIn = false;
    bool fadeOut = false;
    float fadeOutDuration;

    public int line, column;
    public Texture Texture { get; set; }

    public Area Area
    {
        get { return new Area(line, column); }
    }

    public void Start()
    {
        fadeOutDuration = GameObject.Find("SimulationManager").GetComponent<SimulationManager>().landmarkFadeOutDuration;
    }
    public void DisplayLandmarks(HololensTracker.Direction from, bool checkBigArea)
    {
        if (Area.InBigArea() && checkBigArea)
        {
            foreach (Area area in Area.BigAreaAreas())
            {
                area.GetAreaDetector().DisplayLandmarks(from, false);
            }
        }
        List<Landmark.LandmarkPosition> landmarksToDisplay = GetLandmarksFromDirection(from);
        if (Texture != null)
        {
            Component[] renderers = GetComponentsInChildren(typeof(Renderer));
            foreach (Component renderer in renderers)
            {
                Landmark landmark = renderer.gameObject.GetComponent<Landmark>();
                if (landmarksToDisplay.Contains(landmark.position))
                {
                    if (!((Renderer)renderer).enabled)
                    {
                        object[] parms = new object[1] { (Renderer)renderer };
                        ((Renderer)renderer).material.mainTexture = Texture;
                        ToFadeMode(((Renderer)renderer).material);
                        SimulationManager.SetObscurable(renderer.gameObject);
                        r = (Renderer)renderer;
                        r.enabled = true;
                        color = r.material.color;
                        alpha = 0f;
                        fadeIn = true;
                    }
                }
            }
        }
    }

    public void ToFadeMode(Material material)
    {
        material.SetOverrideTag("RenderType", "Fade");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    float alpha = 0f;
    Color color;

    public void Update()
    {
        if (fadeIn)
        {

            Color newColor = new Color(color.r, color.g, color.b, alpha);
            alpha += Time.deltaTime / fadeOutDuration;
            if (alpha >= 1)
            {
                fadeIn = false;
                return;
            }
            r.material.SetColor("_Color", newColor);
        } else if (fadeOut)
        {
            if (alpha <= 0)
            {
                r.enabled = false;
                fadeOut = false;
                return;
            }
            Color newColor = new Color(color.r, color.g, color.b, alpha);
            alpha -= Time.deltaTime / fadeOutDuration;
            r.material.SetColor("_Color", newColor);
        }
    }

    public void RemoveLandmarks(bool checkBigArea)
    {
        if (Area.InBigArea() && checkBigArea)
        {
            foreach (Area area in Area.BigAreaAreas())
            {
                area.GetAreaDetector().RemoveLandmarks(false);
            }
            return;
        }
        Component[] renderers = GetComponentsInChildren(typeof(Renderer));
        foreach (Component renderer in renderers)
        {
            if (((Renderer)renderer).enabled)
            {
                r = (Renderer)renderer;
                color = r.material.color;
                alpha = 1f;
                fadeOut = true;
            }
        }
    }

    private List<Landmark.LandmarkPosition> GetLandmarksFromDirection(HololensTracker.Direction from)
    {
        List<Landmark.LandmarkPosition> res = new List<Landmark.LandmarkPosition>();
        if (Area.InBigArea())
        {
            if (Area.line == 2 && Area.column == 3)
            {
                res.Add(Landmark.LandmarkPosition.TOP_RIGHT_FACE_DOWN);
            } else if (Area.line == 2 && Area.column == 4)
            {
                res.Add(Landmark.LandmarkPosition.BOTTOM_RIGHT_FACE_LEFT);
            } else if (Area.line == 3 && Area.column == 3)
            {
                res.Add(Landmark.LandmarkPosition.TOP_LEFT_FACE_RIGHT);
            } else
            {
                res.Add(Landmark.LandmarkPosition.BOTTOM_LEFT_FACE_UP);
            }
        } else
        {
            if (from == HololensTracker.Direction.DOWN || from == HololensTracker.Direction.UP)
            {
                res.Add(Landmark.LandmarkPosition.BOTTOM_LEFT_FACE_UP);
                res.Add(Landmark.LandmarkPosition.BOTTOM_RIGHT_FACE_UP);
                res.Add(Landmark.LandmarkPosition.TOP_LEFT_FACE_DOWN);
                res.Add(Landmark.LandmarkPosition.TOP_RIGHT_FACE_DOWN);
            }
            else
            {

                res.Add(Landmark.LandmarkPosition.BOTTOM_LEFT_FACE_RIGHT);
                res.Add(Landmark.LandmarkPosition.BOTTOM_RIGHT_FACE_LEFT);
                res.Add(Landmark.LandmarkPosition.TOP_LEFT_FACE_RIGHT);
                res.Add(Landmark.LandmarkPosition.TOP_RIGHT_FACE_LEFT);
            }
        }
     
        return res;
    }
}
