using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using static EnergyDuel.Accion;

namespace EnergyDuel.Editor
{
    internal static class DuelMenuBuilder
    {
        private static readonly Color Azul=new Color(.14f,.85f,1), Naranja=new Color(1,.42f,.18f);
        private static readonly Color Panel=new Color(.022f,.046f,.077f,.97f), BotonColor=new Color(.08f,.17f,.24f);
        private static RectTransform Rect(Transform padre,string nombre,float x,float y,float w,float h)
        {
            var go=new GameObject(nombre,typeof(RectTransform));var rt=(RectTransform)go.transform;
            rt.SetParent(padre,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);
            rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);return rt;
        }
        private static Image Fondo(RectTransform rt,Color color)
        {var img=rt.gameObject.AddComponent<Image>();img.color=color;img.raycastTarget=false;return img;}
        internal static DuelMenu Crear(DuelHud hud)
        {
            var menu=hud.GetComponent<DuelMenu>();
            if(menu)return menu;
            menu=hud.gameObject.AddComponent<DuelMenu>();
            var scaler=hud.GetComponent<CanvasScaler>();scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var font=hud.reloj.font;var botones=new List<DuelMenu.Boton>();
            menu.paginas=new GameObject[4];menu.valores=new Text[9];
            Text Texto(Transform padre,string nombre,string valor,float x,float y,float w,float h,int size,Color col,bool bold=false,TextAnchor alineacion=TextAnchor.MiddleCenter)
            {
                var rt=Rect(padre,nombre,x,y,w,h);var text=rt.gameObject.AddComponent<Text>();
                text.font=font;text.fontSize=size;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
                text.text=valor;text.alignment=alineacion;text.color=col;text.raycastTarget=false;return text;
            }
            RectTransform Pagina(Pantalla pagina,string titulo,float alto)
            {
                var rt=Rect(hud.transform,pagina.ToString(),0,-18,580,alto);Fondo(rt,Panel);
                Fondo(Rect(rt,"Borde ION",-145,alto/2-2,290,4),Azul);
                Fondo(Rect(rt,"Borde SOLAR",145,alto/2-2,290,4),Naranja);
                if(titulo!="")Texto(rt,"Título",titulo,0,alto/2-35,536,48,29,Color.white,true);
                menu.paginas[(int)pagina-1]=rt.gameObject;return rt;
            }
            void Boton(RectTransform panel,Pantalla pagina,string label,Accion accion,float x,float y,float width,int paso=0)
            {
                float height=paso==0?40:30;
                var rt=Rect(panel,label,x,y,width,height);Fondo(rt,BotonColor);
                Texto(rt,"Etiqueta",label,0,1,width-4,height,19,Color.white,true);
                var bar=Fondo(Rect(rt,"Selección",0,-height/2+2,width,4),paso<0?Naranja:Azul);
                bar.sprite=hud.progreso.sprite;bar.type=Image.Type.Filled;bar.fillMethod=Image.FillMethod.Horizontal;bar.fillAmount=0;
                botones.Add(new DuelMenu.Boton{zona=rt,pagina=pagina,accion=accion,paso=paso,progreso=bar});
            }
            var p=Pantalla.Principal;var main=Pagina(p,"ENERGY DUEL AR",348);
            Texto(main,"Control","Mantén la mano sobre una opción o haz clic.",0,83,530,36,18,Azul);
            Boton(main,p,"JUGAR",Jugar,0,31,400);Boton(main,p,"CONFIGURACIÓN",Configurar,0,-17,400);
            Boton(main,p,"INSTRUCCIONES",Ayuda,0,-65,400);Boton(main,p,"SALIR",Salir,0,-113,400);
            p=Pantalla.Configuracion;var config=Pagina(p,"CONFIGURACIÓN",480);
            string[] names={"Perfil","Webcam","Inferencias máximas","Detección","Presencia","Seguimiento","Sensibilidad de espada","Respuesta del suavizado","Diagnóstico"};
            for(int i=0;i<names.Length;i++)
            {
                int y=154-i*34;
                Texto(config,names[i],names[i],-135,y,235,30,18,Color.white,false,TextAnchor.MiddleLeft);
                menu.valores[i]=Texto(config,"Valor","",98,y,153,32,18,Azul,true);
                var accion=(Accion)((int)Accion.Perfil+i);
                Boton(config,p,"−",accion,-4,y,39,-1);Boton(config,p,"+",accion,206,y,39,1);
            }
            menu.nota=Texto(config,"Resolución","",0,-151,540,34,16,Color.white);
            Boton(config,p,"VOLVER",Volver,-121,-200,216);Boton(config,p,"GUARDAR",Guardar,121,-200,216);
            p=Pantalla.Instrucciones;var ayuda=Pagina(p,"INSTRUCCIONES",390);
            Texto(ayuda,"Cómo jugar","J1 · ION empieza a la izquierda y J2 · SOLAR a la derecha. Muestren una mano cada uno y manténganla quieta durante la calibración.\n\nFlexiona parcialmente los dedos para empuñar la espada. Mueve la hoja a través del núcleo rival y retírala para atacar de nuevo.\n\nAbre la palma para bloquear. Gana quien reduzca la vida rival a cero.",0,0,512,246,19,Color.white,false,TextAnchor.MiddleLeft);
            Boton(ayuda,p,"VOLVER",Volver,0,-152,310);
            p=Pantalla.Victoria;var final=Pagina(p,"",320);
            menu.ganador=Texto(final,"Ganador","",0,108,535,56,34,Azul,true);
            menu.resumen=Texto(final,"Marcador","",0,9,530,137,21,Color.white);
            Boton(final,p,"REVANCHA",Jugar,-123,-106,220);Boton(final,p,"MENÚ PRINCIPAL",Volver,123,-106,220);
            menu.botones=botones.ToArray();
            menu.cursores=new RectTransform[2];
            for(int j=0;j<2;j++)
            {
                var rt=Rect(hud.transform,"Cursor "+(j+1),0,0,16,16);Fondo(rt,j==0?Azul:Naranja);
                rt.localRotation=Quaternion.Euler(0,0,45);menu.cursores[j]=rt;rt.gameObject.SetActive(false);
            }
            menu.diagnostico=Texto(hud.transform,"Diagnóstico","",0,88,720,52,16,Color.white,true);
            menu.diagnostico.rectTransform.anchorMin=menu.diagnostico.rectTransform.anchorMax=new Vector2(.5f,0);
            menu.diagnostico.gameObject.AddComponent<Outline>().effectColor=Color.black;
            menu.diagnostico.gameObject.SetActive(false);
            menu.indicadoresJuego=new[]{hud.reloj.gameObject,hud.barras[0].transform.parent.parent.gameObject,hud.barras[1].transform.parent.parent.gameObject};
            foreach(var page in menu.paginas)page.SetActive(false);
            EditorUtility.SetDirty(menu);return menu;
        }
    }
}