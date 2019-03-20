using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using System.Net;
using GitHub.ICSharpCode.SharpZipLib.Zip;
using System.IO;

public class AllovisorBuilder : MonoBehaviour
{
    [MenuItem("Build/Download allonet assets")]
    public static void DownloadAllonet()
    {

        string currentVersion = File.Exists("Assets/allonet/allonet.cache") ?
            File.ReadAllText("Assets/allonet/allonet.cache") : 
            "-1";
        string targetVersion = File.ReadAllText("Assets/allonet/allonet.lock");
        if(currentVersion == targetVersion)
        {
            Debug.Log("Allonet is up to date");
            return;
        }

        // uhmmmmmm Unity's .Net runtime is out of date and doesn't understand SHA256 on Windows. ffs.
        ServicePointManager.ServerCertificateValidationCallback += delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) {
            return true;
        };


        using (WebClient wc = new WebClient())
        {
            Download(wc, targetVersion, "Allonet-Linux-x64", "Allonet-Linux-x64/build/liballonet.so", "liballonet.so");
            Download(wc, targetVersion, "Allonet-Windows-x64", "Allonet-Windows-x64/build/Debug/allonet.dll", "liballonet.dll");
            Download(wc, targetVersion, "Allonet-Mac-x64", "Allonet-Mac-x64/build/liballonet.dylib", "liballonet.bundle");
        }
        System.IO.File.WriteAllText("Assets/allonet/allonet.cache", targetVersion);
        Debug.Log("Finished downloading Allonet for all platforms");
    }

    private static void Download(WebClient wc, string targetVersion, string artifactName, string path, string destination)
    {
        Debug.Log("Downloading Allonet "+artifactName);
        var buildlisturl = "https://dev.azure.com/alloverse/allonet/_apis/build/builds/" + targetVersion + "/artifacts?artifactName=" + artifactName + "&api-version=5.0";
        var jsons = wc.DownloadString(buildlisturl);
        var json = LitJson.JsonMapper.ToObject(jsons);
        var artifactUrl = json["resource"]["downloadUrl"].ToString();
        wc.DownloadFile(artifactUrl, "out.zip");
        var zip = new ZipFile("out.zip");
        ZipEntry entry = zip.GetEntry(path);
        using (FileStream outputFile = File.Create("Assets/allonet/"+destination))
        {
            Stream zippedStream = zip.GetInputStream(entry);
            byte[] dataBuffer = new byte[256 * 1024];
            int readBytes;
            while ((readBytes = zippedStream.Read(dataBuffer, 0, 256 * 1024)) > 0)
            {
                outputFile.Write(dataBuffer, 0, readBytes);
                outputFile.Flush();
            }
        }
        zip.Close();
        File.Delete("out.zip");
    }

    [MenuItem("Build/Upgrade allonet")]
    public static void UpgradeAllonet()
    {
        using (WebClient wc = new WebClient())
        {
            var jsons = wc.DownloadString("https://dev.azure.com/alloverse/allonet/_apis/build/builds?api-version=5.0");
            var json = LitJson.JsonMapper.ToObject(jsons);
            var latestId = json["value"][0]["id"].ToString();
            Debug.Log("Latest Allonet build ID is " + latestId);
            System.IO.File.WriteAllText("Assets/allonet/allonet.lock", latestId);
        }
        DownloadAllonet();
    }

    // inspo: https://gist.github.com/sanukin39/997d8364d16c5c27dae75a3bc1f1f045
    [MenuItem("Build/[Mac] Build")]
    public static void MacBuild()
    {
        DownloadAllonet();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = Scenes();
        buildPlayerOptions.locationPathName = "Build/Mac/Alloverse Visor";
        buildPlayerOptions.target = BuildTarget.StandaloneOSX;
        buildPlayerOptions.options = BuildOptions.ShowBuiltPlayer;

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

    [MenuItem("Build/[Mac] Build and Run")]
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

    private static string[] Scenes()
    {
        return new[] { "Assets/Menu/Menu.unity", "Assets/Scenes/NetworkScene.unity" };
    }

    [MenuItem("Build/[Android] Build and Install")]
    public static void AndroidBuild()
    {
        DownloadAllonet();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = Scenes();
        buildPlayerOptions.locationPathName = "Build/Android/AlloverseVisor.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.AutoRunPlayer;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        // Add URL schemes
        // TODO!!

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }
    }

}
