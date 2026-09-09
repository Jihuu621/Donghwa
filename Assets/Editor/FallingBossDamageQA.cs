using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FallingBossDamageQA
{
    private const string Pending = "FallingBossDamageQA.Pending";
    [InitializeOnLoadMethod]
    private static void Register() => EditorApplication.playModeStateChanged += OnPlay;
    private static void OnPlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        int code = 0;
        try { Run(); } catch (Exception e) { Debug.LogException(e); code = 1; }
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }

    public static void Run()
    {
        if (!Application.isPlaying)
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use an isolated batch project.");
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
            return;
        }
        var failures = new List<string>();
        int count = 0;
        foreach (string test in new[] { "trigger", "solid", "child", "multiple", "stationary", "smoke", "fast", "already-overlapping" })
        {
            try { Check(test); Debug.Log("FALLING_BOSS_QA_PASS: " + test); }
            catch (Exception e) { failures.Add(test + ": " + e.Message); }
            count++;
        }
        if (failures.Count > 0) throw new Exception(string.Join("\n", failures));
        Debug.Log($"FALLING_BOSS_QA_ALL_PASS: {count}");
    }

    private static void Check(string test)
    {
        var scene = SceneManager.CreateScene("Falling boss QA " + Guid.NewGuid(), new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        try
        {
            var boss = new GameObject("Cheshire health probe");
            boss.layer = 6; // CheshireCat.prefab body layer.
            SceneManager.MoveGameObjectToScene(boss, scene);
            var health = boss.AddComponent<CheshireCatHealth>();
            var ai = boss.GetComponent<CheshireCatAI>();
            ai.enabled = false; // Isolate impact from attack/teleport scheduling, retain actual health logic.
            boss.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            if (test == "smoke") typeof(CheshireCatAI).GetField("<IsSmokeForm>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ai, true);
            var target = boss;
            if (test == "child")
            {
                target = new GameObject("Untagged child body");
                target.transform.SetParent(boss.transform, false);
                target.layer = boss.layer;
            }
            var hitbox = target.AddComponent<BoxCollider2D>();
            hitbox.size = new Vector2(3f, 2f);
            hitbox.isTrigger = test != "solid";
            if (test == "multiple") boss.AddComponent<BoxCollider2D>().isTrigger = true;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TTTEEESSSTTT/Test2/FallingPlatform.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FallingPlatform.prefab");
            if (prefab == null) throw new Exception("Real FallingPlatform prefab missing");
            var falling = UnityEngine.Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(falling, scene);
            foreach (var joint in falling.GetComponents<Joint2D>()) UnityEngine.Object.DestroyImmediate(joint);
            var body = falling.GetComponent<Rigidbody2D>();
            falling.layer = 10; // Puz scene instance override.
            body.gravityScale = 0f;
            body.angularVelocity = 0f;
            bool overlapping = test == "stationary" || test == "already-overlapping";
            body.position = new Vector2(0f, overlapping ? 0f : 4f);
            body.linearVelocity = new Vector2(0f, overlapping ? 0f : test == "fast" ? -400f : -8f);
            var physics = scene.GetPhysicsScene2D();
            Physics2D.SyncTransforms();
            float initial = health.CurrentHP;
            for (int i = 0; i < 40; i++)
            {
                if (test == "already-overlapping" && i == 2) body.linearVelocity = Vector2.down * 8f;
                physics.Simulate(0.02f);
            }
            float expected = test == "stationary" || test == "smoke" ? initial : initial - health.MaxHP * 0.2f;
            if (Mathf.Abs(health.CurrentHP - expected) > 0.001f)
                throw new Exception($"HP expected {expected}, actual {health.CurrentHP}");
        }
        finally
        {
            foreach (var root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);
            SceneManager.UnloadSceneAsync(scene);
        }
    }
}
