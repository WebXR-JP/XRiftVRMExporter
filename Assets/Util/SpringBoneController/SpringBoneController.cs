using System.Collections.Generic;
using UnityEngine;
using UniVRM10;

/// <summary>
/// 複数のVRM10SpringBoneJointを一括操作するコントローラー
/// </summary>
public class SpringBoneController : MonoBehaviour
{
    [Header("収集対象")]
    [SerializeField] private GameObject targetRoot;

    [Header("SpringBone設定")]
    [SerializeField, Range(0f, 4f)] private float stiffnessForce = 1.0f;
    [SerializeField, Range(0f, 2f)] private float gravityPower = 0f;
    [SerializeField] private Vector3 gravityDir = new Vector3(0, -1, 0);
    [SerializeField, Range(0f, 1f)] private float dragForce = 0.4f;
    [SerializeField] private float jointRadius = 0.02f;

    [Header("管理対象")]
    [SerializeField] private List<VRM10SpringBoneJoint> joints = new();

    // 実行時のVrm10Instanceキャッシュ
    private Vrm10Instance cachedVrmInstance;

    public GameObject TargetRoot => targetRoot;
    public List<VRM10SpringBoneJoint> Joints => joints;
    public float StiffnessForce => stiffnessForce;
    public float GravityPower => gravityPower;
    public Vector3 GravityDir => gravityDir;
    public float DragForce => dragForce;
    public float JointRadius => jointRadius;

    /// <summary>
    /// targetRoot以下の全VRM10SpringBoneJointを収集
    /// </summary>
    public void CollectJoints()
    {
        joints.Clear();
        if (targetRoot == null) return;

        var found = targetRoot.GetComponentsInChildren<VRM10SpringBoneJoint>(true);
        joints.AddRange(found);

        // Vrm10Instanceもキャッシュ
        cachedVrmInstance = targetRoot.GetComponentInParent<Vrm10Instance>();
        if (cachedVrmInstance == null)
        {
            cachedVrmInstance = targetRoot.GetComponentInChildren<Vrm10Instance>();
        }
    }

    /// <summary>
    /// 現在の設定値を全ジョイントに適用
    /// </summary>
    public void ApplyToAll()
    {
        // 実行時: Runtimeに反映
        if (Application.isPlaying)
        {
            ApplyToRuntime();
        }

#if UNITY_EDITOR
        // エディタ: SerializedObjectで値を設定
        foreach (var joint in joints)
        {
            if (joint == null) continue;

            UnityEditor.Undo.RecordObject(joint, "Apply SpringBone Settings");

            var so = new UnityEditor.SerializedObject(joint);
            so.FindProperty("m_stiffnessForce").floatValue = stiffnessForce;
            so.FindProperty("m_gravityPower").floatValue = gravityPower;
            so.FindProperty("m_gravityDir").vector3Value = gravityDir;
            so.FindProperty("m_dragForce").floatValue = dragForce;
            so.FindProperty("m_jointRadius").floatValue = jointRadius;
            so.ApplyModifiedProperties();
        }
#endif
    }

    /// <summary>
    /// 実行時にSpringBoneランタイムへ値を反映
    /// </summary>
    private void ApplyToRuntime()
    {
        if (cachedVrmInstance == null)
        {
            // キャッシュがなければ再取得
            if (targetRoot != null)
            {
                cachedVrmInstance = targetRoot.GetComponentInParent<Vrm10Instance>();
                if (cachedVrmInstance == null)
                {
                    cachedVrmInstance = targetRoot.GetComponentInChildren<Vrm10Instance>();
                }
            }
        }

        if (cachedVrmInstance == null) return;

        var runtime = cachedVrmInstance.Runtime;
        if (runtime?.SpringBone == null) return;

        foreach (var joint in joints)
        {
            if (joint == null) continue;

            // コンポーネントの値を更新
            joint.m_stiffnessForce = stiffnessForce;
            joint.m_gravityPower = gravityPower;
            joint.m_gravityDir = gravityDir;
            joint.m_dragForce = dragForce;
            joint.m_jointRadius = jointRadius;

            // ランタイムに反映
            runtime.SpringBone.SetJointLevel(joint.transform, joint.Blittable);
        }
    }
}
