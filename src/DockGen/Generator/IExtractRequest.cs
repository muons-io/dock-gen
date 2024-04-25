namespace DockGen.Generator;

// marker interfaces
public interface IExtractRequest { }

public interface IExtractRequest<TResult> : IExtractRequest
{
}