# UnitySkinnedMeshCombiner
Combines Unity SkinnedMeshRenderers in Unity 2019. Takes a list of SkinnedMeshInstance and outputs a single mesh with working bones and blendshapes. Should avoid most GC, but there doesn't seem to be a way to avoid that with copying of blendshapes.

Each skinnedmeshinstance requires the Animator component because that's the only place Unity seems to keep the mapping for each bone. Also needs both the Mesh and SkinnedMeshRenderer, which allows you to modify the mesh before combining it.

Used in [Embark](https://steamcommunity.com/app/1055090).
