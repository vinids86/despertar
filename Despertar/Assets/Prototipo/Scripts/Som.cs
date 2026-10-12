using System;
using UnityEngine;

// Sons gerados por código, menos os passos e o tiro do jogador (arquivos em Prototipo/Sons, ligados pelo montador da cena).
// O que acontece no mundo fica mais grave em câmera lenta; o que é do jogador, não.
public class Som : MonoBehaviour
{
    public static Som I;

    public AudioClip passo, tiro;

    const int Taxa = 44100;
    // os arquivos começam com um pouco de silêncio (~0,12 s o passo, ~0,1 s o tiro)
    const float SilencioPasso = 0.1f, SilencioTiro = 0.09f;
    AudioSource[] fontes;
    int proxima;
    AudioClip tiroInimigo, aparar, perfeito, esquiva, dano, entrarLento, sairLento, morte, dash, erro, recarga, engatilhar, vazio;

    void Awake()
    {
        I = this;
        fontes = new AudioSource[16];
        for (int i = 0; i < fontes.Length; i++)
        {
            var f = gameObject.AddComponent<AudioSource>();
            f.playOnAwake = false;
            f.spatialBlend = 0f;
            fontes[i] = f;
        }

        var rng = new System.Random(7);
        float Ruido() => (float)(rng.NextDouble() * 2.0 - 1.0);
        float Seno(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        tiroInimigo = Gerar("tiro inimigo", 0.22f, t => Ruido() * Mathf.Exp(-t * 22f) * 0.6f + Seno(140f, t) * Mathf.Exp(-t * 14f) * 0.45f);
        aparar = Gerar("aparar", 0.35f, t => (Seno(1900f, t) + 0.5f * Seno(3800f, t)) * Mathf.Exp(-t * 12f) * 0.4f + Ruido() * Mathf.Exp(-t * 80f) * 0.5f);
        perfeito = Gerar("perfeito", 0.7f, t => (Seno(1500f, t) + 0.8f * Seno(2250f, t)) * Mathf.Exp(-t * 5f) * 0.3f + Ruido() * Mathf.Exp(-t * 60f) * 0.6f);
        esquiva = Gerar("esquiva", 0.3f, t => Seno(2600f, t) * Mathf.Exp(-t * 14f) * 0.25f + Ruido() * Mathf.Sin(Mathf.PI * t / 0.3f) * 0.2f);
        dano = Gerar("dano", 0.3f, t => Seno(70f - 40f * t, t) * Mathf.Exp(-t * 9f) * 0.9f + Ruido() * Mathf.Exp(-t * 25f) * 0.4f);
        morte = Gerar("morte", 0.5f, t => Mathf.Sign(Seno(400f - 500f * t, t)) * Mathf.Exp(-t * 6f) * 0.2f + Ruido() * Mathf.Exp(-t * 10f) * 0.3f);
        dash = Gerar("dash", 0.22f, t => Ruido() * Mathf.Sin(Mathf.PI * t / 0.22f) * 0.35f);
        erro = Gerar("erro", 0.12f, t => Mathf.Sign(Seno(220f, t)) * Mathf.Exp(-t * 20f) * 0.12f);
        entrarLento = Varredura("entrar lento", 320f, 55f, 0.5f, 0.5f);
        sairLento = Varredura("sair lento", 70f, 260f, 0.3f, 0.4f);

        // cliques metálicos da recarga: tirar o pente; pôr o pente e puxar o ferrolho; gatilho sem bala
        float Clique(float t, float hz) => t < 0f ? 0f : Ruido() * Mathf.Exp(-t * 120f) * 0.5f + Seno(hz, t) * Mathf.Exp(-t * 45f) * 0.3f;
        recarga = Gerar("recarga", 0.2f, t => Clique(t, 2400f) + Clique(t - 0.07f, 1600f) * 0.6f);
        engatilhar = Gerar("engatilhar", 0.3f, t => Clique(t, 1800f) + Clique(t - 0.12f, 2800f) * 0.9f);
        vazio = Gerar("vazio", 0.08f, t => Clique(t, 3200f) * 0.6f);
    }

    static AudioClip Gerar(string nome, float duracao, Func<float, float> onda)
    {
        int n = Mathf.CeilToInt(duracao * Taxa);
        var dados = new float[n];
        for (int i = 0; i < n; i++)
            dados[i] = Mathf.Clamp(onda(i / (float)Taxa), -1f, 1f);
        var clip = AudioClip.Create(nome, n, 1, Taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }

    static AudioClip Varredura(string nome, float de, float para, float duracao, float volume)
    {
        int n = Mathf.CeilToInt(duracao * Taxa);
        var dados = new float[n];
        float fase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Taxa;
            fase += 2f * Mathf.PI * Mathf.Lerp(de, para, t / duracao) / Taxa;
            dados[i] = Mathf.Sin(fase) * volume * (1f - t / duracao);
        }
        var clip = AudioClip.Create(nome, n, 1, Taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }

    void Tocar(AudioClip clip, float volume, bool doMundo = true, float variacao = 0.05f, float inicio = 0f)
    {
        if (clip == null)
            return;
        var f = fontes[proxima];
        proxima = (proxima + 1) % fontes.Length;
        float escala = doMundo ? Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(Time.timeScale)) : 1f;
        f.pitch = escala * (1f + UnityEngine.Random.Range(-variacao, variacao));
        if (inicio <= 0f)
        {
            f.volume = 1f;
            f.PlayOneShot(clip, volume);
            return;
        }
        // PlayOneShot sempre toca do começo: para pular o silêncio, toca como clipe da própria fonte
        f.clip = clip;
        f.volume = volume;
        f.time = inicio;
        f.Play();
    }

    public void TiroJogador() => Tocar(tiro, 0.5f, false, 0.05f, SilencioTiro);
    public void Passo() => Tocar(passo, 0.35f, false, 0.08f, SilencioPasso);
    public void Recarga() => Tocar(recarga, 0.6f, false);
    public void Engatilhar() => Tocar(engatilhar, 0.7f, false);
    public void Vazio() => Tocar(vazio, 0.6f, false);
    public void TiroInimigo() => Tocar(tiroInimigo, 0.5f);
    public void Aparar(bool foiPerfeito) => Tocar(foiPerfeito ? perfeito : aparar, foiPerfeito ? 0.9f : 0.7f, false, 0.02f);
    public void Esquiva() => Tocar(esquiva, 0.7f, false);
    public void Dano() => Tocar(dano, 0.9f, false);
    public void Morte() => Tocar(morte, 0.6f);
    public void Dash() => Tocar(dash, 0.5f, false);
    public void Erro() => Tocar(erro, 0.5f, false, 0f);
    public void EntrarLento() => Tocar(entrarLento, 0.6f, false, 0f);
    public void SairLento() => Tocar(sairLento, 0.5f, false, 0f);
}
