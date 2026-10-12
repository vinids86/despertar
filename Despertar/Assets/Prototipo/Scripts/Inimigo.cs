using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TipoArma { Pistola, Metralhadora, Escopeta }

// Agente da simulação. Cada arma atira num ritmo diferente, e é esse ritmo que o jogador aprende a aparar.
// O visor acende antes de cada ataque: é o aviso.
public class Inimigo : MonoBehaviour
{
    public static readonly List<Inimigo> Vivos = new List<Inimigo>();

    static MaterialPropertyBlock mpb;
    static readonly int EmissaoID = Shader.PropertyToID("_EmissionColor");
    static readonly int CorID = Shader.PropertyToID("_BaseColor");
    static readonly Color VisorNormal = new Color(2.5f, 0.15f, 0.1f);
    static readonly Color VisorAviso = new Color(10f, 3f, 1.5f);
    static readonly Color CorCorpo = new Color(0.05f, 0.05f, 0.06f);
    const float VelocidadeBala = 24f;
    const float TempoDeAviso = 0.5f;

    public TipoArma Arma { get; private set; }

    int vida;
    Transform boca;
    Renderer corpo, visor;
    Vector3 origem;
    float fase, proximoAtaque, nascido, flashAte, aviso;
    bool atacando;

    public static Inimigo Criar(Vector3 chao, TipoArma arma)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Agente (" + arma + ")";
        go.transform.position = chao + Vector3.up;
        var corpo = go.GetComponent<Renderer>();
        corpo.sharedMaterial = Recursos.I.agente;

        var visor = Peca(go.transform, "Visor", new Vector3(0f, 0.55f, 0.36f), new Vector3(0.62f, 0.1f, 0.3f), Recursos.I.visor);

        // a arma é um bloco escuro na mão, maior conforme o tipo
        float comprimento = arma == TipoArma.Pistola ? 0.3f : arma == TipoArma.Metralhadora ? 0.5f : 0.75f;
        float espessura = arma == TipoArma.Escopeta ? 0.14f : 0.1f;
        Peca(go.transform, "Arma", new Vector3(0.38f, 0.15f, 0.3f + comprimento / 2f), new Vector3(espessura, espessura * 1.3f, comprimento), Recursos.I.agente);

        var boca = new GameObject("Boca").transform;
        boca.SetParent(go.transform, false);
        boca.localPosition = new Vector3(0.38f, 0.17f, 0.35f + comprimento);

        var i = go.AddComponent<Inimigo>();
        i.Arma = arma;
        i.boca = boca;
        i.corpo = corpo;
        i.visor = visor;
        i.vida = arma == TipoArma.Escopeta ? 4 : 3;
        i.origem = go.transform.position;
        i.fase = Random.value * 10f;
        i.nascido = Time.time;
        i.proximoAtaque = Time.time + Random.Range(1.2f, 2.4f);
        Efeitos.Codigo(go.transform.position, 18, false);
        return i;
    }

    static Renderer Peca(Transform pai, string nome, Vector3 posicao, Vector3 escala, Material material)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(p.GetComponent<Collider>());
        p.name = nome;
        p.transform.SetParent(pai, false);
        p.transform.localPosition = posicao;
        p.transform.localScale = escala;
        var r = p.GetComponent<Renderer>();
        r.sharedMaterial = material;
        return r;
    }

    void OnEnable() => Vivos.Add(this);
    void OnDisable() => Vivos.Remove(this);

    void Update()
    {
        // surge "decodificando": cresce de baixo para cima
        float surgir = Mathf.Clamp01((Time.time - nascido) / 0.35f);
        transform.localScale = new Vector3(1f, Mathf.Max(0.02f, surgir), 1f);

        aviso = Mathf.MoveTowards(aviso, 0f, Time.deltaTime * 3f);
        var j = Jogador.I;
        if (j != null && !j.Morto)
        {
            Mirar(j);
            Andar(j);
            if (!atacando && surgir >= 1f && Time.time >= proximoAtaque && VeJogador(j))
                StartCoroutine(Atacar(j));
        }
        Pintar();
    }

    void Mirar(Jogador j)
    {
        Vector3 para = j.transform.position - transform.position;
        para.y = 0f;
        if (para.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(para), 240f * Time.deltaTime);
    }

    // anda de um lado para o outro perto de onde surgiu, sem atravessar a cobertura
    void Andar(Jogador j)
    {
        fase += Time.deltaTime * (atacando ? 0.2f : 0.9f);
        Vector3 alvo = origem + Vector3.right * Mathf.Sin(fase) * 1.6f;
        Vector3 nova = Vector3.MoveTowards(transform.position, alvo, 2.5f * Time.deltaTime);
        Vector3 d = nova - transform.position;
        if (d.sqrMagnitude > 0f && !Physics.SphereCast(transform.position, 0.45f, d.normalized, out _, d.magnitude + 0.05f, ~0, QueryTriggerInteraction.Ignore))
            transform.position = nova;
    }

    bool VeJogador(Jogador j)
    {
        if (Physics.Linecast(boca.position, j.Centro, out var hit, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponent<Jogador>() != null;
        return true;
    }

    IEnumerator Atacar(Jogador j)
    {
        atacando = true;
        float inicio = Time.time;
        while (Time.time - inicio < TempoDeAviso)
        {
            aviso = Mathf.Max(aviso, (Time.time - inicio) / TempoDeAviso);
            yield return null;
        }

        switch (Arma)
        {
            case TipoArma.Pistola: // um tiro: "1 ... 1 ... 1"
                Disparar(j, 0f, true);
                proximoAtaque = Time.time + Random.Range(1.3f, 1.8f);
                break;
            case TipoArma.Metralhadora: // rajada de três: "ta-ta-ta"
                for (int k = 0; k < 3; k++)
                {
                    Disparar(j, 0f, true);
                    yield return new WaitForSeconds(0.13f);
                }
                proximoAtaque = Time.time + Random.Range(2f, 2.6f);
                break;
            case TipoArma.Escopeta: // cinco de uma vez: só um vem reto, os outros pedem dash
                for (int k = 0; k < 5; k++)
                    Disparar(j, k == 0 ? 0f : 9f, k == 0);
                proximoAtaque = Time.time + Random.Range(2.4f, 3f);
                break;
        }
        atacando = false;
    }

    void Disparar(Jogador j, float espalhamento, bool comSom)
    {
        if (j == null || j.Morto)
            return;
        Vector3 dir = (j.Centro - boca.position).normalized;
        if (espalhamento > 0f)
            dir = Quaternion.AngleAxis(Random.Range(-espalhamento, espalhamento), Vector3.up)
                * Quaternion.AngleAxis(Random.Range(-espalhamento, espalhamento) * 0.5f, transform.right) * dir;
        Bala.Criar(boca.position, dir * VelocidadeBala, this);
        if (comSom)
            Som.I?.TiroInimigo();
        Efeitos.Clarao(boca.position, new Color(4f, 1.2f, 0.5f));
        aviso = 1f;
    }

    public void Dano(int quanto, Vector3 ponto)
    {
        vida -= quanto;
        flashAte = Time.unscaledTime + 0.06f;
        Efeitos.Faiscas(ponto, (ponto - transform.position).normalized, new Color(0.6f, 4f, 1f), 6);
        if (vida <= 0)
        {
            Efeitos.Codigo(transform.position, 40, true);
            Som.I?.Morte();
            Destroy(gameObject);
        }
    }

    void Pintar()
    {
        mpb ??= new MaterialPropertyBlock();
        visor.GetPropertyBlock(mpb);
        mpb.SetColor(EmissaoID, Color.Lerp(VisorNormal, VisorAviso, aviso));
        visor.SetPropertyBlock(mpb);
        corpo.GetPropertyBlock(mpb);
        mpb.SetColor(CorID, Time.unscaledTime < flashAte ? Color.white : CorCorpo);
        corpo.SetPropertyBlock(mpb);
    }
}
