using System.IO;
using System.Linq;
using UnityEditor;
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
}
