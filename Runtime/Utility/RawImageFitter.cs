using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tactile.UI.Utility
{
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(AspectRatioFitter))]
    public class RawImageFitter : MonoBehaviour
    {
        [SerializeField] private FitMode imageFit;
        
        private RawImage _rawImage;
        private AspectRatioFitter _fitter;
        private Vector2Int _lastDimensions;
        
        public FitMode ImageFit
        {
            get => imageFit;
            set => SetFitMode(value);
        }
        
        #region Unity Events
        
        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _fitter = GetComponent<AspectRatioFitter>();
            SetFitMode(imageFit);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                SetFitMode(imageFit);    
            }
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
            SetFitMode(imageFit);
        }

        #endregion

        private void SetFitMode(FitMode newFitMode)
        {
            if (!_fitter)
                return;
            
            imageFit = newFitMode;
            _fitter.aspectMode = imageFit switch
            {
                FitMode.Fill => AspectRatioFitter.AspectMode.EnvelopeParent,
                FitMode.Fit => AspectRatioFitter.AspectMode.FitInParent,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public enum FitMode
        {
            Fill,
            Fit
        }
    }
}