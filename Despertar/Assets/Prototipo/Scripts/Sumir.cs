using UnityEngine;

// Apaga uma linha aos poucos, em tempo real (o rastro do tiro do jogador não fica lento).
public class Sumir : MonoBehaviour
{
    LineRenderer linha;
    Color inicio, fim;
    float vida, total;

    public void Iniciar(LineRenderer lr, float duracao)
    {
        linha = lr;
        inicio = lr.startColor;
        fim = lr.endColor;
        vida = total = duracao;
    }

    void Update()
    {
        vida -= Time.unscaledDeltaTime;
        if (vida <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        float a = vida / total;
        linha.startColor = new Color(inicio.r, inicio.g, inicio.b, inicio.a * a);
        linha.endColor = new Color(fim.r, fim.g, fim.b, fim.a * a);
    }
}
