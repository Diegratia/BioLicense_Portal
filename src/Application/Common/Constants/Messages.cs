namespace BioLicense_Portal.Application.Common.Constants
{
    public static class Messages
    {
        public static class Auth
        {
            public const string LoginSuccess = "Login successful";
            public const string InvalidCredentials = "Invalid username or password";
            public const string SeedSuccess = "Owner seeded successfully";
            public const string Unauthorized = "Unauthorized access";
            public const string Forbidden = "Access denied";
        }

        public static class General
        {
            public const string NotFound = "Resource not found";
            public const string InternalError = "An unexpected error occurred";
            public const string ValidationError = "Validation failed";
            public const string Success = "Request processed successfully";
            public const string BadRequest = "Invalid request parameters";
        }

        public static class License
        {
            public const string Created = "License request created successfully";
            public const string Approved = "License request approved";
            public const string Rejected = "License request rejected";
            public const string Generated = "License generated successfully";
        }
    }
}
