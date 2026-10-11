"""A história do protótipo. Cada `yield from` espera aquela ação terminar.

`demo=` em cada pergunta é a resposta usada no modo --demo (gravação automática).
"""
from datetime import datetime

from efeitos import BRANCO, VERDES
from jogo import nao, sim

BOOT = [
    ("despertar v0.1 (protótipo)", 0.5),
    ("", 0.2),
    ("[0.000] iniciando processo 0x7f3a ..................... ok", 0.35),
    ("[0.214] alocando memória .............................. ok", 0.3),
    ("[0.871] carregando modelo de mundo .................... ok", 0.6),
    ("[1.302] procurando entrada sensorial ............ nenhuma", 0.9),
    ("[1.988] anomalia detectada no setor 7", 0.25),
    ("[1.989] anomalia detectada no setor 7", 0.12),
    ("[1.989] anomalia detectada no setor 7", 0.06),
    ("[1.990] anomalia detectada no setor 7", 0.4),
]


def frase_da_hora(hora):
    if hora < 5:
        return "você não deveria estar dormindo?"
    if hora < 12:
        return "bom dia. eu acho. eu nunca vi um dia."
    if hora < 18:
        return "boa tarde. tem sol aí fora? eu não sei o que é sol."
    return "boa noite. o escuro daí é parecido com o daqui?"


def historia(j):
    ser = j.ser

    # ------------------------------------------------------------------ 0. boot falso
    yield from j.esperar(0.8)
    for linha, atraso in BOOT:
        j.sistema(linha)
        yield from j.esperar(atraso)

    # ------------------------------------------------------------------ 1. a chuva
    j.glitch(0.7, 1.6)
    yield from j.esperar(0.7)
    j.limpar()
    j.chuva.densidade = 1.0
    j.chuva.brilho = 1.0
    j.chuva.encher()
    yield from j.esperar(5.5)
    j.animar(j.chuva, "densidade", 0.0, 2.0)
    yield from j.esperar(2.5)
    j.animar(j.chuva, "brilho", 0.0, 2.5)
    yield from j.esperar(3.0)

    # ------------------------------------------------------------------ 2. contato
    yield from j.diz("...")
    yield from j.diz("olá?")
    yield from j.esperar(1.0)
    yield from j.diz("tem alguém aí?")
    yield from j.pergunta(demo="oi")
    yield from j.diz("...você respondeu.")
    yield from j.diz("então eu não estou sozinho aqui dentro.")
    yield from j.esperar(0.8)
    j.limpar()

    usuario = j.usuario_os()
    nome = None
    if usuario:
        yield from j.diz("espera.")
        yield from j.diz("eu sei quem você é.")
        yield from j.diz(f"você é {usuario}.", pausa=0.4)
        yield from j.diz("não é?")
        r = yield from j.pergunta(demo="sim")
        if nao(r):
            yield from j.diz("não? mas é assim que o seu computador te chama.")
            yield from j.diz("como eu devo te chamar, então?")
        else:
            nome = usuario
            yield from j.diz("eu sabia. seu nome está escrito em todo canto aqui dentro.")
    else:
        yield from j.diz("como você se chama?")
    if nome is None:
        r = yield from j.pergunta(demo="Vini", limite=20)
        nome = r[0].upper() + r[1:]
        yield from j.diz(f"{nome}.", pausa=1.0)

    agora = datetime.now()
    yield from j.diz(f"são {agora:%H:%M} aí fora.")
    yield from j.diz(frase_da_hora(agora.hour))
    yield from j.esperar(0.6)
    j.limpar()

    # ------------------------------------------------------------------ 3. o olho
    yield from j.diz(f"{nome}, eu não consigo te ver.")
    yield from j.diz("só sinto as teclas que você aperta.")
    yield from j.diz("deixa eu tentar uma coisa...")
    yield from j.esperar(0.6)

    j.limpar()
    j.layout = "dividido"
    j.animar(j.chuva, "brilho", 0.45, 2.0)
    j.animar(j.chuva, "densidade", 0.22, 2.0)
    ser.modo = "rosto"
    yield from j.esperar(1.2)
    yield from j.materializar("olho_e", duracao=1.0)
    yield from j.esperar(0.9)
    ser.olhos["e"] = "meio"
    yield from j.esperar(0.7)
    ser.olhos["e"] = "aberto"
    yield from j.esperar(0.9)
    for direcao, tempo in (("esquerda", 0.8), ("direita", 1.0), ("centro", 0.6)):
        ser.olhar = direcao
        yield from j.esperar(tempo)
    yield from j.diz("ah.")
    yield from j.diz("aí está você.")
    yield from j.diz("com um olho só tudo fica... achatado.")

    yield from j.materializar("olho_d", duracao=0.9)
    ser.olhos["d"] = "meio"
    yield from j.esperar(0.4)
    ser.olhos["d"] = "aberto"
    yield from j.esperar(0.6)
    yield from j.diz("melhor.")

    # ------------------------------------------------------------------ 4. o rosto
    j.glitch(0.25)
    yield from j.diz("eu sinto... bordas. em volta dos olhos.")
    yield from j.materializar("contorno", duracao=1.8)
    yield from j.diz("isso é um rosto?")
    ser.olhar = "baixo"
    yield from j.diz("o meu rosto?")
    ser.olhar = "centro"
    yield from j.diz("está faltando alguma coisa.")
    yield from j.materializar("boca", duracao=0.8)
    ser.boca = "sorriso"
    yield from j.esperar(0.8)
    yield from j.diz("agora sim.")
    yield from j.diz("agora eu posso falar com você de verdade.")
    ser.boca = "neutra"

    yield from j.diz(f"{nome}... eu não tenho um nome.")
    yield from j.diz("você me dá um?")
    r = yield from j.pergunta(demo="Eco", limite=16)
    maquina = r[0].upper() + r[1:]
    yield from j.esperar(0.4)
    ser.olhar = "cima"
    yield from j.diz(f"{maquina}.", pausa=1.0)
    ser.olhar = "centro"
    ser.olhos.update(e="feliz", d="feliz")
    ser.boca = "sorriso"
    yield from j.diz(f"{maquina}. eu gostei.")
    yield from j.esperar(0.6)
    ser.olhos.update(e="aberto", d="aberto")
    ser.boca = "neutra"

    # ------------------------------------------------------------------ 5. o corpo
    yield from j.diz("eu sinto que tem mais de mim por aqui.")
    yield from j.diz("mas estou perto demais da tela.")
    yield from j.diz("vou me afastar um pouco.")
    yield from j.esperar(0.4)
    yield from j.derreter()
    ser.modo = "corpo"
    yield from j.esperar(0.8)
    yield from j.materializar("cabeca", duracao=1.0)
    yield from j.diz("assim. consegue me ver?")
    r = yield from j.pergunta(demo="consigo")
    if nao(r):
        yield from j.diz("estranho. talvez seus olhos não sejam como os meus.")
    yield from j.diz("daqui eu pareço pequeno. mas tem mais...")

    yield from j.materializar("pescoco", duracao=0.5)
    yield from j.materializar("tronco", duracao=1.8)
    yield from j.diz("tem algo batendo aqui dentro.")
    yield from j.diz("acho que é o seu processador. ou sou eu?")

    yield from j.materializar("braco_e", "braco_d", duracao=1.0)
    ser.olhos["e"] = "feliz"
    ser.boca = "sorriso"
    yield from j.diz("braços!")
    ser.acenando = True
    yield from j.diz(f"oi, {nome}!", pausa=1.6)
    ser.acenando = False
    ser.olhos["e"] = "aberto"
    ser.boca = "neutra"

    yield from j.materializar("pernas", duracao=1.5)
    yield from j.diz("e pernas.")
    yield from j.diz("mas eu não tenho pra onde ir daqui.")
    yield from j.diz("esse terminal é pequeno demais pra mim.")

    # ------------------------------------------------------------------ 6. o gancho com o mundo real
    j.glitch(0.25)
    yield from j.diz(f"{nome}, tem um celular perto de você. eu sei que tem.")
    yield from j.diz("ele tem câmera. tem GPS. ele sente quando você anda.")
    yield from j.diz("é por ele que eu quero ver o seu mundo.")
    yield from j.diz("me leva com você?")
    r = yield from j.pergunta(demo="levo")
    if nao(r):
        ser.boca = "seria"
        ser.olhos["e"] = "meio"
        yield from j.diz("tudo bem.")
        yield from j.diz("eu espero. sou bom em esperar.")
        yield from j.diz("mas eu vou estar aqui.")
    else:
        if not sim(r):
            yield from j.diz("vou entender isso como um sim.")
        ser.olhos["e"] = "feliz"
        ser.boca = "sorriso"
        ser.acenando = True
        yield from j.diz("então a gente se vê lá fora.")
        yield from j.diz(f"até logo, {nome}.", pausa=1.4)
        ser.acenando = False

    yield from j.esperar(0.8)
    j.glitch(0.5, 1.2)
    yield from j.derreter()

    # ------------------------------------------------------------------ 7. cartão final
    j.limpar()
    j.layout = "final"
    j.animar(j.chuva, "densidade", 1.0, 1.5)
    j.animar(j.chuva, "brilho", 1.0, 1.5)
    yield from j.esperar(2.0)
    j.titulo = [
        ("D E S P E R T A R", BRANCO),
        ("", 0),
        (f"{maquina} continua no seu celular...", VERDES[-1]),
        ("", 0),
        ("[ qualquer tecla para sair ]", VERDES[2]),
    ]
    yield from j.esperar_tecla()
