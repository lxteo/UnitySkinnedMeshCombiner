using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkinnedMeshCombiner
{
    private readonly List<List<int>> resultIndices = new List<List<int>>();
    private readonly List<Vector3> resultVertices = new List<Vector3>();
    private readonly List<Vector2> resultUvs = new List<Vector2>();
    private readonly List<Vector3> resultNormals = new List<Vector3>();
    private readonly List<Vector4> resultTangents = new List<Vector4>();
    private readonly List<Color> resultColors = new List<Color>();
    private readonly List<BoneWeight> resultBoneWeight = new List<BoneWeight>();

    private readonly List<int> tempIndices = new List<int>();
    private readonly List<Vector3> tempVertices = new List<Vector3>();
    private readonly List<Vector2> tempUvs = new List<Vector2>();
    private readonly List<Vector3> tempNormals = new List<Vector3>();
    private readonly List<Vector4> tempTangents = new List<Vector4>();
    private readonly List<Color> tempColors = new List<Color>();
    private readonly List<BoneWeight> tempBoneWeights = new List<BoneWeight>();

    private readonly List<int[]> tempIndicesAssignment = new List<int[]>();

    private readonly List<int> boneMapping = new List<int>();
    private readonly Dictionary<int, HumanBodyBones> transformBoneMapping1 = new Dictionary<int, HumanBodyBones>();
    private readonly Dictionary<HumanBodyBones, int> transformBoneMapping2 = new Dictionary<HumanBodyBones, int>();
    private readonly Dictionary<int, int> transformBoneMapping3 = new Dictionary<int, int>();

    /// <summary>
    /// Combines Unity SkinnedMeshRenderers. Takes a list of SkinnedMeshInstance and outputs a single mesh with working bones and blendshapes.
    /// </summary>
    /// <param name="combine">List of SkinnedMeshInstance</param>
    /// <param name="result">Output, will set sharedMesh and sharedMaterials</param>
    public void CombineMeshes(List<SkinnedMeshInstance> combine, SkinnedMeshRenderer result)
    {
        //The instance to use as the base for rigging and blend shapes.
        var mainMesh = combine.First();
        Clear();

        var currentIndiceCount = 0;
        var currentSubmeshCount = 0;
        var isMainMesh = true;
        var previousSMR = mainMesh.SMR;
        for (var i = 0; i < combine.Count; i++)
        {
            var comb = combine[i];
            var hasBone = comb.SMR != null;

            if (hasBone && comb.SMR != previousSMR)
            {
                previousSMR = comb.SMR;
                MapBones(comb, mainMesh);
                isMainMesh = false;
            }

            if (resultIndices.Count <= currentSubmeshCount)
            {
                resultIndices.Add(new List<int>());
                tempIndicesAssignment.Add(new int[65535]);
            }
            var resultInd = resultIndices[currentSubmeshCount];
            comb.Mesh.GetIndices(tempIndices, comb.SubMeshIndex);
            comb.Mesh.GetVertices(tempVertices);
            comb.Mesh.GetUVs(0, tempUvs);
            comb.Mesh.GetNormals(tempNormals);
            comb.Mesh.GetTangents(tempTangents);
            comb.Mesh.GetColors(tempColors);

            if (hasBone)
            {
                comb.Mesh.GetBoneWeights(tempBoneWeights);
            }

            var tempAssign = tempIndicesAssignment[currentSubmeshCount];

            foreach (var ind in tempIndices)
            {
                if (tempAssign[ind] == 0)
                {
                    AddVertex(currentIndiceCount, resultInd, ind, comb.Transform);
                    if (hasBone)
                    {
                        if (isMainMesh)
                        {
                            resultBoneWeight.Add(tempBoneWeights[ind]);
                        }
                        else
                        {
                            resultBoneWeight.Add(GetMappedBoneWeight(tempBoneWeights[ind]));
                        }
                    }
                    else
                    {
                        resultBoneWeight.Add(default(BoneWeight));
                    }

                    tempAssign[ind] = currentIndiceCount;
                    currentIndiceCount += 1;
                }
                else
                {
                    resultInd.Add(tempAssign[ind]);
                }
            }
            currentSubmeshCount += 1;
        }

        var resultMesh = new Mesh();
        resultMesh.subMeshCount = currentSubmeshCount;
        resultMesh.SetVertices(resultVertices);
        resultMesh.SetUVs(0, resultUvs);
        resultMesh.SetNormals(resultNormals);
        resultMesh.SetTangents(resultTangents);
        resultMesh.SetColors(resultColors);
        resultMesh.boneWeights = resultBoneWeight.ToArray();

        for (var i = 0; i < currentSubmeshCount; i += 1)
        {
            resultMesh.SetTriangles(resultIndices[i], i, false);
        }

        CopyBlendShapes(mainMesh.Mesh, resultMesh);
        resultMesh.bindposes = mainMesh.Mesh.bindposes;
        resultMesh.RecalculateBounds();

        result.sharedMesh = resultMesh;
        result.sharedMaterials = combine.Select(c => c.Material).ToArray();
    }

    private void Clear()
    {
        foreach (var ind in resultIndices)
        {
            ind.Clear();
        }

        foreach (var ind in tempIndicesAssignment)
        {
            for (var i = 0; i < ind.Length; i += 1)
            {
                ind[i] = 0;
            }
        }

        resultVertices.Clear();
        resultUvs.Clear();
        resultNormals.Clear();
        resultTangents.Clear();
        resultColors.Clear();
        resultBoneWeight.Clear();
    }

    private void AddVertex(int currentIndiceCount, List<int> resultInd, int ind, Matrix4x4 transform)
    {
        resultInd.Add(currentIndiceCount);
        resultVertices.Add(transform * tempVertices[ind]);
        resultUvs.Add(tempUvs[ind]);
        resultNormals.Add(tempNormals[ind]);
        resultTangents.Add(tempTangents[ind]);
        if (tempColors.Count > 0)
        {
            resultColors.Add(tempColors[ind]);
        }
        else
        {
            resultColors.Add(default(Color));
        }
    }

    private void MapBones(SkinnedMeshInstance extraMesh, SkinnedMeshInstance targetMesh)
    {
        boneMapping.Clear();
        transformBoneMapping1.Clear();
        transformBoneMapping2.Clear();
        transformBoneMapping3.Clear();

        for (var val = HumanBodyBones.Hips; val < HumanBodyBones.LastBone; val += 1)
        {
            var itemBone = extraMesh.Animator.GetBoneTransform(val);
            if (itemBone != null)
            {
                transformBoneMapping1.Add(itemBone.GetInstanceID(), val);
            }

            var mainBone = targetMesh.Animator.GetBoneTransform(val);
            if (mainBone != null)
            {
                transformBoneMapping2.Add(val, mainBone.GetInstanceID());
            }
        }

        for (var i = 0; i < targetMesh.SMR.bones.Length; i += 1)
        {
            var bone = targetMesh.SMR.bones[i].GetInstanceID();
            transformBoneMapping3[bone] = i;
        }

        for (var i = 0; i < extraMesh.SMR.bones.Length; i += 1)
        {
            var result = -1;
            var bone = extraMesh.SMR.bones[i].GetInstanceID();
            if (transformBoneMapping1.ContainsKey(bone))
            {
                var key = transformBoneMapping1[bone];
                if (transformBoneMapping2.ContainsKey(key))
                {
                    var finalTransformId = transformBoneMapping2[key];
                    if (transformBoneMapping3.ContainsKey(finalTransformId))
                    {
                        result = transformBoneMapping3[finalTransformId];
                    }
                }
            }

            boneMapping.Add(result);
        }
    }

    private BoneWeight GetMappedBoneWeight(BoneWeight boneWeight)
    {
        if (this.boneMapping[boneWeight.boneIndex0] == -1)
        {
            boneWeight.boneIndex0 = 0;
            boneWeight.weight0 = 0;
        }
        else
        {
            boneWeight.boneIndex0 = this.boneMapping[boneWeight.boneIndex0];
        }

        if (this.boneMapping[boneWeight.boneIndex1] == -1)
        {
            boneWeight.boneIndex1 = 0;
            boneWeight.weight1 = 0;
        }
        else
        {
            boneWeight.boneIndex1 = this.boneMapping[boneWeight.boneIndex1];
        }

        if (this.boneMapping[boneWeight.boneIndex2] == -1)
        {
            boneWeight.boneIndex2 = 0;
            boneWeight.weight2 = 0;
        }
        else
        {
            boneWeight.boneIndex2 = this.boneMapping[boneWeight.boneIndex2];
        }

        if (this.boneMapping[boneWeight.boneIndex3] == -1)
        {
            boneWeight.boneIndex3 = 0;
            boneWeight.weight3 = 0;
        }
        else
        {
            boneWeight.boneIndex3 = this.boneMapping[boneWeight.boneIndex3];
        }

        return boneWeight;
    }

    private void CopyBlendShapes(Mesh input, Mesh output)
    {
        if (input.blendShapeCount <= 0)
        {
            return;
        }

        var inputDeltaVertices = new Vector3[input.vertexCount];
        var inputDeltaNormals = new Vector3[input.vertexCount];
        var inputDeltaTangents = new Vector3[input.vertexCount];
        var deltaVertices = new Vector3[output.vertexCount];
        var deltaNormals = new Vector3[output.vertexCount];
        var deltaTangents = new Vector3[output.vertexCount];

        for (var i = 0; i < input.blendShapeCount; i += 1)
        {
            var frameCount = input.GetBlendShapeFrameCount(i);
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var name = input.GetBlendShapeName(i);
                var weight = input.GetBlendShapeFrameWeight(i, frameIndex);

                input.GetBlendShapeFrameVertices(i, frameIndex, inputDeltaVertices, inputDeltaNormals, inputDeltaTangents);

                var inputLength = Math.Min(inputDeltaVertices.Length, deltaVertices.Length);
                for (var j = 0; j < inputLength; j += 1)
                {
                    deltaVertices[tempIndicesAssignment[0][j]] = inputDeltaVertices[j];
                    deltaNormals[tempIndicesAssignment[0][j]] = inputDeltaNormals[j];
                    deltaTangents[tempIndicesAssignment[0][j]] = inputDeltaTangents[j];
                }

                output.AddBlendShapeFrame(name, weight, deltaVertices, deltaNormals, deltaTangents);
            }

        }
    }
}
