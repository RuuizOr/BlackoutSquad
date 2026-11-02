using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Botón con animación al pasar el mouse o seleccionarlo con gamepad (escala y color)
// y sonidos de hover y click. Funciona aunque el juego esté en pausa (tiempo sin escala).
public class BotonAnimado : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] float escalaHover = 1.08f;
    [SerializeField] TMP_Text texto;
    [SerializeField] Color colorNormal = new Color(0.93f, 0.9f, 0.78f);
    [SerializeField] Color colorHover = new Color(1f, 0.7f, 0.2f);

    bool resaltado;
    Vector3 escalaBase;

    void Awake()
    {
        escalaBase = transform.localScale;
        if (texto == null) texto = GetComponentInChildren<TMP_Text>();
    }

    void OnEnable()
    {
        // Si sigue seleccionado (al reabrir la pausa) no llega OnSelect: hay que resaltarlo aquí
        resaltado = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        transform.localScale = escalaBase;
        if (texto != null) texto.color = colorNormal;
    }

    void Update()
    {
        Vector3 objetivo = escalaBase * (resaltado ? escalaHover : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, objetivo, Time.unscaledDeltaTime * 14f);
        if (texto != null) texto.color = Color.Lerp(texto.color, resaltado ? colorHover : colorNormal, Time.unscaledDeltaTime * 14f);
    }

    void Resaltar(bool valor)
    {
        if (valor && !resaltado) AudioManager.SonarUI(Sfx.UiHover);
        resaltado = valor;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        Resaltar(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData e) { Resaltar(false); }
    public void OnSelect(BaseEventData e) { Resaltar(true); }
    public void OnDeselect(BaseEventData e) { Resaltar(false); }
    public void OnPointerClick(PointerEventData e) { AudioManager.SonarUI(Sfx.UiClick); }
    public void OnSubmit(BaseEventData e) { AudioManager.SonarUI(Sfx.UiClick); }
}
