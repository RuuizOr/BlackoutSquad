using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Importación de texturas (pixel art), audio y creación de fuentes TMP.
public static partial class ConstructorJuego
{
    static Material materialContorno, materialHud;
    static TMP_FontAsset fuenteTitulos, fuenteHud;

    struct Importacion
    {
        public float ppu;
        public Vector2Int celda;   // (0,0) = sprite único
        public Vector2 pivote;
        public Vector4 borde;
        public bool repetir;
    }

    static Importacion Imp(float ppu, Vector2 pivote, int celdaX = 0, int celdaY = 0, bool repetir = false, float borde = 0f)
    {
        return new Importacion { ppu = ppu, pivote = pivote, celda = new Vector2Int(celdaX, celdaY), repetir = repetir, borde = Vector4.one * borde };
    }

    static readonly Vector2 Abajo = new Vector2(0.5f, 0f);
    static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
    static readonly Vector2 AbajoIzq = Vector2.zero;
    static readonly Vector2 Izquierda = new Vector2(0f, 0.5f);

    static void ImportarArte()
    {
        const float ppuFondo = 160f / AltoPantalla; // capas de ansimuz: 160 px = alto de pantalla
        var reglas = new Dictionary<string, Importacion>
        {
            // Personajes (hojas en cuadrícula, pies abajo)
            { "Personajes/comando_alfa.png", Imp(PpuPersonajes, Abajo, 96, 120) },
            { "Personajes/comando_bravo.png", Imp(PpuPersonajes, Abajo, 140, 150) },
            { "Personajes/soldado_raso.png", Imp(PpuPersonajes, Abajo, 72, 96) },
            { "Personajes/soldado_escudo.png", Imp(PpuPersonajes, Abajo, 72, 96) },
            { "Personajes/soldado_elite.png", Imp(PpuPersonajes, Abajo, 72, 96) },
            { "Personajes/kruger.png", Imp(PpuPersonajes, Abajo, 80, 100) },
            { "Personajes/torreta.png", Imp(PpuPersonajes, Abajo, 96, 96) },
            { "Personajes/tanque.png", Imp(PpuPersonajes, Abajo, 144, 64) },
            { "Personajes/cañon_jefe.png", Imp(PpuPersonajes, Centro, 48, 48) },
            { "Personajes/jefe_campamento.png", Imp(PpuPersonajes, Abajo) },
            { "Personajes/punto_control.png", Imp(PpuPersonajes, Abajo) },
            { "Personajes/maquina_fortaleza.png", Imp(PpuPersonajes, Abajo) },
            // Proyectiles
            { "Proyectiles/misil.png", Imp(PpuPersonajes, Centro, 72, 32) },
            { "Proyectiles/llama.png", Imp(PpuPersonajes, Centro, 24, 24) },
            // Efectos
            { "Efectos/explosion_grande.png", Imp(PpuPersonajes, Centro, 92, 92) },
            { "Efectos/explosion_chica.png", Imp(PpuPersonajes, Centro, 60, 60) },
            { "Efectos/humo.png", Imp(PpuPersonajes, Centro, 50, 50) },
            { "Efectos/fogonazo.png", Imp(PpuPersonajes, Izquierda, 16, 16) },
            // Objetos del escenario
            { "Objetos/bandera.png", Imp(PpuTiles, Abajo, 32, 64) },
            { "Objetos/alambre_puas.png", Imp(PpuTiles, Abajo) },
            { "Objetos/contenedor_verde.png", Imp(PpuTiles, AbajoIzq) },
            { "Objetos/contenedor_oxido.png", Imp(PpuTiles, AbajoIzq) },
            { "Objetos/caja_madera.png", Imp(PpuTiles, AbajoIzq) },
            { "Objetos/bloque_grieta.png", Imp(PpuTiles, AbajoIzq) },
            { "Objetos/sacos_arena.png", Imp(PpuTiles, Abajo) },
            { "Objetos/barrera_arena.png", Imp(PpuTiles, Abajo) },
            { "Objetos/carpa.png", Imp(PpuTiles, Abajo) },
            { "Objetos/antorcha.png", Imp(PpuTiles, Abajo) },
            { "Objetos/arbusto_1.png", Imp(PpuTiles, Abajo) },
            { "Objetos/arbusto_2.png", Imp(PpuTiles, Abajo) },
            { "Objetos/flor_roja.png", Imp(PpuTiles, Abajo) },
            { "Objetos/troncos.png", Imp(PpuTiles, Abajo) },
            { "Objetos/palmera_grande.png", Imp(40f, Abajo) },
            { "Objetos/palmera_chica.png", Imp(40f, Abajo) },
            { "Objetos/arbusto_fierce.png", Imp(40f, Abajo) },
            { "Objetos/helecho.png", Imp(40f, Abajo) },
            { "Objetos/puas_metal.png", Imp(40f, Abajo) },
            { "Objetos/cofre_cerrado.png", Imp(PpuPersonajes, Abajo) },
            { "Objetos/cofre_abierto.png", Imp(PpuPersonajes, Abajo) },
            { "Objetos/cofre_metal_cerrado.png", Imp(PpuPersonajes, Abajo) },
            { "Objetos/cofre_metal_abierto.png", Imp(PpuPersonajes, Abajo) },
            // Tiles
            { "Tiles/tiles_selva.png", Imp(PpuTiles, Centro, 32, 32) },
            { "Tiles/tiles_base.png", Imp(PpuTiles, Centro, 32, 32) },
            { "Tiles/tiles_fortaleza.png", Imp(PpuTiles, Centro, 32, 32) },
            // Fondos generados (180 px de alto = pantalla completa)
            { "Fondos/selva_cielo.png", Imp(180f / AltoPantalla, Abajo, 0, 0, true) },
            { "Fondos/selva_lejos.png", Imp(ppuFondo, Abajo, 0, 0, true) },
            { "Fondos/selva_montes.png", Imp(ppuFondo, Abajo, 0, 0, true) },
            { "Fondos/selva_palmeras.png", Imp(180f / AltoPantalla, Abajo, 0, 0, true) },
            // Interfaz
            { "UI/panel.png", Imp(100f, Centro, 0, 0, false, 12f) },
            { "UI/boton.png", Imp(100f, Centro, 0, 0, false, 12f) },
            { "UI/barra_marco.png", Imp(100f, Centro, 0, 0, false, 6f) },
        };

        foreach (string ruta in Directory.GetFiles(RutaArte, "*.png", SearchOption.AllDirectories))
        {
            string r = ruta.Replace('\\', '/');
            Importacion imp;
            if (r.StartsWith(RutaGenerado + "/"))
            {
                string clave = r.Substring(RutaGenerado.Length + 1);
                if (!reglas.TryGetValue(clave, out imp))
                    imp = clave.StartsWith("UI/") ? Imp(100f, Centro) : Imp(PpuPersonajes, Centro);
            }
            else if (r.Contains("Parallax")) imp = Imp(ppuFondo, Abajo, 0, 0, true);
            else imp = Imp(PpuTiles, Centro); // packs originales (solo referencia)
            ConfigurarTextura(r, imp);
        }
    }

    static void ConfigurarTextura(string ruta, Importacion c)
    {
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) return;
        bool multiple = c.celda.x > 0;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        imp.spritePixelsPerUnit = c.ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = c.repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.maxTextureSize = 4096; // con el máximo de 2048 Unity encoge las hojas anchas y deforma los cuadros

        TextureImporterSettings s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect; // necesario para el modo "Tiled"
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = c.pivote;
        s.spriteBorder = c.borde;
        s.spriteGenerateFallbackPhysicsShape = false;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();

        if (multiple) CortarEnCuadricula(imp, ruta, c);
    }

    // Corta la hoja en celdas iguales. Conserva los IDs de los sprites existentes
    // para que los prefabs no pierdan sus referencias al reconstruir.
    static void CortarEnCuadricula(TextureImporter imp, string ruta, Importacion c)
    {
        imp.GetSourceTextureWidthAndHeight(out int ancho, out int alto);
        SpriteDataProviderFactories fabrica = new SpriteDataProviderFactories();
        fabrica.Init();
        ISpriteEditorDataProvider datos = fabrica.GetSpriteEditorDataProviderFromObject(imp);
        datos.InitSpriteEditorDataProvider();
        SpriteRect[] existentes = datos.GetSpriteRects();

        string baseNombre = Path.GetFileNameWithoutExtension(ruta);
        int columnas = ancho / c.celda.x, filas = alto / c.celda.y;
        List<SpriteRect> nuevos = new List<SpriteRect>();
        int indice = 0;
        for (int f = 0; f < filas; f++)
        {
            for (int col = 0; col < columnas; col++)
            {
                string nombre = baseNombre + "_" + indice++;
                SpriteRect sr = existentes.FirstOrDefault(e => e.name == nombre) ?? new SpriteRect { spriteID = GUID.Generate() };
                sr.name = nombre;
                sr.rect = new Rect(col * c.celda.x, alto - (f + 1) * c.celda.y, c.celda.x, c.celda.y);
                sr.alignment = SpriteAlignment.Custom;
                sr.pivot = c.pivote;
                sr.border = Vector4.zero;
                nuevos.Add(sr);
            }
        }
        datos.SetSpriteRects(nuevos.ToArray());
        ISpriteNameFileIdDataProvider ids = datos.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (ids != null) ids.SetNameFileIdPairs(nuevos.Select(n => new SpriteNameFileIdPair(n.name, n.spriteID)).ToList());
        datos.Apply();
        imp.SaveAndReimport();
    }

    static void ImportarAudio()
    {
        foreach (string ruta in Directory.GetFiles(RutaAudio, "*.*", SearchOption.AllDirectories))
        {
            if (ruta.EndsWith(".meta") || ruta.EndsWith(".txt")) continue;
            AudioImporter imp = AssetImporter.GetAtPath(ruta.Replace('\\', '/')) as AudioImporter;
            if (imp == null) continue;
            bool esMusica = ruta.Replace('\\', '/').Contains("/Musica/");
            AudioImporterSampleSettings s = imp.defaultSampleSettings;
            s.loadType = esMusica ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = esMusica ? 0.6f : 0.8f;
            imp.defaultSampleSettings = s;
            imp.forceToMono = !esMusica;
            imp.SaveAndReimport();
        }
    }

    // =====================================================================
    // Fuentes TMP (Black Ops One para títulos y botones, Press Start 2P para el HUD)
    // =====================================================================
    const string Caracteres = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz" +
                              "ÁÉÍÓÚÜÑáéíóúüñ¡¿«»·×—–'∞";

    static void CrearFuentes()
    {
        fuenteTitulos = CrearFuente("Assets/Fuentes/BlackOpsOne-Regular.ttf", "Assets/Fuentes/BlackOpsOne SDF.asset", 64);
        fuenteHud = CrearFuente("Assets/Fuentes/PressStart2P-Regular.ttf", "Assets/Fuentes/PressStart2P SDF.asset", 48);

        materialContorno = CrearMaterialTexto(fuenteTitulos, "Assets/Fuentes/BlackOpsOne Contorno.mat", 0.18f, true);
        materialHud = CrearMaterialTexto(fuenteHud, "Assets/Fuentes/PressStart2P Contorno.mat", 0.25f, true);
    }

    static TMP_FontAsset CrearFuente(string rutaTtf, string rutaAsset, int tamañoMuestreo)
    {
        TMP_FontAsset fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(rutaAsset);
        if (fa != null) return fa; // ya existe: no lo recreamos para no romper referencias

        Font ttf = Cargar<Font>(rutaTtf);
        fa = TMP_FontAsset.CreateFontAsset(ttf, tamañoMuestreo, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        fa.name = Path.GetFileNameWithoutExtension(rutaAsset);
        AssetDatabase.CreateAsset(fa, rutaAsset);
        // Conservar los caracteres ya generados al hacer el build (la propiedad es interna en TMP)
        SerializedObject so = new SerializedObject(fa);
        SerializedProperty limpiar = so.FindProperty("m_ClearDynamicDataOnBuild");
        if (limpiar != null) { limpiar.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
        fa.atlasTextures[0].name = fa.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = fa.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        fa.TryAddCharacters(Caracteres, out string _);
        EditorUtility.SetDirty(fa.atlasTextures[0]);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    static Material CrearMaterialTexto(TMP_FontAsset fa, string ruta, float contorno, bool sombra)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        bool nuevo = m == null;
        if (nuevo) m = new Material(fa.material);
        else m.CopyPropertiesFromMaterial(fa.material);
        m.shaderKeywords = fa.material.shaderKeywords;
        m.EnableKeyword(ShaderUtilities.Keyword_Outline);
        m.SetFloat(ShaderUtilities.ID_OutlineWidth, contorno);
        m.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.05f, 0.06f, 0.04f));
        if (sombra)
        {
            m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.7f));
            m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
            m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
        }
        if (nuevo) AssetDatabase.CreateAsset(m, ruta);
        EditorUtility.SetDirty(m);
        return m;
    }
}
