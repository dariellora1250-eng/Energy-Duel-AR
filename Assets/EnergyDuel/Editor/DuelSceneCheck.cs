using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mediapipe.Unity.Sample;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using MpScreen = Mediapipe.Unity.Screen;

namespace EnergyDuel.Editor
{
    internal static class DuelSceneCheck
    {
        internal static T Unico<T>(Scene escena) where T : Component
        {
            var encontrados=escena.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<T>(true)).ToArray();
            if (encontrados.Length!=1)
                throw new InvalidDataException($"{escena.path} · se esperaba un {typeof(T).Name}, encontrados {encontrados.Length}.");
            return encontrados[0];
        }

        internal static void Reparar(Scene escena)
        {
            var juego=Unico<DuelMatch>(escena);
            juego.captura=Unico<DuelCapture>(escena);
            juego.hud=Unico<DuelHud>(escena);
            juego.reglas=AssetDatabase.LoadAssetAtPath<DuelRules>(DuelLayout.RulesPath);
            var roots=escena.GetRootGameObjects();
            var zonas=new Transform[2];
            var armas=new DuelWeapon[2];
            var barras=new Image[2];
            for (int i=0;i<2;i++)
            {
                int j=i+1;
                // Recupera instancias reales del prefab, incluso si el enlace de la escena se perdió.
                var ruta=DuelAssets.Output+"/Arsenal J"+j+".prefab";
                var instancias=roots.Where(o=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o)==ruta).ToArray();
                if (instancias.Length>1) throw new InvalidDataException($"J{j} · hay {instancias.Length} instancias de {ruta}.");
                var original=AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (!original) throw new InvalidDataException("No se pudo importar "+ruta);
                var instancia=instancias.Length==1 ? instancias[0] : (GameObject)PrefabUtility.InstantiatePrefab(original,escena);
                armas[i]=instancia.GetComponent<DuelWeapon>();
                zonas[i]=roots.FirstOrDefault(o=>o.name=="Núcleo vulnerable J"+j)?.transform;
                barras[i]=juego.hud.transform.Find($"Cabecera/Jugador {j}/Capacidad/Salud")?.GetComponent<Image>();
            }
            juego.armas=armas;
            juego.zonas=zonas;
            juego.hud.barras=barras;
            var camara=Unico<Camera>(escena);
            var pantalla=Unico<MpScreen>(escena);
            var raw=Unico<RawImage>(escena);
            juego.captura.camara=camara;
            // Solo se desactiva el gráfico. Screen sigue disponible para inicializar la webcam.
            raw.enabled=false;
            Asignar(pantalla,"_screen",raw);
            Asignar(juego.captura,"screen",pantalla);
            Asignar(juego.captura,"_bootstrapPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(DuelAssets.Output+"/Bootstrap.prefab"));
            DuelInterface.AjustarAspecto(juego.hud);
            juego.menu=DuelMenuBuilder.Crear(juego.hud);
            EditorUtility.SetDirty(juego);
            EditorUtility.SetDirty(juego.hud);
        }

        private static void Asignar(Object propietario,string campo,Object valor)
        {
            var so=new SerializedObject(propietario);
            var p=so.FindProperty(campo);
            if (p==null) throw new InvalidDataException(propietario.name+" · campo desconocido "+campo);
            p.objectReferenceValue=valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static DuelMatch Validar(Scene escena)
        {
            var juego=Unico<DuelMatch>(escena);
            var errores=new List<string>();
            void Exigir(bool cumple,string mensaje) { if (!cumple) errores.Add(mensaje); }
            void Ref(Object obj,string ruta) => Exigir(obj,ruta+" · referencia ausente");
            void Par<T>(T[] items,string campo) where T : Object
            {
                Exigir(items?.Length==2,campo+" · se esperaban 2 entradas, hay "+(items?.Length??0));
                if (items==null) return;
                for (int i=0;i<items.Length;i++) Ref(items[i],campo+"["+i+"]");
                if(items.Length==2) Exigir(items[0]!=items[1],campo+" · ambos jugadores apuntan al mismo objeto");
            }
            Ref(juego.captura,"DuelMatch.captura"); Ref(juego.hud,"DuelMatch.hud"); Ref(juego.reglas,"DuelMatch.reglas");
            Ref(juego.menu,"DuelMatch.menu");
            if(juego.menu)
            {
                var m=juego.menu;
                Exigir(m.paginas?.Length==4 && m.paginas.All(x=>x),"DuelMenu.paginas");
                Exigir(m.botones?.Length>=27 && m.botones.All(b=>b!=null && b.zona && b.progreso),"DuelMenu.botones");
                Exigir(m.valores?.Length==9 && m.valores.All(x=>x),"DuelMenu.valores");
                Ref(m.ganador,"DuelMenu.ganador");Ref(m.resumen,"DuelMenu.resumen");Ref(m.diagnostico,"DuelMenu.diagnostico");Ref(m.nota,"DuelMenu.nota");
                Par(m.cursores,"DuelMenu.cursores");
            }
            Par(juego.zonas,"DuelMatch.zonas"); Par(juego.armas,"DuelMatch.armas");
            if (juego.armas!=null) for (int i=0;i<juego.armas.Length;i++)
            {
                var a=juego.armas[i];
                if (!a) continue;
                string prefijo=$"J{i+1} / DuelWeapon.";
                Ref(a.espada,prefijo+"espada"); Ref(a.escudo,prefijo+"escudo"); Ref(a.destello,prefijo+"destello");
                Ref(a.inicioHoja,prefijo+"inicioHoja"); Ref(a.punta,prefijo+"punta"); Ref(a.materialEstela,prefijo+"materialEstela");
            }
            var hud=juego.hud;
            if (hud)
            {
                Ref(hud.creditos,"DuelHud.creditos"); Ref(hud.aviso,"DuelHud.aviso"); Ref(hud.resultado,"DuelHud.resultado");
                Ref(hud.reloj,"DuelHud.reloj"); Ref(hud.zonaReinicio,"DuelHud.zonaReinicio");
                Ref(hud.reiniciar,"DuelHud.reiniciar"); Ref(hud.progreso,"DuelHud.progreso");
                Par(hud.estados,"DuelHud.estados"); Par(hud.vidas,"DuelHud.vidas"); Par(hud.barras,"DuelHud.barras");
            }
            if (juego.captura)
            {
                Ref(juego.captura.camara,"DuelCapture.camara");
                var so=new SerializedObject(juego.captura);
                var pantalla=so.FindProperty("screen").objectReferenceValue as MpScreen;
                Ref(pantalla,"DuelCapture.screen");
                if (pantalla)
                {
                    var raw=new SerializedObject(pantalla).FindProperty("_screen").objectReferenceValue as RawImage;
                    Ref(raw,"Screen._screen (RawImage)");
                    if (raw) Exigir(raw.GetComponentInParent<Canvas>()?.worldCamera==juego.captura.camara,"Fondo de webcam · Canvas.worldCamera");
                }
                var prefab=so.FindProperty("_bootstrapPrefab").objectReferenceValue as GameObject;
                var bootstrap=prefab ? prefab.GetComponent<Bootstrap>() : null;
                Ref(bootstrap,"DuelCapture._bootstrapPrefab / Bootstrap");
                if (bootstrap)
                {
                    var settings=new SerializedObject(bootstrap).FindProperty("_appSettings").objectReferenceValue as AppSettings;
                    Exigir(settings && AssetDatabase.GetAssetPath(settings)==DuelAssets.Output+"/Webcam.asset","Bootstrap._appSettings · se esperaba Generado/Webcam.asset");
                    if (settings)
                    {
                        Exigir(settings.defaultImageSource==ImageSourceType.WebCamera,"Webcam.defaultImageSource · se esperaba WebCamera");
                        Exigir(settings.assetLoaderType==AppSettings.AssetLoaderType.StreamingAssets,"Webcam.assetLoaderType · se esperaba StreamingAssets");
                    }
                }
            }
            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)
                        errores.Add(raiz.name+"/"+t.name+" · componente con script ausente");
            if (errores.Count>0) throw new InvalidDataException(escena.path+"\n• "+string.Join("\n• ",errores));
            return juego;
        }
    }
}
