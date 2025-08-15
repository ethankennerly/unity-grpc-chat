#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class NuGetRestoreOnLoad
{
    static NuGetRestoreOnLoad()
    {
        // If NuGetForUnity is present, kick a restore once per domain reload.
        var t = System.Type.GetType("NuGetForUnity.NugetHelper, NuGetForUnity");
        if (t != null)
        {
            // NuGetForUnity 3.x+: MenuCommand is enough; this keeps it decoupled.
            EditorApplication.delayCall += () =>
            {
                // Equivalent to: NuGet → Restore Packages
                EditorApplication.ExecuteMenuItem("NuGet/Restore Packages");
            };
        }
    }
}
#endif
