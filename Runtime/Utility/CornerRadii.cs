using System;

namespace Tactile.UI.Utility
{
    [Serializable]
    public struct CornerRadii
    {
        public float topLeft;
        public float topRight;
        public float bottomLeft;
        public float bottomRight;

        public float this[(bool, bool) corner] => GetCornerSize(corner);

        public float GetCornerSize((bool, bool) corner)
        {
            return corner switch
            {
                (false, false) => bottomLeft,
                (false, true) => topLeft,
                (true, false) => bottomRight,
                (true, true) => topRight,
            };
        }
    }
}