#!/usr/bin/env python3
"""DESPERTAR — protótipo de terminal.

    python despertar.py            joga
    python despertar.py --rapido   tudo 3x mais rápido (para testar)
    python despertar.py --ascii    sem katakana (se a fonte do terminal não tiver)
    python despertar.py --demo     joga sozinho, com respostas prontas (para gravar)

Esc sai a qualquer momento. Qualquer tecla completa a fala que está sendo digitada.
"""
import argparse
import sys

import efeitos
import roteiro
from jogo import Jogo
from terminal import Terminal


def main():
    p = argparse.ArgumentParser(description="DESPERTAR: protótipo de terminal")
    p.add_argument("--rapido", action="store_true", help="velocidade 3x")
    p.add_argument("--velocidade", type=float, default=None, help="multiplicador de velocidade")
    p.add_argument("--ascii", action="store_true", help="usar só caracteres ASCII na chuva")
    p.add_argument("--demo", action="store_true", help="responde sozinho (para gravação)")
    p.add_argument("--fps", type=int, default=30)
    p.add_argument("--seed", type=int, default=None)
    args = p.parse_args()

    if not sys.stdin.isatty() or not sys.stdout.isatty():
        print("rode o despertar direto num terminal (não funciona com entrada/saída redirecionada).")
        return 1
    if args.ascii:
        efeitos.usar_ascii()
    velocidade = args.velocidade or (3.0 if args.rapido else 1.0)

    try:
        with Terminal() as term:
            Jogo(term, velocidade=velocidade, demo=args.demo, fps=args.fps, seed=args.seed).rodar(roteiro.historia)
    except KeyboardInterrupt:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
