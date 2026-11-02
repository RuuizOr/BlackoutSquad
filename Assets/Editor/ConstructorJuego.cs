using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Menú "Blackout Squad/Construir juego": genera TODO el juego (capas, física, importación de arte
// y audio, fuentes, prefabs, tiles, escenas, interfaz y Build Profiles).
// Se puede ejecutar varias veces: sobrescribe prefabs y escenas sin duplicar nada.
public static partial class ConstructorJuego
{
    // ---------- Rutas ----------
    const string RutaArte = "Assets/Art";
    const string RutaGenerado = "Assets/Art/Generado";
    const string RutaAudio = "Assets/Audio";
    const string RutaPrefabs = "Assets/Prefabs";
    const string RutaDatos = "Assets/Datos";
    const string RutaEscenas = "Assets/Scenes";
    const string RutaNiveles = "Assets/Niveles";
    const string RutaResources = "Assets/Resources";

    // Unidades: personajes y objetos a 48 px por unidad (el soldado mide ~1.75 u), tiles a 32 px.
    const float PpuPersonajes = 48f;
    const float PpuTiles = 32f;
    const float AltoPantalla = 11.25f; // 540 px de referencia / 48 ppu

    static readonly string[] NombresEscenas = { "Splash", "MainMenu", "Nivel1", "Nivel2", "Nivel3" };

    static DateTime horaCargaCodigo;

    [InitializeOnLoadMethod]
    static void RegistrarCargaCodigo()
    {
        horaCargaCodigo = DateTime.UtcNow;
    }

    // Si hay scripts más nuevos que el código cargado, construir ahora guardaría datos viejos
    static bool CodigoDesactualizado()
    {
        if (EditorApplication.isCompiling) return true;
        return Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories).Any(f => File.GetLastWriteTimeUtc(f) > horaCargaCodigo);
    }

    static void Avisar(string mensaje)
    {
        EditorUtility.DisplayDialog("Blackout Squad", mensaje, "OK");
    }

    [MenuItem("Blackout Squad/Construir juego", priority = 0)]
    public static void ConstruirJuego()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Avisar("Detén el juego (botón Play) y vuelve a ejecutar 'Construir juego'.");
            return;
        }
        AssetDatabase.Refresh(); // detecta scripts e imágenes nuevos
        if (CodigoDesactualizado())
        {
            Avisar("Unity todavía está cargando los scripts nuevos. Espera a que termine y vuelve a ejecutar 'Construir juego'.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            Progreso("Configurando proyecto...", 0.02f);
            ConfigurarProyecto();
            Progreso("Importando arte...", 0.08f);
            ImportarArte();
            Progreso("Importando audio...", 0.2f);
            ImportarAudio();
            Progreso("Creando fuentes TMP...", 0.25f);
            CrearFuentes();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Progreso("Creando prefabs...", 0.3f);
            CrearPrefabs();
            Progreso("Creando tiles...", 0.55f);
            CrearTiles();

            Progreso("Escena Splash...", 0.6f);
            CrearEscenaSplash();
            Progreso("Escena MainMenu...", 0.65f);
            CrearEscenaMenu();
            for (int i = 1; i <= 3; i++)
            {
                Progreso("Escena Nivel" + i + "...", 0.65f + i * 0.1f);
                CrearEscenaNivel(i);
            }
            Progreso("Build Profiles...", 0.98f);
            ConfigurarEscenasBuild();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(RutaEscenas + "/Splash.unity");
            Debug.Log("<b>Blackout Squad</b>: ¡juego construido! Abre la escena Splash y presiona Play.");
        }
        catch (Exception e)
        {
            Debug.LogError("Blackout Squad: error al construir el juego: " + e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static void Progreso(string texto, float t)
    {
        EditorUtility.DisplayProgressBar("Blackout Squad", texto, t);
    }

    // =====================================================================
    // Configuración del proyecto: capas, matriz de colisiones, player settings
    // =====================================================================
    static void ConfigurarProyecto()
    {
        // Capas
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty capas = tagManager.FindProperty("layers");
        for (int i = 0; i < Capas.Nombres.Length; i++)
            capas.GetArrayElementAtIndex(Capas.Jugador + i).stringValue = Capas.Nombres[i];
        tagManager.ApplyModifiedPropertiesWithoutUndo();

        // Matriz de colisiones 2D: qué capas chocan entre sí
        var choques = new Dictionary<int, int[]>
        {
            { Capas.Jugador,     new[] { Capas.Suelo, Capas.Item, Capas.Disparador, Capas.BalaEnemigo } },
            { Capas.Enemigo,     new[] { Capas.Suelo, Capas.BalaJugador, Capas.Disparador } },
            { Capas.BalaJugador, new[] { Capas.Enemigo, Capas.Suelo } },
            { Capas.BalaEnemigo, new[] { Capas.Jugador, Capas.Suelo } },
            { Capas.Suelo,       new[] { Capas.Jugador, Capas.Enemigo, Capas.BalaJugador, Capas.BalaEnemigo, Capas.Suelo, 0 } },
            { Capas.Item,        new[] { Capas.Jugador } },
            { Capas.Disparador,  new[] { Capas.Jugador, Capas.Enemigo } },
        };
        int[] nuestras = { Capas.Jugador, Capas.Enemigo, Capas.BalaJugador, Capas.BalaEnemigo, Capas.Suelo, Capas.Item, Capas.Disparador };
        for (int a = 0; a < 32; a++)
        {
            for (int b = 0; b < 32; b++)
            {
                bool aNuestra = nuestras.Contains(a), bNuestra = nuestras.Contains(b);
                if (!aNuestra && !bNuestra) continue;
                bool chocan;
                if (aNuestra && bNuestra) chocan = choques[a].Contains(b) || choques[b].Contains(a);
                else
                {
                    int nuestra = aNuestra ? a : b, otra = aNuestra ? b : a;
                    chocan = nuestra == Capas.Suelo && otra == 0; // Default solo choca con el suelo
                }
                Physics2D.IgnoreLayerCollision(a, b, !chocan);
            }
        }
        SerializedObject fisica = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0]);
        SerializedProperty matriz = fisica.FindProperty("m_LayerCollisionMatrix");
        if (matriz != null && matriz.isArray)
        {
            for (int a = 0; a < 32 && a < matriz.arraySize; a++)
            {
                uint mascara = 0;
                for (int b = 0; b < 32; b++)
                    if (!Physics2D.GetIgnoreLayerCollision(a, b)) mascara |= 1u << b;
                matriz.GetArrayElementAtIndex(a).uintValue = mascara;
            }
            fisica.ApplyModifiedPropertiesWithoutUndo();
        }

        // Player settings (PC Windows)
        PlayerSettings.productName = "Blackout Squad";
        PlayerSettings.companyName = "Equipo Blackout Squad";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.runInBackground = false;

        foreach (string carpeta in new[] { RutaPrefabs, RutaDatos, RutaEscenas, RutaNiveles, RutaResources,
                 RutaPrefabs + "/Efectos", RutaPrefabs + "/Proyectiles", RutaPrefabs + "/Objetos",
                 RutaPrefabs + "/Enemigos", RutaPrefabs + "/Jugador", RutaPrefabs + "/Escenario",
                 RutaDatos + "/Armas", RutaDatos + "/Personajes", RutaNiveles + "/Tiles", RutaDatos + "/Materiales" })
            CrearCarpeta(carpeta);
    }

    static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        CrearCarpeta(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }

    // =====================================================================
    // Utilidades para asignar campos serializados con SerializedObject
    // =====================================================================
    public static void Asignar(Object componente, string campo, object valor)
    {
        SerializedObject so = new SerializedObject(componente);
        SerializedProperty p = so.FindProperty(campo);
        if (p == null)
        {
            Debug.LogError("Campo '" + campo + "' no existe en " + componente.GetType().Name);
            return;
        }
        EscribirValor(p, valor);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EscribirValor(SerializedProperty p, object valor)
    {
        switch (valor)
        {
            case null: p.objectReferenceValue = null; break;
            case Object o: p.objectReferenceValue = o; break;
            case bool b: p.boolValue = b; break;
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case string s: p.stringValue = s; break;
            case Color c: p.colorValue = c; break;
            case Vector2 v2: p.vector2Value = v2; break;
            case Vector3 v3: p.vector3Value = v3; break;
            case LayerMask m: p.intValue = m.value; break;
            case Enum e: p.intValue = Convert.ToInt32(e); break;
            case System.Collections.IList lista:
                p.arraySize = lista.Count;
                for (int k = 0; k < lista.Count; k++) EscribirValor(p.GetArrayElementAtIndex(k), lista[k]);
                break;
            default: Debug.LogError("Tipo no soportado: " + valor.GetType()); break;
        }
    }

    // Clips para AnimadorSprites: (nombre, frames, fps, bucle)
    public static void AsignarClips(AnimadorSprites anim, SpriteRenderer render, string inicial, params (string nombre, Sprite[] frames, float fps, bool bucle)[] clips)
    {
        SerializedObject so = new SerializedObject(anim);
        so.FindProperty("render").objectReferenceValue = render;
        so.FindProperty("clipInicial").stringValue = inicial;
        SerializedProperty lista = so.FindProperty("clips");
        lista.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++)
        {
            SerializedProperty e = lista.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("nombre").stringValue = clips[i].nombre;
            e.FindPropertyRelative("fps").floatValue = clips[i].fps;
            e.FindPropertyRelative("bucle").boolValue = clips[i].bucle;
            SerializedProperty f = e.FindPropertyRelative("frames");
            f.arraySize = clips[i].frames.Length;
            for (int k = 0; k < clips[i].frames.Length; k++) f.GetArrayElementAtIndex(k).objectReferenceValue = clips[i].frames[k];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        if (render != null && clips.Length > 0 && clips[0].frames.Length > 0) render.sprite = clips[0].frames[0];
    }

    // ---------- Carga de assets ----------
    static Sprite[] Frames(string ruta)
    {
        Sprite[] s = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>()
            .OrderBy(x => NumeroFinal(x.name)).ToArray();
        if (s.Length == 0) Debug.LogError("No hay sprites en " + ruta);
        return s;
    }

    static Sprite[] Elegir(Sprite[] origen, params int[] indices)
    {
        return indices.Select(i => origen[Mathf.Clamp(i, 0, origen.Length - 1)]).ToArray();
    }

    static int NumeroFinal(string nombre)
    {
        int i = nombre.LastIndexOf('_');
        return (i >= 0 && int.TryParse(nombre.Substring(i + 1), out int n)) ? n : 0;
    }

    static Sprite SpriteUnico(string ruta)
    {
        Sprite s = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        if (s == null) Debug.LogError("No se encontró sprite: " + ruta);
        return s;
    }

    static T Cargar<T>(string ruta) where T : Object
    {
        T a = AssetDatabase.LoadAssetAtPath<T>(ruta);
        if (a == null) Debug.LogError("No se encontró: " + ruta);
        return a;
    }

    static GameObject GuardarPrefab(GameObject go, string ruta)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ruta);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject Nuevo(string nombre, Transform padre = null, int capa = 0)
    {
        GameObject go = new GameObject(nombre);
        go.layer = capa;
        if (padre != null) go.transform.SetParent(padre, false);
        return go;
    }

    static SpriteRenderer AgregarSprite(GameObject go, Sprite sprite, int orden)
    {
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sortingOrder = orden;
        return r;
    }

    // =====================================================================
    // Build Profiles: Splash (0), MainMenu (1), Nivel1 (2), Nivel2 (3), Nivel3 (4)
    // =====================================================================
    static void ConfigurarEscenasBuild()
    {
        EditorBuildSettings.scenes = NombresEscenas
            .Select(n => new EditorBuildSettingsScene(RutaEscenas + "/" + n + ".unity", true)).ToArray();
    }
}
