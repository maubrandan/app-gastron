using Microsoft.AspNetCore.Mvc;

namespace Resto.Api.Infrastructure;

public static class ApiProblems
{
    public const string DomainErrorType = "https://resto.local/errors/domain";
    public const string ConcurrencyConflictType = "https://resto.local/errors/concurrency-conflict";
    public const string NotFoundErrorType = "https://resto.local/errors/not-found";
    public const string UnauthorizedErrorType = "https://resto.local/errors/unauthorized";
    public const string InternalErrorType = "https://resto.local/errors/internal";

    public static IActionResult BusinessError(this ControllerBase controller, string detail) =>
        controller.Problem(
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Error de negocio",
            type: DomainErrorType);

    public static IActionResult ConcurrencyConflict(this ControllerBase controller, string detail) =>
        controller.Problem(
            detail: detail,
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflicto de concurrencia",
            type: ConcurrencyConflictType);

    public static IActionResult NotFoundError(this ControllerBase controller, string detail) =>
        controller.Problem(
            detail: detail,
            statusCode: StatusCodes.Status404NotFound,
            title: "Recurso no encontrado",
            type: NotFoundErrorType);

    public static IActionResult UnauthorizedError(this ControllerBase controller, string detail) =>
        controller.Problem(
            detail: detail,
            statusCode: StatusCodes.Status401Unauthorized,
            title: "No autorizado",
            type: UnauthorizedErrorType);

    public static object ToPayload(int status, string type, string title, string detail) =>
        new { type, title, status, detail };
}
