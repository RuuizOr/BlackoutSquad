using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Convierte los mapas de texto (Assets/Niveles/Mapas/NivelN.txt) en escenas jugables.
public static partial class ConstructorJuego
{
    // Orden de los tiles en cada hoja: 0 suelo superior, 1 relleno, 2 plataforma, 3 agua superficie, 4 agua
    static readonly Dictionary<string, Tile[]> tiles = new Dictionary<string, Tile[]>();

    static void CrearTiles()
    {
        foreach (string tema in new[] { "selva", "base", "fortaleza" })
        {
            Sprite[] s = Frames(G("Tiles/tiles_" + tema + ".png"));
            Tile[] t = new Tile[5];
            for (int i = 0; i < 5; i++)
            {
                string ruta = RutaNiveles + "/Tiles/" + tema + "_" + i + ".asset";
                t[i] = CrearOActualizar(ruta, () => ScriptableObject.CreateInstance<Tile>());
                t[i].sprite = s[i];
                t[i].color = Color.white;
                t[i].colliderType = (i <= 2) ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                EditorUtility.SetDirty(t[i]);
            }
            tiles[tema] = t;
        }
    }

    // Datos de presentación de cada nivel
    class ConfigNivel
    {
        public string tema, titulo, subtitulo, briefing = "", final = "";
        public Color colorCielo;
        public float vida, agresividad, botiquines;
        public (string ruta, float factor, float alturaPiso, Color color)[] fondo;
        public bool contenedoresOxido;
    }

    static ConfigNivel Config(int n)
    {
        const string m = RutaArte + "/ParallaxMountain_ansimuz/";
        const string ind = RutaArte + "/ParallaxIndustrial_ansimuz/";
        Color c = Color.white;
        switch (n)
        {
            case 1:
                return new ConfigNivel
                {
                    tema = "selva", titulo = "MISIÓN 1: SELVA HOSTIL", subtitulo = "Llega al campamento enemigo",
                    colorCielo = new Color(0.4f, 0.68f, 0.8f), vida = 1f, agresividad = 1f, botiquines = 1f,
                    briefing = TextoBriefing,
                    fondo = new[]
                    {
                        (G("Fondos/selva_cielo.png"), 0f, 0f, c),
                        (G("Fondos/selva_lejos.png"), 0.08f, 1.5f, c),
                        (G("Fondos/selva_montes.png"), 0.2f, 0.5f, c),
                        (G("Fondos/selva_palmeras.png"), 0.45f, 1.2f, c),
                    }
                };
            case 2:
                return new ConfigNivel
                {
                    tema = "base", titulo = "MISIÓN 2: BASE MILITAR", subtitulo = "Destruye el punto de control",
                    colorCielo = new Color(0.42f, 0.3f, 0.42f), vida = 1.3f, agresividad = 1.2f, botiquines = 0.7f,
                    fondo = new[]
                    {
                        (m + "parallax-mountain-bg.png", 0f, 0f, c),
                        (m + "parallax-mountain-montain-far.png", 0.08f, 0.5f, c),
                        (m + "parallax-mountain-mountains.png", 0.18f, 0f, c),
                        (ind + "industrial_0002_far-buildings.png", 0.35f, 1.5f, new Color(0.75f, 0.6f, 0.75f)),
                        (m + "parallax-mountain-foreground-trees.png", 0.55f, 0.5f, new Color(0.7f, 0.6f, 0.7f)),
                    }
                };
            default:
                return new ConfigNivel
                {
                    tema = "fortaleza", titulo = "MISIÓN 3: LA FORTALEZA", subtitulo = "Derrota al Comandante Kruger",
                    colorCielo = new Color(0.12f, 0.2f, 0.18f), vida = 1.6f, agresividad = 1.4f, botiquines = 0.5f,
                    final = TextoFinal, contenedoresOxido = true,
                    fondo = new[]
                    {
                        (ind + "industrial_0003_bg.png", 0f, 0f, c),
                        (ind + "industrial_0002_far-buildings.png", 0.12f, 1.5f, c),
                        (ind + "industrial_0001_buildings.png", 0.3f, 0.8f, c),
                        (ind + "industrial_0000_foreground.png", 0.5f, 1f, new Color(0.75f, 0.8f, 0.75f)),
                    }
                };
        }
    }

    const string TextoBriefing =
        "<b>OPERACIÓN APAGÓN</b>\n\n" +
        "La facción hostil del Comandante Kruger prepara una ofensiva desde su fortaleza en las montañas.\n\n" +
        "El escuadrón <color=#F0A030>BLACKOUT SQUAD</color> ha sido enviado a infiltrarse en su territorio: " +
        "cruzar la selva, tomar la base militar y llegar hasta Kruger para detener la ofensiva.\n\n" +
        "Tu misión, Comando Alfa: abrir el camino.";

    const string TextoFinal =
        "Kruger ha caído y su fortaleza arde a tus espaldas. Sin su comandante, la ofensiva se derrumba antes de empezar.\n" +
        "El Blackout Squad regresa a casa. Misión cumplida.";

    // =====================================================================
    // Escena de nivel
    // =====================================================================
    static void CrearEscenaNivel(int n)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ConfigNivel cfg = Config(n);
        Tile[] t = tiles[cfg.tema];

        // ----- Leer el mapa -----
        string rutaMapa = RutaNiveles + "/Mapas/Nivel" + n + ".txt";
        List<string> filas = File.ReadAllLines(rutaMapa, Encoding.UTF8)
            .Where(l => !(l.StartsWith("# ") || l == "#") && l.Trim().Length > 0)
            .Select(l => l.TrimEnd()).ToList();
        int alto = filas.Count;
        int ancho = filas.Max(f => f.Length);
        char En(int col, int fil) => (fil >= 0 && fil < alto && col >= 0 && col < filas[fil].Length) ? filas[fil][col] : '.';
        float Y(int fil) => alto - 1 - fil; // fila del mapa -> y del mundo (parte de abajo de la celda)

        // ----- Contenedores de la jerarquía -----
        Transform raizEscenario = Nuevo("Escenario").transform;
        Transform raizEnemigos = Nuevo("Enemigos").transform;
        Transform raizObjetos = Nuevo("Objetos").transform;
        Transform raizDecor = Nuevo("Decoracion").transform;
        Transform raizFondo = Nuevo("Fondo").transform;

        // ----- Tilemaps -----
        Grid grid = Nuevo("Grid", raizEscenario).AddComponent<Grid>();
        Tilemap suelo = CrearTilemap("Suelo", grid.transform, 0, true, false);
        Tilemap plataformas = CrearTilemap("Plataformas", grid.transform, 0, true, true);
        Tilemap agua = CrearTilemap("Agua", grid.transform, 15, false, false);

        GameObject jugador = null;
        Transform puntoInicio = null;
        int arenaX = -1, paredX = ancho - 1;
        Vector3 posJefe = Vector3.zero;
        GameObject puntoControl = null;
        var tramosAgua = new List<(int x0, int x1, int fila)>();

        for (int r = 0; r < alto; r++)
        {
            for (int x = 0; x < ancho; x++)
            {
                char ch = En(x, r);
                Vector3Int celda = new Vector3Int(x, (int)Y(r), 0);
                Vector3 pie = new Vector3(x + 0.5f, Y(r), 0f);
                switch (ch)
                {
                    case '#': suelo.SetTile(celda, En(x, r - 1) == '#' ? t[1] : t[0]); break;
                    case '=': plataformas.SetTile(celda, t[2]); break;
                    case '~':
                        bool superficie = En(x, r - 1) != '~';
                        agua.SetTile(celda, superficie ? t[3] : t[4]);
                        if (superficie)
                        {
                            if (tramosAgua.Count > 0 && tramosAgua[^1].fila == r && tramosAgua[^1].x1 == x - 1)
                                tramosAgua[^1] = (tramosAgua[^1].x0, x, r);
                            else tramosAgua.Add((x, x, r));
                        }
                        break;
                    case 'D': Instanciar(cfg.tema == "selva" ? pfBloqueMadera : pfBloqueConcreto, new Vector3(x, Y(r), 0f), raizEscenario); break;
                    case '^': Instanciar(pfAlambre, pie, raizEscenario); break;
                    case 'O': Instanciar((cfg.contenedoresOxido || x % 2 == 0) ? pfContenedorOxido : pfContenedorVerde, new Vector3(x, Y(r), 0f), raizEscenario); break;
                    case 'P':
                        puntoInicio = Nuevo("PuntoInicio", raizEscenario).transform;
                        puntoInicio.position = pie + Vector3.up * 0.1f;
                        jugador = Instanciar(pfJugador, pie + Vector3.up * 0.1f, null);
                        break;
                    case 'C': Instanciar(pfCheckpoint, pie, raizEscenario); break;
                    case 's': Instanciar(pfSoldado, pie, raizEnemigos); break;
                    case 'e': Instanciar(pfSoldadoEscudo, pie, raizEnemigos); break;
                    case 'l': Instanciar(pfSoldadoElite, pie, raizEnemigos); break;
                    case 't': Instanciar(pfTorreta, pie, raizEnemigos); break;
                    case 'a': Instanciar(pfItemAmetralladora, pie, raizObjetos); break;
                    case 'h': Instanciar(pfItemEscopeta, pie, raizObjetos); break;
                    case 'f': Instanciar(pfItemLanzallamas, pie, raizObjetos); break;
                    case '+': Instanciar(pfItemBotiquin, pie, raizObjetos); break;
                    case '1': Instanciar(pfItemVida, pie, raizObjetos); break;
                    case 'g': Instanciar(pfItemGranadas, pie, raizObjetos); break;
                    case 'K': Instanciar(pfCofreBotiquin, pie, raizObjetos); break;
                    case 'M': Instanciar(pfCofreAmetralladora, pie, raizObjetos); break;
                    case 'G': Instanciar(pfCofreGranadas, pie, raizObjetos); break;
                    case '[': arenaX = x; break;
                    case ']': paredX = x; break;
                    case 'J': posJefe = pie; break;
                    case 'Z': puntoControl = Instanciar(pfPuntoControl, pie, raizEnemigos); break;
                    case 'p': Decorar(raizDecor, G("Objetos/palmera_grande.png"), pie, -8, 1f); break;
                    case 'q': Decorar(raizDecor, G("Objetos/palmera_chica.png"), pie, -8, 1f); break;
                    case 'b': Decorar(raizDecor, G(cfg.tema == "selva" ? "Objetos/arbusto_fierce.png" : "Objetos/helecho.png"), pie, -3, 0.8f); break;
                    case 'T': Decorar(raizDecor, G("Objetos/carpa.png"), pie, -5, 1.4f); break;
                    case 'L': Decorar(raizDecor, G("Objetos/antorcha.png"), pie, -4, 1.3f); break;
                    case 'x': Decorar(raizDecor, G("Objetos/troncos.png"), pie, -3, 1f); break;
                    case 'S': Decorar(raizDecor, G("Objetos/sacos_arena.png"), pie, -2, 1f); break;
                    case 'Y': Decorar(raizDecor, G("Personajes/maquina_fortaleza.png"), pie, -6, 1.6f, new Color(0.7f, 0.7f, 0.75f)); break;
                }
            }
        }

        // ----- Generar YA la colisión de los tilemaps (si no, la escena se guarda sin piso) -----
        foreach (Tilemap tm in new[] { suelo, plataformas })
        {
            tm.CompressBounds();
            tm.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            tm.GetComponent<CompositeCollider2D>().GenerateGeometry();
        }

        // ----- Zonas de muerte: agua/lava y vacío bajo el nivel -----
        foreach (var tramo in tramosAgua)
        {
            float arriba = Y(tramo.fila) + 0.55f;
            ZonaMuerteCaja("Foso", raizEscenario, new Vector2((tramo.x0 + tramo.x1 + 1) * 0.5f, (arriba - 6f) * 0.5f),
                new Vector2(tramo.x1 - tramo.x0 + 1, arriba + 6f));
        }
        ZonaMuerteCaja("Vacio", raizEscenario, new Vector2(ancho * 0.5f, -2.5f), new Vector2(ancho + 60f, 2f));

        // Pared invisible al inicio del nivel
        GameObject izq = Nuevo("ParedInicio", raizEscenario, Capas.Suelo);
        izq.transform.position = new Vector3(-0.5f, 0f, 0f);
        BoxCollider2D bi = izq.AddComponent<BoxCollider2D>();
        bi.size = new Vector2(1f, alto * 3f);
        bi.offset = new Vector2(0f, alto);

        // ----- Arena del jefe -----
        GameObject paredDerecha = Barrera("ParedArena", raizEscenario, paredX + 0.5f, alto);
        GameObject barreraIzq = Barrera("BarreraArena", raizEscenario, arenaX + 0.5f, alto);
        float pisoArena = AlturaSuelo(filas, arenaX + 2, alto);
        GameObject jefeGo = Instanciar(n == 1 ? pfJefeCampamento : n == 2 ? pfTanque : pfKruger, posJefe, raizEnemigos);
        if (n == 1) jefeGo.transform.localScale = new Vector3(1.5f, 1.5f, 1f); // el cañón mira a la izquierda por diseño

        GameObject arenaGo = Nuevo("ArenaJefe", raizEscenario, Capas.Disparador);
        arenaGo.transform.position = new Vector3(arenaX + 2.5f, pisoArena, 0f);
        BoxCollider2D ba = arenaGo.AddComponent<BoxCollider2D>();
        ba.isTrigger = true;
        ba.size = new Vector2(1f, alto * 2f);
        ba.offset = new Vector2(0f, alto * 0.5f);
        ArenaJefe arena = arenaGo.AddComponent<ArenaJefe>();
        Asignar(arena, "jefe", jefeGo.GetComponent<JefeBase>());
        Asignar(arena, "barreras", new Object[] { barreraIzq });
        Asignar(arena, "minX", (float)arenaX);
        Asignar(arena, "maxX", (float)paredX);

        // ----- Cámara, luz y fondo -----
        GameObject camGo = CrearCamara(cfg.colorCielo, jugador != null ? jugador.transform.position : Vector3.zero);
        CamaraSeguimiento seg = camGo.AddComponent<CamaraSeguimiento>();
        Asignar(seg, "objetivo", jugador != null ? jugador.transform : null);
        Asignar(seg, "limiteIzquierdo", 0f);
        Asignar(seg, "limiteDerecho", (float)(paredX + 1));
        Asignar(seg, "limiteInferior", 0f);
        Asignar(seg, "limiteSuperior", (float)alto);
        CrearLuzGlobal();
        CrearParallax(raizFondo, cfg, ancho);

        // ----- Controlador del nivel -----
        GameObject ctrlGo = Nuevo("ControladorNivel");
        ControladorNivel ctrl = ctrlGo.AddComponent<ControladorNivel>();
        Asignar(ctrl, "numeroNivel", n);
        Asignar(ctrl, "titulo", cfg.titulo);
        Asignar(ctrl, "subtitulo", cfg.subtitulo);
        Asignar(ctrl, "musicaNivel", Cargar<AudioClip>(RutaAudio + "/Musica/musica_nivel" + n + ".wav"));
        Asignar(ctrl, "textoBriefing", cfg.briefing);
        Asignar(ctrl, "textoFinal", cfg.final);
        Asignar(ctrl, "multiplicadorVida", cfg.vida);
        Asignar(ctrl, "multiplicadorAgresividad", cfg.agresividad);
        Asignar(ctrl, "multiplicadorBotiquines", cfg.botiquines);
        Asignar(ctrl, "jugador", jugador != null ? jugador.GetComponent<JugadorMovimiento>() : null);
        Asignar(ctrl, "puntoInicio", puntoInicio);
        Asignar(ctrl, "objetivoFinal", puntoControl != null ? puntoControl.GetComponent<SaludEnemigo>() : null);

        // ----- Interfaz -----
        CrearInterfazNivel();
        CrearEventSystem();

        if (jugador == null) Debug.LogError("El mapa del Nivel" + n + " no tiene 'P' (inicio del jugador).");
        if (arenaX < 0) Debug.LogError("El mapa del Nivel" + n + " no tiene '[' (arena del jefe).");
        EditorSceneManager.SaveScene(escena, RutaEscenas + "/Nivel" + n + ".unity");
    }

    static Tilemap CrearTilemap(string nombre, Transform padre, int orden, bool colision, bool unaVia)
    {
        GameObject go = Nuevo(nombre, padre, colision ? Capas.Suelo : 0);
        Tilemap tm = go.AddComponent<Tilemap>();
        TilemapRenderer tr = go.AddComponent<TilemapRenderer>();
        tr.sortingOrder = orden;
        if (!colision) return tm;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        TilemapCollider2D tc = go.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        CompositeCollider2D cc = go.AddComponent<CompositeCollider2D>();
        cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
        go.AddComponent<GenerarColisionTilemap>(); // respaldo: regenera la colisión al iniciar
        if (unaVia)
        {
            cc.usedByEffector = true;
            PlatformEffector2D ef = go.AddComponent<PlatformEffector2D>();
            ef.useOneWay = true;
            ef.surfaceArc = 160f;
        }
        return tm;
    }

    static GameObject Instanciar(GameObject prefab, Vector3 pos, Transform padre)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        if (padre != null) go.transform.SetParent(padre, true);
        return go;
    }

    static void Decorar(Transform padre, string sprite, Vector3 pos, int orden, float escala, Color? color = null)
    {
        GameObject go = Nuevo(Path.GetFileNameWithoutExtension(sprite), padre);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * escala;
        SpriteRenderer sr = AgregarSprite(go, SpriteUnico(sprite), orden);
        if (color.HasValue) sr.color = color.Value;
    }

    static void ZonaMuerteCaja(string nombre, Transform padre, Vector2 centro, Vector2 tamaño)
    {
        GameObject go = Nuevo(nombre, padre, Capas.Disparador);
        go.transform.position = centro;
        BoxCollider2D b = go.AddComponent<BoxCollider2D>();
        b.isTrigger = true;
        b.size = tamaño;
        go.AddComponent<ZonaMuerte>();
    }

    static GameObject Barrera(string nombre, Transform padre, float x, int alto)
    {
        GameObject go = Nuevo(nombre, padre, Capas.Suelo);
        go.transform.position = new Vector3(x, 0f, 0f);
        SpriteRenderer sr = AgregarSprite(go, SpriteUnico(G("Objetos/barrera_arena.png")), 3);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(1f, alto);
        BoxCollider2D b = go.AddComponent<BoxCollider2D>();
        b.size = new Vector2(1f, alto * 3f);
        b.offset = new Vector2(0f, alto * 1.5f);
        return go;
    }

    static float AlturaSuelo(List<string> filas, int x, int alto)
    {
        for (int r = 0; r < alto; r++)
            if (x < filas[r].Length && (filas[r][x] == '#' || filas[r][x] == '=')) return alto - r;
        return 0f;
    }

    static GameObject CrearCamara(Color fondo, Vector3 posJugador)
    {
        GameObject camGo = Nuevo("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(posJugador.x + 4f, 5.625f, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = fondo;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 50f;
        PixelPerfectCamera ppc = camGo.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = (int)PpuPersonajes;
        ppc.refResolutionX = 960;
        ppc.refResolutionY = 540;
        ppc.gridSnapping = PixelPerfectCamera.GridSnapping.None;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
        camGo.AddComponent<AudioListener>();
        return camGo;
    }

    static void CrearLuzGlobal()
    {
        GameObject luz = Nuevo("Luz Global 2D");
        Light2D l = luz.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Global;
        l.intensity = 1f;
        l.color = Color.white;
        l.targetSortingLayers = SortingLayer.layers.Select(s => s.id).ToArray();
    }

    // Capas de fondo con parallax (sprites en modo Tiled para que se repitan)
    static void CrearParallax(Transform padre, ConfigNivel cfg, int ancho)
    {
        const float mitadAlto = 5.625f;
        for (int i = 0; i < cfg.fondo.Length; i++)
        {
            var capa = cfg.fondo[i];
            Sprite s = SpriteUnico(capa.ruta);
            if (s == null) continue;
            GameObject go = Nuevo("Capa" + i + "_" + Path.GetFileNameWithoutExtension(capa.ruta), padre);
            SpriteRenderer sr = AgregarSprite(go, s, -100 + i * 10);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(ancho * 2f + 80f, s.rect.height / s.pixelsPerUnit);
            sr.color = capa.color;
            float factorY = capa.factor <= 0f ? 0f : 0.15f;
            ParallaxCapa p = go.AddComponent<ParallaxCapa>();
            Asignar(p, "factorX", capa.factor);
            Asignar(p, "factorY", factorY);
            // Con la cámara en su punto más bajo, la base de la capa queda a "alturaPiso" sobre el borde inferior
            Asignar(p, "alturaBase", capa.alturaPiso - mitadAlto * (1f - factorY));
        }
    }
}
