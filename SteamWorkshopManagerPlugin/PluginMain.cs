using ModuleShared;
using Newtonsoft.Json;
using System.Collections.Generic;
using GSMyAdmin.WebServer;

namespace SteamWorkshopManagerPlugin;

[AMPDependency("GenericModule", "steamcmdplugin")]
public class PluginMain : AMPPlugin {
    private readonly Settings _settings;
    private readonly ILogger _log;
    private readonly IConfigSerializer _config;
    private readonly IPlatformInfo _platform;
    private readonly IRunningTasksManager _tasks;
    private readonly IApplicationWrapper _application;
    private readonly IPluginMessagePusher _message;
    private readonly IFeatureManager _features;

    private GSMyAdmin.WebServer.WebMethods _core;
    private GSMyAdmin.WebServer.WebMethods Core {
        get {
            if (_core != null) {
                return _core;
            }
            var webServer = (LocalWebServer) _features.RequestFeature<ISessionInjector>();
            var field = webServer.GetType().GetField("APImodule", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) {
                _log.Debug("Failed to get APImodule field from LocalWebServer");
                return null;
            }
            var apiService = (ApiService) field.GetValue(webServer);
            if (apiService == null) {
                _log.Debug("Failed to get APImodule from LocalWebServer");
                return null;
            }
            var baseMethodsField = apiService.GetType().GetField("baseMethods", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (baseMethodsField == null) {
                _log.Debug("Failed to get baseMethods field from ApiService");
                return null;
            }
            Core = (GSMyAdmin.WebServer.WebMethods) baseMethodsField.GetValue(apiService);
            if (Core == null) {
                _log.Debug("Failed to get baseMethods from ApiService");
                return null;
            }

            return _core;
        }
        set => _core = value;
    }

    private void SetSettings(Dictionary<string, object> settings) {
        Dictionary<string, string> configs = new();
        foreach (var kvp in settings) {
            configs[kvp.Key] = kvp.Value switch {
                string str => str,
                _ => JsonConvert.SerializeObject(settings[kvp.Key])
            };
            Core.SetConfig(kvp.Key, configs[kvp.Key]);
        }
        _message.Push("setsettings", settings);
    }
    
    //All constructor arguments after currentPlatform are optional, and you may ommit them if you don't
    //need that particular feature. The features you request don't have to be in any particular order.
    //Warning: Do not add new features to the feature manager here, only do that in Init();
    public PluginMain(ILogger log, IConfigSerializer config, IPlatformInfo platform,
        IRunningTasksManager taskManager, IApplicationWrapper application, 
        IPluginMessagePusher message, IFeatureManager features) {
        //These are the defaults, but other mechanisms are available.
        config.SaveMethod = PluginSaveMethod.KVP;
        config.KVPSeparator = "=";
        _log = log;
        _config = config;
        _platform = platform;
        _settings = config.Load<Settings>(AutoSave: true); //Automatically saves settings when they're changed.
        _tasks = taskManager;
        _application = application;
        _message = message;
        _features = features;
        _settings.SettingModified += Settings_SettingModified;
    }

    /*
        Rundown of the different interfaces you can ask for in your constructor:
        IRunningTasksManager - Used to put tasks in the left hand side of AMP to update the user on progress.
        IApplicationWrapper - A reference to the running application from the running module.
        IPluginMessagePusher - For 'push' type notifications that your front-end code can react to via PushedMessage in Plugin.js
        IFeatureManager - To expose/consume features to/from other plugins.
    */

    //Your init function should not invoke any code that depends on other plugins.
    //You may expose functionality via IFeatureManager.RegisterFeature, but you cannot yet use RequestFeature.
    public override void Init(out WebMethodsBase APIMethods) {
        APIMethods = new WebMethods();
    }

    void Settings_SettingModified(object sender, SettingModifiedEventArgs e) {
        //If you need to export settings to a different application, this is where you'd do it.
    }

    public override bool HasFrontendContent => true;

    //This gets called after every plugin is loaded. From here on it's safe
    //to use code that depends on other plugins and use IFeatureManager.RequestFeature
    public override void PostInit() {

    }

    public override IEnumerable<SettingStore> SettingStores => Utilities.EnumerableFrom(_settings);
}
