using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MyRayTracing : VolumeComponent, IPostProcessComponent
{
    public ComputeShader RayTracingShader;

    public BoolParameter Enable = new BoolParameter(false);

    public TextureParameter SkyboxTexture = new TextureParameter(null);

    [Tooltip("Accumulate sampling")]
    public BoolParameter AccSample = new BoolParameter(false);

    [Range(0, 10), Tooltip("Ray bounce times between objects")]
    public IntParameter rayBounce = new IntParameter(2);

    public BoolParameter CreateTestScene = new BoolParameter(false);

    [Tooltip("Random seed for test scene")]
    public IntParameter SceneSeed = new IntParameter(0);

    private static bool isSceneCreated = false;

    private static ComputeBuffer _sphereBuffer = null;

    public bool IsActive()
    {
        if(Enable.value)
        {
            if (CreateTestScene.value && !isSceneCreated)
            {
                CreateSpheres();
                isSceneCreated = true;
            }
            else if (isSceneCreated && !CreateTestScene.value)
            {
                isSceneCreated = false;
            }
        }
        return Enable.value;
    }

    public bool IsTileCompatible() => false;

    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public Vector3 emission;
    }

    private void CreateSpheres()
    {
        Random.InitState(SceneSeed.value);

        Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
        uint SpheresMax = 100;
        float SpherePlacementRadius = 100.0f;
        List<Sphere> spheres = new List<Sphere>();

        for (int i = 0; i < SpheresMax; i++)
        {
            Sphere sphere = new Sphere();
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }

            Color color = Random.ColorHSV();
            float chance = Random.value;
            if(chance < 0.8f)
            {
                bool metal = Random.value < 0.4f;
                sphere.albedo = metal ? Vector4.zero : new Vector4(color.r, color.g, color.b);
                sphere.specular = metal ? new Vector4(color.r, color.g, color.b) : new Vector4(0.04f, 0.04f, 0.04f);
                sphere.smoothness = Random.value;
            }
            else
            {
                Color emission = Random.ColorHSV(0, 1, 0, 1, 3.0f, 8.0f);
                sphere.emission = new Vector3(emission.r, emission.g, emission.b);
            }

            spheres.Add(sphere);

        SkipSphere: continue;
        }

        if (_sphereBuffer != null) _sphereBuffer.Release();
        if (spheres.Count > 0)
        {
            _sphereBuffer = new ComputeBuffer(spheres.Count, 56);
            _sphereBuffer.SetData(spheres);
        }
        if (_sphereBuffer != null)
            RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
    }

}
