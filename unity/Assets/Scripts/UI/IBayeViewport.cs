using IBaye.UnityBridge;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IBaye.UnityBridge.UI
{
    [RequireComponent(typeof(RawImage))]
    public sealed class IBayeViewport : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public IBayeHost Host;
        public bool CaptureKeyboardInput = true;
        public bool CapturePointerInput = true;

        private RawImage _rawImage;
        private Texture2D _texture;
        private RectTransform _rect;

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _rect = transform as RectTransform;

            if (Host == null)
            {
                Host = FindObjectOfType<IBayeHost>();
            }
        }

        private void Update()
        {
            if (Host == null)
            {
                return;
            }

            if (Host.TryConsumeLatestFrame(out byte[] frameRgba, out int width, out int height))
            {
                RefreshTexture(frameRgba, width, height);
            }

            if (CaptureKeyboardInput)
            {
                PumpKeyboardInput();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CapturePointerInput || Host == null)
            {
                return;
            }

            if (TryMapToEngineSpace(eventData, out int x, out int y))
            {
                Host.SendTouch(IBayeNative.TouchDown, x, y);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!CapturePointerInput || Host == null)
            {
                return;
            }

            if (TryMapToEngineSpace(eventData, out int x, out int y))
            {
                Host.SendTouch(IBayeNative.TouchUp, x, y);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!CapturePointerInput || Host == null)
            {
                return;
            }

            if (TryMapToEngineSpace(eventData, out int x, out int y))
            {
                Host.SendTouch(IBayeNative.TouchMove, x, y);
            }
        }

        private void RefreshTexture(byte[] frameRgba, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                _texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                _texture.filterMode = FilterMode.Point;
                _texture.wrapMode = TextureWrapMode.Clamp;
                _rawImage.texture = _texture;
            }

            _texture.LoadRawTextureData(frameRgba);
            _texture.Apply(false, false);
        }

        private void PumpKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Host.SendKey(IBayeNative.KeyEnter);
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Backspace))
            {
                Host.SendKey(IBayeNative.KeyExit);
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Host.SendKey(IBayeNative.KeyUp);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Host.SendKey(IBayeNative.KeyDown);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Host.SendKey(IBayeNative.KeyLeft);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Host.SendKey(IBayeNative.KeyRight);
            }
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                Host.SendKey(IBayeNative.KeyPgUp);
            }
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                Host.SendKey(IBayeNative.KeyPgDn);
            }
        }

        private bool TryMapToEngineSpace(PointerEventData eventData, out int x, out int y)
        {
            x = 0;
            y = 0;

            if (_rect == null || Host.FrameWidth <= 0 || Host.FrameHeight <= 0)
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            Rect rect = _rect.rect;
            if (!rect.Contains(localPoint))
            {
                return false;
            }

            float relX = (localPoint.x - rect.xMin) / rect.width;
            float relYTopOrigin = (rect.yMax - localPoint.y) / rect.height;

            relX = Mathf.Clamp01(relX);
            relYTopOrigin = Mathf.Clamp01(relYTopOrigin);

            x = Mathf.FloorToInt(relX * (Host.FrameWidth - 1));
            y = Mathf.FloorToInt(relYTopOrigin * (Host.FrameHeight - 1));
            return true;
        }
    }
}
