using System;
using System.Collections.Generic;

public static class Submission
{
    public static string ResolveGreeting(string name)
    {
        var services = new Dictionary<Type, Func<object>> { [typeof(IGreeting)] = () => new FrenchGreeting() };
        return ((IGreeting)services[typeof(IGreeting)]()).Greet(name);
    }

    private interface IGreeting { string Greet(string name); }
    private sealed class FrenchGreeting : IGreeting { public string Greet(string name) => $"Bonjour {name.Trim()}"; }
}
