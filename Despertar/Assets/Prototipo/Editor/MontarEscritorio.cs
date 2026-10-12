using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Monta um andar de escritório com o POLYGON Office: salas em sequência (recepção > corredor >
// escritório aberto > reunião > arquivo > elevador), com Agentes em cada sala e o elevador como saída.
// Menu: Despertar > Montar escritório. Refaz a cena do zero.
public static class MontarEscritorio
{
    const string CaminhoCena = "Assets/Prototipo/Escritorio.unity";
    const float C = 2.5f; // lado de uma célula da grade (tamanho das peças do Office)
    const float Altura = 3f; // pé-direito

    // as paredes do Office só têm a face da frente: cada borda leva uma peça virada para cada lado
    const string Parede = "SM_Bld_Wall_Baseboard_01";
    const string ParedeFora = "SM_Bld_Wall_Blank_01";
    const string Janela = "SM_Bld_Wall_Blank_Window_01";
    const string Vidro = "SM_Bld_Wall_Glass_01";
    const string Porta = "SM_Bld_Wall_Blank_Door_Large_01"; // ocupa 2 células; vão de 1,85 x 2,44 m
    const string PortaVidro = "SM_Bld_Wall_Glass_Large_Door_01";
    const string Elevador = "SM_Bld_Wall_Blank_Elevator_01";

    class Sala
    {
        public string nome;
        public int i0, k0, w, d; // em células
        public string piso;
        public bool janelas, vidro; // vidro: paredes internas de vidro
        public Transform raiz;
    }

    // a rota segue na ordem da lista
    static readonly Sala[] Salas =
    {
        new Sala { nome = "Recepção", i0 = 0, k0 = 0, w = 4, d = 4, piso = "SM_Bld_Floor_Tiles_01", janelas = true },
        new Sala { nome = "Corredor", i0 = 1, k0 = 4, w = 2, d = 5, piso = "SM_Bld_Floor_Carpet_01" },
        new Sala { nome = "Escritório", i0 = 0, k0 = 9, w = 6, d = 5, piso = "SM_Bld_Floor_Carpet_01", janelas = true },
        new Sala { nome = "Reunião", i0 = 6, k0 = 11, w = 3, d = 3, piso = "SM_Bld_Floor_Wood_01", janelas = true, vidro = true },
        new Sala { nome = "Arquivo", i0 = 6, k0 = 7, w = 3, d = 4, piso = "SM_Bld_Floor_Concrete_01" },
        new Sala { nome = "Elevador", i0 = 9, k0 = 7, w = 2, d = 2, piso = "SM_Bld_Floor_Tiles_01" },
    };

    // portas da rota: célula do lado de cá, direção para o lado de lá; cada porta ocupa 2 células da borda
    static readonly (int i, int k, int di, int dk)[] Portas =
    {
        (1, 3, 0, 1), // recepção > corredor
        (1, 8, 0, 1), // corredor > escritório
        (5, 11, 1, 0), // escritório > reunião
        (6, 11, 0, -1), // reunião > arquivo
        (8, 7, 1, 0), // arquivo > elevador
    };

    // o elevador fica na borda leste desta célula; o poço é a célula seguinte, fora da grade
    static readonly (int i, int k) CelulaElevador = (10, 7);

    // (sala, prefab, x, z, giro, altura): móveis, em metros
    static readonly (string, string, float, float, float, float)[] Moveis =
    {
        ("Recepção", "SM_Prop_Desk_05", 8f, 6f, -90f, 0f),
        ("Recepção", "SM_Prop_Chair_08", 9.2f, 6f, -90f, 0f),
        ("Recepção", "SM_Prop_Computer_Setup_01", 7.8f, 6.3f, 90f, 0.77f),
        ("Recepção", "SM_Prop_Couch_02", 1f, 2.8f, 90f, 0f),
        ("Recepção", "SM_Prop_Couch_02", 1f, 5.6f, 90f, 0f),
        ("Recepção", "SM_Prop_CoffeeTable_01", 2.4f, 4.2f, 90f, 0f),
        ("Recepção", "SM_Prop_Rug_01", 2.2f, 4.2f, 0f, 0f),
        ("Recepção", "SM_Prop_Plant_12", 1f, 9f, 0f, 0f),
        ("Recepção", "SM_Prop_Plant_06", 9.1f, 1f, 0f, 0f),
        ("Recepção", "SM_Prop_WaterCooler_01", 9.5f, 9.4f, 180f, 0f),

        ("Corredor", "SM_Prop_WaterCooler_01", 7.1f, 12.5f, -90f, 0f),
        ("Corredor", "SM_Prop_VendingMachine_01", 6.85f, 15.5f, -90f, 0f),
        ("Corredor", "SM_Prop_Photocopier_01", 3.05f, 15f, 90f, 0f),
        ("Corredor", "SM_Prop_Plant_04", 3f, 21.8f, 0f, 0f),
        ("Corredor", "SM_Prop_CardboardBox_Stack_01", 6.9f, 21.6f, 15f, 0f),
        ("Corredor", "SM_Prop_Fire_Extinguisher_01", 2.75f, 12f, 90f, 1.1f),

        ("Escritório", "SM_Prop_Photocopier_02", 14.4f, 24f, -90f, 0f),
        ("Escritório", "SM_Prop_Plant_12", 1f, 23.5f, 0f, 0f),
        ("Escritório", "SM_Prop_Whiteboard_02", 0.8f, 28f, 90f, 0f),
        ("Escritório", "SM_Prop_Cabinets_01", 14.5f, 34.4f, -90f, 0f),

        ("Reunião", "SM_Prop_Table_Conference_01", 18.75f, 31.6f, 90f, 0f),
        ("Reunião", "SM_Prop_Chair_06", 17.1f, 30.4f, 90f, 0f),
        ("Reunião", "SM_Prop_Chair_06", 17.1f, 32.8f, 90f, 0f),
        ("Reunião", "SM_Prop_Chair_06", 20.4f, 30.4f, -90f, 0f),
        ("Reunião", "SM_Prop_Chair_06", 20.4f, 32.8f, -90f, 0f),
        ("Reunião", "SM_Prop_TV_Wall_01", 22.3f, 31.6f, -90f, 1.4f),
        ("Reunião", "SM_Prop_Plant_04", 15.6f, 34.4f, 0f, 0f),

        ("Arquivo", "SM_Prop_Cabinets_01", 21.9f, 26.9f, -90f, 0f),
        ("Arquivo", "SM_Prop_CardboardBox_Stack_01", 15.9f, 26.6f, 0f, 0f),

        ("Elevador", "SM_Prop_Plant_02", 23.1f, 17.9f, 0f, 0f),
        ("Elevador", "SM_Prop_Bin_02", 27f, 21.9f, 0f, 0f),
    };

    // (sala, x, z, arma): onde os Agentes surgem, no lado da sala longe da porta de entrada
    static readonly (string, float, float, TipoArma)[] Agentes =
    {
        ("Recepção", 7.5f, 8.6f, TipoArma.Pistola),
        ("Recepção", 2.2f, 8.2f, TipoArma.Pistola),
        ("Corredor", 4f, 20.8f, TipoArma.Pistola),
        ("Escritório", 13.5f, 34f, TipoArma.Metralhadora),
        ("Escritório", 7f, 34f, TipoArma.Pistola),
        ("Escritório", 1.3f, 33.5f, TipoArma.Pistola),
        ("Reunião", 21.5f, 34.2f, TipoArma.Escopeta),
        ("Reunião", 21.6f, 28.4f, TipoArma.Pistola),
        ("Arquivo", 21.3f, 25.3f, TipoArma.Metralhadora),
        ("Arquivo", 16.9f, 18.4f, TipoArma.Escopeta),
        ("Elevador", 24.4f, 21.9f, TipoArma.Pistola),
        ("Elevador", 26.6f, 21.6f, TipoArma.Metralhadora),
    };

    // (prefab, x, z, giro): prédios do outro lado da rua, para as janelas não darem no vazio
    static readonly (string, float, float, float)[] Vizinhos =
    {
        ("SM_Bld_OfficeOld_Large_01", -20f, 30f, 90f),
        ("SM_Bld_OfficeSquare_02", -20f, 10f, 90f),
        ("SM_Bld_OfficeOld_Small_01", 5f, 57f, 180f),
        ("SM_Bld_OfficeSquare_04", 25f, 57f, 180f),
        ("SM_Bld_OfficeOld_Large_02", 50f, 30f, -90f),
        ("SM_Bld_OfficeSquare_02", 5f, -22f, 0f),
    };

    static Dictionary<(int, int), Sala> grade;

    [MenuItem("Despertar/Montar escritório")]
    public static void Montar()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MontarPrototipo.Iluminacao();

        var predio = new GameObject("Escritório").transform;
        grade = new Dictionary<(int, int), Sala>();
        foreach (var s in Salas)
        {
            s.raiz = new GameObject("Sala - " + s.nome).transform;
            s.raiz.SetParent(predio);
            for (int i = s.i0; i < s.i0 + s.w; i++)
                for (int k = s.k0; k < s.k0 + s.d; k++)
                    grade[(i, k)] = s;
        }

        foreach (var s in Salas)
        {
            PisoETeto(s.raiz, s.i0, s.k0, s.w, s.d, s.piso);
            Luzes(s);
        }
        Paredes();
        var (portaE, portaD) = CriarElevador(SalaPorNome("Elevador").raiz);
        foreach (var (sala, nome, x, z, giro, y) in Moveis)
            Colocar(nome, new Vector3(x, y, z), giro, SalaPorNome(sala).raiz);
        Mesas(SalaPorNome("Escritório").raiz);
        Servidores(SalaPorNome("Arquivo").raiz);
        Rua();
        Physics.SyncTransforms();

        MontarPrototipo.CriarJogador(new Vector3(5f, 0.05f, 1.5f), 0f);
        var fase = MontarPrototipo.CriarJogo().AddComponent<Fase>();
        PreencherFase(fase, portaE, portaD);
        MontarPrototipo.Salvar(cena, CaminhoCena, -1);
    }

    static Sala SalaPorNome(string nome) => System.Array.Find(Salas, s => s.nome == nome);

    static void PisoETeto(Transform pai, int i0, int k0, int w, int d, string piso)
    {
        for (int i = i0; i < i0 + w; i++)
            for (int k = k0; k < k0 + d; k++)
            {
                var canto = new Vector3((i + 1) * C, 0f, k * C); // o pivô das peças fica nesse canto
                SemColisor(Colocar(piso, canto, 0f, pai));
                var teto = Colocar("SM_Bld_Ceiling_Panel_01", canto + Vector3.up * Altura, 0f, pai);
                // o forro só tem a face de baixo: sem isso, o sol atravessa e ilumina o chão
                foreach (var r in teto.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = ShadowCastingMode.TwoSided;
            }

        // um bloco de colisão inteiro para o piso e outro para o teto, sem emendas entre peças
        var centro = new Vector3((i0 + w / 2f) * C, 0f, (k0 + d / 2f) * C);
        var tamanho = new Vector3(w * C, 1f, d * C);
        Caixa("Piso (colisão)", centro + Vector3.down * 0.5f, tamanho, pai);
        Caixa("Teto (colisão)", centro + Vector3.up * (Altura + 0.5f), tamanho, pai);
    }

    // uma luminária de forro com luz a cada 2x2 células
    static void Luzes(Sala s)
    {
        for (int a = 0; a * 2 < s.w; a++)
            for (int b = 0; b * 2 < s.d; b++)
            {
                float x = (s.i0 + a * 2 + Mathf.Min(2, s.w - a * 2) / 2f) * C;
                float z = (s.k0 + b * 2 + Mathf.Min(2, s.d - b * 2) / 2f) * C;
                Luz(new Vector3(x, 0f, z), s.raiz);
            }
    }

    static void Luz(Vector3 ponto, Transform pai)
    {
        Colocar("SM_Bld_Ceiling_Panel_Light_01", new Vector3(ponto.x, Altura - 0.045f, ponto.z), 0f, pai);
        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.transform.SetParent(pai);
        luz.transform.position = new Vector3(ponto.x, Altura - 0.6f, ponto.z);
        luz.type = LightType.Point;
        luz.range = 7f;
        luz.intensity = 3f;
        luz.color = new Color(0.88f, 0.94f, 1f);
        luz.shadows = LightShadows.None;
    }

    // Toda borda entre salas diferentes (ou entre sala e rua) ganha parede, uma peça virada para cada lado.
    static void Paredes()
    {
        // bordas que são porta: (célula, direção) -> é a primeira das duas células da porta?
        var portas = new Dictionary<((int, int), (int, int)), bool>();
        foreach (var (i, k, di, dk) in Portas)
        {
            var (ai, ak) = (dk != 0 ? 1 : 0, di != 0 ? 1 : 0); // passo ao longo da borda
            portas[((i, k), (di, dk))] = true;
            portas[((i + ai, k + ak), (di, dk))] = false;
            portas[((i + di, k + dk), (-di, -dk))] = true;
            portas[((i + di + ai, k + dk + ak), (-di, -dk))] = false;
        }

        foreach (var par in grade)
        {
            var (i, k) = par.Key;
            var sala = par.Value;
            foreach (var dir in new[] { (1, 0), (0, 1), (-1, 0), (0, -1) })
            {
                var viz = (i + dir.Item1, k + dir.Item2);
                grade.TryGetValue(viz, out var outra);
                if (outra == sala)
                    continue;
                if ((i, k) == CelulaElevador && dir == (1, 0))
                    continue; // aqui vai o elevador

                if (portas.TryGetValue(((i, k), dir), out bool primeira))
                {
                    if (primeira)
                    {
                        bool vidro = sala.vidro || outra.vidro;
                        Peca(vidro ? PortaVidro : Porta, i, k, dir, 2, sala.raiz);
                    }
                    continue;
                }

                if (outra == null)
                {
                    // fachada: por dentro janela ou parede; por fora, a mesma abertura numa parede lisa
                    bool janela = sala.janelas && (i + k) % 2 == 0;
                    Peca(janela ? Janela : Parede, i, k, dir, 1, sala.raiz);
                    Peca(janela ? Janela : ParedeFora, viz.Item1, viz.Item2, (-dir.Item1, -dir.Item2), 1, sala.raiz);
                }
                else
                {
                    Peca(sala.vidro || outra.vidro ? Vidro : Parede, i, k, dir, 1, sala.raiz);
                }
            }
        }
    }

    // Uma peça de parede na borda da célula (i, k) do lado `dir`, com a frente virada para dentro da célula.
    // `n` = quantas células a peça ocupa ao longo da borda (a partir desta, no sentido +x ou +z).
    static GameObject Peca(string nome, int i, int k, (int, int) dir, int n, Transform pai)
    {
        Vector3 pos;
        float giro;
        switch (dir)
        {
            case (0, -1): giro = 0f; pos = new Vector3((i + n) * C, 0f, k * C); break;
            case (0, 1): giro = 180f; pos = new Vector3(i * C, 0f, (k + 1) * C); break;
            case (1, 0): giro = -90f; pos = new Vector3((i + 1) * C, 0f, (k + n) * C); break;
            default: giro = 90f; pos = new Vector3(i * C, 0f, k * C); break;
        }
        return Colocar(nome, pos, giro, pai);
    }

    // A parede do elevador na borda leste da última sala, e o poço atrás dela com piso, forro e luz.
    static (Transform, Transform) CriarElevador(Transform pai)
    {
        var (i, k) = CelulaElevador;
        var elevador = Peca(Elevador, i, k, (1, 0), 1, pai);
        PisoETeto(pai, i + 1, k, 1, 1, "SM_Bld_Floor_Tiles_01");
        Luz(new Vector3((i + 1.5f) * C, 0f, (k + 0.5f) * C), pai);

        // as folhas da porta precisam se mexer: desfaz o prefab para poder animá-las
        PrefabUtility.UnpackPrefabInstance(elevador, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        Transform esquerda = null, direita = null;
        foreach (var t in elevador.GetComponentsInChildren<Transform>())
        {
            if (t.name.EndsWith("Door_L"))
                esquerda = t;
            else if (t.name.EndsWith("Door_R"))
                direita = t;
        }
        foreach (var t in new[] { esquerda, direita })
            if (t != null)
                t.gameObject.isStatic = false;
        elevador.isStatic = false;
        return (esquerda, direita);
    }

    // ilhas de baias: duas mesas de frente uma para a outra, divisória no meio, computador e cadeira
    static void Mesas(Transform pai)
    {
        var rng = new System.Random(7);
        foreach (float z in new[] { 26.3f, 31.7f }) // com as cadeiras, sobra um corredor de 2 m entre as fileiras
            foreach (float x in new[] { 3f, 7f, 11f })
            {
                Colocar("SM_Prop_CubicalWall_01", new Vector3(x - 1f, 0f, z), 0f, pai);
                foreach (int lado in new[] { -1, 1 })
                {
                    float giro = lado < 0 ? 0f : 180f;
                    Colocar("SM_Prop_Desk_01", new Vector3(x, 0f, z + lado * 0.55f), giro, pai);
                    // o computador tem a frente (teclado) no +z do modelo: vira para a cadeira, de costas para a divisória
                    Colocar("SM_Prop_Computer_Setup_01", new Vector3(x + (float)(rng.NextDouble() - 0.5) * 0.6f, 0.77f, z + lado * 0.5f), giro + 180f, pai);
                    Colocar("SM_Prop_Chair_02", new Vector3(x + 0.2f, 0f, z + lado * 1.35f), giro + (float)rng.NextDouble() * 40f - 20f, pai);
                }
            }
    }

    // fileiras de armários de servidor, deixando um corredor da porta de entrada até a saída
    static void Servidores(Transform pai)
    {
        for (float z = 19.5f; z <= 24.5f; z += 1f)
            Colocar("SM_Prop_Server_Cabinet_01_Full", new Vector3(16f, 0f, z), 90f, pai);
        for (float z = 21f; z <= 25f; z += 1f)
            Colocar("SM_Prop_Server_Cabinet_02_Full", new Vector3(19.2f, 0f, z), -90f, pai);
    }

    static void PreencherFase(Fase fase, Transform portaE, Transform portaD)
    {
        var lista = new List<Fase.Sala>();
        foreach (var s in Salas)
        {
            var pontos = new List<Vector3>();
            var armas = new List<TipoArma>();
            foreach (var (sala, x, z, arma) in Agentes)
                if (sala == s.nome)
                {
                    pontos.Add(Livre(new Vector3(x, 0f, z)));
                    armas.Add(arma);
                }
            var centro = new Vector3((s.i0 + s.w / 2f) * C, 1.5f, (s.k0 + s.d / 2f) * C);
            lista.Add(new Fase.Sala
            {
                nome = s.nome,
                area = new Bounds(centro, new Vector3(s.w * C - 0.6f, 4f, s.d * C - 0.6f)),
                pontos = pontos.ToArray(),
                armas = armas.ToArray(),
            });
        }
        fase.salas = lista.ToArray();
        var (i, k) = CelulaElevador;
        fase.saida = new Bounds(new Vector3((i + 1.5f) * C, 1.5f, (k + 0.5f) * C), new Vector3(C - 0.6f, 4f, C - 0.6f));
        fase.portaEsquerda = portaE;
        fase.portaDireita = portaD;
    }

    // se o ponto cair dentro de algum móvel, procura um espaço livre por perto (cápsula do Agente)
    static Vector3 Livre(Vector3 pe)
    {
        var p = pe;
        for (int t = 0; t < 32 && Ocupado(p); t++)
            p = pe + Quaternion.Euler(0f, t * 47f, 0f) * Vector3.forward * (0.6f + t * 0.15f);
        if (Ocupado(p))
            Debug.LogWarning("[Despertar] sem espaço livre para um Agente perto de " + pe);
        return p;
    }

    static bool Ocupado(Vector3 pe) =>
        Physics.CheckCapsule(pe + Vector3.up * 0.55f, pe + Vector3.up * 1.5f, 0.5f, ~0, QueryTriggerInteraction.Ignore);

    static void Rua()
    {
        var raiz = new GameObject("Rua").transform;
        var chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
        chao.name = "Asfalto";
        chao.transform.SetParent(raiz);
        chao.transform.position = new Vector3(14f, -0.02f, 17f);
        chao.transform.localScale = new Vector3(30f, 1f, 30f);
        Object.DestroyImmediate(chao.GetComponent<Collider>());
        chao.GetComponent<Renderer>().sharedMaterial = MontarPrototipo.Mat("Asfalto", "Universal Render Pipeline/Lit", m =>
        {
            m.SetColor("_BaseColor", new Color(0.2f, 0.2f, 0.21f));
            m.SetFloat("_Smoothness", 0.15f);
        });
        chao.isStatic = true;

        foreach (var (nome, x, z, giro) in Vizinhos)
        {
            var go = Colocar(nome, Vector3.zero, giro, raiz);
            if (go == null)
                continue;
            var b = MontarPrototipo.Limites(go);
            go.transform.position = new Vector3(x - b.center.x, 0f, z - b.center.z);
        }
    }

    // ------------------------------------------------------------------ utilidades

    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

    // Procura primeiro no Office (vários nomes se repetem nos outros pacotes), depois no resto.
    static GameObject Carregar(string nome)
    {
        if (cache.TryGetValue(nome, out var guardado) && guardado != null)
            return guardado;
        foreach (var guid in AssetDatabase.FindAssets(nome + " t:Prefab", new[] { "Assets/Synty/PolygonOffice" }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(caminho) == nome)
                return cache[nome] = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        }
        return cache[nome] = MontarPrototipo.Carregar(nome);
    }

    static GameObject Colocar(string nome, Vector3 pos, float giro, Transform pai)
    {
        var prefab = Carregar(nome);
        if (prefab == null)
            return null;
        var go = MontarPrototipo.Instanciar(prefab, pos, giro, pai);
        go.isStatic = true;
        return go;
    }

    static void SemColisor(GameObject go)
    {
        if (go == null)
            return;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
    }

    static void Caixa(string nome, Vector3 centro, Vector3 tamanho, Transform pai)
    {
        var c = new GameObject(nome).AddComponent<BoxCollider>();
        c.transform.SetParent(pai);
        c.transform.position = centro;
        c.size = tamanho;
    }
}
