"""Motor do protótipo: laço de quadros, layout da tela, diálogo e as primitivas do roteiro.

O roteiro é um gerador: cada `yield` deixa passar um quadro. As primitivas abaixo
(`diz`, `pergunta`, `esperar`, `materializar`...) também são geradores, usados com
`yield from`, então a história se lê de cima para baixo como um texto.
"""
import getpass
import random
import re
import textwrap
import time
import unicodedata

from corpo import Ser
from efeitos import BRANCO, VERDES, Chuva, aplicar_glitch
from terminal import Grade

LARGURA_MIN, ALTURA_MIN = 80, 24

COR_MAQUINA = 46
COR_JOGADOR = 252
COR_SISTEMA = 244

PONTUACAO_PAUSA = {".": 0.32, "!": 0.32, "?": 0.32, ",": 0.16, ":": 0.2, ";": 0.2}

USUARIOS_GENERICOS = {
    "root", "user", "usuario", "admin", "administrator", "administrador", "guest",
    "convidado", "pi", "ubuntu", "runner", "default", "owner", "pc",
}


def sem_acento(s):
    return "".join(c for c in unicodedata.normalize("NFD", s) if unicodedata.category(c) != "Mn")


def sim(resposta):
    r = sem_acento(resposta.lower()).strip(" .!?")
    palavras = set(re.findall(r"\w+", r))
    return r.startswith("s") or bool(palavras & {
        "claro", "levo", "bora", "pode", "ok", "yes", "y", "quero", "vamos", "uhum", "aham",
        "consigo", "isso", "exato", "obvio", "certeza", "vejo", "to", "estou",
    })


def nao(resposta):
    r = sem_acento(resposta.lower()).strip(" .!?")
    palavras = set(re.findall(r"\w+", r))
    return r.startswith("n") or bool(palavras & {"nao", "nunca", "jamais", "nope", "no"})


class Fala:
    def __init__(self, quem, texto, visivel=None):
        self.quem = quem  # "maquina" | "jogador" | "sistema"
        self.texto = texto
        self.visivel = len(texto) if visivel is None else visivel


class Dialogo:
    """Histórico de falas, quebrado na largura do painel, com a linha de digitação no fim."""

    def __init__(self):
        self.falas = []
        self.entrada = None  # texto sendo digitado pelo jogador (None = não está perguntando)

    def limpar(self):
        self.falas = []

    def linhas(self, largura, cursor_visivel):
        out = []
        for f in self.falas:
            prefixo = "> " if f.quem == "jogador" else ""
            cor = {"maquina": COR_MAQUINA, "jogador": COR_JOGADOR}.get(f.quem, COR_SISTEMA)
            quebradas = textwrap.wrap(f.texto, max(10, largura - len(prefixo))) or [""]
            restante = f.visivel
            for i, linha in enumerate(quebradas):
                if restante <= 0 and i > 0:
                    break
                pedaco = linha[:max(0, restante)]
                restante -= len(linha) + 1
                out.append(((prefixo if i == 0 else " " * len(prefixo)) + pedaco, cor, f.quem))
            if f.quem != "sistema":
                out.append(("", 0, None))
        if self.entrada is not None:
            cursor = "█" if cursor_visivel else " "
            out.append(("> " + self.entrada + cursor, BRANCO, "jogador"))
        return out


class Jogo:
    def __init__(self, terminal, velocidade=1.0, demo=False, fps=30, seed=None):
        self.term = terminal
        self.velocidade = velocidade
        self.demo = demo
        self.fps = fps
        self.rng = random.Random(seed)
        self.t = 0.0  # tempo do jogo (já multiplicado pela velocidade)
        self.chuva = Chuva(self.rng)
        self.chuva.brilho = 0.0
        self.chuva.densidade = 0.0
        self.ser = Ser(self.rng)
        self.dialogo = Dialogo()
        self.layout = "intro"  # "intro" | "dividido" | "final"
        self.titulo = None  # linhas do cartão final
        self._glitch_ate = 0.0
        self._glitch_forca = 0.0
        self._animacoes = []
        self._teclas = []
        self._pular = False
        self._tamanho = (0, 0)

    # =============================================================== primitivas do roteiro

    def esperar(self, segundos):
        fim = self.t + segundos
        while self.t < fim:
            if self._pular and self.dialogo.entrada is None:
                self._pular = False
                return
            yield

    def diz(self, texto, pausa=None):
        """A máquina digita uma fala, letra por letra. Qualquer tecla completa a linha."""
        fala = Fala("maquina", texto, 0)
        self.dialogo.falas.append(fala)
        self._pular = False
        proximo = self.t
        while fala.visivel < len(texto):
            if self._pular:
                fala.visivel = len(texto)
                self._pular = False
                break
            while self.t >= proximo and fala.visivel < len(texto):
                ch = texto[fala.visivel]
                fala.visivel += 1
                if ch in PONTUACAO_PAUSA and (fala.visivel == len(texto) or texto[fala.visivel] in " .!?"):
                    proximo += PONTUACAO_PAUSA[ch]
                elif ch == " ":
                    proximo += 0.035
                else:
                    proximo += self.rng.uniform(0.028, 0.06)
            ultimo = texto[fala.visivel - 1]
            self.ser.falando = ultimo not in PONTUACAO_PAUSA and fala.visivel < len(texto)
            yield
        self.ser.falando = False
        yield from self.esperar(0.55 + 0.022 * len(texto) if pausa is None else pausa)

    def sistema(self, texto):
        """Linha de sistema (boot): aparece de uma vez, em cinza."""
        self.dialogo.falas.append(Fala("sistema", texto))

    def pergunta(self, demo="sim", limite=24):
        """Espera o jogador digitar algo e apertar Enter. Devolve o texto digitado."""
        self.dialogo.entrada = ""
        self._teclas.clear()
        olhar_antes = self.ser.olhar
        if self.layout == "dividido":
            self.ser.olhar = "direita"  # a máquina olha para o que você está digitando
        if self.demo:
            yield from self.esperar(0.9)
            for ch in demo:
                self.dialogo.entrada += ch
                yield from self.esperar(self.rng.uniform(0.08, 0.22))
            yield from self.esperar(0.45)
        else:
            enviado = False
            while not enviado:
                for tecla in self._teclas:
                    if tecla == "ENTER":
                        if self.dialogo.entrada.strip():
                            enviado = True
                            break
                    elif tecla == "APAGAR":
                        self.dialogo.entrada = self.dialogo.entrada[:-1]
                    elif len(tecla) == 1 and len(self.dialogo.entrada) < limite:
                        self.dialogo.entrada += tecla
                self._teclas.clear()
                if not enviado:
                    yield
        texto = self.dialogo.entrada.strip()
        self.dialogo.entrada = None
        self.ser.olhar = olhar_antes
        self.dialogo.falas.append(Fala("jogador", texto))
        self._pular = False
        yield from self.esperar(0.5)
        return texto

    def esperar_tecla(self, ignorar=1.0, demo_segundos=4.0):
        """Espera qualquer tecla. Teclas no primeiro `ignorar` segundos não contam (evita pular sem querer)."""
        fim = self.t + (demo_segundos if self.demo else ignorar)
        while self.t < fim:
            yield
        if self.demo:
            return
        self._pular = False  # fora de uma pergunta, qualquer tecla liga _pular
        while not self._pular:
            yield

    def materializar(self, *partes, duracao=1.2):
        efeitos = [self.ser.materializar(p, duracao) for p in partes]
        while not all(e.pronto for e in efeitos) or self.ser.efeitos:
            yield

    def derreter(self):
        w, h = self._tamanho
        _, oy = self._origem_ser(w, h)
        ef = self.ser.derreter(limite_y=h - oy)
        while not ef.pronto:
            yield

    def animar(self, obj, attr, alvo, segundos):
        """Interpola um atributo até `alvo` ao longo do tempo (não bloqueia o roteiro)."""
        self._animacoes = [a for a in self._animacoes if not (a[0] is obj and a[1] == attr)]
        self._animacoes.append([obj, attr, getattr(obj, attr), alvo, self.t, max(0.001, segundos)])

    def glitch(self, segundos=0.3, forca=1.0):
        self._glitch_ate = self.t + segundos
        self._glitch_forca = forca

    def limpar(self):
        self.dialogo.limpar()

    def usuario_os(self):
        """Nome de usuário do sistema operacional, se parecer um nome de verdade."""
        try:
            bruto = getpass.getuser()
        except Exception:
            return None
        nome = re.split(r"[._\-\s@]", bruto.strip())[0]
        if not nome or nome.lower() in USUARIOS_GENERICOS or not nome.isalpha() or len(nome) < 3:
            return None
        return nome[0].upper() + nome[1:]

    # =============================================================== layout e desenho

    def _painel(self, w):
        """Largura do painel esquerdo (onde a máquina aparece)."""
        return min(max(44, w // 2), w - 36)

    def _origem_ser(self, w, h):
        x0, y0, bw, bh = self.ser.caixa()
        pw = self._painel(w)
        return (pw - bw) // 2 - x0, (h - bh) // 2 - y0

    def _rect_dialogo(self, w, h):
        if self.layout == "intro":
            return 4, 2, w - 8, h - 4
        pw = self._painel(w)
        return pw + 2, 2, w - pw - 5, h - 4

    def compor(self, w, h):
        g = Grade(w, h)
        excluir = None
        if self.layout == "dividido":
            excluir = (self._painel(w), 0, w, h)
        self.chuva.desenhar(g, excluir)

        if self.layout == "dividido":
            ox, oy = self._origem_ser(w, h)
            self.ser.desenhar(g, ox, oy)

        if self.layout != "final":
            x, y, rw, rh = self._rect_dialogo(w, h)
            cursor = int(self.t * 2.2) % 2 == 0
            linhas = self.dialogo.linhas(rw, cursor)[-rh:]
            for i, (texto, cor, quem) in enumerate(linhas):
                g.texto(x, y + i, texto, cor)
                if quem == "sistema" and texto.endswith(" ok"):
                    g.texto(x + len(texto) - 2, y + i, "ok", VERDES[-1])

        if self.titulo:
            self._desenhar_titulo(g, w, h)

        if self.t < self._glitch_ate:
            aplicar_glitch(g, self.rng, self._glitch_forca)
        return g

    def _desenhar_titulo(self, g, w, h):
        largura = max(len(l) for l, _ in self.titulo) + 8
        altura = len(self.titulo) + 4
        x0, y0 = (w - largura) // 2, (h - altura) // 2
        for yy in range(altura):
            for xx in range(largura):
                borda = yy in (0, altura - 1) or xx in (0, largura - 1)
                ch = " "
                if borda:
                    ch = "+" if yy in (0, altura - 1) and xx in (0, largura - 1) else "-" if yy in (0, altura - 1) else "|"
                g.por(x0 + xx, y0 + yy, ch, VERDES[1])
        for i, (linha, cor) in enumerate(self.titulo):
            g.texto((w - len(linha)) // 2, y0 + 2 + i, linha, cor)

    def _desenhar_aviso_tamanho(self, w, h):
        g = Grade(w, h)
        msgs = ["aumente a janela do terminal", f"mínimo {LARGURA_MIN}x{ALTURA_MIN}, atual {w}x{h}"]
        for i, m in enumerate(msgs):
            g.texto(max(0, (w - len(m)) // 2), h // 2 - 1 + i, m, COR_MAQUINA)
        return g

    # =============================================================== laço principal

    def _atualizar(self, dt):
        self.t += dt
        for a in list(self._animacoes):
            obj, attr, ini, alvo, t0, dur = a
            f = min(1.0, (self.t - t0) / dur)
            setattr(obj, attr, ini + (alvo - ini) * f)
            if f >= 1.0:
                self._animacoes.remove(a)
        self.chuva.atualizar(dt)
        self.ser.atualizar(dt)

    def rodar(self, roteiro):
        historia = roteiro(self)
        quadro = 1.0 / self.fps
        anterior = time.perf_counter()
        while True:
            agora = time.perf_counter()
            dt = min(0.1, agora - anterior) * self.velocidade
            anterior = agora

            teclas = self.term.teclas()
            if "ESC" in teclas:
                return
            if teclas:
                if self.dialogo.entrada is None:
                    self._pular = True
                else:
                    self._teclas.extend(teclas)

            w, h = self.term.tamanho()
            if w < LARGURA_MIN or h < ALTURA_MIN:
                self.term.apresentar(self._desenhar_aviso_tamanho(w, h))
                time.sleep(0.2)
                anterior = time.perf_counter()
                continue
            if (w, h) != self._tamanho:
                self._tamanho = (w, h)
                self.chuva.redimensionar(w, h)
                self.term.forcar_redesenho()

            self._atualizar(dt)
            try:
                next(historia)
            except StopIteration:
                return
            self.term.apresentar(self.compor(w, h))

            sobra = quadro - (time.perf_counter() - agora)
            if sobra > 0:
                time.sleep(sobra)
