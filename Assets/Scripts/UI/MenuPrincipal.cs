using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menú principal: JUGAR, OPCIONES y SALIR.
public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] GameObject panelPrincipal;
    [SerializeField] GameObject panelOpciones;

    [SerializeField] Button botonJugar;
    [SerializeField] Button botonOpciones;
    [SerializeField] Button botonSalir;
    [SerializeField] Button botonVolverOpciones;

    void Awake()
    {
        Time.timeScale = 1f;
        botonJugar.onClick.AddListener(Jugar);
        botonOpciones.onClick.AddListener(() => Mostrar(panelOpciones, botonVolverOpciones));
        botonSalir.onClick.AddListener(Salir);
        botonVolverOpciones.onClick.AddListener(() => Mostrar(panelPrincipal, botonOpciones));
    }

    void Start()
    {
        Mostrar(panelPrincipal, botonJugar);
        AudioManager am = AudioManager.Instancia;
        if (am != null) am.ReproducirMusica(am.musicaMenu, 1f);
    }

    void Update()
    {
        // ESC regresa al panel principal
        if (ControlesJuego.PausaPresionada && !panelPrincipal.activeSelf)
            Mostrar(panelPrincipal, botonJugar);
    }

    void Mostrar(GameObject panel, Button seleccionado)
    {
        panelPrincipal.SetActive(panel == panelPrincipal);
        panelOpciones.SetActive(panel == panelOpciones);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(seleccionado.gameObject);
    }

    void Jugar()
    {
        if (GameManager.Instancia != null) GameManager.Instancia.NuevaPartida();
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
