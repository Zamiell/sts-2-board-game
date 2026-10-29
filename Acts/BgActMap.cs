using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Random;

namespace BoardGame.Acts;

// A board game act map: one of the act's printed layouts, with its dark and light tokens shuffled onto it. Row 0
// is Neow's room, the layout's rows follow, and the boss sits above the last row.
public sealed class BgActMap : ActMap
{
    // The tokens placed face-down on the map's dark and light spaces.
    private static readonly char[] DarkTokens = ['E', 'E', 'E', 'M', 'M', 'M', '?', '?'];
    private static readonly char[] LightTokens = ['M', '?', '$', '$', 'R', 'R', 'R'];

    private const int Columns = 7;

    private const char BurningEliteToken = 'B';

    public override MapPoint BossMapPoint { get; }

    public override MapPoint StartingMapPoint { get; }

    // Ascension 13: a second boss after the first.
    public override MapPoint? SecondBossMapPoint { get; }

    protected override MapPoint?[,] Grid { get; }

    // The burning elite (Emerald Key), if one of the dark tokens hides it.
    public MapPoint? BurningElite { get; }

    public BgActMap(BgMapData.Layout layout, Rng rng, bool secondBoss = false, bool burningElite = false)
    {
        // The layout's top row is the boss.
        var rows = layout.Rows.Length - 1;
        Grid = new MapPoint?[Columns, rows + 1];
        StartingMapPoint = new MapPoint(Columns / 2, 0) { PointType = MapPointType.Ancient };
        BossMapPoint = new MapPoint(Columns / 2, rows + 1) { PointType = MapPointType.Boss };
        if (secondBoss)
        {
            SecondBossMapPoint = new MapPoint(Columns / 2, rows + 2) { PointType = MapPointType.Boss };
            BossMapPoint.AddChildPoint(SecondBossMapPoint);
        }
        // While the party has no Emerald Key, one of the dark monster tokens is the burning elite instead.
        var darkTokens = DarkTokens.ToList();
        if (burningElite)
        {
            darkTokens[darkTokens.IndexOf('M')] = BurningEliteToken;
        }
        var dark = new Queue<char>(darkTokens.UnstableShuffle(rng));
        var light = new Queue<char>(LightTokens.ToList().UnstableShuffle(rng));
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < Columns; col++)
            {
                var symbol = layout.Rows[row][col] switch
                {
                    'D' => dark.Dequeue(),
                    'L' => light.Dequeue(),
                    var s => s,
                };
                if (TypeOf(symbol) is { } type)
                {
                    Grid[col, row + 1] = new MapPoint(col, row + 1) { PointType = type };
                    if (symbol == BurningEliteToken)
                    {
                        BurningElite = Grid[col, row + 1];
                    }
                }
            }
        }
        foreach (var (from, to) in layout.Edges)
        {
            var (fromRow, fromCol) = Parse(from);
            var (toRow, toCol) = Parse(to);
            if (fromRow >= rows || Grid[fromCol, fromRow + 1] is not { } parent)
            {
                continue;
            }
            var child = toRow >= rows ? BossMapPoint : Grid[toCol, toRow + 1];
            if (child != null)
            {
                parent.AddChildPoint(child);
            }
        }
        foreach (var point in GetPointsInRow(1))
        {
            StartingMapPoint.AddChildPoint(point);
            startMapPoints.Add(point);
        }
    }

    // Act IV: rest, shop, (at Ascension 11+) the Spire Shield and Spear, then the Corrupt Heart.
    public BgActMap(bool elite)
    {
        var types = new List<MapPointType> { MapPointType.RestSite, MapPointType.Shop };
        if (elite)
        {
            types.Add(MapPointType.Elite);
        }
        Grid = new MapPoint?[Columns, types.Count + 1];
        StartingMapPoint = new MapPoint(Columns / 2, 0) { PointType = MapPointType.Ancient };
        BossMapPoint = new MapPoint(Columns / 2, types.Count + 1) { PointType = MapPointType.Boss };
        var previous = StartingMapPoint;
        for (var i = 0; i < types.Count; i++)
        {
            var point = new MapPoint(Columns / 2, i + 1) { PointType = types[i] };
            Grid[Columns / 2, i + 1] = point;
            previous.AddChildPoint(point);
            if (i == 0)
            {
                startMapPoints.Add(point);
            }
            previous = point;
        }
        previous.AddChildPoint(BossMapPoint);
    }

    private static (int Row, int Col) Parse(string point)
    {
        var parts = point.Split('-');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    private static MapPointType? TypeOf(char symbol) => symbol switch
    {
        'M' => MapPointType.Monster,
        '?' => MapPointType.Unknown,
        '$' => MapPointType.Shop,
        'R' => MapPointType.RestSite,
        'T' => MapPointType.Treasure,
        'E' or BurningEliteToken => MapPointType.Elite,
        _ => null,
    };
}
