namespace Orleans.Pipeline.Shared;

[GenerateSerializer, Immutable]
[Alias("Orleans.Pipeline.Shared.PipeTransferItem`1")]
public readonly record struct PipeTransferItem<T>(TransferMode Mode, T? Item, string Metadata);
