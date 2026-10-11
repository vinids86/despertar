"""Camada de terminal: tela em buffer com diff, cores ANSI 256 e teclado sem bloqueio.

Funciona no Linux/macOS (termios) e no Windows 10+ (msvcrt + modo VT do console).
Só biblioteca padrão.
"""
import os
import shutil
import sys

WINDOWS = os.name == "nt"

if WINDOWS:
    import msvcrt
else:
    import codecs
    import select
    import termios
    import tty


class Grade:
    """Buffer da tela: um caractere e uma cor (índice ANSI 256, 0 = padrão) por célula."""

    def __init__(self, largura, altura):
        self.largura = largura
        self.altura = altura
        self.chars = [" "] * (largura * altura)
        self.cores = [0] * (largura * altura)

    def limpar(self):
        n = self.largura * self.altura
        self.chars = [" "] * n
        self.cores = [0] * n

    def por(self, x, y, ch, cor):
        if 0 <= x < self.largura and 0 <= y < self.altura:
            i = y * self.largura + x
            self.chars[i] = ch
            self.cores[i] = cor

    def texto(self, x, y, s, cor):
        for k, ch in enumerate(s):
            self.por(x + k, y, ch, cor)

    def pegar(self, x, y):
        if 0 <= x < self.largura and 0 <= y < self.altura:
            return self.chars[y * self.largura + x]
        return " "

    def copia(self):
        g = Grade.__new__(Grade)
        g.largura, g.altura = self.largura, self.altura
        g.chars, g.cores = self.chars[:], self.cores[:]
        return g


def _ativar_vt_windows():
    """Liga o processamento de sequências ANSI e UTF-8 no console do Windows."""
    import ctypes

    kernel32 = ctypes.windll.kernel32
    kernel32.SetConsoleOutputCP(65001)
    handle = kernel32.GetStdHandle(-11)  # STD_OUTPUT_HANDLE
    modo = ctypes.c_uint32()
    if kernel32.GetConsoleMode(handle, ctypes.byref(modo)):
        kernel32.SetConsoleMode(handle, modo.value | 0x0004)  # ENABLE_VIRTUAL_TERMINAL_PROCESSING


def _normalizar(brutos):
    """Converte caracteres crus em teclas: 'ENTER', 'APAGAR', 'ESC' ou o próprio caractere."""
    teclas = []
    i = 0
    while i < len(brutos):
        ch = brutos[i]
        i += 1
        if ch in ("\r", "\n"):
            teclas.append("ENTER")
        elif ch in ("\x7f", "\x08"):
            teclas.append("APAGAR")
        elif ch == "\x03":
            raise KeyboardInterrupt
        elif ch == "\x1b":
            # Setas e afins chegam como ESC [ ... letra. Descarta a sequência inteira.
            if i < len(brutos) and brutos[i] in "[O":
                i += 1
                while i < len(brutos) and not ("@" <= brutos[i] <= "~"):
                    i += 1
                i += 1
            else:
                teclas.append("ESC")
        elif ch.isprintable():
            teclas.append(ch)
    return teclas


class Terminal:
    """Abre a tela alternativa, esconde o cursor e desenha só o que mudou entre quadros."""

    def __init__(self):
        self.anterior = None
        self._estado_tty = None
        self._decoder = None

    def __enter__(self):
        if WINDOWS:
            _ativar_vt_windows()
        else:
            fd = sys.stdin.fileno()
            self._estado_tty = termios.tcgetattr(fd)
            tty.setcbreak(fd)  # sem eco e sem esperar Enter; Ctrl+C continua funcionando
            self._decoder = codecs.getincrementaldecoder("utf-8")(errors="ignore")
        try:
            sys.stdout.reconfigure(encoding="utf-8")
        except (AttributeError, ValueError):
            pass
        self._escrever("\x1b[?1049h\x1b[?25l\x1b[0m\x1b[2J")
        return self

    def __exit__(self, *exc):
        self._escrever("\x1b[0m\x1b[2J\x1b[H\x1b[?25h\x1b[?1049l")
        if self._estado_tty is not None:
            termios.tcsetattr(sys.stdin.fileno(), termios.TCSADRAIN, self._estado_tty)
        return False

    def tamanho(self):
        t = shutil.get_terminal_size((80, 24))
        return t.columns, t.lines

    def teclas(self):
        """Teclas apertadas desde a última chamada (não bloqueia)."""
        brutos = []
        if WINDOWS:
            while msvcrt.kbhit():
                ch = msvcrt.getwch()
                if ch in ("\x00", "\xe0"):  # tecla especial: descarta o código que vem junto
                    msvcrt.getwch()
                    continue
                brutos.append(ch)
        else:
            fd = sys.stdin.fileno()
            while select.select([fd], [], [], 0)[0]:
                dados = os.read(fd, 1024)
                if not dados:
                    break
                brutos.extend(self._decoder.decode(dados))
        return _normalizar(brutos)

    def apresentar(self, grade):
        """Desenha a grade, emitindo só as células que mudaram desde o último quadro."""
        ant = self.anterior
        completo = ant is None or ant.largura != grade.largura or ant.altura != grade.altura
        partes = ["\x1b[0m\x1b[2J"] if completo else []
        w, h = grade.largura, grade.altura
        chars, cores = grade.chars, grade.cores
        cor_atual = -1
        cursor = None
        ultima = w * h - 1  # nunca escreve no canto inferior direito (alguns consoles rolam a tela)
        for i in range(w * h):
            ch = chars[i]
            if i == ultima:
                break
            if completo:
                if ch == " ":
                    continue
            elif ch == ant.chars[i] and (ch == " " or cores[i] == ant.cores[i]):
                continue
            y, x = divmod(i, w)
            if cursor != i:
                partes.append(f"\x1b[{y + 1};{x + 1}H")
            cor = cores[i]
            if ch != " " and cor != cor_atual:
                partes.append(f"\x1b[38;5;{cor}m" if cor else "\x1b[39m")
                cor_atual = cor
            partes.append(ch)
            cursor = i + 1 if x + 1 < w else None
        if partes:
            self._escrever("".join(partes))
        self.anterior = grade.copia()

    def forcar_redesenho(self):
        self.anterior = None

    def _escrever(self, s):
        sys.stdout.write(s)
        sys.stdout.flush()
