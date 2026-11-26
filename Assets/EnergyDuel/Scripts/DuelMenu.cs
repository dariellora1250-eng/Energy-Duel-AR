using System;
using UnityEngine;
using UnityEngine.UI;

namespace EnergyDuel
{
    public enum Pantalla { Juego, Principal, Configuracion, Instrucciones, Victoria }
    public enum Accion { Jugar,Configurar,Ayuda,Salir,Volver,Guardar,Perfil,Resolucion,Frecuencia,Deteccion,Presencia,Seguimiento,Sensibilidad,Suavizado,Medir }
    public sealed class Permanencia
    {
        private int opcion=-1;
        private float desde;
        private bool consumida;
        public float Progreso {get;private set;}
        public bool Paso(int id,float t,float espera)
        {
            if(id!=opcion){opcion=id;desde=t;consumida=false;}
            Progreso=id<0?0:Mathf.Clamp01((t-desde)/espera);
            if(consumida || Progreso<1)return false;
            consumida=true;return true;
        }
    }
    public sealed class DuelMenu : MonoBehaviour
    {
        [Serializable] public class Boton
        {
            public RectTransform zona;
            public Image progreso;
            public Pantalla pagina;
            public Accion accion;
            public int paso;
        }
        public GameObject[] paginas;
        public Boton[] botones;
        public Text[] valores;
        public Text ganador, resumen, diagnostico, nota;
        public RectTransform[] cursores;
        public GameObject[] indicadoresJuego;
        public Pantalla Actual {get;private set;}
        private DuelMatch juego;
        private DuelOptions borrador;
        private readonly Permanencia[] seleccion={new Permanencia(),new Permanencia()};
        private readonly Vector2[] puntos=new Vector2[2], ultimos=new Vector2[2], salida=new Vector2[2];
        private readonly bool[] bloquear={false,false};
        private int cantidad;
        private readonly int[] objetivos={-1,-1};
        private readonly float[] avances=new float[2];
        private float muestra=-100, proximoDato;
        private static readonly int[] frecs={10,15,20,30};
        public void Iniciar(DuelMatch partida){juego=partida;Abrir(Pantalla.Principal);}
        public void Abrir(Pantalla pantalla)
        {
            Actual=pantalla;
            for(int i=0;i<paginas.Length;i++) paginas[i].SetActive(i+1==(int)pantalla);
            for(int i=0;i<indicadoresJuego.Length;i++)indicadoresJuego[i].SetActive(pantalla==Pantalla.Juego);
            for(int j=0;j<2;j++){cursores[j].gameObject.SetActive(false);seleccion[j].Paso(-1,0,1);}
            foreach(var b in botones)b.progreso.fillAmount=0;
            if(pantalla==Pantalla.Configuracion){borrador=juego.captura.opciones.Copia();ActualizarValores();}
        }
        public void Resultado(int[] vidas,int[] golpes,int[] bloqueos,float duracion)
        {
            Abrir(Pantalla.Victoria);
            int vencedor=vidas[0]==vidas[1]?-1:vidas[0]>vidas[1]?0:1;
            ganador.text=vencedor<0?"EMPATE":vencedor==0?"VICTORIA  ·  ION":"VICTORIA  ·  SOLAR";
            ganador.color=vencedor<0?Color.white:vencedor==0?new Color(.18f,.89f,1):new Color(1,.43f,.19f);
            resumen.text=$"Duración   {(int)duracion/60:00}:{(int)duracion%60:00}\n\nION     {vidas[0]} PV     {golpes[0]} impactos     {bloqueos[0]} bloqueos\nSOLAR     {vidas[1]} PV     {golpes[1]} impactos     {bloqueos[1]} bloqueos";
        }
        public void Muestra(DuelCapture.Frame frame,float t)
        {
            cantidad=frame.cantidad;muestra=t;
            for(int i=0;i<cantidad;i++)
            {
                var p=frame.manos[i].imagen;
                puntos[i]=juego.captura.camara.WorldToScreenPoint(Vector3.Lerp(p[0],p[9],.65f));
            }
            if(cantidad==2 && (ultimos[0]-puntos[1]).sqrMagnitude+(ultimos[1]-puntos[0]).sqrMagnitude <
                (ultimos[0]-puntos[0]).sqrMagnitude+(ultimos[1]-puntos[1]).sqrMagnitude)
                (puntos[0],puntos[1])=(puntos[1],puntos[0]);
            for(int j=0;j<cantidad;j++)ultimos[j]=puntos[j];
        }
        public void Navegar(float t,float confirmar,float vigencia)
        {
            if(Actual==Pantalla.Juego)return;
            if(t-muestra>vigencia)cantidad=0;
            if(Input.GetMouseButtonDown(0))
            {
                int i=Objetivo(Input.mousePosition);
                if(i>=0){Activar(botones[i]);return;}
            }
            for(int j=0;j<2;j++)
            {
                bool visible=j<cantidad;
                if(cursores[j].gameObject.activeSelf!=visible)cursores[j].gameObject.SetActive(visible);
                int objetivo=-1;
                if(visible)
                {
                    cursores[j].position=puntos[j];
                    if(bloquear[j] && (salida[j]-puntos[j]).sqrMagnitude>Mathf.Pow(UnityEngine.Screen.height*.055f,2))bloquear[j]=false;
                    if(!bloquear[j])objetivo=Objetivo(puntos[j]);
                }
                else bloquear[j]=false;
                if(seleccion[j].Paso(objetivo,visible?muestra:t,confirmar))
                {
                    for(int h=0;h<cantidad;h++){salida[h]=puntos[h];bloquear[h]=true;}
                    Activar(botones[objetivo]);return;
                }
                objetivos[j]=objetivo;avances[j]=seleccion[j].Progreso;
            }
            for(int i=0;i<botones.Length;i++)if(botones[i].pagina==Actual)
            {
                float valor=Mathf.Max(objetivos[0]==i?avances[0]:0,objetivos[1]==i?avances[1]:0);
                if(botones[i].progreso.fillAmount!=valor)botones[i].progreso.fillAmount=valor;
            }
        }
        private int Objetivo(Vector2 p)
        {
            for(int i=0;i<botones.Length;i++) if(botones[i].pagina==Actual && RectTransformUtility.RectangleContainsScreenPoint(botones[i].zona,p))return i;
            return -1;
        }
        public void Activar(Boton b)
        {
            var cap=juego.captura;
            switch(b.accion)
            {
                case Accion.Jugar:juego.NuevaPartida();return;
                case Accion.Configurar:Abrir(Pantalla.Configuracion);return;
                case Accion.Ayuda:Abrir(Pantalla.Instrucciones);return;
                case Accion.Volver:Abrir(Pantalla.Principal);return;
                case Accion.Guardar:cap.Aplicar(borrador);juego.AjustarControles();Abrir(Pantalla.Principal);return;
                case Accion.Salir:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying=false;
#else
                    Application.Quit();
#endif
                    return;
                case Accion.Perfil:borrador.perfil=(Perfil)(((int)borrador.perfil+b.paso+3)%3);borrador.umbrales=cap.Preset(borrador.perfil);break;
                case Accion.Resolucion:
                    int ancho=borrador.ancho==640?1280:640;
                    if(borrador.ancho==0 && !cap.PuedeProbar(640))ancho=1280;
                    if(cap.PuedeProbar(ancho))borrador.ancho=ancho;
                    break;
                case Accion.Frecuencia:borrador.fps=frecs[(Array.IndexOf(frecs,borrador.fps)+b.paso+4)%4];break;
                case Accion.Deteccion:borrador.umbrales.deteccion=Mathf.Clamp01(borrador.umbrales.deteccion+b.paso*.05f);break;
                case Accion.Presencia:borrador.umbrales.presencia=Mathf.Clamp01(borrador.umbrales.presencia+b.paso*.05f);break;
                case Accion.Seguimiento:borrador.umbrales.seguimiento=Mathf.Clamp01(borrador.umbrales.seguimiento+b.paso*.05f);break;
                case Accion.Sensibilidad:borrador.sensibilidad=Mathf.Clamp(borrador.sensibilidad+b.paso*.025f,-.15f,.15f);break;
                case Accion.Suavizado:borrador.respuesta=Mathf.Clamp(borrador.respuesta+b.paso*.25f,.5f,2);break;
                case Accion.Medir:borrador.diagnostico=!borrador.diagnostico;break;
            }
            ActualizarValores();
        }
        private void ActualizarValores()
        {
            valores[0].text=borrador.perfil.ToString();valores[1].text=borrador.ancho==640?"640 × 480":borrador.ancho==1280?"1280 × 720":"Automática";
            valores[2].text=borrador.fps+" FPS";valores[3].text=borrador.umbrales.deteccion.ToString("0.00");
            valores[4].text=borrador.umbrales.presencia.ToString("0.00");valores[5].text=borrador.umbrales.seguimiento.ToString("0.00");
            valores[6].text=borrador.sensibilidad.ToString("+0.000;-0.000;0.000");
            valores[7].text=borrador.respuesta.ToString("0.00")+"×";valores[8].text=borrador.diagnostico?"Sí":"No";
            nota.text=string.IsNullOrEmpty(juego.captura.AvisoResolucion)?"Al guardar se verifica el tamaño que entrega la webcam.":juego.captura.AvisoResolucion;
        }
        public void Datos(float t,Pista[] pistas)
        {
            var c=juego.captura;bool mostrar=c.opciones.diagnostico;
            if(diagnostico.gameObject.activeSelf!=mostrar)diagnostico.gameObject.SetActive(mostrar);
            if(!mostrar || t<proximoDato)return;
            proximoDato=t+.5f;
            string Edad(Pista p)=>p.ultima<0?"—":(t-p.ultima).ToString("0.0")+" s";
            diagnostico.text=$"Render {c.RenderHz:F0} FPS   ·   Webcam {c.CapturaHz:F0} FPS   ·   Inferencia {c.InferenciaHz:F1}/s   ·   {c.LatenciaMs:F0} ms\nÚltima mano    J1 {Edad(pistas[0])}     J2 {Edad(pistas[1])}    ·    {c.AnchoReal} × {c.AltoReal}";
        }
    }
}
