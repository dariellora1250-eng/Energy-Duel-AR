using UnityEngine;

namespace EnergyDuel
{
    public sealed class TiempoDuelo
    {
        private float sinManos;
        public float Duracion {get;private set;}
        public bool Suspendido=>sinManos>=2;
        public void Avanzar(float dt,bool alguna)
        {
            if(alguna){sinManos=0;Duracion+=dt;return;}
            Duracion+=Mathf.Min(dt,Mathf.Max(0,2-sinManos));sinManos+=dt;
        }
    }
    public sealed class DuelMatch : MonoBehaviour
    {
        public DuelCapture captura;
        public DuelRules reglas;
        public DuelHud hud;
        public DuelMenu menu;
        public DuelWeapon[] armas;
        public Transform[] zonas;
        private readonly int[] vidas=new int[2],golpes=new int[2],bloqueos=new int[2];
        private readonly DuelCombat[] ataques=new DuelCombat[2];
        private readonly Hoja[] anterior=new Hoja[2],hojas=new Hoja[2];
        private readonly Arma[] previa=new Arma[2],actual=new Arma[2];
        private readonly bool[] tocando=new bool[2],rechazado=new bool[2];
        private readonly Vector2[] direccion=new Vector2[2],referencia=new Vector2[2];
        private bool cruce;
        private bool escudosEnContacto;
        private readonly float[] duracionContacto=new float[2], sancionFin=new float[2];
        private readonly Arma[] armaActiva=new Arma[2];
        private DuelAudio audioDuelo;
        private HandTracking pistas=new HandTracking();
        private TiempoDuelo reloj=new TiempoDuelo();
        private enum Fase { Menu,Calibrar,Cuenta,Combate,Final }
        private Fase fase;
        private float inicioCuenta,aspectoAnterior=-1,antes,umbralBase,suavizadoBase;
        private DuelRules copia;
        private void Start()
        {
            copia=Instantiate(reglas);reglas=copia;
            audioDuelo=GetComponent<DuelAudio>();if(!audioDuelo)audioDuelo=gameObject.AddComponent<DuelAudio>();
            umbralBase=reglas.espadaHasta;suavizadoBase=reglas.suavizado;
            AjustarControles();hud.Iniciar(reglas);menu.Iniciar(this);
            for(int j=0;j<2;j++){armas[j].Ocultar();zonas[j].gameObject.SetActive(false);}
        }
        public void AjustarControles()
        {
            reglas.espadaHasta=Mathf.Clamp(umbralBase+captura.opciones.sensibilidad,.15f,reglas.escudoDesde-.035f);
            reglas.suavizado=suavizadoBase*captura.opciones.respuesta;
        }
        public void NuevaPartida()
        {
            pistas=new HandTracking();reloj=new TiempoDuelo();fase=Fase.Calibrar;cruce=false;
            hud.resultado.text="";hud.Reiniciar();menu.Abrir(Pantalla.Juego);
            for(int j=0;j<2;j++)
            {
                vidas[j]=reglas.vidaInicial;golpes[j]=bloqueos[j]=0;ataques[j]=new DuelCombat();
                previa[j]=actual[j]=armaActiva[j]=Arma.Ninguna;tocando[j]=rechazado[j]=false;
                duracionContacto[j]=sancionFin[j]=0;armas[j].Renovar();
                armas[j].Ocultar();zonas[j].gameObject.SetActive(true);
            }
        }
        private void LateUpdate()
        {
            float t=captura.Segundos,dt=Mathf.Max(0,t-antes);antes=t;
            if(aspectoAnterior!=captura.Aspecto)
            {
                aspectoAnterior=captura.Aspecto;
                for(int j=0;j<2;j++)
                {
                    var uv=j==0?reglas.zonaJ1:reglas.zonaJ2;
                    zonas[j].position=new Vector3((uv.x-.5f)*aspectoAnterior,uv.y-.5f,.4f);
                    zonas[j].localScale=Vector3.one*reglas.radioZona;
                }
            }
            bool nueva=captura.Leer(out var frame) && t-frame.tiempo<=reglas.edadMaxima;
            if(nueva)
            {
                menu.Muestra(frame,frame.tiempo);
                pistas.Aplicar(frame,aspectoAnterior,reglas);
                foreach(var p in pistas.jugadores)if(p.valida)p.recibida=t;
            }
            menu.Datos(t,pistas.jugadores);
            if(menu.Actual==Pantalla.Principal)audioDuelo.MusicaMenu();
            else if(menu.Actual==Pantalla.Victoria)audioDuelo.MusicaVictoria();
            else if(menu.Actual==Pantalla.Juego && fase==Fase.Combate)audioDuelo.MusicaCombate();
            if(menu.Actual!=Pantalla.Juego)
            {
                hud.Mensaje(captura.Preparada ? captura.AvisoResolucion : captura.Estado);
                menu.Navegar(t,reglas.tiempoSeleccion,reglas.edadMaxima);
                return;
            }
            int reconocidas=0;
            for(int j=0;j<2;j++)
            {
                var p=pistas.jugadores[j];p.Expirar(t,reglas);
                if(p.valida && p.asignada)reconocidas++;
                armas[j].Mostrar(p,t,reglas);
                Arma solicitada=ModoValido(p);
                if(solicitada!=Arma.Ninguna && solicitada!=armaActiva[j])
                {
                    armaActiva[j]=solicitada;
                    if(t>=sancionFin[j])armas[j].Renovar();
                    audioDuelo.SonidoArma(solicitada==Arma.Espada);
                }
                if(solicitada!=Arma.Ninguna && t>=sancionFin[j] && armas[j].Rota){armas[j].Renovar();audioDuelo.SonidoArma(solicitada==Arma.Espada);}
                if(t<sancionFin[j])
                {
                    actual[j]=Arma.Ninguna;
                    hud.Mensaje("J"+(j+1)+" · arma rota. Recuperación "+Mathf.CeilToInt(sancionFin[j]-t)+" s");
                }
                else if(armaActiva[j]!=Arma.Ninguna && armas[j].Rota)actual[j]=Arma.Ninguna;
                else actual[j]=solicitada;
                if(armas[j].Rota && armaActiva[j]!=Arma.Ninguna && t>=sancionFin[j])
                {
                    sancionFin[j]=Mathf.Max(sancionFin[j],t+(armaActiva[j]==Arma.Espada?reglas.penalizacionEspada:reglas.penalizacionEscudo));
                    hud.Evento(j,armaActiva[j]==Arma.Espada?"ESPADA ROTA":"ESCUDO ROTO",t);
                    audioDuelo.SonidoRoto();armas[j].durabilidad=0;armaActiva[j]=Arma.Ninguna;
                }
                if(actual[j]!=Arma.Espada || (nueva && frame.salto))ataques[j].Interrumpir();
                if(nueva && (frame.salto || p.reentrada))previa[j]=Arma.Ninguna;
            }
            if(fase==Fase.Calibrar)
            {
                hud.Mensaje("J1 a la izquierda y J2 a la derecha. Mantengan una mano quieta.");
                if(reconocidas==2){inicioCuenta=t;fase=Fase.Cuenta;}
            }
            else if(fase==Fase.Cuenta)
            {
                float falta=3-(t-inicioCuenta);
                hud.resultado.text=falta>2?"3":falta>1?"2":falta>0?"1":"";
                if(falta<=0)fase=Fase.Combate;
            }
            else if(fase==Fase.Combate)
            {
                reloj.Avanzar(dt,reconocidas>0);
                hud.Mensaje(reloj.Suspendido?"Buscando manos. El duelo continúa al recuperar cualquiera.":
                    pistas.Ambigua?"Separen las manos para recuperar su identidad.":
                    (!pistas.jugadores[0].asignada || !pistas.jugadores[1].asignada)?"Para recalibrar la mano perdida, muéstrala en su lado.":
                    "Golpea el núcleo rival. Abre la mano para defender.");
                if(!reloj.Suspendido)Resolver(t,dt);
            }
            if(!captura.Preparada)hud.Mensaje(captura.Estado);
            hud.Tiempo(reloj.Duracion);
            for(int j=0;j<2;j++)hud.Jugador(j,vidas[j],reglas.vidaInicial,pistas.jugadores[j],t);
        }
        public static Arma ModoValido(Pista p)=>p.valida && p.asignada && !p.reentrada?p.arma:Arma.Ninguna;

        private bool Escudo(int j,out Vector2 punto,out float instante)
        {
            int rival=1-j;var esc=armas[rival];var h=hojas[j];var old=previa[j]==Arma.Espada?anterior[j]:h;
            punto=h.a;instante=1;bool contacto=false;
            for(int i=0;i<6;i++)
            {
                int k=(i+1)%6;
                var edge=new Hoja(esc.contorno[i],esc.contorno[k]);
                var before=previa[rival]==Arma.Escudo?new Hoja(esc.anteriorEscudo[i],esc.anteriorEscudo[k]):edge;
                if(Hoja.Cruzan(old,h,before,edge,armas[j].Radio,out var cruceBorde,out float cuando) && cuando<=instante)
                {instante=cuando;punto=cruceBorde;contacto=true;}
            }
            return contacto || DuelCombat.CortaPoligono(h.a,h.b,esc.contorno);
        }
        private void Rebote(int j,Vector2 punto,bool escudo,float t,float instante)
        {
            var h=hojas[j];var avance=h.b-anterior[j].b;
            direccion[j]=avance.sqrMagnitude>.000001f?avance.normalized:(h.b-h.a).normalized;
            referencia[j]=h.b;rechazado[j]=true;ataques[j].Interrumpir();
            Vector2 rival=armas[1-j].transform.position;
            float lado=Hoja.Cruz(h.b-h.a,rival-h.a);
            int signo=Mathf.Abs(lado)<.0001f?(j==0?1:-1):lado>0?-1:1;
            var desplazamiento=previa[j]==Arma.Espada?(anterior[j].a-h.a)*(1-instante):Vector2.zero;
            armas[j].Rebotar(t,punto,escudo?reglas.giroBloqueo:reglas.giroDuelo,escudo?reglas.tiempoBloqueo:reglas.tiempoDuelo,signo,desplazamiento);
        }
        private void Resolver(float t,float dt)
        {
            for(int j=0;j<2;j++)hojas[j]=armas[j].Natural;
            bool choque=false;
            if(actual[0]==Arma.Espada && actual[1]==Arma.Espada)
            {
                var a=previa[0]==Arma.Espada?anterior[0]:hojas[0];var b=previa[1]==Arma.Espada?anterior[1]:hojas[1];
                float radio=armas[0].Radio+armas[1].Radio;
                choque=Hoja.Cruzan(a,hojas[0],b,hojas[1],radio+(cruce?.003f:0),out var contacto,out float instante);
                if(choque)
                {
                    if(!cruce){Rebote(0,contacto,false,t,instante);Rebote(1,contacto,false,t,instante);armas[0].Desgastar(reglas.desgasteChoqueEspadas);armas[1].Desgastar(reglas.desgasteChoqueEspadas);audioDuelo.SonidoChoque();hud.Evento(0,"CHOQUE",t);hud.Evento(1,"CHOQUE",t);}
                    int presion=Vector2.Dot(hojas[0].b-anterior[0].b,hojas[0].b-hojas[0].a)>Vector2.Dot(hojas[1].b-anterior[1].b,hojas[1].b-hojas[1].a)?0:1;
                    duracionContacto[presion]+=dt;
                    if(duracionContacto[presion]>reglas.demoraPresion)armas[presion].Desgastar(reglas.desgastePresionSegundo*dt);
                    armas[0].Limitar(hojas[1],radio);armas[1].Limitar(hojas[0],radio);
                }
            }
            if(!choque)duracionContacto[0]=duracionContacto[1]=0;
            cruce=choque;
            // Prueba continua de contacto entre los polígonos de ambos escudos.
            bool escudos=actual[0]==Arma.Escudo && actual[1]==Arma.Escudo && !armas[0].Rota && !armas[1].Rota;
            bool choqueEscudos=false;
            if(escudos)for(int i=0;i<6&&!choqueEscudos;i++)for(int k=0;k<6;k++)
                if(Hoja.Cruzan(new Hoja(armas[0].contorno[i],armas[0].contorno[(i+1)%6]),new Hoja(armas[0].contorno[i],armas[0].contorno[(i+1)%6]),new Hoja(armas[1].contorno[k],armas[1].contorno[(k+1)%6]),new Hoja(armas[1].contorno[k],armas[1].contorno[(k+1)%6]),.025f,out _,out _)){choqueEscudos=true;break;}
            if(choqueEscudos&&!escudosEnContacto)
            {
                for(int j=0;j<2;j++){armas[j].PulsarEscudo(t,reglas.giroChoqueEscudos,reglas.tiempoChoqueEscudos,j==0?1:-1);armas[j].Desgastar(reglas.desgasteChoqueEscudos);audioDuelo.SonidoChoque(true);hud.Evento(j,"CHOQUE DE ESCUDOS",t);}
            }
            escudosEnContacto=choqueEscudos;
            for(int j=0;j<2;j++)
            {
                int rival=1-j;var h=hojas[j];Vector2 punto=default;
                float instante=1;
                bool bloqueo=actual[j]==Arma.Espada && actual[rival]==Arma.Escudo && !armas[rival].Rota && Escudo(j,out punto,out instante);
                if(bloqueo)
                {
                    if(!tocando[j]){Rebote(j,punto,true,t,instante);armas[j].Desgastar(reglas.desgasteEspadaEscudo);armas[rival].Desgastar(reglas.desgasteEscudoBloqueando);audioDuelo.SonidoChoque(true);armas[rival].Bloquear(t);bloqueos[rival]++;hud.Evento(rival,"BLOQUEO",t);}
                    var borde=armas[rival].contorno;
                    for(int i=0;i<6;i++)armas[j].Limitar(new Hoja(borde[i],borde[(i+1)%6]),armas[j].Radio);
                }
                tocando[j]=bloqueo;
                if(actual[j]!=Arma.Espada || armas[j].Rota){rechazado[j]=false;continue;}
                if(bloqueo || choque)continue;
                if(rechazado[j])
                {
                    // Un ataque rechazado sigue consumido aunque la hoja avance más allá del escudo.
                    float avance=Vector2.Dot(h.b-referencia[j],direccion[j]);
                    if(avance>0)referencia[j]+=avance*direccion[j];
                    if(avance>-.018f)continue;
                    rechazado[j]=false;
                }
                if(!armas[j].Rota && t>=sancionFin[j] && ataques[j].Evaluar(h.a,h.b,zonas[rival].position,reglas.radioZona,null,t,dt,reglas)==Contacto.Impacto)
                {vidas[rival]=Mathf.Max(0,vidas[rival]-reglas.dano);golpes[j]++;hud.Evento(j,"IMPACTO",t);}
            }
            for(int j=0;j<2;j++){anterior[j]=hojas[j];previa[j]=actual[j];}
            if(vidas[0]>0 && vidas[1]>0)return;
            fase=Fase.Final;for(int j=0;j<2;j++){armas[j].Ocultar();zonas[j].gameObject.SetActive(false);}
            menu.Resultado(vidas,golpes,bloqueos,reloj.Duracion);
        }
        private void OnDestroy(){if(copia)Destroy(copia);}
    }
}
