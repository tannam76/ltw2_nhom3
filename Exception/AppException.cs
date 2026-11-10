namespace CourseManagement.Exceptions
{
    public class AppException : Exception
    {
        public int StatusCode { get; }

        public AppException(string message, int statusCode = StatusCodes.Status400BadRequest)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message)
            : base(message, StatusCodes.Status404NotFound)
        {
        }
    }

    public class BadRequestException : AppException
    {
        public BadRequestException(string message)
            : base(message, StatusCodes.Status400BadRequest)
        {
        }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message)
            : base(message, StatusCodes.Status401Unauthorized)
        {
        }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message)
            : base(message, StatusCodes.Status409Conflict)
        {
        }
    }
}

namespace ProjectAPIdemo.Exceptions
{
    public class AppException(string message, int statusCode = StatusCodes.Status400BadRequest)
        : CourseManagement.Exceptions.AppException(message, statusCode);

    public class NotFoundException(string message)
        : CourseManagement.Exceptions.NotFoundException(message);

    public class BadRequestException(string message)
        : CourseManagement.Exceptions.BadRequestException(message);

    public class UnauthorizedException(string message)
        : CourseManagement.Exceptions.UnauthorizedException(message);

    public class ConflictException(string message)
        : CourseManagement.Exceptions.ConflictException(message);
}
