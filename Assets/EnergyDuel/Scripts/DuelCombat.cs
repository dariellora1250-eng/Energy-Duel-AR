using UnityEngine;

namespace EnergyDuel
{
    public enum Contacto { Ninguno, Bloqueo, Impacto }

    public sealed class DuelCombat
    {
        private bool preparado, armado;
        private Vector2 baseAnterior, puntaAnterior;
        private float proximo, recorrido;

        public void Interrumpir() { preparado=false; armado=false; recorrido=0; }

        // Polígono barrido entre dos muestras. Se comprueban también los extremos
        // para detectar una hoja que cruza la zona entera entre inferencias.
        public Contacto Evaluar(Vector2 inicio, Vector2 punta, Vector2 objetivo, float radio,
            Vector2[] escudo, float t, float dt, DuelRules r)
        {
            bool dentro = Distancia(objetivo,inicio,punta)<=radio;
            bool defendido = escudo!=null && CortaPoligono(inicio,punta,escudo);
            Contacto resultado=Contacto.Ninguno;
            if (preparado)
            {
                float paso=Mathf.Max(Vector2.Distance(puntaAnterior,punta),Vector2.Distance(baseAnterior,inicio));
                float rapidez=paso/Mathf.Max(.001f,dt);
                recorrido=rapidez>=r.velocidadMinima*.5f ? recorrido+paso : 0;
                bool barrido=dentro || Distancia(objetivo,baseAnterior,inicio)<=radio ||
                    Distancia(objetivo,puntaAnterior,punta)<=radio || EnBarrido(objetivo,inicio,punta);
                bool bloqueo=defendido;
                if (!bloqueo && escudo!=null)
                    bloqueo=CortaPoligono(baseAnterior,inicio,escudo) || CortaPoligono(puntaAnterior,punta,escudo) ||
                        CortaPoligono(baseAnterior,puntaAnterior,escudo) || EnBarrido(escudo[0],inicio,punta);
                if (armado && t>=proximo && rapidez>=r.velocidadMinima && recorrido>=r.recorridoMinimo)
                    resultado=bloqueo ? Contacto.Bloqueo : barrido ? Contacto.Impacto : Contacto.Ninguno;
                if (barrido || bloqueo) armado=false;
            }
            if (resultado!=Contacto.Ninguno) { proximo=t+r.intervalo; recorrido=0; }
            if (!dentro && !defendido && t>=proximo) armado=true;
            baseAnterior=inicio;
            puntaAnterior=punta;
            preparado=true;
            return resultado;
        }

        private bool EnBarrido(Vector2 p,Vector2 inicio,Vector2 punta) =>
            Triangulo(p,baseAnterior,puntaAnterior,punta) || Triangulo(p,baseAnterior,punta,inicio);

        private static float Cruz(Vector2 a,Vector2 b) => a.x*b.y-a.y*b.x;
        private static bool Triangulo(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
        {
            if (Mathf.Abs(Cruz(b-a,c-a))<.0000001f) return false;
            float u=Cruz(b-a,p-a), v=Cruz(c-b,p-b), w=Cruz(a-c,p-c);
            return (u>=0 && v>=0 && w>=0) || (u<=0 && v<=0 && w<=0);
        }
        private static float Distancia(Vector2 p,Vector2 a,Vector2 b)
        {
            Vector2 d=b-a;
            return (p-a-d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0000001f,d.sqrMagnitude))).magnitude;
        }
        public static bool CortaPoligono(Vector2 a,Vector2 b,Vector2[] pol)
        {
            // Hexágono convexo proyectado. Los triángulos también cubren una hoja completamente interior.
            for (int i=0;i<pol.Length;i++)
            {
                var c=pol[i]; var d=pol[(i+1)%pol.Length];
                if (Triangulo(a,pol[0],c,d) || Triangulo(b,pol[0],c,d)) return true;
                float den=Cruz(b-a,d-c);
                if (Mathf.Abs(den)<.0000001f) continue;
                float s=Cruz(c-a,d-c)/den, u=Cruz(c-a,b-a)/den;
                if (s>=0 && s<=1 && u>=0 && u<=1) return true;
            }
            return false;
        }
    }
}
