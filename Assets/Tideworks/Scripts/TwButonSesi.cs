using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tideworks
{
    /// <summary>
    /// Herhangi bir UI Button'a ekleyin: tıklayınca "tok", üstüne gelince hafif "blip" çalar.
    /// İsterseniz Inspector'dan farklı ses adları yazabilirsiniz.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TwButonSesi : MonoBehaviour, IPointerEnterHandler
    {
        public string tiklamaSesi = Ses.Tik;
        public string uzerineGelmeSesi = Ses.Hover;
        [Range(0f, 1f)] public float seviye = 1f;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => TwSes.Efekt(tiklamaSesi, seviye));
        }

        public void OnPointerEnter(PointerEventData e)
        {
            var b = GetComponent<Button>();
            if (b != null && b.interactable && !string.IsNullOrEmpty(uzerineGelmeSesi))
                TwSes.Efekt(uzerineGelmeSesi, seviye * 0.4f);
        }
    }
}
