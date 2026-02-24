namespace SquareBuddy.WebApi.Core;

using System.Linq;
using System.Text;

public interface ISceneGraphGenerator
{
    SceneGraph GenerateSceneGraph(IEnumerable<Figurine> figurines, double rowCount, double colCount);
}

public enum ShapeType { Any, Linear, Compact }

public class BiomeRule
{
    public string TargetId { get; set; }
    public int MinCount { get; set; }
    public string Label { get; set; }
    public bool AbsorbItems { get; set; }
    public ShapeType Shape { get; set; } = ShapeType.Any; // New Field
}

public class SceneGraphGenerator : ISceneGraphGenerator
{
    public SceneGraph GenerateSceneGraph(IEnumerable<Figurine> figurines, double rowCount, double colCount)
    {
        List<List<Figurine>> clusters = GetSpatialClusters(figurines.ToList());
        SceneGraph scene = GenerateSceneWithRelations(clusters, rowCount, colCount);

        return scene;
    }

    // TODO: get them from database on start 
    private static List<BiomeRule> BiomeRules = new List<BiomeRule>
    {
        new() { TargetId = "tree", MinCount = 3, Label = "forest", AbsorbItems = true },
        new() { TargetId = "stone", MinCount = 3, Label = "mountain range", AbsorbItems = true },
        new() { TargetId = "water", MinCount = 4, Label = "lake", Shape = ShapeType.Compact, AbsorbItems = true },
        new() { TargetId = "water", MinCount = 4, Label = "river", Shape = ShapeType.Linear, AbsorbItems = true }
    };

    public static List<List<Figurine>> GetSpatialClusters(List<Figurine> boardState)
    {
        var visited = new HashSet<Figurine>();
        var clusters = new List<List<Figurine>>();

        // Optimize lookup: Dictionary O(1) vs List Search O(N)
        // Key: (x,y), Value: Figurine
        var gridMap = boardState
            .GroupBy(f => f.Location)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var figurine in boardState)
        {
            if (visited.Contains(figurine))
            {
                continue;
            }

            var currentCluster = new List<Figurine>();
            Traverse(figurine, currentCluster, gridMap, visited);
            clusters.Add(currentCluster);
        }

        return clusters;
    }

    private static void Traverse(
        Figurine current,
        List<Figurine> cluster,
        Dictionary<Point, Figurine> map,
        HashSet<Figurine> visited)
    {
        var stack = new Stack<Figurine>();
        stack.Push(current);

        while (stack.Count > 0)
        {
            Figurine node = stack.Pop();

            if (visited.Contains(node))
            {
                continue;
            }

            visited.Add(node);
            cluster.Add(node);

            // Check all 8 neighbors
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    Point neighborPos = new Point
                    {
                        X = node.Location.X + dx,
                        Y = node.Location.Y + dy
                    };

                    if (map.TryGetValue(neighborPos, out var neighbor))
                    {
                        if (!visited.Contains(neighbor))
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }
        }
    }

    private static Point GetCentroid(List<Figurine> cluster)
    {
        return new Point
        {
            X = cluster.Average(f => f.Location.X),
            Y = cluster.Average(f => f.Location.Y)
        };
    }

    private static ShapeType AnalyzeShape(List<Figurine> cluster)
    {
        if (cluster.Count < 3) return ShapeType.Linear; // 2 points always form a line

        // Build internal adjacency set for O(1) lookups
        HashSet<Point> positions = cluster.Select(c => new Point { X = c.Location.X, Y = c.Location.Y }).ToHashSet();
        int totalNeighbors = 0;

        foreach (Figurine? item in cluster)
        {
            int neighborCount = 0;
            // Check 8-way adjacency
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    if (positions.Contains(new Point { X = item.Location.X + dx, Y = item.Location.Y + dy }))
                    {
                        neighborCount++;
                    }
                }
            }

            totalNeighbors += neighborCount;
        }

        // Metric: Average Neighbors per Node
        // Linear (Chain): Avg ~ 2.0 (End nodes have 1, middle have 2)
        // Compact (Blob): Avg > 2.5 (Internal nodes have 3-8)
        double avgNeighbors = (double)totalNeighbors / cluster.Count;

        // Threshold can be tuned. 2.4 is a safe breakpoint for grids.
        return avgNeighbors <= 2.4 ? ShapeType.Linear : ShapeType.Compact;
    }

    private static string DescribeContent(List<Figurine> cluster, List<BiomeRule> rules)
    {
        var summaryParts = new List<string>();
        Dictionary<string, int> counts = cluster.GroupBy(f => f.Id).ToDictionary(g => g.Key, g => g.Count());

        // Pre-calculate cluster shape once
        ShapeType currentShape = AnalyzeShape(cluster);

        foreach (var rule in rules)
        {
            if (!counts.ContainsKey(rule.TargetId))
            {
                continue;
            }

            if (counts[rule.TargetId] < rule.MinCount)
            {
                continue;
            }

            // CHECK SHAPE CONSTRAINT
            bool shapeMatch = rule.Shape == ShapeType.Any || rule.Shape == currentShape;

            if (shapeMatch)
            {
                summaryParts.Add($"a {rule.Label}");

                if (rule.AbsorbItems)
                {
                    counts.Remove(rule.TargetId);
                }
            }
        }

        foreach (var kvp in counts)
        {
            string readableName = kvp.Key;
            summaryParts.Add($"{kvp.Value} {readableName}{(kvp.Value > 1 ? "s" : "")}");
        }

        if (summaryParts.Count == 0)
        {
            return "Empty Area";
        }

        // ... (rest of function: Generic Fallback) ...
        return string.Join(" and ", summaryParts);
    }

    private static string GetCardinalDirection(Point loc, double width, double height)
    {
        double cx = width / 2.0;
        double cy = height / 2.0;

        // Define deadzone for "Center" (middle 33% of board)
        double marginX = width * 0.165;
        double marginY = height * 0.165;

        bool isCentralX = Math.Abs(loc.X - cx) < marginX;
        bool isCentralY = Math.Abs(loc.Y - cy) < marginY;

        if (isCentralX && isCentralY)
        {
            return "center";
        }

        var dir = new StringBuilder();

        if (!isCentralY)
        {
            dir.Append(loc.Y < cy ? "north" : "south");
        }

        if (!isCentralX)
        {
            dir.Append(loc.X < cx ? "west" : "east");
        }

        return dir.ToString().Replace("northwest", "north-west")
                             .Replace("northeast", "north-east")
                             .Replace("southwest", "south-west")
                             .Replace("southeast", "south-east");
    }

    public static SceneGraph GenerateSceneWithRelations(
        List<List<Figurine>> clusters,
        double rowCount,
        double colCount,
        List<BiomeRule> rules = null)
    {
        rules ??= BiomeRules;
        // 1. Calculate Dynamic Thresholds
        // Diagonal is the maximum possible distance on the board
        double maxDist = Math.Sqrt(Math.Pow(rowCount, 2) + Math.Pow(colCount, 2));

        // logic: Near is within ~25% of the board, Medium is within ~50%
        double thresholdNear = maxDist * 0.25;
        double thresholdMedium = maxDist * 0.50;

        var clusterData = new List<SceneCluster>();

        // 1. Process basic cluster info (previous step)
        for (int i = 0; i < clusters.Count; i++)
        {
            List<Figurine>? cluster = clusters[i];
            Point? centroid = GetCentroid(cluster);

            clusterData.Add(new SceneCluster {
                Id = i, // Assign numeric ID for reference
                Position = GetCardinalDirection(centroid, colCount, rowCount),
                Centroid = centroid,
                Contents = DescribeContent(cluster, rules)
            });
        }

        // 2. Calculate Relations (N*N complexity, negligible for board games)
        List<SceneRelation> relations = new();

        for (int i = 0; i < clusterData.Count; i++)
        {
            for (int j = i + 1; j < clusterData.Count; j++)
            {
                SceneCluster? c1 = clusterData[i];
                SceneCluster? c2 = clusterData[j];

                double dist = GetEuclideanDistance(c1.Centroid, c2.Centroid);
                string distLabel = GetDistanceLabel(dist, thresholdNear, thresholdMedium);

                relations.Add(new SceneRelation {
                    FromClusterId = c1.Id,
                    ToClusterId = c2.Id,
                    DistanceUnits = Math.Round(dist, 1),
                    SemanticDistance = distLabel
                });
            }
        }

        return new SceneGraph {
            Clusters = clusterData,
            Relations = relations
        };
    }

    // Helpers
    private static double GetEuclideanDistance(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
    }

    private static string GetDistanceLabel(double distance, double tNear, double tMedium)
    {
        // Hard constraint for immediate neighbors
        if (distance <= 1.5)
        {
            return "adjacent";
        }

        if (distance <= tNear)
        {
            return "close";
        }

        if (distance <= tMedium)
        {
            return "nearby";
        }

        return "far away";
    }
}
