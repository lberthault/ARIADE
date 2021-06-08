using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Makes the link between the area detector gameobject equipped with a collider and the abstract Area class */
public class AreaDetector : MonoBehaviour
{

    public int line, column;
    public Texture texture;
    public Material baseMaterial;

    public Area Area
    {
        get { return new Area(line, column); }
    }

    public void DisplayLandmarks(bool display)
    {
        Component[] renderers = GetComponentsInChildren(typeof(Renderer));
        foreach (Component renderer in renderers)
        {
            ((Renderer)renderer).enabled = display;
            //((Renderer)renderer).material = new Material(baseMaterial);
            ((Renderer)renderer).material.mainTexture = texture;
            SimulationManager.SetObscurable(renderer.gameObject);
        }
    }

}
