using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pantalla de carga al entrar a cada misión: nombre de la misión, un consejo al azar y una barra de progreso.
// Se dibuja sobre el fundido a negro de Transicion (se crea por código la primera vez y se reutiliza).
public static class PantallaCarga
{
    public const float TiempoMinimo = 2.4f; // segundos para alcanzar a leer el consejo

    static readonly string[] Consejos =
    {
        "Agáchate para esquivar las balas rectas de los soldados.",
        "Al soldado con escudo dale por detrás o lánzale una granada.",
        "Rodar te hace invencible un instante: úsalo para atravesar disparos.",
        "Al tanque dale por detrás o desde arriba.",
        "Mantén presionado SALTAR para llegar más alto.",
        "Las banderas son puntos de control: si pierdes una vida, reapareces ahí.",
        "Rompe los cofres: pueden traer botiquines, granadas o una ametralladora.",
        "Las armas especiales tienen munición limitada; al acabarse vuelves a la pistola.",
        "Cuidado con los fosos, el agua y el alambre de púas.",
        "Termina una misión sin morir y gana un bono de 5000 puntos.",
        "Puedes disparar en 8 direcciones, incluso hacia abajo mientras saltas.",
        "Los bloques agrietados se pueden destruir a balazos.",
        "Kruger cambia su forma de atacar cuando le queda poca vida. ¡No te confíes!",
        "Las torretas no se mueven: acércate agachado y destrúyelas.",
    };

    static readonly string[] Misiones = { "SELVA HOSTIL", "BASE MILITAR", "LA FORTALEZA" };

    static GameObject raiz;
    static TextMeshProUGUI titulo, subtitulo, consejo, cargando;
    static Image barra;
    static int ultimoConsejo = -1;

    public static void Mostrar(Transform canvasTransicion, string escena)
    {
        if (raiz == null) Construir(canvasTransicion);
        if (raiz == null) return;

        int numero = 1;
        int.TryParse(escena.Replace("Nivel", ""), out numero);
        titulo.text = "MISIÓN " + numero;
        subtitulo.text = numero >= 1 && numero <= Misiones.Length ? Misiones[numero - 1] : "";

        int i;
        do { i = Random.Range(0, Consejos.Length); } while (i == ultimoConsejo && Consejos.Length > 1);
        ultimoConsejo = i;
        consejo.text = Consejos[i];

        Progreso(0f);
        raiz.SetActive(true);
    }

    public static void Progreso(float valor)
    {
        if (raiz == null) return;
        barra.fillAmount = Mathf.Clamp01(valor);
        int puntos = 1 + (int)(Time.unscaledTime * 3f) % 3;
        cargando.text = "CARGANDO" + new string('.', puntos);
    }

    public static void Ocultar()
    {
        if (raiz != null) raiz.SetActive(false);
    }

    // ---------- Interfaz ----------
    static void Construir(Transform canvasTransicion)
    {
        if (canvasTransicion == null) return;

        // Escala igual que el resto de la interfaz del juego
        if (canvasTransicion.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler cs = canvasTransicion.gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight = 0.5f;
        }

        TMP_FontAsset fuenteTitulo = null, fuenteTexto = null;
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (t.font == null) continue;
            if (fuenteTitulo == null && t.font.name.Contains("BlackOps")) fuenteTitulo = t.font;
            if (fuenteTexto == null && t.font.name.Contains("PressStart")) fuenteTexto = t.font;
        }

        raiz = new GameObject("PantallaCarga", typeof(RectTransform));
        raiz.transform.SetParent(canvasTransicion, false);
        Estirar((RectTransform)raiz.transform);

        titulo = Texto("Titulo", fuenteTitulo, 110f, new Color(1f, 0.68f, 0.18f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1600f, 140f));
        subtitulo = Texto("Subtitulo", fuenteTexto, 34f, new Color(0.93f, 0.9f, 0.78f), new Vector2(0.5f, 0.5f), new Vector2(0f, 135f), new Vector2(1600f, 50f));

        TextMeshProUGUI etiqueta = Texto("EtiquetaConsejo", fuenteTitulo, 38f, new Color(1f, 0.68f, 0.18f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1400f, 50f));
        etiqueta.text = "CONSEJO";
        consejo = Texto("Consejo", fuenteTexto, 28f, new Color(0.93f, 0.9f, 0.78f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(1400f, 120f));
        consejo.textWrappingMode = TextWrappingModes.Normal;
        consejo.lineSpacing = 20f;

        // Barra de carga
        GameObject marco = new GameObject("MarcoBarra", typeof(RectTransform));
        marco.transform.SetParent(raiz.transform, false);
        Image fondo = marco.AddComponent<Image>();
        fondo.color = new Color(0.93f, 0.9f, 0.78f, 0.9f);
        fondo.raycastTarget = false;
        Colocar((RectTransform)marco.transform, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(900f, 30f));
        GameObject interior = new GameObject("Fondo", typeof(RectTransform));
        interior.transform.SetParent(marco.transform, false);
        Image negro = interior.AddComponent<Image>();
        negro.color = new Color(0.08f, 0.06f, 0.06f);
        negro.raycastTarget = false;
        Estirar((RectTransform)interior.transform, 4f);
        GameObject relleno = new GameObject("Relleno", typeof(RectTransform));
        relleno.transform.SetParent(interior.transform, false);
        barra = relleno.AddComponent<Image>();
        barra.color = new Color(1f, 0.68f, 0.18f);
        barra.raycastTarget = false;
        barra.sprite = SpriteBlanco();
        barra.type = Image.Type.Filled;
        barra.fillMethod = Image.FillMethod.Horizontal;
        Estirar((RectTransform)relleno.transform, 3f);

        cargando = Texto("Cargando", fuenteTexto, 24f, new Color(0.93f, 0.9f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0f, 165f), new Vector2(900f, 40f));
        cargando.alignment = TextAlignmentOptions.Left;

        raiz.SetActive(false);
    }

    static TextMeshProUGUI Texto(string nombre, TMP_FontAsset fuente, float tamaño, Color color, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(raiz.transform, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.fontSize = tamaño;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        Colocar(t.rectTransform, ancla, pos, tam);
        return t;
    }

    static void Colocar(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    static void Estirar(RectTransform rt, float margen = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    // Image.Type.Filled necesita un sprite
    static Sprite SpriteBlanco()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }
}
