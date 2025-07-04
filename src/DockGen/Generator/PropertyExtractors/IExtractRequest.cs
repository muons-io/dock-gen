namespace DockGen.Generator.PropertyExtractors;

// marker interfaces
public interface IExtractRequest { }

public interface IExtractRequest<TResult> : IExtractRequest
{
}