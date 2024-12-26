namespace Orleans.Pipeline.Shared;

/// <summary>
/// The transfer item for the pipe. This can either contain user data or other internal controll information
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Mode"></param>
/// <param name="Item"></param>
/// <param name="Metadata"></param>
[GenerateSerializer, Immutable]
[Alias("Orleans.Pipeline.Shared.PipeTransferItem`1")]
public readonly record struct PipeTransferItem<T>(TransferMode Mode, T? Item, string Metadata);
