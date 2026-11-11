using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using FaseToque = UnityEngine.InputSystem.TouchPhase;

public enum BotonTactil { Saltar, Disparar, Granada, Rodar, Pausa }

// Controles en pantalla para celular (Android y navegador en el teléfono).
// Joystick flotante a la izquierda (mover, apuntar arriba y agacharse) y botones a la derecha.
// Solo se muestran durante un nivel, cuando no está pausado. Soporta varios dedos a la vez.
// No usa el EventSystem: lee los toques directamente, así no interfiere con los menús.
// ControlesJuego lee sus valores, así que el resto del juego no necesita saber que existe.
[DefaultExecutionOrder(-500)]
public class ControlesTactiles : MonoBehaviour
{
    // ---------- Valores que lee ControlesJuego ----------
    public static Vector2 Direccion { get; private set; }

    static readonly bool[] mantenido = new bool[5];
    static readonly int[] framePresionado = { -1, -1, -1, -1, -1 };

    public static bool Mantenido(BotonTactil b) => mantenido[(int)b];
    public static bool Presionado(BotonTactil b) => framePresionado[(int)b] == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        Direccion = Vector2.zero;
        for (int i = 0; i < mantenido.Length; i++) { mantenido[i] = false; framePresionado[i] = -1; }
    }

    public static void Crear()
    {
        if (FindAnyObjectByType<ControlesTactiles>() != null) return;
        GameObject go = new GameObject("ControlesTactiles");
        DontDestroyOnLoad(go);
        go.AddComponent<ControlesTactiles>();
    }

    // ---------- Diseño (unidades del canvas de referencia 1920x1080) ----------
    const float RadioBase = 150f, RadioPerilla = 70f;
    static readonly Vector2 PosBase = new Vector2(250f, 240f); // desde la esquina inferior izquierda
    const float ZonaJoystickAncho = 0.45f, ZonaJoystickAlto = 0.75f; // fracción de la pantalla

    class Boton
    {
        public BotonTactil tipo;
        public RectTransform rt;
        public Image imagen;
        public float radio;
        public int dedo = -1;
    }

    RectTransform raiz, baseJoystick, perilla;
    Image imgBase, imgPerilla;
    Boton[] botones;
    int dedoJoystick = -1;
    Vector2 origenJoystick;
    bool visible;
    Sprite spCirculo, spAro;

    static readonly Color ColorBoton = new Color(0.08f, 0.08f, 0.08f, 0.45f); // oscuro: se lee sobre cielo y sobre suelo
    static readonly Color ColorPresionado = new Color(1f, 0.65f, 0.2f, 0.6f);

    void Awake()
    {
        if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
        spCirculo = CrearSpriteCirculo(false);
        spAro = CrearSpriteCirculo(true);
        ConstruirUI();
        SceneManager.sceneLoaded += AlCargarEscena;
        MostrarControles(false);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        SoltarTodo();
        AplicarFuente();
    }

    void Update()
    {
        bool debeVerse = ControladorNivel.Instancia != null && ControladorNivel.Instancia.PuedePausar && !MenuPausa.Pausado;
        if (debeVerse != visible) MostrarControles(debeVerse);
        if (!visible) return;

        foreach (Touch t in Touch.activeTouches)
        {
            Vector2 pos = APosicionLocal(t.screenPosition);
            switch (t.phase)
            {
                case FaseToque.Began:
                    EmpezarToque(t.touchId, pos);
                    break;
                case FaseToque.Moved:
                case FaseToque.Stationary:
                    if (t.touchId == dedoJoystick) MoverJoystick(pos);
                    break;
                case FaseToque.Ended:
                case FaseToque.Canceled:
                    TerminarToque(t.touchId);
                    break;
            }
        }
    }

    // ---------- Toques ----------
    void EmpezarToque(int dedo, Vector2 pos)
    {
        // Primero los botones (tienen prioridad aunque estén cerca de la zona del joystick)
        Boton mejor = null;
        float mejorDist = float.MaxValue;
        foreach (Boton b in botones)
        {
            if (b.dedo != -1) continue;
            float d = Vector2.Distance(pos, b.rt.anchoredPosition + Esquina(b.rt));
            if (d <= b.radio * 1.3f && d < mejorDist) { mejor = b; mejorDist = d; }
        }
        if (mejor != null)
        {
            mejor.dedo = dedo;
            mantenido[(int)mejor.tipo] = true;
            framePresionado[(int)mejor.tipo] = Time.frameCount;
            mejor.imagen.color = ColorPresionado;
            return;
        }

        // Joystick flotante: aparece donde se toca, dentro de la zona izquierda
        Vector2 tam = raiz.rect.size;
        Vector2 desdeAbajoIzq = pos + tam * 0.5f;
        if (dedoJoystick == -1 && desdeAbajoIzq.x < tam.x * ZonaJoystickAncho && desdeAbajoIzq.y < tam.y * ZonaJoystickAlto)
        {
            dedoJoystick = dedo;
            Vector2 minimo = new Vector2(RadioBase, RadioBase) - tam * 0.5f;
            origenJoystick = new Vector2(Mathf.Max(pos.x, minimo.x), Mathf.Max(pos.y, minimo.y));
            baseJoystick.anchoredPosition = origenJoystick + tam * 0.5f;
            imgBase.color = new Color(1f, 1f, 1f, 0.45f);
            imgPerilla.color = new Color(1f, 1f, 1f, 0.7f);
            MoverJoystick(pos);
        }
    }

    void MoverJoystick(Vector2 pos)
    {
        Vector2 delta = Vector2.ClampMagnitude(pos - origenJoystick, RadioBase);
        perilla.anchoredPosition = delta;
        Direccion = delta / RadioBase;
    }

    void TerminarToque(int dedo)
    {
        if (dedo == dedoJoystick) SoltarJoystick();
        foreach (Boton b in botones)
        {
            if (b.dedo != dedo) continue;
            b.dedo = -1;
            mantenido[(int)b.tipo] = false;
            b.imagen.color = ColorBoton;
        }
    }

    void SoltarJoystick()
    {
        dedoJoystick = -1;
        Direccion = Vector2.zero;
        baseJoystick.anchoredPosition = PosBase;
        perilla.anchoredPosition = Vector2.zero;
        imgBase.color = new Color(1f, 1f, 1f, 0.22f);
        imgPerilla.color = new Color(1f, 1f, 1f, 0.35f);
    }

    void SoltarTodo()
    {
        SoltarJoystick();
        foreach (Boton b in botones)
        {
            b.dedo = -1;
            mantenido[(int)b.tipo] = false;
            b.imagen.color = ColorBoton;
        }
    }

    void MostrarControles(bool mostrar)
    {
        visible = mostrar;
        raiz.gameObject.SetActive(mostrar);
        if (!mostrar) SoltarTodo();
    }

    Vector2 APosicionLocal(Vector2 pantalla)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, pantalla, null, out Vector2 local);
        return local;
    }

    // Posición del ancla del botón respecto al centro de la raíz
    Vector2 Esquina(RectTransform rt)
    {
        Vector2 tam = raiz.rect.size;
        return new Vector2((rt.anchorMin.x - 0.5f) * tam.x, (rt.anchorMin.y - 0.5f) * tam.y);
    }

    // ---------- Construcción de la interfaz ----------
    void ConstruirUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // encima del HUD (10), debajo de la transición (500)
        CanvasScaler cs = gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;

        raiz = new GameObject("Raiz", typeof(RectTransform)).GetComponent<RectTransform>();
        raiz.SetParent(transform, false);
        raiz.anchorMin = Vector2.zero;
        raiz.anchorMax = Vector2.one;
        raiz.offsetMin = raiz.offsetMax = Vector2.zero;

        // Joystick
        baseJoystick = Elemento("BaseJoystick", raiz, Vector2.zero, PosBase, RadioBase * 2f, spAro, out imgBase);
        perilla = Elemento("Perilla", baseJoystick, new Vector2(0.5f, 0.5f), Vector2.zero, RadioPerilla * 2f, spCirculo, out imgPerilla);

        // Botones (desde la esquina inferior derecha, salvo la pausa)
        botones = new[]
        {
            CrearBoton(BotonTactil.Disparar, "DISPARAR", new Vector2(1f, 0f), new Vector2(-210f, 210f), 115f, 26f),
            CrearBoton(BotonTactil.Saltar, "SALTAR", new Vector2(1f, 0f), new Vector2(-445f, 150f), 95f, 24f),
            CrearBoton(BotonTactil.Rodar, "RODAR", new Vector2(1f, 0f), new Vector2(-425f, 375f), 78f, 20f),
            CrearBoton(BotonTactil.Granada, "GRANADA", new Vector2(1f, 0f), new Vector2(-200f, 445f), 78f, 18f),
            CrearBoton(BotonTactil.Pausa, "II", new Vector2(0.5f, 1f), new Vector2(0f, -75f), 55f, 30f),
        };
        SoltarTodo();
        AplicarFuente();
    }

    Boton CrearBoton(BotonTactil tipo, string texto, Vector2 ancla, Vector2 pos, float radio, float tamTexto)
    {
        RectTransform rt = Elemento("Boton" + tipo, raiz, ancla, pos, radio * 2f, spCirculo, out Image img);
        img.color = ColorBoton;

        TextMeshProUGUI etiqueta = new GameObject("Texto", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        etiqueta.rectTransform.SetParent(rt, false);
        etiqueta.rectTransform.anchorMin = Vector2.zero;
        etiqueta.rectTransform.anchorMax = Vector2.one;
        etiqueta.rectTransform.offsetMin = etiqueta.rectTransform.offsetMax = Vector2.zero;
        etiqueta.text = texto;
        etiqueta.fontSize = tamTexto;
        etiqueta.alignment = TextAlignmentOptions.Center;
        etiqueta.color = new Color(1f, 1f, 1f, 0.9f);
        etiqueta.raycastTarget = false;
        etiqueta.textWrappingMode = TextWrappingModes.NoWrap;

        return new Boton { tipo = tipo, rt = rt, imagen = img, radio = radio };
    }

    static RectTransform Elemento(string nombre, Transform padre, Vector2 ancla, Vector2 pos, float tamaño, Sprite sprite, out Image img)
    {
        RectTransform rt = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(tamaño, tamaño);
        img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return rt;
    }

    // Usa la misma fuente que el resto de la interfaz de la escena
    void AplicarFuente()
    {
        if (botones == null) return;
        TMP_FontAsset fuente = null;
        foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (t.font != null && t.transform.root != transform) { fuente = t.font; break; }
        }
        if (fuente == null) return;
        foreach (Boton b in botones)
        {
            TMP_Text etiqueta = b.rt.GetComponentInChildren<TMP_Text>(true);
            if (etiqueta != null) etiqueta.font = fuente;
        }
    }

    // Círculo (relleno o aro) suavizado, generado por código
    static Sprite CrearSpriteCirculo(bool aro)
    {
        const int n = 128;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[n * n];
        float r = n * 0.5f - 1f;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f));
            float a = Mathf.Clamp01(r - d);                       // borde exterior suave
            if (aro) a *= Mathf.Clamp01(d - (r - 10f));           // hueco interior
            else a *= Mathf.Lerp(1f, 0.75f, d / r);               // ligero degradado
            px[y * n + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
