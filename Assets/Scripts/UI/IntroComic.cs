using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Intro tipo cómic de la Operación Apagón: 4 viñetas antes del briefing de la Misión 1 (solo en partida nueva).
// Las imágenes están en Resources/Intro (las genera Herramientas/generar_intro.py con el arte del juego).
// Aceptar (Enter, Espacio, clic, A/Cruz o tocar la pantalla) completa el texto o pasa a la siguiente viñeta;
// SALTAR (o ESC / Start / B) termina la intro. Todo se crea por código: no modifica la escena.
public class IntroComic : MonoBehaviour
{
    struct Vineta
    {
        public string imagen, texto;
        public Vineta(string imagen, string texto) { this.imagen = imagen; this.texto = texto; }
    }

    const float VelocidadTexto = 45f; // letras por segundo

    Image imagen, fondo;
    TextMeshProUGUI texto, contador, ayuda;
    CanvasGroup grupoVineta;
    bool saltar;

    // Muestra la intro completa y termina cuando el jugador la acaba o la salta (funciona con Time.timeScale = 0)
    public static IEnumerator Reproducir(PersonajeDatos personaje)
    {
        string nombre = personaje != null && !string.IsNullOrEmpty(personaje.nombre) ? personaje.nombre : "COMANDO ALFA";
        bool esBravo = nombre.ToUpperInvariant().Contains("BRAVO");
        Vineta[] vinetas =
        {
            new Vineta("Intro/vineta1", "En lo alto de las montañas, el <color=#E8603A>COMANDANTE KRUGER</color> reúne a su ejército en una fortaleza de acero."),
            new Vineta("Intro/vineta2", "Su plan, la <color=#F0A030>OPERACIÓN APAGÓN</color>: dejar a oscuras toda la región y avanzar con sus tanques en plena noche."),
            new Vineta("Intro/vineta3", "Solo un escuadrón puede detenerlo antes de que sea demasiado tarde: el <color=#F0A030>BLACKOUT SQUAD</color>."),
            new Vineta(esBravo ? "Intro/vineta4_bravo" : "Intro/vineta4_alfa", "Primer objetivo: cruzar la selva hostil y llegar al campamento enemigo. ¡Adelante, <color=#F0A030>" + nombre + "</color>!"),
        };

        // La intro corre su propia rutina (no depende del objeto que la llamó) y aquí solo se espera a que termine
        GameObject go = new GameObject("IntroComic");
        IntroComic intro = go.AddComponent<IntroComic>();
        intro.Construir();
        intro.StartCoroutine(intro.Correr(vinetas));
        while (intro != null && !intro.terminado) yield return null;
        if (go != null) Destroy(go);
        ControlesJuego.IgnorarAccionesEsteFrame();
    }

    bool terminado;

    IEnumerator Correr(Vineta[] vinetas)
    {
        yield return PrepararFondo();
        yield return Secuencia(vinetas);
        terminado = true;
    }

    // Fondo: el nivel desenfocado en lugar de negro
    IEnumerator PrepararFondo()
    {
        CanvasGroup raiz = gameObject.AddComponent<CanvasGroup>();
        raiz.alpha = 0f;

        // Espera a que termine el fundido de entrada al nivel (si no, la foto saldría negra)
        float limite = Time.unscaledTime + 3f;
        while (Transicion.Cubriendo && Time.unscaledTime < limite) yield return null;

        // El HUD no debe salir en la foto
        Canvas hud = HUD.Instancia != null ? HUD.Instancia.GetComponent<Canvas>() : null;
        bool hudVisible = hud != null && hud.enabled;
        if (hudVisible) hud.enabled = false;
        Texture2D tex = null;
        yield return FondoDesenfocado.Capturar(t => tex = t);
        if (hudVisible) hud.enabled = true;

        if (tex != null)
        {
            FondoDesenfocado.Aplicar(transform, tex, 0.5f);
            fondo.color = new Color(0f, 0f, 0f, 0f); // sigue bloqueando toques al juego
        }

        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            raiz.alpha = t / 0.3f;
            yield return null;
        }
        raiz.alpha = 1f;
    }

    IEnumerator Secuencia(Vineta[] vinetas)
    {
        for (int i = 0; i < vinetas.Length && !saltar; i++)
        {
            Sprite sp = Resources.Load<Sprite>(vinetas[i].imagen);
            if (sp == null) continue;
            imagen.sprite = sp;
            contador.text = (i + 1) + " / " + vinetas.Length;
            texto.text = vinetas[i].texto;
            texto.maxVisibleCharacters = 0;
            texto.ForceMeshUpdate();
            int total = texto.textInfo.characterCount;
            AudioManager.SonarUI(Sfx.UiClick);

            // Entrada de la viñeta: aparece y se acerca un poco
            float t = 0f, letras = 0f;
            bool textoCompleto = false;
            yield return null; // que el toque que abrió la viñeta no cuente como "aceptar"
            while (!saltar)
            {
                t += Time.unscaledDeltaTime;
                float entrada = Mathf.Clamp01(t / 0.35f);
                grupoVineta.alpha = entrada;
                imagen.rectTransform.localScale = Vector3.one * (Mathf.Lerp(1.08f, 1f, 1f - (1f - entrada) * (1f - entrada)) + t * 0.006f);

                if (!textoCompleto)
                {
                    letras += Time.unscaledDeltaTime * VelocidadTexto;
                    texto.maxVisibleCharacters = Mathf.Min(total, (int)letras);
                    textoCompleto = letras >= total;
                }
                ayuda.alpha = textoCompleto ? 0.6f + Mathf.Sin(Time.unscaledTime * 5f) * 0.4f : 0f;
                ayuda.text = "> " + ControlesJuego.TextoBotonAceptar + " PARA CONTINUAR";

                if (ControlesJuego.PausaPresionada || ControlesJuego.CancelarPresionado) saltar = true;
                else if (ControlesJuego.AceptarPresionado && t > 0.2f)
                {
                    if (!textoCompleto) { textoCompleto = true; texto.maxVisibleCharacters = total; }
                    else break;
                }
                yield return null;
            }

            // Salida rápida
            for (float s = 0f; s < 0.15f && !saltar; s += Time.unscaledDeltaTime)
            {
                grupoVineta.alpha = 1f - s / 0.15f;
                yield return null;
            }
        }
    }

    // ---------- Interfaz ----------
    void Construir()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30; // encima del HUD y de los controles táctiles; debajo de la transición
        CanvasScaler cs = gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 1f; // ajusta al alto: en pantallas anchas queda todo dentro
        gameObject.AddComponent<GraphicRaycaster>();

        TMP_FontAsset fuenteTitulo = null, fuenteTexto = null;
        foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (t.font == null) continue;
            string n = t.font.name;
            if (fuenteTitulo == null && n.Contains("BlackOps")) fuenteTitulo = t.font;
            if (fuenteTexto == null && n.Contains("PressStart")) fuenteTexto = t.font;
        }

        fondo = Crear<Image>("Fondo", transform);
        Estirar(fondo.rectTransform);
        fondo.color = new Color(0.06f, 0.04f, 0.05f, 1f);
        fondo.raycastTarget = true; // bloquea toques al juego

        GameObject cont = new GameObject("Vineta", typeof(RectTransform));
        cont.transform.SetParent(transform, false);
        grupoVineta = cont.AddComponent<CanvasGroup>();
        Estirar((RectTransform)cont.transform);

        imagen = Crear<Image>("Imagen", cont.transform);
        imagen.preserveAspect = true;
        Colocar(imagen.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1180f, 737f));

        // Cuadro de narración estilo cómic (crema con borde oscuro)
        Image borde = Crear<Image>("Narracion", cont.transform);
        borde.color = new Color(0.06f, 0.03f, 0.04f);
        Colocar(borde.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1260f, 200f));
        Image caja = Crear<Image>("Caja", borde.transform);
        caja.color = new Color(0.98f, 0.93f, 0.76f);
        Estirar(caja.rectTransform, 6f);
        texto = Crear<TextMeshProUGUI>("Texto", caja.transform);
        Estirar(texto.rectTransform, 22f);
        if (fuenteTitulo != null) texto.font = fuenteTitulo;
        texto.fontSize = 40f;
        texto.color = new Color(0.1f, 0.06f, 0.06f);
        texto.alignment = TextAlignmentOptions.Center;
        texto.textWrappingMode = TextWrappingModes.Normal;

        contador = Crear<TextMeshProUGUI>("Contador", transform);
        Colocar(contador.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(260f, 40f), new Vector2(0f, 1f));
        if (fuenteTexto != null) contador.font = fuenteTexto;
        contador.fontSize = 24f;
        contador.color = new Color(0.94f, 0.63f, 0.19f);

        ayuda = Crear<TextMeshProUGUI>("Ayuda", transform);
        Colocar(ayuda.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 12f), new Vector2(900f, 40f), new Vector2(1f, 0f));
        if (fuenteTexto != null) ayuda.font = fuenteTexto;
        ayuda.fontSize = 20f;
        ayuda.alignment = TextAlignmentOptions.Right;
        ayuda.color = new Color(0.93f, 0.9f, 0.78f);

        // Botón SALTAR (se puede tocar o hacer clic)
        Image fondoSaltar = Crear<Image>("BotonSaltar", transform);
        fondoSaltar.color = new Color(0f, 0f, 0f, 0.55f);
        fondoSaltar.raycastTarget = true;
        Colocar(fondoSaltar.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -24f), new Vector2(230f, 64f), new Vector2(1f, 1f));
        Button saltarBtn = fondoSaltar.gameObject.AddComponent<Button>();
        saltarBtn.onClick.AddListener(() => saltar = true);
        TextMeshProUGUI tSaltar = Crear<TextMeshProUGUI>("Texto", fondoSaltar.transform);
        Estirar(tSaltar.rectTransform);
        if (fuenteTitulo != null) tSaltar.font = fuenteTitulo;
        tSaltar.text = "SALTAR >>";
        tSaltar.fontSize = 30f;
        tSaltar.alignment = TextAlignmentOptions.Center;
        tSaltar.color = new Color(0.93f, 0.9f, 0.78f);
    }

    static T Crear<T>(string nombre, Transform padre) where T : Graphic
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        T g = go.AddComponent<T>();
        g.raycastTarget = false;
        return g;
    }

    static void Estirar(RectTransform rt, float margen = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    static void Colocar(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tamaño, Vector2? pivote = null)
    {
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = pivote ?? new Vector2(0.5f, ancla.y);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamaño;
    }
}
