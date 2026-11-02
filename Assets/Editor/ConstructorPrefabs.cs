using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

// Creación de prefabs. Cada prefab tiene su SpriteRenderer y sus frames asignados en el
// Inspector, así se puede cambiar la "skin" arrastrando otros sprites sin tocar código.
public static partial class ConstructorJuego
{
    // Prefabs que usan las escenas
    static GameObject pfExplosionGrande, pfExplosionChica, pfHumo, pfFogonazo, pfChispas, pfEscombros, pfPolvo, pfCasquillo, pfPopup;
    static GameObject pfBalaJugador, pfPerdigon, pfLlama, pfBalaEnemiga, pfBalaPesada, pfOndaChoque, pfGranada, pfMortero;
    static GameObject pfItemAmetralladora, pfItemEscopeta, pfItemLanzallamas, pfItemBotiquin, pfItemVida, pfItemGranadas;
    static GameObject pfCofreBotiquin, pfCofreAmetralladora, pfCofreGranadas;
    static GameObject pfSoldado, pfSoldadoEscudo, pfSoldadoElite, pfTorreta;
    static GameObject pfJefeCampamento, pfTanque, pfKruger, pfPuntoControl;
    static GameObject pfJugador, pfAlambre, pfCheckpoint, pfBloqueMadera, pfBloqueConcreto, pfContenedorVerde, pfContenedorOxido;
    static ArmaDatos armaPistola, armaAmetralladora, armaEscopeta, armaLanzallamas;
    static PhysicsMaterial2D matSinFriccion, matRebote;
    static Material matParticulas;

    static string G(string ruta) => RutaGenerado + "/" + ruta;

    static void CrearPrefabs()
    {
        CrearMaterialesFisica();
        CrearEfectos();
        CrearProyectiles();
        CrearArmas();
        CrearItems();
        CrearEscenario();
        CrearEnemigos();
        CrearJefes();
        CrearJugador();
        CrearSistemas();
    }

    static void CrearMaterialesFisica()
    {
        matSinFriccion = CrearOActualizar(RutaDatos + "/Materiales/SinFriccion.physicsMaterial2D", () => new PhysicsMaterial2D());
        matSinFriccion.friction = 0f;
        matSinFriccion.bounciness = 0f;
        matRebote = CrearOActualizar(RutaDatos + "/Materiales/Rebote.physicsMaterial2D", () => new PhysicsMaterial2D());
        matRebote.friction = 0.5f;
        matRebote.bounciness = 0.35f;
        EditorUtility.SetDirty(matSinFriccion);
        EditorUtility.SetDirty(matRebote);

        matParticulas = CrearOActualizar(RutaDatos + "/Materiales/Particulas.mat",
            () => new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")));
        matParticulas.mainTexture = Cargar<Texture2D>(G("Efectos/particula.png"));
        EditorUtility.SetDirty(matParticulas);
    }

    static T CrearOActualizar<T>(string ruta, System.Func<T> crear) where T : Object
    {
        T a = AssetDatabase.LoadAssetAtPath<T>(ruta);
        if (a != null) return a;
        a = crear();
        AssetDatabase.CreateAsset(a, ruta);
        return a;
    }

    // =====================================================================
    // Efectos
    // =====================================================================
    static void CrearEfectos()
    {
        string r = RutaPrefabs + "/Efectos/";
        pfExplosionGrande = CrearEfecto("ExplosionGrande", Frames(G("Efectos/explosion_grande.png")), 22f, 1.6f, Color.white, r);
        pfExplosionChica = CrearEfecto("ExplosionChica", Frames(G("Efectos/explosion_chica.png")), 24f, 1f, Color.white, r);
        pfHumo = CrearEfecto("Humo", Frames(G("Efectos/humo.png")), 12f, 1.2f, new Color(0.55f, 0.55f, 0.5f, 0.8f), r);
        pfFogonazo = CrearEfecto("Fogonazo", Frames(G("Efectos/fogonazo.png")), 30f, 1.4f, Color.white, r, false);

        pfChispas = Particulas("Chispas", r, 10, new Vector2(2f, 6f), new Vector2(0.12f, 0.3f), new Vector2(0.05f, 0.09f),
            new Color(1f, 0.95f, 0.5f), new Color(1f, 0.55f, 0.1f), 0.6f, 0.6f);
        pfEscombros = Particulas("Escombros", r, 16, new Vector2(3f, 8f), new Vector2(0.5f, 0.9f), new Vector2(0.08f, 0.16f),
            new Color(0.45f, 0.38f, 0.28f), new Color(0.3f, 0.3f, 0.3f), 2.5f, 1.3f);
        pfPolvo = Particulas("Polvo", r, 6, new Vector2(0.4f, 1.4f), new Vector2(0.3f, 0.5f), new Vector2(0.12f, 0.24f),
            new Color(0.85f, 0.8f, 0.65f, 0.6f), new Color(0.7f, 0.65f, 0.5f, 0.5f), -0.1f, 0.8f);
        pfCasquillo = Particulas("Casquillo", r, 1, new Vector2(2f, 3.5f), new Vector2(0.5f, 0.7f), new Vector2(0.06f, 0.07f),
            new Color(0.9f, 0.72f, 0.25f), new Color(0.75f, 0.55f, 0.2f), 2.2f, 0.9f);

        // Texto flotante
        GameObject p = Nuevo("PopupTexto");
        TextMeshPro t = p.AddComponent<TextMeshPro>();
        t.font = fuenteTitulos;
        t.fontSharedMaterial = materialContorno;
        t.fontSize = 4.5f;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.text = "+100";
        t.sortingOrder = 40;
        t.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
        PopupTexto popup = p.AddComponent<PopupTexto>();
        Asignar(popup, "texto", t);
        pfPopup = GuardarPrefab(p, r + "PopupTexto.prefab");
    }

    static GameObject CrearEfecto(string nombre, Sprite[] frames, float fps, float escala, Color color, string carpeta, bool rotar = true)
    {
        GameObject go = Nuevo(nombre);
        go.transform.localScale = Vector3.one * escala;
        SpriteRenderer sr = AgregarSprite(go, frames[0], 30);
        sr.color = color;
        EfectoAnimado e = go.AddComponent<EfectoAnimado>();
        Asignar(e, "render", sr);
        Asignar(e, "frames", frames);
        Asignar(e, "fps", fps);
        Asignar(e, "rotacionAleatoria", rotar && nombre != "Humo");
        return GuardarPrefab(go, carpeta + nombre + ".prefab");
    }

    static GameObject Particulas(string nombre, string carpeta, int cantidad, Vector2 velocidad, Vector2 vida, Vector2 tamaño,
        Color c1, Color c2, float gravedad, float duracionObjeto)
    {
        GameObject go = Nuevo(nombre);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vida.x, vida.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velocidad.x, velocidad.y);
        main.startSize = new ParticleSystem.MinMaxCurve(tamaño.x, tamaño.y);
        main.startColor = new ParticleSystem.MinMaxGradient(c1, c2);
        main.gravityModifier = gravedad;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cantidad) });

        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = nombre == "Polvo" ? 80f : 50f;
        sh.radius = 0.05f;
        sh.rotation = new Vector3(-90f, 0f, 0f); // apunta hacia arriba

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = matParticulas;
        pr.sortingOrder = 31;

        AutoDevolver ad = go.AddComponent<AutoDevolver>();
        Asignar(ad, "segundos", duracionObjeto);
        return GuardarPrefab(go, carpeta + nombre + ".prefab");
    }

    // =====================================================================
    // Proyectiles
    // =====================================================================
    static void CrearProyectiles()
    {
        string r = RutaPrefabs + "/Proyectiles/";
        pfBalaJugador = CrearBala("BalaJugador", SpriteUnico(G("Proyectiles/bala_jugador.png")), Capas.BalaJugador, 1.3f, 0.09f, Color.white, r);
        pfPerdigon = CrearBala("Perdigon", SpriteUnico(G("Proyectiles/bala_perdigon.png")), Capas.BalaJugador, 1f, 0.08f, Color.white, r);
        pfBalaEnemiga = CrearBala("BalaEnemiga", SpriteUnico(G("Proyectiles/bala_enemiga.png")), Capas.BalaEnemigo, 1.4f, 0.08f, Color.white, r);
        pfBalaPesada = CrearBala("BalaPesada", SpriteUnico(G("Proyectiles/bala_enemiga.png")), Capas.BalaEnemigo, 2.4f, 0.08f, new Color(1f, 0.6f, 0.3f), r);

        // Llama del lanzallamas: atraviesa, crece y se desvanece
        Sprite[] llama = Frames(G("Proyectiles/llama.png"));
        pfLlama = CrearBala("Llama", llama[0], Capas.BalaJugador, 1.2f, 0.22f, Color.white, r, true, 1.6f, llama);
        // Onda de choque de Kruger
        pfOndaChoque = CrearBala("OndaChoque", llama[1], Capas.BalaEnemigo, 2.2f, 0.14f, new Color(1f, 0.5f, 0.2f), r, false, 0f, llama);

        // Granada del jugador
        GameObject g = Nuevo("Granada", null, Capas.BalaJugador);
        AgregarSprite(g, SpriteUnico(G("Proyectiles/granada.png")), 25);
        g.transform.localScale = Vector3.one * 1.3f;
        Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.2f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D cc = g.AddComponent<CircleCollider2D>();
        cc.radius = 0.12f;
        cc.sharedMaterial = matRebote;
        ProyectilExplosivo pe = g.AddComponent<ProyectilExplosivo>();
        Asignar(pe, "tiempoMaximo", 1.4f);
        Asignar(pe, "radio", 2.3f);
        Asignar(pe, "daño", 8);
        Asignar(pe, "explosionGrande", true);
        Asignar(pe, "capasQueDaña", (LayerMask)(Capas.MascaraEnemigo | Capas.MascaraSuelo));
        Asignar(pe, "capasQueDetonan", (LayerMask)Capas.MascaraEnemigo);
        pfGranada = GuardarPrefab(g, r + "Granada.prefab");

        // Mortero de los jefes
        GameObject m = Nuevo("Mortero", null, Capas.BalaEnemigo);
        SpriteRenderer msr = AgregarSprite(m, SpriteUnico(G("Proyectiles/obus.png")), 25);
        msr.color = new Color(1f, 0.75f, 0.6f);
        m.transform.localScale = Vector3.one * 1.3f;
        Rigidbody2D mrb = m.AddComponent<Rigidbody2D>();
        mrb.gravityScale = 1.5f;
        mrb.interpolation = RigidbodyInterpolation2D.Interpolate;
        CircleCollider2D mc = m.AddComponent<CircleCollider2D>();
        mc.radius = 0.2f;
        ProyectilExplosivo mpe = m.AddComponent<ProyectilExplosivo>();
        Asignar(mpe, "tiempoMaximo", 4f);
        Asignar(mpe, "radio", 1.6f);
        Asignar(mpe, "daño", 25);
        Asignar(mpe, "explosionGrande", false);
        Asignar(mpe, "capasQueDaña", (LayerMask)Capas.MascaraJugador);
        Asignar(mpe, "capasQueDetonan", (LayerMask)Capas.MascaraJugador);
        Asignar(mpe, "explotarAlTocarSuelo", true);
        Asignar(mpe, "giro", 0f);
        pfMortero = GuardarPrefab(m, r + "Mortero.prefab");
    }

    static GameObject CrearBala(string nombre, Sprite sprite, int capa, float escala, float radio, Color color, string carpeta,
        bool atraviesa = false, float crecimiento = 0f, Sprite[] animacion = null)
    {
        GameObject go = Nuevo(nombre, null, capa);
        go.transform.localScale = Vector3.one * escala;
        SpriteRenderer sr = AgregarSprite(go, sprite, 25);
        sr.color = color;
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = radio;
        Bala b = go.AddComponent<Bala>();
        Asignar(b, "atraviesa", atraviesa);
        Asignar(b, "chispasAlImpactar", !atraviesa);
        Asignar(b, "crecimiento", crecimiento);
        Asignar(b, "render", crecimiento > 0f ? sr : null);
        if (animacion != null)
        {
            AnimadorSprites a = go.AddComponent<AnimadorSprites>();
            AsignarClips(a, sr, "Idle", ("Idle", animacion, 12f, crecimiento <= 0f));
            sr.color = color;
        }
        return GuardarPrefab(go, carpeta + nombre + ".prefab");
    }

    // =====================================================================
    // Armas (ScriptableObjects editables)
    // =====================================================================
    static void CrearArmas()
    {
        string r = RutaDatos + "/Armas/";
        armaPistola = Arma(r + "Pistola.asset", "PISTOLA", G("Objetos/icono_pistola.png"), "disparo_pistola.wav",
            pfBalaJugador, 0.2f, 1, 18f, 1.0f, 1, 0f, 0);
        armaAmetralladora = Arma(r + "Ametralladora.asset", "AMETRALLADORA", G("Objetos/icono_ametralladora.png"), "disparo_ametralladora.wav",
            pfBalaJugador, 0.08f, 1, 20f, 1.0f, 1, 3f, 150);
        armaEscopeta = Arma(r + "Escopeta.asset", "ESCOPETA", G("Objetos/icono_escopeta.png"), "disparo_escopeta.wav",
            pfPerdigon, 0.6f, 1, 16f, 0.24f, 6, 14f, 30);
        armaLanzallamas = Arma(r + "Lanzallamas.asset", "LANZALLAMAS", G("Objetos/icono_lanzallamas.png"), "llama_SINTETIZADO.wav",
            pfLlama, 0.05f, 1, 9f, 0.35f, 1, 6f, 200);
        armaLanzallamas.expulsaCasquillo = false;
        armaLanzallamas.muestraFogonazo = false;
        armaLanzallamas.intervaloSonido = 0.12f;
        armaLanzallamas.volumenSonido = 0.5f;
        armaEscopeta.volumenSonido = 0.9f;
        armaAmetralladora.volumenSonido = 0.55f;
        foreach (ArmaDatos a in new[] { armaPistola, armaAmetralladora, armaEscopeta, armaLanzallamas }) EditorUtility.SetDirty(a);
    }

    static ArmaDatos Arma(string ruta, string nombre, string icono, string sonido, GameObject bala, float cadencia, int daño,
        float velocidad, float vida, int proyectiles, float dispersion, int municion)
    {
        ArmaDatos a = CrearOActualizar(ruta, () => ScriptableObject.CreateInstance<ArmaDatos>());
        a.nombre = nombre;
        a.icono = SpriteUnico(icono);
        a.sonidoDisparo = Cargar<AudioClip>(RutaAudio + "/SFX/" + sonido);
        a.prefabBala = bala;
        a.cadencia = cadencia;
        a.daño = daño;
        a.velocidadBala = velocidad;
        a.vidaBala = vida;
        a.proyectilesPorDisparo = proyectiles;
        a.dispersionGrados = dispersion;
        a.municion = municion;
        a.expulsaCasquillo = true;
        a.muestraFogonazo = true;
        a.intervaloSonido = 0f;
        a.volumenSonido = 0.7f;
        return a;
    }

    // =====================================================================
    // Ítems y cofres
    // =====================================================================
    static void CrearItems()
    {
        string r = RutaPrefabs + "/Objetos/";
        pfItemAmetralladora = Item("ItemAmetralladora", G("Objetos/icono_ametralladora.png"), TipoItem.Arma, armaAmetralladora, 0, "+AMETRALLADORA", new Color(1f, 0.75f, 0.2f), r);
        pfItemEscopeta = Item("ItemEscopeta", G("Objetos/icono_escopeta.png"), TipoItem.Arma, armaEscopeta, 0, "+ESCOPETA", new Color(1f, 0.75f, 0.2f), r);
        pfItemLanzallamas = Item("ItemLanzallamas", G("Objetos/icono_lanzallamas.png"), TipoItem.Arma, armaLanzallamas, 0, "+LANZALLAMAS", new Color(1f, 0.5f, 0.2f), r);
        pfItemBotiquin = Item("ItemBotiquin", G("Objetos/botiquin.png"), TipoItem.Botiquin, null, 40, "+BOTIQUÍN", new Color(0.6f, 1f, 0.5f), r);
        pfItemVida = Item("ItemVidaExtra", G("Objetos/vida_extra.png"), TipoItem.VidaExtra, null, 1, "+1 VIDA", new Color(0.5f, 0.9f, 1f), r);
        pfItemGranadas = Item("ItemGranadas", G("Proyectiles/granada.png"), TipoItem.Granadas, null, 3, "+3 GRANADAS", new Color(0.8f, 1f, 0.5f), r);

        pfCofreBotiquin = CofrePrefab("CofreBotiquin", pfItemBotiquin, false, r);
        pfCofreAmetralladora = CofrePrefab("CofreAmetralladora", pfItemAmetralladora, true, r);
        pfCofreGranadas = CofrePrefab("CofreGranadas", pfItemGranadas, false, r);
    }

    static GameObject Item(string nombre, string sprite, TipoItem tipo, ArmaDatos arma, int cantidad, string popup, Color color, string carpeta)
    {
        GameObject go = Nuevo(nombre, null, Capas.Item);
        CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.55f;
        cc.offset = new Vector2(0f, 0.5f);
        GameObject visual = Nuevo("Visual", go.transform, Capas.Item);
        visual.transform.localScale = Vector3.one * 1.5f;
        AgregarSprite(visual, SpriteUnico(sprite), 8);
        ItemRecogible it = go.AddComponent<ItemRecogible>();
        Asignar(it, "tipo", tipo);
        Asignar(it, "arma", arma);
        Asignar(it, "cantidad", cantidad);
        Asignar(it, "textoPopup", popup);
        Asignar(it, "colorPopup", color);
        Asignar(it, "visual", visual.transform);
        return GuardarPrefab(go, carpeta + nombre + ".prefab");
    }

    static GameObject CofrePrefab(string nombre, GameObject contenido, bool metal, string carpeta)
    {
        GameObject go = Nuevo(nombre, null, Capas.Enemigo);
        go.transform.localScale = Vector3.one * 1.5f;
        SpriteRenderer sr = AgregarSprite(go, SpriteUnico(G(metal ? "Objetos/cofre_metal_cerrado.png" : "Objetos/cofre_cerrado.png")), 6);
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(0.6f, 0.55f);
        bc.offset = new Vector2(0f, 0.3f);
        Cofre c = go.AddComponent<Cofre>();
        Asignar(c, "contenido", contenido);
        Asignar(c, "render", sr);
        Asignar(c, "spriteAbierto", SpriteUnico(G(metal ? "Objetos/cofre_metal_abierto.png" : "Objetos/cofre_abierto.png")));
        return GuardarPrefab(go, carpeta + nombre + ".prefab");
    }

    // =====================================================================
    // Escenario: alambre, checkpoint, bloques, contenedores
    // =====================================================================
    static void CrearEscenario()
    {
        string r = RutaPrefabs + "/Escenario/";

        GameObject a = Nuevo("AlambrePuas", null, Capas.Disparador);
        AgregarSprite(a, SpriteUnico(G("Objetos/alambre_puas.png")), 4);
        BoxCollider2D ac = a.AddComponent<BoxCollider2D>();
        ac.isTrigger = true;
        ac.size = new Vector2(0.9f, 0.55f);
        ac.offset = new Vector2(0f, 0.3f);
        a.AddComponent<AlambrePuas>();
        pfAlambre = GuardarPrefab(a, r + "AlambrePuas.prefab");

        Sprite[] bandera = Frames(G("Objetos/bandera.png"));
        GameObject c = Nuevo("Checkpoint", null, Capas.Disparador);
        c.transform.localScale = Vector3.one * 1.3f;
        SpriteRenderer csr = AgregarSprite(c, bandera[0], -2);
        BoxCollider2D cc = c.AddComponent<BoxCollider2D>();
        cc.isTrigger = true;
        cc.size = new Vector2(1f, 6f);
        cc.offset = new Vector2(0f, 2.5f);
        Checkpoint cp = c.AddComponent<Checkpoint>();
        Asignar(cp, "render", csr);
        Asignar(cp, "spriteInactiva", bandera[0]);
        Asignar(cp, "spriteActiva", bandera[1]);
        pfCheckpoint = GuardarPrefab(c, r + "Checkpoint.prefab");

        pfBloqueMadera = Bloque("BloqueMadera", G("Objetos/caja_madera.png"), 4, r);
        pfBloqueConcreto = Bloque("BloqueConcreto", G("Objetos/bloque_grieta.png"), 6, r);
        pfContenedorVerde = Contenedor("ContenedorVerde", G("Objetos/contenedor_verde.png"), r);
        pfContenedorOxido = Contenedor("ContenedorOxido", G("Objetos/contenedor_oxido.png"), r);
    }

    static GameObject Bloque(string nombre, string sprite, int vida, string carpeta)
    {
        GameObject go = Nuevo(nombre, null, Capas.Suelo);
        SpriteRenderer sr = AgregarSprite(go, SpriteUnico(sprite), 1);
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one;
        bc.offset = new Vector2(0.5f, 0.5f);
        PlataformaDestructible p = go.AddComponent<PlataformaDestructible>();
        Asignar(p, "vida", vida);
        Asignar(p, "render", sr);
        return GuardarPrefab(go, carpeta + nombre + ".prefab");
    }

    static GameObject Contenedor(string nombre, string sprite, string carpeta)
    {
        GameObject go = Nuevo(nombre, null, Capas.Suelo);
        AgregarSprite(go, SpriteUnico(sprite), -1);
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(3f, 2f);
        bc.offset = new Vector2(1.5f, 1f);
        return GuardarPrefab(go, carpeta + nombre + ".prefab");
    }

    // =====================================================================
    // Enemigos
    // =====================================================================
    static void CrearEnemigos()
    {
        string r = RutaPrefabs + "/Enemigos/";
        var dropsRaso = new[] { (pfItemAmetralladora, 0.08f), (pfItemBotiquin, 0.08f) };
        var dropsEscudo = new[] { (pfItemGranadas, 0.45f), (pfItemBotiquin, 0.08f) };
        var dropsElite = new[] { (pfItemEscopeta, 0.25f), (pfItemBotiquin, 0.12f) };

        pfSoldado = Soldado("SoldadoRaso", G("Personajes/soldado_raso.png"), 1, 100, 0f, false, false, 1, 1.6f, 2.4f, 0.7f, dropsRaso, r);
        pfSoldadoEscudo = Soldado("SoldadoEscudo", G("Personajes/soldado_escudo.png"), 5, 200, 1f, false, true, 1, 2.3f, 1.4f, 0.9f, dropsEscudo, r);
        pfSoldadoElite = Soldado("SoldadoElite", G("Personajes/soldado_elite.png"), 3, 250, 0f, true, false, 3, 1.7f, 3.2f, 0.7f, dropsElite, r);

        // Torreta fija
        Sprite[] t = Frames(G("Personajes/torreta.png"));
        GameObject go = Nuevo("Torreta", null, Capas.Enemigo);
        go.transform.localScale = new Vector3(-1f, 1f, 1f);
        GameObject spr = Nuevo("Sprite", go.transform, Capas.Enemigo);
        SpriteRenderer sr = AgregarSprite(spr, t[0], 10);
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(1.4f, 1.5f);
        bc.offset = new Vector2(0f, 0.75f);
        SaludEnemigo s = go.AddComponent<SaludEnemigo>();
        Asignar(s, "vidaMaxima", 10);
        Asignar(s, "puntos", 300);
        Asignar(s, "tipoMuerte", TipoMuerte.Explota);
        AnimadorSprites anim = go.AddComponent<AnimadorSprites>();
        AsignarClips(anim, sr, "Idle", ("Idle", Elegir(t, 0), 1f, true), ("Disparo", Elegir(t, 1), 1f, true));
        GameObject boca = Nuevo("Boca", go.transform, Capas.Enemigo);
        boca.transform.localPosition = new Vector3(0.8f, 1.1f, 0f);
        TorretaEnemiga te = go.AddComponent<TorretaEnemiga>();
        Asignar(te, "animador", anim);
        Asignar(te, "bocaDisparo", boca.transform);
        Asignar(te, "prefabBala", pfBalaEnemiga);
        Asignar(te, "dañoBala", 20);
        Asignar(te, "velocidadBala", 8f);
        Asignar(te, "rangoVision", 11f);
        pfTorreta = GuardarPrefab(go, r + "Torreta.prefab");
    }

    static GameObject Soldado(string nombre, string hoja, int vida, int puntos, float proteccion, bool apunta, bool avanza,
        int balasRafaga, float tiempoRafaga, float velocidadPersecucion, float anchoCuerpo, (GameObject, float)[] drops, string carpeta)
    {
        Sprite[] f = Frames(hoja);
        GameObject go = Nuevo(nombre, null, Capas.Enemigo);
        go.transform.localScale = new Vector3(-1f, 1f, 1f); // empiezan mirando a la izquierda (hacia el jugador)
        GameObject spr = Nuevo("Sprite", go.transform, Capas.Enemigo);
        SpriteRenderer sr = AgregarSprite(spr, f[0], 10);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(anchoCuerpo, 1.6f);
        bc.offset = new Vector2(0f, 0.8f);
        bc.sharedMaterial = matSinFriccion;

        SaludEnemigo s = go.AddComponent<SaludEnemigo>();
        Asignar(s, "vidaMaxima", vida);
        Asignar(s, "puntos", puntos);
        Asignar(s, "proteccionFrontal", proteccion);
        Asignar(s, "tipoMuerte", TipoMuerte.Cae);

        AnimadorSprites anim = go.AddComponent<AnimadorSprites>();
        AsignarClips(anim, sr, "Idle",
            ("Idle", Elegir(f, 0), 1f, true),
            ("Disparo", Elegir(f, 1), 1f, true),
            ("Caminar", Elegir(f, 0, 2, 3, 2), 8f, true));

        GameObject boca = Nuevo("Boca", go.transform, Capas.Enemigo);
        boca.transform.localPosition = new Vector3(0.75f, 1.17f, 0f);

        EnemigoSoldado e = go.AddComponent<EnemigoSoldado>();
        Asignar(e, "animador", anim);
        Asignar(e, "bocaDisparo", boca.transform);
        Asignar(e, "prefabBala", pfBalaEnemiga);
        Asignar(e, "dañoBala", 20);
        Asignar(e, "velocidadBala", 8.5f);
        Asignar(e, "apuntaDirecto", apunta);
        Asignar(e, "avanzaMientrasDispara", avanza);
        Asignar(e, "balasPorRafaga", balasRafaga);
        Asignar(e, "tiempoEntreRafagas", tiempoRafaga);
        Asignar(e, "velocidadPersecucion", velocidadPersecucion);

        SoltarObjeto so = go.AddComponent<SoltarObjeto>();
        AsignarDrops(so, drops);
        return GuardarPrefab(go, carpeta + nombre + ".prefab");
    }

    static void AsignarDrops(SoltarObjeto so, (GameObject prefab, float prob)[] drops)
    {
        SerializedObject s = new SerializedObject(so);
        SerializedProperty lista = s.FindProperty("posibles");
        lista.arraySize = drops.Length;
        for (int i = 0; i < drops.Length; i++)
        {
            lista.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = drops[i].prefab;
            lista.GetArrayElementAtIndex(i).FindPropertyRelative("probabilidad").floatValue = drops[i].prob;
        }
        s.ApplyModifiedPropertiesWithoutUndo();
    }

    // =====================================================================
    // Jefes
    // =====================================================================
    static void CrearJefes()
    {
        string r = RutaPrefabs + "/Enemigos/";

        // ----- Nivel 1: Cañón pesado del campamento -----
        GameObject j1 = Nuevo("JefeCampamento", null, Capas.Enemigo);
        j1.transform.localScale = Vector3.one * 1.5f;
        GameObject spr1 = Nuevo("Sprite", j1.transform, Capas.Enemigo);
        SpriteRenderer sr1 = AgregarSprite(spr1, SpriteUnico(G("Personajes/jefe_campamento.png")), 9);
        BoxCollider2D bc1 = j1.AddComponent<BoxCollider2D>();
        bc1.size = new Vector2(2.4f, 2.8f);
        bc1.offset = new Vector2(0f, 1.4f);
        SaludEnemigo s1 = j1.AddComponent<SaludEnemigo>();
        Asignar(s1, "vidaMaxima", 110);
        Asignar(s1, "puntos", 2500);
        Asignar(s1, "esJefe", true);
        Asignar(s1, "aplicarDificultad", false);
        Asignar(s1, "renders", new Object[] { sr1 });
        AnimadorSprites a1 = j1.AddComponent<AnimadorSprites>();
        AsignarClips(a1, sr1, "Idle", ("Idle", new[] { sr1.sprite }, 1f, true));
        Sprite[] cañon = Frames(G("Personajes/cañon_jefe.png"));
        SpriteRenderer alto = CañonJefe(j1.transform, "CañonAlto", new Vector3(-1.45f, 0.9f, 0f), cañon[0]);
        SpriteRenderer bajo = CañonJefe(j1.transform, "CañonBajo", new Vector3(-1.45f, 0.24f, 0f), cañon[0]);
        GameObject mortero = Nuevo("BocaMortero", j1.transform, Capas.Enemigo);
        mortero.transform.localPosition = new Vector3(0f, 2.9f, 0f);
        JefeCampamento jc = j1.AddComponent<JefeCampamento>();
        Asignar(jc, "nombreJefe", "CAÑÓN PESADO \"MAMUT\"");
        Asignar(jc, "animador", a1);
        Asignar(jc, "bocaDisparo", alto.transform);
        Asignar(jc, "prefabBala", pfBalaEnemiga);
        Asignar(jc, "dañoBala", 20);
        Asignar(jc, "velocidadBala", 7.5f);
        Asignar(jc, "cañonAlto", alto.transform);
        Asignar(jc, "cañonBajo", bajo.transform);
        Asignar(jc, "renderCañonAlto", alto);
        Asignar(jc, "renderCañonBajo", bajo);
        Asignar(jc, "spriteCañon", cañon[0]);
        Asignar(jc, "spriteCañonFuego", cañon[1]);
        Asignar(jc, "prefabMortero", pfMortero);
        Asignar(jc, "bocaMortero", mortero.transform);
        pfJefeCampamento = GuardarPrefab(j1, r + "JefeCampamento.prefab");

        // ----- Nivel 2: Tanque (mini-jefe) -----
        Sprite[] tk = Frames(G("Personajes/tanque.png"));
        GameObject j2 = Nuevo("TanqueMiniJefe", null, Capas.Enemigo);
        j2.transform.localScale = new Vector3(-1.8f, 1.8f, 1f);
        GameObject spr2 = Nuevo("Sprite", j2.transform, Capas.Enemigo);
        SpriteRenderer sr2 = AgregarSprite(spr2, tk[0], 9);
        Rigidbody2D rb2 = j2.AddComponent<Rigidbody2D>();
        rb2.gravityScale = 3f;
        rb2.mass = 50f;
        rb2.freezeRotation = true;
        rb2.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D bc2 = j2.AddComponent<BoxCollider2D>();
        bc2.size = new Vector2(2.7f, 1.0f);
        bc2.offset = new Vector2(0f, 0.5f);
        bc2.sharedMaterial = matSinFriccion;
        SaludEnemigo s2 = j2.AddComponent<SaludEnemigo>();
        Asignar(s2, "vidaMaxima", 90);
        Asignar(s2, "puntos", 3000);
        Asignar(s2, "esJefe", true);
        Asignar(s2, "aplicarDificultad", false);
        Asignar(s2, "proteccionFrontal", 0.85f);
        AnimadorSprites a2 = j2.AddComponent<AnimadorSprites>();
        AsignarClips(a2, sr2, "Idle", ("Idle", Elegir(tk, 0), 1f, true), ("Mover", tk, 12f, true));
        GameObject boca2 = Nuevo("Boca", j2.transform, Capas.Enemigo);
        boca2.transform.localPosition = new Vector3(1.45f, 0.92f, 0f);
        VehiculoMiniJefe vm = j2.AddComponent<VehiculoMiniJefe>();
        Asignar(vm, "nombreJefe", "TANQUE \"RINOCERONTE\"");
        Asignar(vm, "animador", a2);
        Asignar(vm, "bocaDisparo", boca2.transform);
        Asignar(vm, "prefabBala", pfBalaPesada);
        Asignar(vm, "dañoBala", 25);
        Asignar(vm, "velocidadBala", 8f);
        Asignar(vm, "cajaContacto", new Vector2(4.6f, 1.8f));
        SoltarObjeto so2 = j2.AddComponent<SoltarObjeto>();
        AsignarDrops(so2, new[] { (pfItemEscopeta, 1f) });
        pfTanque = GuardarPrefab(j2, r + "TanqueMiniJefe.prefab");

        // Punto de control (objetivo final del nivel 2)
        GameObject pc = Nuevo("PuntoControl", null, Capas.Enemigo);
        pc.transform.localScale = Vector3.one * 1.5f;
        SpriteRenderer srpc = AgregarSprite(pc, SpriteUnico(G("Personajes/punto_control.png")), 2);
        BoxCollider2D bcpc = pc.AddComponent<BoxCollider2D>();
        bcpc.size = new Vector2(2.4f, 2.8f);
        bcpc.offset = new Vector2(0f, 1.4f);
        SaludEnemigo spc = pc.AddComponent<SaludEnemigo>();
        Asignar(spc, "vidaMaxima", 40);
        Asignar(spc, "puntos", 5000);
        Asignar(spc, "tipoMuerte", TipoMuerte.Explota);
        Asignar(spc, "aplicarDificultad", false);
        Asignar(spc, "invulnerableAlInicio", true);
        Asignar(spc, "renders", new Object[] { srpc });
        pfPuntoControl = GuardarPrefab(pc, r + "PuntoControl.prefab");

        // ----- Nivel 3: Comandante Kruger -----
        Sprite[] kr = Frames(G("Personajes/kruger.png"));
        GameObject j3 = Nuevo("ComandanteKruger", null, Capas.Enemigo);
        j3.transform.localScale = new Vector3(-2.1f, 2.1f, 1f);
        GameObject spr3 = Nuevo("Sprite", j3.transform, Capas.Enemigo);
        SpriteRenderer sr3 = AgregarSprite(spr3, kr[0], 9);
        Rigidbody2D rb3 = j3.AddComponent<Rigidbody2D>();
        rb3.gravityScale = 3f;
        rb3.mass = 20f;
        rb3.freezeRotation = true;
        rb3.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D bc3 = j3.AddComponent<BoxCollider2D>();
        bc3.size = new Vector2(0.8f, 1.65f);
        bc3.offset = new Vector2(0f, 0.83f);
        bc3.sharedMaterial = matSinFriccion;
        SaludEnemigo s3 = j3.AddComponent<SaludEnemigo>();
        Asignar(s3, "vidaMaxima", 220);
        Asignar(s3, "puntos", 10000);
        Asignar(s3, "esJefe", true);
        Asignar(s3, "aplicarDificultad", false);
        AnimadorSprites a3 = j3.AddComponent<AnimadorSprites>();
        AsignarClips(a3, sr3, "Idle", ("Idle", Elegir(kr, 0), 1f, true), ("Disparo", Elegir(kr, 1), 1f, true),
            ("Caminar", Elegir(kr, 0, 2, 3, 2), 6f, true));
        GameObject boca3 = Nuevo("Boca", j3.transform, Capas.Enemigo);
        boca3.transform.localPosition = new Vector3(0.72f, 1.05f, 0f);
        JefeKruger jk = j3.AddComponent<JefeKruger>();
        Asignar(jk, "nombreJefe", "COMANDANTE KRUGER");
        Asignar(jk, "animador", a3);
        Asignar(jk, "bocaDisparo", boca3.transform);
        Asignar(jk, "prefabBala", pfBalaPesada);
        Asignar(jk, "dañoBala", 20);
        Asignar(jk, "velocidadBala", 8.5f);
        Asignar(jk, "prefabOndaChoque", pfOndaChoque);
        Asignar(jk, "prefabMortero", pfMortero);
        pfKruger = GuardarPrefab(j3, r + "ComandanteKruger.prefab");
    }

    static SpriteRenderer CañonJefe(Transform padre, string nombre, Vector3 pos, Sprite sprite)
    {
        GameObject c = Nuevo(nombre, padre, Capas.Enemigo);
        c.transform.localPosition = pos;
        c.transform.localScale = new Vector3(-1f, 1f, 1f); // el sprite apunta a la derecha; el jefe dispara a la izquierda
        return AgregarSprite(c, sprite, 10);
    }

    // =====================================================================
    // Jugador
    // =====================================================================
    static void CrearJugador()
    {
        Sprite[] f = Frames(G("Personajes/comando_alfa.png"));
        GameObject go = Nuevo("ComandoAlfa", null, Capas.Jugador);
        go.tag = "Player";

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 3.2f;
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.6f, 1.65f);
        bc.offset = new Vector2(0f, 0.825f);
        bc.sharedMaterial = matSinFriccion;

        // Visual (gira al rodar alrededor del centro) > Sprite
        GameObject visual = Nuevo("Visual", go.transform, Capas.Jugador);
        visual.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        GameObject spr = Nuevo("Sprite", visual.transform, Capas.Jugador);
        spr.transform.localPosition = new Vector3(0f, -0.65f, 0f);
        SpriteRenderer sr = AgregarSprite(spr, f[0], 20);

        AnimadorSprites anim = go.AddComponent<AnimadorSprites>();
        AsignarClips(anim, sr, "Idle",
            ("Idle", Elegir(f, 0), 1f, true),
            ("IdleDisparo", Elegir(f, 1), 1f, true),
            ("Correr", Elegir(f, 2, 3, 4, 5), 10f, true),
            ("CorrerDisparo", Elegir(f, 6, 7, 8, 9), 10f, true),
            ("Salto", Elegir(f, 10), 1f, true),
            ("SaltoDisparo", Elegir(f, 11), 1f, true),
            ("Agachado", Elegir(f, 12), 1f, true),
            ("AgachadoDisparo", Elegir(f, 13), 1f, true),
            ("Arriba", Elegir(f, 14), 1f, true),
            ("Muerte", Elegir(f, 15), 1f, true));

        JugadorMovimiento mov = go.AddComponent<JugadorMovimiento>();
        Asignar(mov, "visual", visual.transform);
        Asignar(mov, "capasSuelo", (LayerMask)Capas.MascaraSuelo);
        JugadorDisparo disp = go.AddComponent<JugadorDisparo>();
        Asignar(disp, "armaBasica", armaPistola);
        Asignar(disp, "prefabGranada", pfGranada);
        VidaJugador vida = go.AddComponent<VidaJugador>();
        Asignar(vida, "render", sr);
        AnimacionJugador an = go.AddComponent<AnimacionJugador>();
        Asignar(an, "animador", anim);
        pfJugador = GuardarPrefab(go, RutaPrefabs + "/Jugador/ComandoAlfa.prefab");
    }

    // =====================================================================
    // Comandos jugables: el primero debe ser Alfa (la skin que trae el prefab del jugador)
    // =====================================================================
    static readonly (string hoja, string nombre, string descripcion, string retrato, Vector2 frente, Vector2 agachado, Vector2 aire, Vector2 arriba)[] Escuadron =
    {
        ("comando_alfa", "COMANDO ALFA", "El veterano del escuadrón. Disciplinado y siempre al frente.", "icono_vida",
            new Vector2(0.72f, 1.07f), new Vector2(0.72f, 0.72f), new Vector2(0.65f, 1f), new Vector2(-0.35f, 2.45f)),
        // Puntas del arma: las imprime Herramientas/preparar_escuadron.py
        ("comando_bravo", "COMANDO BRAVO", "Blindaje de asalto y cañón pesado. Abre paso donde nadie más puede.", "retrato_bravo",
            new Vector2(0.62f, 1.12f), new Vector2(0.62f, 0.88f), new Vector2(0.58f, 1.12f), new Vector2(-0.19f, 2.04f)),
    };

    static PersonajeDatos[] CrearPersonajes()
    {
        PersonajeDatos[] personajes = new PersonajeDatos[Escuadron.Length];
        for (int i = 0; i < Escuadron.Length; i++)
        {
            string ruta = RutaDatos + "/Personajes/" + Escuadron[i].hoja + ".asset";
            PersonajeDatos p = CrearOActualizar(ruta, () => ScriptableObject.CreateInstance<PersonajeDatos>());
            p.nombre = Escuadron[i].nombre;
            p.descripcion = Escuadron[i].descripcion;
            p.frames = Frames(G("Personajes/" + Escuadron[i].hoja + ".png"));
            p.retrato = SpriteUnico(G("UI/" + Escuadron[i].retrato + ".png"));
            p.bocaFrente = Escuadron[i].frente;
            p.bocaAgachado = Escuadron[i].agachado;
            p.bocaAire = Escuadron[i].aire;
            p.bocaArriba = Escuadron[i].arriba;
            EditorUtility.SetDirty(p);
            personajes[i] = p;
        }
        return personajes;
    }

    // =====================================================================
    // Sistemas globales (Resources/SistemasJuego): GameManager, AudioManager, Efectos, Transición
    // =====================================================================
    static void CrearSistemas()
    {
        GameObject go = Nuevo("SistemasJuego");
        GameManager gm = go.AddComponent<GameManager>();
        Asignar(gm, "personajes", CrearPersonajes());

        AudioManager am = go.AddComponent<AudioManager>();
        string m = RutaAudio + "/Musica/";
        am.musicaMenu = Cargar<AudioClip>(m + "musica_menu.wav");
        am.musicaNiveles = new[] { Cargar<AudioClip>(m + "musica_nivel1.wav"), Cargar<AudioClip>(m + "musica_nivel2.wav"), Cargar<AudioClip>(m + "musica_nivel3.wav") };
        am.musicaJefe = Cargar<AudioClip>(m + "musica_jefe.ogg");
        am.musicaFinal = Cargar<AudioClip>(m + "musica_final.wav");
        am.jingleVictoria = Cargar<AudioClip>(m + "jingle_victoria.ogg");
        am.jingleGameOver = Cargar<AudioClip>(m + "jingle_gameover.ogg");
        am.jingleInicioNivel = Cargar<AudioClip>(m + "jingle_inicio_nivel.ogg");
        AsignarSonidos(am);

        Efectos ef = go.AddComponent<Efectos>();
        Asignar(ef, "explosionGrande", pfExplosionGrande);
        Asignar(ef, "explosionChica", pfExplosionChica);
        Asignar(ef, "humo", pfHumo);
        Asignar(ef, "fogonazo", pfFogonazo);
        Asignar(ef, "chispas", pfChispas);
        Asignar(ef, "escombros", pfEscombros);
        Asignar(ef, "polvo", pfPolvo);
        Asignar(ef, "casquillo", pfCasquillo);
        Asignar(ef, "popupTexto", pfPopup);

        // Canvas de transición (fundido a negro)
        GameObject canvasGo = Nuevo("Transicion", go.transform);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasGroup grupo = canvasGo.AddComponent<CanvasGroup>();
        GameObject negro = Nuevo("Negro", canvasGo.transform);
        RectTransform rt = negro.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        negro.AddComponent<UnityEngine.UI.Image>().color = Color.black;
        Transicion tr = canvasGo.AddComponent<Transicion>();
        Asignar(tr, "grupo", grupo);

        GuardarPrefab(go, RutaResources + "/SistemasJuego.prefab");
    }

    static void AsignarSonidos(AudioManager am)
    {
        string s = RutaAudio + "/SFX/";
        var tabla = new List<(Sfx, string[], float)>
        {
            (Sfx.DisparoEnemigo, new[] { "disparo_enemigo" }, 0.7f),
            (Sfx.CañonJefe, new[] { "cañon_jefe" }, 0.9f),
            (Sfx.Misil, new[] { "misil" }, 0.7f),
            (Sfx.SinMunicion, new[] { "sin_municion" }, 0.8f),
            (Sfx.RecogerArma, new[] { "recoger_arma" }, 1f),
            (Sfx.Explosion, new[] { "explosion" }, 0.9f),
            (Sfx.ExplosionGrande, new[] { "explosion_grande" }, 1f),
            (Sfx.ExplosionChica, new[] { "explosion_chica" }, 0.8f),
            (Sfx.LanzarGranada, new[] { "rodar_SINTETIZADO" }, 0.6f),
            (Sfx.Paso, new[] { "paso_1", "paso_2" }, 0.5f),
            (Sfx.Salto, new[] { "salto" }, 0.7f),
            (Sfx.Aterrizaje, new[] { "aterrizaje" }, 0.6f),
            (Sfx.Rodar, new[] { "rodar_SINTETIZADO" }, 0.8f),
            (Sfx.RecogerObjeto, new[] { "recoger_objeto" }, 0.9f),
            (Sfx.DañoJugador, new[] { "daño_jugador" }, 1f),
            (Sfx.DañoEnemigo, new[] { "daño_enemigo" }, 0.6f),
            (Sfx.EnemigoDerrotado, new[] { "enemigo_derrotado" }, 0.8f),
            (Sfx.ImpactoBala, new[] { "impacto_bala" }, 0.6f),
            (Sfx.GolpeEscudo, new[] { "golpe_escudo" }, 0.7f),
            (Sfx.RomperCofre, new[] { "romper_cofre" }, 0.9f),
            (Sfx.Checkpoint, new[] { "checkpoint" }, 1f),
            (Sfx.VidaExtra, new[] { "vida_extra" }, 1f),
            (Sfx.CaerFoso, new[] { "caer_foso" }, 0.9f),
            (Sfx.AlarmaJefe, new[] { "alarma_jefe" }, 0.7f),
            (Sfx.CambioFase, new[] { "cambio_fase" }, 1f),
            (Sfx.UiHover, new[] { "ui_hover" }, 0.6f),
            (Sfx.UiClick, new[] { "ui_click" }, 0.9f),
            (Sfx.Pausa, new[] { "pausa" }, 0.9f),
            (Sfx.Reanudar, new[] { "reanudar" }, 0.9f),
        };

        SerializedObject so = new SerializedObject(am);
        SerializedProperty lista = so.FindProperty("sonidos");
        lista.arraySize = tabla.Count;
        for (int i = 0; i < tabla.Count; i++)
        {
            SerializedProperty e = lista.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("id").intValue = (int)tabla[i].Item1;
            e.FindPropertyRelative("volumen").floatValue = tabla[i].Item3;
            bool esUI = tabla[i].Item1 == Sfx.UiHover || tabla[i].Item1 == Sfx.UiClick;
            e.FindPropertyRelative("variacionTono").floatValue = esUI ? 0.02f : 0.08f;
            SerializedProperty clips = e.FindPropertyRelative("clips");
            clips.arraySize = tabla[i].Item2.Length;
            for (int k = 0; k < tabla[i].Item2.Length; k++)
                clips.GetArrayElementAtIndex(k).objectReferenceValue = Cargar<AudioClip>(s + tabla[i].Item2[k] + ".wav");
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
