namespace Orleans.Pipeline.Shared;

[GenerateSerializer]
[Alias("Orleans.Pipeline.Shared.PipeTransferItem`1")]
public class PipeTransferItem<T>
{
    [Id(0)]
    public required TransferMode Mode { get; init; }

    [Id(1)]
    public T? Item { get; init; }
    
    [Id(2)]
    public required string Metadata { get; init; }
}
