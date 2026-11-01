using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tideworks
{
    /// <summary>
    /// Tideworks ses yöneticisi. Sahneye bir şey eklemenize gerek yok: ilk kullanımda kendini kurar.
    /// Sesler Assets/Tideworks/Resources/TideworksSes klasöründen adıyla yüklenir.
    ///
    /// Kullanım:
    ///   TwSes.Efekt(Ses.Tik);                      // kısa efekt
    ///   TwSes.Muzik(Ses.MuzikAnaTema);             // müziği yumuşak geçişle değiştir
    ///   TwSes.Ambiyans(Ses.AmbiyansDeniz);         // arka plan döngüsü
    ///   TwSes.TekneMotoru(true);                   // motor döngüsünü aç/kapat
    ///   TwSes.EkranGecisi(Ses.MuzikTamir, Ses.AmbiyansAtolye);  // geçiş sesi + müzik + ambiyans
    /// </summary>
    public class TwSes : MonoBehaviour
    {
        public static float MuzikSeviyesi = 0.35f;
        public static float AmbiyansSeviyesi = 0.5f;
        public static float EfektSeviyesi = 0.8f;

        const string Klasor = "TideworksSes/";
        static TwSes ornek;
        readonly Dictionary<string, AudioClip> onbellek = new Dictionary<string, AudioClip>();

        AudioSource muzikA, muzikB, ambiyans, motor, efekt;
        bool aAktif = true;
        Coroutine muzikGecis, ambiyansGecis;

        static TwSes O
        {
            get
            {
                if (ornek == null)
                {
                    var go = new GameObject("TwSes");
                    DontDestroyOnLoad(go);
                    ornek = go.AddComponent<TwSes>();
                    ornek.Kur();
                }
                return ornek;
            }
        }

        void Kur()
        {
            muzikA = Kaynak(true);
            muzikB = Kaynak(true);
            ambiyans = Kaynak(true);
            motor = Kaynak(true);
            efekt = Kaynak(false);
        }

        AudioSource Kaynak(bool dongu)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = dongu;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        AudioClip Yukle(string ad)
        {
            AudioClip c;
            if (!onbellek.TryGetValue(ad, out c))
            {
                c = Resources.Load<AudioClip>(Klasor + ad);
                if (c == null) Debug.LogWarning("[TwSes] Ses bulunamadı: " + ad);
                onbellek[ad] = c;
            }
            return c;
        }

        // ───────────── genel API ─────────────

        /// <summary>Kısa bir efekt çal. seviye: 0..1 (ek çarpan).</summary>
        public static void Efekt(string ad, float seviye = 1f)
        {
            var c = O.Yukle(ad);
            if (c == null) return;
            O.efekt.volume = 1f;
            O.efekt.pitch = 1f;
            O.efekt.PlayOneShot(c, EfektSeviyesi * seviye);
        }

        /// <summary>Hafif ses perdesi farkıyla çal (tekrarlanan seslerde doğallık için).</summary>
        public static void EfektCesitli(string ad, float seviye = 1f)
        {
            var c = O.Yukle(ad);
            if (c == null) return;
            var s = O.gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.pitch = Random.Range(0.94f, 1.06f);
            s.PlayOneShot(c, EfektSeviyesi * seviye);
            Destroy(s, c.length / s.pitch + 0.1f);
        }

        /// <summary>Müziği değiştir; eskisi gecis saniyede kısılır, yenisi açılır.</summary>
        public static void Muzik(string ad, float gecis = 1.2f)
        {
            var c = O.Yukle(ad);
            if (c == null) return;
            var aktif = O.aAktif ? O.muzikA : O.muzikB;
            if (aktif.clip == c && aktif.isPlaying) return;
            var yeni = O.aAktif ? O.muzikB : O.muzikA;
            O.aAktif = !O.aAktif;
            yeni.clip = c;
            yeni.volume = 0f;
            yeni.Play();
            if (O.muzikGecis != null) O.StopCoroutine(O.muzikGecis);
            O.muzikGecis = O.StartCoroutine(O.Capraz(aktif, yeni, MuzikSeviyesi, gecis));
        }

        /// <summary>Arka plan ambiyansını değiştir (null veya "" verirseniz kısar).</summary>
        public static void Ambiyans(string ad, float gecis = 1.2f)
        {
            var s = O.ambiyans;
            var yeni = string.IsNullOrEmpty(ad) ? null : O.Yukle(ad);
            if (yeni != null && s.clip == yeni && s.isPlaying) return; // zaten çalıyor, baştan başlatma
            if (O.ambiyansGecis != null) O.StopCoroutine(O.ambiyansGecis);
            O.ambiyansGecis = O.StartCoroutine(O.AmbiyansDegistir(s, yeni, gecis));
        }

        /// <summary>
        /// Oyun açılınca (hangi sahneden başlarsa başlasın) sakin Akdeniz teması ve deniz sesi kendiliğinden başlar.
        /// Sahneler kendi müziğini TwSes.Muzik / TwSahneSesi ile değiştirebilir.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OyunAcilinca()
        {
            var aktif = O.aAktif ? O.muzikA : O.muzikB;
            if (!aktif.isPlaying) Muzik(Ses.MuzikAkdeniz);
            if (!O.ambiyans.isPlaying) Ambiyans(Ses.AmbiyansDeniz);
        }

        public static void TekneMotoru(bool acik)
        {
            var s = O.motor;
            if (acik)
            {
                if (s.clip == null) s.clip = O.Yukle(Ses.TekneMotor);
                if (!s.isPlaying) s.Play();
                O.StartCoroutine(O.Seviye(s, AmbiyansSeviyesi * 0.8f, 0.6f, false));
            }
            else
            {
                O.StartCoroutine(O.Seviye(s, 0f, 0.6f, true));
            }
        }

        /// <summary>Ekran değişiminde tek çağrı: geçiş sesi + müzik + ambiyans.</summary>
        public static void EkranGecisi(string muzik, string ambiyansAdi, string gecisSesi = Ses.GecisDalga)
        {
            if (!string.IsNullOrEmpty(gecisSesi)) Efekt(gecisSesi, 0.8f);
            if (!string.IsNullOrEmpty(muzik)) Muzik(muzik);
            Ambiyans(ambiyansAdi);
        }

        // Temel seviyeler: Settings menüsündeki kaydırıcılar bunları 0..1 ile çarpar.
        public const float MuzikTaban = 0.35f;
        public const float AmbiyansTaban = 0.5f;
        public const float EfektTaban = 0.8f;

        /// <summary>
        /// Settings menüsünden çağrılır (GameSettings). muzik: Music, efekt: Sound effects (0..1).
        /// Ambiyans ve tekne motoru efekt seviyesini izler. Çalan sesler anında güncellenir.
        /// </summary>
        public static void AyarlariUygula(float muzik, float efekt) => AyarlariUygula(muzik, efekt, efekt);

        /// <summary>ambiyans: Ocean waves (deniz/atölye döngüleri ve tekne motoru), 0..1.</summary>
        public static void AyarlariUygula(float muzik, float efekt, float ambiyans)
        {
            MuzikSeviyesi = MuzikTaban * Mathf.Clamp01(muzik);
            AmbiyansSeviyesi = AmbiyansTaban * Mathf.Clamp01(ambiyans);
            EfektSeviyesi = EfektTaban * Mathf.Clamp01(efekt);

            if (ornek == null) return;
            var aktif = ornek.aAktif ? ornek.muzikA : ornek.muzikB;
            if (ornek.muzikGecis != null) { ornek.StopCoroutine(ornek.muzikGecis); ornek.muzikGecis = null; }
            var pasif = ornek.aAktif ? ornek.muzikB : ornek.muzikA;
            pasif.volume = 0f;
            pasif.Stop();
            if (aktif.isPlaying) aktif.volume = MuzikSeviyesi;
            if (ornek.ambiyans.isPlaying) ornek.ambiyans.volume = AmbiyansSeviyesi;
            if (ornek.motor.isPlaying) ornek.motor.volume = AmbiyansSeviyesi * 0.8f;
        }

        public static void HepsiniDurdur(float gecis = 0.8f)
        {
            O.StartCoroutine(O.Seviye(O.muzikA, 0f, gecis, true));
            O.StartCoroutine(O.Seviye(O.muzikB, 0f, gecis, true));
            O.StartCoroutine(O.Seviye(O.ambiyans, 0f, gecis, true));
            O.StartCoroutine(O.Seviye(O.motor, 0f, gecis, true));
        }

        // ───────────── iç işler ─────────────

        IEnumerator Capraz(AudioSource eski, AudioSource yeni, float hedef, float sure)
        {
            float t = 0f, eskiBas = eski.volume;
            while (t < sure)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / sure);
                yeni.volume = hedef * k;
                eski.volume = eskiBas * (1f - k);
                yield return null;
            }
            yeni.volume = hedef;
            eski.volume = 0f;
            eski.Stop();
        }

        IEnumerator AmbiyansDegistir(AudioSource s, AudioClip yeni, float sure)
        {
            if (s.isPlaying) yield return Seviye(s, 0f, sure * 0.5f, true);
            if (yeni == null) yield break;
            s.clip = yeni;
            s.volume = 0f;
            s.Play();
            yield return Seviye(s, AmbiyansSeviyesi, sure * 0.5f, false);
        }

        IEnumerator Seviye(AudioSource s, float hedef, float sure, bool sonundaDurdur)
        {
            float bas = s.volume, t = 0f;
            while (t < sure)
            {
                t += Time.unscaledDeltaTime;
                s.volume = Mathf.Lerp(bas, hedef, Mathf.Clamp01(t / sure));
                yield return null;
            }
            s.volume = hedef;
            if (sonundaDurdur) s.Stop();
        }
    }

    /// <summary>Ses adları. Yazım hatası yapmamak için bunları kullanın.</summary>
    public static class Ses
    {
        // Arayüz
        public const string Tik = "ui_tik";
        public const string Hover = "ui_hover";
        public const string Onay = "ui_onay";
        public const string Hata = "ui_hata";
        public const string Pop = "ui_baloncuk_pop";
        // Geçişler
        public const string GecisDalga = "gecis_dalga";
        public const string GecisKabarcik = "gecis_kabarcik";
        public const string GecisParilti = "gecis_sayfa_parilti";
        // Tekne
        public const string TekneMotor = "tekne_motor_loop";
        public const string AgAt = "tekne_ag_at";
        public const string AtikTopla = "atik_topla";
        public const string AgDolu = "ag_dolu_uyari";
        // Tamir
        public const string ParcaTak = "parca_tak";
        public const string Cekic = "tamir_cekic";
        public const string Zimpara = "tamir_zimpara";
        public const string Vida = "tamir_vida";
        public const string MakineEritici = "makine_eritici";
        public const string MakineBitti = "makine_bitti";
        public const string RadyoAcilis = "radyo_acilis";
        public const string OnarimTamam = "onarim_tamam";
        // Döngüler
        public const string AmbiyansDeniz = "ambiyans_deniz_loop";
        public const string AmbiyansAtolye = "ambiyans_atolye_loop";
        public const string MuzikAnaTema = "muzik_ana_tema_loop";
        public const string MuzikTamir = "muzik_tamir_sakin_loop";
        /// <summary>Atolye sahneleri (ic mekan, tezgah, ayristirma): vibrafonlu caz, 112 bpm.</summary>
        public const string MuzikTamirciCaz = "08_YENI_Tamirci_Caz_vibrafon_112bpm";
        /// <summary>Diger sahneler (menu, deniz, kumsal): sakin, Akdeniz havasi (Tools/Audio/mediterranean_loop.py).</summary>
        public const string MuzikAkdeniz = "muzik_akdeniz_sakin_loop";
    }
}
