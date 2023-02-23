namespace TippingProject.Models
{
    public class LoginCredentials
    {
        public string name { get; set; }
        public string password { get; set; }
    }
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}