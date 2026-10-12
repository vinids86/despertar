using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// TESTE de visual: troca o sol de praia do Palm City por uma cidade nublada, fria e sem cor,
// mais perto de uma simulação que se passa pelo mundo real. Liga sozinho em toda cena com o Jogador;
// V alterna entre este visual e o original. Para desfazer o teste, basta apagar este arquivo.
public class VisualRealista : MonoBehaviour
{
    static bool ligado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        ligado = true;
        SceneManager.sceneLoaded -= AoCarregar;
        SceneManager.sceneLoaded += AoCarregar;
    }

    static void AoCarregar(Scene cena, LoadSceneMode modo)
    {
        if (Object.FindAnyObjectByType<Jogador>() != null && Object.FindAnyObjectByType<VisualRealista>() == null)
            new GameObject("Visual realista (teste)").AddComponent<VisualRealista>();
    }

    // o que a cena tinha antes, para poder voltar
    Volume[] originais;
    Light sol;
    float solIntensidade, solSombra;
    Color solCor;
    Quaternion solGiro;
    Camera cam;
    CameraClearFlags fundo;
    Color fundoCor;
    AmbientMode ambiente;
    Color ambCeu, ambMeio, ambChao;
    bool nevoa;
    FogMode nevoaModo;
    Color nevoaCor;
    float nevoaInicio, nevoaFim, reflexo;

    Volume volume;
    static readonly Color Cinza = new Color(0.6f, 0.63f, 0.65f); // céu nublado e névoa

    void Start()
    {
        // o volume da câmera lenta tem prioridade 10 e continua valendo por cima deste
        originais = System.Array.FindAll(FindObjectsByType<Volume>(), v => v.priority < 10f);
        sol = RenderSettings.sun;
        if (sol != null)
        {
            solIntensidade = sol.intensity;
            solSombra = sol.shadowStrength;
            solCor = sol.color;
            solGiro = sol.transform.rotation;
        }
        cam = Camera.main;
        if (cam != null)
        {
            fundo = cam.clearFlags;
            fundoCor = cam.backgroundColor;
        }
        ambiente = RenderSettings.ambientMode;
        ambCeu = RenderSettings.ambientSkyColor;
        ambMeio = RenderSettings.ambientEquatorColor;
        ambChao = RenderSettings.ambientGroundColor;
        nevoa = RenderSettings.fog;
        nevoaModo = RenderSettings.fogMode;
        nevoaCor = RenderSettings.fogColor;
        nevoaInicio = RenderSettings.fogStartDistance;
        nevoaFim = RenderSettings.fogEndDistance;
        reflexo = RenderSettings.reflectionIntensity;

        CriarVolume();
        Aplicar();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.vKey.wasPressedThisFrame)
        {
            ligado = !ligado;
            Aplicar();
        }
    }

    void Aplicar()
    {
        volume.enabled = ligado;
        foreach (var v in originais)
            if (v != null)
                v.enabled = !ligado;

        if (sol != null)
        {
            sol.intensity = ligado ? 0.5f : solIntensidade;
            sol.shadowStrength = ligado ? 0.5f : solSombra;
            sol.color = ligado ? new Color(0.8f, 0.84f, 0.9f) : solCor;
            sol.transform.rotation = ligado ? Quaternion.Euler(62f, -70f, 0f) : solGiro;
        }
        // céu liso da cor da névoa: o horizonte some no cinza
        if (cam != null)
        {
            cam.clearFlags = ligado ? CameraClearFlags.SolidColor : fundo;
            cam.backgroundColor = ligado ? Cinza : fundoCor;
        }
        RenderSettings.ambientMode = ligado ? AmbientMode.Trilight : ambiente;
        RenderSettings.ambientSkyColor = ligado ? new Color(0.62f, 0.65f, 0.68f) : ambCeu;
        RenderSettings.ambientEquatorColor = ligado ? new Color(0.5f, 0.52f, 0.54f) : ambMeio;
        RenderSettings.ambientGroundColor = ligado ? new Color(0.25f, 0.25f, 0.25f) : ambChao;
        RenderSettings.fog = ligado || nevoa;
        RenderSettings.fogMode = ligado ? FogMode.Linear : nevoaModo;
        RenderSettings.fogColor = ligado ? Cinza : nevoaCor;
        RenderSettings.fogStartDistance = ligado ? 4f : nevoaInicio;
        RenderSettings.fogEndDistance = ligado ? 60f : nevoaFim;
        RenderSettings.reflectionIntensity = ligado ? 0.45f : reflexo;
    }

    void CriarVolume()
    {
        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;

        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        perfil.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
        var cor = perfil.Add<ColorAdjustments>();
        cor.saturation.Override(-30f); // sem exagero: as balas vermelhas precisam continuar legíveis
        cor.contrast.Override(14f);
        cor.colorFilter.Override(new Color(0.94f, 0.98f, 0.98f));
        perfil.Add<WhiteBalance>().temperature.Override(-14f);
        // um quase nada de verde nas sombras: a simulação
        perfil.Add<ShadowsMidtonesHighlights>().shadows.Override(new Vector4(0.96f, 1.03f, 1f, 0f));
        var vinheta = perfil.Add<Vignette>();
        vinheta.intensity.Override(0.28f);
        vinheta.smoothness.Override(0.5f);
        var grao = perfil.Add<FilmGrain>();
        grao.type.Override(FilmGrainLookup.Medium1);
        grao.intensity.Override(0.3f);
        var brilho = perfil.Add<Bloom>();
        brilho.threshold.Override(1f);
        brilho.intensity.Override(0.7f);
        brilho.scatter.Override(0.6f);
        volume.sharedProfile = perfil;
    }
}
