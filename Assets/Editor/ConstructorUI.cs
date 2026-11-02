using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Construcción de toda la interfaz: HUD, pausa, Game Over/Victoria, splash y menú principal.
public static partial class ConstructorJuego
{
    // Paleta militar
    static readonly Color Crema = new Color(0.93f, 0.9f, 0.78f);
    static readonly Color Naranja = new Color(1f, 0.68f, 0.18f);
    static readonly Color Rojo = new Color(0.92f, 0.28f, 0.2f);
    static readonly Color Oliva = new Color(0.45f, 0.55f, 0.28f);
    static readonly Color OscuroAlfa = new Color(0f, 0f, 0f, 170f / 255f);

    static Sprite spPanel, spBoton, spMarco, spBlanco, spPerilla;

    static void CargarSpritesUI()
    {
        spPanel = SpriteUnico(G("UI/panel.png"));
        spBoton = SpriteUnico(G("UI/boton.png"));
        spMarco = SpriteUnico(G("UI/barra_marco.png"));
        spBlanco = SpriteUnico(G("UI/blanco.png"));
        spPerilla = SpriteUnico(G("UI/perilla.png"));
    }

    // =====================================================================
    // Utilidades de UI
    // =====================================================================
    static GameObject CrearCanvas(string nombre, int orden)
    {
        GameObject go = Nuevo(nombre);
        go.layer = LayerMask.NameToLayer("UI");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = orden;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static void CrearEventSystem()
    {
        GameObject es = Nuevo("EventSystem");
        es.AddComponent<EventSystem>();
        // Sin acciones asignadas, el módulo usa las acciones de UI por defecto del Input System al activarse
        es.AddComponent<InputSystemUIInputModule>();
    }

    static RectTransform UI(string nombre, Transform padre)
    {
        GameObject go = Nuevo(nombre, padre);
        go.layer = LayerMask.NameToLayer("UI");
        return go.AddComponent<RectTransform>();
    }

    static RectTransform Colocar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 tamaño)
    {
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamaño;
        return rt;
    }

    static RectTransform Estirar(RectTransform rt, float margen = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
        return rt;
    }

    static Image Imagen(Transform padre, string nombre, Sprite sprite, Color color, bool rebanada = false)
    {
        RectTransform rt = UI(nombre, padre);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (rebanada)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.3f;
        }
        return img;
    }

    static TextMeshProUGUI Texto(Transform padre, string nombre, string contenido, float tamaño, Color color, bool titulo,
        TextAlignmentOptions alineacion = TextAlignmentOptions.Center)
    {
        RectTransform rt = UI(nombre, padre);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = titulo ? fuenteTitulos : fuenteHud;
        t.fontSharedMaterial = titulo ? materialContorno : materialHud;
        t.text = contenido;
        t.fontSize = tamaño;
        t.color = color;
        t.alignment = alineacion;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    static Button Boton(Transform padre, string nombre, string texto, Vector2 posicion, Vector2 tamaño, float tamañoTexto = 40f)
    {
        Image img = Imagen(padre, nombre, spBoton, Color.white, true);
        img.raycastTarget = true;
        Colocar(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), posicion, tamaño);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.92f, 0.75f);
        cb.selectedColor = new Color(1f, 0.92f, 0.75f);
        cb.pressedColor = new Color(0.8f, 0.7f, 0.5f);
        b.colors = cb;
        TextMeshProUGUI t = Texto(img.transform, "Texto", texto, tamañoTexto, Crema, true);
        Estirar(t.rectTransform);
        BotonAnimado ba = img.gameObject.AddComponent<BotonAnimado>();
        Asignar(ba, "texto", t);
        return b;
    }

    static Slider Deslizador(Transform padre, string nombre, string etiqueta, Vector2 posicion)
    {
        RectTransform raiz = Colocar(UI(nombre, padre), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), posicion, new Vector2(620f, 100f));
        TextMeshProUGUI e = Texto(raiz, "Etiqueta", etiqueta, 26f, Crema, false, TextAlignmentOptions.Left);
        Colocar(e.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(620f, 40f));

        RectTransform barra = Colocar(UI("Barra", raiz), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(620f, 40f));
        Slider s = barra.gameObject.AddComponent<Slider>();
        Image fondo = Imagen(barra, "Fondo", spMarco, new Color(0.3f, 0.3f, 0.3f), true);
        Estirar(fondo.rectTransform);
        RectTransform areaRelleno = Estirar(UI("AreaRelleno", barra), 7f);
        Image relleno = Imagen(areaRelleno, "Relleno", spBlanco, Naranja);
        Estirar(relleno.rectTransform);
        RectTransform areaPerilla = Estirar(UI("AreaPerilla", barra), 0f);
        areaPerilla.offsetMin = new Vector2(14f, 0f);
        areaPerilla.offsetMax = new Vector2(-14f, 0f);
        Image perilla = Imagen(areaPerilla, "Perilla", spPerilla, Color.white);
        perilla.raycastTarget = true;
        perilla.rectTransform.anchorMin = new Vector2(0f, 0f);
        perilla.rectTransform.anchorMax = new Vector2(0f, 1f);
        perilla.rectTransform.sizeDelta = new Vector2(30f, 16f);
        fondo.raycastTarget = true;

        s.fillRect = relleno.rectTransform;
        s.handleRect = perilla.rectTransform;
        s.targetGraphic = perilla;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = 0.7f;
        return s;
    }

    static Image Ventana(Transform padre, string nombre, Vector2 tamaño)
    {
        Image v = Imagen(padre, nombre, spPanel, Color.white, true);
        v.raycastTarget = true;
        Colocar(v.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, tamaño);
        return v;
    }

    static Image FondoOscuro(Transform padre, string nombre, Color color)
    {
        Image f = Imagen(padre, nombre, null, color);
        f.raycastTarget = true; // bloquea clics al juego
        Estirar(f.rectTransform);
        return f;
    }

    // Panel con sliders de volumen (se reutiliza en pausa y menú)
    static GameObject PanelVolumen(Transform padre, string nombre, out Button volver)
    {
        Image v = Ventana(padre, nombre, new Vector2(800f, 600f));
        TextMeshProUGUI t = Texto(v.transform, "Titulo", "OPCIONES", 80f, Naranja, true);
        Colocar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(700f, 100f));
        Slider musica = Deslizador(v.transform, "SliderMusica", "MÚSICA", new Vector2(0f, 70f));
        Slider efectos = Deslizador(v.transform, "SliderEfectos", "EFECTOS", new Vector2(0f, -50f));
        volver = Boton(v.transform, "BotonVolver", "VOLVER", new Vector2(0f, -200f), new Vector2(420f, 80f));
        PanelOpciones po = v.gameObject.AddComponent<PanelOpciones>();
        Asignar(po, "sliderMusica", musica);
        Asignar(po, "sliderEfectos", efectos);
        return v.gameObject;
    }

    // =====================================================================
    // Interfaz de nivel: HUD + pausa + pantalla final
    // =====================================================================
    static void CrearInterfazNivel()
    {
        CargarSpritesUI();
        GameObject canvas = CrearCanvas("InterfazNivel", 10);
        Transform c = canvas.transform;

        // ----- Panel superior izquierdo -----
        Image panel = Imagen(c, "PanelJugador", spPanel, new Color(1f, 1f, 1f, 0.92f), true);
        Colocar(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(600f, 200f));
        Image retrato = Imagen(panel.transform, "Retrato", SpriteUnico(G("UI/icono_vida.png")), Color.white);
        retrato.preserveAspect = true;
        Colocar(retrato.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -20f), new Vector2(72f, 72f));
        TextMeshProUGUI vidas = Texto(panel.transform, "TextoVidas", "x3", 30f, Crema, false, TextAlignmentOptions.Left);
        Colocar(vidas.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -22f), new Vector2(120f, 40f));
        TextMeshProUGUI etiquetaVida = Texto(panel.transform, "EtiquetaVida", "VIDA", 18f, Oliva * 1.6f, false, TextAlignmentOptions.Left);
        Colocar(etiquetaVida.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230f, -28f), new Vector2(120f, 30f));
        Image marco = Imagen(panel.transform, "MarcoVida", spMarco, Color.white, true);
        Colocar(marco.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -62f), new Vector2(470f, 34f));
        Image barraVida = Imagen(marco.transform, "BarraVida", spBlanco, new Color(0.45f, 0.8f, 0.3f));
        barraVida.type = Image.Type.Filled;
        barraVida.fillMethod = Image.FillMethod.Horizontal;
        Estirar(barraVida.rectTransform, 6f);

        Image iconoArma = Imagen(panel.transform, "IconoArma", SpriteUnico(G("Objetos/icono_pistola.png")), Color.white);
        iconoArma.preserveAspect = true;
        Colocar(iconoArma.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -110f), new Vector2(72f, 72f));
        TextMeshProUGUI textoArma = Texto(panel.transform, "TextoArma", "PISTOLA", 20f, Crema, false, TextAlignmentOptions.Left);
        Colocar(textoArma.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -112f), new Vector2(300f, 30f));
        TextMeshProUGUI municion = Texto(panel.transform, "TextoMunicion", "∞", 28f, Naranja, false, TextAlignmentOptions.Left);
        Colocar(municion.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -146f), new Vector2(200f, 40f));
        Image iconoGranada = Imagen(panel.transform, "IconoGranada", SpriteUnico(G("UI/icono_granada.png")), Color.white);
        iconoGranada.preserveAspect = true;
        Colocar(iconoGranada.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, -112f), new Vector2(44f, 50f));
        TextMeshProUGUI granadas = Texto(panel.transform, "TextoGranadas", "x5", 28f, Crema, false, TextAlignmentOptions.Left);
        Colocar(granadas.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(474f, -120f), new Vector2(110f, 40f));

        // ----- Puntuación (arriba a la derecha) -----
        TextMeshProUGUI etiquetaPuntos = Texto(c, "EtiquetaPuntos", "PUNTOS", 20f, Oliva * 1.6f, false, TextAlignmentOptions.Right);
        Colocar(etiquetaPuntos.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(400f, 30f));
        TextMeshProUGUI puntos = Texto(c, "TextoPuntos", "0000000", 42f, Crema, false, TextAlignmentOptions.Right);
        Colocar(puntos.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -66f), new Vector2(500f, 56f));

        // ----- Barra del jefe -----
        RectTransform panelJefe = Colocar(UI("PanelJefe", c), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(1000f, 100f));
        TextMeshProUGUI nombreJefe = Texto(panelJefe, "NombreJefe", "JEFE", 34f, Rojo, true);
        Colocar(nombreJefe.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1000f, 46f));
        Image marcoJefe = Imagen(panelJefe, "MarcoJefe", spMarco, Color.white, true);
        Colocar(marcoJefe.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(960f, 38f));
        Image barraJefe = Imagen(marcoJefe.transform, "BarraJefe", spBlanco, Rojo);
        barraJefe.type = Image.Type.Filled;
        barraJefe.fillMethod = Image.FillMethod.Horizontal;
        Estirar(barraJefe.rectTransform, 6f);
        panelJefe.anchoredPosition = new Vector2(0f, -36f);

        // ----- Aviso central -----
        TextMeshProUGUI aviso = Texto(c, "TextoAviso", "¡PELIGRO!", 70f, Naranja, true);
        Colocar(aviso.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(1600f, 120f));

        // ----- Tarjeta de título -----
        RectTransform tarjeta = Colocar(UI("TarjetaTitulo", c), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1920f, 240f));
        CanvasGroup cgTarjeta = tarjeta.gameObject.AddComponent<CanvasGroup>();
        cgTarjeta.blocksRaycasts = false;
        Image franja = Imagen(tarjeta, "Franja", spBlanco, new Color(0.05f, 0.07f, 0.04f, 0.75f));
        Estirar(franja.rectTransform);
        Image lineaArriba = Imagen(tarjeta, "LineaArriba", spBlanco, Naranja);
        Colocar(lineaArriba.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1920f, 6f));
        Image lineaAbajo = Imagen(tarjeta, "LineaAbajo", spBlanco, Naranja);
        Colocar(lineaAbajo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 6f));
        TextMeshProUGUI titulo = Texto(tarjeta, "Titulo", "MISIÓN", 86f, Naranja, true);
        Colocar(titulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1800f, 110f));
        TextMeshProUGUI subtitulo = Texto(tarjeta, "Subtitulo", "", 30f, Crema, false);
        Colocar(subtitulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1800f, 50f));

        // ----- Briefing -----
        Image briefing = FondoOscuro(c, "PanelBriefing", new Color(0f, 0f, 0f, 0.88f));
        Image vb = Ventana(briefing.transform, "Ventana", new Vector2(1240f, 720f));
        TextMeshProUGUI tb = Texto(vb.transform, "Titulo", "BRIEFING DE MISIÓN", 64f, Naranja, true);
        Colocar(tb.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1100f, 90f));
        TextMeshProUGUI cuerpo = Texto(vb.transform, "Cuerpo", "", 34f, Crema, true, TextAlignmentOptions.TopLeft);
        cuerpo.fontSharedMaterial = fuenteTitulos.material;
        Colocar(cuerpo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1080f, 440f));
        TextMeshProUGUI pie = Texto(vb.transform, "Continuar", "ENTER / ESPACIO / (A)  PARA COMENZAR", 22f, Naranja, false);
        Colocar(pie.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1100f, 40f));

        HUD hud = canvas.AddComponent<HUD>();
        Asignar(hud, "retrato", retrato);
        Asignar(hud, "barraVida", barraVida);
        Asignar(hud, "textoVidas", vidas);
        Asignar(hud, "iconoArma", iconoArma);
        Asignar(hud, "textoArma", textoArma);
        Asignar(hud, "textoMunicion", municion);
        Asignar(hud, "textoGranadas", granadas);
        Asignar(hud, "textoPuntos", puntos);
        Asignar(hud, "panelJefe", panelJefe.gameObject);
        Asignar(hud, "barraJefe", barraJefe);
        Asignar(hud, "nombreJefe", nombreJefe);
        Asignar(hud, "tarjetaTitulo", cgTarjeta);
        Asignar(hud, "textoTitulo", titulo);
        Asignar(hud, "textoSubtitulo", subtitulo);
        Asignar(hud, "textoAviso", aviso);
        Asignar(hud, "panelBriefing", briefing.gameObject);
        Asignar(hud, "textoBriefing", cuerpo);

        // ----- Menú de pausa (ventana 800x600 sobre el juego oscurecido) -----
        Image raizPausa = FondoOscuro(c, "MenuPausa", OscuroAlfa);
        Image vp = Ventana(raizPausa.transform, "VentanaPausa", new Vector2(800f, 600f));
        TextMeshProUGUI tp = Texto(vp.transform, "Titulo", "PAUSA", 96f, Naranja, true);
        Colocar(tp.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 120f));
        Button bReanudar = Boton(vp.transform, "BotonReanudar", "REANUDAR", new Vector2(0f, 95f), new Vector2(520f, 80f));
        Button bReiniciar = Boton(vp.transform, "BotonReiniciar", "REINICIAR", new Vector2(0f, -1f), new Vector2(520f, 80f));
        Button bOpciones = Boton(vp.transform, "BotonOpciones", "OPCIONES", new Vector2(0f, -97f), new Vector2(520f, 80f));
        Button bSalir = Boton(vp.transform, "BotonSalir", "SALIR AL MENÚ", new Vector2(0f, -193f), new Vector2(520f, 80f));
        GameObject vo = PanelVolumen(raizPausa.transform, "VentanaOpciones", out Button bVolver);
        MenuPausa mp = canvas.AddComponent<MenuPausa>();
        Asignar(mp, "raizPausa", raizPausa.gameObject);
        Asignar(mp, "ventanaPrincipal", vp.gameObject);
        Asignar(mp, "ventanaOpciones", vo);
        Asignar(mp, "botonReanudar", bReanudar);
        Asignar(mp, "botonReiniciar", bReiniciar);
        Asignar(mp, "botonOpciones", bOpciones);
        Asignar(mp, "botonSalir", bSalir);
        Asignar(mp, "botonVolverOpciones", bVolver);

        // ----- Game Over / Victoria (misma ventana) -----
        Image raizFinal = FondoOscuro(c, "PantallaFinal", OscuroAlfa);
        Image vf = Ventana(raizFinal.transform, "VentanaFinal", new Vector2(800f, 600f));
        TextMeshProUGUI tf = Texto(vf.transform, "Titulo", "GAME OVER", 100f, Rojo, true);
        Colocar(tf.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(760f, 120f));
        TextMeshProUGUI tpts = Texto(vf.transform, "Puntuacion", "PUNTUACIÓN: 0000000", 30f, Crema, false);
        Colocar(tpts.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(760f, 50f));
        TextMeshProUGUI thist = Texto(vf.transform, "Historia", "", 26f, Crema, true);
        thist.fontSharedMaterial = fuenteTitulos.material;
        Colocar(thist.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(700f, 150f));
        Button bfReiniciar = Boton(vf.transform, "BotonReiniciar", "REINICIAR NIVEL ACTUAL", new Vector2(0f, -150f), new Vector2(600f, 80f), 36f);
        Button bfMenu = Boton(vf.transform, "BotonMenu", "MENÚ PRINCIPAL", new Vector2(0f, -245f), new Vector2(600f, 80f), 36f);
        PantallaFinal pf = canvas.AddComponent<PantallaFinal>();
        Asignar(pf, "raiz", raizFinal.gameObject);
        Asignar(pf, "textoTitulo", tf);
        Asignar(pf, "textoPuntuacion", tpts);
        Asignar(pf, "textoHistoria", thist);
        Asignar(pf, "botonReiniciar", bfReiniciar);
        Asignar(pf, "botonMenu", bfMenu);
    }

    // =====================================================================
    // Splash
    // =====================================================================
    static void CrearEscenaSplash()
    {
        CargarSpritesUI();
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cam = CrearCamara(new Color(0.03f, 0.04f, 0.03f), Vector3.zero);
        Object.DestroyImmediate(cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>());
        CrearLuzGlobal();

        GameObject canvas = CrearCanvas("CanvasSplash", 0);
        RectTransform grupoTitulo = Estirar(UI("GrupoTitulo", canvas.transform));
        CanvasGroup cgT = grupoTitulo.gameObject.AddComponent<CanvasGroup>();
        TextMeshProUGUI titulo = Texto(grupoTitulo, "Titulo", "BLACKOUT SQUAD", 170f, Naranja, true);
        Colocar(titulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1800f, 220f));
        TextMeshProUGUI sub = Texto(grupoTitulo, "Subtitulo", "UN RUN & GUN DEL EQUIPO BLACKOUT", 30f, Crema, false);
        Colocar(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(1600f, 60f));

        Splash sp = canvas.AddComponent<Splash>();
        Asignar(sp, "grupoTitulo", cgT);
        CrearEventSystem();
        EditorSceneManager.SaveScene(escena, RutaEscenas + "/Splash.unity");
    }

    // =====================================================================
    // Menú principal
    // =====================================================================
    static void CrearEscenaMenu()
    {
        CargarSpritesUI();
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cam = CrearCamara(new Color(0.1f, 0.08f, 0.12f), Vector3.zero);
        Object.DestroyImmediate(cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>());
        CrearLuzGlobal();

        GameObject canvas = CrearCanvas("CanvasMenu", 0);
        Transform c = canvas.transform;

        // Fondo animado: capas de montañas que se desplazan a distintas velocidades
        string m = RutaArte + "/ParallaxMountain_ansimuz/parallax-mountain-";
        (string archivo, float vel)[] capas =
        {
            ("bg.png", 0.004f), ("montain-far.png", 0.01f), ("mountains.png", 0.02f), ("trees.png", 0.04f), ("foreground-trees.png", 0.08f)
        };
        foreach (var capa in capas)
        {
            Texture2D tex = Cargar<Texture2D>(m + capa.archivo);
            RectTransform rt = Estirar(UI("Fondo_" + capa.archivo, c));
            RawImage ri = rt.gameObject.AddComponent<RawImage>();
            ri.texture = tex;
            ri.raycastTarget = false;
            FondoAnimadoUI fa = rt.gameObject.AddComponent<FondoAnimadoUI>();
            float repeticiones = (1920f / 1080f) / ((float)tex.width / tex.height);
            Asignar(fa, "velocidad", capa.vel);
            Asignar(fa, "repeticionesX", repeticiones);
            ri.uvRect = new Rect(0f, 0f, repeticiones, 1f);
        }
        Image sombra = Imagen(c, "Sombra", spBlanco, new Color(0.02f, 0.03f, 0.02f, 0.45f));
        Estirar(sombra.rectTransform);
        Image piso = Imagen(c, "Piso", spBlanco, new Color(0.08f, 0.06f, 0.1f));
        Colocar(piso.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, AltoPisoMenu));
        Image bordePiso = Imagen(c, "BordePiso", spBlanco, Naranja);
        Colocar(bordePiso.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, AltoPisoMenu), new Vector2(1920f, 6f));

        // Los dos comandos corriendo sobre el suelo, cada uno con su nombre debajo
        const float escalaSoldado = 4f, xFrente = 730f, separacion = 430f;
        for (int i = Escuadron.Length - 1; i >= 0; i--)
        {
            float xSoldado = xFrente - separacion * i;
            Sprite[] cuadros = Frames(G("Personajes/" + Escuadron[i].hoja + ".png"));
            Image soldado = Imagen(c, "Soldado_" + Escuadron[i].hoja, cuadros[2], Color.white);
            soldado.preserveAspect = true;
            Vector2 tamañoCelda = cuadros[2].rect.size * escalaSoldado;
            Colocar(soldado.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(xSoldado, AltoPisoMenu), tamañoCelda);
            AnimadorImagenUI ai = soldado.gameObject.AddComponent<AnimadorImagenUI>();
            Asignar(ai, "frames", Elegir(cuadros, 2, 3, 4, 5));
            Asignar(ai, "fps", 10f);
            Asignar(ai, "desfase", i * 0.13f);

            TextMeshProUGUI nombre = Texto(c, "Nombre_" + Escuadron[i].hoja, Escuadron[i].nombre, 34f, Naranja, true);
            Colocar(nombre.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(xSoldado, 62f), new Vector2(420f, 50f));
        }

        // Título
        TextMeshProUGUI titulo = Texto(c, "Titulo", "BLACKOUT SQUAD", 160f, Naranja, true);
        Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1800f, 200f));
        TextMeshProUGUI sub = Texto(c, "Subtitulo", "OPERACIÓN APAGÓN", 34f, Crema, false);
        Colocar(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(1600f, 50f));

        // Panel principal
        RectTransform panelPrincipal = Estirar(UI("PanelPrincipal", c));
        float x = 500f;
        Image marcoBotones = Imagen(panelPrincipal, "MarcoBotones", spPanel, new Color(1f, 1f, 1f, 0.92f), true);
        Colocar(marcoBotones.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -70f), new Vector2(620f, 430f));
        Button jugar = Boton(panelPrincipal, "BotonJugar", "JUGAR", new Vector2(x, 40f), new Vector2(520f, 90f), 48f);
        Button opciones = Boton(panelPrincipal, "BotonOpciones", "OPCIONES", new Vector2(x, -70f), new Vector2(520f, 90f), 48f);
        Button salir = Boton(panelPrincipal, "BotonSalir", "SALIR", new Vector2(x, -180f), new Vector2(520f, 90f), 48f);
        TextMeshProUGUI ayuda = Texto(panelPrincipal, "Controles",
            "A/D MOVER · W SALTAR · ESPACIO DISPARAR · ↑ APUNTAR ARRIBA · S AGACHARSE · E GRANADA · SHIFT RODAR · ESC PAUSA",
            16f, Crema, false);
        Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1880f, 40f));

        // Panel de opciones
        RectTransform contOpciones = Estirar(UI("PanelOpciones", c));
        PanelVolumen(contOpciones, "VentanaOpciones", out Button volverOpciones);

        SeleccionPersonaje seleccion = CrearPanelSeleccion(c);

        MenuPrincipal mp = canvas.AddComponent<MenuPrincipal>();
        Asignar(mp, "panelPrincipal", panelPrincipal.gameObject);
        Asignar(mp, "panelOpciones", contOpciones.gameObject);
        Asignar(mp, "seleccion", seleccion);
        Asignar(mp, "botonJugar", jugar);
        Asignar(mp, "botonOpciones", opciones);
        Asignar(mp, "botonSalir", salir);
        Asignar(mp, "botonVolverOpciones", volverOpciones);

        CrearEventSystem();
        EditorSceneManager.SaveScene(escena, RutaEscenas + "/MainMenu.unity");
    }

    const float AltoPisoMenu = 120f;

    // Pantalla previa a la partida: una tarjeta por cada comando del escuadrón
    static SeleccionPersonaje CrearPanelSeleccion(Transform padre)
    {
        Image panel = FondoOscuro(padre, "PanelSeleccion", new Color(0.03f, 0.04f, 0.03f, 1f)); // opaco: tapa por completo el menú principal
        Transform p = panel.transform;

        TextMeshProUGUI titulo = Texto(p, "Titulo", "ELIGE TU COMANDO", 110f, Naranja, true);
        Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1800f, 140f));

        const float anchoTarjeta = 400f, separacion = 520f;
        Button[] tarjetas = new Button[Escuadron.Length];
        Image[] retratos = new Image[Escuadron.Length];
        float primeraX = -separacion * (Escuadron.Length - 1) * 0.5f;
        for (int i = 0; i < Escuadron.Length; i++)
        {
            Button b = Boton(p, "Tarjeta_" + Escuadron[i].hoja, Escuadron[i].nombre, new Vector2(primeraX + separacion * i, 30f), new Vector2(anchoTarjeta, 520f), 32f);
            TextMeshProUGUI nombre = b.GetComponentInChildren<TextMeshProUGUI>();
            Colocar(nombre.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(anchoTarjeta - 30f, 60f));
            Image retrato = Imagen(b.transform, "Retrato", Frames(G("Personajes/" + Escuadron[i].hoja + ".png"))[0], Color.white);
            retrato.preserveAspect = true;
            Colocar(retrato.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(336f, 360f));
            tarjetas[i] = b;
            retratos[i] = retrato;
        }

        TextMeshProUGUI descripcion = Texto(p, "Descripcion", "", 24f, Crema, false);
        Colocar(descripcion.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -285f), new Vector2(1700f, 80f));
        Button volver = Boton(p, "BotonVolver", "VOLVER", new Vector2(0f, -420f), new Vector2(420f, 80f));

        SeleccionPersonaje s = panel.gameObject.AddComponent<SeleccionPersonaje>();
        Asignar(s, "tarjetas", tarjetas);
        Asignar(s, "retratos", retratos);
        Asignar(s, "textoDescripcion", descripcion);
        Asignar(s, "botonVolver", volver);
        return s;
    }
}
