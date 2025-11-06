using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Procedural Generation Method/Noise")]
public class Noise : ProceduralGenerationMethod
{
    [Header("Noise Parameters")]
    public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    [Range(0f, 1f)] public float frequency = 0.04f;
    [Range(0f, 1f)] public float amplitude = 0.981f;

    [Header("Fractal Parameters")]
    public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.None;
    [Range(1, 10)] public int octaves = 2;
    [Range(0f, 5f)] public float lacunarity = 1.2f;
    [Range(0f, 1f)] public float persistence = 0.5f;

    [Header("Heights")]
    [Range(-1f, 1f)] public float waterHeight = -0.71f;
    [Range(-1f, 1f)] public float sandHeight = -0.48f;
    [Range(-1f, 1f)] public float grassHeight = 0.6f;
    [Range(-1f, 1f)] public float rockHeight = 1.0f;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(noiseType);
        noise.SetFrequency(frequency);
        noise.SetDomainWarpAmp(amplitude);

        noise.SetFractalType(fractalType);
        noise.SetFractalOctaves(octaves);
        noise.SetFractalLacunarity(lacunarity);

        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Lenght; y++)
            {
                Grid.TryGetCellByCoordinates(x, y, out var cell);

                if (noise.GetNoise(x, y) <= waterHeight)
                    AddTileToCell(cell, WATER_TILE_NAME, true);
                else if (noise.GetNoise(x, y) <= sandHeight)
                    AddTileToCell(cell, SAND_TILE_NAME, true);
                else if (noise.GetNoise(x, y) <= grassHeight)
                    AddTileToCell(cell, GRASS_TILE_NAME, true);
                else if (noise.GetNoise(x, y) <= rockHeight)
                    AddTileToCell(cell, ROCK_TILE_NAME, true);
            }
        }

        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
    }
}
