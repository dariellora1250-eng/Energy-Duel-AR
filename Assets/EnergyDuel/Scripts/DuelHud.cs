using UnityEngine;
using UnityEngine.UI;

namespace EnergyDuel
{
    public sealed class DuelHud : MonoBehaviour
    {
        public Text creditos, reloj, aviso, resultado, reiniciar;
        public RectTransform zonaReinicio;
        public Image progreso;
        public Text[] estados = new Text[2], vidas = new Text[2];
        public Image[] barras = new Image[2];
        private readonly int[] ultimaVida = {-1,-1};
        private int segundo = -1;

        public void Iniciar(DuelRules reglas)
        {
            creditos.text = reglas.nombre + (string.IsNullOrEmpty(reglas.matricula) ? "" : "\n"+reglas.matricula);
            zonaReinicio.gameObject.SetActive(false);
        }

        public sealed class EstadoVisible
        {
            private string candidata,estable="LISTO",evento;
            private float desde,ultimoCambio,fin;
            public string Paso(string objetivo,float t)
            {
                if(objetivo!=candidata){candidata=objetivo;desde=t;}
                if(t-desde>=.18f && t-ultimoCambio>=.32f && estable!=candidata){estable=candidata;ultimoCambio=t;}
                return t<fin?evento:estable;
            }
            public void Avisar(string aviso,float t){evento=aviso;fin=t+.8f;}
        }
        private readonly EstadoVisible[] visibles={new EstadoVisible(),new EstadoVisible()};
        private readonly float[] proxima={0,0};
        public void Reiniciar()
        {
            for(int i=0;i<2;i++){visibles[i]=new EstadoVisible();proxima[i]=0;ultimaVida[i]=-1;}
            segundo=-1;
        }
        public void Evento(int j,string aviso,float t){visibles[j].Avisar(aviso,t);proxima[j]=0;}
        public void Jugador(int j,int vida,int maximo,Pista pista,float t)
        {
            if(ultimaVida[j]!=vida)
            {
                ultimaVida[j]=vida;vidas[j].text=vida+" / "+maximo;barras[j].fillAmount=(float)vida/maximo;
            }
            if(t<proxima[j])return;
            proxima[j]=t+.10f;
            string estado=!pista.valida ? (pista.asignada?"RECUPERANDO":"BUSCANDO MANO") :
                !pista.asignada || pista.arma==Arma.Ninguna?"LISTO":pista.arma==Arma.Espada?"ATAQUE":"DEFENSA";
            string texto=visibles[j].Paso(estado,t);
            if(estados[j].text!=texto) estados[j].text=texto;
        }

        public void Tiempo(float t)
        {
            int s=Mathf.FloorToInt(t);
            if (s==segundo) return;
            segundo=s;
            reloj.text=(s/60).ToString("00")+" : "+(s%60).ToString("00");
        }

        public bool SobreReinicio(Camera camara,Vector2 mano) =>
            RectTransformUtility.RectangleContainsScreenPoint(zonaReinicio,camara.WorldToScreenPoint(mano),null);

        public void Mensaje(string texto)
        {
            if (aviso.text!=texto) aviso.text=texto;
        }

        private void LateUpdate()
        {
            var panel=resultado.transform.parent.gameObject;
            bool visible=resultado.text.Length!=0;
            if (panel.activeSelf!=visible) panel.SetActive(visible);
        }
    }
}
