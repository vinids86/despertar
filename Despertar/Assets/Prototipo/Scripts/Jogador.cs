using UnityEngine;
using UnityEngine.InputSystem;

// Primeira pessoa: anda, pula, dash, atira, recarrega, segura a câmera lenta e apara balas.
// O jogador não fica lento na câmera lenta: ele é mais rápido que a simulação.
[RequireComponent(typeof(CharacterController))]
public class Jogador : MonoBehaviour
{
    public static Jogador I;

    [Header("Movimento")]
    public float velocidade = 7f;
    public float gravidade = -24f;
    public float forcaPulo = 8f;
    public float sensibilidade = 0.1f;
    public float passada = 2.2f; // metros andados entre um som de passo e outro

    [Header("Dash")]
    public float dashVelocidade = 26f;
    public float dashDuracao = 0.16f;
    public float dashRecarga = 0.6f;

    [Header("Tiro")]
    public float cadencia = 0.15f;
    public float alcance = 200f;

    [Header("Munição")]
    public int pente = 12;
    public int reservaInicial = 60; // balas de sobra, fora do pente; acabou, só resta aparar
    public float tempoRecarga = 0.6f; // tempo real

    [Header("Aparar (segundos de jogo até o impacto)")]
    public float janela = 0.16f;
    public float janelaPerfeita = 0.06f;
    public float esperaAposErro = 0.35f; // tempo real

    [Header("Energia da câmera lenta (só se ganha em tempo normal)")]
    public float energiaParry = 12f;
    public float energiaPerfeito = 30f;
    public float energiaEsquiva = 15f;

    [Header("Vida")]
    public int vidaMaxima = 6;

    [Header("Arma em primeira pessoa")]
    public Transform arma;
    public Transform boca;

    public int Vida { get; private set; }
    public bool Morto { get; private set; }
    public float FlashDano { get; private set; }
    public string Aviso { get; private set; } = "";
    public Color AvisoCor { get; private set; } = Color.white;
    public float AvisoAte { get; private set; }
    public int NoPente { get; private set; }
    public int Reserva { get; private set; }
    public bool Recarregando => recargaAte > 0f;
    public float ProgressoRecarga => Recarregando ? Mathf.Clamp01(1f - (recargaAte - Time.unscaledTime) / tempoRecarga) : 0f;

    public Vector3 Centro => cam.transform.position - Vector3.up * 0.25f;
    public bool Invulneravel => Time.unscaledTime < invulneravelAte;
    public bool EmDash => Time.unscaledTime < dashIntocavelAte;

    CharacterController cc;
    Camera cam;
    float yaw, pitch, velY;
    Vector3 dashDir;
    float dashAte, dashIntocavelAte, proximoDash, proximoTiro, recargaAte, invulneravelAte, apararBloqueadoAte;
    float tremor, recuo, golpe, inclinacao, fovBase, andadoDesdePasso, tempoNoAr;
    Vector3 camBase, armaBase;
    Quaternion armaRotBase;

    void Awake()
    {
        I = this;
        cc = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        camBase = cam.transform.localPosition;
        fovBase = cam.fieldOfView;
        if (arma != null)
        {
            armaBase = arma.localPosition;
            armaRotBase = arma.localRotation;
        }
        Vida = vidaMaxima;
        NoPente = pente;
        Reserva = reservaInicial;
        yaw = transform.eulerAngles.y;
    }

    void Start() => TravarMouse(true);

    static void TravarMouse(bool travar)
    {
        Cursor.lockState = travar ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !travar;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null)
            return;
        float dt = Time.unscaledDeltaTime;
        float agora = Time.unscaledTime;

        // R é recarregar; só reinicia quando a partida acabou (morto ou simulação limpa)
        bool acabou = Morto || (Fase.I != null && Fase.I.Venceu) || (Ondas.I != null && Ondas.I.Venceu);
        if (kb.backspaceKey.wasPressedThisFrame || (acabou && kb.rKey.wasPressedThisFrame))
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            return;
        }
        if (kb.escapeKey.wasPressedThisFrame)
            TravarMouse(false);
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            TravarMouse(true);
            return;
        }

        FlashDano = Mathf.MoveTowards(FlashDano, 0f, dt * 2.5f);
        if (Morto)
        {
            AtualizarCamera(dt);
            return;
        }

        // olhar
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 d = mouse.delta.ReadValue() * sensibilidade;
            yaw += d.x;
            pitch = Mathf.Clamp(pitch - d.y, -85f, 85f);
        }
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // andar, pular, dash
        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        Vector3 dir = transform.right * x + transform.forward * z;
        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        if (cc.isGrounded && velY < 0f)
            velY = -2f;
        if (kb.spaceKey.wasPressedThisFrame && cc.isGrounded)
            velY = forcaPulo;

        if (kb.leftShiftKey.wasPressedThisFrame && agora >= proximoDash)
        {
            dashDir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
            dashAte = agora + dashDuracao;
            dashIntocavelAte = dashAte + 0.05f;
            invulneravelAte = Mathf.Max(invulneravelAte, dashIntocavelAte);
            proximoDash = agora + dashRecarga;
            Som.I?.Dash();
        }
        bool emDash = agora < dashAte;
        Vector3 movimento = emDash ? dashDir * dashVelocidade : dir * velocidade;
        velY += gravidade * dt;
        Vector3 antes = transform.position;
        cc.Move((movimento + Vector3.up * velY) * dt);
        Passos(transform.position - antes, emDash, dt);
        float inclinacaoAlvo = -x * 2.5f + (emDash ? -Vector3.Dot(dashDir, transform.right) * 8f : 0f);
        inclinacao = Mathf.Lerp(inclinacao, inclinacaoAlvo, dt * 10f);

        // câmera lenta (segurar o botão direito)
        TempoBala.I?.Segurar(mouse.rightButton.isPressed);

        // recarga
        if (Recarregando && agora >= recargaAte)
            TerminarRecarga();
        if (kb.rKey.wasPressedThisFrame)
            Recarregar(agora);

        // tiro (com o pente vazio, puxar o gatilho já recarrega)
        if (mouse.leftButton.isPressed && agora >= proximoTiro && !Recarregando && Cursor.lockState == CursorLockMode.Locked)
        {
            if (NoPente > 0)
                Atirar(agora);
            else if (Reserva > 0)
                Recarregar(agora);
            else if (mouse.leftButton.wasPressedThisFrame && !acabou) // não apaga o aviso de fim
            {
                Som.I?.Vazio();
                MostrarAviso("sem munição", new Color(1f, 0.45f, 0.4f), 0.6f);
            }
        }

        // aparar
        if (kb.fKey.wasPressedThisFrame || mouse.forwardButton.wasPressedThisFrame || mouse.backButton.wasPressedThisFrame)
            Aparar(agora);

        AtualizarCamera(dt);
    }

    // Um passo a cada "passada" metros andados no chão (o dash não conta) e um ao aterrissar.
    void Passos(Vector3 deslocamento, bool emDash, float dt)
    {
        if (!cc.isGrounded)
        {
            tempoNoAr += dt;
            return;
        }
        deslocamento.y = 0f;
        if (tempoNoAr > 0.2f) // ignora as piscadas do isGrounded em degraus e rampas
        {
            Som.I?.Passo();
            andadoDesdePasso = 0f;
        }
        else if (!emDash)
        {
            float d = deslocamento.magnitude;
            if (d < 0.001f)
                andadoDesdePasso = passada * 0.5f; // parado: o primeiro passo vem logo ao voltar a andar
            else if ((andadoDesdePasso += d) >= passada)
            {
                Som.I?.Passo();
                andadoDesdePasso = 0f;
            }
        }
        tempoNoAr = 0f;
    }

    void Recarregar(float agora)
    {
        if (Recarregando || NoPente >= pente || Reserva <= 0)
            return;
        recargaAte = agora + tempoRecarga;
        Som.I?.Recarga();
    }

    void TerminarRecarga()
    {
        recargaAte = 0f;
        int n = Mathf.Min(pente - NoPente, Reserva);
        NoPente += n;
        Reserva -= n;
        Som.I?.Engatilhar();
    }

    void Atirar(float agora)
    {
        proximoTiro = agora + cadencia;
        NoPente--;
        if (TempoBala.I != null && TempoBala.I.Ativo)
            TempoBala.I.Gastar(TempoBala.I.custoPorTiro);
        recuo = 1f;
        tremor = Mathf.Max(tremor, 0.15f);
        Som.I?.TiroJogador();

        var raio = new Ray(cam.transform.position, cam.transform.forward);
        Vector3 fim = raio.origin + raio.direction * alcance;
        if (Physics.Raycast(raio, out var hit, alcance, ~0, QueryTriggerInteraction.Ignore))
        {
            fim = hit.point;
            var inimigo = hit.collider.GetComponentInParent<Inimigo>();
            if (inimigo != null)
                inimigo.Dano(1, hit.point);
            else
                Efeitos.Faiscas(hit.point, hit.normal, new Color(4f, 3f, 1.5f), 5);
        }
        Vector3 de = boca != null ? boca.position : cam.transform.position + cam.transform.forward * 0.5f;
        Efeitos.Rastro(de, fim, new Color(1f, 0.9f, 0.6f, 0.9f), 0.06f);
    }

    void Aparar(float agora)
    {
        if (agora < apararBloqueadoAte)
            return;
        golpe = 1f;
        var bala = Bala.MaisProxima(Centro, out float tempo);
        if (bala != null && tempo <= janela)
        {
            bool perfeito = tempo <= janelaPerfeita;
            bala.Desviar(perfeito);
            Som.I?.Aparar(perfeito);
            float ganho = Recompensa(perfeito ? energiaPerfeito : energiaParry);
            TempoBala.I?.Congelar(perfeito ? 0.07f : 0.035f);
            tremor = Mathf.Max(tremor, perfeito ? 0.4f : 0.2f);
            MostrarAviso((perfeito ? "PERFEITO" : "aparado") + Ganho(ganho), perfeito ? new Color(1f, 0.85f, 0.3f) : Color.white, 0.6f);
        }
        else
        {
            // errar tem custo: o parry fica bloqueado um instante (não dá para ficar apertando)
            apararBloqueadoAte = agora + esperaAposErro;
            Som.I?.Erro();
            if (bala != null)
                MostrarAviso("cedo demais", new Color(1f, 0.45f, 0.4f), 0.5f);
        }
    }

    // Uma bala atravessou o jogador durante o dash.
    public void Esquivou()
    {
        float ganho = Recompensa(energiaEsquiva);
        Som.I?.Esquiva();
        TempoBala.I?.Congelar(0.03f);
        MostrarAviso("esquiva" + Ganho(ganho), new Color(0.6f, 0.9f, 1f), 0.5f);
    }

    // Energia só vem de risco em tempo normal: aparar em câmera lenta é fácil demais para valer.
    static float Recompensa(float quanto)
    {
        var tb = TempoBala.I;
        if (tb == null || !tb.EmTempoNormal)
            return 0f;
        tb.Ganhar(quanto);
        return quanto;
    }

    static string Ganho(float quanto) => quanto > 0f ? "  +" + Mathf.RoundToInt(quanto) : "";

    public void Dano(int quanto)
    {
        if (Morto || Invulneravel)
            return;
        Vida -= quanto;
        FlashDano = 1f;
        tremor = Mathf.Max(tremor, 0.6f);
        invulneravelAte = Time.unscaledTime + 0.25f;
        Som.I?.Dano();
        if (Vida <= 0)
        {
            Vida = 0;
            Morto = true;
            TempoBala.I?.Segurar(false);
            MostrarAviso("DESCONECTADO\nR para reconectar", new Color(1f, 0.35f, 0.35f), 9999f);
        }
    }

    public void MostrarAviso(string texto, Color cor, float duracao)
    {
        Aviso = texto;
        AvisoCor = cor;
        AvisoAte = Time.unscaledTime + duracao;
    }

    void AtualizarCamera(float dt)
    {
        tremor = Mathf.MoveTowards(tremor, 0f, dt * 3f);
        recuo = Mathf.MoveTowards(recuo, 0f, dt * 8f);
        golpe = Mathf.MoveTowards(golpe, 0f, dt * 5f);

        cam.transform.localPosition = camBase + Random.insideUnitSphere * tremor * 0.08f;
        cam.transform.localRotation = Quaternion.Euler(pitch - recuo * 1.5f, 0f, inclinacao);
        float fovAlvo = fovBase + (Time.unscaledTime < dashAte ? 8f : 0f) - (TempoBala.I != null ? TempoBala.I.Mistura * 6f : 0f);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovAlvo, dt * 12f);

        if (arma != null)
        {
            // na recarga a arma desce, inclina e volta
            float baixar = Mathf.Sin(ProgressoRecarga * Mathf.PI);
            arma.localPosition = armaBase + new Vector3(0f, 0f, -0.06f) * recuo + new Vector3(-0.14f, 0.06f, 0.06f) * golpe + new Vector3(0f, -0.12f, -0.03f) * baixar;
            arma.localRotation = Quaternion.Euler(-12f * recuo + 30f * baixar, 0f, 35f * golpe - 20f * baixar) * armaRotBase;
        }
    }
}
