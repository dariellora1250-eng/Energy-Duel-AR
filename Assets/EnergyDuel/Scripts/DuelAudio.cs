using UnityEngine;

namespace EnergyDuel
{
    /// <summary>Reproduce música y efectos locales. Los clips se guardan en Recursos/EnergyDuelAudio.</summary>
    public sealed class DuelAudio : MonoBehaviour
    {
        public static DuelAudio Instancia { get; private set; }
        private AudioSource musica, efectos;
        private AudioClip menu, combate, victoria, espada, escudo, impacto, roto, interfaz;
        private const string General="ED.vol", Musica="ED.music", Efectos="ED.sfx";
        public static float Volumen(string clave,float defecto)=>PlayerPrefs.GetFloat(clave,defecto);
        public static void GuardarVolumen(string clave,float valor){PlayerPrefs.SetFloat(clave,Mathf.Clamp01(valor));PlayerPrefs.Save();if(Instancia)Instancia.ActualizarVolumen();}
        private void Awake()
        {
            if(Instancia && Instancia!=this){Destroy(gameObject);return;} Instancia=this;
            musica=gameObject.AddComponent<AudioSource>();efectos=gameObject.AddComponent<AudioSource>();
            musica.loop=true;musica.playOnAwake=false;efectos.playOnAwake=false;
            menu=Cargar("title");combate=Cargar("urgent");victoria=Cargar("victory");
            espada=Cargar("sfxmint-energy-sword-07");escudo=Cargar("sfxmint-magic-scifi-shield-54");
            impacto=Cargar("impact");roto=Cargar("break");interfaz=Cargar("click");
            ActualizarVolumen();MusicaMenu();
        }
        private static AudioClip Cargar(string n)=>Resources.Load<AudioClip>("EnergyDuelAudio/"+n);
        public void ActualizarVolumen(){if(!musica)return;float g=Volumen(General,1);musica.volume=g*Volumen(Musica,.55f);efectos.volume=g*Volumen(Efectos,.8f);}
        public void MusicaMenu()=>Poner(menu); public void MusicaCombate()=>Poner(combate); public void MusicaVictoria()=>Poner(victoria);
        private void Poner(AudioClip c){if(!c||musica.clip==c)return;musica.clip=c;musica.Play();}
        public void SonidoArma(bool esEspada){Sonar(esEspada?espada:escudo);}
        public void SonidoChoque(bool escudoImpactado=false){Sonar(impacto);}
        public void SonidoRoto(){Sonar(roto?roto:impacto);}
        public void SonidoMenu(){Sonar(interfaz);}
        public void Sonar(AudioClip c){if(c)efectos.PlayOneShot(c);}
    }
}
