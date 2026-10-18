using UnityEngine;

// Alambre de púas: hace daño por contacto cada cierto tiempo.
public class AlambrePuas : MonoBehaviour
{
    [SerializeField] int daño = 15;
    [SerializeField] float intervalo = 0.6f;

    float contador;

    void OnTriggerStay2D(Collider2D otro)
    {
        contador -= Time.deltaTime;
        if (contador > 0f) return;
        VidaJugador v = otro.GetComponentInParent<VidaJugador>();
        if (v == null) return;
        // empuja hacia afuera del alambre
        float lado = Mathf.Sign(otro.transform.position.x - transform.position.x);
        if (v.RecibirDaño(daño, new Vector2(lado, 0f))) contador = intervalo;
    }
}
