using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EnergyDuel.Editor
{
    public class DuelStageTests
    {
        [Test] public void RelojContinuaConUnaManoYReanudaSinCuenta()
        {
            var r=new TiempoDuelo();r.Avanzar(10,true);r.Avanzar(.35f,false);r.Avanzar(2,true);
            Assert.That(r.Duracion,Is.EqualTo(12.35f).Within(.001f));
            r.Avanzar(2.5f,false);Assert.IsTrue(r.Suspendido);Assert.That(r.Duracion,Is.EqualTo(14.35f).Within(.001f));
            r.Avanzar(.1f,true);Assert.IsFalse(r.Suspendido);Assert.That(r.Duracion,Is.EqualTo(14.45f).Within(.001f));
        }
        [Test] public void ManoSinMuestraNoAtacaNiBloquea()
        {
            foreach(var arma in new[]{Arma.Espada,Arma.Escudo})
            {
                var p=new Pista{valida=true,asignada=true,arma=arma};
                Assert.AreEqual(arma,DuelMatch.ModoValido(p));p.Perder();
                Assert.AreEqual(Arma.Ninguna,DuelMatch.ModoValido(p));
                p.valida=true;p.reentrada=true;Assert.AreEqual(Arma.Ninguna,DuelMatch.ModoValido(p));
            }
        }
        [Test] public void RecuperacionPorLateralidadEnElLadoOpuesto()
        {
            var r=ScriptableObject.CreateInstance<DuelRules>();
            try
            {
                var tracker=new HandTracking();var f=new DuelCapture.Frame{cantidad=2};
                void Muestra(float t,float x,float y,bool invertida)
                {
                    f.tiempo=t;
                    for(int j=0;j<2;j++)
                    {
                        var m=f.manos[invertida?1-j:j];m.lateralidad=j==0?.95f:-.95f;
                        for(int n=0;n<21;n++)m.imagen[n]=m.geometria[n]=new Vector3(j==0?x:y,n==0?0:.08f,n*.001f);
                    }
                    tracker.Aplicar(f,1.5f,r);
                }
                for(int k=0;k<12;k++)Muestra(k*.06f,-.35f,.35f,k%2==0);
                Assert.IsTrue(tracker.jugadores.All(p=>p.asignada));
                for(int k=1;k<=14;k++)Muestra(.66f+k*.06f,-.35f+k*.05f,.35f-k*.05f,k%2==0);
                Assert.Greater(tracker.jugadores[0].centro.x,0);
                f.cantidad=0;f.tiempo=1.55f;tracker.Aplicar(f,1.5f,r);
                f.cantidad=2;Muestra(3.5f,.42f,-.30f,true);
                Assert.IsTrue(tracker.jugadores.All(p=>p.asignada && p.valida));
                Assert.Greater(tracker.jugadores[0].centro.x,0);
            }
            finally{Object.DestroyImmediate(r);}
        }
        [Test] public void BarridoDobleDetectaCruceRapidoYDescartaDistantes()
        {
            Hoja V(float x)=>new Hoja(new Vector2(x,-.3f),new Vector2(x,.3f));
            Assert.IsTrue(Hoja.Cruzan(V(-.25f),V(.25f),V(.25f),V(-.25f),.01f,out _));
            Assert.IsFalse(Hoja.Cruzan(V(-.25f),V(-.2f),V(.25f),V(.21f),.01f,out _));
        }
        [Test] public void IndicadorIgnoraCambiosBrevesYRetieneEventos()
        {
            var e=new DuelHud.EstadoVisible();e.Paso("ATAQUE",0);
            Assert.AreEqual("ATAQUE",e.Paso("ATAQUE",.4f));e.Paso("RECUPERANDO",.5f);
            Assert.AreEqual("ATAQUE",e.Paso("ATAQUE",.55f));
            e.Avisar("BLOQUEO",1);
            Assert.AreEqual("BLOQUEO",e.Paso("DEFENSA",1.7f));
            Assert.AreEqual("DEFENSA",e.Paso("DEFENSA",1.95f));
        }
        [Test] public void PermanenciaExigeContinuidadYSoloActivaUnaVez()
        {
            var p=new Permanencia();
            Assert.IsFalse(p.Paso(1,0,.5f));Assert.IsFalse(p.Paso(2,.4f,.5f));
            Assert.IsTrue(p.Paso(2,.95f,.5f));Assert.IsFalse(p.Paso(2,2,.5f));
            p.Paso(-1,2.1f,.5f);Assert.IsFalse(p.Paso(2,2.2f,.5f));
        }

        private static void Metodo(object o,string nombre,params object[] args)=>
            o.GetType().GetMethod(nombre,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
        private static T Campo<T>(object o,string nombre)=>(T)o.GetType().GetField(nombre,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);

        [Test] public void EscenaBloqueaSobreNucleoRebotaYReinicia()
        {
            string ruta=AssetDatabase.GenerateUniqueAssetPath("Assets/EnergyDuel/Editor/PruebaTemporal.unity");
            Assert.IsTrue(AssetDatabase.CopyAsset(DuelAssets.Scene,ruta));
            var scene=EditorSceneManager.OpenScene(ruta,OpenSceneMode.Additive);
            try
            {
                var m=DuelSceneCheck.Validar(scene);
                foreach(var a in m.armas)Metodo(a,"Awake");
                m.menu.Iniciar(m);
                m.menu.Activar(m.menu.botones.First(b=>b.accion==Accion.Ayuda));
                Assert.AreEqual(Pantalla.Instrucciones,m.menu.Actual);
                m.menu.Activar(m.menu.botones.First(b=>b.accion==Accion.Volver));
                Assert.AreEqual(Pantalla.Principal,m.menu.Actual);
                m.menu.Activar(m.menu.botones.First(b=>b.accion==Accion.Jugar));
                var modos=Campo<Arma[]>(m,"actual");var hp=Campo<int[]>(m,"vidas");
                var stats=Campo<int[]>(m,"bloqueos");
                modos[0]=Arma.Espada;modos[1]=Arma.Escudo;
                var espada=m.armas[0];var escudo=m.armas[1];
                espada.espada.SetActive(true);escudo.escudo.SetActive(true);
                espada.transform.localScale=Vector3.one*.1f;
                m.zonas[1].position=new Vector3(0,.2f,0);
                for(int i=0;i<6;i++)escudo.contorno[i]=(Vector2)DuelWeapon.Hexagono()[i]*.10f+new Vector2(0,.2f);
                espada.transform.position=Vector3.left*.3f;Metodo(m,"Resolver",1f,.05f);
                espada.transform.position=Vector3.zero;Metodo(m,"Resolver",1.05f,.05f);
                Assert.AreEqual(m.reglas.vidaInicial,hp[1]);Assert.AreEqual(1,stats[1]);
                Assert.That(Quaternion.Angle(espada.transform.rotation,espada.espada.transform.rotation),Is.EqualTo(m.reglas.giroBloqueo).Within(.1f));
                Metodo(m,"Resolver",1.10f,.05f);Assert.AreEqual(1,stats[1]);
                m.NuevaPartida();modos[0]=modos[1]=Arma.Espada;
                m.armas[1].transform.localScale=Vector3.one*.1f;m.armas[1].transform.position=Vector3.zero;
                Metodo(m,"Resolver",2f,.05f);
                foreach(var a in m.armas)Assert.That(Quaternion.Angle(a.transform.rotation,a.espada.transform.rotation),Is.EqualTo(m.reglas.giroDuelo).Within(.1f));
                Assert.AreEqual(m.reglas.vidaInicial,hp[0]);Assert.AreEqual(m.reglas.vidaInicial,hp[1]);
                hp[0]=0;modos[0]=modos[1]=Arma.Ninguna;Metodo(m,"Resolver",3f,.05f);
                Assert.AreEqual(Pantalla.Victoria,m.menu.Actual);
                Assert.That(m.menu.ganador.text,Does.Contain("SOLAR"));
                m.menu.Activar(m.menu.botones.First(b=>b.pagina==Pantalla.Victoria && b.accion==Accion.Jugar));
                Assert.IsTrue(hp.All(x=>x==m.reglas.vidaInicial));Assert.IsTrue(stats.All(x=>x==0));
                Assert.IsTrue(Campo<int[]>(m,"golpes").All(x=>x==0));
                Assert.AreEqual(0,Campo<TiempoDuelo>(m,"reloj").Duracion);
                Assert.IsTrue(Campo<HandTracking>(m,"pistas").jugadores.All(p=>!p.asignada));
                Assert.IsTrue(m.armas.All(a=>!a.espada.activeSelf && !a.escudo.activeSelf));
            }
            finally{EditorSceneManager.CloseScene(scene,true);AssetDatabase.DeleteAsset(ruta);}
        }
    }
}
