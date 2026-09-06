namespace Infrastructure.Save
{
    public interface ISaveService
    {
        void SaveInt(string key, int value);
        int LoadInt(string key, int defaultValue = 0);
    }
}