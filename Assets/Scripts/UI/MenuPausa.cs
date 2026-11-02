using TMPro;
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
    public static MenuPausa Instancia { get; private set; }

    TMP_Text titulo;

    void Awake()
    {
        Instancia = this;
        Pausado = false;
        raizPausa.SetActive(false);
        Transform t = ventanaPrincipal.transform.Find("Titulo");
        if (t != null) titulo = t.GetComponent<TMP_Text>();
        botonReanudar.onClick.AddListener(Reanudar);
        botonReiniciar.onClick.AddListener(Reiniciar);
        botonOpciones.onClick.AddListener(AbrirOpciones);
        botonSalir.onClick.AddListener(SalirAlMenu);
        botonVolverOpciones.onClick.AddListener(CerrarOpciones);
    }

    void Update()
    {
        // B / Círculo solo sirve para regresar: en juego es la granada
        if (ControlesJuego.PausaPresionada || (Pausado && ControlesJuego.CancelarPresionado))
        {
            if (Pausado)
            {
                if (ventanaOpciones.activeSelf) CerrarOpciones();
                else Reanudar();
            }
            else if (PuedePausar) Pausar();
        }
    }

    static bool PuedePausar => ControladorNivel.Instancia == null || ControladorNivel.Instancia.PuedePausar;

    // Se desconectó el único mando: pausa avisando el motivo
    public void PausarSinMando()
    {
        if (Pausado || !PuedePausar) return;
        Pausar();
        if (titulo != null) titulo.text = "SIN MANDO";
    }

    void Pausar()
    {
        Pausado = true;
        if (GameManager.Instancia != null) GameManager.Instancia.Pausado = true;
        // Los botones del menú no deben llegar al jugador (A saltaría, B lanzaría una granada...)
        if (JugadorMovimiento.Instancia != null) JugadorMovimiento.Instancia.ControlBloqueado = true;
        if (titulo != null) titulo.text = "PAUSA";
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
        if (JugadorMovimiento.Instancia != null) JugadorMovimiento.Instancia.ControlBloqueado = false;
        ControlesJuego.IgnorarAccionesEsteFrame(); // el botón que eligió REANUDAR no debe hacer saltar
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
