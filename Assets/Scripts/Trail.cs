using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trail : MonoBehaviour
{
    private TrailRenderer trailRenderer;
    public Material trailMaterial;
    public float trailLifetime = 9999f;
    public float trailSize = 0.05f;
    public float trailSensibility = 0.01f;
    private bool initiated = false;

    public bool Initiated
    {
        get { return initiated; }
    }
            
    public void Initiate()
    {
        trailRenderer = (TrailRenderer)gameObject.AddComponent(typeof(TrailRenderer));
        trailRenderer.startWidth = trailSize;
        trailRenderer.endWidth = trailSize;
        trailRenderer.time = trailLifetime;
        trailRenderer.minVertexDistance = trailSensibility;
        trailRenderer.material = trailMaterial;
        initiated = true;
    }

    public void Clear()
    {
        if (initiated)
        {
            trailRenderer.Clear();
        }
    }

    public void CheckModifications()
    {
        if (initiated)
        {
            if (trailRenderer.time != trailLifetime)
            {
                trailRenderer.time = trailLifetime;
            }
            if (trailRenderer.startWidth != trailSize)
            {
                trailRenderer.startWidth = trailSize;
                trailRenderer.endWidth = trailSize;
            }
            if (trailRenderer.minVertexDistance != trailSensibility)
            {
                trailRenderer.minVertexDistance = trailSensibility;
            }
            if (trailRenderer.material != trailMaterial)
            {
                trailRenderer.material = trailMaterial;
            }
        }
    }
}
