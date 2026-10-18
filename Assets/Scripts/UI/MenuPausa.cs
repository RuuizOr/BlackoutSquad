using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menú de pausa: ESC (o Start) detiene el juego (Time.timeScale = 0) y muestra una ventana
// centrada sobre el juego oscurecido. ESC de nuevo reanuda.
public class MenuPausa : MonoBehaviour
{
    [SerializeField] GameObject raizPausa;      // fondo oscuro + ventana
    [SerializeField] GameObject ventanaPrincipal;
    [SerializeField] GameObject ventanaOpciones;
    [SerializeField] Button botonReanudar;
    [SerializeField] Button botonReiniciar;
    [SerializeField] Button botonOpciones;
    [SerializeField] Button botonSalir;
    [SerializeField] Button botonVolverOpciones;

    public static bool Pausado { get; private set; }

    void Awake()
    {
        Pausado = false;
        raizPausa.SetActive(false);
        botonReanudar.onClick.AddListener(Reanudar);
        botonReiniciar.onClick.AddListener(Reiniciar);
        botonOpciones.onClick.AddListener(AbrirOpciones);
        botonSalir.onClick.AddListener(SalirAlMenu);
        botonVolverOpciones.onClick.AddListener(CerrarOpciones);
    }

    void Update()
    {
        if (!ControlesJuego.PausaPresionada) return;
        if (Pausado)
        {
            if (ventanaOpciones.activeSelf) CerrarOpciones();
            else Reanudar();
        }
        else if (ControladorNivel.Instancia == null || ControladorNivel.Instancia.PuedePausar)
        {
            Pausar();
        }
    }

    void Pausar()
    {
        Pausado = true;
        if (GameManager.Instancia != null) GameManager.Instancia.Pausado = true;
        Time.timeScale = 0f;
        raizPausa.SetActive(true);
        ventanaPrincipal.SetActive(true);
        ventanaOpciones.SetActive(false);
        Seleccionar(botonReanudar.gameObject);
        AudioManager.SonarUI(Sfx.Pausa);
        if (AudioManager.Instancia != null) AudioManager.Instancia.AtenuarPorPausa(true);
    }

    public void Reanudar()
    {
        Pausado = false;
        if (GameManager.Instancia != null) GameManager.Instancia.Pausado = false;
        Time.timeScale = 1f;
        raizPausa.SetActive(false);
        AudioManager.SonarUI(Sfx.Reanudar);
        if (AudioManager.Instancia != null) AudioManager.Instancia.AtenuarPorPausa(false);
    }

    void Reiniciar()
    {
        Pausado = false;
        if (AudioManager.Instancia != null) AudioManager.Instancia.AtenuarPorPausa(false);
        if (GameManager.Instancia != null) GameManager.Instancia.ReiniciarNivel();
    }

    void SalirAlMenu()
    {
        Pausado = false;
        if (AudioManager.Instancia != null) AudioManager.Instancia.AtenuarPorPausa(false);
        if (GameManager.Instancia != null) GameManager.Instancia.IrAlMenu();
    }

    void AbrirOpciones()
    {
        ventanaPrincipal.SetActive(false);
        ventanaOpciones.SetActive(true);
        Seleccionar(botonVolverOpciones.gameObject);
    }

    void CerrarOpciones()
    {
        ventanaOpciones.SetActive(false);
        ventanaPrincipal.SetActive(true);
        Seleccionar(botonOpciones.gameObject);
    }

    static void Seleccionar(GameObject go)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }
}
