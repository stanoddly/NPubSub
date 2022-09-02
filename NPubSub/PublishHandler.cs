namespace NPubSub;

public delegate Task PublishHandler<in TArg>(TArg arg);

public static class PublishHandlerExtension
{
    public static Task SafeInvoke<TArg>(this PublishHandler<TArg>? publishHandler, TArg arg)
    {
        return publishHandler?.Invoke(arg) ?? Task.CompletedTask;
    } 
}
