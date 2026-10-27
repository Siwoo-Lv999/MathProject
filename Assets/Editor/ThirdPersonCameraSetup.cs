using System;
using System.Linq;
using _01.Code.Camera;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class ThirdPersonCameraSetup
{
    private const string ScenePath = "Assets/03.Scene/GameScene.unity";

    [MenuItem("Tools/Camera/Configure Mouse Look")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before configuring the camera.");

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
        {
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene before opening GameScene.");
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        CinemachineCamera camera = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<CinemachineCamera>(true)).Single();
        var controller = camera.GetComponent<CinemachineInputAxisController>();
        var orbit = camera.GetComponent<CinemachineOrbitalFollow>();
        if (controller == null || orbit == null || camera.Follow == null)
            throw new InvalidOperationException("The FreeLook camera needs its input, orbit and tracking target.");

        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Controls.inputactions");
        InputAction look = actions.FindAction("Player/Look", true);
        var lookReference = AssetDatabase.LoadAllAssetsAtPath("Assets/Controls.inputactions")
            .OfType<InputActionReference>().First(reference => reference.action == look);

        Undo.RecordObjects(new UnityEngine.Object[] { controller, orbit }, "Configure Mouse Look");
        controller.SynchronizeControllers();
        controller.AutoEnableInputs = true;
        controller.PlayerIndex = -1;
        foreach (var axis in controller.Controllers)
        {
            bool horizontal = axis.Name == "Look Orbit X";
            bool vertical = axis.Name == "Look Orbit Y";
            axis.Enabled = horizontal || vertical;
            if (!axis.Enabled)
                continue;

            axis.Input.InputAction = lookReference;
            axis.Input.Gain = horizontal ? 0.15f : -0.15f;
            axis.Input.CancelDeltaTime = true;
            axis.Driver.AccelTime = 0f;
            axis.Driver.DecelTime = 0f;
        }

        // A future target-facing character rotation must not rotate the camera orbit.
        orbit.TrackerSettings.BindingMode = BindingMode.WorldSpace;

        var cursor = camera.GetComponent<CameraCursor>();
        if (cursor == null)
            cursor = Undo.AddComponent<CameraCursor>(camera.gameObject);
        var serializedCursor = new SerializedObject(cursor);
        serializedCursor.FindProperty("inputController").objectReferenceValue = controller;
        serializedCursor.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(orbit);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("GameScene could not be saved.");
        Debug.Log("[Camera] Mouse look connected: X=0.15, Y=-0.15. Esc releases the cursor; click the Game view to capture it.");
    }
}
