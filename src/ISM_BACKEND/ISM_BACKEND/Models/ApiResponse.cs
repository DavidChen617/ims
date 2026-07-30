namespace ISM_BACKEND.Models;

public class ApiResponse<T>
{
    public bool success { get; set; }
    public string? user_message { get; set; }
    public T? data { get; set; }

    public static ApiResponse<T> Ok(T data, string? userMessage = null) => new() { success = true, data = data, user_message = userMessage };
    public static ApiResponse<T> Fail(string userMessage) => new() { success = false, user_message = userMessage };
}
