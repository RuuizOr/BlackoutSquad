using UnityEngine;

// Bandera de checkpoint: se activa al pasar y guarda el punto de reaparición.
public class Checkpoint : MonoBehaviour
{
    [SerializeField] SpriteRenderer render;
    [SerializeField] Sprite spriteInactiva;
    [SerializeField] Sprite spriteActiva;

    bool activado;

    void Start()
    {
        if (render != null) render.sprite = spriteInactiva;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (activado || otro.GetComponentInParent<JugadorMovimiento>() == null) return;
        activado = true;
        if (render != null) render.sprite = spriteActiva;
        if (ControladorNivel.Instancia != null) ControladorNivel.Instancia.ActivarCheckpoint(transform.position + Vector3.up * 0.5f);
        AudioManager.Sonar(Sfx.Checkpoint);
        Efectos.Popup(transform.position + Vector3.up * 2.5f, "CHECKPOINT", new Color(1f, 0.6f, 0.15f));
    }
}
