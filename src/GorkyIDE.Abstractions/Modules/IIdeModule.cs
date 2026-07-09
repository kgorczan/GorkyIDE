namespace GorkyIDE.Abstractions.Modules;

public interface IIdeModule
{
    string Id { get; }

    ValueTask ConfigureAsync(IModuleConfigurationContext context, CancellationToken cancellationToken);

    ValueTask ActivateAsync(IModuleActivationContext context, CancellationToken cancellationToken);

    ValueTask DeactivateAsync(CancellationToken cancellationToken);
}
