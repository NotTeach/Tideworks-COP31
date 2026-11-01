using UnityEngine;

namespace Tideworks
{
    /// <summary>
    /// Sahneye ekleyin: sahne açılınca müziği ve ambiyansı başlatır. Kod yazmadan kullanmak için.
    /// </summary>
    public class TwSahneSesi : MonoBehaviour
    {
        public string muzik = Ses.MuzikAnaTema;
        public string ambiyans = Ses.AmbiyansDeniz;
        public bool tekneMotoru = false;

        void Start()
        {
            if (!string.IsNullOrEmpty(muzik)) TwSes.Muzik(muzik);
            TwSes.Ambiyans(ambiyans);
            TwSes.TekneMotoru(tekneMotoru);
        }
    }
}
