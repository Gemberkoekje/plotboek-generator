using Qowaiv.Customization;
using Qowaiv.OpenApi;

namespace PlotboekGenerator.core.Models;

[OpenApiDataType(
    description: "Feedback Id",
    type: "Uuid",
    example: "5f2b8c14-9d0a-4f6e-8b21-3c7e5a9d1f04")]
[Id<UuidBehavior, Uuid>]
public readonly partial struct FeedbackId { }
