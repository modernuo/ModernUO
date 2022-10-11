/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Rectangle3D.cs                                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server;

[NoSort]
[PropertyObject]
public struct Rectangle3D
{
    private Point3D m_Start;
    private Point3D m_End;

    public Rectangle3D(Point3D start, Point3D end)
    {
        m_Start = start;
        m_End = end;
    }

    public Rectangle3D(int x, int y, int z, int width, int height, int depth)
    {
        m_Start = new Point3D(x, y, z);
        m_End = new Point3D(x + width, y + height, z + depth);
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Point3D Start
    {
        get => m_Start;
        set => m_Start = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Point3D End
    {
        get => m_End;
        set => m_End = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int X
    {
        get => m_Start.m_X;
        set => m_Start.m_X = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Y
    {
        get => m_Start.m_Y;
        set => m_Start.m_Y = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Z
    {
        get => m_Start.m_Z;
        set => m_Start.m_Z = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Width => m_End.X - m_Start.X;

    [CommandProperty(AccessLevel.Counselor)]
    public int Height => m_End.Y - m_Start.Y;

    [CommandProperty(AccessLevel.Counselor)]
    public int Depth => m_End.Z - m_Start.Z;

    public void MakeHold(Rectangle3D r)
    {
        if (r.m_Start.m_X < m_Start.m_X)
        {
            m_Start.m_X = r.m_Start.m_X;
        }

        if (r.m_Start.m_Y < m_Start.m_Y)
        {
            m_Start.m_Y = r.m_Start.m_Y;
        }

        if (r.m_Start.m_Z < m_Start.m_Z)
        {
            m_Start.m_Z = r.m_Start.m_Z;
        }

        if (r.m_End.m_X > m_End.m_X)
        {
            m_End.m_X = r.m_End.m_X;
        }

        if (r.m_End.m_Y > m_End.m_Y)
        {
            m_End.m_Y = r.m_End.m_Y;
        }

        if (r.m_End.m_Z < m_End.m_Z)
        {
            m_End.m_Z = r.m_End.m_Z;
        }
    }

    public bool Contains(Point3D p) =>
        p.m_X >= m_Start.m_X
        && p.m_X < m_End.m_X
        && p.m_Y >= m_Start.m_Y
        && p.m_Y < m_End.m_Y
        && p.m_Z >= m_Start.m_Z
        && p.m_Z < m_End.m_Z;

    public bool Contains(Point2D p) =>
        p.m_X >= m_Start.m_X
        && p.m_X < m_End.m_X
        && p.m_Y >= m_Start.m_Y
        && p.m_Y < m_End.m_Y;

    public bool Contains(IPoint2D p) =>
        p.X >= m_Start.m_X
        && p.X < m_End.m_X
        && p.Y >= m_Start.m_Y
        && p.Y < m_End.m_Y;

    public bool Contains(IPoint3D p) =>
        p.X >= m_Start.m_X
        && p.X < m_End.m_X
        && p.Y >= m_Start.m_Y
        && p.Y < m_End.m_Y
        && p.Z >= m_Start.m_Z
        && p.Z < m_End.m_Z;
}
