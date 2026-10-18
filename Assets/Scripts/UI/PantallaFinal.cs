using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ventana de GAME OVER o VICTORIA sobre el juego oscurecido, con la puntuación final.
public class PantallaFinal : MonoBehaviour
{
    public static PantallaFinal Instancia { get; private set; }

    [SerializeField] GameObject raiz;
    [SerializeField] TMP_Text textoTitulo;
    [SerializeField] TMP_Text textoPuntuacion;
    [SerializeField] TMP_Text textoHistoria;
    [SerializeField] Button botonReiniciar;
    [SerializeField] Button botonMenu;
    [SerializeField] Color colorGameOver = new Color(0.95f, 0.3f, 0.2f);
    [SerializeField] Color colorVictoria = new Color(1f, 0.75f, 0.2f);

    void Awake()
    {
        Instancia = this;
        raiz.SetActive(false);
        botonReiniciar.onClick.AddListener(() => { if (GameManager.Instancia != null) GameManager.Instancia.ReiniciarNivel(); });
        botonMenu.onClick.AddListener(() => { if (GameManager.Instancia != null) GameManager.Instancia.IrAlMenu(); });
    }

    public void MostrarGameOver(int puntos)
    {
        Mostrar("GAME OVER", colorGameOver, puntos, "Tu escuadrón ha caído... pero la misión no ha terminado.");
        botonReiniciar.gameObject.SetActive(true);
        Seleccionar(botonReiniciar.gameObject);
    }

    public void MostrarVictoria(int puntos, string historia)
    {
        Mostrar("VICTORIA", colorVictoria, puntos, historia);
        botonReiniciar.gameObject.SetActive(false);
        Seleccionar(botonMenu.gameObject);
    }

    void Mostrar(string titulo, Color color, int puntos, string historia)
    {
        if (JugadorMovimiento.Instancia != null) JugadorMovimiento.Instancia.ControlBloqueado = true;
        Time.timeScale = 0f;
        if (GameManager.Instancia != null) GameManager.Instancia.Pausado = true;
        raiz.SetActive(true);
        textoTitulo.text = titulo;
        textoTitulo.color = color;
        textoPuntuacion.text = "PUNTUACIÓN: " + puntos.ToString("D7");
        textoHistoria.text = historia;
    }

    static void Seleccionar(GameObject go)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }
}
