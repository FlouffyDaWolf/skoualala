using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Simple Room Placement")]
    public class SimpleRoomPlacement : ProceduralGenerationMethod
    {
        [Header("Room Parameters")]
        [SerializeField] private int _maxRooms = 10;
        [SerializeField] private Vector2Int _minRoomSize = new Vector2Int(5, 5);
        [SerializeField] private Vector2Int _maxRoomSize = new Vector2Int(15, 15);
        [SerializeField, Tooltip("Tiles buffer around rooms")] private int _spacing = 1;

        private readonly List<RectInt> _rooms = new();

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            _rooms.Clear();

            int attempts = 0;
            while (_rooms.Count < _maxRooms && attempts < _maxSteps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempts++;

                int width = RandomService.Range(_minRoomSize.x, _maxRoomSize.x);
                int height = RandomService.Range(_minRoomSize.y, _maxRoomSize.y);

                if (width >= Grid.Width || height >= Grid.Lenght)
                    continue;

                int x = RandomService.Range(0, Grid.Width - width);
                int y = RandomService.Range(0, Grid.Lenght - height);
                var newRoom = new RectInt(x, y, width, height);

                if (!CanPlaceRoom(newRoom, _spacing))
                    continue;

                PlaceRoom(newRoom);
                _rooms.Add(newRoom);

                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            }
            
            // ✅ Et tu gardes uniquement :
            BuildGround();
        }

        private void PlaceRoom(RectInt room)
        {
            for (int x = room.xMin; x < room.xMax; x++)
            {
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                        continue;

                    AddTileToCell(cell, ROOM_TILE_NAME, false);
                }
            }
        }

        private void BuildGround()
        {
            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                        continue;

                    if (cell.ContainObject &&
                        (cell.GridObject.Template.Name == ROOM_TILE_NAME || cell.GridObject.Template.Name == CORRIDOR_TILE_NAME))
                        continue;

                    AddTileToCell(cell, GRASS_TILE_NAME, false);
                }
            }
        }
    }
}