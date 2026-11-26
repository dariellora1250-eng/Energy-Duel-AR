using UnityEngine;

namespace EnergyDuel
{
    public sealed class DuelWeapon : MonoBehaviour
    {
        public float durabilidad=100;
        public bool Rota=>durabilidad<=0;
        public float Desgastar(float cantidad){if(Rota)return 0;float antes=durabilidad;durabilidad=Mathf.Max(0,durabilidad-cantidad);return antes-durabilidad;}
        public void Renovar(){durabilidad=100;PuntoDebil=Random.Range(0,6);if(marcaDebil)marcaDebil.localPosition=vertices[PuntoDebil]*.88f;}
        public int PuntoDebil{get;private set;}
        public Vector2 PosicionPuntoDebil=>escudo?(Vector2)escudo.transform.TransformPoint(vertices[PuntoDebil]):Vector2.zero;
        public float RadioPuntoDebil=.075f;
        private Transform marcaDebil;
        public void PulsarEscudo(float t,float grados,float segundos,int sentido)
        { finReboteEscudo=t+segundos;tiempoReboteEscudo=segundos;anguloEscudo=grados*sentido;finPulso=t+segundos;chispa.position=transform.position;Activo(chispa.gameObject,true);Activo(destello,true);Activo(onda.gameObject,true); }
        public GameObject espada,escudo,destello;
        public Transform inicioHoja,punta;
        public Material materialEstela;
        public readonly Vector2[] contorno=new Vector2[6],anteriorEscudo=new Vector2[6];
        private static readonly Vector3[] vertices=Hexagono();
        private static readonly int Opacidad=Shader.PropertyToID("_Opacity");
        private TrailRenderer estela;
        private Renderer[] superficies;
        private MaterialPropertyBlock propiedad;
        private Transform chispa,onda;
        private float finPulso,finRebote,duracion,angulo,ultimaAlfa=-1,finReboteEscudo,tiempoReboteEscudo,anguloEscudo;
        private float longitudVisual=1;
        private Vector2 retroceso;
        private bool pose,trazando;
        public Vector2 Base=>transform.TransformPoint(inicioHoja.localPosition);
        public Vector2 Punta=>transform.TransformPoint(punta.localPosition);
        public Hoja Natural=>new Hoja(Base,Punta);
        public float Radio=>Mathf.Max(.002f,transform.localScale.x*.12f);

        private void Awake()
        {
            estela=punta.gameObject.AddComponent<TrailRenderer>();estela.sharedMaterial=materialEstela;
            estela.time=.09f;estela.minVertexDistance=.01f;estela.startWidth=1;estela.endWidth=0;
            estela.emitting=false;estela.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            onda=Efecto("Onda",DuelShape.Forma.AroHexagonal,escudo.transform);
            var marca=new GameObject("Punto débil amarillo");marca.transform.SetParent(escudo.transform,false);marcaDebil=marca.transform;
            var formaDebil=marca.AddComponent<DuelShape>();formaDebil.forma=DuelShape.Forma.Marca;
            var rendererDebil=marca.GetComponent<MeshRenderer>();rendererDebil.sharedMaterial=destello.GetComponent<MeshRenderer>().sharedMaterial;rendererDebil.sortingOrder=9;
            marca.transform.localScale=Vector3.one*.08f;Renovar();
            chispa=Efecto("Chispa",DuelShape.Forma.Marca,transform);
            superficies=GetComponentsInChildren<Renderer>(true);
            propiedad=new MaterialPropertyBlock();
        }
        private Transform Efecto(string nombre,DuelShape.Forma forma,Transform padre)
        {
            var go=new GameObject(nombre);go.SetActive(false);go.transform.SetParent(padre,false);
            var shape=go.AddComponent<DuelShape>();shape.forma=forma;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=destello.GetComponent<MeshRenderer>().sharedMaterial;
            renderer.sortingOrder=8;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }
        public static Vector3[] Hexagono()
        {
            var v=new Vector3[6];
            for(int i=0;i<6;i++){float a=(i*60+30)*Mathf.Deg2Rad;v[i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*1.2f;}
            return v;
        }
        public void Ocultar()
        {
            Activo(espada,false);Activo(escudo,false);Activo(chispa.gameObject,false);
            Activo(destello,false);Activo(onda.gameObject,false);
            if(trazando){estela.emitting=false;estela.Clear();trazando=false;}
            pose=false;finPulso=finRebote=finReboteEscudo=0;longitudVisual=1;espada.transform.localScale=Vector3.one;
        }
        private static void Activo(GameObject go,bool valor){if(go.activeSelf!=valor)go.SetActive(valor);}
        public void Mostrar(Pista p,float t,DuelRules r)
        {
            float perdida=t-p.recibida;
            float alfa=p.valida?1:1-Mathf.InverseLerp(r.retencionVisual,r.retencionVisual+r.desvanecer,perdida);
            if(!p.asignada || alfa<=0 || p.arma==Arma.Ninguna){Ocultar();return;}
            Activo(espada,p.arma==Arma.Espada && !Rota);Activo(escudo,p.arma==Arma.Escudo && !Rota);
            if(Mathf.Abs(ultimaAlfa-alfa)>.015f || (ultimaAlfa!=alfa && alfa==1))
            {
                propiedad.SetFloat(Opacidad,alfa);
                for(int i=0;i<superficies.Length;i++)superficies[i].SetPropertyBlock(propiedad);
                ultimaAlfa=alfa;
            }
            if(p.valida)
            {
                // El objetivo cambia a la frecuencia del detector; interpolamos en cada render.
                float k=pose?1-Mathf.Exp(-Time.unscaledDeltaTime*(r.suavizado*1.5f+24*p.velocidad.magnitude)):1;
                transform.SetPositionAndRotation(Vector3.Lerp(transform.position,p.posicion,k),
                    Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(p.normalPalma,p.ejePalma),k));
                transform.localScale=Vector3.one*Mathf.Lerp(transform.localScale.x,p.escala,k);
                pose=true;
            }
            bool trazar=espada.activeSelf && p.valida && r.estela;
            if(trazar!=trazando){estela.Clear();estela.emitting=trazando=trazar;}
            estela.widthMultiplier=transform.localScale.x*.09f;
            longitudVisual=Mathf.MoveTowards(longitudVisual,1,Time.unscaledDeltaTime*9);
            espada.transform.localScale=new Vector3(1,longitudVisual,1);
            AplicarRebote(t);
            float fEscudo=finReboteEscudo>t?Mathf.Clamp01((finReboteEscudo-t)/tiempoReboteEscudo):0;
            escudo.transform.localRotation=Quaternion.AngleAxis(anguloEscudo*fEscudo*fEscudo,Vector3.forward);
            if(escudo.activeSelf)
                for(int i=0;i<6;i++){anteriorEscudo[i]=contorno[i];contorno[i]=escudo.transform.TransformPoint(vertices[i]);}
            bool flash=escudo.activeSelf && t<finPulso;
            Activo(destello,flash);Activo(onda.gameObject,flash);
            if(flash){float s=.25f+1.2f*(1-(finPulso-t)/.22f);onda.localScale=new Vector3(s,s,.03f);}
            Activo(chispa.gameObject,t<finRebote && espada.activeSelf);
        }
        public void Rebotar(float t,Vector2 punto,float grados,float segundos,int sentido,Vector2 desplazamiento)
        {
            finRebote=t+segundos;duracion=segundos;angulo=grados*sentido;
            retroceso=desplazamiento;
            chispa.position=new Vector3(punto.x,punto.y,-.04f);
            chispa.localScale=new Vector3(.34f,.34f,.08f);Activo(chispa.gameObject,true);
            AplicarRebote(t);
        }
        private void AplicarRebote(float t)
        {
            float f=finRebote>t ? Mathf.Clamp01((finRebote-t)/duracion):0;
            espada.transform.position=transform.position+(Vector3)(retroceso*(f*f));
            espada.transform.rotation=Quaternion.AngleAxis(angulo*f*f,Vector3.forward)*transform.rotation;
        }
        public void Limitar(Hoja barrera,float radio)
        {
            var visible=new Hoja(inicioHoja.position,punta.position);
            if(visible.Distancia(barrera,out var contacto)>radio)return;
            float largo=Vector2.Distance(espada.transform.position,punta.position);
            float corte=Mathf.Clamp01((Vector2.Distance(espada.transform.position,contacto)-radio)/Mathf.Max(.001f,largo));
            longitudVisual*=corte;
            espada.transform.localScale=new Vector3(1,longitudVisual,1);
        }
        public void Bloquear(float ahora){finPulso=ahora+.22f;Activo(destello,true);Activo(onda.gameObject,true);}
    }
}

