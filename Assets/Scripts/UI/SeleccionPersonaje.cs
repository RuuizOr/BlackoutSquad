using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pantalla "ELIGE TU COMANDO" (estilo Metal Slug): el comando seleccionado corre en su tarjeta
// y al confirmarlo empieza la partida. Funciona con mouse, teclado y mando.
public class SeleccionPersonaje : MonoBehaviour
{
    const int FrameCorrer = 2, FramesCorrer = 4;

    [SerializeField] Button[] tarjetas;
    [SerializeField] Image[] retratos;
    [SerializeField] TMP_Text textoDescripcion;
    [SerializeField] Button botonVolver;
    [SerializeField] float fps = 10f;

    PersonajeDatos[] personajes;

    public Button BotonVolver => botonVolver;

    // Al entrar queda marcado el último comando elegido
    public Button TarjetaInicial
    {
        get
        {
            int i = GameManager.Instancia != null ? GameManager.Instancia.IndicePersonaje : 0;
            return tarjetas[Mathf.Clamp(i, 0, tarjetas.Length - 1)];
        }
    }

    void Awake()
    {
        personajes = GameManager.Instancia != null ? GameManager.Instancia.Personajes : null;
        for (int i = 0; i < tarjetas.Length; i++)
        {
            bool existe = personajes != null && i < personajes.Length;
            tarjetas[i].gameObject.SetActive(existe);
            if (!existe) continue;
            int indice = i;
            tarjetas[i].GetComponentInChildren<TMP_Text>().text = personajes[i].nombre;
            tarjetas[i].onClick.AddListener(() => Confirmar(indice));
        }
    }

    void Update()
    {
        if (personajes == null) return;
        GameObject actual = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        int cuadroCorrer = FrameCorrer + Mathf.FloorToInt(Time.unscaledTime * fps) % FramesCorrer;
        for (int i = 0; i < tarjetas.Length && i < personajes.Length; i++)
        {
            bool seleccionado = actual == tarjetas[i].gameObject;
            retratos[i].sprite = personajes[i].frames[seleccionado ? cuadroCorrer : 0];
            if (seleccionado) textoDescripcion.text = personajes[i].descripcion;
        }
    }

    void Confirmar(int indice)
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null) return;
        gm.ElegirPersonaje(indice);
        gm.NuevaPartida();
    }
}
