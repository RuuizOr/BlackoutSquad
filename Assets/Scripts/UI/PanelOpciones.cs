using UnityEngine;
using UnityEngine.UI;

// Sliders de volumen de música y efectos (se usan en el menú principal y en la pausa).
public class PanelOpciones : MonoBehaviour
{
    [SerializeField] Slider sliderMusica;
    [SerializeField] Slider sliderEfectos;

    void Awake()
    {
        sliderMusica.onValueChanged.AddListener(CambiarMusica);
        sliderEfectos.onValueChanged.AddListener(CambiarEfectos);
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
