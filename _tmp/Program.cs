using System;
using System.Text.Json;

public abstract record Base(int Id, string Name);
public sealed record Derived(int Id, string Name, string Extra) : Base(Id, Name);

var d = new Derived(1, "n", "extra");
Base b = d;
Console.WriteLine("via base: " + JsonSerializer.Serialize(b));
Console.WriteLine("via derived: " + JsonSerializer.Serialize(d));
