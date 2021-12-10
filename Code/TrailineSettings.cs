namespace Celeste.Mod.Trailine {

    public class TrailineSettings : EverestModuleSettings {

        public bool Enabled { get; set; } = true;

        public void CreateEnabledEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.OnOff("Enabled", Enabled)
                .Change(value => {
                    Enabled = value;
                    if (value) {
                        TrailineModule.Instance.Load();
                    } else {
                        TrailineModule.Instance.Unload();
                    }
                });
            textMenu.Add(item);
        }

    }
}
