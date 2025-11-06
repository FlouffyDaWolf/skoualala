using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Procedural Generation Method/CellularAutomata")]
public class CellularAutomata : ProceduralGenerationMethod
{
    [SerializeField] private int _noiseDensity = 50;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        GenerateNoise();

        int[,] grid = new int[Grid.Lenght, Grid.Width];
        for (int step = 0; step < _maxSteps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Task task1 = Task.Run(() => InOrderGen(ref grid));
            Task task2 = Task.Run(() => OutOrderGen(ref grid));

            await Task.WhenAll(task1, task2);

            for (int x = 0; x < Grid.Lenght; x++)
            {
                for (int y = 0; y < Grid.Width; y++)
                {
                    Grid.TryGetCellByCoordinates(x, y, out var cell);

                    switch (grid[x, y])
                    {
                        case 1:
                            AddTileToCell(cell, GRASS_TILE_NAME, true);
                            break;
                        case 0:
                            AddTileToCell(cell, WATER_TILE_NAME, true);
                            break;
                    }
                }
            }

            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
        }
    }

    void InOrderGen(ref int[,] grid)
    {
        for (int x = 0; x < Grid.Lenght / 2; x++)
        {
            for (int y = 0; y < Grid.Width; y++)
            {
                int grassAmount = 0;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < Grid.Lenght && ny >= 0 && ny < Grid.Width)
                        {
                            Grid.TryGetCellByCoordinates(nx, ny, out var neighbor);
                            if (neighbor.GridObject.Template.Name == GRASS_TILE_NAME)
                            {
                                grassAmount++;
                            }
                        }
                    }
                }
                if (grassAmount >= 4)
                    grid[x, y] = 1;
                else
                    grid[x, y] = 0;
            }
        }
        Debug.Log("done thread 1");
    }
    
    void OutOrderGen(ref int[,] grid)
    {
        for (int x = Grid.Lenght - 1; x >= Grid.Lenght / 2; x--)
        {
            for (int y = Grid.Width - 1; y >= 0; y--)
            {
                int grassAmount = 0;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < Grid.Lenght && ny >= 0 && ny < Grid.Width)
                        {
                            Grid.TryGetCellByCoordinates(nx, ny, out var neighbor);
                            if (neighbor.GridObject.Template.Name == GRASS_TILE_NAME)
                            {
                                grassAmount++;
                            }
                        }
                    }
                }
                if (grassAmount >= 4)
                    grid[x, y] = 1;
                else
                    grid[x, y] = 0;
            }
        }
        Debug.Log("done thread 2");
    }

    void GenerateNoise()
    {
        for (int x = 0; x < Grid.Lenght; x++)
        {
            for (int y = 0; y < Grid.Width; y++)
            {
                Grid.TryGetCellByCoordinates(x, y, out var cell);
                if (RandomService.Range(0, 100) < _noiseDensity)
                    AddTileToCell(cell, GRASS_TILE_NAME, false);
                else
                    AddTileToCell(cell, WATER_TILE_NAME, false);
            }
        }
    }
}
