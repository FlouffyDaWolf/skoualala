using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Procedural Generation Method/Noise")]
public class Noise : ProceduralGenerationMethod
{
    [Header("Roof Noise Parameters")]
    public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    [Range(0f, 1f)] public float frequency = 0.04f;
    [Range(0f, 1f)] public float amplitude = 0.981f;

    [Header("Roof Fractal Parameters")]
    public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.None;
    [Range(1, 10)] public int octaves = 2;
    [Range(0f, 5f)] public float lacunarity = 1.2f;
    [Range(0f, 1f)] public float persistence = 0.5f;

    [Header("Floor Noise Parameters")]
    public FastNoiseLite.NoiseType floornoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    [Range(0f, 1f)] public float floorfrequency = 0.04f;
    [Range(0f, 1f)] public float flooramplitude = 0.981f;

    [Header("Floor Fractal Parameters")]
    public FastNoiseLite.FractalType floorfractalType = FastNoiseLite.FractalType.None;
    [Range(1, 10)] public int flooroctaves = 2;
    [Range(0f, 5f)] public float floorlacunarity = 1.2f;
    [Range(0f, 1f)] public float floorpersistence = 0.5f;

    [Header("Heights")]
    [Range(0, 20)] public int offset = 0;

    Mesh mesh;
    Mesh roofMesh;
    GameObject floor;
    GameObject roof;
    public Material mat;

    FastNoiseLite noiseRoof;
    FastNoiseLite noiseFloor;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        noiseRoof = new FastNoiseLite();
        noiseRoof.SetNoiseType(noiseType);
        noiseRoof.SetFrequency(frequency);
        noiseRoof.SetDomainWarpAmp(amplitude);

        noiseRoof.SetFractalType(fractalType);
        noiseRoof.SetFractalOctaves(octaves);
        noiseRoof.SetFractalLacunarity(lacunarity);
        noiseRoof.SetFractalGain(persistence);

        noiseFloor = new FastNoiseLite();
        noiseFloor.SetNoiseType(floornoiseType);
        noiseFloor.SetFrequency(floorfrequency);
        noiseFloor.SetDomainWarpAmp(flooramplitude);

        noiseFloor.SetFractalType(floorfractalType);
        noiseFloor.SetFractalOctaves(flooroctaves);
        noiseFloor.SetFractalLacunarity(floorlacunarity);
        noiseFloor.SetFractalGain(floorpersistence);

        DestroyImmediate(GameObject.Find("floor"));

        mesh = new Mesh();
        mesh.name = "floor";
        mesh.indexFormat = IndexFormat.UInt32;
        floor = new GameObject("floor", typeof(MeshRenderer), typeof(MeshFilter));

        floor.GetComponent<MeshFilter>().mesh = mesh;
        floor.GetComponent<MeshRenderer>().material = mat;

        List<Vector3> floorVertices = new List<Vector3>();
        List<int> floorTriangles = new List<int>();

        List<Vector3> roofVertices = new List<Vector3>();
        List<int> roofTriangles = new List<int>();

        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Lenght; y++)
            {
                float floorHeight = -(noiseFloor.GetNoise(x, y) + 1f) * 10f + (int)Math.Floor((float)offset / 2);
                float roofHeight = (noiseRoof.GetNoise(x, y) + 1f) * 10f - (int)Math.Ceiling((float)offset / 2);

                if (Math.Abs(roofHeight - floorHeight) <= 3 
                    || y == Grid.Lenght - 1
                    || y == 0
                    || x == Grid.Width - 1
                    || x == 0)
                {
                    float joinedHeight = (floorHeight + roofHeight) / 2f;
    
                    floorVertices.Add(new Vector3(x, joinedHeight, y));
                    roofVertices.Add(new Vector3(x, joinedHeight, y));
                }
                else
                {
                    floorVertices.Add(new Vector3(x, floorHeight, y));
                    roofVertices.Add(new Vector3(x, roofHeight, y));
                }
            }
        }

        for (int x = 0; x < Grid.Width - 1; x++)
        {
            for (int y = 0; y < Grid.Lenght - 1; y++)
            {
                int idx = x * Grid.Lenght + y;

                floorTriangles.Add(idx);
                floorTriangles.Add(idx + 1);
                floorTriangles.Add(idx + Grid.Lenght);

                floorTriangles.Add(idx + 1);
                floorTriangles.Add(idx + Grid.Lenght + 1);
                floorTriangles.Add(idx + Grid.Lenght);

                roofTriangles.Add(idx);
                roofTriangles.Add(idx + Grid.Lenght);
                roofTriangles.Add(idx + 1);

                roofTriangles.Add(idx + 1);
                roofTriangles.Add(idx + Grid.Lenght);
                roofTriangles.Add(idx + Grid.Lenght + 1);
            }
        }

        List<Vector3> combinedVertices = new List<Vector3>();
        combinedVertices.AddRange(floorVertices);
        combinedVertices.AddRange(roofVertices);

        List<int> combinedTriangles = new List<int>();
        combinedTriangles.AddRange(floorTriangles);

        int vertexOffset = floorVertices.Count;
        for (int i = 0; i < roofTriangles.Count; i++)
        {
            combinedTriangles.Add(roofTriangles[i] + vertexOffset);
        }
        
        mesh.Clear();
        mesh.vertices = combinedVertices.ToArray();
        mesh.triangles = combinedTriangles.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        MeshCollider collider = floor.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
    }
}
