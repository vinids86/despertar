"""O corpo da máquina: arte ASCII dividida em partes que vão se materializando.

Cada parte é uma lista de segmentos (linha, coluna, texto). Espaços no começo e no fim
de um segmento são transparentes (a chuva aparece por trás); espaços no meio são opacos.
'`' força uma célula opaca vazia. ':' vira código piscando (o "recheio" do corpo).

Dois enquadramentos:
  - "rosto": close no rosto (olhos grandes, boca que fala).
  - "corpo": a máquina se afasta e o corpo inteiro vai sendo montado.
"""
import random

from efeitos import Decodificador, Derretimento, glifo

COR_CONTORNO = 40
COR_DETALHE = 83
COR_PUPILA = 231
COR_PELE = 22
CORES_CODIGO = (22, 28, 28, 34)

# --------------------------------------------------------------------------- rosto (close)

ROSTO_LARGURA, ROSTO_ALTURA = 31, 16

ROSTO_CONTORNO = [
    (0, 8, '_.-"""""""""-._'),
    (1, 5, ".-'" + " " * 15 + "'-."),
    (2, 3, ".'" + " " * 21 + "'."),
    (3, 2, "/" + " " * 25 + "\\"),
    (4, 1, "/" + " " * 27 + "\\"),
    *[(r, 0, "|" + " " * 29 + "|") for r in range(5, 10)],
    (10, 1, "\\" + " " * 27 + "/"),
    (11, 2, "\\" + " " * 25 + "/"),
    (12, 3, "\\" + " " * 23 + "/"),
    (13, 4, "'." + " " * 19 + ".'"),
    (14, 6, "'-." + " " * 13 + ".-'"),
    (15, 9, "'-.._____..-'"),
]

OLHO_POS = {"e": (3, 4), "d": (17, 4)}  # canto superior esquerdo de cada olho (11x5)

OLHO = {
    "aberto": [
        " .-------. ",
        "/         \\",
        "|         |",
        "\\         /",
        " '-------' ",
    ],
    "meio": [
        "",
        "",
        "/---------\\",
        "|         |",
        " '-------' ",
    ],
    "fechado": [
        "",
        "",
        "\\         /",
        " '-------' ",
        "",
    ],
    "feliz": [
        "",
        " .-------. ",
        "/         \\",
        "",
        "",
    ],
}

# Onde a pupila "(@)" fica dentro do olho, conforme a direção do olhar.
PUPILA = {"centro": (2, 4), "esquerda": (2, 2), "direita": (2, 6), "cima": (1, 4), "baixo": (3, 4)}

BOCA_POS = (8, 11)  # 15x3

BOCA = {
    "neutra": ["", "  '-._____.-'  ", ""],
    "seria": ["", "   ---------   ", ""],
    "sorriso": ["  \\         /  ", "   '-.___.-'   ", ""],
    "fala1": ["     .---.     ", "    (     )    ", "     '---'     "],
    "fala2": ["   .-------.   ", "   |       |   ", "   '-------'   "],
    "fala3": ["", "   .-=====-.   ", "    '-----'    "],
    "fala4": ["", "    .-----.    ", "    '-----'    "],
}
FALA = ("fala1", "fala2", "fala3", "fala4")


def _celulas_internas(segmentos):
    """Células opacas (miolo) de uma lista de segmentos."""
    for r, c, s in segmentos:
        idx = [i for i, ch in enumerate(s) if ch != " "]
        if idx:
            for k in range(idx[0], idx[-1] + 1):
                if s[k] == " ":
                    yield c + k, r


def _pele():
    """Algumas células dentro do rosto viram código bem apagado: a 'pele' da máquina."""
    caixas = [(x, y, x + 11, y + 5) for x, y in OLHO_POS.values()]
    bx, by = BOCA_POS
    caixas.append((bx, by, bx + 15, by + 3))
    rng = random.Random(7)
    celulas = []
    for x, y in _celulas_internas(ROSTO_CONTORNO):
        if any(x0 <= x < x1 and y0 <= y < y1 for x0, y0, x1, y1 in caixas):
            continue
        if rng.random() < 0.16:
            celulas.append((x, y))
    return celulas


PELE = _pele()

# --------------------------------------------------------------------------- corpo inteiro

CORPO = {
    "cabeca": [
        (0, 10, '.-""""""-.'),
        (1, 9, "/          \\"),
        (2, 8, "|            |"),
        (3, 8, "|     ..     |"),
        (4, 9, "\\          /"),
        (5, 10, "'-.____.-'"),
    ],
    "pescoco": [(6, 13, "|  |")],
    "tronco": [
        (7, 6, '_.--"""\'  \'"""--._'),
        (8, 5, "/  ::::::::::::::  \\"),
        *[(r, 9, "|::::::::::|") for r in range(9, 15)],
    ],
    "braco_e": [(9, 4, "/  /"), (10, 3, "/  /"), (11, 2, "/  /"), (12, 1, "(__/")],
    "braco_d": [(9, 22, "\\  \\"), (10, 23, "\\  \\"), (11, 24, "\\  \\"), (12, 25, "\\__)")],
    "pernas": [
        (15, 9, "|::::/\\::::|"),
        (16, 9, "|:::/  \\:::|"),
        *[seg for r in (17, 18, 19) for seg in ((r, 9, "|::|"), (r, 17, "|::|"))],
        (20, 9, "|__|"),
        (20, 17, "|__|"),
        (21, 7, "(____)"),
        (21, 17, "(____)"),
    ],
}
ORDEM_CORPO = ("cabeca", "pescoco", "tronco", "braco_e", "braco_d", "pernas")

# Braço direito levantado, acenando. A mão balança: esquerda, reta, direita, reta...
_BRACO_ERGUIDO = [(5, 25, "/  /"), (6, 24, "/  /"), (7, 23, "/  /"), (8, 22, "/  /")]
ACENO = [
    _BRACO_ERGUIDO + [(2, 24, "\\\\\\\\"), (3, 24, "\\   \\"), (4, 25, "\\___\\")],
    _BRACO_ERGUIDO + [(2, 25, "\\|||/"), (3, 25, "|   |"), (4, 25, "\\___/")],
    _BRACO_ERGUIDO + [(2, 27, "////"), (3, 26, "/   /"), (4, 25, "/___/")],
    _BRACO_ERGUIDO + [(2, 25, "\\|||/"), (3, 25, "|   |"), (4, 25, "\\___/")],
]

OLHOS_PEQUENOS = ((12, 2), (17, 2))
BOCA_PEQUENA = (13, 4)
BOCA_PEQUENA_QUADROS = {
    "neutra": "'--'",
    "seria": "----",
    "sorriso": "\\__/",
    "fala1": "(  )",
    "fala2": ".--.",
    "fala3": "(--)",
    "fala4": "'=='",
}
OLHO_PEQUENO = {"aberto": "O", "meio": "o", "fechado": "-", "feliz": "^"}


def _caixa(partes):
    xs, ys = [], []
    for segs in partes:
        for r, c, s in segs:
            idx = [i for i, ch in enumerate(s) if ch != " "]
            if idx:
                xs += [c + idx[0], c + idx[-1]]
                ys.append(r)
    return min(xs), min(ys), max(xs) - min(xs) + 1, max(ys) - min(ys) + 1


CORPO_CAIXA = _caixa(CORPO.values())


class Ser:
    """A máquina. O roteiro liga partes, muda olhos/boca e o Ser cuida de animar e desenhar."""

    def __init__(self, rng):
        self.rng = rng
        self.modo = "rosto"
        self.visiveis = set()
        self.efeitos = []  # [efeito, parte que fica visível quando ele acabar (ou None)]
        self.olhos = {"e": "fechado", "d": "fechado"}
        self.olhar = "centro"
        self.boca = "neutra"
        self.falando = False
        self.acenando = False
        self.t = 0.0
        self._prox_piscada = 3.0
        self._piscando = 0.0
        self._boca_quadro = "neutra"
        self._prox_boca = 0.0
        self._codigo = {}

    # ----------------------------------------------------------------- controle pelo roteiro

    def materializar(self, parte, duracao=1.2):
        ef = Decodificador(self.celulas(parte), duracao, self.rng)
        self.efeitos.append([ef, parte])
        return ef

    def derreter(self, limite_y):
        celulas = [c for p in list(self.visiveis) for c in self.celulas(p)]
        self.visiveis.clear()
        ef = Derretimento(celulas, self.rng, limite_y)
        self.efeitos.append([ef, None])
        return ef

    def caixa(self):
        """(x0, y0, largura, altura) do enquadramento atual, para centralizar."""
        if self.modo == "rosto":
            return 0, 0, ROSTO_LARGURA, ROSTO_ALTURA
        return CORPO_CAIXA

    # ----------------------------------------------------------------- animação

    def atualizar(self, dt):
        self.t += dt
        rng = self.rng
        if self.t >= self._prox_piscada:
            self._piscando = 0.14
            self._prox_piscada = self.t + rng.uniform(2.2, 6.0)
        self._piscando = max(0.0, self._piscando - dt)
        if self.falando:
            if self.t >= self._prox_boca:
                self._boca_quadro = rng.choice([q for q in FALA if q != self._boca_quadro])
                self._prox_boca = self.t + rng.uniform(0.07, 0.13)
        else:
            self._boca_quadro = self.boca
        for chave in self._codigo:
            if rng.random() < 0.06:
                pele = len(chave) == 3
                self._codigo[chave] = (glifo(rng), COR_PELE if pele else rng.choice(CORES_CODIGO))
        for item in self.efeitos:
            item[0].atualizar(dt)
        for item in [i for i in self.efeitos if i[0].pronto]:
            self.efeitos.remove(item)
            if item[1]:
                self.visiveis.add(item[1])

    def _estado_olho(self, lado):
        estado = self.olhos[lado]
        if self._piscando > 0 and estado in ("aberto", "meio"):
            return "fechado"
        return estado

    # ----------------------------------------------------------------- arte de cada parte

    def segmentos(self, parte):
        """Segmentos (linha, coluna, texto, cor) de uma parte no estado atual."""
        segs = []
        if self.modo == "rosto":
            if parte == "contorno":
                segs = [(r, c, s, COR_CONTORNO) for r, c, s in ROSTO_CONTORNO]
            elif parte in ("olho_e", "olho_d"):
                lado = parte[-1]
                ox, oy = OLHO_POS[lado]
                estado = self._estado_olho(lado)
                for k, linha in enumerate(OLHO[estado]):
                    segs.append((oy + k, ox, linha, COR_DETALHE))
                if estado in ("aberto", "meio"):
                    pr, pc = PUPILA[self.olhar] if estado == "aberto" else (3, 4)
                    segs.append((oy + pr, ox + pc, "(", COR_DETALHE))
                    segs.append((oy + pr, ox + pc + 1, "@", COR_PUPILA))
                    segs.append((oy + pr, ox + pc + 2, ")", COR_DETALHE))
            elif parte == "boca":
                bx, by = BOCA_POS
                for k, linha in enumerate(BOCA[self._boca_quadro]):
                    segs.append((by + k, bx, linha, COR_DETALHE))
        else:
            if parte == "braco_d" and self.acenando:
                arte = ACENO[int(self.t / 0.16) % len(ACENO)]
            else:
                arte = CORPO[parte]
            segs = [(r, c, s, COR_CONTORNO) for r, c, s in arte]
            if parte == "cabeca":
                olho = OLHO_PEQUENO[self._estado_olho("e")]
                for x, y in OLHOS_PEQUENOS:
                    segs.append((y, x, olho, COR_PUPILA))
                bx, by = BOCA_PEQUENA
                segs.append((by, bx, BOCA_PEQUENA_QUADROS[self._boca_quadro], COR_DETALHE))
        return segs

    def celulas(self, parte):
        """Células visíveis (não vazias) de uma parte: usado pelos efeitos."""
        out = []
        for r, c, s, cor in self.segmentos(parte):
            for k, ch in enumerate(s):
                if ch not in " `":
                    if ch == ":":
                        out.append((c + k, r, glifo(self.rng), CORES_CODIGO[1]))
                    else:
                        out.append((c + k, r, ch, cor))
        return out

    # ----------------------------------------------------------------- desenho

    def _desenhar_segmento(self, grade, ox, oy, r, c, s, cor):
        idx = [i for i, ch in enumerate(s) if ch != " "]
        if not idx:
            return
        for k in range(idx[0], idx[-1] + 1):
            ch = s[k]
            x, y = c + k, r
            if ch in " `":
                grade.por(ox + x, oy + y, " ", 0)
            elif ch == ":":
                g = self._codigo.get((x, y))
                if g is None:
                    g = self._codigo[(x, y)] = (glifo(self.rng), self.rng.choice(CORES_CODIGO))
                grade.por(ox + x, oy + y, g[0], g[1])
            else:
                grade.por(ox + x, oy + y, ch, cor)

    def desenhar(self, grade, ox, oy):
        if self.modo == "rosto":
            ordem = ("contorno", "olho_e", "olho_d", "boca")
        else:
            ordem = ORDEM_CORPO
        for parte in ordem:
            if parte not in self.visiveis:
                continue
            for r, c, s, cor in self.segmentos(parte):
                self._desenhar_segmento(grade, ox, oy, r, c, s, cor)
            if parte == "contorno":
                for x, y in PELE:
                    g = self._codigo.get((x, y, "pele"))
                    if g is None:
                        g = self._codigo[(x, y, "pele")] = (glifo(self.rng), COR_PELE)
                    grade.por(ox + x, oy + y, g[0], g[1])
        for ef, _ in self.efeitos:
            ef.desenhar(grade, ox, oy)
