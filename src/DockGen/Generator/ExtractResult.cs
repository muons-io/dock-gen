namespace DockGen.Generator;

public sealed class ExtractResult<TValue>
{
    private readonly TValue? _value;
    public bool Extracted { get; }

    private ExtractResult(TValue? value, bool extracted)
    {
        this._value = value;
        this.Extracted = extracted;
    }
    
    public TValue Value => this.Extracted ? this._value! : throw new InvalidOperationException("Cannot access value when nothing was extracted");
    
    public static ExtractResult<TValue> Return(TValue value) => new(value, true);
    public static ExtractResult<TValue> Empty() => new(default, false);
    public static implicit operator ValueTask<ExtractResult<TValue>>(ExtractResult<TValue> result) => ValueTask.FromResult(result);
}