var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSmtpEmailSender(options => builder.Configuration.GetSection("Smtp").Bind(options));
builder.Services.AddMembershipEmail(options => builder.Configuration.GetSection("MembershipEmail").Bind(options));

var app = builder.Build();

// En una aplicación real es Persiltech.Membership quien llama a estos puertos. Aquí los
// dispara un endpoint para poder ver el correo que sale desde el archivo .http.
app.MapPost("/notifications/email-confirmation", async (
    NotificationRequest request,
    IMembershipEmailSender emailSender,
    CancellationToken cancellationToken) =>
{
    await emailSender.SendEmailConfirmationAsync(
        new EmailConfirmationMessage(
            request.UserId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Token),
        cancellationToken);

    return Results.NoContent();
});

app.MapPost("/notifications/password-reset", async (
    NotificationRequest request,
    IMembershipEmailSender emailSender,
    CancellationToken cancellationToken) =>
{
    await emailSender.SendPasswordResetAsync(
        new PasswordResetMessage(
            request.UserId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Token),
        cancellationToken);

    return Results.NoContent();
});

app.MapPost("/notifications/email-change", async (
    NotificationRequest request,
    IMembershipEmailSender emailSender,
    CancellationToken cancellationToken) =>
{
    await emailSender.SendEmailChangeAsync(
        new EmailChangeMessage(
            request.UserId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Token),
        cancellationToken);

    return Results.NoContent();
});

app.Run();
