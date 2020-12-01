using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace Jarvis
{
    public class JarvisScript
    {
        private List<Point> points;
        private List<Point> calculatedPoints;

        public JarvisScript(List<Point> npoints)
        {
            points = npoints;
        }

        private Point GetLeftPoint()
        {
            if (points == null || points.Count == 0)
            {
                return null;
            }

            return points.OrderBy(p1 => p1.GetPosition().x).First();
        }

        private void RunJarvis()
        {
            calculatedPoints = new List<Point>();
            Point originalPoint = GetLeftPoint();
            Point selectedPoint = null;

            Vector3 vectorSource = Vector3.up;
            Vector3 vectorTest = Vector3.zero;

            int maxIteration = 10000;
            int currentIndex = 0;
            
            while (originalPoint != selectedPoint && ++currentIndex < maxIteration)
            {
                if (selectedPoint == null)
                {
                    selectedPoint = originalPoint;
                }

                if (calculatedPoints.Contains(selectedPoint))
                {
                    Debug.Log("Already exist");
                }
                else
                {
                    calculatedPoints.Add(selectedPoint);
                }

                selectedPoint = points.OrderBy(calculatedPoint =>
                {
                    if (calculatedPoint == selectedPoint)
                    {
                        return 360;
                    }

                    vectorTest = calculatedPoint.GetPosition() - selectedPoint.GetPosition();
                    return MathUtils.AngleClockwise(vectorSource, vectorTest);
                }).First();

                vectorSource = vectorTest;
            }
        }

        public List<Point> ComputeAndPrintJarvis()
        {
            RunJarvis();

            if (calculatedPoints == null || calculatedPoints.Count == 0)
            {
                return null;
            }
            
            int index = 0;
            foreach (Point p in calculatedPoints)
            {
                p.SetGameObjectName("Objet : " + index);
                index++;
            }
            
            return calculatedPoints;
        }
    }
}