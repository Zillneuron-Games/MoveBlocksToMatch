#if UNITY_EDITOR && UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Zillneuron.Blockmates.Services.SignIn
{
    internal static class AppleSignInBuild
    {
        [PostProcessBuild]
        private static void AddCapability(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            var capabilities = new ProjectCapabilityManager(
                projectPath, "Blockmates.entitlements", targetGuid: project.GetUnityMainTargetGuid());
            capabilities.AddSignInWithApple();
            capabilities.WriteToFile();
        }
    }
}
#endif
