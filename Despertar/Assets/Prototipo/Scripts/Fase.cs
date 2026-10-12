using System.Collections;
using UnityEngine;

// Fase linear: cada sala tem seus Agentes, que surgem quando o jogador entra nela.
// Com todos eliminados, o elevador abre; entrar nele encerra a simulação.
// Preenchido pelo montador (Despertar > Montar escritório).
public class Fase : MonoBehaviour
{
    public static Fase I;

    [System.Serializable]
    public class Sala
    {
        public string nome;
        public Bounds area;
        public Vector3[] pontos;
        public TipoArma[] armas;
        [System.NonSerialized] public bool ativa;
    }

    public Sala[] salas;
    public Bounds saida; // dentro do elevador
    public Transform portaEsquerda, portaDireita; // folhas da porta do elevador
    public float esperaInicial = 1.5f;

    public bool Aberto { get; private set; }
    public bool Venceu { get; private set; }

    public string Status => Venceu ? "" : Aberto ? "o elevador abriu" : "agentes: " + Inimigo.Vivos.Count + "   objetivo: o elevador";

    float inicio, proximoAviso;

    void Awake()
    {
        I = this;
        inicio = Time.time;
    }

    void Update()
    {
        var j = Jogador.I;
        if (j == null || j.Morto || Venceu)
            return;
        Vector3 p = j.transform.position + Vector3.up * 0.5f;

        if (Time.time >= inicio + esperaInicial)
            foreach (var s in salas)
                if (!s.ativa && s.area.Contains(p))
                {
                    s.ativa = true;
                    for (int i = 0; i < s.pontos.Length; i++)
                        Inimigo.Criar(s.pontos[i], s.armas[i]);
                }

        if (!Aberto && Inimigo.Vivos.Count == 0 && System.Array.TrueForAll(salas, s => s.ativa))
        {
            Aberto = true;
            StartCoroutine(AbrirElevador());
            j.MostrarAviso("o elevador abriu", new Color(0.6f, 1f, 0.7f), 2f);
        }

        if (Aberto && saida.Contains(p))
        {
            Venceu = true;
            j.MostrarAviso("SIMULAÇÃO LIMPA\nR para jogar de novo", new Color(0.5f, 1f, 0.6f), 9999f);
        }
        else if (!Aberto && (p - saida.center).sqrMagnitude < 16f && Time.unscaledTime >= proximoAviso)
        {
            proximoAviso = Time.unscaledTime + 3f;
            j.MostrarAviso("o elevador só abre sem Agentes na simulação", new Color(1f, 0.6f, 0.5f), 1.5f);
        }
    }

    IEnumerator AbrirElevador()
    {
        if (portaEsquerda == null || portaDireita == null)
            yield break;
        Vector3 e0 = portaEsquerda.localPosition, d0 = portaDireita.localPosition;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            portaEsquerda.localPosition = e0 + Vector3.right * 0.72f * k;
            portaDireita.localPosition = d0 - Vector3.right * 0.72f * k;
            yield return null;
        }
        portaEsquerda.localPosition = e0 + Vector3.right * 0.72f;
        portaDireita.localPosition = d0 - Vector3.right * 0.72f;
    }
}
