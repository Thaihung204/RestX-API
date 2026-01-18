namespace RestX.BLL.DTOs.Auth
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        public static AuthResponseDto SuccessResponse(string message, object? data = null)
        {
            return new AuthResponseDto
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static AuthResponseDto FailureResponse(string message)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = message,
                Data = null
            };
        }
    }
}
