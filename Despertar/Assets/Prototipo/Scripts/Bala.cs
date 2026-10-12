using System.Collections.Generic;
using UnityEngine;

// Bala inimiga: devagar o bastante para ser lida em câmera lenta e aparada.
// Muda de cor conforme o tempo até o impacto: vermelha (longe), branca (dá para aparar), dourada (perfeito).
public class Bala : MonoBehaviour
{
    public static readonly List<Bala> Ativas = new List<Bala>();

    static MaterialPropertyBlock mpb;
    static readonly int CorID = Shader.PropertyToID("_BaseColor");
    static readonly Color CorLonge = new Color(4f, 0.35f, 0.25f);
    static readonly Color CorJanela = new Color(4f, 4f, 4f);
    static readonly Color CorPerfeita = new Color(5f, 3.6f, 0.8f);
    static readonly Color CorDesviada = new Color(0.4f, 3f, 5f);
    const float Raio = 0.1f;

    public Vector3 Velocidade;
    public Inimigo Dono;
    public bool Desviada { get; private set; }

    float vida = 5f;
    bool esquivada;
    Renderer rend;
    TrailRenderer rastro;

    public static Bala Criar(Vector3 posicao, Vector3 velocidade, Inimigo dono)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.name = "Bala";
        go.transform.SetPositionAndRotation(posicao, Quaternion.LookRotation(velocidade));
        go.transform.localScale = new Vector3(0.09f, 0.09f, 0.4f);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = Recursos.I.bala;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var t = go.AddComponent<TrailRenderer>();
        t.time = 0.15f;
        t.startWidth = 0.06f;
        t.endWidth = 0f;
        t.sharedMaterial = Recursos.I.rastro;
        t.startColor = new Color(1f, 0.4f, 0.3f, 0.8f);
        t.endColor = new Color(1f, 0.3f, 0.2f, 0f);
        t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var b = go.AddComponent<Bala>();
        b.Velocidade = velocidade;
        b.Dono = dono;
        b.rend = r;
        b.rastro = t;
        b.Pintar(CorLonge);
        return b;
    }

    void OnEnable() => Ativas.Add(this);
    void OnDisable() => Ativas.Remove(this);

    void Pintar(Color cor)
    {
        mpb ??= new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor(CorID, cor);
        rend.SetPropertyBlock(mpb);
    }

    // Tempo (de jogo) até chegar ao alvo; infinito se vai errar, se já passou ou se foi desviada.
    public float TempoAte(Vector3 alvo)
    {
        float v = Velocidade.magnitude;
        if (Desviada || v < 0.01f)
            return float.PositiveInfinity;
        Vector3 dir = Velocidade / v;
        Vector3 p = alvo - transform.position;
        float aoLongo = Vector3.Dot(p, dir);
        if (aoLongo < -0.3f || (p - dir * aoLongo).magnitude > 1.1f)
            return float.PositiveInfinity;
        return Mathf.Max(0f, aoLongo) / v;
    }

    public static Bala MaisProxima(Vector3 alvo, out float tempo)
    {
        Bala melhor = null;
        tempo = float.PositiveInfinity;
        foreach (var b in Ativas)
        {
            float t = b.TempoAte(alvo);
            if (t < tempo)
            {
                tempo = t;
                melhor = b;
            }
        }
        return melhor;
    }

    // Aparada: volta para quem atirou, mais rápida.
    public void Desviar(bool perfeito)
    {
        Desviada = true;
        Vector3 destino = Dono != null ? Dono.transform.position : transform.position - Velocidade;
        Velocidade = (destino - transform.position).normalized * (perfeito ? 60f : 45f);
        vida = 4f;
        Pintar(CorDesviada);
        rastro.startColor = new Color(0.4f, 0.9f, 1f, 0.9f);
        rastro.endColor = new Color(0.4f, 0.9f, 1f, 0f);
        Efeitos.Faiscas(transform.position, Velocidade.normalized, new Color(5f, 4f, 1.5f), perfeito ? 14 : 7);
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

        if (Desviada && Dono != null)
        {
            Vector3 desejada = (Dono.transform.position - transform.position).normalized * Velocidade.magnitude;
            Velocidade = Vector3.RotateTowards(Velocidade, desejada, 6f * dt, 0f);
        }
        else if (!Desviada && Jogador.I != null)
        {
            var j = Jogador.I;
            float t = TempoAte(j.Centro);
            Pintar(t <= j.janelaPerfeita ? CorPerfeita : t <= j.janela ? CorJanela : CorLonge);
        }

        Vector3 passo = Velocidade * dt;
        float distancia = passo.magnitude;
        if (distancia > 0f && Physics.SphereCast(transform.position, Raio, passo / distancia, out var hit, distancia, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Acertou(hit))
                return;
        }
        transform.position += passo;
        if (distancia > 0f)
            transform.rotation = Quaternion.LookRotation(Velocidade);
    }

    // Devolve true se a bala acabou aqui.
    bool Acertou(RaycastHit hit)
    {
        var jogador = hit.collider.GetComponent<Jogador>();
        if (jogador != null)
        {
            if (Desviada)
                return false;
            if (jogador.Invulneravel)
            {
                if (jogador.EmDash && !esquivada)
                {
                    esquivada = true;
                    jogador.Esquivou();
                }
                return false; // atravessa (no dash o jogador fica intocável)
            }
            jogador.Dano(1);
            Destroy(gameObject);
            return true;
        }

        var inimigo = hit.collider.GetComponentInParent<Inimigo>();
        if (inimigo != null)
        {
            if (!Desviada)
                return false; // Agentes não se acertam
            inimigo.Dano(3, hit.point);
            Destroy(gameObject);
            return true;
        }

        Efeitos.Faiscas(hit.point, hit.normal, Desviada ? new Color(0.5f, 2.5f, 4f) : new Color(4f, 1.2f, 0.6f), 4);
        Destroy(gameObject);
        return true;
    }
}
