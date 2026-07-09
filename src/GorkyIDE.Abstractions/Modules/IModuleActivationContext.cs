using GorkyIDE.Abstractions.Commands;
using GorkyIDE.Abstractions.Events;
using GorkyIDE.Abstractions.Settings;

namespace GorkyIDE.Abstractions.Modules;

public interface IModuleActivationContext
{
    IServiceProvider Services { get; }

    ICommandRegistry Commands { get; }

    IEventBus Events { get; }

    ISettingsStore Settings { get; }
}
