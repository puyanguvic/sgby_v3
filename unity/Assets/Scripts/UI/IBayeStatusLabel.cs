using IBaye.UnityBridge;
using UnityEngine;
using UnityEngine.UI;

namespace IBaye.UnityBridge.UI
{
    [RequireComponent(typeof(Text))]
    public sealed class IBayeStatusLabel : MonoBehaviour
    {
        public IBayeHost Host;
        public string Prefix = "Engine: ";

        private Text _text;

        private void Awake()
        {
            _text = GetComponent<Text>();
            if (Host == null)
            {
                Host = FindObjectOfType<IBayeHost>();
            }
        }

        private void Update()
        {
            if (Host == null)
            {
                _text.text = Prefix + "host missing";
                return;
            }

            _text.text = Prefix + Host.StatusText;
        }
    }
}
