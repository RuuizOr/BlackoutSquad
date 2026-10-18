using UnityEngine;

// Devuelve el objeto al pool (o lo destruye) después de unos segundos. Útil para partículas.
public class AutoDevolver : MonoBehaviour
{
    [SerializeField] float segundos = 1f;
    float tiempo;

    void OnEnable()
    {
        tiempo = segundos;
    }

    void Update()
    {
        tiempo -= Time.deltaTime;
        if (tiempo <= 0f) PoolObjetos.Devolver(gameObject);
    }
}
