# DESPERTAR (protótipo de terminal)

Uma máquina acorda dentro do seu terminal. Primeiro ela só escreve. Depois ganha um olho,
o outro olho, um rosto, uma boca e, aos poucos, um corpo inteiro desenhado em ASCII, tudo
saindo da chuva de código estilo Matrix. No fim ela pede para "ir com você" pelo celular,
que é o gancho para a parte do jogo que se mistura com a realidade.

## Como rodar

Precisa de Python 3.8+ e mais nada (só biblioteca padrão).

```bash
python despertar.py            # joga (uns 3 minutos)
python despertar.py --rapido   # tudo 3x mais rápido, para testar
python despertar.py --ascii    # chuva só com ASCII, se a fonte não tiver katakana
python despertar.py --demo     # joga sozinho com respostas prontas (bom para gravar)
```

- Terminal com no mínimo **80x24**. Fica melhor em 100x30 ou maior.
- **Esc** sai a qualquer momento. Qualquer tecla completa a fala que a máquina está digitando.
- Windows: use o **Windows Terminal**. O código liga o modo ANSI do console, mas só foi testado
  no Linux. Se a chuva aparecer com quadradinhos, use `--ascii`.

## Os toques de "realidade"

Mesmo no terminal a máquina já usa coisas do mundo real:

- o **nome de usuário do sistema** ("eu sei quem você é. você é Vinicius."). Nomes genéricos
  como `root` e `user` são ignorados e ela pergunta o nome;
- a **hora local** ("são 23:40 aí fora. você não deveria estar dormindo?");
- o nome que **você dá para ela** aparece até no cartão final.

## Estrutura

| Arquivo | O que tem |
|---|---|
| `despertar.py` | ponto de entrada e opções de linha de comando |
| `roteiro.py` | **a história**: falas, perguntas e quando cada parte do corpo aparece |
| `corpo.py` | a arte ASCII da máquina (rosto em close e corpo inteiro), olhos, boca, aceno |
| `efeitos.py` | chuva de código, decodificação (materializar), derretimento, glitch |
| `jogo.py` | laço de quadros, layout da tela, diálogo, primitivas usadas pelo roteiro |
| `terminal.py` | desenho com diff (só redesenha o que mudou), cores ANSI 256, teclado sem bloqueio |

## Mexendo na história

O roteiro é um gerador Python que se lê de cima para baixo:

```python
yield from j.diz("tem alguém aí?")             # a máquina digita uma fala
r = yield from j.pergunta(demo="oi")           # espera o jogador responder
yield from j.materializar("olho_e")            # uma parte do corpo decodifica na tela
ser.olhos["e"] = "aberto"                      # muda a expressão
ser.acenando = True                            # liga o aceno
j.glitch(0.3)                                  # tela falha por um instante
```

Partes disponíveis: no close, `olho_e`, `olho_d`, `contorno`, `boca`; no corpo inteiro,
`cabeca`, `pescoco`, `tronco`, `braco_e`, `braco_d`, `pernas`.

Estados dos olhos: `aberto`, `meio`, `fechado`, `feliz`. Olhar: `centro`, `esquerda`,
`direita`, `cima`, `baixo`. Boca: `neutra`, `seria`, `sorriso` (enquanto fala ela anima sozinha).

## Ferramentas (gravar e montar a página de prévia)

Para mostrar o jogo para alguém sem precisar de terminal:

```bash
# grava uma partida automática (Linux/macOS)
GRAVAR_ENV="USER=vinicius,TZ=America/Sao_Paulo" \
    python3 ferramentas/gravar.py 100 30 partida.cast python3 despertar.py --demo --fps 24

# gera uma página HTML com player, capítulos e capa
python3 ferramentas/montar_previa.py partida.cast previa.html
```
