using System.Reflection;
using Application.Abstracts;
using Davish.Result;
using Davish.Sendr;

namespace Application.BehaviorDecorator;

// 只對實作了 ICacheableQuery 的 request 生效,其他 request 直接放行、不碰快取。
//
// TResponse 在這裡的實際情況一律是 Result<TValue> —— 但 Davish.Result 的建構子是
// internal,System.Text.Json 沒有任何可用的建構子可以拿來反序列化整個 Result<TValue>。
// 只能反序列化拆出來、單獨快取的 TValue(一般的 record,序列化沒問題),cache hit 時
// 再用 Result.Success(value) 包回去。TValue 在編譯期不知道是什麼,只能靠反射在執行期
// 動態決定、動態呼叫 ICacher 對應的泛型方法。
public sealed class CachingDecorator(ICacher cacher) : IRequestDecorator.WithResponse
{
    private static readonly MethodInfo GetAsyncDefinition = typeof(ICacher).GetMethod(nameof(ICacher.GetAsync))!;
    private static readonly MethodInfo SetAsyncDefinition = typeof(ICacher).GetMethod(nameof(ICacher.SetAsync))!;

    private static readonly MethodInfo SuccessDefinition = typeof(Result).GetMethods()
        .Single(m => m.Name == nameof(Result.Success) && m.IsGenericMethod);

    public async Task<TResponse> HandleAsync<TRequest, TResponse>(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    ) where TRequest : IRequest<TResponse>
    {
        if (request is not ICacheableQuery cacheable)
            return await next();

        var valueType = typeof(TResponse).GetGenericArguments()[0];

        var cachedValue = await InvokeGetAsync(valueType, cacheable.CacheKey, cancellationToken);

        if (cachedValue is not null)
            return (TResponse)SuccessDefinition.MakeGenericMethod(valueType).Invoke(null, [cachedValue])!;

        var response = await next();

        if (response is Result { IsSuccess: true })
        {
            var value = typeof(TResponse).GetProperty("Value")!.GetValue(response)!;
            await InvokeSetAsync(valueType, cacheable.CacheKey, value, cacheable.CacheTtl, cancellationToken);
        }

        return response;
    }

    private async Task<object?> InvokeGetAsync(Type valueType, string key, CancellationToken ct)
    {
        var task = (Task)GetAsyncDefinition.MakeGenericMethod(valueType).Invoke(cacher, [key, ct])!;
        await task;

        return task.GetType().GetProperty("Result")!.GetValue(task);
    }

    private async Task InvokeSetAsync(Type valueType, string key, object value, TimeSpan ttl, CancellationToken ct)
    {
        var task = (Task)SetAsyncDefinition.MakeGenericMethod(valueType).Invoke(cacher, [key, value, ttl, ct])!;
        await task;
    }
}
