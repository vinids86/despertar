"""Efeitos visuais: chuva de código, decodificação, derretimento e glitch."""

# Katakana de meia largura (ocupa 1 coluna, como no filme) + dígitos e símbolos.
KATAKANA = "".join(chr(c) for c in range(0xFF66, 0xFF9E))
GLIFOS_MATRIX = KATAKANA + "0123456789:.=*+-<>|Z"
GLIFOS_ASCII = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ@#$%&*+=<>?/|"

GLIFOS = GLIFOS_MATRIX


def usar_ascii():
    """Troca para glifos só-ASCII (para terminais sem fonte com katakana)."""
    global GLIFOS
    GLIFOS = GLIFOS_ASCII


# Paleta ANSI 256: verdes do mais escuro ao mais claro.
VERDES = [22, 28, 34, 40, 46]
VERDE_CLARO = 120
VERDE_PALIDO = 157
BRANCO = 231


def glifo(rng):
    return rng.choice(GLIFOS)


class Chuva:
    """A chuva de caracteres verdes. Os caracteres ficam parados; a 'cabeça' de cada gota desce."""

    def __init__(self, rng):
        self.rng = rng
        self.densidade = 1.0  # 0..1: quantas gotas nascem
        self.brilho = 1.0  # 0..1: escurece a chuva inteira
        self.gotas = []
        self.w = self.h = 0
        self._acumulado = 0.0

    def redimensionar(self, w, h):
        if (w, h) != (self.w, self.h):
            self.w, self.h = w, h
            self.gotas = [g for g in self.gotas if g[0] < w]
            for g in self.gotas:
                g[4] = (g[4] + [glifo(self.rng) for _ in range(h)])[:h]

    def nova_gota(self, y=None):
        rng = self.rng
        return [
            rng.randrange(self.w),  # 0 coluna
            -rng.uniform(0, 4) if y is None else y,  # 1 posição da cabeça
            rng.uniform(9, 26),  # 2 velocidade (linhas/s)
            rng.randint(6, max(8, self.h)),  # 3 comprimento do rastro
            [glifo(rng) for _ in range(self.h)],  # 4 caracteres fixos da coluna
        ]

    def encher(self):
        """Começa com a tela já cheia de gotas (em vez de esperar caírem)."""
        for _ in range(int(self.w * 0.8)):
            self.gotas.append(self.nova_gota(y=self.rng.uniform(0, self.h * 1.5)))

    def atualizar(self, dt):
        rng = self.rng
        self._acumulado += self.densidade * self.w * 0.55 * dt
        while self._acumulado >= 1:
            self._acumulado -= 1
            self.gotas.append(self.nova_gota())
        vivas = []
        for g in self.gotas:
            g[1] += g[2] * dt
            if rng.random() < 0.3:
                g[4][rng.randrange(self.h)] = glifo(rng)
            if g[1] - g[3] < self.h:
                vivas.append(g)
        self.gotas = vivas

    def desenhar(self, grade, excluir=None):
        if self.brilho <= 0.01:
            return
        niveis = len(VERDES)
        for x, cabeca_f, _vel, comp, chars in self.gotas:
            if excluir and excluir[0] <= x < excluir[2]:
                y_ex0, y_ex1 = excluir[1], excluir[3]
            else:
                y_ex0 = y_ex1 = -1
            cabeca = int(cabeca_f)
            for k in range(comp):
                y = cabeca - k
                if y < 0 or y >= self.h:
                    continue
                if y_ex0 <= y < y_ex1:
                    continue
                if k == 0:
                    cor = BRANCO if self.brilho > 0.75 else VERDE_PALIDO if self.brilho > 0.4 else VERDES[1]
                else:
                    f = 1 - k / comp
                    nivel = int(f * niveis * self.brilho + 0.999) - 1
                    if nivel < 0:
                        continue
                    cor = VERDES[min(nivel, niveis - 1)]
                grade.por(x, y, chars[y], cor)


class Decodificador:
    """Células 'decodificam': glifos aleatórios piscando até travarem no caractere certo.

    Varre de cima para baixo, como se o corpo estivesse sendo impresso pela matriz.
    """

    def __init__(self, celulas, duracao, rng):
        self.rng = rng
        self.celulas = celulas  # lista de (x, y, ch, cor)
        self.t = 0.0
        if celulas:
            y0 = min(c[1] for c in celulas)
            y1 = max(c[1] for c in celulas)
        else:
            y0 = y1 = 0
        alt = max(1, y1 - y0)
        self.inicio = []
        self.trava = []
        for c in celulas:
            ini = (c[1] - y0) / alt * duracao * 0.55 + rng.uniform(0, duracao * 0.2)
            self.inicio.append(ini)
            self.trava.append(ini + rng.uniform(0.12, 0.25) * duracao)
        self.fim = max(self.trava, default=0) + 0.1
        self.lixo = [glifo(rng) for _ in celulas]

    @property
    def pronto(self):
        return self.t >= self.fim

    def atualizar(self, dt):
        self.t += dt
        rng = self.rng
        for i in range(len(self.lixo)):
            if rng.random() < 0.35:
                self.lixo[i] = glifo(rng)

    def desenhar(self, grade, ox, oy):
        t = self.t
        for i, (x, y, ch, cor) in enumerate(self.celulas):
            if t < self.inicio[i]:
                continue
            if t < self.trava[i]:
                grade.por(ox + x, oy + y, self.lixo[i], VERDE_CLARO if (i + int(t * 20)) % 3 else VERDE_PALIDO)
            elif t < self.trava[i] + 0.08:
                grade.por(ox + x, oy + y, ch, BRANCO)
            else:
                grade.por(ox + x, oy + y, ch, cor)


class Derretimento:
    """O inverso da decodificação: as células viram código e escorrem para baixo."""

    def __init__(self, celulas, rng, limite_y, duracao=1.6):
        self.rng = rng
        self.limite_y = limite_y
        self.t = 0.0
        self.celulas = celulas
        self.atraso = [rng.uniform(0, duracao * 0.6) for _ in celulas]
        self.chars = [c[2] for c in celulas]

    @property
    def pronto(self):
        return all(self._queda(i) + c[1] > self.limite_y for i, c in enumerate(self.celulas))

    def _queda(self, i):
        t = self.t - self.atraso[i]
        return 0 if t <= 0 else 18 * t * t + 4 * t

    def atualizar(self, dt):
        self.t += dt
        for i in range(len(self.chars)):
            if self.t > self.atraso[i] and self.rng.random() < 0.4:
                self.chars[i] = glifo(self.rng)

    def desenhar(self, grade, ox, oy):
        for i, (x, y, _ch, cor) in enumerate(self.celulas):
            q = self._queda(i)
            yy = y + int(q)
            if yy > self.limite_y:
                continue
            if q <= 0:
                grade.por(ox + x, oy + y, _ch, cor)
            else:
                grade.por(ox + x, oy + yy, self.chars[i], VERDES[2] if q > 3 else VERDE_CLARO)


def aplicar_glitch(grade, rng, forca):
    """Bagunça a grade já montada: desloca linhas e troca células por glifos."""
    w, h = grade.largura, grade.altura
    for _ in range(int(h * forca * 0.5)):
        y = rng.randrange(h)
        d = rng.choice((-3, -2, -1, 1, 2, 3))
        base = y * w
        linha_c = grade.chars[base:base + w]
        linha_k = grade.cores[base:base + w]
        grade.chars[base:base + w] = linha_c[-d:] + linha_c[:-d]
        grade.cores[base:base + w] = linha_k[-d:] + linha_k[:-d]
    for _ in range(int(w * h * forca * 0.04)):
        grade.por(rng.randrange(w), rng.randrange(h), glifo(rng), rng.choice((BRANCO, VERDE_CLARO, 46)))
