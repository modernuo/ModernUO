using System;

namespace Server.Misc
{
    public delegate void DoEffect_Callback(Point3D p, Map map);

    public static class Geometry
    {
        public static void Swap<T>(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        public static double RadiansToDegrees(double angle) => angle * (180.0 / Math.PI);

        public static double DegreesToRadians(double angle) => angle * (Math.PI / 180.0);

        public static Point2D ArcPoint(Point3D loc, int radius, int angle)
        {
            int sideA, sideB;

            angle = Math.Clamp(angle, 0, 90);

            sideA = (int)Math.Round(radius * Math.Sin(DegreesToRadians(angle)));
            sideB = (int)Math.Round(radius * Math.Cos(DegreesToRadians(angle)));

            return new Point2D(loc.X - sideB, loc.Y - sideA);
        }

        public static void Circle2D(Point3D loc, Map map, int radius, DoEffect_Callback effect)
        {
            Circle2D(loc, map, radius, effect, 0, 360);
        }

        public static void Circle2D(Point3D loc, Map map, int radius, DoEffect_Callback effect, int angleStart, int angleEnd)
        {
            if (angleStart is < 0 or > 360)
            {
                angleStart = 0;
            }

            if (angleEnd is > 360 or < 0)
            {
                angleEnd = 360;
            }

            if (angleStart == angleEnd)
            {
                return;
            }

            var opposite = angleStart > angleEnd;

            var startQuadrant = angleStart / 90;
            var endQuadrant = angleEnd / 90;

            var start = ArcPoint(loc, radius, angleStart % 90);
            var end = ArcPoint(loc, radius, angleEnd % 90);

            if (opposite)
            {
                Swap(ref start, ref end);
                Swap(ref startQuadrant, ref endQuadrant);
            }

            var startPoint = new CirclePoint(start, angleStart, startQuadrant);
            var endPoint = new CirclePoint(end, angleEnd, endQuadrant);

            var error = -radius;
            var x = radius;
            var y = 0;

            while (x > y)
            {
                plot4points(loc, map, x, y, startPoint, endPoint, effect, opposite);
                plot4points(loc, map, y, x, startPoint, endPoint, effect, opposite);

                error += y * 2 + 1;
                ++y;

                if (error >= 0)
                {
                    --x;
                    error -= x * 2;
                }
            }

            plot4points(loc, map, x, y, startPoint, endPoint, effect, opposite);
        }

        public static void plot4points(
            Point3D loc, Map map, int x, int y, CirclePoint start, CirclePoint end,
            DoEffect_Callback effect, bool opposite
        )
        {
            var pointA = new Point2D(loc.X - x, loc.Y - y);
            var pointB = new Point2D(loc.X - y, loc.Y - x);

            var quadrant = 2;

            if (x == 0 && start.Quadrant == 3)
            {
                quadrant = 3;
            }

            if (WithinCircleBounds(quadrant == 3 ? pointB : pointA, quadrant, loc, start, end, opposite))
            {
                effect(new Point3D(loc.X + x, loc.Y + y, loc.Z), map);
            }

            quadrant = 3;

            if (y == 0 && start.Quadrant == 0)
            {
                quadrant = 0;
            }

            if (x != 0 && WithinCircleBounds(quadrant == 0 ? pointA : pointB, quadrant, loc, start, end, opposite))
            {
                effect(new Point3D(loc.X - x, loc.Y + y, loc.Z), map);
            }

            if (y != 0 && WithinCircleBounds(pointB, 1, loc, start, end, opposite))
            {
                effect(new Point3D(loc.X + x, loc.Y - y, loc.Z), map);
            }

            if (x != 0 && y != 0 && WithinCircleBounds(pointA, 0, loc, start, end, opposite))
            {
                effect(new Point3D(loc.X - x, loc.Y - y, loc.Z), map);
            }
        }

        public static bool WithinCircleBounds(
            Point2D pointLoc, int pointQuadrant, Point3D center, CirclePoint start,
            CirclePoint end, bool opposite
        )
        {
            if (start.Angle == 0 && end.Angle == 360)
            {
                return true;
            }

            var startX = start.Point.X;
            var startY = start.Point.Y;
            var endX = end.Point.X;
            var endY = end.Point.Y;

            var x = pointLoc.X;
            var y = pointLoc.Y;

            if (pointQuadrant < start.Quadrant || pointQuadrant > end.Quadrant)
            {
                return opposite;
            }

            if (pointQuadrant > start.Quadrant && pointQuadrant < end.Quadrant)
            {
                return !opposite;
            }

            var withinBounds = true;

            if (start.Quadrant == end.Quadrant)
            {
                if (startX == endX && (x > startX || y > startY || y < endY))
                {
                    withinBounds = false;
                }
                else if (startY == endY && (y < startY || x < startX || x > endX))
                {
                    withinBounds = false;
                }
                else if (x < startX || x > endX || y > startY || y < endY)
                {
                    withinBounds = false;
                }
            }
            else if (pointQuadrant == start.Quadrant && (x < startX || y > startY))
            {
                withinBounds = false;
            }
            else if (pointQuadrant == end.Quadrant && (x > endX || y < endY))
            {
                withinBounds = false;
            }

            return opposite ? !withinBounds : withinBounds;
        }

        public static void Line2D(Point3D start, Point3D end, Map map, DoEffect_Callback effect)
        {
            var steep = (end.Y - start.Y).Abs() > (end.X - start.X).Abs();

            var x0 = start.X;
            var x1 = end.X;
            var y0 = start.Y;
            var y1 = end.Y;

            if (steep)
            {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }

            if (x0 > x1)
            {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }

            var deltax = x1 - x0;
            var deltay = (y1 - y0).Abs();
            var error = deltax / 2;
            var ystep = y0 < y1 ? 1 : -1;
            var y = y0;

            for (var x = x0; x <= x1; x++)
            {
                if (steep)
                {
                    effect(new Point3D(y, x, start.Z), map);
                }
                else
                {
                    effect(new Point3D(x, y, start.Z), map);
                }

                error -= deltay;

                if (error < 0)
                {
                    y += ystep;
                    error += deltax;
                }
            }
        }

        public class CirclePoint
        {
            public CirclePoint(Point2D point, int angle, int quadrant)
            {
                Point = point;
                Angle = angle;
                Quadrant = quadrant;
            }

            public Point2D Point { get; }

            public int Angle { get; }

            public int Quadrant { get; }
        }
    }
}
