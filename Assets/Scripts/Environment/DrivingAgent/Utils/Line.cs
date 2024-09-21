using System;
using UnityEngine;

namespace Utils {
    [Serializable]
    public class Line {
        
        public Vector3 startPosition;
        public Vector3 endPosition;
        public Color color;

        public Line(Vector3 startPos, Vector3 endPos, Color lineColor) {
            startPosition = startPos;
            endPosition = endPos;
            color = lineColor;
        }
    }
}