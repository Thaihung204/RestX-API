namespace RestX.BLL.DataTranferObjects.Auth
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public static AuthResponse SuccessResponse(string message, object? data = null)
        {
            return new AuthResponse
            {
                Success = true,
                Message = message,
                Data = data
            };
        }
        public static AuthResponse FailureResponse(string message)
        {
            return new AuthResponse
            {
                Success = false,
                Message = message,
                Data = null
            };
        }
    }
}
