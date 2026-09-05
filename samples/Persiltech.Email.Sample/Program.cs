var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSmtpEmailSender(options => builder.Configuration.GetSection("Smtp").Bind(options));

var app = builder.Build();

// El endpoint es del consumidor: el paquete solo aporta IEmailSender. Está aquí para poder
// ejercitar el envío desde el archivo .http.
app.MapPost("/email", async (
    SendEmailRequest request,
    IEmailSender emailSender,
    CancellationToken cancellationToken) =>
{
    var message = new EmailMessage
    {
        To = request.To,
        Subject = request.Subject,
        HtmlBody = request.HtmlBody,
        TextBody = request.TextBody
    };

    await emailSender.SendAsync(message, cancellationToken);

    return Results.NoContent();
});

app.Run();
