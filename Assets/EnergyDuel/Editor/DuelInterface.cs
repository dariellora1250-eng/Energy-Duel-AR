using static EnergyDuel.Editor.DuelAssets;
using static EnergyDuel.Editor.DuelDocument;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EnergyDuel.Editor
{
    internal static class DuelInterface
    {
        internal static void AjustarAspecto(DuelHud hud)
        {
            // Un Image sin sprite dibuja un rectángulo; Filled requiere un sprite real para recortarlo.
            var blanco=SpriteBarra();
            var menu=hud.GetComponent<DuelMenu>();
            bool EsMenu(Transform t)=>menu && System.Array.Exists(menu.paginas,p=>p && t.IsChildOf(p.transform));
            foreach (var fondo in hud.GetComponentsInChildren<Image>(true))
            {
                if(EsMenu(fondo.transform))continue;
                bool barra=fondo.type==UnityEngine.UI.Image.Type.Filled;
                fondo.sprite=barra ? blanco : null;
                fondo.type=barra ? UnityEngine.UI.Image.Type.Filled : UnityEngine.UI.Image.Type.Simple;
                if (!barra && fondo.color.maxColorComponent<.15f)
                    fondo.color=new Color(.018f,.039f,.075f,1);
            }
            foreach (var texto in hud.GetComponentsInChildren<Text>(true))
            {
                if(EsMenu(texto.transform))continue;
                texto.fontSize=Mathf.Max(18,texto.fontSize);
                if (texto!=hud.creditos && texto.name!="Identificador") texto.color=Color.white;
            }
            if (hud.creditos) hud.creditos.rectTransform.sizeDelta=new Vector2(385,52);
            if (hud.aviso)
            {
                hud.aviso.rectTransform.sizeDelta=new Vector2(1230,58);
                ((RectTransform)hud.aviso.transform.parent).sizeDelta=new Vector2(0,64);
            }
            if (hud.resultado) hud.resultado.transform.parent.gameObject.SetActive(false);
            if (hud.zonaReinicio) hud.zonaReinicio.gameObject.SetActive(false);
        }

        private static Sprite SpriteBarra()
        {
            const string path=Output+"/Rectangulo.png";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite) return sprite;
            if (!File.Exists(path))
            {
                var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);
                try
                {
                    tex.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});
                    tex.Apply();
                    File.WriteAllBytes(path,tex.EncodeToPNG());
                }
                finally { Object.DestroyImmediate(tex); }
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new InvalidDataException("Sprite de barras ilegible en "+path);
        }
        private const string TextScript="{fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}";
        private const string ImageScript="{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}";
        private const string Claro="{r: 0.92, g: 0.97, b: 1, a: 1}";
        private const string Fondo="{r: 0.018, g: 0.039, b: 0.075, a: 1}";
        private const string Cian="{r: 0.14, g: 0.85, b: 1, a: 1}";
        private const string Solar="{r: 1, g: 0.42, b: 0.21, a: 1}";
        private static string Franja(float y,float height,float pivot) =>
            "  m_AnchorMin: "+V(0,y)+"\n  m_AnchorMax: "+V(1,y)+"\n  m_AnchoredPosition: "+V(0,0)+"\n  m_SizeDelta: "+V(0,height)+"\n  m_Pivot: "+V(.5f,pivot);

        internal static int Create(DuelDocument doc)
        {
            var root=doc.Object("Interfaz del duelo",null,Rect(0,0,1280,720));
            DuelLayout.Canvas(doc,root,0,10);
            doc.Behaviour(root,"{fileID: 11500000, guid: 0cd44c1031e13a943bb63640046fad76, type: 3}",
                "  m_UiScaleMode: 1\n  m_ReferencePixelsPerUnit: 100\n  m_ScaleFactor: 1\n  m_ReferenceResolution: {x: 1280, y: 720}\n  m_ScreenMatchMode: 0\n  m_MatchWidthOrHeight: 0");
            var top=doc.Object("Cabecera",root,Franja(1,112,1));
            Image(doc,top,Fondo);
            Label(doc,top,"Título","ENERGY DUEL AR",Rect(0,1,430,40,24,-4,0,1),30,3,Claro,true);
            int creditos=Label(doc,top,"Autor","",Rect(1,1,385,46,-24,-2,1,1),17,5,Claro);
            int reloj=Label(doc,top,"Tiempo","00 : 00",Rect(.5f,1,160,44,0,-4,.5f,1),26,4,Claro,true);
            int[] vida=new int[2],estados=new int[2],barras=new int[2];
            for(int j=0;j<2;j++)
            {
                bool izquierda=j==0;
                string color=izquierda?Cian:Solar;
                float lado=izquierda?0:1, dx=izquierda?24:-24;
                var panel=doc.Object("Jugador "+(j+1),top,Rect(lado,0,500,64,dx,3,lado,0));
                Label(doc,panel,"Identificador",izquierda?"J1   /   ION":"J2   /   SOLAR",Rect(0,1,220,25,0,0,0,1),20,3,color,true);
                vida[j]=Label(doc,panel,"Vida","100 / 100",Rect(1,1,150,25,0,0,1,1),18,5,Claro);
                var rail=doc.Object("Capacidad",panel,Rect(.5f,.5f,500,7));
                Image(doc,rail,"{r: 0.18, g: 0.25, b: 0.34, a: 1}");
                barras[j]=Image(doc,doc.Object("Salud",rail,Rect(.5f,.5f,500,7)),color,true);
                estados[j]=Label(doc,panel,"Seguimiento","CALIBRAR",Rect(0,0,500,22,0,0,0,0),15,3,Claro);
            }
            var pie=doc.Object("Instrucciones",root,Franja(0,54,0)); Image(doc,pie,Fondo);
            int aviso=Label(doc,pie,"Mensaje","Preparando la webcam…",Rect(.5f,.5f,1230,50),19,4,Claro);
            var cartel=doc.Object("Panel de resultado",root,Rect(.5f,.61f,570,74));
            Image(doc,cartel,Fondo);
            int resultado=Label(doc,cartel,"Resultado","",Rect(.5f,.5f,555,68),36,4,Claro,true);
            var boton=doc.Object("Reiniciar",root,Rect(.5f,.43f,300,68));
            Image(doc,boton,Fondo);
            int reiniciar=Label(doc,boton,"Acción","REINICIAR",Rect(.5f,.52f,290,50),25,4,Claro,true);
            int progreso=Image(doc,doc.Object("Tiempo mantenido",boton,Rect(.5f,0,300,6,0,0,.5f,0)),Cian,true);
            return doc.NativeScript(root,"DuelHud","  creditos: "+Ref(creditos)+"\n  reloj: "+Ref(reloj)+"\n  aviso: "+Ref(aviso)+
                "\n  resultado: "+Ref(resultado)+"\n  reiniciar: "+Ref(reiniciar)+"\n  zonaReinicio: "+Ref(boton.tr)+"\n  progreso: "+Ref(progreso)+"\n"+
                DuelLayout.Pair("estados",estados[0],estados[1])+DuelLayout.Pair("vidas",vida[0],vida[1])+DuelLayout.Pair("barras",barras[0],barras[1]));
        }
        private static int Image(DuelDocument doc,Node n,string color,bool filled=false)
        {
            doc.Component(n,222,"CanvasRenderer","  m_CullTransparentMesh: 1");
            return doc.Behaviour(n,ImageScript,"  m_Color: "+color+"\n  m_RaycastTarget: 0\n  m_Sprite: {fileID: 0}\n  m_Type: "+
                (filled?3:0)+"\n  m_FillCenter: 1\n  m_FillMethod: 0\n  m_FillOrigin: 0\n  m_FillAmount: 1\n  m_PixelsPerUnitMultiplier: 1");
        }
        private static int Label(DuelDocument doc,Node parent,string name,string text,string rect,int size,int align,string color,bool bold=false)
        {
            var n=doc.Object(name,parent,rect);
            doc.Component(n,222,"CanvasRenderer","  m_CullTransparentMesh: 1");
            return doc.Behaviour(n,TextScript,"  m_Color: "+color+"\n  m_RaycastTarget: 0\n  m_FontData:\n    m_Font: {fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}\n    m_FontSize: "+size+
                "\n    m_FontStyle: "+(bold?1:0)+"\n    m_Alignment: "+align+"\n    m_RichText: 0\n    m_HorizontalOverflow: 0\n    m_VerticalOverflow: 0\n    m_LineSpacing: 1\n  m_Text: \""+text+"\"");
        }
    }
}
