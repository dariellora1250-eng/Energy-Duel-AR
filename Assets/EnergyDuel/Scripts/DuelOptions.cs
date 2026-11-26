using System;
using UnityEngine;

namespace EnergyDuel
{
    public enum Perfil { Original, Equilibrado, Rendimiento }

    [Serializable]
    public struct Confianzas
    {
        [Range(0,1)] public float deteccion, presencia, seguimiento;
        public Confianzas(float d,float p,float s) {deteccion=d; presencia=p; seguimiento=s;}
    }

    [Serializable]
    public sealed class DuelOptions
    {
        private const string Clave="EnergyDuelAR.opciones.v2";
        public Perfil perfil=Perfil.Equilibrado;
        public Confianzas umbrales=new Confianzas(.25f,.30f,.40f);
        public int ancho=640, fps=20;
        [Range(-.15f,.15f)] public float sensibilidad;
        [Range(0.5f,2)] public float respuesta=1;
        public bool diagnostico;
        public DuelOptions Copia() => (DuelOptions)MemberwiseClone();
        public void Guardar() { PlayerPrefs.SetString(Clave,JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
        public void Cargar()
        {
            if(PlayerPrefs.HasKey(Clave)) JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(Clave),this);
            if(fps!=10 && fps!=15 && fps!=20 && fps!=30) fps=20;
            if(ancho!=1280 && ancho!=0) ancho=640;
            umbrales.deteccion=Mathf.Clamp01(umbrales.deteccion); umbrales.presencia=Mathf.Clamp01(umbrales.presencia);
            umbrales.seguimiento=Mathf.Clamp01(umbrales.seguimiento);
            respuesta=Mathf.Clamp(respuesta,.5f,2); sensibilidad=Mathf.Clamp(sensibilidad,-.15f,.15f);
        }
    }
}
