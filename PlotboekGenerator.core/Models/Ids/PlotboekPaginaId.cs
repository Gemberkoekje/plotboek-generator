using Qowaiv.Customization;
using Qowaiv.OpenApi;

namespace PlotboekGenerator.core.Models;

[OpenApiDataType(
    description: "Plotboek Pagina Id",
    type: "Uuid",
    example: "a3c01573-7941-4d1d-a6f7-6f35d003595a")]
[Id<UuidBehavior, Uuid>]
public readonly partial struct PlotboekPaginaId { }
