"""Roda o jogo num pseudo-terminal e grava a saída em asciicast v2 (Linux/macOS).

Uso, da raiz do projeto:
    GRAVAR_ENV="USER=vinicius,TZ=America/Sao_Paulo" \
        python3 ferramentas/gravar.py 100 30 partida.cast python3 despertar.py --demo --fps 24

GRAVAR_ENV define variáveis para o jogo (nome de usuário e fuso que a máquina vai "descobrir").
O .cast também toca no asciinema (asciinema play partida.cast).
"""
import codecs
import fcntl
import json
import os
import pty
import select
import struct
import sys
import termios
import time

def gravar(cmd, w, h, caminho, env_extra=None):
    pid, fd = pty.fork()
    if pid == 0:
        fcntl.ioctl(0, termios.TIOCSWINSZ, struct.pack("HHHH", h, w, 0, 0))
        os.environ.update(env_extra or {})
        os.environ["TERM"] = "xterm-256color"
        os.environ.pop("COLUMNS", None); os.environ.pop("LINES", None)
        os.execvp(cmd[0], cmd)
    dec = codecs.getincrementaldecoder("utf-8")(errors="replace")
    eventos, t0 = [], time.perf_counter()
    while True:
        r, _, _ = select.select([fd], [], [], 1.0)
        if not r:
            continue
        try:
            dados = os.read(fd, 65536)
        except OSError:
            break
        if not dados:
            break
        eventos.append([round(time.perf_counter() - t0, 4), "o", dec.decode(dados)])
    _, status = os.waitpid(pid, 0)
    with open(caminho, "w") as f:
        f.write(json.dumps({"version": 2, "width": w, "height": h, "env": {"TERM": "xterm-256color"}}) + "\n")
        for e in eventos:
            f.write(json.dumps(e, ensure_ascii=False) + "\n")
    return status, len(eventos)

if __name__ == "__main__":
    w, h, caminho = int(sys.argv[1]), int(sys.argv[2]), sys.argv[3]
    env = dict(kv.split("=", 1) for kv in os.environ.get("GRAVAR_ENV", "").split(",") if kv)
    print(gravar(sys.argv[4:], w, h, caminho, env))
