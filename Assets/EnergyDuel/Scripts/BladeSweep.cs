using UnityEngine;

namespace EnergyDuel
{
    public readonly struct Hoja
    {
        public readonly Vector2 a,b;
        public Hoja(Vector2 a,Vector2 b){this.a=a;this.b=b;}
        public Vector2 Cerca(Vector2 p)
        {
            var d=b-a;
            return a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(1e-9f,d.sqrMagnitude));
        }
        public static float Cruz(Vector2 u,Vector2 v)=>u.x*v.y-u.y*v.x;
        public float Distancia(Hoja otra,out Vector2 punto)
        {
            var d=b-a;var e=otra.b-otra.a;var offset=otra.a-a;
            float den=Cruz(d,e);
            if(Mathf.Abs(den)>1e-8f)
            {
                float t=Cruz(offset,e)/den, s=Cruz(offset,d)/den;
                if(t>=0 && t<=1 && s>=0 && s<=1){punto=a+t*d;return 0;}
            }
            float mejor=float.MaxValue;Vector2 contacto=default;
            Comparar(a,otra.Cerca(a),ref mejor,ref contacto);
            Comparar(b,otra.Cerca(b),ref mejor,ref contacto);
            Comparar(otra.a,Cerca(otra.a),ref mejor,ref contacto);
            Comparar(otra.b,Cerca(otra.b),ref mejor,ref contacto);
            punto=contacto;return Mathf.Sqrt(mejor);
        }
        private static void Comparar(Vector2 x,Vector2 y,ref float distancia,ref Vector2 punto)
        {
            float s=(x-y).sqrMagnitude;
            if(s<distancia){distancia=s;punto=(x+y)*.5f;}
        }
        public static Hoja Interpolar(Hoja x,Hoja y,float t)=>new Hoja(Vector2.Lerp(x.a,y.a,t),Vector2.Lerp(x.b,y.b,t));

        // Avance conservador de dos cápsulas en movimiento simultáneo. La cota
        // de velocidad evita saltar sobre un contacto entre pasos de render.
        public static bool Cruzan(Hoja x0,Hoja x1,Hoja y0,Hoja y1,float radio,out Vector2 punto)
            =>Cruzan(x0,x1,y0,y1,radio,out punto,out _);
        public static bool Cruzan(Hoja x0,Hoja x1,Hoja y0,Hoja y1,float radio,out Vector2 punto,out float instante)
        {
            float viaje=Mathf.Max((x1.a-x0.a).magnitude,(x1.b-x0.b).magnitude)+Mathf.Max((y1.a-y0.a).magnitude,(y1.b-y0.b).magnitude);
            float t=0;instante=0;
            for(int i=0;i<12;i++)
            {
                float gap=Interpolar(x0,x1,t).Distancia(Interpolar(y0,y1,t),out punto)-radio;
                if(gap<=.0005f){instante=t;return true;}
                if(viaje<.0001f || gap>(1-t)*viaje)return false;
                t+=gap/viaje;
            }
            // Contacto tangencial muy cercano: se resuelve como bloqueo conservador.
            instante=t;return Interpolar(x0,x1,t).Distancia(Interpolar(y0,y1,t),out punto)<=radio+.002f;
        }
    }
}
