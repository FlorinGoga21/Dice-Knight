using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyGame.Environment;
using UnityEditor;
using UnityEngine;

/// <summary>Checks running background layers without modifying saved scenes or player saves.</summary>
public static class VerifyBackgroundScrolling
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo Owner = typeof(Background).GetField("owner", Private);
    private static readonly FieldInfo Tiles = typeof(Background).GetField("tiles", Private);
    private static readonly FieldInfo Factor = typeof(Background).GetField("parallaxFactor", Private);

    public static object Main()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Run this check in Play mode after background initialization.");
        var camera = Camera.main;
        var owners = UnityEngine.Object.FindObjectsByType<Background>().Where(b => ReferenceEquals(Owner.GetValue(b), b)).ToArray();
        if (camera == null || owners.Length == 0) throw new Exception("No initialized background layers or camera.");
        var position = camera.transform.position;
        float size = camera.orthographicSize;
        int coverageChecks = 0;
        try
        {
            var before = owners.ToDictionary(b => b, b => GetTiles(b)[0].position.x);
            Move(camera, position.x + 0.25f, owners);
            foreach (var owner in owners)
            {
                float width = owner.GetComponent<SpriteRenderer>().bounds.size.x;
                float delta = GetTiles(owner)[0].position.x - before[owner];
                float expected = 0.25f * (float)Factor.GetValue(owner);
                float error = Mathf.Repeat(delta - expected + width * 0.5f, width) - width * 0.5f;
                if (Mathf.Abs(error) > 0.002f) throw new Exception("Incorrect parallax ratio for " + owner.name);
            }

            foreach (float delta in new[] { 0f, 9.27f, 9.29f, 100f, 1000f, -1000f, 5000f, -5000f, 0f })
            {
                Move(camera, position.x + delta, owners);
                coverageChecks += CheckCoverage(camera, owners);
            }

            camera.orthographicSize = 12f;
            Move(camera, position.x, owners);
            coverageChecks += CheckCoverage(camera, owners);
            int expandedCount = UnityEngine.Object.FindObjectsByType<Background>().Length;
            for (int i = 0; i < 20; i++)
            {
                Move(camera, position.x + (i % 2 == 0 ? 1200f : -1200f), owners);
                coverageChecks += CheckCoverage(camera, owners);
            }
            if (UnityEngine.Object.FindObjectsByType<Background>().Length != expandedCount)
                throw new Exception("Tile count keeps growing after viewport coverage is established.");

            return new { scene = camera.gameObject.scene.name, layers = owners.Length, coverageChecks,
                parallaxRatiosCorrect = true, reverseAndTeleportPassed = true, resizedViewportPassed = true,
                stableTileCount = expandedCount };
        }
        finally
        {
            camera.orthographicSize = size;
            camera.transform.position = position;
            foreach (var owner in owners) owner.SendMessage("LateUpdate");
        }
    }

    private static List<Transform> GetTiles(Background owner) => (List<Transform>)Tiles.GetValue(owner);

    private static void Move(Camera camera, float x, Background[] owners)
    {
        var position = camera.transform.position; position.x = x; camera.transform.position = position;
        foreach (var owner in owners) owner.SendMessage("LateUpdate");
    }

    private static int CheckCoverage(Camera camera, Background[] owners)
    {
        float half = camera.orthographicSize * camera.aspect;
        foreach (var owner in owners)
        {
            var bounds = GetTiles(owner).Select(t => t.GetComponent<SpriteRenderer>().bounds).OrderBy(b => b.min.x).ToArray();
            if (bounds.First().min.x > camera.transform.position.x - half + 0.002f
                || bounds.Last().max.x < camera.transform.position.x + half - 0.002f)
                throw new Exception("Viewport is uncovered in " + owner.name);
            for (int i = 1; i < bounds.Length; i++)
                if (Mathf.Abs(bounds[i].min.x - bounds[i - 1].max.x) > 0.002f)
                    throw new Exception("Gap or overlapping repeat in " + owner.name);
        }
        return owners.Length;
    }
}
