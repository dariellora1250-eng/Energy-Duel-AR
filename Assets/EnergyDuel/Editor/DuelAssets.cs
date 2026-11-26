using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EnergyDuel.Editor
{
    // Receta de recursos de texto de Unity 2022. Los GUID y fileID estables permiten
    // construir desde cero, conservar enlaces y comprobar referencias sin abrir la webcam.
    internal static class DuelAssets
    {
        internal const string Root="Assets/EnergyDuel";
        internal const string Output=Root+"/Generado";
        internal const string Scene=Output+"/Energy Duel AR.unity";
        internal const string Sample="Assets/Samples/MediaPipe Unity Plugin/0.16.3/Official Solutions";
        internal const string Model="hand_landmarker.bytes";
        internal const string Header="%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n";
        internal static string Num(float f) => f.ToString("0.######",CultureInfo.InvariantCulture);
        internal static string V(float x,float y) => "{x: "+Num(x)+", y: "+Num(y)+"}";
        internal static string V(float x,float y,float z) => "{x: "+Num(x)+", y: "+Num(y)+", z: "+Num(z)+"}";
        internal static string Ref(int id) => "{fileID: "+id+"}";
        internal static string Ext(string path,int id,int type=2) => "{fileID: "+id+", guid: "+GuidFor(path)+", type: "+type+"}";
        internal static string GuidFor(string path)
        {
            string meta=path+".meta";
            if (File.Exists(meta)) return Regex.Match(File.ReadAllText(meta),@"(?m)^guid: ([a-f0-9]+)").Groups[1].Value;
            using var md5=MD5.Create();
            return string.Concat(md5.ComputeHash(Encoding.UTF8.GetBytes("EnergyDuelAR/"+path)).Select(b=>b.ToString("x2")));
        }
        internal static void Meta(string path,string importer,string content="")
        {
            if (!File.Exists(path+".meta"))
                File.WriteAllText(path+".meta","fileFormatVersion: 2\nguid: "+GuidFor(path)+"\n"+importer+":\n  externalObjects: {}\n"+content+"  userData: \n  assetBundleName: \n  assetBundleVariant: \n");
        }
        internal static void Asset(string path,string content,string importer,int main=0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!File.Exists(path)) File.WriteAllText(path,content);
            Meta(path,importer,main==0?"":"  mainObjectFileID: "+main+"\n");
        }
        internal static string Script(string name) => Ext(Root+"/Scripts/"+name+".cs",11500000,3);
        internal static string Material(string name) => Ext(Output+"/"+name+".mat",2100000);
        public static void Create(string package)
        {
            Directory.CreateDirectory(Output);
            Meta(Root+"/Energy.shader","ShaderImporter","  defaultTextures: []\n  nonModifiableTextures: []\n");
            foreach (string path in Directory.GetFiles(Root+"/Scripts","*.cs")) Meta(path.Replace('\\','/'),"MonoImporter",
                "  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n");
            string settings=Output+"/Webcam.asset", bootstrap=Output+"/Bootstrap.prefab";
            var source=File.ReadAllText(Sample+"/Scenes/AppSettings.asset").Replace("\r","");
            source=source.Replace("m_Name: AppSettings","m_Name: Webcam");
            source=Regex.Replace(source,@"_assetLoaderType: \d+","_assetLoaderType: 0");
            source=Regex.Replace(source,@"_preferableInferenceMode: \d+","_preferableInferenceMode: 1");
            source=Regex.Replace(source,@"_preferredDefaultWebCamWidth: \d+","_preferredDefaultWebCamWidth: 640");
            Asset(settings,source,"NativeFormatImporter",11400000);
            // Migra la copia anterior conservando los ajustes editados y su GUID.
            var existing=File.ReadAllText(settings);
            var named=Regex.Replace(existing,@"(?m)^  m_Name: AppSettings(?=\r?$)","  m_Name: Webcam");
            if (existing!=named) File.WriteAllText(settings,named);
            var prefab=File.ReadAllText(Sample+"/Resources/Bootstrap.prefab");
            Asset(bootstrap,prefab.Replace(GuidFor(Sample+"/Scenes/AppSettings.asset"),GuidFor(settings)),"PrefabImporter");
            string streaming="Assets/StreamingAssets";
            Directory.CreateDirectory(streaming);
            var originalModel=Path.Combine(package,"PackageResources/MediaPipe",Model);
            var localModel=Path.Combine(streaming,Model);
            if (!File.Exists(originalModel) || new FileInfo(originalModel).Length==0)
                throw new FileNotFoundException("Falta el modelo Hand Landmarker del paquete instalado.",originalModel);
            using (var sha=SHA256.Create())
            {
                byte[] Hash(string path) { using var stream=File.OpenRead(path); return sha.ComputeHash(stream); }
                if (!File.Exists(localModel) || !Hash(originalModel).SequenceEqual(Hash(localModel)))
                    File.Copy(originalModel,localModel,true);
            }
            foreach (var notice in new[]{"LICENSE.md","Third Party Notices.md"})
            {
                var dest=streaming+"/MediaPipe-"+Path.GetFileNameWithoutExtension(notice)+".txt";
                if (!File.Exists(dest)) File.Copy(Path.Combine(package,notice),dest);
            }
            CreateMaterial("Ion",.04f,.83f,1,1);
            CreateMaterial("Solar",1,.28f,.095f,1);
            CreateMaterial("Plasma azul",.12f,.78f,1,.26f);
            CreateMaterial("Plasma rojo",1,.26f,.1f,.26f);
            CreateMaterial("Titanio",.14f,.19f,.25f,1);
            CreateMaterial("Nucleo",.91f,.99f,1,1);
            CreateMaterial("Pulso",.85f,.98f,1,.62f);
            DuelLayout.Create();
            MigrarInstanciasAntiguas();
        }

        private static void MigrarInstanciasAntiguas()
        {
            // Unity deriva IDs de objetos instanciados combinando el ID del prefab
            // con el de su instancia. Las dos instancias antiguas usaban IDs de sus propios hijos.
            var original=File.ReadAllText(Scene);
            var scene=original;
            foreach (var id in new[]{112,131})
            {
                string cabecera="--- !u!1001 &"+id;
                var bloque=Regex.Match(scene,"(?ms)^"+cabecera+@"\r?\nPrefabInstance:\r?\n.*?(?=^---|\z)");
                string arma=Output+"/Arsenal J"+(id==112 ? 1 : 2)+".prefab";
                if (!bloque.Success || !bloque.Value.Contains("guid: "+GuidFor(arma))) continue;
                string nuevo=(id*65536).ToString(CultureInfo.InvariantCulture);
                scene=Regex.Replace(scene,"(?m)^"+cabecera+@"(?=\r?$)","--- !u!1001 &"+nuevo);
                scene=scene.Replace("{fileID: "+id+"}","{fileID: "+nuevo+"}");
            }
            if (scene!=original) File.WriteAllText(Scene,scene);
        }
        private static void CreateMaterial(string name,float r,float g,float b,float a)
        {
            Asset(Output+"/"+name+".mat",Header+"--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_Name: "+name+
                "\n  m_Shader: "+Ext(Root+"/Energy.shader",4800000,3)+"\n  m_CustomRenderQueue: -1\n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs: []\n    m_Ints: []\n    m_Floats: []\n    m_Colors:\n    - _Color: {r: "+Num(r)+", g: "+Num(g)+", b: "+Num(b)+", a: "+Num(a)+"}\n","NativeFormatImporter",2100000);
        }
    }

    // Representación mínima de los documentos YAML que emplean escena y prefabs.
    internal sealed class DuelDocument
    {
        internal sealed class Node
        {
            internal int id,tr,parent;
            internal string name,layout;
            internal string pos=DuelAssets.V(0,0,0), scale=DuelAssets.V(1,1,1);
            internal bool active=true;
            internal readonly List<int> children=new List<int>(), components=new List<int>();
        }
        private readonly List<Node> nodes=new List<Node>();
        private readonly StringBuilder bodies=new StringBuilder();
        private int last=100;
        internal Node Object(string name,Node parent=null,string layout=null)
        {
            var n=new Node {id=++last,tr=++last,name=name,parent=parent?.tr??0,layout=layout};
            parent?.children.Add(n.tr);
            nodes.Add(n);
            return n;
        }
        internal int Component(Node n,int type,string title,string fields)
        {
            int id=++last;
            n.components.Add(id);
            bodies.Append("--- !u!"+type+" &"+id+"\n"+title+":\n  m_ObjectHideFlags: 0\n  m_GameObject: "+DuelAssets.Ref(n.id)+"\n"+fields.TrimEnd('\r','\n')+"\n");
            return id;
        }
        internal int Behaviour(Node n,string script,string fields) =>
            Component(n,114,"MonoBehaviour","  m_Enabled: 1\n  m_Script: "+script+"\n  m_Name: \n  m_EditorClassIdentifier: \n"+fields);
        internal int NativeScript(Node n,string name,string fields) => Behaviour(n,DuelAssets.Script(name),fields);
        internal static string Rect(float x,float y,float w,float h,float dx=0,float dy=0,float pivotX=.5f,float pivotY=.5f)
        {
            var anchor=DuelAssets.V(x,y);
            return "  m_AnchorMin: "+anchor+"\n  m_AnchorMax: "+anchor+"\n  m_AnchoredPosition: "+DuelAssets.V(dx,dy)+
                "\n  m_SizeDelta: "+DuelAssets.V(w,h)+"\n  m_Pivot: "+DuelAssets.V(pivotX,pivotY);
        }
        internal void Raw(string yaml) => bodies.Append(yaml.TrimEnd()+"\n");
        internal int Prefab(string path,int sourceId,string script)
        {
            int instance=(++last)*65536, component=++last;
            bodies.Append("--- !u!1001 &"+instance+"\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {fileID: 0}\n    m_Modifications: []\n    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n  m_SourcePrefab: "+DuelAssets.Ext(path,100100000,3)+"\n");
            bodies.Append("--- !u!114 &"+component+" stripped\nMonoBehaviour:\n  m_CorrespondingSourceObject: "+DuelAssets.Ext(path,sourceId,3)+"\n  m_PrefabInstance: "+DuelAssets.Ref(instance)+"\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_Script: "+DuelAssets.Script(script)+"\n  m_Name: \n  m_EditorClassIdentifier: \n");
            return component;
        }
        internal string Yaml()
        {
            var s=new StringBuilder(DuelAssets.Header);
            foreach (var n in nodes)
            {
                s.Append("--- !u!1 &"+n.id+"\nGameObject:\n  m_ObjectHideFlags: 0\n  serializedVersion: 6\n  m_Component:\n  - component: "+DuelAssets.Ref(n.tr)+"\n");
                foreach (var id in n.components) s.Append("  - component: "+DuelAssets.Ref(id)+"\n");
                s.Append("  m_Layer: "+(n.layout==null?0:5)+"\n  m_Name: "+n.name+"\n  m_TagString: Untagged\n  m_IsActive: "+(n.active?1:0)+"\n");
                string transform=n.layout==null?"4 &"+n.tr+"\nTransform:":"224 &"+n.tr+"\nRectTransform:";
                s.Append("--- !u!"+transform+"\n  m_ObjectHideFlags: 0\n  m_GameObject: "+DuelAssets.Ref(n.id)+"\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: "+n.pos+"\n  m_LocalScale: "+n.scale+"\n  m_Children:");
                if (n.children.Count==0) s.Append(" []\n");
                else { s.Append('\n'); foreach(var c in n.children) s.Append("  - "+DuelAssets.Ref(c)+"\n"); }
                s.Append("  m_Father: "+DuelAssets.Ref(n.parent)+"\n");
                if (n.layout!=null) s.Append(n.layout+"\n");
            }
            return s.Append(bodies).ToString();
        }
    }
}
