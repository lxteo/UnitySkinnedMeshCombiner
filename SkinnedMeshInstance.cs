using UnityEngine;

public struct SkinnedMeshInstance
{
    public Mesh Mesh { get; set; }
    public SkinnedMeshRenderer SMR { get; set; }
    public int SubMeshIndex { get; set; }
    public Matrix4x4 Transform { get; set; }
    public Material Material { get; set; }
    public Animator Animator { get; set; }
}
