using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menú "Blackout Squad/Compilar build Windows": genera el ejecutable en la carpeta Build/Windows.
public static class CompilarBuild
{
    [MenuItem("Blackout Squad/Compilar build Windows", priority = 20)]
    public static void CompilarWindows()
    {
        string[] escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (escenas.Length == 0)
        {
            EditorUtility.DisplayDialog("Blackout Squad", "No hay escenas en Build Profiles. Ejecuta primero 'Blackout Squad/Construir juego'.", "OK");
            return;
        }

        string carpeta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build", "Windows");
        Directory.CreateDirectory(carpeta);
        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = Path.Combine(carpeta, "BlackoutSquad.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        if (reporte.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build listo en: " + carpeta);
            EditorUtility.RevealInFinder(opciones.locationPathName);
        }
        else
        {
            Debug.LogError("El build falló: " + reporte.summary.result + " (revisa la consola)");
        }
    }

    // Menú "Blackout Squad/Compilar build Web": genera la versión WebGL en Build/Web para publicarla en GitHub Pages.
    // Sin compresión, porque GitHub Pages no envía los encabezados que necesitan Brotli/Gzip.
    [MenuItem("Blackout Squad/Compilar build Web", priority = 21)]
    public static void CompilarWeb()
    {
        string[] escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (escenas.Length == 0)
        {
            Debug.LogError("No hay escenas en Build Profiles. Ejecuta primero 'Blackout Squad/Construir juego'.");
            return;
        }

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;

        string carpeta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build", "Web");
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        Directory.CreateDirectory(carpeta);

        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = carpeta,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        if (reporte.summary.result == BuildResult.Succeeded)
            Debug.Log("Build web listo en: " + carpeta);
        else
            Debug.LogError("El build web falló: " + reporte.summary.result + " (revisa la consola)");
    }

    // Menú "Blackout Squad/Compilar build Android": genera Build/Android/BlackoutSquad.apk para instalar en el celular.
    // Pantalla siempre horizontal, IL2CPP + ARM64 (lo que usan los teléfonos actuales).
    [MenuItem("Blackout Squad/Compilar build Android", priority = 22)]
    public static void CompilarAndroid()
    {
        string[] escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (escenas.Length == 0)
        {
            Debug.LogError("No hay escenas en Build Profiles. Ejecuta primero 'Blackout Squad/Construir juego'.");
            return;
        }

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("El módulo de Android no está cargado. Instálalo desde Unity Hub (Android Build Support) y reinicia Unity.");
            return;
        }

        ConfigurarAndroid();

        string carpeta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build", "Android");
        Directory.CreateDirectory(carpeta);
        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = Path.Combine(carpeta, "BlackoutSquad.apk"),
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        if (reporte.summary.result == BuildResult.Succeeded && File.Exists(opciones.locationPathName))
            Debug.Log("Build Android listo en: " + opciones.locationPathName);
        else
            Debug.LogError("El build Android falló: " + reporte.summary.result + " (revisa la consola)");
    }

    static void ConfigurarAndroid()
    {
        NamedBuildTarget android = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(android, "com.equipoblackoutsquad.blackoutsquad");
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = false; // .apk para instalar directo (no .aab de Play Store)

        // Solo horizontal (las dos orientaciones)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
    }
}
