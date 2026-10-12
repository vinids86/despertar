using UnityEngine;
using UnityEngine.Rendering;

// Efeitos feitos só de primitivas: rastro do tiro, faíscas, clarão e o "código" que sobra quando um Agente some.
public static class Efeitos
{
    static MaterialPropertyBlock mpb;
    static readonly int CorID = Shader.PropertyToID("_BaseColor");

    public static void Rastro(Vector3 de, Vector3 para, Color cor, float duracao)
    {
        var go = new GameObject("Rastro");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, de);
        lr.SetPosition(1, para);
        lr.startWidth = 0.035f;
        lr.endWidth = 0.012f;
        lr.sharedMaterial = Recursos.I.rastro;
        lr.startColor = cor;
        lr.endColor = new Color(cor.r, cor.g, cor.b, cor.a * 0.2f);
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;
        go.AddComponent<Sumir>().Iniciar(lr, duracao);
    }

    public static void Faiscas(Vector3 ponto, Vector3 normal, Color cor, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var go = Cubo(ponto, 0.05f, cor);
            Vector3 vel = (normal + Random.insideUnitSphere * 0.9f).normalized * Random.Range(3f, 9f);
            go.AddComponent<Fragmento>().Iniciar(vel, Random.Range(0.15f, 0.4f), 12f);
        }
    }

    // Blocos verdes: surgem subindo (materializar) ou se espalham (desfazer).
    public static void Codigo(Vector3 centro, int quantidade, bool espalhar)
    {
        for (int i = 0; i < quantidade; i++)
        {
            Vector3 p = centro + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.9f, 0.9f), Random.Range(-0.4f, 0.4f));
            var cor = new Color(0.2f, Random.Range(1.5f, 4f), 0.5f);
            var go = Cubo(p, Random.Range(0.06f, 0.14f), cor);
            Vector3 vel = espalhar
                ? (p - centro).normalized * Random.Range(1f, 4f) + Vector3.up * Random.Range(0f, 2f)
                : Vector3.up * Random.Range(0.5f, 2f);
            go.AddComponent<Fragmento>().Iniciar(vel, Random.Range(0.4f, 1.1f), espalhar ? 4f : -1f);
        }
    }

    public static void Clarao(Vector3 ponto, Color cor)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.position = ponto;
        go.transform.localScale = Vector3.one * 0.25f;
        Configurar(go.GetComponent<Renderer>(), cor);
        go.AddComponent<Fragmento>().Iniciar(Vector3.zero, 0.06f, 0f);
    }

    static GameObject Cubo(Vector3 ponto, float tamanho, Color cor)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetPositionAndRotation(ponto, Random.rotation);
        go.transform.localScale = Vector3.one * tamanho;
        Configurar(go.GetComponent<Renderer>(), cor);
        return go;
    }

    static void Configurar(Renderer r, Color cor)
    {
        r.sharedMaterial = Recursos.I.bala;
        r.shadowCastingMode = ShadowCastingMode.Off;
        mpb ??= new MaterialPropertyBlock();
        mpb.Clear();
        mpb.SetColor(CorID, cor);
        r.SetPropertyBlock(mpb);
    }
}
