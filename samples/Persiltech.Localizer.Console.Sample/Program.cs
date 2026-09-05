Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
Console.WriteLine(Messages.Hello);

Thread.CurrentThread.CurrentUICulture = new CultureInfo("es-PE");
Console.WriteLine(Messages.Hello);

Console.WriteLine(Messages.HelloIn(new CultureInfo("en-US")));
Console.WriteLine(Messages.Hello);

using (new CultureScope(new CultureInfo("en-US")))
{
    Console.WriteLine(Messages.Hello);
}

Console.WriteLine(Messages.Hello);
