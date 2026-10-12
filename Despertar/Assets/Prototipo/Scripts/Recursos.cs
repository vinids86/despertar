using UnityEngine;

// Materiais usados pelo que é criado durante o jogo (Agentes, balas, efeitos).
// Preenchido pelo montador da cena (Despertar > Montar escritório).
public class Recursos : MonoBehaviour
{
    public static Recursos I;

    public Material agente;
    public Material visor;
    public Material bala;
    public Material rastro;

    void Awake() => I = this;
}
