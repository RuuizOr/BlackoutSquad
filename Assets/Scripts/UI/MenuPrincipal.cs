using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menú principal: JUGAR (lleva a elegir comando), OPCIONES y SALIR.
public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] GameObject panelPrincipal;
    [SerializeField] GameObject panelOpciones;
    [SerializeField] SeleccionPersonaje seleccion;

    [SerializeField] Button botonJugar;
    [SerializeField] Button botonOpciones;
    [SerializeField] Button botonSalir;
    [SerializeField] Button botonVolverOpciones;

    TMP_Text textoControles;
    TipoControl tipoMostrado;

    void Awake()
    {
        Time.timeScale = 1f;
        Transform t = panelPrincipal.transform.Find("Controles");
        if (t != null) textoControles = t.GetComponent<TMP_Text>();
        MostrarControles();
        botonJugar.onClick.AddListener(Jugar);
        botonOpciones.onClick.AddListener(() => Mostrar(panelOpciones, botonVolverOpciones));
        botonSalir.onClick.AddListener(Salir);
#if UNITY_WEBGL && !UNITY_EDITOR
        // En el navegador no se puede cerrar la pestaña desde el juego, así que SALIR no aparece.
        botonSalir.gameObject.SetActive(false);
        AjustarMarcoSinSalir();
#endif
        botonVolverOpciones.onClick.AddListener(() => Mostrar(panelPrincipal, botonOpciones));
        if (seleccion != null) seleccion.BotonVolver.onClick.AddListener(() => Mostrar(panelPrincipal, botonJugar));
    }

    void Start()
    {
        Mostrar(panelPrincipal, botonJugar);
        AudioManager am = AudioManager.Instancia;
        if (am != null) am.ReproducirMusica(am.musicaMenu, 1f);
    }

    void Update()
    {
        // ESC, Start o B / Círculo regresan al panel principal
        if ((ControlesJuego.PausaPresionada || ControlesJuego.CancelarPresionado) && !panelPrincipal.activeSelf)
            Mostrar(panelPrincipal, botonJugar);

        if (tipoMostrado != ControlesJuego.TipoActual) MostrarControles();
    }

    void MostrarControles()
    {
        tipoMostrado = ControlesJuego.TipoActual;
        if (textoControles != null) textoControles.text = ControlesJuego.TextoAyudaControles;
    }

    void Mostrar(GameObject panel, Button seleccionado)
    {
        panelPrincipal.SetActive(panel == panelPrincipal);
        panelOpciones.SetActive(panel == panelOpciones);
        if (seleccion != null) seleccion.gameObject.SetActive(panel == seleccion.gameObject);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(seleccionado.gameObject);
    }

    void Jugar()
    {
        if (seleccion != null) Mostrar(seleccion.gameObject, seleccion.TarjetaInicial);
        else if (GameManager.Instancia != null) GameManager.Instancia.NuevaPartida();
    }

    // Encoge el recuadro de los botones para que abarque solo JUGAR y OPCIONES
    // (conserva el mismo margen de arriba y de abajo que con tres botones).
    void AjustarMarcoSinSalir()
    {
        Transform t = panelPrincipal.transform.Find("MarcoBotones");
        RectTransform marco = t != null ? t as RectTransform : null;
        RectTransform rtSalir = botonSalir.transform as RectTransform;
        RectTransform rtOpciones = botonOpciones.transform as RectTransform;
        if (marco == null || rtSalir == null || rtOpciones == null) return;

        float hueco = rtSalir.anchoredPosition.y - rtOpciones.anchoredPosition.y; // negativo: SALIR está abajo
        Vector2 tamaño = marco.sizeDelta;
        tamaño.y += hueco;
        marco.sizeDelta = tamaño;
        marco.anchoredPosition += new Vector2(0f, -hueco * 0.5f);
    }

    void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
