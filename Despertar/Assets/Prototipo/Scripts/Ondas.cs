using System.Collections.Generic;
using UnityEngine;

// Ondas de Agentes. Quando uma acaba, vem a próxima; no fim, a simulação está limpa.
public class Ondas : MonoBehaviour
{
    public static Ondas I;

    public Transform[] pontos;

    static readonly TipoArma[][] Lista =
    {
        new[] { TipoArma.Pistola, TipoArma.Pistola },
        new[] { TipoArma.Pistola, TipoArma.Metralhadora, TipoArma.Pistola },
        new[] { TipoArma.Metralhadora, TipoArma.Escopeta, TipoArma.Pistola, TipoArma.Metralhadora },
        new[] { TipoArma.Escopeta, TipoArma.Metralhadora, TipoArma.Metralhadora, TipoArma.Pistola, TipoArma.Escopeta },
    };

    public int Onda { get; private set; }
    public int Total => Lista.Length;
    public bool Venceu { get; private set; }

    bool agendada;
    float proximaEm;

    void Awake() => I = this;

    void Start() => Agendar(1.5f);

    void Agendar(float daqui)
    {
        agendada = true;
        proximaEm = Time.time + daqui;
        Jogador.I?.MostrarAviso("onda " + (Onda + 1) + " de " + Total, new Color(0.6f, 1f, 0.7f), 1.8f);
    }

    void Update()
    {
        var j = Jogador.I;
        if (j == null || j.Morto || Venceu)
            return;

        if (agendada)
        {
            if (Time.time >= proximaEm)
            {
                agendada = false;
                Gerar(Lista[Onda]);
                Onda++;
            }
        }
        else if (Inimigo.Vivos.Count == 0)
        {
            if (Onda >= Total)
            {
                Venceu = true;
                j.MostrarAviso("SIMULAÇÃO LIMPA\nR para jogar de novo", new Color(0.5f, 1f, 0.6f), 9999f);
            }
            else
            {
                Agendar(2f);
            }
        }
    }

    void Gerar(TipoArma[] tipos)
    {
        var ordem = new List<Transform>(pontos);
        for (int i = ordem.Count - 1; i > 0; i--)
        {
            int k = Random.Range(0, i + 1);
            (ordem[i], ordem[k]) = (ordem[k], ordem[i]);
        }
        for (int i = 0; i < tipos.Length; i++)
            Inimigo.Criar(ordem[i % ordem.Count].position, tipos[i]);
    }
}
