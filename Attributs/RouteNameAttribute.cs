using System;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class RouteNameAttribute : Attribute
{
    public string RouteName { get; }

    public RouteNameAttribute(string routeName)
    {
        RouteName = routeName;
    }
}
