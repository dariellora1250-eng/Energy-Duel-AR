using UnityEngine;

namespace EnergyDuel
{
    public sealed class DuelRules : ScriptableObject
    {
        [Header("CrÃ©ditos")]
        public string nombre = "", matricula = "";
        [Header("Gestos Â· puntuaciÃ³n de extensiÃ³n")]
        [Range(0,1)] public float espadaHasta = .68f;
        [Range(0,1)] public float escudoDesde = .82f;
        [Min(.02f)] public float confirmarGesto = .11f;
        [Header("Seguimiento Â· segundos")]
        [Min(.05f)] public float vigencia = .22f;
        [Range(.25f,.5f)] public float retencionVisual = .35f;
        [Min(.03f)] public float desvanecer = .16f;
        [Min(.20f)] public float edadMaxima = .44f;
        [Min(.4f)] public float olvido = 1.4f;
        [Min(.1f)] public float calibracion = .45f;
        [Range(3,45)] public float suavizado = 20;
        [Header("Combate Â· distancias en alturas de imagen")]
        public Vector2 zonaJ1 = new Vector2(.30f,.48f), zonaJ2 = new Vector2(.70f,.48f);
        [Range(.025f,.16f)] public float radioZona = .066f;
        [Min(1)] public int vidaInicial = 100, dano = 10;
        [Min(.05f)] public float intervalo = .48f;
        [Min(.01f)] public float velocidadMinima = .24f;
        [Min(.001f)] public float recorridoMinimo = .022f;
        [Min(.1f)] public float reinicio = 1.2f;
        public bool estela = true;
        [Header("Choques y navegaciÃ³n")]
        [Range(20,35)] public float giroDuelo=30;
        [Range(.15f,.25f)] public float tiempoDuelo=.20f;
        [Range(50,75)] public float giroBloqueo=60;
        [Range(.25f,.4f)] public float tiempoBloqueo=.32f;
        [Range(.35f,2f)] public float tiempoSeleccion=.75f;
        [Header("Durabilidad y penalizaciÃ³n Â· segundos")]
        [Min(0)] public float penalizacionEspada=3f, penalizacionEscudo=2f;
        [Min(0)] public float desgasteChoqueEspadas=10, desgastePresionSegundo=20, demoraPresion=.35f;
        [Min(0)] public float desgasteEspadaEscudo=20, desgasteEscudoBloqueando=8, desgasteChoqueEscudos=5;
        [Range(20,75)] public float giroChoqueEspadas=30;
        [Range(50,90)] public float giroChoqueEscudos=75;
        [Range(.15f,.5f)] public float tiempoChoqueEscudos=.42f;

        private void OnValidate()
        {
            escudoDesde = Mathf.Max(escudoDesde, espadaHasta + .02f);
            vigencia = Mathf.Min(vigencia, retencionVisual);
        }
    }
}

