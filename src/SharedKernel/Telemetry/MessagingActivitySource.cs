using System.Diagnostics;

namespace SharedKernel.Telemetry;

public static class MessagingActivitySource
{
    public const string Name = "Ims.Messaging";

    public static readonly ActivitySource Instance = new(Name, "1.0.0");
}
