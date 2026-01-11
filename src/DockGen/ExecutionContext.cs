namespace DockGen;

public static class ExecutionContext
{
    private static readonly AsyncLocal<IServiceProvider?> ServiceProviderAsyncLocal = new();

    public static IServiceProvider? ServiceProvider
    {
        get => ServiceProviderAsyncLocal.Value;

        set => ServiceProviderAsyncLocal.Value = value;
    }
}
