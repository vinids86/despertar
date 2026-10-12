using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

// Peças em comum para montar as cenas do protótipo (hoje só o escritório: Despertar > Montar escritório):
// o objeto "Jogo" com a HUD, a iluminação, o jogador e utilidades para carregar os prefabs da Synty.
public static class MontarPrototipo
{
    const string Pasta = "Assets/Prototipo";
    const string PastaHUD = "Assets/Synty/InterfaceSciFiSoldierHUD";

    // O objeto "Jogo": materiais, câmera lenta, som e HUD.
    internal static GameObject CriarJogo()
    {
        var jogo = new GameObject("Jogo");
        var recursos = jogo.AddComponent<Recursos>();
        recursos.agente = Mat("Agente", "Universal Render Pipeline/Lit", m =>
        {
            m.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.06f));
            m.SetFloat("_Smoothness", 0.85f);
            m.SetFloat("_Metallic", 0.2f);
        });
        recursos.visor = Mat("Visor", "Universal Render Pipeline/Lit", m =>
        {
            m.SetColor("_BaseColor", Color.black);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(2.5f, 0.15f, 0.1f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        });
        recursos.bala = Mat("Brilho", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", new Color(4f, 0.4f, 0.3f)));
        recursos.rastro = Mat("Rastro", "Sprites/Default", m => { });
        jogo.AddComponent<TempoBala>();
        var som = jogo.AddComponent<Som>();
        som.passo = AssetDatabase.LoadAssetAtPath<AudioClip>(Pasta + "/Sons/PassoPedra.wav");
        som.tiro = AssetDatabase.LoadAssetAtPath<AudioClip>(Pasta + "/Sons/TiroPistola.wav");
        som.recarga = AssetDatabase.LoadAssetAtPath<AudioClip>(Pasta + "/Sons/RecargaArma.wav");
        som.vazio = AssetDatabase.LoadAssetAtPath<AudioClip>(Pasta + "/Sons/GatilhoVazio.wav");
        PreencherHUD(jogo.AddComponent<HUD>());
        return jogo;
    }

    // Liga a HUD às peças do pacote INTERFACE Sci-Fi Soldier HUD.
    internal static void PreencherHUD(HUD hud)
    {
        hud.fonteTexto = Fonte("exo-2-0/Exo2.0-SemiBold.otf", "Exo2 SDF");
        hud.fonteTitulo = Fonte("Orbitron/Orbitron-ExtraBold.ttf", "Orbitron SDF");
        if (hud.fonteTitulo.fallbackFontAssetTable == null || !hud.fonteTitulo.fallbackFontAssetTable.Contains(hud.fonteTexto))
        {
            hud.fonteTitulo.fallbackFontAssetTable = new List<TMP_FontAsset> { hud.fonteTexto }; // a Orbitron não tem o "·"
            EditorUtility.SetDirty(hud.fonteTitulo);
        }
        hud.prefabMira = PecaHUD("Reticles_Crosshairs/HUD_SciFiSoldier_Reticle_Hexagon_01");
        hud.prefabTempo = PecaHUD("Player_Health_Equipment/HUD_SciFiSoldier_HealthBar_03");
        hud.prefabArma = PecaHUD("Player_Health_Equipment/HUD_SciFiSoldier_CurrentWeapon_03");
        hud.prefabDano = PecaHUD("FullscreenFX/FX_SciFiSoldier_Damage_Vignette_01");
        hud.prefabFaixa = PecaHUD("Objectives_Story_Location/HUD_SciFiSoldier_Event_NewLocation_01");
        hud.prefabEtiqueta = PecaHUD("Log/_Parts/HUD_SciFiSoldier_EventLog_Item_01");
        hud.anel = SpriteHUD("HUD/SPR_HUD_SciFiSoldier_Reticle_Ring_Medium_01");
        hud.marca = SpriteHUD("HUD/SPR_HUD_SciFiSoldier_Reticle_Ring_Small_01");
        hud.segmento = SpriteHUD("HUD/SPR_HUD_SciFiSoldier_Bar_Angled_01");
        hud.caixa = SpriteHUD("HUD/SPR_HUD_SciFiSoldier_Box_02");
        hud.iconeVida = SpriteHUD("Icons_Status/ICON_SciFiSoldier_Status_Health_02");
        hud.iconeTempo = SpriteHUD("Icons_Map/ICON_SciFiSoldier_Map_Lightning_01");
        hud.iconeArma = SpriteHUD("Icons_Weapons/ICON_SM_Wep_Pistol_01_SciFiCity_Side");
        EditorUtility.SetDirty(hud);
    }

    static GameObject PecaHUD(string nome) => AssetDatabase.LoadAssetAtPath<GameObject>(PastaHUD + "/Prefabs/" + nome + ".prefab");

    static Sprite SpriteHUD(string nome) => AssetDatabase.LoadAssetAtPath<Sprite>(PastaHUD + "/Sprites/" + nome + ".png");

    // As fontes TMP do pacote não têm acentos. Geramos as nossas das mesmas fontes (licença OFL, em Fontes/),
    // estáticas, com o ASCII e os acentos do português.
    static TMP_FontAsset Fonte(string origem, string nome)
    {
        string caminho = Pasta + "/Fontes/" + nome + ".asset";
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(caminho);
        if (fa != null)
            return fa;
        Directory.CreateDirectory(Pasta + "/Fontes");
        var fonte = AssetDatabase.LoadAssetAtPath<Font>(PastaHUD + "/Fonts/" + origem);
        fa = TMP_FontAsset.CreateFontAsset(fonte, 56, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        var letras = new StringBuilder();
        for (char c = ' '; c <= '~'; c++)
            letras.Append(c);
        letras.Append("ÀÁÂÃÇÉÊÍÓÔÕÚàáâãçéêíóôõú·");
        if (!fa.TryAddCharacters(letras.ToString(), out string faltando))
            Debug.Log("[Despertar] " + nome + " sem: " + faltando);
        fa.atlasPopulationMode = AtlasPopulationMode.Static;
        fa.name = nome;
        AssetDatabase.CreateAsset(fa, caminho);
        fa.atlasTexture.name = nome + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
        fa.material.name = nome + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    // Salva a cena e a coloca nas Build Settings (na posição pedida; -1 = no fim).
    internal static void Salvar(UnityEngine.SceneManagement.Scene cena, string caminho, int posicao)
    {
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(cena, caminho);
        var cenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        cenas.RemoveAll(s => s.path == caminho);
        cenas.Insert(posicao < 0 ? cenas.Count : Mathf.Min(posicao, cenas.Count), new EditorBuildSettingsScene(caminho, true));
        EditorBuildSettings.scenes = cenas.ToArray();
        Debug.Log("[Despertar] cena montada em " + caminho);
    }

    internal static void Iluminacao()
    {
        var sol = new GameObject("Sol").AddComponent<Light>();
        sol.type = LightType.Directional;
        sol.intensity = 1.4f;
        sol.shadows = LightShadows.Soft;
        sol.color = new Color(1f, 0.94f, 0.84f);
        sol.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
        RenderSettings.sun = sol;

        // o céu do Palm City é um domo (malha), não funciona como skybox: usa o céu padrão
        RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.75f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.2f);

        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Synty/PolygonPalmCity/Volumes/Global Volume_Base_01.asset");
        if (perfil != null)
        {
            var v = new GameObject("Volume global").AddComponent<Volume>();
            v.isGlobal = true;
            v.sharedProfile = perfil;
        }
    }

    internal static void CriarJogador(Vector3 posicao, float giro)
    {
        var go = new GameObject("Jogador");
        go.transform.SetPositionAndRotation(posicao, Quaternion.Euler(0f, giro, 0f));
        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.7f; // com a margem de colisão, ainda passa folgado por um vão de porta de 2 m
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.85f, 0f);
        cc.stepOffset = 0.4f;

        var camGo = new GameObject("Câmera") { tag = "MainCamera" };
        camGo.transform.SetParent(go.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 600f;
        camGo.AddComponent<AudioListener>();
        var dados = camGo.AddComponent<UniversalAdditionalCameraData>();
        dados.renderPostProcessing = true;
        dados.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

        Transform arma = null, boca = null;
        var pistola = Carregar("SM_Wep_Pistol_01");
        if (pistola != null)
        {
            var a = Instanciar(pistola, Vector3.zero, 0f, camGo.transform);
            PrefabUtility.UnpackPrefabInstance(a, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var c in a.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
            foreach (var r in a.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;
            a.transform.localPosition = new Vector3(0.17f, -0.18f, 0.4f);
            a.transform.localRotation = Quaternion.Euler(0f, -4f, 0f);
            arma = a.transform;

            // furo do cano no mesh da SM_Wep_Pistol_01 (os limites incluem o cabo e jogavam o ponto para baixo)
            boca = new GameObject("Boca").transform;
            boca.SetParent(arma, false);
            boca.localPosition = new Vector3(0f, 0.061f, 0.21f);
        }

        var j = go.AddComponent<Jogador>();
        j.arma = arma;
        j.boca = boca;
    }

    // ------------------------------------------------------------------ utilidades

    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

    // Procura primeiro no City (alguns nomes também existem no Palm City), depois em todo o Synty.
    internal static GameObject Carregar(string nome)
    {
        if (cache.TryGetValue(nome, out var guardado) && guardado != null)
            return guardado;
        foreach (var pasta in new[] { "Assets/Synty/PolygonCity", "Assets/Synty" })
        {
            foreach (var guid in AssetDatabase.FindAssets(nome + " t:Prefab", new[] { pasta }))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(caminho) == nome)
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
                    cache[nome] = go;
                    return go;
                }
            }
        }
        Debug.LogWarning("[Despertar] prefab não encontrado: " + nome);
        return null;
    }

    internal static GameObject Instanciar(GameObject prefab, Vector3 posicao, float giro, Transform pai)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        go.transform.SetPositionAndRotation(posicao, Quaternion.Euler(0f, giro, 0f));
        return go;
    }

    internal static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);
        var b = rs[0].bounds;
        foreach (var r in rs)
            b.Encapsulate(r.bounds);
        return b;
    }

    internal static Material Mat(string nome, string shader, System.Action<Material> configurar)
    {
        Directory.CreateDirectory(Pasta + "/Materiais");
        string caminho = Pasta + "/Materiais/" + nome + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, caminho);
        }
        else
        {
            m.shader = Shader.Find(shader);
        }
        configurar(m);
        EditorUtility.SetDirty(m);
        return m;
    }
}
