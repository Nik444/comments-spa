using Comments.Realtime.Broadcasting;
using Comments.Realtime.Hubs;
using Comments.Realtime.Messaging;
using Comments.Realtime.Options;
using Microsoft.AspNetCore.Http.Connections;

var builder = WebApplication.CreateBuilder(args);

// [разбор] Настройки читаются из appsettings.json (разделы "RabbitMq" и "Realtime") в объекты RabbitMqOptions и RealtimeOptions.
// [разбор] ValidateOnStart: если значение неверное (например, порт 0), сервис не запустится и сразу скажет почему.
builder.Services.AddOptions<RabbitMqOptions>()
    .BindConfiguration(RabbitMqOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RealtimeOptions>()
    .BindConfiguration(RealtimeOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// [разбор] Список разрешённых сайтов нужен уже сейчас, до построения приложения, поэтому читаем его вручную.
var allowedOrigins = builder.Configuration.GetSection(RealtimeOptions.SectionName).Get<RealtimeOptions>()?.AllowedOrigins ?? [];

// [разбор] CORS: браузер не даёт странице с одного адреса (например, фронтенд на :5173) обращаться к другому (:5170),
// [разбор] если сервер явно не разрешил. Здесь разрешаем: либо только сайты из списка, либо любые, если список пуст.
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins);
    else
        policy.AllowAnyOrigin();

    policy.AllowAnyHeader().AllowAnyMethod();
}));

// [разбор] Включает SignalR. Сам хаб (CommentsHub) подключается ниже через MapHub.
builder.Services.AddSignalR();

// [разбор] Буфер один на весь сервис (Singleton): в него пишет приём из RabbitMQ, а читает рассылка клиентам.
builder.Services.AddSingleton<CommentEventBuffer>();

// [разбор] Две фоновые задачи, которые стартуют вместе с сервисом и работают, пока он жив:
// [разбор] первая принимает события из RabbitMQ, вторая раз в секунду рассылает их клиентам.
builder.Services.AddHostedService<RabbitMqCommentEventConsumer>();
builder.Services.AddHostedService<CommentSignalBroadcaster>();

var app = builder.Build();

app.UseCors();

// Страница для ручной проверки: открывается на корневом адресе сервиса только при разработке.
if (app.Environment.IsDevelopment())
{
    // [разбор] UseDefaultFiles: адрес "/" превращается в "/index.html". UseStaticFiles: отдаёт файлы из папки wwwroot.
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

// Только WebSocket: без запроса negotiate и без запасных транспортов, они при большом числе подключений не нужны.
// [разбор] Браузеры подключаются к адресу /hubs/comments. Transports говорит, что кроме WebSocket ничего не принимаем.
app.MapHub<CommentsHub>("/hubs/comments", hub => hub.Transports = HttpTransportType.WebSockets);

// [разбор] Простой адрес для проверки, что сервис жив (потом пригодится балансировщику).
app.MapGet("/health", () => Results.Ok());

app.Run();
