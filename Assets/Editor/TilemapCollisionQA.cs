using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Isolated physics scene: never advances or edits the user's open scene.
public static class TilemapCollisionQA
{
    private const string PendingKey = "TilemapCollisionQA.Pending";

    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        int exitCode = 0;
        try { Run(); }
        catch (Exception e) { Debug.LogException(e); exitCode = 1; }
        finally
        {
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            else EditorApplication.isPlaying = false;
        }
    }

    [MenuItem("Tools/QA/Tilemap collision regression", true)]
    private static bool CanRunFromMenu() => Application.isPlaying;

    [MenuItem("Tools/QA/Tilemap collision regression")]
    public static void Run()
    {
        if (!Application.isPlaying)
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Run this check in Play Mode or in an isolated batch project.");
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
            return;
        }
        int passed = 0;
        foreach (bool oneWay in new[] { false, true })
        foreach (float horizontalSpeed in new[] { 0f, 8f })
        foreach (float x in new[] { -0.05f, 0f, 0.05f, 0.5f })
        {
            Check(oneWay, x, false, horizontalSpeed);
            passed++;
            Check(oneWay, x, true, horizontalSpeed);
            passed++;
        }
        Debug.Log($"TILEMAP_QA_PASS: {passed} seam landing / underside jump simulations.");
    }

    private static void Check(bool oneWay, float x, bool fromBelow, float horizontalSpeed)
    {
        Scene scene = SceneManager.CreateScene("Tilemap QA " + Guid.NewGuid(),
            new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        try
        {
            var grid = new GameObject("Grid", typeof(Grid));
            SceneManager.MoveGameObjectToScene(grid, scene);
            var mapObject = new GameObject("Terrain", typeof(Tilemap));
            mapObject.transform.SetParent(grid.transform);
            var body = mapObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = mapObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.vertexDistance = 0.0005f;
            composite.offsetDistance = 0f;
            var collider = mapObject.AddComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            collider.extrusionFactor = 0.01f;
            if (oneWay)
            {
                var effector = mapObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.useOneWayGrouping = true;
                effector.surfaceArc = 160f;
                effector.useSideFriction = false;
                composite.usedByEffector = true;
            }
            tile.colliderType = Tile.ColliderType.Grid;
            var map = mapObject.GetComponent<Tilemap>();
            for (int i = -32; i < 32; i++) map.SetTile(new Vector3Int(i, 0, 0), tile);
            collider.ProcessTilemapChanges();
            composite.GenerateGeometry();

            var player = new GameObject("Player physics probe");
            SceneManager.MoveGameObjectToScene(player, scene);
            player.transform.position = new Vector3(x, fromBelow ? -1.2f : 4f, 0f);
            var capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.59992886f, 2.0511353f);
            var rb = player.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = new Vector2(horizontalSpeed, fromBelow ? 18f : -40f);
            Physics2D.SyncTransforms();
            float highest = rb.position.y;
            var physics = scene.GetPhysicsScene2D();
            for (int i = 0; i < 150; i++)
            {
                physics.Simulate(0.02f);
                highest = Mathf.Max(highest, rb.position.y);
            }
            if (!fromBelow || oneWay)
            {
                if (rb.position.y < 1.95f || rb.position.y > 2.15f)
                    throw new Exception($"Landing failed: oneWay={oneWay}, x={x}, below={fromBelow}, y={rb.position.y}");
            }
            else if (highest > -0.9f)
                throw new Exception($"Solid underside penetration: x={x}, highest={highest}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tile);
            foreach (var root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);
            SceneManager.UnloadSceneAsync(scene);
        }
    }
}
