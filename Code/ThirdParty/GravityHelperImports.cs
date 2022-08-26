using System;
using MonoMod.ModInterop;

namespace Celeste.Mod.Trailine.ThirdParty {
    [ModImportName("GravityHelper")]
    public static class GravityHelperImports {

        public static Func<bool> IsPlayerInverted;

    }
}
