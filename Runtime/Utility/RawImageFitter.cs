using UnityEngine;
using UnityEngine.UI;

namespace Tactile.UI.Utility
{
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(AspectRatioFitter))]
    public class RawImageFitter : MonoBehaviour
    {
        private RawImage _rawImage;
        private AspectRatioFitter _fitter;
        private Vector2Int _lastDimensions;
        
        #region Unity Events

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
        }

        private void Update()
        {
            if (!_rawImage || !_rawImage.texture)
                return;

            var tex = _rawImage.texture;
            var currentSize = new Vector2Int(tex.width, tex.height);
            if (currentSize == _lastDimensions)
                return;

            var ratio = (float)currentSize.x / currentSize.y;
            _fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            _fitter.aspectRatio = ratio;
            _lastDimensions = currentSize;
        }

        #endregion
    }
}