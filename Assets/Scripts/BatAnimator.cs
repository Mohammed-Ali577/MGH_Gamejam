using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public static class CreateBatAnimatorController
{
    [MenuItem("Assets/Create/Bat Animator Controller with Clips")]
    public static void CreateController()
    {
        // Ensure folder
        const string folder = "Assets/AnimatorControllers";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "AnimatorControllers");

        // Paths
        string controllerPath = Path.Combine(folder, "Bat.controller");
        string idleAnimPath = Path.Combine(folder, "Bat_Idle.anim");
        string flyAnimPath = Path.Combine(folder, "Bat_Fly.anim");

        // Create controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Add parameter
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        // Create simple idle clip (small slow bob)
        AnimationClip idleClip = new AnimationClip();
        idleClip.name = "Bat_Idle";
        {
            EditorCurveBinding bind = new EditorCurveBinding
            {
                type = typeof(Transform),
                path = "",
                propertyName = "m_LocalPosition.y"
            };

            // slow small bob
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 0.03f),
                new Keyframe(1f, 0f)
            );
            curve.preWrapMode = WrapMode.Loop;
            curve.postWrapMode = WrapMode.Loop;

            AnimationUtility.SetEditorCurve(idleClip, bind, curve);

            // mark loop
            SerializedObject serializedClip = new SerializedObject(idleClip);
            SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
            if (settings != null)
            {
                settings.FindPropertyRelative("m_LoopTime").boolValue = true;
                serializedClip.ApplyModifiedProperties();
            }

            AssetDatabase.CreateAsset(idleClip, idleAnimPath);
        }

        // Create simple fly clip (larger/faster bob)
        AnimationClip flyClip = new AnimationClip();
        flyClip.name = "Bat_Fly";
        {
            EditorCurveBinding bind = new EditorCurveBinding
            {
                type = typeof(Transform),
                path = "",
                propertyName = "m_LocalPosition.y"
            };

            // faster larger bob
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.12f),
                new Keyframe(0.5f, 0f),
                new Keyframe(0.75f, -0.06f),
                new Keyframe(1f, 0f)
            );
            curve.preWrapMode = WrapMode.Loop;
            curve.postWrapMode = WrapMode.Loop;

            AnimationUtility.SetEditorCurve(flyClip, bind, curve);

            // mark loop
            SerializedObject serializedClip = new SerializedObject(flyClip);
            SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
            if (settings != null)
            {
                settings.FindPropertyRelative("m_LoopTime").boolValue = true;
                serializedClip.ApplyModifiedProperties();
            }

            AssetDatabase.CreateAsset(flyClip, flyAnimPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Setup states in controller
        var layer = controller.layers[0];
        var sm = layer.stateMachine;

        var idleState = sm.AddState("Idle");
        idleState.motion = idleClip;

        var flyState = sm.AddState("Fly");
        flyState.motion = flyClip;

        sm.defaultState = idleState;

        // Transition Idle -> Fly when Speed > 0.1
        var t1 = idleState.AddTransition(flyState);
        t1.hasExitTime = false;
        t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        // Transition Fly -> Idle when Speed < 0.05
        var t2 = flyState.AddTransition(idleState);
        t2.hasExitTime = false;
        t2.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = controller;
        EditorUtility.DisplayDialog("Bat Animator Controller", "Created Bat.controller and two simple animation clips at:\n" + folder, "OK");
    }
}
#endif