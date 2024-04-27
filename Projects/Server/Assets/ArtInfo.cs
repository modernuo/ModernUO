using System.Drawing;

namespace Server.Assets;

public class ArtInfo(int width, int height, Rectangle bounds)
{
    public int Width = width;
    public int Height = height;
    public Rectangle Bounds = bounds;
}
