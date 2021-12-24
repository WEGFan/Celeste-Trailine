using Microsoft.Xna.Framework;

namespace Celeste.Mod.Trailine.Utils {
    public static class ColorUtils {

        public static string ColorToHex(Color color) {
            return
                $"#{color.R.ToString("X").PadLeft(2, '0')}" +
                $"{color.G.ToString("X").PadLeft(2, '0')}" +
                $"{color.B.ToString("X").PadLeft(2, '0')}";
        }

    }
}
