using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Sliders de volumen de música y efectos (se usan en el menú principal y en la pausa).
public class PanelOpciones : MonoBehaviour
{
    [SerializeField] Slider sliderMusica;
    [SerializeField] Slider sliderEfectos;
    [SerializeField] Color colorSeleccionado = new Color(1f, 0.68f, 0.18f);

    TMP_Text etiquetaMusica, etiquetaEfectos;
    Color colorEtiqueta;

    // Solo en celular: tamaño y transparencia de los controles táctiles, y vibración
    Slider sliderTamaño, sliderOpacidad;
    TMP_Text etiquetaTamaño, etiquetaOpacidad, textoVibracion;

    void Awake()
    {
        sliderMusica.onValueChanged.AddListener(CambiarMusica);
        sliderEfectos.onValueChanged.AddListener(CambiarEfectos);
        etiquetaMusica = BuscarEtiqueta(sliderMusica);
        etiquetaEfectos = BuscarEtiqueta(sliderEfectos);
        if (etiquetaMusica != null) colorEtiqueta = etiquetaMusica.color;
        if (ControlesJuego.EsMovil) AgregarOpcionesMovil();
    }

    // Copia las filas que ya existen (mismo estilo) y agranda la ventana para que quepan.
    // Se hace por código para no tener que modificar las escenas.
    void AgregarOpcionesMovil()
    {
        RectTransform ventana = transform as RectTransform;
        RectTransform filaMusica = sliderMusica.transform.parent as RectTransform;
        RectTransform filaEfectos = sliderEfectos.transform.parent as RectTransform;
        RectTransform volver = transform.Find("BotonVolver") as RectTransform;
        if (ventana == null || filaMusica == null || filaEfectos == null || volver == null) return;

        float paso = filaMusica.anchoredPosition.y - filaEfectos.anchoredPosition.y; // 120 en el diseño original
        float extra = paso * 3f;
        ventana.sizeDelta += new Vector2(0f, extra);
        filaMusica.anchoredPosition += new Vector2(0f, extra * 0.5f);
        filaEfectos.anchoredPosition += new Vector2(0f, extra * 0.5f);
        volver.anchoredPosition -= new Vector2(0f, extra * 0.5f);

        sliderTamaño = ClonarFila(filaEfectos, "FilaTamaño", "TAMAÑO DE CONTROLES", -paso, out etiquetaTamaño);
        sliderTamaño.minValue = AjustesJuego.TamañoMin;
        sliderTamaño.maxValue = AjustesJuego.TamañoMax;
        sliderTamaño.onValueChanged.AddListener(v => AjustesJuego.TamañoControles = v);

        sliderOpacidad = ClonarFila(filaEfectos, "FilaOpacidad", "OPACIDAD DE CONTROLES", -paso * 2f, out etiquetaOpacidad);
        sliderOpacidad.minValue = AjustesJuego.OpacidadMin;
        sliderOpacidad.maxValue = AjustesJuego.OpacidadMax;
        sliderOpacidad.onValueChanged.AddListener(v => AjustesJuego.OpacidadControles = v);

        RectTransform vib = Instantiate(volver, volver.parent);
        vib.name = "BotonVibracion";
        vib.anchoredPosition = new Vector2(volver.anchoredPosition.x, filaEfectos.anchoredPosition.y - paso * 3f + 15f);
        Button botonVib = vib.GetComponent<Button>();
        botonVib.onClick.RemoveAllListeners();
        botonVib.onClick.AddListener(() =>
        {
            AjustesJuego.Vibracion = !AjustesJuego.Vibracion;
            ActualizarTextoVibracion();
            Vibracion.Vibrar(80); // para sentir que quedó encendida
        });
        textoVibracion = vib.GetComponentInChildren<TMP_Text>(true);
    }

    Slider ClonarFila(RectTransform original, string nombre, string texto, float desplazamientoY, out TMP_Text etiqueta)
    {
        RectTransform fila = Instantiate(original, original.parent);
        fila.name = nombre;
        fila.anchoredPosition = original.anchoredPosition + new Vector2(0f, desplazamientoY);
        Slider s = fila.GetComponentInChildren<Slider>(true);
        s.onValueChanged.RemoveAllListeners();
        etiqueta = BuscarEtiqueta(s);
        if (etiqueta != null) etiqueta.text = texto;
        return s;
    }

    // Con las opciones extra la ventana es más alta: en pantallas muy anchas se encoge para caber completa
    void AjustarAlPantalla()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform ventana = transform as RectTransform;
        if (canvas == null || ventana == null) return;
        Canvas.ForceUpdateCanvases();
        float disponible = ((RectTransform)canvas.rootCanvas.transform).rect.height * 0.94f;
        float escala = Mathf.Min(1f, disponible / ventana.rect.height);
        ventana.localScale = new Vector3(escala, escala, 1f);
    }

    void ActualizarTextoVibracion()
    {
        if (textoVibracion != null) textoVibracion.text = AjustesJuego.Vibracion ? "VIBRACIÓN: SÍ" : "VIBRACIÓN: NO";
    }

    static TMP_Text BuscarEtiqueta(Slider s)
    {
        Transform t = s.transform.parent.Find("Etiqueta");
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    void OnEnable()
    {
        if (sliderTamaño != null) AjustarAlPantalla();
        if (sliderTamaño != null) sliderTamaño.SetValueWithoutNotify(AjustesJuego.TamañoControles);
        if (sliderOpacidad != null) sliderOpacidad.SetValueWithoutNotify(AjustesJuego.OpacidadControles);
        ActualizarTextoVibracion();

        AudioManager am = AudioManager.Instancia;
        if (am == null) return;
        sliderMusica.SetValueWithoutNotify(am.VolumenMusica);
        sliderEfectos.SetValueWithoutNotify(am.VolumenEfectos);
    }

    void OnDisable()
    {
        if (AudioManager.Instancia != null) AudioManager.Instancia.GuardarVolumenes();
        AjustesJuego.Guardar();
    }

    // Con mando hay que ver qué slider se está moviendo: su etiqueta se pinta de naranja
    void Update()
    {
        GameObject actual = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (etiquetaMusica != null) etiquetaMusica.color = actual == sliderMusica.gameObject ? colorSeleccionado : colorEtiqueta;
        if (etiquetaEfectos != null) etiquetaEfectos.color = actual == sliderEfectos.gameObject ? colorSeleccionado : colorEtiqueta;
        if (etiquetaTamaño != null) etiquetaTamaño.color = actual == sliderTamaño.gameObject ? colorSeleccionado : colorEtiqueta;
        if (etiquetaOpacidad != null) etiquetaOpacidad.color = actual == sliderOpacidad.gameObject ? colorSeleccionado : colorEtiqueta;
    }

    void CambiarMusica(float v)
    {
        if (AudioManager.Instancia != null) AudioManager.Instancia.CambiarVolumenMusica(v);
    }

    void CambiarEfectos(float v)
    {
        if (AudioManager.Instancia == null) return;
        AudioManager.Instancia.CambiarVolumenEfectos(v);
        AudioManager.SonarUI(Sfx.UiHover); // para escuchar el volumen nuevo
    }
}
