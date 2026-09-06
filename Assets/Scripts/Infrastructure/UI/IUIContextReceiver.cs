namespace Infrastructure.UI
{
    public interface IUIContextReceiver<in TContext>
    {
        void SetContext(TContext context);
    }
}