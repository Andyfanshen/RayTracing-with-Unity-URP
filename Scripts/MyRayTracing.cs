using System.Collections.Generic;
using System.Linq;
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

    private static List<MeshObject> _meshObjects = new List<MeshObject>();

    private static List<Vector3> _vertices = new List<Vector3>();

    private static List<int> _indices = new List<int>();

    private static readonly int mapWidth = 1024;
    private static readonly int mapHeight = 1024;
    private static List<RenderTargetIdentifier> _baseMaps = new List<RenderTargetIdentifier>();
    private static List<RenderTargetIdentifier> _normalMaps = new List<RenderTargetIdentifier>();
    private static List<RenderTargetIdentifier> _MASKMaps = new List<RenderTargetIdentifier>();

    private static ComputeBuffer _meshObjectBuffer;

    private static ComputeBuffer _vertexBuffer;

    private static ComputeBuffer _indexBuffer;

    private static Texture2DArray _baseMapArray = null;
    private static Texture2DArray _normalMapArray = null;
    private static Texture2DArray _MASKMapArray = null;

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
                _sphereBuffer.Release();
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

    struct MeshObject
    {
        public Matrix4x4 localToWorldMatrix;
        public int indices_offset;
        public int indices_count;
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

    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride) where T : struct
    {
        if(buffer != null)
        {
            if(data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }

        if(data.Count != 0)
        {
            if(buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }

            buffer.SetData(data);
        }
    }

    private bool CreateTexture3D(CommandBuffer cmd, List<RenderTargetIdentifier> textures, ref Texture2DArray texture2DArray)
    {
        if(textures.Count>0)
        {
            texture2DArray = new Texture2DArray(mapWidth, mapHeight, textures.Count, TextureFormat.RGBA32, true, true);
            for (int i = 0; i < textures.Count; i++)
            {
                cmd.ConvertTexture(textures[i], 0, texture2DArray, i);
            }

            return true;
        }
        return false;
    }

    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if(buffer != null)
        {
            RayTracingShader.SetBuffer(0, name, buffer);
        }
    }

    public void SetRayTracingObjectsParameters(CommandBuffer cmd)
    {
        _meshObjects.Clear();
        _vertices.Clear();
        _indices.Clear();
        _baseMaps.Clear();
        _normalMaps.Clear();
        _MASKMaps.Clear();

        var _rayTracingObjects = GameObject.FindGameObjectsWithTag("RayTracing");

        foreach (var obj in _rayTracingObjects)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;

            int firstVertex = _vertices.Count;
            _vertices.AddRange(mesh.vertices);

            int firstIndex = _indices.Count;
            var indices = mesh.GetIndices(0);
            _indices.AddRange(indices.Select(index => index + firstVertex));

            _meshObjects.Add(new MeshObject()
            {
                localToWorldMatrix = obj.transform.localToWorldMatrix,
                indices_offset = firstIndex,
                indices_count = indices.Length
            });

            obj.GetComponent<RayTracingMat>().AddTextures(_baseMaps, _normalMaps, _MASKMaps);
        }

        CreateComputeBuffer(ref _meshObjectBuffer, _meshObjects, 72);
        CreateComputeBuffer(ref _vertexBuffer, _vertices, 12);
        CreateComputeBuffer(ref _indexBuffer, _indices, 4);

        SetComputeBuffer("_MeshObjects", _meshObjectBuffer);
        SetComputeBuffer("_Vertices", _vertexBuffer);
        SetComputeBuffer("_Indices", _indexBuffer);

        if (CreateTexture3D(cmd, _baseMaps, ref _baseMapArray))
            RayTracingShader.SetTexture(0, "_baseMapArray", _baseMapArray);
        if (CreateTexture3D(cmd, _normalMaps, ref _normalMapArray))
            RayTracingShader.SetTexture(0, "_normalMapArray", _normalMapArray);
        if (CreateTexture3D(cmd, _MASKMaps, ref _MASKMapArray))
            RayTracingShader.SetTexture(0, "_MaskMapArray", _MASKMapArray);
    }
}
