// Captura adaptada de HandLandmarkerRunner (c) 2023 homuler, licencia MIT.
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.Experimental;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mode = Mediapipe.Tasks.Vision.Core.RunningMode;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using UnityEngine.UI;

namespace EnergyDuel
{
    public sealed class DuelCapture : VisionTaskApiRunner<HandLandmarker>
    {
        [Header("Hand Landmarker · Windows")]
        public BaseOptions.Delegate inference = BaseOptions.Delegate.CPU;
        public ImageReadMode lectura = ImageReadMode.CPUAsync;
        public Mode modo = Mode.LIVE_STREAM;
        [Range(1,2)] public int manos = 2;
        public Confianzas original=new Confianzas(.01f,.10f,.90f);
        public Confianzas equilibrado=new Confianzas(.25f,.30f,.40f);
        public Confianzas rendimiento=new Confianzas(.15f,.20f,.25f);
        public DuelOptions opciones=new DuelOptions();
        public Camera camara;
        public string Estado {get;private set;}="Preparando la webcam…";
        public string AvisoResolucion {get;private set;}="";
        public bool Preparada {get;private set;}
        public float Aspecto {get;private set;}=16f/9f;
        public float Segundos => reloj.ElapsedMilliseconds*.001f;
        public float RenderHz {get;private set;}
        public float CapturaHz {get;private set;}
        public float InferenciaHz {get;private set;}
        public float LatenciaMs {get;private set;}
        public float HastaEnvioMs {get;private set;}
        public int AnchoReal => webcam ? webcam.width : 0;
        public int AltoReal => webcam ? webcam.height : 0;
        private readonly Stopwatch reloj=new Stopwatch();
        private readonly object cerrojo=new object();
        private Frame entrada=new Frame(),salida=new Frame();
        private bool pendiente,activo,cambiarDetector;
        private long procesado=-1,sumaLatencia;
        private int callbacks, renders, capturas;
        private int envios;
        private long esperaTotal;
        private float desdeMedicion,aspectoVentana;
        private WebCamTexture webcam;
        private RectTransform imagen;
        private RawImage fondo;
        private ImageSource fuente;
        private readonly Slot[] slots={new Slot(),new Slot()};
        private int enVuelo=-1, preparando=-1;
        private Vector2 flip,offset;
        private bool asyncRead;
        private TextureFramePool fallback;
        private int anchoAplicado, rechazados;
        private int fpsPrevios;
        private bool guardarResolucion;

        // Dos espacios fijos. Una imagen pertenece al grafo hasta su callback; la otra
        // contiene como máximo la lectura más reciente. Nunca se encolan inferencias.
        private sealed class Slot
        {
            public Unity.Collections.NativeArray<byte> bytes;
            public RenderTexture rt;
            public AsyncGPUReadbackRequest solicitud;
            public TextureFrame cpu;
            public long marca;
            public void Liberar()
            {
                cpu?.Release(); cpu=null;
                if(bytes.IsCreated) bytes.Dispose();
                if(rt){rt.Release();UnityEngine.Object.Destroy(rt);rt=null;}
            }
        }
        public Confianzas Preset(Perfil perfil) => perfil==Perfil.Original?original:perfil==Perfil.Rendimiento?rendimiento:equilibrado;
        private static int BitResolucion(int ancho)=>ancho==1280?2:1;
        public bool PuedeProbar(int ancho) => (rechazados & BitResolucion(ancho))==0;
        private int ResolucionConfirmada()=>webcam && webcam.width==1280 && webcam.height==720?1280:
            webcam && webcam.width==640 && webcam.height==480?640:0;
        private void Awake()
        {
            opciones.umbrales=Preset(opciones.perfil);
            opciones.Cargar();
            fpsPrevios=Application.targetFrameRate;
            Application.targetFrameRate=60;
        }
        public void Aplicar(DuelOptions nuevos,bool persistir=true)
        {
            var anterior=opciones;
            cambiarDetector |= !anterior.umbrales.Equals(nuevos.umbrales) || anterior.perfil!=nuevos.perfil || anterior.ancho!=nuevos.ancho;
            opciones=nuevos.Copia();
            guardarResolucion=persistir && anterior.ancho!=opciones.ancho;
            if(persistir && !guardarResolucion) opciones.Guardar();
        }
        public sealed class Mano
        {
            public readonly Vector3[] imagen = new Vector3[21], geometria = new Vector3[21];
            public bool metrica;
            public float lateralidad;
        }
        public sealed class Frame
        {
            public readonly Mano[] manos = { new Mano(), new Mano() };
            public int cantidad;
            public float tiempo;
            public bool salto;
        }


        protected override IEnumerator Run()
        {
            activo=true; reloj.Start();
            yield return null;
            if(inference!=BaseOptions.Delegate.CPU || lectura==ImageReadMode.GPU)
            { Estado="Selecciona inferencia CPU y lectura CPU Async.";yield break; }
            fuente=ImageSourceProvider.ImageSource;
            if(fuente.sourceCandidateNames.Length==0){Estado="Webcam no disponible.";yield break;}
            fuente.SelectSource(0);
            imagen=(RectTransform)screen.transform;
            fondo=screen.GetComponent<RawImage>();
            var config=new HandLandmarkDetectionConfig();
            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);
            bool reiniciado=false;
            while(activo)
            {
                cambiarDetector=false;
                if(!webcam || anchoAplicado!=opciones.ancho)
                {
                    int pedido=opciones.ancho, anterior=anchoAplicado;
                    yield return AbrirCamara(pedido);
                    if(!webcam || (pedido!=0 && ResolucionConfirmada()!=pedido))
                    {
                        if(pedido!=0)rechazados|=BitResolucion(pedido);
                        AvisoResolucion="La cámara no confirmó la resolución solicitada.";
                        int alternativa=anterior!=0 ? anterior : pedido==1280 ? 640 : 1280;
                        yield return AbrirCamara(alternativa);
                        if(ResolucionConfirmada()!=alternativa)rechazados|=BitResolucion(alternativa);
                        if(!webcam)yield return AbrirCamara(0);
                        if(!webcam){Estado="La webcam no pudo iniciar. Comprueba su conexión.";yield break;}
                    }
                    else AvisoResolucion="";
                    anchoAplicado=ResolucionConfirmada();
                    if(opciones.ancho==pedido)opciones.ancho=anchoAplicado;
                    if(guardarResolucion){opciones.Guardar();guardarResolucion=false;}
                }
                config.Delegate=inference; config.ImageReadMode=lectura; config.RunningMode=modo; config.NumHands=manos;
                config.MinHandDetectionConfidence=opciones.umbrales.deteccion;
                config.MinHandPresenceConfidence=opciones.umbrales.presencia;
                config.MinTrackingConfidence=opciones.umbrales.seguimiento;
                var settings=config.GetHandLandmarkerOptions(modo==Mode.LIVE_STREAM?Recibir:null);
                try {taskApi=HandLandmarker.CreateFromOptions(settings);}
                catch(Exception e){Estado="No se pudo cargar el detector.";Debug.LogException(e,this);}
                if(taskApi==null)yield break;
                var tr=fuente.GetTransformationOptions();
                flip=new Vector2(tr.flipHorizontally?-1:1,tr.flipVertically?-1:1);
                offset=new Vector2(tr.flipHorizontally?1:0,tr.flipVertically?1:0);
                var imageOptions=new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees:(int)tr.rotationAngle);
                var resultado=HandLandmarkerResult.Alloc(manos);
                asyncRead=lectura==ImageReadMode.CPUAsync && SystemInfo.supportsAsyncGPUReadback;
                int width=Math.Min(webcam.width,opciones.perfil==Perfil.Rendimiento?480:640);
                int height=Mathf.RoundToInt((float)width*webcam.height/webcam.width);
                if(asyncRead) foreach(var s in slots)
                {
                    s.rt=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                    s.rt.Create();
                    s.bytes=new Unity.Collections.NativeArray<byte>(width*height*4,Unity.Collections.Allocator.Persistent,Unity.Collections.NativeArrayOptions.UninitializedMemory);
                }
                else fallback=new TextureFramePool(webcam.width,webcam.height,TextureFormat.RGBA32,2);
                long proximaLectura=0,proximoEnvio=0;
                bool timeout=false;
                Estado=""; Preparada=true;
                while(!cambiarDetector)
                {
                    yield return null;
                    long now=reloj.ElapsedMilliseconds;
                    if(enVuelo>=0 && Interlocked.Read(ref procesado)>=slots[enVuelo].marca)
                    {SoltarCPU(enVuelo);enVuelo=-1;}
                    if(enVuelo>=0 && now-slots[enVuelo].marca>3000){timeout=true;break;}
                    if(isPaused)continue;
                    bool lista=preparando>=0 && (!asyncRead || slots[preparando].solicitud.done);
                    if(lista && asyncRead && slots[preparando].solicitud.hasError){preparando=-1;lista=false;}
                    if(lista && enVuelo<0 && now>=proximoEnvio)
                    {
                        var s=slots[preparando];
                        if(now-s.marca<150)
                        {
                            using var frame=asyncRead ? new Mediapipe.Image(ImageFormat.Types.Format.Srgba,width,height,width*4,s.bytes) : s.cpu.BuildCPUImage();
                            enVuelo=preparando;
                            esperaTotal+=now-s.marca;envios++;
                            proximoEnvio=now+1000/opciones.fps;
                            if(modo==Mode.LIVE_STREAM) taskApi.DetectAsync(frame,s.marca,imageOptions);
                            else
                            {
                                bool ok=modo==Mode.IMAGE ? taskApi.TryDetect(frame,imageOptions,ref resultado) : taskApi.TryDetectForVideo(frame,s.marca,imageOptions,ref resultado);
                                Recibir(ok?resultado:default,frame,s.marca);
                            }
                        }
                        else SoltarCPU(preparando);
                        preparando=-1; lista=false;
                    }
                    if(!webcam.didUpdateThisFrame || now<proximaLectura || (preparando>=0 && !lista))continue;
                    int index=enVuelo==0?1:0;
                    if(preparando>=0) SoltarCPU(preparando);
                    var slot=slots[index];
                    if(asyncRead)
                    {
                        Graphics.Blit(webcam,slot.rt,flip,offset);
                        slot.solicitud=AsyncGPUReadback.RequestIntoNativeArray(ref slot.bytes,slot.rt,0,TextureFormat.RGBA32);
                    }
                    else
                    {
                        if(!fallback.TryGetTextureFrame(out slot.cpu))continue;
                        slot.cpu.ReadTextureOnCPU(webcam,tr.flipHorizontally,tr.flipVertically);
                    }
                    slot.marca=now;preparando=index;
                    proximaLectura=now+1000/opciones.fps;
                }
                Preparada=false;
                while(asyncRead && preparando>=0 && !slots[preparando].solicitud.done)yield return null;
                // Drena el único envío antes de cambiar el modelo o la resolución.
                while(enVuelo>=0 && Interlocked.Read(ref procesado)<slots[enVuelo].marca && reloj.ElapsedMilliseconds-slots[enVuelo].marca<3000)yield return null;
                ((IDisposable)taskApi).Dispose(); taskApi=null;
                LiberarSlots();
                lock(cerrojo) pendiente=false;
                if(timeout && reiniciado){Estado="El detector no responde.";yield break;}
                reiniciado=timeout;
            }
        }

        private IEnumerator AbrirCamara(int width)
        {
            if(webcam){fondo.enabled=false; fuente.Stop(); webcam=null;}
            var sizes=fuente.availableResolutions;
            int index=Array.FindIndex(sizes,s=>s.width==width && s.height==(width==640?480:720));
            if(width!=0)
            {
                if(index<0)yield break;
                fuente.SelectResolution(index);
            }
            // En Windows la lista del ejemplo es orientativa. Sólo aceptamos el tamaño negociado.
            var pasos=new System.Collections.Generic.Stack<IEnumerator>(); pasos.Push(fuente.Play());
            while(pasos.Count>0)
            {
                var paso=pasos.Peek();bool sigue;
                try{sigue=paso.MoveNext();}catch(Exception e){Estado=e.Message;fuente.Stop();yield break;}
                if(!sigue){pasos.Pop();continue;}
                if(paso.Current is IEnumerator hijo && !(hijo is CustomYieldInstruction)) pasos.Push(hijo);
                else yield return paso.Current;
            }
            webcam=fuente.GetCurrentTexture() as WebCamTexture;
            if(!webcam || !webcam.isPlaying){webcam=null;yield break;}
            screen.Initialize(fuente);fondo.enabled=true;
            bool rotada=Mediapipe.Unity.CoordinateSystem.ImageCoordinate.IsInverted(fuente.rotation);
            int h=rotada?webcam.width:webcam.height;
            Aspecto=(float)(rotada?webcam.height:webcam.width)/h;
            imagen.localScale=Vector3.one/h; AjustarCamara();
        }

        private void SoltarCPU(int i){slots[i].cpu?.Release();slots[i].cpu=null;}
        private void LiberarSlots()
        {
            foreach(var s in slots)s.Liberar();
            fallback?.Dispose();fallback=null;enVuelo=preparando=-1;
        }
        private void Update()
        {
            if(webcam && aspectoVentana!=camara.aspect)AjustarCamara();
            renders++; if(webcam && webcam.didUpdateThisFrame)capturas++;
            float t=Segundos,dt=t-desdeMedicion;
            if(dt<1)return;
            int n;long latency;
            lock(cerrojo){n=callbacks;latency=sumaLatencia;callbacks=0;sumaLatencia=0;}
            RenderHz=renders/dt;CapturaHz=capturas/dt;InferenciaHz=n/dt;
            LatenciaMs=n==0?0:(float)latency/n;
            HastaEnvioMs=envios==0?0:(float)esperaTotal/envios;
            esperaTotal=0;envios=0;
            desdeMedicion=t;renders=capturas=0;
        }
        private void AjustarCamara()
        {
            aspectoVentana = camara.aspect;
            camara.orthographicSize = .5f * Mathf.Max(1f, Aspecto / aspectoVentana);
        }

        // Mismo rectángulo, rotación y escala que el RawImage del ejemplo. Z sólo expresa relieve local.
        public Vector3 Proyectar(Vector3 punto)
        {
            var r = imagen.rect;
            var local = new Vector3((punto.x - .5f) * r.width, (.5f - punto.y) * r.height, punto.z * r.width);
            var p = imagen.TransformVector(local);
            return p;
        }

        private void Recibir(HandLandmarkerResult r, Mediapipe.Image ignorada, long ms)
        {
            // MediaPipe reutiliza las listas nativas. El callback solo escribe valores en un búfer propio.
            lock (cerrojo)
            {
                entrada.salto = pendiente;
                entrada.cantidad = 0;
                entrada.tiempo = ms * .001f;
                int n = Math.Min(2, r.handLandmarks?.Count ?? 0);
                for (int i = 0; i < n; i++)
                {
                    var puntos = r.handLandmarks[i].landmarks;
                    if (puntos == null || puntos.Count != 21) continue;
                    var m = entrada.manos[entrada.cantidad];
                    var mundo = r.handWorldLandmarks != null && i < r.handWorldLandmarks.Count ? r.handWorldLandmarks[i].landmarks : null;
                    m.metrica = mundo != null && mundo.Count == 21;
                    bool valido = true;
                    for (int k = 0; k < 21; k++)
                    {
                        var p = puntos[k];
                        m.imagen[k] = new Vector3(p.x,p.y,p.z);
                        if (m.metrica) { var w = mundo[k]; m.geometria[k] = new Vector3(w.x,w.y,w.z); }
                        var s = p.x + p.y + p.z + (m.metrica ? m.geometria[k].sqrMagnitude : 0);
                        if (float.IsNaN(s) || float.IsInfinity(s)) valido = false;
                    }
                    if (!valido) continue;
                    var cats = r.handedness != null && i < r.handedness.Count ? r.handedness[i].categories : null;
                    m.lateralidad = cats == null || cats.Count == 0 ? 0 :
                        (cats[0].categoryName == "Right" ? cats[0].score : -cats[0].score);
                    entrada.cantidad++;
                }
                pendiente = true;
                sumaLatencia+=reloj.ElapsedMilliseconds-ms;callbacks++;
            }
            Interlocked.Exchange(ref procesado,ms);
        }

        public bool Leer(out Frame resultado)
        {
            lock (cerrojo)
            {
                resultado = null;
                if (!pendiente) return false;
                (entrada, salida) = (salida, entrada);
                pendiente = false;
                resultado = salida;
            }
            var rect=imagen.rect;
            var matriz=imagen.localToWorldMatrix;
            for (int i = 0; i < resultado.cantidad; i++)
            {
                var m = resultado.manos[i];
                for (int k = 0; k < 21; k++)
                {
                    var p=m.imagen[k];
                    m.imagen[k]=matriz.MultiplyVector(new Vector3((p.x-.5f)*rect.width,(.5f-p.y)*rect.height,p.z*rect.width));
                    if (!m.metrica) m.geometria[k] = m.imagen[k];
                }
            }
            return true;
        }

        public override void Stop()
        {
            if(!activo)return;
            activo=Preparada=false;
            var tarea=taskApi;
            base.Stop();
            ((IDisposable)tarea)?.Dispose();
            if(asyncRead && preparando>=0 && !slots[preparando].solicitud.done) slots[preparando].solicitud.WaitForCompletion();
            LiberarSlots();webcam=null;
            lock(cerrojo) pendiente=false;
        }
        private void OnDestroy(){Stop();Application.targetFrameRate=fpsPrevios;}
    }
}
