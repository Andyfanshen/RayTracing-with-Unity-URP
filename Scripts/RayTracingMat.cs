using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RayTracingMat : MonoBehaviour
{
    public Texture2D baseTexture = null;
    public Texture2D normalTexture = null;
    public Texture2D MASKTexture = null;

    public void AddTextures(List<RenderTargetIdentifier> baseMaps, List<RenderTargetIdentifier> normalMaps, List<RenderTargetIdentifier> MASKMaps)
    {
        baseMaps.Add(new RenderTargetIdentifier(baseTexture));
        normalMaps.Add(new RenderTargetIdentifier(normalTexture));
        MASKMaps.Add(new RenderTargetIdentifier(MASKTexture));
    }
}
