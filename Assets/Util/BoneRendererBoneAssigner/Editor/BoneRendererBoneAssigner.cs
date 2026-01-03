using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditor;

/// <summary>
/// BoneRenderer に Humanoid Hips 以下の全ボーンを自動アサイン
/// </summary>
public static class BoneRendererBoneAssigner
{
    [MenuItem("CONTEXT/BoneRenderer/Assign Humanoid Bones (Hips)")]
    private static void AssignFromBoneRenderer(MenuCommand command)
    {
        var boneRenderer = command.context as BoneRenderer;
        if (boneRenderer != null)
            AssignHumanoidBones(boneRenderer);
    }

    [MenuItem("CONTEXT/BoneRenderer/Assign Humanoid Bones (Hips)", true)]
    private static bool ValidateBoneRenderer(MenuCommand command)
    {
        var boneRenderer = command.context as BoneRenderer;
        if (boneRenderer == null) return false;
        var animator = boneRenderer.GetComponentInParent<Animator>();
        return animator != null && animator.isHuman;
    }

    public static void AssignHumanoidBones(BoneRenderer boneRenderer)
    {
        var animator = boneRenderer.GetComponentInParent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogWarning("Humanoid Animator not found");
            return;
        }

        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null)
        {
            Debug.LogWarning("Hips bone not found");
            return;
        }

        Undo.RecordObject(boneRenderer, "Assign Humanoid Bones");

        var bones = new List<Transform>();
        CollectDescendants(hips, bones);
        boneRenderer.transforms = bones.ToArray();

        Debug.Log($"Assigned {bones.Count} bones from Hips");
    }

    private static void CollectDescendants(Transform parent, List<Transform> result)
    {
        result.Add(parent);
        for (int i = 0; i < parent.childCount; i++)
        {
            CollectDescendants(parent.GetChild(i), result);
        }
    }
}
