using ModuleShared;

namespace SteamWorkshopManagerPlugin;

public class Settings : SettingStore {
    public class WorkshopManagerSettings : SettingSectionStore { }

    public WorkshopManagerSettings WorkshopManager = new WorkshopManagerSettings();
}
