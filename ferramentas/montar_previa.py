"""Gera uma página HTML que toca uma gravação (.cast) com capítulos e quadro de capa.

Uso, da raiz do projeto:
    python3 ferramentas/montar_previa.py partida.cast previa.html

Os capítulos são achados procurando frases do roteiro na tela gravada; se mudar as falas,
ajuste a lista `alvos` abaixo. A capa é o momento do aceno ("oi, <nome>!").
"""
import base64
import gzip
import json
import os
import re
import sys

SEQ = re.compile(r"\x1b\[([0-9;?]*)([A-Za-z])")

def capitulos(caminho):
    linhas = open(caminho).read().splitlines()
    cab = json.loads(linhas[0]); w, h = cab["width"], cab["height"]
    grade = [[" "] * w for _ in range(h)]; cx = cy = 0; resto = ""
    alvos = [
        ("Chuva", lambda tela: sum(0xFF66 <= ord(c) <= 0xFF9D for c in tela) > 400),
        ("Contato", "olá?"),
        ("O olho", "'-------'"),
        ("O rosto", "bordas"),
        ("Um nome", "me dá um?"),
        ("O corpo", "me afastar"),
        ("O convite", "celular perto"),
        ("Fim", "D E S P E R T A R"),
    ]
    caps = [("Boot", 0.0)]; capa = None; fim = 0
    for l in linhas[1:]:
        t, _, dados = json.loads(l); fim = t
        dados = resto + dados; k = dados.rfind("\x1b"); resto = ""
        if k >= 0 and not SEQ.match(dados, k): dados, resto = dados[:k], dados[k:]
        i = 0
        while i < len(dados):
            m = SEQ.match(dados, i)
            if m:
                p, f = m.groups()
                if f == "H": a = (p or "1;1").split(";"); cy, cx = int(a[0]) - 1, int(a[1]) - 1
                elif f == "J" and p == "2": grade = [[" "] * w for _ in range(h)]
                i = m.end(); continue
            ch = dados[i]; i += 1
            if ch == "\x1b": continue
            if 0 <= cy < h and 0 <= cx < w: grade[cy][cx] = ch
            cx += 1
        if len(caps) <= len(alvos) or capa is None:
            tela = "\n".join("".join(r) for r in grade)
            if len(caps) <= len(alvos):
                nome, cond = alvos[len(caps) - 1]
                if (cond(tela) if callable(cond) else cond in tela):
                    caps.append((nome, max(0.0, round(t - 0.3, 2))))
            if capa is None and "braços!" in tela and re.search(r"oi, \w+!", tela):
                capa = round(t + 0.6, 2)
    return caps, capa, fim, w, h

MOLDE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "molde_previa.html")


def montar(cast, saida, molde=MOLDE):
    caps, capa, fim, w, h = capitulos(cast)
    dados = base64.b64encode(gzip.compress(open(cast, "rb").read(), 9)).decode()
    html = open(molde).read()
    html = html.replace("__CAPITULOS__", json.dumps([{"nome": n, "t": t} for n, t in caps], ensure_ascii=False))
    html = html.replace("__CAPA__", str(capa or fim * 0.6)).replace("__DURACAO__", str(round(fim, 2)))
    html = html.replace("__COLS__", str(w)).replace("__ROWS__", str(h))
    html = html.replace("__CAST__", dados)
    open(saida, "w").write(html)
    print("capitulos:", caps, "capa:", capa, "duração:", fim, "tamanho:", len(html) // 1024, "KB")

if __name__ == "__main__":
    montar(*sys.argv[1:4])
