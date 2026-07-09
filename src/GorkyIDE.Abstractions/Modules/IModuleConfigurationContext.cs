using GorkyIDE.Abstractions.Commands;
using GorkyIDE.Abstractions.Events;
using GorkyIDE.Abstractions.Services;
using GorkyIDE.Abstractions.Settings;

namespace GorkyIDE.Abstractions.Modules;

public interface IModuleConfigurationContext
{
    IServiceRegistry Services { get; }

    ICommandRegistry Commands { get; }

    ISettingsStore Settings { get; }
}
