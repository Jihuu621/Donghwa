using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class RabbitWeirdPrefabBuilder
{
    private const string IdleSpritePath = "Assets/Sprites/Rabbit_Weird/Rabbit_Weird_Idle.png";
    private const string AttackSpritePath = "Assets/Sprites/Rabbit_Weird/Rabbit_Weird_Attack.png";
    private const string AnimationFolder = "Assets/Animation/Rabbit_Weird";
    private const string IdleClipPath = AnimationFolder + "/Rabbit_Weird_Idle.anim";
    private const string AttackClipPath = AnimationFolder + "/Rabbit_Weird_Attack.anim";
    private const string ControllerPath = AnimationFolder + "/Rabbit_Weird.controller";
    private const string DataPath = "Assets/SO/EnemySO/Rabbit_Weird.asset";
    private const string PrefabPath = "Assets/Prefabs/Rabbit_Weird.prefab";

    [MenuItem("Tools/AI/Rebuild Rabbit Weird")]
    public static void Build()
    {
        EnsureFolder("Assets/Animation", "Rabbit_Weird");

        Sprite[] idleSprites = LoadSprites(IdleSpritePath);
        Sprite[] attackSprites = LoadSprites(AttackSpritePath);
        if (idleSprites.Length == 0 || attackSprites.Length == 0)
        {
            throw new InvalidOperationException("Rabbit_Weird sprite sheets have no imported sprites.");
        }

        AnimationClip idleClip = CreateOrUpdateClip(
            IdleClipPath, "Rabbit_Weird_Idle", idleSprites, 12f, true);
        AnimationClip attackClip = CreateOrUpdateClip(
            AttackClipPath, "Rabbit_Weird_Attack", attackSprites, 12f, true);
        AnimatorController controller = CreateOrUpdateController(idleClip, attackClip);
        EnemyData data = CreateOrUpdateEnemyData();
        CreateOrUpdatePrefab(idleSprites[0], controller, data);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Rabbit_Weird prefab rebuilt: {PrefabPath}");
    }

    public static void BuildFromCommandLine()
    {
        Build();
    }

    private static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => GetTrailingNumber(sprite.name))
            .ThenBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToArray();
    }

    private static int GetTrailingNumber(string value)
    {
        int separator = value.LastIndexOf('_');
        return separator >= 0 && int.TryParse(value.Substring(separator + 1), out int number)
            ? number
            : int.MaxValue;
    }

    private static AnimationClip CreateOrUpdateClip(string path, string clipName,
        IReadOnlyList<Sprite> sprites, float frameRate, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = clipName;
        clip.frameRate = frameRate;

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty loopTime = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopTime != null) loopTime.boolValue = loop;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateOrUpdateController(
        AnimationClip idleClip, AnimationClip attackClip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ChildAnimatorState[] existingStates = stateMachine.states;
        for (int i = 0; i < existingStates.Length; i++)
        {
            stateMachine.RemoveState(existingStates[i].state);
        }

        AnimatorState idleState = stateMachine.AddState("Rabbit_Weird_Idle", new Vector3(280f, 80f));
        idleState.motion = idleClip;
        AnimatorState attackState = stateMachine.AddState("Rabbit_Weird_Attack", new Vector3(520f, 80f));
        attackState.motion = attackClip;
        stateMachine.defaultState = idleState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static EnemyData CreateOrUpdateEnemyData()
    {
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, DataPath);
        }

        data.EnemyName = "이상한 토끼";
        data.EnemyDescription = "세 번 연속 몸을 던지고 가까운 적에게 앞발을 휘두르는 토끼 변종.";
        data.MaxHP = 55f;
        data.Damage = 8f;
        data.MoveSpeed = 3.5f;
        data.PatrolSpeed = 2.2f;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void CreateOrUpdatePrefab(Sprite idleSprite,
        RuntimeAnimatorController controller, EnemyData data)
    {
        GameObject root = new GameObject("Rabbit_Weird");
        try
        {
            root.layer = LayerMask.NameToLayer("Enemy");
            root.tag = "Enemy";

            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = idleSprite;
            renderer.sortingOrder = 2;

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 5f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Vertical;
            collider.size = new Vector2(1.15f, 1.65f);
            collider.offset = new Vector2(0f, -0.05f);

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            EnemyDataManager dataManager = root.AddComponent<EnemyDataManager>();
            SerializedObject serializedDataManager = new SerializedObject(dataManager);
            serializedDataManager.FindProperty("_enemyData").objectReferenceValue = data;
            serializedDataManager.ApplyModifiedPropertiesWithoutUndo();

            Health health = root.AddComponent<Health>();
            SerializedObject serializedHealth = new SerializedObject(health);
            serializedHealth.FindProperty("maxHP").floatValue = data.MaxHP;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<EnemyFSM>();
            root.AddComponent<RabbitWeirdSetup>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
