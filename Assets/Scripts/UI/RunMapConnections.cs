using System;
using System.Collections.Generic;

/// <summary>Connects two columns in row order without crossing routes.</summary>
internal static class RunMapConnections
{
    internal static bool[,] Build(IReadOnlyList<int> sourceRows, IReadOnlyList<int> targetRows,
        int optionalRowRange, float extraConnectionChance, Random random)
    {
        if (sourceRows.Count == 0 || targetRows.Count == 0)
            throw new ArgumentException("Map columns must contain at least one room.");

        var connections = new bool[sourceRows.Count, targetRows.Count];

        // Nearest targets in sorted columns preserve row order.
        for (int source = 0; source < sourceRows.Count; source++)
        {
            int closest = 0;
            for (int target = 1; target < targetRows.Count; target++)
            {
                if (Math.Abs(sourceRows[source] - targetRows[target]) <
                    Math.Abs(sourceRows[source] - targetRows[closest]))
                    closest = target;
            }
            connections[source, closest] = true;
        }

        // Give every destination an incoming route, including sparse columns and the boss.
        for (int target = 0; target < targetRows.Count; target++)
        {
            bool hasIncoming = false;
            for (int source = 0; source < sourceRows.Count; source++)
                hasIncoming |= connections[source, target];
            if (hasIncoming)
                continue;

            int closest = -1;
            int closestDistance = int.MaxValue;
            for (int source = 0; source < sourceRows.Count; source++)
            {
                int distance = Math.Abs(sourceRows[source] - targetRows[target]);
                if (distance < closestDistance && CanConnect(source, target, sourceRows, targetRows, connections))
                {
                    closest = source;
                    closestDistance = distance;
                }
            }

            if (closest < 0)
                throw new InvalidOperationException("Could not connect a map room without crossing a route.");
            connections[closest, target] = true;
        }

        // Shuffle optional branches so equal layouts can still produce different paths.
        var candidates = new List<int>();
        for (int source = 0; source < sourceRows.Count; source++)
        {
            for (int target = 0; target < targetRows.Count; target++)
            {
                if (!connections[source, target] &&
                    Math.Abs(sourceRows[source] - targetRows[target]) <= optionalRowRange)
                    candidates.Add(source * targetRows.Count + target);
            }
        }
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int other = random.Next(i + 1);
            int candidate = candidates[i];
            candidates[i] = candidates[other];
            candidates[other] = candidate;
        }
        foreach (int candidate in candidates)
        {
            int source = candidate / targetRows.Count;
            int target = candidate % targetRows.Count;
            if (random.NextDouble() < extraConnectionChance &&
                CanConnect(source, target, sourceRows, targetRows, connections))
                connections[source, target] = true;
        }

        return connections;
    }

    private static bool CanConnect(int source, int target, IReadOnlyList<int> sourceRows,
        IReadOnlyList<int> targetRows, bool[,] connections)
    {
        for (int otherSource = 0; otherSource < sourceRows.Count; otherSource++)
        {
            for (int otherTarget = 0; otherTarget < targetRows.Count; otherTarget++)
            {
                if (connections[otherSource, otherTarget] &&
                    (sourceRows[source] - sourceRows[otherSource]) *
                    (targetRows[target] - targetRows[otherTarget]) < 0)
                    return false;
            }
        }
        return true;
    }
}
