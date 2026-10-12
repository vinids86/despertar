using UnityEngine;
using UnityEngine.InputSystem;

// Interface mínima (OnGUI): mira, anel de tempo do parry, barra da câmera lenta, vida, onda e avisos.
// O anel funciona como num jogo de ritmo: ele fecha conforme a bala chega; aperte quando encostar no anel fixo.
public class HUD : MonoBehaviour
{
    const string Ajuda =
        "WASD mover · Espaço pular · Shift dash\n" +
        "Botão esquerdo atirar\n" +
        "Botão direito (segurar) câmera lenta\n" +
        "F aparar: quando o anel fechar\n" +
        "R reiniciar · H ajuda · Esc soltar o mouse";

    const float RaioAlvo = 26f;
    const float RaioMaximo = 140f;
    const float Horizonte = 0.6f; // segundos de jogo antes do impacto em que o anel aparece

    Texture2D anel;
    GUIStyle grande, pequeno;
    float ajudaAte;

    void Start()
    {
        anel = CriarAnel(256, 6f);
        ajudaAte = Time.unscaledTime + 12f;
    }

    static Texture2D CriarAnel(int tamanho, float espessura)
    {
        var t = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float c = (tamanho - 1) / 2f;
        float raio = c - 2f - espessura / 2f;
        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                float d = Mathf.Abs(Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) - raio);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(espessura / 2f - d + 0.5f)));
            }
        }
        t.Apply();
        return t;
    }

    void OnGUI()
    {
        var j = Jogador.I;
        if (j == null)
            return;
        if (grande == null)
        {
            grande = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            pequeno = new GUIStyle(GUI.skin.label) { fontSize = 15 };
        }

        float w = Screen.width, h = Screen.height;
        var centro = new Vector2(w / 2f, h / 2f);

        if (j.FlashDano > 0f)
            Retangulo(new Rect(0, 0, w, h), new Color(1f, 0f, 0f, j.FlashDano * 0.35f));
        if (j.Morto)
            Retangulo(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.45f));

        // mira
        Retangulo(new Rect(centro.x - 1.5f, centro.y - 1.5f, 3f, 3f), Color.white);

        // anel de tempo da bala mais próxima
        var bala = Bala.MaisProxima(j.Centro, out float tempo);
        if (bala != null && tempo < Horizonte && !j.Morto)
        {
            Anel(centro, RaioAlvo, new Color(1f, 1f, 1f, 0.35f));
            float r = RaioAlvo + tempo / Horizonte * (RaioMaximo - RaioAlvo);
            var cor = tempo <= j.janelaPerfeita ? new Color(1f, 0.85f, 0.3f) : tempo <= j.janela ? Color.white : new Color(1f, 0.35f, 0.3f);
            Anel(centro, r, cor);
            MarcarBala(bala.transform.position, cor);
        }

        // barra da câmera lenta
        var tb = TempoBala.I;
        if (tb != null)
        {
            const float largura = 320f;
            var fundo = new Rect(centro.x - largura / 2f, h - 48f, largura, 10f);
            Retangulo(fundo, new Color(0f, 0f, 0f, 0.5f));
            Retangulo(new Rect(fundo.x, fundo.y, largura * tb.Energia / tb.maximo, fundo.height),
                tb.Ativo ? new Color(0.6f, 1f, 0.7f) : new Color(0.3f, 0.8f, 0.45f, 0.9f));
            Retangulo(new Rect(fundo.x + largura * tb.minimoParaAtivar / tb.maximo, fundo.y - 2f, 2f, fundo.height + 4f), new Color(1f, 1f, 1f, 0.5f));
            GUI.Label(new Rect(fundo.x, fundo.y - 22f, largura, 20f), "TEMPO", pequeno);
        }

        // vida
        for (int i = 0; i < j.vidaMaxima; i++)
            Retangulo(new Rect(30f + i * 22f, h - 50f, 16f, 16f), i < j.Vida ? new Color(0.9f, 0.95f, 1f) : new Color(1f, 1f, 1f, 0.15f));

        if (Ondas.I != null)
            GUI.Label(new Rect(30f, 24f, 500f, 22f), "onda " + Mathf.Max(1, Ondas.I.Onda) + "/" + Ondas.I.Total + "   agentes: " + Inimigo.Vivos.Count, pequeno);
        else if (Fase.I != null)
            GUI.Label(new Rect(30f, 24f, 500f, 22f), Fase.I.Status, pequeno);

        if (Time.unscaledTime < j.AvisoAte)
        {
            grande.normal.textColor = j.AvisoCor;
            GUI.Label(new Rect(0f, h * 0.27f, w, 120f), j.Aviso, grande);
        }

        var kb = Keyboard.current;
        if (Time.unscaledTime < ajudaAte || (kb != null && kb.hKey.isPressed))
            GUI.Label(new Rect(30f, 56f, 560f, 120f), Ajuda, pequeno);
    }

    // Um quadradinho onde a bala está na tela (ou na borda, se estiver fora da visão).
    void MarcarBala(Vector3 posicao, Color cor)
    {
        var cam = Camera.main;
        if (cam == null)
            return;
        Vector3 p = cam.WorldToScreenPoint(posicao);
        if (p.z < 0f)
            p = new Vector3(Screen.width - p.x, Screen.height - p.y, 0f) * 1f;
        float x = Mathf.Clamp(p.x, 30f, Screen.width - 30f);
        float y = Mathf.Clamp(Screen.height - p.y, 30f, Screen.height - 30f);
        Anel(new Vector2(x, y), 9f, cor);
    }

    void Anel(Vector2 c, float r, Color cor)
    {
        GUI.color = cor;
        GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), anel);
        GUI.color = Color.white;
    }

    static void Retangulo(Rect r, Color cor)
    {
        GUI.color = cor;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
