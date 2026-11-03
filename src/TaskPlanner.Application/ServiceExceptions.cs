namespace TaskPlanner.Application;

public class ServiceNotFoundException : Exception
{
    public ServiceNotFoundException(string message) : base(message)
    {
    }
}

public class ServiceValidationException : Exception
{
    public ServiceValidationException(string message) : base(message)
    {
    }
}

public class ServiceForbiddenException : Exception
{
    public ServiceForbiddenException(string message) : base(message)
    {
    }
}
