using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Fundido a negro entre escenas. Vive dentro del prefab "SistemasJuego" (persistente).
public class Transicion : MonoBehaviour
{
    public static Transicion Instancia { get; private set; }

    [SerializeField] CanvasGroup grupo;
    [SerializeField] float duracionFundido = 0.45f;

    bool cargando;

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        grupo.alpha = 1f;
        grupo.blocksRaycasts = false;
        StartCoroutine(Fundir(1f, 0f));
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargar;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargar;
    }

    void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (!cargando) StartCoroutine(Fundir(1f, 0f));
    }

    public static void Cargar(string escena)
    {
        if (Instancia == null) { Time.timeScale = 1f; SceneManager.LoadScene(escena); return; }
        if (Instancia.cargando) return;
        Instancia.StartCoroutine(Instancia.RutinaCargar(escena));
    }

    IEnumerator RutinaCargar(string escena)
    {
        cargando = true;
        grupo.blocksRaycasts = true;

        // Al entrar a una misión se muestra la pantalla de carga con un consejo (aparece junto con el fundido)
        bool esNivel = escena.StartsWith("Nivel");
        if (esNivel) PantallaCarga.Mostrar(grupo.transform, escena);

        yield return Fundir(grupo.alpha, 1f);
        Time.timeScale = 1f;
        if (GameManager.Instancia != null) GameManager.Instancia.Pausado = false;
        AsyncOperation op = SceneManager.LoadSceneAsync(escena);
        if (esNivel)
        {
            // Tiempo mínimo para alcanzar a leer el consejo
            op.allowSceneActivation = false;
            float inicio = Time.unscaledTime;
            while (op.progress < 0.9f || Time.unscaledTime - inicio < PantallaCarga.TiempoMinimo)
            {
                PantallaCarga.Progreso(Mathf.Min(op.progress / 0.9f, (Time.unscaledTime - inicio) / PantallaCarga.TiempoMinimo));
                yield return null;
            }
            PantallaCarga.Progreso(1f);
            op.allowSceneActivation = true;
        }
        while (!op.isDone) yield return null;
        yield return null;
        cargando = false;
        yield return Fundir(1f, 0f);
        PantallaCarga.Ocultar();
        grupo.blocksRaycasts = false;
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        float t = 0f;
        while (t < duracionFundido)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracionFundido);
            yield return null;
        }
        grupo.alpha = hasta;
    }
}
