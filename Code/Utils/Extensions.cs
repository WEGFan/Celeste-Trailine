using System;

namespace Celeste.Mod.Trailine.Utils {
    public static class CommonExtensions {

        public static T Apply<T>(this T obj, Action<T> action) {
            action(obj);
            return obj;
        }

    }

    public static class OverworldExtensions {

        public static Oui Goto(this Overworld overworld, Type type) {
            Oui next = (Oui)typeof(Overworld).GetMethod("Goto")!
                .MakeGenericMethod(type)
                .Invoke(overworld, null);
            return next;
        }

    }
}
