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

    void Awake()
    {
        sliderMusica.onValueChanged.AddListener(CambiarMusica);
        sliderEfectos.onValueChanged.AddListener(CambiarEfectos);
        etiquetaMusica = BuscarEtiqueta(sliderMusica);
        etiquetaEfectos = BuscarEtiqueta(sliderEfectos);
        if (etiquetaMusica != null) colorEtiqueta = etiquetaMusica.color;
    }

    static TMP_Text BuscarEtiqueta(Slider s)
    {
        Transform t = s.transform.parent.Find("Etiqueta");
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    void OnEnable()
    {
        AudioManager am = AudioManager.Instancia;
        if (am == null) return;
        sliderMusica.SetValueWithoutNotify(am.VolumenMusica);
        sliderEfectos.SetValueWithoutNotify(am.VolumenEfectos);
    }

    void OnDisable()
    {
        if (AudioManager.Instancia != null) AudioManager.Instancia.GuardarVolumenes();
    }

    // Con mando hay que ver qué slider se está moviendo: su etiqueta se pinta de naranja
    void Update()
    {
        GameObject actual = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (etiquetaMusica != null) etiquetaMusica.color = actual == sliderMusica.gameObject ? colorSeleccionado : colorEtiqueta;
        if (etiquetaEfectos != null) etiquetaEfectos.color = actual == sliderEfectos.gameObject ? colorSeleccionado : colorEtiqueta;
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
