using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Interface feita com as peças do pacote INTERFACE Sci-Fi Soldier HUD (Synty): mira, anel de tempo do parry,
// barra da câmera lenta, vida, munição, onda/objetivo, avisos e ajuda.
// O anel funciona como num jogo de ritmo: ele fecha conforme a bala chega; aperte quando encostar no anel fixo.
// Prefabs, sprites e fontes são preenchidos pelo montador da cena (MontarPrototipo.PreencherHUD).
public class HUD : MonoBehaviour
{
    const string Ajuda =
        "<b>WASD</b> mover · <b>Espaço</b> pular · <b>Shift</b> dash\n" +
        "<b>Botão esquerdo</b> atirar · <b>R</b> recarregar\n" +
        "<b>Botão direito</b> (segurar) câmera lenta\n" +
        "<b>F</b> aparar: quando o anel fechar\n" +
        "<b>Backspace</b> reiniciar · <b>H</b> ajuda · <b>Esc</b> soltar o mouse";

    // em unidades do canvas: referência 3840x2160, a mesma das peças do pacote
    const float RaioAlvo = 92f;
    const float RaioMaximo = 420f;
    const float Horizonte = 0.6f; // segundos de jogo antes do impacto em que o anel aparece

    public TMP_FontAsset fonteTitulo, fonteTexto;
    public GameObject prefabMira, prefabTempo, prefabArma, prefabDano, prefabFaixa, prefabEtiqueta;
    public Sprite anel, marca, segmento, caixa, iconeVida, iconeTempo, iconeArma;

    Canvas canvas;
    Material sombraTitulo, sombraTexto;
    Image escuro, anelAlvo, anelBala, marcaBala, tempoCheio;
    Animator mira, dano, faixa, etiqueta;
    Slider tempo;
    Image[] vida;
    Toggle[] balas;
    TMP_Text municao, nomeArma, status, aviso, textoFaixa;
    GameObject ajuda;
    float ajudaAte, avisoDesde, avisoVisto;
    int vidaVista;

    void Start()
    {
        var j = Jogador.I;
        ajudaAte = Time.unscaledTime + 12f;
        vidaVista = j != null ? j.Vida : 0;
        ShaderUtilities.GetShaderPropertyIDs();
        sombraTitulo = Sombra(fonteTitulo);
        sombraTexto = Sombra(fonteTexto);

        var go = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escala = go.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(3840f, 2160f);
        escala.matchWidthOrHeight = 0.5f;
        var raiz = go.transform;

        // tela escura ao morrer e o vinhete de dano, na tela toda
        escuro = Imagem("Escuro", raiz, null, new Color(0f, 0f, 0f, 0.45f));
        escuro.rectTransform.anchorMin = Vector2.zero;
        escuro.rectTransform.anchorMax = Vector2.one;
        escuro.rectTransform.sizeDelta = Vector2.zero;
        dano = Colocar(prefabDano, raiz).GetComponent<Animator>();

        // anel do parry, marca da bala e mira, no centro
        anelAlvo = Imagem("Anel alvo", raiz, anel, new Color(1f, 1f, 1f, 0.35f));
        anelAlvo.rectTransform.sizeDelta = Vector2.one * RaioAlvo * 2f;
        anelBala = Imagem("Anel da bala", raiz, anel, Color.white);
        marcaBala = Imagem("Marca da bala", raiz, marca, Color.white);
        marcaBala.rectTransform.sizeDelta = new Vector2(60f, 60f);
        mira = Colocar(prefabMira, raiz).GetComponent<Animator>();
        Ancorar(mira.transform, new Vector2(0.5f, 0.5f), Vector2.zero, 1f);

        // câmera lenta: barra embaixo, no meio, com um risco no mínimo para ativar
        var barra = Colocar(prefabTempo, raiz).transform;
        Ancorar(barra, new Vector2(0.5f, 0f), new Vector2(0f, 60f), 1f);
        barra.Find("Icon").GetComponent<Image>().sprite = iconeTempo;
        tempo = barra.GetComponentInChildren<Slider>();
        tempoCheio = tempo.fillRect.GetComponent<Image>();
        var rotulo = Texto("Rótulo", tempo.transform, fonteTitulo, 40f, TextAlignmentOptions.BottomLeft);
        rotulo.text = "TEMPO";
        rotulo.rectTransform.anchorMin = rotulo.rectTransform.anchorMax = new Vector2(0f, 1f);
        rotulo.rectTransform.pivot = Vector2.zero;
        rotulo.rectTransform.anchoredPosition = new Vector2(6f, 14f);
        rotulo.rectTransform.sizeDelta = new Vector2(400f, 60f);
        var tb = TempoBala.I;
        if (tb != null)
        {
            var risco = Imagem("Mínimo", tempo.transform, null, new Color(1f, 1f, 1f, 0.7f));
            risco.rectTransform.anchorMin = risco.rectTransform.anchorMax = new Vector2(tb.minimoParaAtivar / tb.maximo, 0.5f);
            risco.rectTransform.sizeDelta = new Vector2(6f, 40f);
        }

        // vida: um segmento por ponto, embaixo à esquerda
        var painel = new GameObject("Vida", typeof(RectTransform)).transform;
        painel.SetParent(raiz, false);
        Ancorar(painel, Vector2.zero, new Vector2(80f, 80f), 1f);
        var icone = Imagem("Ícone", painel, iconeVida, Color.white);
        Ancorar(icone.transform, new Vector2(0f, 0.5f), Vector2.zero, 1f);
        icone.rectTransform.sizeDelta = new Vector2(80f, 80f);
        vida = new Image[j != null ? j.vidaMaxima : 0];
        for (int i = 0; i < vida.Length; i++)
        {
            vida[i] = Imagem("Ponto", painel, segmento, Color.white);
            Ancorar(vida[i].transform, new Vector2(0f, 0.5f), new Vector2(110f + i * 76f, 0f), 1f);
            vida[i].rectTransform.sizeDelta = new Vector2(88f, 44f);
        }

        // arma: nome, ícone, uma barrinha por bala do pente e a contagem (pente / reserva)
        var arma = Colocar(prefabArma, raiz).transform;
        Ancorar(arma, new Vector2(1f, 0f), new Vector2(-80f, 60f), 1f);
        arma.Find("ICON").GetComponent<Image>().sprite = iconeArma;
        nomeArma = arma.Find("Label_WeaponName").GetComponent<TMP_Text>();
        municao = arma.Find("Label_AmmoCount").GetComponent<TMP_Text>();
        municao.fontSize = 56f;
        municao.alignment = TextAlignmentOptions.BottomRight;
        municao.textWrappingMode = TextWrappingModes.NoWrap;
        municao.rectTransform.sizeDelta = new Vector2(200f, 60f);
        var lista = (RectTransform)arma.Find("Ammo List");
        lista.sizeDelta = new Vector2(-200f, lista.sizeDelta.y);
        lista.anchoredPosition = new Vector2(-100f, lista.anchoredPosition.y);
        balas = new Toggle[lista.childCount];
        for (int i = 0; i < balas.Length; i++)
        {
            balas[i] = lista.GetChild(i).GetComponent<Toggle>();
            balas[i].gameObject.SetActive(j != null && i < j.pente);
        }

        // onda ou objetivo, em cima à esquerda; a ajuda logo abaixo
        etiqueta = Colocar(prefabEtiqueta, raiz).GetComponent<Animator>();
        Ancorar(etiqueta.transform, new Vector2(0f, 1f), new Vector2(80f, -80f), 1f);
        etiqueta.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        status = etiqueta.GetComponentInChildren<TMP_Text>(true);

        var fundo = Imagem("Ajuda", raiz, caixa, new Color(0f, 0.16f, 0.32f, 0.82f));
        fundo.type = Image.Type.Sliced;
        Ancorar(fundo.transform, new Vector2(0f, 1f), new Vector2(80f, -190f), 1f);
        var coluna = fundo.gameObject.AddComponent<VerticalLayoutGroup>();
        coluna.padding = new RectOffset(44, 52, 26, 32);
        var ajuste = fundo.gameObject.AddComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var textoAjuda = Texto("Texto", fundo.transform, fonteTexto, 40f, TextAlignmentOptions.TopLeft);
        textoAjuda.text = Ajuda;
        textoAjuda.lineSpacing = 12f;
        ajuda = fundo.gameObject;

        // avisos: a faixa grande para os que ficam na tela (fim da simulação, desconectado) e texto para os rápidos
        faixa = Colocar(prefabFaixa, raiz).GetComponent<Animator>();
        var rf = (RectTransform)faixa.transform;
        rf.anchorMin = new Vector2(0f, 0.6f);
        rf.anchorMax = new Vector2(1f, 0.6f);
        textoFaixa = faixa.GetComponentInChildren<TMP_Text>(true);
        faixa.gameObject.SetActive(false);
        aviso = Texto("Aviso", raiz, fonteTitulo, 104f, TextAlignmentOptions.Center);
        aviso.rectTransform.sizeDelta = new Vector2(3200f, 400f);
    }

    void Update()
    {
        var j = Jogador.I;
        if (j == null || canvas == null)
            return;
        float agora = Time.unscaledTime;

        escuro.enabled = j.Morto;
        if (j.Vida < vidaVista)
            dano.SetTrigger("Hit");
        vidaVista = j.Vida;

        // mira: no tempo lento ela se abre
        mira.gameObject.SetActive(!j.Morto);
        var tb = TempoBala.I;
        if (tb != null && !j.Morto)
            mira.SetFloat("Aim", tb.Mistura);

        // anel de tempo da bala mais próxima
        float tempoBala = 0f;
        var bala = j.Morto ? null : Bala.MaisProxima(j.Centro, out tempoBala);
        bool mostrar = bala != null && tempoBala < Horizonte;
        anelAlvo.enabled = anelBala.enabled = marcaBala.enabled = mostrar;
        if (mostrar)
        {
            float r = RaioAlvo + tempoBala / Horizonte * (RaioMaximo - RaioAlvo);
            var cor = tempoBala <= j.janelaPerfeita ? new Color(1f, 0.85f, 0.3f) : tempoBala <= j.janela ? Color.white : new Color(1f, 0.35f, 0.3f);
            anelBala.rectTransform.sizeDelta = Vector2.one * r * 2f;
            anelBala.color = cor;
            MarcarBala(bala.transform.position, cor);
        }

        if (tb != null)
        {
            tempo.SetValueWithoutNotify(tb.Energia / tb.maximo);
            tempoCheio.color = tb.Ativo ? new Color(0.6f, 1f, 0.75f) : new Color(0.3f, 0.85f, 0.55f);
        }

        bool critico = j.Vida <= 2;
        for (int i = 0; i < vida.Length; i++)
            vida[i].color = i >= j.Vida ? new Color(0f, 0.13f, 0.25f, 0.75f) : critico ? new Color(1f, 0.35f, 0.3f) : new Color(0.92f, 0.96f, 1f);

        // munição: na recarga, as barrinhas enchem junto com o som
        int cheias = j.NoPente;
        if (j.Recarregando)
            cheias = Mathf.RoundToInt(Mathf.Lerp(j.NoPente, j.NoPente + Mathf.Min(j.pente - j.NoPente, j.Reserva), j.ProgressoRecarga));
        for (int i = 0; i < balas.Length; i++)
            balas[i].SetIsOnWithoutNotify(i < cheias);
        municao.text = j.NoPente + "<size=60%><alpha=#AA> / " + j.Reserva;
        municao.color = j.NoPente == 0 ? new Color(1f, 0.4f, 0.35f) : j.NoPente <= j.pente / 4 ? new Color(1f, 0.75f, 0.4f) : Color.white;
        bool semMunicao = j.NoPente == 0 && j.Reserva == 0 && !j.Recarregando;
        nomeArma.text = semMunicao ? "SEM MUNIÇÃO" : "PISTOLA";
        nomeArma.color = semMunicao ? new Color(1f, 0.4f, 0.35f) : Color.white;

        string texto = Ondas.I != null ? "onda " + Mathf.Max(1, Ondas.I.Onda) + "/" + Ondas.I.Total + "   agentes: " + Inimigo.Vivos.Count
            : Fase.I != null ? Fase.I.Status : "";
        etiqueta.gameObject.SetActive(texto.Length > 0);
        if (texto.Length > 0)
            etiqueta.SetBool("Active", true); // sem isso a etiqueta do pacote sai sozinha, como um evento
        status.text = texto;

        var kb = Keyboard.current;
        ajuda.SetActive(agora < ajudaAte || (kb != null && kb.hKey.isPressed));

        Avisos(j, agora);
    }

    // Avisos rápidos entram com um tranco e somem no fim; os que ficam na tela usam a faixa, com a 2ª linha embaixo.
    void Avisos(Jogador j, float agora)
    {
        if (j.AvisoAte != avisoVisto)
        {
            avisoVisto = j.AvisoAte;
            avisoDesde = agora;
        }
        float resta = j.AvisoAte - agora;
        bool fixo = resta > 60f;
        aviso.enabled = resta > 0f;
        if (faixa.gameObject.activeSelf != fixo)
            faixa.gameObject.SetActive(fixo);
        if (!aviso.enabled)
            return;

        var linhas = j.Aviso.Split('\n');
        var cor = j.AvisoCor;
        if (fixo)
        {
            faixa.SetBool("Active", true);
            textoFaixa.text = linhas[0];
            textoFaixa.color = cor;
            aviso.text = linhas.Length > 1 ? linhas[1] : "";
            aviso.fontSize = 60f;
            aviso.color = Color.white;
            aviso.rectTransform.anchorMin = aviso.rectTransform.anchorMax = new Vector2(0.5f, 0.49f);
            aviso.rectTransform.localScale = Vector3.one;
            return;
        }
        aviso.text = j.Aviso;
        aviso.fontSize = 104f;
        cor.a = Mathf.Clamp01(resta / 0.2f);
        aviso.color = cor;
        aviso.rectTransform.anchorMin = aviso.rectTransform.anchorMax = new Vector2(0.5f, 0.73f);
        aviso.rectTransform.localScale = Vector3.one * (1f + 0.3f * Mathf.Clamp01(1f - (agora - avisoDesde) / 0.12f));
    }

    // A marca onde a bala está na tela (ou na borda, se estiver fora da visão).
    void MarcarBala(Vector3 posicao, Color cor)
    {
        var cam = Camera.main;
        if (cam == null)
            return;
        Vector3 p = cam.WorldToScreenPoint(posicao);
        if (p.z < 0f)
            p = new Vector3(Screen.width - p.x, Screen.height - p.y, 0f);
        float borda = 60f * canvas.scaleFactor;
        var tela = new Vector2(Mathf.Clamp(p.x, borda, Screen.width - borda), Mathf.Clamp(p.y, borda, Screen.height - borda));
        marcaBala.rectTransform.anchoredPosition = (tela - new Vector2(Screen.width, Screen.height) / 2f) / canvas.scaleFactor;
        marcaBala.color = cor;
    }

    // Instancia uma peça do pacote trocando as fontes (as dele não têm acentos)
    // e fazendo as animações ignorarem a câmera lenta.
    GameObject Colocar(GameObject prefab, Transform pai)
    {
        var go = Instantiate(prefab, pai, false);
        foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
            Fonte(t, t.font != null && (t.font.name.Contains("Orbitron") || t.font.name.Contains("Header")) ? fonteTitulo : fonteTexto);
        foreach (var a in go.GetComponentsInChildren<Animator>(true))
            a.updateMode = AnimatorUpdateMode.UnscaledTime;
        return go;
    }

    void Fonte(TMP_Text t, TMP_FontAsset fonte)
    {
        t.font = fonte;
        t.fontSharedMaterial = fonte == fonteTitulo ? sombraTitulo : sombraTexto;
    }

    TMP_Text Texto(string nome, Transform pai, TMP_FontAsset fonte, float tamanho, TextAlignmentOptions alinhamento)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(pai, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        Fonte(t, fonte);
        t.fontSize = tamanho;
        t.alignment = alinhamento;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    static Image Imagem(string nome, Transform pai, Sprite sprite, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(pai, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = cor;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    static void Ancorar(Transform t, Vector2 ancora, Vector2 posicao, float escala)
    {
        var r = (RectTransform)t;
        r.anchorMin = r.anchorMax = r.pivot = ancora;
        r.anchoredPosition = posicao;
        r.localScale = Vector3.one * escala;
    }

    // Sombra embaixo das letras, para ler em cima das paredes claras do escritório.
    static Material Sombra(TMP_FontAsset fonte)
    {
        var m = new Material(fonte.material);
        m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.65f));
        m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
        m.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.2f);
        m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.4f);
        return m;
    }
}
