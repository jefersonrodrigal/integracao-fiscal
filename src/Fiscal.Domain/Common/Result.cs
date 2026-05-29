namespace Fiscal.Domain.Common;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string Error { get; }
    public IReadOnlyList<string> Errors { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = string.Empty;
        Errors = [];
    }

    private Result(string error, IEnumerable<string>? errors = null)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
        Errors = errors?.ToList().AsReadOnly() ?? [error];
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
    public static Result<T> Failure(IEnumerable<string> errors)
    {
        var list = errors.ToList();
        return new(list.FirstOrDefault() ?? "Erro desconhecido", list);
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error);
}

public sealed class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public IReadOnlyList<string> Errors { get; }

    private Result()
    {
        IsSuccess = true;
        Error = string.Empty;
        Errors = [];
    }

    private Result(string error, IEnumerable<string>? errors = null)
    {
        IsSuccess = false;
        Error = error;
        Errors = errors?.ToList().AsReadOnly() ?? [error];
    }

    public static Result Success() => new();
    public static Result Failure(string error) => new(error);
    public static Result Failure(IEnumerable<string> errors)
    {
        var list = errors.ToList();
        return new(list.FirstOrDefault() ?? "Erro desconhecido", list);
    }
}
