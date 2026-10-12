using UnityEngine;

// Pedacinho que voa, encolhe e some (faíscas, código). Usa o tempo do jogo: em câmera lenta, fica lento.
public class Fragmento : MonoBehaviour
{
    Vector3 velocidade, escalaInicial;
    float vida, total, gravidade;

    public void Iniciar(Vector3 vel, float duracao, float g)
    {
        velocidade = vel;
        vida = total = duracao;
        gravidade = g;
        escalaInicial = transform.localScale;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        vida -= dt;
        if (vida <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        velocidade += Vector3.down * gravidade * dt;
        transform.position += velocidade * dt;
        transform.localScale = escalaInicial * (vida / total);
    }
}
