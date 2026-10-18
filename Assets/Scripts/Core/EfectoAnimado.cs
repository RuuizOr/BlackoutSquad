using UnityEngine;

// Efecto de un solo uso (explosión, fogonazo, humo): reproduce sus frames y vuelve al pool.
public class EfectoAnimado : MonoBehaviour
{
    [SerializeField] SpriteRenderer render;
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 20f;
    [SerializeField] bool rotacionAleatoria;

    float tiempo;

    void OnEnable()
    {
        tiempo = 0f;
        if (frames != null && frames.Length > 0) render.sprite = frames[0];
        if (rotacionAleatoria) transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0, 4) * 90f);
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        int indice = Mathf.FloorToInt(tiempo * fps);
        if (frames == null || indice >= frames.Length)
        {
            PoolObjetos.Devolver(gameObject);
            return;
        }
        render.sprite = frames[indice];
    }
}
