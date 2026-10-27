using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _01.Code.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Bakes editable bone keyframes; no procedural animation runs in the player.</summary>
public static class DoroAnimationAuthoring
{
    private const string PrefabPath = "Assets/04.Art/Characters/Doro/Prefabs/DoroVisual.prefab";
    private const string AnimationPath = "Assets/04.Art/Characters/Doro/Animations";
    private const string ControllerPath = "Assets/02.Database/Player/PlayerController.controller";
    private const string ScenePath = "Assets/03.Scene/GameScene.unity";

    [MenuItem("Tools/DORO/Rebuild Idle and Move Animations")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before authoring Doro animations.");

        Directory.CreateDirectory(AnimationPath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var preview = EditorSceneManager.NewPreviewScene();
        AnimationClip idle;
        AnimationClip move;
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), preview);
            var rig = new RigPose(instance.GetComponentInChildren<Animator>(true).transform);
            idle = SaveClip(Bake(rig, false, 2.4f, 72), AnimationPath + "/Idle.anim");
            move = SaveClip(Bake(rig, true, 0.4f, 24), AnimationPath + "/Move.anim");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (!controller.parameters.Any(p => p.name == "Speed"))
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        if (controller.layers.Length == 0)
            controller.AddLayer("Base Layer");
        var layers = controller.layers;
        layers[0].defaultWeight = 1f;
        controller.layers = layers;
        var machine = controller.layers[0].stateMachine;
        var idleState = FindOrAddState(machine, "Idle", new Vector3(260f, 80f));
        var moveState = FindOrAddState(machine, "Move", new Vector3(520f, 80f));
        idleState.motion = idle;
        moveState.motion = move;
        idleState.writeDefaultValues = false;
        moveState.writeDefaultValues = false;
        machine.defaultState = idleState;
        ConfigureTransition(idleState, moveState, AnimatorConditionMode.Greater, 0.1f, 0.1f);
        ConfigureTransition(moveState, idleState, AnimatorConditionMode.Less, 0.05f, 0f);
        EditorUtility.SetDirty(controller);

        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var animator = prefab.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        ApplyToScene(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[DORO] Baked Idle (2.4s) and Move (0.4s), and connected the player Animator.");
    }

    private static AnimationClip Bake(RigPose rig, bool moving, float duration, int intervals)
    {
        var clip = new AnimationClip { name = moving ? "Move" : "Idle", frameRate = 60f, wrapMode = WrapMode.Loop };
        var rotations = new Quaternion[rig.Bones.Length, intervals + 1];
        var hipPositions = new Vector3[intervals + 1];
        for (int i = 0; i <= intervals; i++)
        {
            // Reuse phase zero at the seam so every animated channel closes exactly.
            float phase = i == intervals ? 0f : (float)i / intervals;
            rig.Reset();
            if (moving)
                PoseMove(rig, phase);
            else
                PoseIdle(rig, phase);
            for (int b = 0; b < rig.Bones.Length; b++)
            {
                var q = rig.Bones[b].localRotation;
                if (i > 0 && Quaternion.Dot(rotations[b, i - 1], q) < 0f)
                    q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                rotations[b, i] = q;
            }
            hipPositions[i] = rig.Named["Hips"].localPosition;
        }

        string[] channels = { "x", "y", "z", "w" };
        for (int b = 0; b < rig.Bones.Length; b++)
        {
            string path = AnimationUtility.CalculateTransformPath(rig.Bones[b], rig.Root);
            for (int c = 0; c < 4; c++)
            {
                var values = new float[intervals + 1];
                for (int i = 0; i <= intervals; i++)
                    values[i] = rotations[b, i][c];
                AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + channels[c]),
                    LoopCurve(values, duration));
            }
        }
        for (int c = 0; c < 3; c++)
        {
            var values = new float[intervals + 1];
            for (int i = 0; i <= intervals; i++)
                values[i] = hipPositions[i][c];
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Armature/Hips", typeof(Transform), "m_LocalPosition." + channels[c]),
                LoopCurve(values, duration));
        }
        clip.EnsureQuaternionContinuity();
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        rig.Reset();
        return clip;
    }

    private static void PoseIdle(RigPose rig, float phase)
    {
        float t = phase * Mathf.PI * 2f;
        rig.MoveHips(new Vector3(0f, 0.006f * Mathf.Sin(t), 0f));
        rig.Rotate("Hips", 0.35f * Mathf.Sin(t), 0f, 0.45f * Mathf.Sin(t));
        rig.Rotate("Spine", -0.3f * Mathf.Sin(t), 0f, 0f);
        rig.Rotate("Chest", 0.45f * Mathf.Sin(t - 0.25f), 0f, 0f);
        rig.Rotate("Neck", 0.4f * Mathf.Sin(t), 1.8f * Mathf.Sin(t), -0.4f * Mathf.Sin(t));
        rig.Rotate("Head", 0.2f * Mathf.Sin(t - 0.4f), 0.6f * Mathf.Sin(t), 0f);
        PoseAccessories(rig, t, 0.65f);
    }

    private static void PoseMove(RigPose rig, float phase)
    {
        float t = phase * Mathf.PI * 2f;
        // A low, springy trot: the opposing front/rear feet share each contact.
        rig.MoveHips(new Vector3(0.006f * Mathf.Sin(t), -0.045f + 0.018f * (1f - Mathf.Cos(2f * t)), 0f));
        rig.Rotate("Hips", 1.0f * Mathf.Sin(2f * t), 0f, 1.8f * Mathf.Sin(t));
        rig.Rotate("Spine", -0.5f * Mathf.Sin(2f * t), 0.8f * Mathf.Sin(t), -0.6f * Mathf.Sin(t));
        rig.Rotate("Chest", -0.4f * Mathf.Sin(2f * t), -0.6f * Mathf.Sin(t), -0.5f * Mathf.Sin(t));
        rig.Rotate("Neck", -0.5f * Mathf.Sin(2f * t), -0.5f * Mathf.Sin(t), -0.5f * Mathf.Sin(t));
        rig.Rotate("Head", 0.5f * Mathf.Sin(2f * t - 0.3f), 0f, -0.3f * Mathf.Sin(t));
        rig.Step("Left arm", "Left elbow", "Left wrist", phase, false);
        rig.Step("Right arm", "Right elbow", "Right wrist", Mathf.Repeat(phase + 0.5f, 1f), false);
        rig.Step("Left leg", "Left knee", "Left ankle", Mathf.Repeat(phase + 0.5f, 1f), true);
        rig.Step("Right leg", "Right knee", "Right ankle", phase, true);
        PoseAccessories(rig, t, 2.4f);
    }

    private static void PoseAccessories(RigPose rig, float t, float amount)
    {
        rig.Rotate("Head飾", amount * Mathf.Sin(t - 0.6f), 0f, amount * 0.4f * Mathf.Sin(t));
        for (int i = 0; i < 5; i++)
        {
            float sway = amount * 0.5f * Mathf.Sin(t - 0.8f - i * 0.45f);
            rig.Rotate("Head飾_" + i + "_1", sway, 0f, sway * 0.25f);
            rig.Rotate("翼_" + i + "_1_L", sway * 0.35f, 0f, sway * 0.2f);
            rig.Rotate("翼_" + i + "_1_R", sway * 0.35f, 0f, -sway * 0.2f);
        }
    }

    private static AnimationCurve LoopCurve(float[] values, float duration)
    {
        int intervals = values.Length - 1;
        float dt = duration / intervals;
        var keys = new Keyframe[values.Length];
        for (int i = 0; i <= intervals; i++)
        {
            int previous = i == 0 || i == intervals ? intervals - 1 : i - 1;
            int next = i == intervals ? 1 : i + 1;
            float tangent = (values[next] - values[previous]) / (2f * dt);
            keys[i] = new Keyframe(i * dt, values[i], tangent, tangent);
        }
        return new AnimationCurve(keys) { preWrapMode = WrapMode.Loop, postWrapMode = WrapMode.Loop };
    }

    private static AnimationClip SaveClip(AnimationClip clip, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
        EditorUtility.CopySerialized(clip, existing);
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(clip);
        return existing;
    }

    private static AnimatorState FindOrAddState(AnimatorStateMachine machine, string name, Vector3 position)
    {
        var state = machine.states.FirstOrDefault(s => s.state.name == name).state;
        return state != null ? state : machine.AddState(name, position);
    }

    private static void ConfigureTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, float duration)
    {
        var transition = from.transitions.FirstOrDefault(t => t.destinationState == to) ?? from.AddTransition(to);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
        transition.orderedInterruption = false;
        transition.conditions = new[] { new AnimatorCondition { mode = mode, parameter = "Speed", threshold = threshold } };
    }

    private static void ApplyToScene(AnimatorController controller)
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            Directory.CreateDirectory("Logs/DoroAnimation/Backups");
            const string backup = "Logs/DoroAnimation/Backups/GameScene.before-animation.unity";
            if (!File.Exists(backup) && !EditorSceneManager.SaveScene(scene, backup, true))
                throw new IOException("Could not back up GameScene.");
            var player = scene.GetRootGameObjects().Single(go => go.name == "Player");
            var visual = player.transform.Find("Visual/DoroVisual");
            var animator = visual.GetComponentInChildren<Animator>(true);
            Undo.RecordObject(animator, "Connect Doro Animations");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            var module = player.GetComponentInChildren<PlayerAnimator>(true);
            if (module == null)
            {
                var moduleObject = new GameObject("Animator");
                Undo.RegisterCreatedObjectUndo(moduleObject, "Create Player Animator Module");
                Undo.SetTransformParent(moduleObject.transform, player.transform, "Parent Animator Module");
                moduleObject.transform.localPosition = Vector3.zero;
                module = Undo.AddComponent<PlayerAnimator>(moduleObject);
            }
            var serialized = new SerializedObject(module);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("visualRoot").objectReferenceValue = visual;
            serialized.ApplyModifiedProperties();

            // Locomotion rotates only the visual. Keep the capsule upright under gravity.
            var body = player.GetComponent<Rigidbody>();
            Undo.RecordObject(body, "Keep Doro Upright");
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("Could not save the animated player scene.");
        }
        finally
        {
            if (openedHere)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private sealed class RigPose
    {
        public readonly Transform Root;
        public readonly Transform[] Bones;
        public readonly Dictionary<string, Transform> Named;
        private readonly Vector3[] positions;
        private readonly Quaternion[] rotations;
        private readonly Dictionary<Transform, Vector3> modelPositions;
        private readonly Dictionary<Transform, Quaternion> modelRotations;

        public RigPose(Transform root)
        {
            Root = root;
            var animator = root.GetComponent<Animator>();
            animator.enabled = false;
            Bones = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t != root && t.name != "Armature" && t.name != "Body").ToArray();
            Named = Bones.ToDictionary(t => t.name);
            positions = Bones.Select(t => t.localPosition).ToArray();
            rotations = Bones.Select(t => t.localRotation).ToArray();
            modelPositions = Bones.ToDictionary(t => t, t => root.InverseTransformPoint(t.position));
            modelRotations = Bones.ToDictionary(t => t, t => Quaternion.Inverse(root.rotation) * t.rotation);
        }

        public void Reset()
        {
            for (int i = 0; i < Bones.Length; i++)
            {
                Bones[i].localPosition = positions[i];
                Bones[i].localRotation = rotations[i];
            }
        }

        public void MoveHips(Vector3 offset)
        {
            var hips = Named["Hips"];
            hips.localPosition += hips.parent.InverseTransformVector(Root.TransformVector(offset));
        }

        public void Rotate(string name, float pitch, float yaw, float roll)
        {
            var bone = Named[name];
            Vector3 x = bone.parent.InverseTransformDirection(Root.right);
            Vector3 y = bone.parent.InverseTransformDirection(Root.up);
            Vector3 z = bone.parent.InverseTransformDirection(Root.forward);
            bone.localRotation = Quaternion.AngleAxis(yaw, y) * Quaternion.AngleAxis(pitch, x)
                * Quaternion.AngleAxis(roll, z) * bone.localRotation;
        }

        public void Step(string upperName, string middleName, string endName, float phase, bool rear)
        {
            const float stance = 0.58f;
            float stride;
            float lift;
            if (phase < stance)
            {
                stride = Mathf.Lerp(0.12f, -0.12f, phase / stance);
                lift = 0f;
            }
            else
            {
                float swing = (phase - stance) / (1f - stance);
                stride = Mathf.Lerp(-0.12f, 0.12f, Mathf.SmoothStep(0f, 1f, swing));
                lift = (rear ? 0.085f : 0.105f) * Mathf.Sin(swing * Mathf.PI);
            }
            var upper = Named[upperName];
            var middle = Named[middleName];
            var end = Named[endName];
            Vector3 target = Root.TransformPoint(modelPositions[end] + new Vector3(0f, lift, stride));
            Vector3 pole = rear ? Root.forward : -Root.forward;
            Vector3 start = upper.position;
            float a = Vector3.Distance(start, middle.position);
            float b = Vector3.Distance(middle.position, end.position);
            Vector3 axis = (target - start).normalized;
            float distance = Mathf.Clamp(Vector3.Distance(start, target), Mathf.Abs(a - b) + 0.00001f, a + b - 0.00001f);
            float cosine = Mathf.Clamp((a * a + distance * distance - b * b) / (2f * a * distance), -1f, 1f);
            Vector3 bend = Vector3.ProjectOnPlane(pole, axis).normalized;
            Vector3 middleTarget = start + axis * (a * cosine) + bend * (a * Mathf.Sqrt(1f - cosine * cosine));
            upper.rotation = Quaternion.FromToRotation(middle.position - start, middleTarget - start) * upper.rotation;
            middle.rotation = Quaternion.FromToRotation(end.position - middle.position, target - middle.position) * middle.rotation;
            end.rotation = Root.rotation * modelRotations[end];
        }
    }
}
