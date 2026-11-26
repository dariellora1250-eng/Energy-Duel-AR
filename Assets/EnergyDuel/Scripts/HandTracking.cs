using UnityEngine;

namespace EnergyDuel
{
    public enum Arma { Ninguna, Espada, Escudo }

    public static class Gestos
    {
        // Articulaciones MCP/PIP/DIP y alcance relativo a la palma. Las métricas 3D
        // conservan sus ángulos aunque el jugador gire la muñeca.
        public static float Extension(Vector3[] p)
        {
            float palma = Mathf.Max(.0001f, Vector3.Distance(p[0], p[9]));
            float suma = 0, menor=1, mayor=0;
            for (int m = 5; m <= 17; m += 4)
            {
                Vector3 a = p[m+1]-p[m], b = p[m+2]-p[m+1], c = p[m+3]-p[m+2];
                float pip = Vector3.Dot(a.normalized,b.normalized);
                float dip = Vector3.Dot(b.normalized,c.normalized);
                float alcance = Vector3.Distance(p[0],p[m+3])/palma;
                float dedo = .50f * Mathf.InverseLerp(-.15f,.98f,pip) +
                    .18f * Mathf.InverseLerp(0,.98f,dip) + .32f * Mathf.InverseLerp(.85f,1.75f,alcance);
                suma+=dedo; menor=Mathf.Min(menor,dedo); mayor=Mathf.Max(mayor,dedo);
            }
            return .5f*suma*.25f+.25f*(suma-menor-mayor);
        }
    }

    public sealed class Pista
    {
        public bool asignada, valida, memoria;
        public float ultima = -100, delta, escala;
        public Vector2 centro, velocidad;
        public Vector3 posicion;
        public Vector3 normalPalma=Vector3.back, ejePalma=Vector3.up;
        public float mezclaRotacion;
        public Arma arma;
        public float lateralidad;
        public float recibida=-100;
        public bool reentrada;
        private float desde, tiempoEnCasa;
        private Arma candidata;

        public void Perder()
        {
            valida = false;
            candidata = Arma.Ninguna;
        }

        public void Expirar(float ahora, DuelRules reglas)
        {
            float vigencia=Mathf.Clamp(1.6f*delta,reglas.vigencia,Mathf.Max(reglas.vigencia,.32f));
            if (ahora-recibida>vigencia || ahora-ultima>reglas.edadMaxima) Perder();
            if (ahora - ultima > reglas.olvido) { asignada = false; arma = Arma.Ninguna; }
            if (ahora-ultima>8) memoria=false;
        }

        public void Observar(DuelCapture.Mano m, float t, DuelRules r)
        {
            var p = m.imagen;
            var basePalma = (p[5]+p[9]+p[13]+p[17])*.25f;
            var palma = Vector3.Lerp(p[0],basePalma,.62f);
            Vector2 nueva = palma;
            delta = Mathf.Max(.001f,t-ultima);
            bool continua = valida && delta <= r.edadMaxima;
            reentrada=!continua;
            float mezcla=continua ? 1-Mathf.Exp(-(r.suavizado*.65f+16*Mathf.Min(2,(nueva-centro).magnitude/delta))*delta) : 1;
            velocidad = continua ? Vector2.Lerp(velocidad, Vector2.ClampMagnitude((nueva-centro)/delta,2f),.6f) : Vector2.zero;
            float medida = Mathf.Clamp((Vector3.Distance(p[0],basePalma)+Vector3.Distance(p[5],p[17]))*.5f,.024f,.19f);
            Vector3 arriba = (basePalma-p[0]).normalized;
            Vector3 normal = Vector3.Cross(p[5]-p[17],arriba).normalized;
            if (normal.z > 0) normal = -normal;
            // Un perfil lateral aún debe dejar visible la cara holográfica.
            normal = (normal + Vector3.back*.65f).normalized;
            var eje = Vector3.ProjectOnPlane(arriba,normal);
            if (eje.sqrMagnitude>.01f) { normalPalma=normal; ejePalma=eje; }
            posicion = Vector3.Lerp(posicion,new Vector3(nueva.x,nueva.y,0),mezcla);
            mezclaRotacion = mezcla;
            escala = Mathf.Lerp(escala,medida,mezcla);
            if (!continua) { if(!asignada || delta>r.retencionVisual+r.desvanecer) arma=Arma.Ninguna; candidata=Arma.Ninguna; }
            if (!asignada)
            {
                if(delta>.35f || Vector2.Distance(nueva,centro)>.10f)tiempoEnCasa=0;
                else tiempoEnCasa+=Mathf.Min(delta,.12f);
                asignada=tiempoEnCasa>=r.calibracion;
            }
            float extension = Gestos.Extension(m.geometria);
            var gesto = extension >= r.escudoDesde ? Arma.Escudo : extension <= r.espadaHasta ? Arma.Espada : arma;
            if (gesto != candidata) { candidata = gesto; desde = t; }
            if (t-desde >= r.confirmarGesto) arma = candidata;
            lateralidad = continua ? Mathf.Lerp(lateralidad,m.lateralidad,.22f) : m.lateralidad;
            centro = nueva;
            memoria |= asignada;
            valida = true;
            ultima = recibida = t;
        }
    }

    public sealed class HandTracking
    {
        public bool Ambigua {get;private set;}
        private float ambiguaDesde=-1;
        public readonly Pista[] jugadores = { new Pista(), new Pista() };

        private bool Reconocible(int j,DuelCapture.Mano m,Vector2 centro)
        {
            var p=jugadores[j];var otro=jugadores[1-j];
            if(!p.memoria)return false;
            float afinidad=p.lateralidad*m.lateralidad;
            bool exclusiva=afinidad>.65f && otro.memoria && otro.lateralidad*m.lateralidad<-.40f;
            return exclusiva || (afinidad>-.25f && Vector2.Distance(centro,p.centro)<.22f);
        }

        private float Coste(int j, int i, DuelCapture.Frame frame, float aspecto)
        {
            var pista = jugadores[j];
            if (i < 0) return pista.asignada ? .80f : .33f;
            var mano = frame.manos[i];
            var p = mano.imagen;
            Vector2 centro = Vector3.Lerp(p[0],(p[5]+p[9]+p[13]+p[17])*.25f,.62f);
            if (Vector3.Distance(p[0],p[9]) < .009f) return 100;
            if (!pista.asignada)
            {
                if(Reconocible(j,mano,centro))return .06f+Mathf.Min(.15f,Vector2.Distance(centro,pista.centro)*.2f);
                float lado = centro.x/aspecto * (j == 0 ? -1 : 1);
                if (lado < .08f || lado > .5f) return 100;
                return .25f + Mathf.Min(.06f,Mathf.Abs(lado-.25f)*.2f);
            }
            float dt = Mathf.Clamp(frame.tiempo-pista.ultima,0,.14f);
            float distancia = Vector2.Distance(centro,pista.centro+pista.velocidad*dt);
            float edad=frame.tiempo-pista.ultima;
            float afinidad=pista.lateralidad*mano.lateralidad;
            float radio=.14f+Mathf.Min(.55f,edad*1.9f);
            if(afinidad>.65f && !pista.valida) radio+=.25f;
            if(distancia>radio)return 100;
            return distancia*.70f + .15f*Mathf.Max(0,-afinidad);
        }

        public void Aplicar(DuelCapture.Frame frame, float aspecto, DuelRules r)
        {
            // Evalúa las dos permutaciones globalmente. Un lado de pantalla sólo
            // identifica al jugador durante la calibración inicial o la recuperación.
            for (int j=0;j<2;j++)
            {
                jugadores[j].Expirar(frame.tiempo,r);
            }
            int a=-1,b=-1,alternoA=-1,alternoB=-1;
            float mejor=float.PositiveInfinity, segundo=float.PositiveInfinity;
            for (int i=-1;i<frame.cantidad;i++)
            for (int k=-1;k<frame.cantidad;k++)
            {
                if (i >= 0 && i == k) continue;
                float coste = Coste(0,i,frame,aspecto)+Coste(1,k,frame,aspecto);
                if (coste < mejor) { segundo=mejor;alternoA=a;alternoB=b; mejor=coste; a=i; b=k; }
                else if(coste<segundo){segundo=coste;alternoA=i;alternoB=k;}
            }
            // Ante una superposición ambigua se suspende el combate de esa muestra.
            Ambigua=jugadores[0].memoria && jugadores[1].memoria && segundo-mejor < .018f;
            if (Ambigua)
            {
                if(a!=alternoA)a=-1;
                if(b!=alternoB)b=-1;
                if(ambiguaDesde<0)ambiguaDesde=frame.tiempo;
                if(frame.tiempo-ambiguaDesde>.7f)
                {
                    for(int j=0;j<2;j++)if((j==0?a:b)<0)jugadores[j].memoria=jugadores[j].asignada=false;
                }
            }
            else ambiguaDesde=-1;
            for (int j=0;j<2;j++)
            {
                int i=j==0?a:b;
                if (i<0) jugadores[j].Perder();
                else
                {
                    var m=frame.manos[i];var p=m.imagen;
                    var centro=Vector3.Lerp(p[0],(p[5]+p[9]+p[13]+p[17])*.25f,.62f);
                    if(!jugadores[j].asignada && Reconocible(j,m,centro))jugadores[j].asignada=true;
                    jugadores[j].Observar(m,frame.tiempo,r);
                }
            }
        }
    }
}
