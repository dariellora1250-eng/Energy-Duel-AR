using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EnergyDuel;

internal static class DuelMeasureTemp
{
    const string Ticket="Temp/MedirDuelo", Result="Temp/MedidasDuelo.txt", Key="Duel.Medicion";
    static DuelCapture c; static WebCamTexture webcam; static FieldInfo stamp;
    static double begin, latency; static int rendered,captured,received; static long previous;
    [InitializeOnLoadMethod] static void Load(){
        EditorApplication.delayCall+=()=>{
            if(EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Ticket))return;
            var tag=File.ReadAllText(Ticket).Trim();File.Delete(Ticket);
            var sc=SceneManager.GetActiveScene();
            if(sc.isDirty || sc.path!="Assets/EnergyDuel/Generado/Energy Duel AR.unity"){File.AppendAllText(Result,tag+" skipped scene="+sc.path+" dirty="+sc.isDirty+"\n");return;}
            if(tag!="antes") EnergyDuel.Editor.DuelBuilder.Construir();
            SessionState.SetString(Key,tag);EditorApplication.isPlaying=true;
        };
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetString(Key,"")!=""){
                begin=EditorApplication.timeSinceStartup; Camera.onPreRender+=Frame;
                EditorApplication.update+=Finish;
            }
        };
    }
    static bool configured;
    static double sumAge,sumWait,sumRate;static int samples;
    static void Frame(Camera camera){
        if(!c){c=UnityEngine.Object.FindObjectOfType<DuelCapture>(); if(!c)return;stamp=typeof(DuelCapture).GetField("procesado",BindingFlags.Instance|BindingFlags.NonPublic);}
        if(camera!=c.camara || !c.Preparada)return;
        var tag=SessionState.GetString(Key,"");
        if(!configured)
        {
            configured=true;
            var options=c.opciones.Copia();
            options.perfil=tag.Contains("original")?Perfil.Original:tag.Contains("rendimiento")?Perfil.Rendimiento:Perfil.Equilibrado;
            options.umbrales=c.Preset(options.perfil);options.fps=20;
            options.ancho=tag.Contains("720")?1280:640;
            c.Aplicar(options,false);
            UnityEngine.Object.FindObjectOfType<DuelMatch>().NuevaPartida();
            begin=EditorApplication.timeSinceStartup;
        }
        if(EditorApplication.timeSinceStartup-begin<4)return;
        sumAge+=c.LatenciaMs;sumWait+=c.HastaEnvioMs;sumRate+=c.InferenciaHz;samples++;
        if(!webcam){var raw=UnityEngine.Object.FindObjectOfType<RawImage>();if(raw)webcam=raw.texture as WebCamTexture;}
        rendered++; if(webcam && webcam.didUpdateThisFrame)captured++;
        long ms=(long)stamp.GetValue(c);
        if(ms>0 && ms!=previous){received++;latency+=c.Segundos*1000-ms;previous=ms;}
    }
    static void Finish(){
        double dt=EditorApplication.timeSinceStartup-begin-4;
        if(dt<12)return;
        EditorApplication.update-=Finish;Camera.onPreRender-=Frame;
        File.AppendAllText(Result,$"{SessionState.GetString(Key,"")}: {dt:F1}s {webcam?.width}x{webcam?.height} render={rendered/dt:F1} capture={captured/dt:F1} callbacks={received/dt:F1} ageMs={latency/Math.Max(1,received):F0} ready={c?.Preparada} preEnvio={sumWait/Math.Max(1,samples):F0} e2eCallback={sumAge/Math.Max(1,samples):F0} graf={sumRate/Math.Max(1,samples):F1}\n");
        SessionState.EraseString(Key);EditorApplication.isPlaying=false;
    }
}
