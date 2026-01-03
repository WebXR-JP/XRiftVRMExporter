using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpringBoneController))]
public class SpringBoneControllerEditor : Editor
{
    private SerializedProperty targetRootProp;
    private SerializedProperty stiffnessForceProp;
    private SerializedProperty gravityPowerProp;
    private SerializedProperty gravityDirProp;
    private SerializedProperty dragForceProp;
    private SerializedProperty jointRadiusProp;
    private SerializedProperty jointsProp;

    private void OnEnable()
    {
        targetRootProp = serializedObject.FindProperty("targetRoot");
        stiffnessForceProp = serializedObject.FindProperty("stiffnessForce");
        gravityPowerProp = serializedObject.FindProperty("gravityPower");
        gravityDirProp = serializedObject.FindProperty("gravityDir");
        dragForceProp = serializedObject.FindProperty("dragForce");
        jointRadiusProp = serializedObject.FindProperty("jointRadius");
        jointsProp = serializedObject.FindProperty("joints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var controller = (SpringBoneController)target;

        // 収集対象
        EditorGUILayout.LabelField("収集対象", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetRootProp, new GUIContent("Target Root"));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("収集", GUILayout.Height(24)))
        {
            Undo.RecordObject(controller, "Collect SpringBone Joints");
            controller.CollectJoints();
            EditorUtility.SetDirty(controller);
        }
        EditorGUILayout.LabelField($"ジョイント数: {controller.Joints.Count}", GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // SpringBone設定
        EditorGUILayout.LabelField("SpringBone設定", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(stiffnessForceProp, new GUIContent("Stiffness Force", "復元力 (0-4)"));
        EditorGUILayout.PropertyField(gravityPowerProp, new GUIContent("Gravity Power", "重力影響度 (0-2)"));
        EditorGUILayout.PropertyField(gravityDirProp, new GUIContent("Gravity Dir", "重力方向"));
        EditorGUILayout.PropertyField(dragForceProp, new GUIContent("Drag Force", "減衰力 (0-1)"));
        EditorGUILayout.PropertyField(jointRadiusProp, new GUIContent("Joint Radius", "当たり判定半径"));

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            controller.ApplyToAll();
        }

        EditorGUILayout.Space(10);

        // 管理対象リスト
        EditorGUILayout.LabelField("管理対象", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(jointsProp, new GUIContent("Joints"), true);
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
}
