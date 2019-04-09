# UnitySkinnedMeshCombiner
Combines Unity SkinnedMeshRenderers in Unity 2019. Takes a list of SkinnedMeshInstance and outputs a single mesh with working bones and blendshapes. Should avoid most GC, but there doesn't seem to be a way to avoid that with copying of blendshapes.
