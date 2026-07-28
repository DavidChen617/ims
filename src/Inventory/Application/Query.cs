using Davish.Sendr;

namespace Application;

public interface IQuery<out TResponse> : IRequest<TResponse> where TResponse : notnull;

public interface IQueryHandler<in TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IQuery<TResponse> where TResponse : notnull;
