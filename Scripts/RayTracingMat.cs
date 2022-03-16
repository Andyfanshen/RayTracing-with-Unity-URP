using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RayTracingMat : MonoBehaviour
{
    public Color albedo = Color.gray;
    public Color specular = Color.black;
    [Range(0.05f, 0.95f)]
    public float smoothness = 0.2f;
    [Range(0.0f, 10.0f), Tooltip("Index of refraction. Reference:Water=1.33, Glass=1.5~1.9, Diamond=2.42")]
    public float IOR = 0.0f;
    public Color emission = Color.black;
}
