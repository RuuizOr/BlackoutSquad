using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD del nivel: vida, vidas, arma y munición, granadas, puntos, barra del jefe,
// tarjeta de título, avisos en pantalla y briefing de misión.
public class HUD : MonoBehaviour
{
    public static HUD Instancia { get; private set; }

    [Header("Jugador")]
    [SerializeField] Image barraVida;
    [SerializeField] TMP_Text textoVidas;
    [SerializeField] Image iconoArma;
    [SerializeField] TMP_Text textoArma;
    [SerializeField] TMP_Text textoMunicion;
    [SerializeField] TMP_Text textoGranadas;
    [SerializeField] TMP_Text textoPuntos;
    [SerializeField] Color colorVidaAlta = new Color(0.45f, 0.8f, 0.3f);
    [SerializeField] Color colorVidaBaja = new Color(0.95f, 0.3f, 0.2f);

    [Header("Jefe")]
    [SerializeField] GameObject panelJefe;
    [SerializeField] Image barraJefe;
    [SerializeField] TMP_Text nombreJefe;

    [Header("Mensajes")]
    [SerializeField] CanvasGroup tarjetaTitulo;
    [SerializeField] TMP_Text textoTitulo;
    [SerializeField] TMP_Text textoSubtitulo;
    [SerializeField] TMP_Text textoAviso;
    [SerializeField] GameObject panelBriefing;
    [SerializeField] TMP_Text textoBriefing;

    VidaJugador vida;
    JugadorDisparo disparo;
    SaludEnemigo jefe;
    float tiempoAviso;
    Action alTerminarBriefing;
    float puntosMostrados;

    void Awake()
    {
        Instancia = this;
        panelJefe.SetActive(false);
        panelBriefing.SetActive(false);
        tarjetaTitulo.alpha = 0f;
        textoAviso.gameObject.SetActive(false);
    }

    void Start()
    {
        if (JugadorMovimiento.Instancia != null)
        {
            vida = JugadorMovimiento.Instancia.GetComponent<VidaJugador>();
            disparo = JugadorMovimiento.Instancia.GetComponent<JugadorDisparo>();
        }
    }

    void Update()
    {
        ActualizarJugador();
        ActualizarJefe();

        if (tiempoAviso > 0f)
        {
            tiempoAviso -= Time.unscaledDeltaTime;
            float pulso = 1f + Mathf.Sin(Time.unscaledTime * 10f) * 0.04f;
            textoAviso.transform.localScale = Vector3.one * pulso;
            if (tiempoAviso <= 0f) textoAviso.gameObject.SetActive(false);
        }

        if (panelBriefing.activeSelf && ControlesJuego.AceptarPresionado)
        {
            panelBriefing.SetActive(false);
            AudioManager.SonarUI(Sfx.UiClick);
            alTerminarBriefing?.Invoke();
        }
    }

    void ActualizarJugador()
    {
        GameManager gm = GameManager.Instancia;
        if (vida != null)
        {
            float t = vida.Vida / (float)vida.VidaMaxima;
            barraVida.fillAmount = Mathf.MoveTowards(barraVida.fillAmount, t, Time.unscaledDeltaTime * 2f);
            barraVida.color = Color.Lerp(colorVidaBaja, colorVidaAlta, t * 1.5f);
        }
        if (gm != null)
        {
            textoVidas.text = "x" + gm.Vidas;
            // los puntos "cuentan" hacia arriba
            puntosMostrados = Mathf.MoveTowards(puntosMostrados, gm.Puntos, Mathf.Max(50f, (gm.Puntos - puntosMostrados) * 8f) * Time.unscaledDeltaTime);
            textoPuntos.text = Mathf.RoundToInt(puntosMostrados).ToString("D7");
        }
        if (disparo != null)
        {
            ArmaDatos arma = disparo.ArmaActual;
            if (arma != null)
            {
                iconoArma.sprite = arma.icono;
                textoArma.text = arma.nombre;
                textoMunicion.text = arma.EsInfinita ? "∞" : disparo.Municion.ToString();
            }
            textoGranadas.text = "x" + disparo.Granadas;
        }
    }

    void ActualizarJefe()
    {
        if (jefe == null) { if (panelJefe.activeSelf) panelJefe.SetActive(false); return; }
        barraJefe.fillAmount = Mathf.MoveTowards(barraJefe.fillAmount, jefe.Vida / (float)jefe.VidaMaxima, Time.deltaTime * 1.5f);
        if (jefe.Muerto && barraJefe.fillAmount <= 0f) OcultarJefe();
    }

    public void MostrarJefe(SaludEnemigo salud, string nombre)
    {
        jefe = salud;
        nombreJefe.text = nombre;
        barraJefe.fillAmount = 0f;
        panelJefe.SetActive(true);
    }

    public void OcultarJefe()
    {
        jefe = null;
        panelJefe.SetActive(false);
    }

    public void MostrarAviso(string texto, float duracion)
    {
        textoAviso.text = texto;
        textoAviso.gameObject.SetActive(true);
        tiempoAviso = duracion;
    }

    public void MostrarTarjeta(string titulo, string subtitulo)
    {
        textoTitulo.text = titulo;
        textoSubtitulo.text = subtitulo;
        StopCoroutine(nameof(RutinaTarjeta));
        StartCoroutine(nameof(RutinaTarjeta));
    }

    IEnumerator RutinaTarjeta()
    {
        float t = 0f;
        while (t < 0.5f) { t += Time.unscaledDeltaTime; tarjetaTitulo.alpha = t / 0.5f; yield return null; }
        yield return new WaitForSecondsRealtime(2.2f);
        t = 0f;
        while (t < 0.6f) { t += Time.unscaledDeltaTime; tarjetaTitulo.alpha = 1f - t / 0.6f; yield return null; }
        tarjetaTitulo.alpha = 0f;
    }

    public void MostrarBriefing(string texto, Action alTerminar)
    {
        textoBriefing.text = texto;
        alTerminarBriefing = alTerminar;
        panelBriefing.SetActive(true);
    }
}
