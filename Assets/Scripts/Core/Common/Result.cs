namespace Core.Common
{
    public readonly struct Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }

        private Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, string.Empty);
        public static Result Fail(string error) => new(false, error);
    }
}