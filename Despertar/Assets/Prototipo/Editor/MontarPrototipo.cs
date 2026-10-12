using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Monta a cena do protótipo de combate: uma praça cercada de prédios do POLYGON City, o jogador,
// os pontos de onde os Agentes surgem e os materiais. Menu: Despertar > Montar protótipo de combate.
// Pode rodar de novo à vontade: a cena é refeita do zero.
public static class MontarPrototipo
{
    const string Pasta = "Assets/Prototipo";
    const string CaminhoCena = Pasta + "/Combate.unity";
    const float Meia = 18f; // metade do lado da praça
    const float Escala = Meia / 24f; // as posições abaixo foram pensadas para uma praça de 48 m

    // (prefab, x, z, giro em y): cobertura no meio da praça
    static readonly (string, float, float, float)[] Cobertura =
    {
        ("SM_Prop_Barrier_01", -4f, -12f, 0f),
        ("SM_Prop_Barrier_01", 4f, -12f, 0f),
        ("SM_Prop_Planter_01", -11f, -8f, 0f),
        ("SM_Prop_Planter_02", 11f, -8f, 0f),
        ("SM_Veh_Car_Sedan_01", -8f, -1f, 75f),
        ("SM_Veh_Car_Van_01", 9f, 1f, -100f),
        ("SM_Prop_Skip_01", 0f, -3f, 10f),
        ("SM_Prop_ParkBench_01", -2f, 5f, 0f),
        ("SM_Veh_Car_Police_01", -10f, 13f, 20f),
        ("SM_Prop_BusStop_01", 6f, 13f, 180f),
        ("SM_Prop_Barrier_01", 0f, 10f, 0f),
        ("SM_Prop_PowerBox_01", 13f, 17f, 0f),
        ("SM_Prop_Trashbin_01", -15f, 4f, 0f),
        ("SM_Prop_Phones_01", 16f, -3f, 90f),
        ("SM_Prop_Pallet_01", -13f, -14f, 30f),
        ("SM_Prop_TrashBag_01", -14.5f, 5f, 0f),
        ("SM_Prop_Hydrant_01", 15f, -9f, 0f),
    };

    // prédios de cada lado da praça (norte, leste, sul, oeste), enfileirados até fechar o lado
    static readonly string[][] Predios =
    {
        new[] { "SM_Bld_OfficeOld_Large_01", "SM_Bld_OfficeSquare_04", "SM_Bld_OfficeOld_Small_01", "SM_Bld_Station_03" },
        new[] { "SM_Bld_OfficeSquare_02", "SM_Bld_OfficeOld_Small_02", "SM_Bld_OfficeSquare_04", "SM_Bld_Station_03" },
        new[] { "SM_Bld_OfficeOld_Large_02", "SM_Bld_OfficeSquare_02", "SM_Bld_OfficeOld_Small_01", "SM_Bld_Station_03" },
        new[] { "SM_Bld_OfficeOld_Small_02", "SM_Bld_OfficeSquare_04", "SM_Bld_OfficeOld_Large_01", "SM_Bld_Station_03" },
    };

    // onde os Agentes podem surgir (metade norte da praça)
    static readonly Vector2[] Surgimento =
    {
        new Vector2(-14f, 7f), new Vector2(-4f, 16f), new Vector2(3f, 19f), new Vector2(12f, 8f),
        new Vector2(-1f, 21f), new Vector2(16f, 15f), new Vector2(-17f, 19f), new Vector2(7f, 21f),
    };

    [MenuItem("Despertar/Montar protótipo de combate")]
    public static void Montar()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        Directory.CreateDirectory(Pasta + "/Materiais");
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Iluminacao();
        var cenario = new GameObject("Cenário").transform;
        float chao = CriarChao(cenario);
        CriarBordas(cenario, chao);
        CriarCobertura(cenario, chao);
        Physics.SyncTransforms();
        var pontos = CriarPontos(chao);
        CriarJogador(new Vector3(0f, chao + 0.05f, -Meia + 6f), 0f);
        CriarJogo().AddComponent<Ondas>().pontos = pontos;

        Salvar(cena, CaminhoCena, 0);
    }

    // O objeto "Jogo": materiais, câmera lenta, som e HUD (sem as ondas de Agentes).
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
        jogo.AddComponent<HUD>();
        return jogo;
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

    // Devolve a altura do chão (onde os pés pisam).
    static float CriarChao(Transform pai)
    {
        var raiz = new GameObject("Chão").transform;
        raiz.SetParent(pai);
        float lado = 2f * (Meia + 4f);
        float topo = 0f;
        bool feito = false;

        var prefab = Carregar("SM_Env_Sidewalk_01");
        if (prefab != null)
        {
            var teste = Instanciar(prefab, Vector3.zero, 0f, raiz);
            var b = Limites(teste);
            Object.DestroyImmediate(teste);
            float tx = b.size.x, tz = b.size.z;
            if (tx > 0.5f && tz > 0.5f && (lado / tx) * (lado / tz) <= 900f)
            {
                topo = b.max.y;
                int nx = Mathf.CeilToInt(lado / tx), nz = Mathf.CeilToInt(lado / tz);
                for (int i = 0; i < nx; i++)
                    for (int k = 0; k < nz; k++)
                    {
                        var centro = new Vector3(-lado / 2f + (i + 0.5f) * tx, 0f, -lado / 2f + (k + 0.5f) * tz);
                        var t = Instanciar(prefab, centro - new Vector3(b.center.x, 0f, b.center.z), 0f, raiz);
                        foreach (var c in t.GetComponentsInChildren<Collider>())
                            Object.DestroyImmediate(c);
                        t.isStatic = true;
                    }
                feito = true;
            }
        }
        if (!feito)
        {
            var plano = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plano.name = "Chão simples";
            plano.transform.SetParent(raiz);
            plano.transform.localScale = new Vector3(lado / 10f, 1f, lado / 10f);
            Object.DestroyImmediate(plano.GetComponent<Collider>());
        }

        var colisao = new GameObject("Chão (colisão)").AddComponent<BoxCollider>();
        colisao.transform.SetParent(raiz);
        colisao.center = new Vector3(0f, topo - 0.5f, 0f);
        colisao.size = new Vector3(lado, 1f, lado);
        return topo;
    }

    static void CriarBordas(Transform pai, float chao)
    {
        var raiz = new GameObject("Bordas").transform;
        raiz.SetParent(pai);

        // paredes invisíveis
        for (int i = 0; i < 4; i++)
        {
            var p = new GameObject("Parede invisível").AddComponent<BoxCollider>();
            p.transform.SetParent(raiz);
            p.transform.rotation = Quaternion.Euler(0f, 90f * i, 0f);
            p.transform.position = p.transform.rotation * new Vector3(0f, chao + 6f, Meia + 0.5f);
            p.size = new Vector3(2f * Meia + 2f, 12f, 1f);
        }

        // prédios fechando os quatro lados, árvores de calçada em volta
        for (int lado = 0; lado < 4; lado++)
            Fileira(raiz, Predios[lado], Meia + 1f, 180f + 90f * lado, chao, 0f);

        var arvores = new[] { "SM_Env_Tree_01", "SM_Env_Tree_02", "SM_Env_Tree_03" };
        var rng = new System.Random(3);
        for (float s = -Meia + 3f; s <= Meia - 3f; s += 9f)
        {
            foreach (var (x, z) in new[] { (-Meia + 1.5f, s), (Meia - 1.5f, s), (s, -Meia + 1.5f) })
            {
                var prefab = Carregar(arvores[rng.Next(arvores.Length)]);
                if (prefab == null)
                    continue;
                var p = Instanciar(prefab, new Vector3(x, chao, z), (float)rng.NextDouble() * 360f, raiz);
                foreach (var c in p.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(c);
                var tronco = p.AddComponent<CapsuleCollider>();
                tronco.radius = 0.35f;
                tronco.height = 6f;
                tronco.center = new Vector3(0f, 3f, 0f);
                p.isStatic = true;
            }
        }
    }

    // Enfileira prefabs ao longo de um lado da praça, de frente para o centro,
    // com a fachada a `distancia` do centro (o resto do prédio fica para fora).
    static void Fileira(Transform pai, string[] nomes, float distancia, float giro, float chao, float folga, bool repetir = false)
    {
        var prefabs = new List<GameObject>();
        foreach (var n in nomes)
        {
            var p = Carregar(n);
            if (p != null)
                prefabs.Add(p);
        }
        if (prefabs.Count == 0)
            return;

        var rot = Quaternion.Euler(0f, giro, 0f);
        Vector3 fora = rot * Vector3.back; // do centro da praça para fora
        Vector3 ao_longo = rot * Vector3.right;
        var pecas = new List<(GameObject go, float largura, float fundo, Vector3 desvio)>();
        float total = 0f;
        int i = 0;
        while (true)
        {
            var go = Instanciar(prefabs[i % prefabs.Count], Vector3.zero, giro, pai);
            var b = Limites(go);
            float largura = Mathf.Abs(Vector3.Dot(b.size, new Vector3(Mathf.Abs(ao_longo.x), 0f, Mathf.Abs(ao_longo.z))));
            float fundo = Mathf.Abs(Vector3.Dot(b.size, new Vector3(Mathf.Abs(fora.x), 0f, Mathf.Abs(fora.z))));
            if (largura < 0.5f || (total + largura > 2f * Meia + 8f && pecas.Count > 0))
            {
                Object.DestroyImmediate(go);
                break;
            }
            pecas.Add((go, largura, fundo, b.center));
            total += largura + folga;
            i++;
            if (!repetir && i >= prefabs.Count)
                break;
        }
        float cursor = -total / 2f;
        foreach (var (go, largura, fundo, desvio) in pecas)
        {
            Vector3 alvo = fora * (distancia + fundo / 2f) + ao_longo * (cursor + largura / 2f);
            go.transform.position = new Vector3(alvo.x - desvio.x, chao, alvo.z - desvio.z);
            go.isStatic = true;
            ComColisor(go);
            cursor += largura + folga;
        }
    }

    static void CriarCobertura(Transform pai, float chao)
    {
        var raiz = new GameObject("Cobertura").transform;
        raiz.SetParent(pai);
        foreach (var (nome, x, z, giro) in Cobertura)
        {
            var prefab = Carregar(nome);
            if (prefab == null)
                continue;
            var go = Instanciar(prefab, new Vector3(x * Escala, chao, z * Escala), giro, raiz);
            go.isStatic = true;
            ComColisor(go);
        }
    }

    static Transform[] CriarPontos(float chao)
    {
        var raiz = new GameObject("Pontos dos Agentes").transform;
        var lista = new List<Transform>();
        foreach (var s in Surgimento)
        {
            var base_ = new Vector3(s.x * Escala, chao, s.y * Escala);
            var p = base_;
            // se cair dentro de algo, procura um espaço livre por perto
            for (int tentativa = 0; tentativa < 24 && Ocupado(p); tentativa++)
                p = base_ + Quaternion.Euler(0f, tentativa * 47f, 0f) * Vector3.forward * (1f + tentativa * 0.3f);
            var t = new GameObject("Ponto " + (lista.Count + 1)).transform;
            t.SetParent(raiz);
            t.position = p;
            lista.Add(t);
        }
        return lista.ToArray();
    }

    static bool Ocupado(Vector3 pe) =>
        Physics.CheckCapsule(pe + Vector3.up * 0.7f, pe + Vector3.up * 1.6f, 0.6f, ~0, QueryTriggerInteraction.Ignore);

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

    // Caixa de colisão do tamanho do objeto, se ele não tiver nenhuma.
    internal static void ComColisor(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>() != null)
            return;
        var rot = go.transform.rotation;
        go.transform.rotation = Quaternion.identity;
        var b = Limites(go);
        go.transform.rotation = rot;
        var caixa = go.AddComponent<BoxCollider>();
        caixa.center = b.center - go.transform.position;
        caixa.size = b.size;
    }

    internal static Material Mat(string nome, string shader, System.Action<Material> configurar)
    {
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
