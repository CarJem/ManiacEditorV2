using System.Drawing;

namespace ManiacEDv2
{
    public interface IDrawArea
    {
        void DisposeTextures();

        Rectangle GetScreen();
        double GetZoom();
    }
}
