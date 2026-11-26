using System.IO;
using System.Text.RegularExpressions;
using static EnergyDuel.Editor.DuelAssets;
using static EnergyDuel.Editor.DuelDocument;

namespace EnergyDuel.Editor
{
    internal static class DuelLayout
    {
        private const string RawImage="{fileID: 11500000, guid: 1344c3c82d62a2a41a3576d8abb8e3ea, type: 3}";
        internal const string RulesPath=Output+"/Reglas.asset";
        private static string Field(string name,int id) => "  "+name+": "+Ref(id)+"\n";
        internal static string Pair(string name,int a,int b) => "  "+name+":\n  - "+Ref(a)+"\n  - "+Ref(b)+"\n";

        internal static void Create()
        {
            Asset(RulesPath,Header+"--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_Name: Reglas\n  m_Script: "+Script("DuelRules")+
                "\n  nombre: \n  matricula: \n  espadaHasta: 0.68\n  escudoDesde: 0.82\n  confirmarGesto: 0.11\n  vigencia: 0.22\n  retencionVisual: 0.35\n  olvido: 1.4\n  calibracion: 0.45\n  suavizado: 20\n  zonaJ1: {x: 0.3, y: 0.48}\n  zonaJ2: {x: 0.7, y: 0.48}\n  radioZona: 0.066\n  vidaInicial: 100\n  dano: 10\n  intervalo: 0.48\n  velocidadMinima: 0.24\n  recorridoMinimo: 0.022\n  reinicio: 1.2\n  estela: 1\n  penalizacionEspada: 3\n  penalizacionEscudo: 2\n  desgasteChoqueEspadas: 10\n  desgastePresionSegundo: 20\n  demoraPresion: 0.35\n  desgasteEspadaEscudo: 20\n  desgasteEscudoBloqueando: 8\n  desgasteChoqueEscudos: 5\n  giroChoqueEspadas: 30\n  giroChoqueEscudos: 75\n  tiempoChoqueEscudos: 0.42\n","NativeFormatImporter",11400000);

            var doc=new DuelDocument();
            const string sampleScene=Sample+"/Scenes/Hand Landmark Detection/Hand Landmark Detection.unity";
            string original=File.ReadAllText(sampleScene).Replace("\r","");
            // Reutiliza la cámara y ajustes de la escena original; el ejemplo permanece intacto.
            foreach (Match m in Regex.Matches(original,@"(?ms)^--- !u!(?:29|104|157|196) &\d+\n.*?(?=^---|\z)")) doc.Raw(m.Value);
            foreach (Match m in Regex.Matches(original,@"(?ms)^--- !u!\d+ &196938881[4-7]\n.*?(?=^---|\z)"))
                doc.Raw(m.Value.Replace("orthographic: 0","orthographic: 1").Replace("orthographic size: 5","orthographic size: 0.5")
                    .Replace("m_ClearFlags: 1","m_ClearFlags: 2").Replace("x: 0, y: 1, z: -10","x: 0, y: 0, z: -10")
                    .Replace("m_HDR: 1","m_HDR: 0").Replace("m_AllowMSAA: 1","m_AllowMSAA: 0")
                    .Replace("{r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}","{r: 0.025, g: 0.042, b: 0.075, a: 1}"));
            var fondo=doc.Object("Fondo de webcam",null,Rect(.5f,.5f,1,1));
            fondo.pos=V(0,0,3);
            Canvas(doc,fondo,2,-100);
            var imagen=doc.Object("Imagen de la webcam",fondo,Rect(.5f,.5f,640,360));
            imagen.scale=V(.002778f,.002778f,.002778f);
            doc.Component(imagen,222,"CanvasRenderer","  m_CullTransparentMesh: 1");
            int raw=doc.Behaviour(imagen,RawImage,"  m_Color: {r: 1, g: 1, b: 1, a: 1}\n  m_RaycastTarget: 0\n  m_UVRect:\n    serializedVersion: 2\n    x: 0\n    y: 0\n    width: 1\n    height: 1");
            int pantalla=doc.Behaviour(imagen,Ext(Sample+"/Common/Scripts/Screen.cs",11500000,3),Field("_screen",raw));
            var juego=doc.Object("Energy Duel AR");
            int captura=doc.NativeScript(juego,"DuelCapture","  _bootstrapPrefab: {fileID: 968918444066301895, guid: "+GuidFor(Output+"/Bootstrap.prefab")+
                ", type: 3}\n"+Field("screen",pantalla)+Field("camara",1969388816)+
                "  inference: 0\n  lectura: 1\n  modo: 2\n  manos: 2\n  deteccion: 0.01\n  presencia: 0.1\n  seguimiento: 0.9");
            int[] armas=new int[2],zonas=new int[2];
            for (int j=0;j<2;j++)
            {
                string equipo=j==0?"Ion":"Solar";
                string ruta=Output+"/Arsenal J"+(j+1)+".prefab";
                int controller=Weapon(ruta,j,equipo);
                armas[j]=doc.Prefab(ruta,controller,"DuelWeapon");
                var zona=doc.Object("Núcleo vulnerable J"+(j+1));
                zona.pos=V(j==0?-.356f:.356f,-.02f,.4f);
                zona.scale=V(.066f,.066f,.066f);
                zonas[j]=zona.tr;
                Shape(doc,zona,"Perímetro",4,equipo,0,0,0,1,1,.06f,-3);
                Shape(doc,zona,"Núcleo",2,j==0?"Plasma azul":"Plasma rojo",0,0,0,.90f,.90f,.02f,-4);
                Shape(doc,zona,"Centro",5,"Nucleo",0,0,-.02f,.10f,.10f,.06f,-2);
            }
            int hud=DuelInterface.Create(doc);
            doc.NativeScript(juego,"DuelMatch",Field("captura",captura)+"  reglas: "+Ext(RulesPath,11400000)+"\n"+Field("hud",hud)+Pair("armas",armas[0],armas[1])+Pair("zonas",zonas[0],zonas[1]));
            Asset(Scene,doc.Yaml(),"DefaultImporter");
        }

        internal static void Canvas(DuelDocument doc,Node n,int modo,int orden)
        {
            doc.Component(n,223,"Canvas","  m_Enabled: 1\n  serializedVersion: 3\n  m_RenderMode: "+modo+
                "\n  m_Camera: "+(modo==2?Ref(1969388816):Ref(0))+"\n  m_PlaneDistance: 100\n  m_ReceivesEvents: 0\n  m_OverrideSorting: 1\n  m_SortingOrder: "+orden+"\n  m_TargetDisplay: 0");
        }

        private static int Weapon(string ruta,int j,string color)
        {
            var doc=new DuelDocument();
            var root=doc.Object("Arsenal J"+(j+1));
            var espada=doc.Object("Espada",root);
            espada.active=false;
            Shape(doc,espada,"Empuñadura",1,"Titanio",0,-.03f,0,.13f,.48f,.23f,1);
            Shape(doc,espada,"Pomo",1,color,0,-.47f,0,.16f,.09f,.24f,2);
            Shape(doc,espada,"Guarda",1,color,0,.38f,0,.46f,.12f,.24f,2);
            Shape(doc,espada,"Hoja de energía",0,color,0,.43f,0,.35f,3.38f,.46f,2);
            Shape(doc,espada,"Filamento",0,"Nucleo",0,.46f,.08f,.135f,3.14f,.24f,3);
            var inicio=doc.Object("Base de la hoja",espada); inicio.pos=V(0,.43f,0);
            var punta=doc.Object("Punta",espada); punta.pos=V(0,3.81f,0);
            var escudo=doc.Object("Escudo",root); escudo.pos=V(0,0,.18f); escudo.active=false;
            string plasma=j==0?"Plasma azul":"Plasma rojo";
            Shape(doc,escudo,"Membrana",2,plasma,0,0,0,1.2f,1.2f,.06f,1);
            Shape(doc,escudo,"Borde",3,color,0,0,.035f,1.2f,1.2f,.07f,2);
            Shape(doc,escudo,"Circuito interno",3,color,0,0,.045f,.83f,.83f,.025f,2);
            Shape(doc,escudo,"Emisor",3,"Nucleo",0,0,.05f,.27f,.27f,.04f,2);
            var luz=Shape(doc,escudo,"Pulso de bloqueo",2,"Pulso",0,0,.07f,1.16f,1.16f,.02f,4);
            luz.active=false;
            int controlador=doc.NativeScript(root,"DuelWeapon",Field("espada",espada.id)+Field("escudo",escudo.id)+Field("destello",luz.id)+
                Field("inicioHoja",inicio.tr)+Field("punta",punta.tr)+"  materialEstela: "+Material(plasma));
            Asset(ruta,doc.Yaml(),"PrefabImporter");
            return controlador;
        }

        private static Node Shape(DuelDocument doc,Node parent,string name,int shape,string mat,float x,float y,float z,float sx,float sy,float sz,int order)
        {
            var n=doc.Object(name,parent);
            n.pos=V(x,y,z); n.scale=V(sx,sy,sz);
            doc.Component(n,33,"MeshFilter","  m_Mesh: {fileID: 0}");
            doc.Component(n,23,"MeshRenderer","  m_Enabled: 1\n  m_CastShadows: 0\n  m_ReceiveShadows: 0\n  m_LightProbeUsage: 0\n  m_ReflectionProbeUsage: 0\n  m_Materials:\n  - "+Material(mat)+"\n  m_SortingOrder: "+order);
            doc.NativeScript(n,"DuelShape","  forma: "+shape);
            return n;
        }
    }
}
