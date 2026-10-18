namespace Tracker.Domain;

public abstract class Result<T>
{
    private Result() { }

    public sealed class Success : Result<T>
    {
        public T Value { get; }
        internal Success(T value) => Value = value;
    }

    public sealed class Failure : Result<T>
    {
        public string Error { get; }
        internal Failure(string error) => Error = error;
    }

    public static Result<T> Ok(T value) => new Success(value);
    public static Result<T> Fail(string error) => new Failure(error);

    // Map wraps an ordinary returned value; a failure keeps its error.
    public Result<U> Map<U>(Func<T, U> transform) => this switch
    {
        Success success => Result<U>.Ok(transform(success.Value)),
        Failure failure => Result<U>.Fail(failure.Error),
        _ => throw new InvalidOperationException("Unknown result.")
    };

    // Bind joins a step which can itself return a failure.
    public Result<U> Bind<U>(Func<T, Result<U>> next) => this switch
    {
        Success success => next(success.Value),
        Failure failure => Result<U>.Fail(failure.Error),
        _ => throw new InvalidOperationException("Unknown result.")
    };
}
