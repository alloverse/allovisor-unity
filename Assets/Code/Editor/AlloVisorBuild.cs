using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

public class BuildPlayerExample : MonoBehaviour
{
    [MenuItem("Build/Upgrade allonet to latest master")]
    public static void UpgradeAllonet()
    {
        Debug.Log("Not implemented yet");
    }

    // inspo: https://gist.github.com/sanukin39/997d8364d16c5c27dae75a3bc1f1f045
    [MenuItem("Build/Build for MacOS")]
    public static void MacBuild()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Menu/Menu.unity", "Assets/Scenes/NetworkScene.unity" };
        buildPlayerOptions.locationPathName = "Build/Mac/Alloverse Visor";
        buildPlayerOptions.target = BuildTarget.StandaloneOSX;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        // Add URL schemes
        PlistDocument info = new PlistDocument();
        string infoPath = "Build/Mac/Alloverse Visor.app/Contents/Info.plist";
        info.ReadFromFile(infoPath);
        AddUrlTypesToInfo(info);
        info.WriteToFile(infoPath);

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }
    }

    [MenuItem("Build/Build and Run")]
    public static void BuildAndRun()
    {
        MacBuild();
        var proc = new System.Diagnostics.Process();
        proc.StartInfo.FileName = "Build/Mac/Alloverse Visor.app/Contents/MacOS/allovisor";
        proc.Start();

    }

    private static void AddUrlTypesToInfo(PlistDocument info)
    {
        var urlTypes = info.root.CreateArray("CFBundleURLTypes");
        var placeType = urlTypes.AddDict();
        placeType.SetString("CFBundleURLName", "alloverse-place");
        var placeTypeSchemes = placeType.CreateArray("CFBundleURLSchemes");
        placeTypeSchemes.AddString("alloverse-place");
        placeTypeSchemes.AddString("alloplace");

        var applianceType = urlTypes.AddDict();
        applianceType.SetString("CFBundleURLName", "alloverse-appliance");
        var applianceTypeSchemes = applianceType.CreateArray("CFBundleURLSchemes");
        applianceTypeSchemes.AddString("alloverse-appliance+http");
        applianceTypeSchemes.AddString("alloverse-appliance+https");
        applianceTypeSchemes.AddString("alloappliance+http");
        applianceTypeSchemes.AddString("alloappliance+https");
        applianceTypeSchemes.AddString("alloapp+http");
        applianceTypeSchemes.AddString("alloapp+https");
    }

}
