using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sistema visual global (lo crea Arranque y persiste entre escenas):
// - Destello rojo en las orillas de la pantalla cuando el jugador recibe daño.
// - Al cargar un nivel crea su AmbienteNivel (luces, post-procesado y clima).
// Todo se crea por código: no modifica escenas ni prefabs, así que "Construir juego" no lo borra.
public class GestorVisual : MonoBehaviour
{
    public static GestorVisual Instancia { get; private set; }

    Image destello;
    float alphaDestello;

    public static void Crear()
    {
        if (Instancia != null) return;
        GameObject go = new GameObject("GestorVisual");
        DontDestroyOnLoad(go);
        go.AddComponent<GestorVisual>();
    }

    void Awake()
    {
        Instancia = this;
        CrearDestello();
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        if (Instancia == this) Instancia = null;
    }

    void Start()
    {
        // La primera escena ya se cargó antes de que existiera este objeto
        PrepararEscena();
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        alphaDestello = 0f;
        PrepararEscena();
    }

    static void PrepararEscena()
    {
        if (ControladorNivel.Instancia != null && AmbienteNivel.Instancia == null)
            AmbienteNivel.Crear(ControladorNivel.Instancia.NumeroNivel);
    }

    // ---------- Destello de daño ----------
    public static void DestelloDaño()
    {
        if (Instancia != null) Instancia.alphaDestello = 0.75f;
    }

    void Update()
    {
        if (alphaDestello <= 0f) { if (destello.enabled) destello.enabled = false; return; }
        destello.enabled = true;
        destello.color = new Color(1f, 1f, 1f, alphaDestello);
        alphaDestello -= Time.unscaledDeltaTime * 2.2f;
    }

    void CrearDestello()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9; // debajo del HUD (10): no tapa la vida ni los puntos

        GameObject go = new GameObject("DestelloDaño", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        destello = go.AddComponent<Image>();
        destello.sprite = CrearViñetaRoja();
        destello.raycastTarget = false;
        destello.enabled = false;
    }

    // Rojo intenso en las orillas y transparente al centro
    static Sprite CrearViñetaRoja()
    {
        const int n = 128;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
            float d = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.45f) / 0.85f);
            px[y * n + x] = new Color(0.85f, 0.05f, 0.03f, d * d * 0.9f + 0.08f);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
