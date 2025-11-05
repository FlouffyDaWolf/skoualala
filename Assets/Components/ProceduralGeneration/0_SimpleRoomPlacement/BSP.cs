using System.Collections.Generic;
using System.Threading;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Procedural Generation Method/BSP Dungeon")]
public class BSP : ProceduralGenerationMethod
{
    [Header("BSP Parameters")]
    [SerializeField] private int _maxDepth = 4;
    [SerializeField] private Vector2Int _minRoomSize = new Vector2Int(6, 6);
    [SerializeField] private Vector2Int _minLeafSize = new Vector2Int(20, 20);

    private readonly List<RectInt> _rooms = new();

    private class BSPNode
    {
        public RectInt Area;
        public BSPNode Left;
        public BSPNode Right;
        public RectInt? Room;

        public BSPNode(RectInt area)
        {
            Area = area;
        }

        public bool IsLeaf => Left == null && Right == null;
    }

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        _rooms.Clear();

        BSPNode root = new BSPNode(new RectInt(0, 0, Grid.Width, Grid.Lenght));
        SplitNode(root, 0);

        await GenerateRoomsAsync(root, cancellationToken);
        await ConnectRoomsAsync(root, cancellationToken);

        // Vérifie et connecte les salles non reliées
        ConnectUnlinkedRooms();

        // Connecte les zones isolées globalement
        ConnectDisconnectedZones();

        BuildGround();
    }

    private void SplitNode(BSPNode node, int depth)
    {
        if (depth >= _maxDepth)
            return;

        bool splitH = Random.value > 0.5f;
        if (node.Area.width < _minLeafSize.x * 2 || node.Area.height < _minLeafSize.y * 2)
            return;

        if (splitH)
        {
            int splitY = Random.Range(_minLeafSize.y, node.Area.height - _minLeafSize.y);
            node.Left = new BSPNode(new RectInt(node.Area.x, node.Area.y, node.Area.width, splitY));
            node.Right = new BSPNode(new RectInt(node.Area.x, node.Area.y + splitY, node.Area.width, node.Area.height - splitY));
        }
        else
        {
            int splitX = Random.Range(_minLeafSize.x, node.Area.width - _minLeafSize.x);
            node.Left = new BSPNode(new RectInt(node.Area.x, node.Area.y, splitX, node.Area.height));
            node.Right = new BSPNode(new RectInt(node.Area.x + splitX, node.Area.y, node.Area.width - splitX, node.Area.height));
        }

        SplitNode(node.Left, depth + 1);
        SplitNode(node.Right, depth + 1);
    }

    private async UniTask GenerateRoomsAsync(BSPNode node, CancellationToken cancellationToken)
    {
        if (node.IsLeaf)
        {
            int roomWidth = RandomService.Range(_minRoomSize.x, Mathf.Max(_minRoomSize.x + 1, node.Area.width - 2));
            int roomHeight = RandomService.Range(_minRoomSize.y, Mathf.Max(_minRoomSize.y + 1, node.Area.height - 2));

            int roomX = node.Area.x + RandomService.Range(1, Mathf.Max(1, node.Area.width - roomWidth - 1));
            int roomY = node.Area.y + RandomService.Range(1, Mathf.Max(1, node.Area.height - roomHeight - 1));

            RectInt room = new RectInt(roomX, roomY, roomWidth, roomHeight);
            node.Room = room;
            _rooms.Add(room);

            DrawRoom(room);
            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
        }
        else
        {
            await GenerateRoomsAsync(node.Left, cancellationToken);
            await GenerateRoomsAsync(node.Right, cancellationToken);
        }
    }

    private async UniTask ConnectRoomsAsync(BSPNode node, CancellationToken cancellationToken)
    {
        if (node == null || node.IsLeaf)
            return;

        (Vector2Int a, Vector2Int b) = GetClosestConnection(node.Left, node.Right);

        if (!DrawFullCorridorSafe(a, b))
        {
            bool connected = false;
            for (int i = -2; i <= 2 && !connected; i++)
            {
                for (int j = -2; j <= 2 && !connected; j++)
                {
                    Vector2Int altA = a + new Vector2Int(i, j);
                    Vector2Int altB = b + new Vector2Int(-i, -j);
                    if (DrawFullCorridorSafe(altA, altB))
                        connected = true;
                }
            }

            if (!connected)
                ForceConnectionToNearestCorridor(a);
        }

        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
        await ConnectRoomsAsync(node.Left, cancellationToken);
        await ConnectRoomsAsync(node.Right, cancellationToken);
    }

    private (Vector2Int, Vector2Int) GetClosestConnection(BSPNode nodeA, BSPNode nodeB)
    {
        var roomsA = GetAllRoomCenters(nodeA);
        var roomsB = GetAllRoomCenters(nodeB);

        Vector2Int bestA = Vector2Int.zero, bestB = Vector2Int.zero;
        float bestDist = float.MaxValue;

        foreach (var a in roomsA)
        {
            foreach (var b in roomsB)
            {
                float dist = Vector2Int.Distance(a, b);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestA = a;
                    bestB = b;
                }
            }
        }

        return (bestA, bestB);
    }

    private List<Vector2Int> GetAllRoomCenters(BSPNode node)
    {
        List<Vector2Int> centers = new();

        if (node == null)
            return centers;

        if (node.Room.HasValue)
        {
            var r = node.Room.Value;
            centers.Add(new Vector2Int(r.x + r.width / 2, r.y + r.height / 2));
        }

        centers.AddRange(GetAllRoomCenters(node.Left));
        centers.AddRange(GetAllRoomCenters(node.Right));

        return centers;
    }

    private bool DrawFullCorridorSafe(Vector2Int from, Vector2Int to)
    {
        if (WouldCorridorTouchAnother(from, to))
            return false;

        DrawFullCorridor(from, to);
        return true;
    }

    private void DrawFullCorridor(Vector2Int from, Vector2Int to)
    {
        int xDir = from.x < to.x ? 1 : -1;
        int yDir = from.y < to.y ? 1 : -1;

        for (int x = from.x; x != to.x; x += xDir)
        {
            if (Grid.TryGetCellByCoordinates(x, from.y, out var cell))
                AddTileToCell(cell, CORRIDOR_TILE_NAME, false);
        }

        for (int y = from.y; y != to.y; y += yDir)
        {
            if (Grid.TryGetCellByCoordinates(to.x, y, out var cell))
                AddTileToCell(cell, CORRIDOR_TILE_NAME, false);
        }
    }

    private bool WouldCorridorTouchAnother(Vector2Int from, Vector2Int to)
    {
        int xDir = from.x < to.x ? 1 : -1;
        int yDir = from.y < to.y ? 1 : -1;

        for (int x = from.x; x != to.x; x += xDir)
        {
            if (IsAdjacentToCorridor(x, from.y))
                return true;
        }
        for (int y = from.y; y != to.y; y += yDir)
        {
            if (IsAdjacentToCorridor(to.x, y))
                return true;
        }
        return false;
    }

    private bool IsAdjacentToCorridor(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                if (Grid.TryGetCellByCoordinates(x + dx, y + dy, out var neighbor))
                {
                    if (neighbor.ContainObject && neighbor.GridObject.Template.Name == CORRIDOR_TILE_NAME)
                        return true;
                }
            }
        }
        return false;
    }

    private void ForceConnectionToNearestCorridor(Vector2Int from)
    {
        int maxSearch = 10;
        for (int r = 1; r <= maxSearch; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int x = from.x + dx;
                    int y = from.y + dy;

                    if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                        continue;

                    if (cell.ContainObject && cell.GridObject.Template.Name == CORRIDOR_TILE_NAME)
                    {
                        DrawFullCorridor(from, new Vector2Int(x, y));
                        return;
                    }
                }
            }
        }
    }

    private void ConnectUnlinkedRooms()
    {
        foreach (var room in _rooms)
        {
            Vector2Int center = new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);

            if (IsAdjacentToCorridor(center.x, center.y))
                continue;

            Vector2Int? closestCorridor = FindNearestCorridor(center, 20);
            if (closestCorridor.HasValue)
            {
                DrawFullCorridor(center, closestCorridor.Value);
            }
        }
    }

    private Vector2Int? FindNearestCorridor(Vector2Int from, int maxRange)
    {
        float bestDist = float.MaxValue;
        Vector2Int? bestPos = null;

        for (int x = Mathf.Max(0, from.x - maxRange); x < Mathf.Min(Grid.Width, from.x + maxRange); x++)
        {
            for (int y = Mathf.Max(0, from.y - maxRange); y < Mathf.Min(Grid.Lenght, from.y + maxRange); y++)
            {
                if (Grid.TryGetCellByCoordinates(x, y, out var cell) &&
                    cell.ContainObject &&
                    cell.GridObject.Template.Name == CORRIDOR_TILE_NAME)
                {
                    float dist = Vector2Int.Distance(from, new Vector2Int(x, y));
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestPos = new Vector2Int(x, y);
                    }
                }
            }
        }

        return bestPos;
    }

    private void ConnectDisconnectedZones()
    {
        var visited = new bool[Grid.Width, Grid.Lenght];
        var clusters = new List<List<Vector2Int>>();

        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Lenght; y++)
            {
                if (!visited[x, y] && IsCorridorOrRoom(x, y))
                {
                    var cluster = new List<Vector2Int>();
                    FloodFillCorridors(x, y, visited, cluster);
                    clusters.Add(cluster);
                }
            }
        }

        if (clusters.Count <= 1)
            return;

        var mainCluster = clusters[0];
        for (int i = 1; i < clusters.Count; i++)
        {
            Vector2Int closestA = Vector2Int.zero;
            Vector2Int closestB = Vector2Int.zero;
            float bestDist = float.MaxValue;

            foreach (var a in mainCluster)
            {
                foreach (var b in clusters[i])
                {
                    float dist = Vector2Int.Distance(a, b);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        closestA = a;
                        closestB = b;
                    }
                }
            }

            DrawFullCorridor(closestA, closestB);
            mainCluster.AddRange(clusters[i]);
        }
    }

    private void FloodFillCorridors(int startX, int startY, bool[,] visited, List<Vector2Int> cluster)
    {
        Queue<Vector2Int> queue = new();
        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();
            if (pos.x < 0 || pos.y < 0 || pos.x >= Grid.Width || pos.y >= Grid.Lenght)
                continue;

            if (visited[pos.x, pos.y] || !IsCorridorOrRoom(pos.x, pos.y))
                continue;

            visited[pos.x, pos.y] = true;
            cluster.Add(pos);

            queue.Enqueue(new Vector2Int(pos.x + 1, pos.y));
            queue.Enqueue(new Vector2Int(pos.x - 1, pos.y));
            queue.Enqueue(new Vector2Int(pos.x, pos.y + 1));
            queue.Enqueue(new Vector2Int(pos.x, pos.y - 1));
        }
    }

    private bool IsCorridorOrRoom(int x, int y)
    {
        if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
            return false;

        if (!cell.ContainObject)
            return false;

        var name = cell.GridObject.Template.Name;
        return name == CORRIDOR_TILE_NAME || name == ROOM_TILE_NAME;
    }

    private void DrawRoom(RectInt room)
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