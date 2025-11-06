using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Procedural Generation Method/Noise")]
public class Noise : ProceduralGenerationMethod
{
    [SerializeField] private int _noiseDensity = 50;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        

        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
    }
}
