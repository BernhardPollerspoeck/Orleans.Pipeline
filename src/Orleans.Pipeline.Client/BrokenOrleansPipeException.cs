namespace Orleans.Pipeline.Client;

public class BrokenOrleansPipeException(string message) : Exception(message);