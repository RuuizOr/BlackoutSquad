using UnityEngine;
using UnityEngine.UI;

// Anima una Image de la interfaz con una lista de sprites (por ejemplo, los soldados del menú).
[RequireComponent(typeof(Image))]
public class AnimadorImagenUI : MonoBehaviour
{
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 10f;
    [SerializeField] float desfase = 0f; // segundos, para que varios soldados no pisen al mismo tiempo

    Image imagen;
    float tiempo;

    void Awake()
    {
        imagen = GetComponent<Image>();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        tiempo += Time.unscaledDeltaTime;
        imagen.sprite = frames[Mathf.FloorToInt((tiempo + desfase) * fps) % frames.Length];
    }
}
