using UnityEngine;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System.Threading;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(fileName = "newAlgo", menuName = "Procedural Generation Method/newAlgo")]
    public class newAlgo : ProceduralGenerationMethod
    {
        void Start()
        {

        }

        void Update()
        {

        }

        protected override UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}