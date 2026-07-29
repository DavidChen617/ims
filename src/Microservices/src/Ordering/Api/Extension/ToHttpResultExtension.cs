using Davish.Result;

namespace Api.Extension;

public static class ToHttpResultExtension
{
    private static readonly Dictionary<ErrorType, int> ErrorTypeMap = new([
        new(ErrorType.Validation, StatusCodes.Status400BadRequest),
        new(ErrorType.NullValue, StatusCodes.Status404NotFound),
        new(ErrorType.BadRequest, StatusCodes.Status400BadRequest),
        new(ErrorType.Conflict, StatusCodes.Status409Conflict),
        new(ErrorType.Forbidden, StatusCodes.Status403Forbidden),

        new(ErrorType.NotFound, StatusCodes.Status404NotFound),
        new(ErrorType.ServiceUnavailable, StatusCodes.Status503ServiceUnavailable),
        new(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized),
        new(ErrorType.Unexpected, StatusCodes.Status500InternalServerError)
    ]);

    extension(Result result)
    {
        public IResult ToOk() => !result.IsSuccess ? result.ToProblemDetails() : TypedResults.Ok();

        public IResult ToNoContent() => !result.IsSuccess ? result.ToProblemDetails() : TypedResults.NoContent();

        public IResult ToCreated() => !result.IsSuccess ? result.ToProblemDetails() : TypedResults.Created();

        public IResult ToCreated(Uri uri) => !result.IsSuccess ? result.ToProblemDetails() : TypedResults.Created(uri);

        public IResult ToCreatedAtRoute(string routeName, object? routeValues = null) =>
            !result.IsSuccess ? result.ToProblemDetails() : TypedResults.CreatedAtRoute(routeName, routeValues);

        public IResult ToAccepted(Uri uri) =>
            !result.IsSuccess ? result.ToProblemDetails() : TypedResults.Accepted(uri);

        public IResult ToProblemDetails()
        {
            var error = result.Error;

            return error.Fields.Count > 0
                ? result.ToValidationProblem()
                : TypedResults.Problem(
                    title: error.Code,
                    detail: error.Description,
                    statusCode: ErrorTypeMap.GetValueOrDefault(error.Type, StatusCodes.Status500InternalServerError)
                );
        }

        public IResult ToValidationProblem()
        {
            var error = result.Error;

            return TypedResults.ValidationProblem(
                title: error.Code,
                detail: error.Description,
                errors: error.Fields
                    .ToDictionary(x => x.Key, x => x.Value.ToArray())
                    .AsEnumerable()
            );
        }
    }

    extension<TValue>(Result<TValue> result) where TValue : notnull
    {
        public IResult ToOk() => !result.IsSuccess ? result.ToProblemDetails() : TypedResults.Ok(result.Value);

        public IResult ToCreated(Func<TValue, Uri> func) =>
            !result.IsSuccess ? result.ToProblemDetails() : TypedResults.Created(func(result.Value));

        public IResult ToCreatedAtRoute(string routeName, Func<TValue, object> routeValues) =>
            !result.IsSuccess ? result.ToProblemDetails() : TypedResults.CreatedAtRoute(routeName, routeValues(result.Value));

        public IResult ToAccepted(Func<TValue, Uri> func) =>
            !result.IsSuccess ? result.ToProblemDetails() : TypedResults.Accepted(func(result.Value));

        public IResult ToJson() => !result.IsSuccess
            ? result.ToProblemDetails()
            : TypedResults.Json(result.Value, statusCode: StatusCodes.Status200OK);
    }
}
