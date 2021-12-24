using System;

namespace Celeste.Mod.Trailine.Utils {
    public static class CommonExtensions {

        public static T Apply<T>(this T obj, Action<T> action) {
            action(obj);
            return obj;
        }

    }
}
