using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Câmera lenta do jogador: uma barra que gasta enquanto ele segura e a cada tiro dado nela.
// Sozinha, a barra só volta até um piso: o resto se ganha arriscando em tempo normal (parry e esquiva).
// Também faz o "congelamento" de um instante no parry e o visual.
public class TempoBala : MonoBehaviour
{
    public static TempoBala I;

    public float escalaLenta = 0.2f;
    public float maximo = 100f;
    public float energiaInicial = 40f;
    public float gastoPorSegundo = 18f; // em tempo real
    public float custoPorTiro = 6f; // cada tiro dado em câmera lenta
    public float pisoRecarga = 20f; // até onde a barra volta sozinha
    public float recargaPorSegundo = 7f;
    public float esperaParaRecarregar = 1f;
    public float minimoParaAtivar = 12f;

    public float Energia { get; private set; }
    public bool Ativo { get; private set; }
    public float Mistura { get; private set; } // 0 = normal, 1 = totalmente lento (suavizado)
    public bool EmTempoNormal => !Ativo && Mistura < 0.3f;

    float ultimoUso = -99f;
    float congeladoAte;
    float fixedPadrao;
    Volume volume;

    void Awake()
    {
        I = this;
        Energia = energiaInicial;
        fixedPadrao = Time.fixedDeltaTime;
        CriarVisual();
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = fixedPadrao;
    }

    public void Segurar(bool querendo)
    {
        if (querendo && !Ativo && Energia >= minimoParaAtivar)
        {
            Ativo = true;
            Som.I?.EntrarLento();
        }
        else if (!querendo && Ativo)
        {
            Ativo = false;
            Som.I?.SairLento();
        }
    }

    public void Ganhar(float quanto) => Energia = Mathf.Min(maximo, Energia + quanto);

    public void Gastar(float quanto) => Energia = Mathf.Max(0f, Energia - quanto);

    public void Congelar(float segundos) => congeladoAte = Mathf.Max(congeladoAte, Time.unscaledTime + segundos);

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (Ativo)
        {
            Energia -= gastoPorSegundo * dt;
            ultimoUso = Time.unscaledTime;
            if (Energia <= 0f)
            {
                Energia = 0f;
                Ativo = false;
                Som.I?.SairLento();
            }
        }
        else if (Energia < pisoRecarga && Time.unscaledTime - ultimoUso > esperaParaRecarregar)
        {
            Energia = Mathf.Min(pisoRecarga, Energia + recargaPorSegundo * dt);
        }

        bool morto = Jogador.I != null && Jogador.I.Morto;
        float alvo = Ativo || morto ? 1f : 0f;
        Mistura = Mathf.MoveTowards(Mistura, alvo, dt * (alvo > Mistura ? 7f : 4f));

        float escala = Mathf.Lerp(1f, morto ? 0.12f : escalaLenta, Mistura);
        if (Time.unscaledTime < congeladoAte)
            escala = 0.01f;
        Time.timeScale = escala;
        Time.fixedDeltaTime = fixedPadrao * escala;
        if (volume != null)
            volume.weight = Mistura;
    }

    void CriarVisual()
    {
        var go = new GameObject("Volume da câmera lenta");
        go.transform.SetParent(transform, false);
        volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;
        volume.weight = 0f;

        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        var cor = perfil.Add<ColorAdjustments>(true);
        cor.saturation.Override(-70f);
        cor.contrast.Override(18f);
        cor.colorFilter.Override(new Color(0.82f, 1f, 0.86f));
        var vinheta = perfil.Add<Vignette>(true);
        vinheta.intensity.Override(0.45f);
        vinheta.smoothness.Override(0.6f);
        var aberracao = perfil.Add<ChromaticAberration>(true);
        aberracao.intensity.Override(0.55f);
        volume.sharedProfile = perfil;
    }
}
